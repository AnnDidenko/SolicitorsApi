using SolicitorsApi.Application.Search;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Application.Cache;
using SolicitorsApi.Domain;
using System.Diagnostics;

namespace SolicitorsApi.Application.Commands;

public class RunConveyancingSolicitorSearchHandler
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(24);

    private readonly ISolicitorSearchRequestValidator _validator;
    private readonly ISolicitorSearchRequestNormalizer _normalizer;
    private readonly ISolicitorSearchScrapeService _scrapeService;
    private readonly ISolicitorSearchProfileEnricher _profileEnricher;
    private readonly ISolicitorSearchFilter _searchFilter;
    private readonly ISolicitorSearchSorter _searchSorter;
    private readonly ISolicitorSearchResultFactory _resultFactory;
    private readonly ISolicitorSearchCache? _searchCache;
    private readonly ISolicitorProfileCache? _profileCache;
    private readonly ISearchPerformanceMetrics _metrics;
    private readonly SolicitorIdentityKeyBuilder _identityKeyBuilder = new();

    public RunConveyancingSolicitorSearchHandler(
        ISolicitorSearchRequestValidator validator,
        ISolicitorSearchRequestNormalizer normalizer,
        ISolicitorSearchScrapeService scrapeService,
        ISolicitorSearchProfileEnricher profileEnricher,
        ISolicitorSearchFilter searchFilter,
        ISolicitorSearchSorter searchSorter,
        ISolicitorSearchResultFactory resultFactory,
        ISolicitorSearchCache? searchCache = null,
        ISolicitorProfileCache? profileCache = null,
        ISearchPerformanceMetrics? metrics = null)
    {
        _validator = validator;
        _normalizer = normalizer;
        _scrapeService = scrapeService;
        _profileEnricher = profileEnricher;
        _searchFilter = searchFilter;
        _searchSorter = searchSorter;
        _resultFactory = resultFactory;
        _searchCache = searchCache;
        _profileCache = profileCache;
        _metrics = metrics ?? NoOpSearchPerformanceMetrics.Instance;
    }

    public async Task<ApplicationResult<SolicitorSearchResult>> HandleAsync(
        RunConveyancingSolicitorSearchCommand command,
        CancellationToken cancellationToken)
    {
        var context = await _normalizer.NormalizeAsync(command, cancellationToken);
        var validationErrors = await _validator.ValidateAsync(command, context, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SolicitorSearchResult>.Validation(validationErrors);
        }

        var stopwatch = Stopwatch.StartNew();
        var searchStatus = "success";

        try
        {
            var search = await RunSearchAsync(command, context, cancellationToken);
            searchStatus = search.Status;

            return search.Result;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordSearch(searchStatus, stopwatch.Elapsed);
        }
    }

    private async Task<SearchExecutionResult> RunSearchAsync(
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        var searchData = await ResolveSearchDataAsync(context, cancellationToken);

        if (!searchData.IsSuccess)
        {
            return SearchExecutionResult.FailedDependency(searchData.Errors);
        }

        var resolvedSearchData = searchData.Value!;
        var solicitors = resolvedSearchData.SearchData.Solicitors;
        var now = DateTimeOffset.UtcNow;

        await StoreFreshSearchDataAsync(resolvedSearchData, context, now, cancellationToken);
        await StoreDiscoveredSolicitorsAsync(solicitors, now, cancellationToken);

        var enrichedSolicitors = await EnrichSolicitorsAsync(solicitors, command, context, cancellationToken);

        if (!enrichedSolicitors.IsSuccess)
        {
            return SearchExecutionResult.FailedDependency(enrichedSolicitors.Errors);
        }

        var sortedSolicitors = FilterAndSortSolicitors(enrichedSolicitors.Value!, command, context);
        var cacheMetadata = CreateCacheMetadata(resolvedSearchData);
        var result = _resultFactory.Create(
            command,
            context,
            sortedSolicitors,
            resolvedSearchData.SearchData,
            cacheMetadata);

        return SearchExecutionResult.Success(result);
    }

    private async Task<ApplicationResult<ResolvedSearchData>> ResolveSearchDataAsync(
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        var cachedSearchData = await GetCachedSearchDataAsync(
            context,
            recordUnavailable: false,
            cancellationToken);

        if (cachedSearchData is not null)
        {
            return ApplicationResult<ResolvedSearchData>.Ok(ResolvedSearchData.FromCache(cachedSearchData.Value.SearchData, cachedSearchData.Value));
        }

        var searchData = await _scrapeService.SearchAsync(context, cancellationToken);

        if (!searchData.IsSuccess)
        {
            return await ResolveCompleteScrapeFailureAsync(context, searchData.Errors, cancellationToken);
        }

        return await ResolvePartialScrapeFailuresAsync(context, searchData.Value!, cancellationToken);
    }

    private async Task<ApplicationResult<ResolvedSearchData>> ResolveCompleteScrapeFailureAsync(
        SolicitorSearchExecutionContext context,
        IReadOnlyList<ApplicationError> scrapeErrors,
        CancellationToken cancellationToken)
    {
        _metrics.RecordFallback("list", "attempt");

        var cachedSearchData = await GetCachedSearchDataAsync(context, cancellationToken);

        if (cachedSearchData is null)
        {
            _metrics.RecordFallback("list", "miss");

            return ApplicationResult<ResolvedSearchData>.FailedDependency(scrapeErrors);
        }

        _metrics.RecordFallback("list", "hit");

        return ApplicationResult<ResolvedSearchData>.Ok(ResolvedSearchData.FromFallback(cachedSearchData.Value.SearchData, cachedSearchData.Value));
    }

    private async Task<ApplicationResult<ResolvedSearchData>> ResolvePartialScrapeFailuresAsync(
        SolicitorSearchExecutionContext context,
        SolicitorSearchData liveSearchData,
        CancellationToken cancellationToken)
    {
        if (liveSearchData.Failures.Count == 0)
        {
            return ApplicationResult<ResolvedSearchData>.Ok(ResolvedSearchData.FromFresh(liveSearchData));
        }

        _metrics.RecordFallback("list", "attempt");

        var failedLocations = GetFailedLocations(liveSearchData);
        var cachedSearchData = await GetCachedSearchDataAsync(context, failedLocations, cancellationToken);

        if (cachedSearchData is null)
        {
            _metrics.RecordFallback("list", "miss");

            return ApplicationResult<ResolvedSearchData>.FailedDependency(ToApplicationErrors(liveSearchData.Failures));
        }

        _metrics.RecordFallback("list", "hit");

        var mergedSearchData = MergeLiveAndCachedSearchData(liveSearchData, cachedSearchData.Value.SearchData);

        return ApplicationResult<ResolvedSearchData>.Ok(ResolvedSearchData.FromFallback(mergedSearchData, cachedSearchData.Value));
    }

    private async Task StoreFreshSearchDataAsync(
        ResolvedSearchData resolvedSearchData,
        SolicitorSearchExecutionContext context,
        DateTimeOffset fetchedAt,
        CancellationToken cancellationToken)
    {
        if (resolvedSearchData.UsedFallback)
        {
            return;
        }

        if (!resolvedSearchData.ShouldStoreListSegments)
        {
            return;
        }

        await StoreListSegmentsAsync(context, resolvedSearchData.SearchData, fetchedAt, cancellationToken);
    }

    private async Task<ApplicationResult<IReadOnlyList<Solicitor>>> EnrichSolicitorsAsync(
        IReadOnlyList<Solicitor> solicitors,
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        return await _profileEnricher.EnrichIfRequiredAsync(
            solicitors,
            command,
            context.Sort,
            cancellationToken);
    }

    private IReadOnlyList<Solicitor> FilterAndSortSolicitors(
        IReadOnlyList<Solicitor> solicitors,
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchExecutionContext context)
    {
        var filteredSolicitors = _searchFilter.Apply(solicitors, command.MinimumReviewScore);

        return _searchSorter.Apply(filteredSolicitors, context.Sort);
    }

    private static SolicitorSearchCacheMetadata CreateCacheMetadata(ResolvedSearchData resolvedSearchData)
    {
        var servedAt = DateTimeOffset.UtcNow;

        return new SolicitorSearchCacheMetadata
        {
            Status = resolvedSearchData.UsedFallback ? SolicitorSearchCacheStatus.Fallback : SolicitorSearchCacheStatus.Fresh,
            UsedFallback = resolvedSearchData.UsedFallback,
            FetchedAt = resolvedSearchData.FetchedAt ?? servedAt,
            ServedAt = servedAt,
            ExpiresAt = resolvedSearchData.ExpiresAt ?? servedAt.Add(DefaultCacheDuration)
        };
    }

    private static IReadOnlyCollection<string> GetFailedLocations(SolicitorSearchData searchData)
    {
        return searchData.Failures
            .Select(failure => failure.Location)
            .Where(location => !string.IsNullOrWhiteSpace(location))
            .Select(location => location!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<ApplicationError> ToApplicationErrors(IReadOnlyList<ScrapeFailure> failures)
    {
        return failures
            .Select(failure => new ApplicationError(
                failure.Code,
                failure.Message,
                failure.Location))
            .ToArray();
    }

    private async Task<CachedSearchData?> GetCachedSearchDataAsync(
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        return await GetCachedSearchDataAsync(context, context.Locations, cancellationToken: cancellationToken);
    }

    private async Task<CachedSearchData?> GetCachedSearchDataAsync(
        SolicitorSearchExecutionContext context,
        bool recordUnavailable,
        CancellationToken cancellationToken)
    {
        return await GetCachedSearchDataAsync(
            context,
            context.Locations,
            cancellationToken,
            recordUnavailable);
    }

    private async Task<CachedSearchData?> GetCachedSearchDataAsync(
        SolicitorSearchExecutionContext context,
        IReadOnlyCollection<string> locations,
        CancellationToken cancellationToken,
        bool recordUnavailable = true)
    {
        if (_searchCache is null)
        {
            return null;
        }

        var cachedSegments = new List<SolicitorListCacheEntry>();
        var now = DateTimeOffset.UtcNow;

        foreach (var location in locations)
        {
            var cachedSegment = await _searchCache.GetListSegmentAsync(
                location,
                context.AreaOfLaw?.Slug,
                cancellationToken);

            if (cachedSegment is null || cachedSegment.ExpiresAt <= now)
            {
                if (recordUnavailable)
                {
                    _metrics.RecordFallback(
                        "list",
                        cachedSegment is null ? "cacheMiss" : "expired");
                }

                return null;
            }

            cachedSegments.Add(cachedSegment);
        }

        return new CachedSearchData(
            new SolicitorSearchData
            {
                Solicitors = cachedSegments.SelectMany(segment => segment.Solicitors).ToArray(),
                LocationResults = cachedSegments.SelectMany(segment => segment.LocationResults).ToArray()
            },
            cachedSegments.Min(segment => segment.FetchedAt),
            cachedSegments.Min(segment => segment.ExpiresAt));
    }

    private static SolicitorSearchData MergeLiveAndCachedSearchData(
        SolicitorSearchData liveSearchData,
        SolicitorSearchData cachedSearchData)
    {
        var failedLocations = liveSearchData.Failures
            .Select(failure => failure.Location)
            .Where(location => !string.IsNullOrWhiteSpace(location))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return new SolicitorSearchData
        {
            Solicitors = liveSearchData.Solicitors
                .Concat(cachedSearchData.Solicitors)
                .ToArray(),
            LocationResults = liveSearchData.LocationResults
                .Where(result => !failedLocations.Contains(result.Location))
                .Concat(cachedSearchData.LocationResults)
                .ToArray()
        };
    }

    private async Task StoreListSegmentsAsync(
        SolicitorSearchExecutionContext context,
        SolicitorSearchData searchData,
        DateTimeOffset fetchedAt,
        CancellationToken cancellationToken)
    {
        if (_searchCache is null)
        {
            return;
        }

        foreach (var location in context.Locations)
        {
            await _searchCache.StoreListSegmentAsync(
                new SolicitorListCacheEntry
                {
                    Location = location,
                    AreaOfLawSlug = context.AreaOfLaw?.Slug,
                    Solicitors = searchData.Solicitors
                        .Where(solicitor => MatchesLocation(solicitor, location))
                        .ToArray(),
                    LocationResults = searchData.LocationResults
                        .Where(result => string.Equals(result.Location, location, StringComparison.OrdinalIgnoreCase))
                        .ToArray(),
                    FetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.Add(DefaultCacheDuration)
                },
                cancellationToken);
        }
    }

    private async Task StoreDiscoveredSolicitorsAsync(
        IReadOnlyList<Solicitor> solicitors,
        DateTimeOffset lastSeenAt,
        CancellationToken cancellationToken)
    {
        if (_profileCache is null || solicitors.Count == 0)
        {
            return;
        }

        var records = solicitors
            .Select(solicitor => new SolicitorProfileCacheRecord
            {
                SourceIdentity = _identityKeyBuilder.Build(solicitor),
                Solicitor = solicitor,
                LastSeenAt = lastSeenAt,
                ExpiresAt = lastSeenAt.Add(DefaultCacheDuration)
            })
            .ToArray();

        await _profileCache.UpsertDiscoveredSolicitorsAsync(records, cancellationToken);
    }

    private static bool MatchesLocation(Solicitor solicitor, string location)
    {
        return string.Equals(solicitor.Location, location, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(solicitor.City, location, StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct CachedSearchData(
        SolicitorSearchData SearchData,
        DateTimeOffset FetchedAt,
        DateTimeOffset ExpiresAt);

    private sealed record ResolvedSearchData(
        SolicitorSearchData SearchData,
        bool UsedFallback,
        bool ShouldStoreListSegments,
        DateTimeOffset? FetchedAt,
        DateTimeOffset? ExpiresAt)
    {
        public static ResolvedSearchData FromFresh(SolicitorSearchData searchData)
        {
            return new ResolvedSearchData(searchData, false, true, null, null);
        }

        public static ResolvedSearchData FromCache(
            SolicitorSearchData searchData,
            CachedSearchData cacheData)
        {
            return new ResolvedSearchData(searchData, false, false, cacheData.FetchedAt, cacheData.ExpiresAt);
        }

        public static ResolvedSearchData FromFallback(
            SolicitorSearchData searchData,
            CachedSearchData cacheData)
        {
            return new ResolvedSearchData(searchData, true, false, cacheData.FetchedAt, cacheData.ExpiresAt);
        }
    }

    private sealed record SearchExecutionResult(
        ApplicationResult<SolicitorSearchResult> Result,
        string Status)
    {
        public static SearchExecutionResult Success(SolicitorSearchResult result)
        {
            return new SearchExecutionResult(
                ApplicationResult<SolicitorSearchResult>.Ok(result),
                "success");
        }

        public static SearchExecutionResult FailedDependency(IReadOnlyList<ApplicationError> errors)
        {
            return new SearchExecutionResult(
                ApplicationResult<SolicitorSearchResult>.FailedDependency(errors),
                "failedDependency");
        }
    }
}
