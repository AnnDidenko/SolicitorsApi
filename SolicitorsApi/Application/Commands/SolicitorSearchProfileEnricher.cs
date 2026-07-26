using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Cache;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Application.Search;
using SolicitorsApi.Domain;
using System.Diagnostics;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchProfileEnricher : ISolicitorSearchProfileEnricher
{
    private readonly ISolicitorSearchGateway _solicitorSearchGateway;
    private readonly ISolicitorSearchSorter _searchSorter;
    private readonly SolicitorSearchSettings _settings;
    private readonly ISolicitorProfileCache? _profileCache;
    private readonly SolicitorIdentityKeyBuilder _identityKeyBuilder;
    private readonly ISearchPerformanceMetrics _metrics;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(24);

    public SolicitorSearchProfileEnricher(
        ISolicitorSearchGateway solicitorSearchGateway,
        ISolicitorSearchSorter searchSorter,
        IOptions<SolicitorSearchSettings> options,
        ISolicitorProfileCache? profileCache = null,
        SolicitorIdentityKeyBuilder? identityKeyBuilder = null,
        ISearchPerformanceMetrics? metrics = null)
    {
        _solicitorSearchGateway = solicitorSearchGateway;
        _searchSorter = searchSorter;
        _settings = options.Value;
        _profileCache = profileCache;
        _identityKeyBuilder = identityKeyBuilder ?? new SolicitorIdentityKeyBuilder();
        _metrics = metrics ?? NoOpSearchPerformanceMetrics.Instance;
    }

    public async Task<ApplicationResult<IReadOnlyList<Solicitor>>> EnrichIfRequiredAsync(
        IReadOnlyList<Solicitor> solicitors,
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchSort sort,
        CancellationToken cancellationToken)
    {
        if (!RequiresProfileReviewData(command, sort) ||
            (_profileCache is null && SolicitorsAlreadyHaveRequiredProfileData(solicitors)))
        {
            return ApplicationResult<IReadOnlyList<Solicitor>>.Ok(solicitors);
        }

        var profiles = await GetProfilesAsync(solicitors, cancellationToken);

        return profiles.IsSuccess
            ? ApplicationResult<IReadOnlyList<Solicitor>>.Ok(
                EnrichSolicitors(solicitors, profiles.Value ?? new Dictionary<string, SolicitorProfile>()))
            : ApplicationResult<IReadOnlyList<Solicitor>>.FailedDependency(profiles.Errors);
    }

    private bool RequiresProfileReviewData(
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchSort sort)
    {
        return command.MinimumReviewScore.HasValue ||
            _searchSorter.RequiresProfileReviewData(sort);
    }

    private static bool SolicitorsAlreadyHaveRequiredProfileData(IReadOnlyList<Solicitor> solicitors)
    {
        return solicitors.All(solicitor => solicitor.Review is not null);
    }

    private async Task<ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>> GetProfilesAsync(
        IReadOnlyList<Solicitor> solicitors,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var fetchCount = 0;
        var status = "success";

        if (_profileCache is not null)
        {
            var result = await GetProfilesWithCacheAsync(solicitors, cancellationToken);

            _metrics.RecordProfileEnrichment(
                result.FetchCount,
                result.CacheHitCount,
                result.CacheMissCount,
                result.Status,
                result.Elapsed);

            return result.Profiles;
        }

        try
        {
            var profiles = await _solicitorSearchGateway.GetProfilesAsync(
                solicitors,
                _settings.ProfileFetchConcurrency,
                cancellationToken);
            fetchCount = solicitors.Count;

            return ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.Ok(profiles);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            status = "failedDependency";

            return ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.FailedDependency(
                [new ApplicationError(
                    "profileEnrichmentFailed",
                    "Solicitor profile details could not be loaded. Please try again later.")]);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordProfileEnrichment(
                fetchCount,
                0,
                solicitors.Count,
                status,
                stopwatch.Elapsed);
        }
    }

    private async Task<ProfileEnrichmentMetricsResult> GetProfilesWithCacheAsync(
        IReadOnlyList<Solicitor> solicitors,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var identitiesBySolicitor = solicitors.ToDictionary(
            solicitor => solicitor,
            solicitor => _identityKeyBuilder.Build(solicitor));
        var cachedRecords = await _profileCache!.GetBySourceIdentitiesAsync(
            identitiesBySolicitor.Values.Distinct(StringComparer.Ordinal).ToArray(),
            cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var cachedProfiles = new Dictionary<string, SolicitorProfile>(StringComparer.OrdinalIgnoreCase);
        var solicitorsToFetch = new List<Solicitor>();
        var cacheHitCount = 0;
        var cacheMissCount = 0;
        var fetchCount = 0;

        foreach (var solicitor in solicitors)
        {
            var identity = identitiesBySolicitor[solicitor];

            if (cachedRecords.TryGetValue(identity, out var record) &&
                record.ExpiresAt > now &&
                record.Profile is not null &&
                !string.IsNullOrWhiteSpace(solicitor.ProfileSlug) &&
                HasRequiredProfileData(record.Profile))
            {
                cachedProfiles[solicitor.ProfileSlug] = record.Profile;
                cacheHitCount++;

                continue;
            }

            if (solicitor.Review is not null)
            {
                continue;
            }

            cacheMissCount++;
            solicitorsToFetch.Add(solicitor);
        }

        if (solicitorsToFetch.Count == 0)
        {
            stopwatch.Stop();

            return new ProfileEnrichmentMetricsResult(
                ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.Ok(cachedProfiles),
                fetchCount,
                cacheHitCount,
                cacheMissCount,
                "success",
                stopwatch.Elapsed);
        }

        var fetchedProfiles = await GetProfilesFromGatewayAsync(solicitorsToFetch, cancellationToken);
        fetchCount = solicitorsToFetch.Count;

        if (!fetchedProfiles.IsSuccess)
        {
            stopwatch.Stop();

            return new ProfileEnrichmentMetricsResult(
                ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.FailedDependency(fetchedProfiles.Errors),
                fetchCount,
                cacheHitCount,
                cacheMissCount,
                "failedDependency",
                stopwatch.Elapsed);
        }

        foreach (var profile in fetchedProfiles.Value ?? new Dictionary<string, SolicitorProfile>())
        {
            cachedProfiles[profile.Key] = profile.Value;
        }

        await StoreFetchedProfilesAsync(solicitorsToFetch, fetchedProfiles.Value ?? new Dictionary<string, SolicitorProfile>(), now, cancellationToken);

        stopwatch.Stop();

        return new ProfileEnrichmentMetricsResult(
            ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.Ok(cachedProfiles),
            fetchCount,
            cacheHitCount,
            cacheMissCount,
            "success",
            stopwatch.Elapsed);
    }

    private async Task<ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>> GetProfilesFromGatewayAsync(
        IReadOnlyList<Solicitor> solicitors,
        CancellationToken cancellationToken)
    {
        try
        {
            var profiles = await _solicitorSearchGateway.GetProfilesAsync(
                solicitors,
                _settings.ProfileFetchConcurrency,
                cancellationToken);

            return ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.Ok(profiles);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            return ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.FailedDependency(
                [new ApplicationError(
                    "profileEnrichmentFailed",
                    "Solicitor profile details could not be loaded. Please try again later.")]);
        }
    }

    private async Task StoreFetchedProfilesAsync(
        IReadOnlyList<Solicitor> solicitors,
        IReadOnlyDictionary<string, SolicitorProfile> fetchedProfiles,
        DateTimeOffset fetchedAt,
        CancellationToken cancellationToken)
    {
        foreach (var solicitor in solicitors)
        {
            if (string.IsNullOrWhiteSpace(solicitor.ProfileSlug) ||
                !fetchedProfiles.TryGetValue(solicitor.ProfileSlug, out var profile))
            {
                continue;
            }

            await _profileCache!.UpsertProfileDetailsAsync(
                new SolicitorProfileCacheRecord
                {
                    SourceIdentity = _identityKeyBuilder.Build(solicitor),
                    Solicitor = solicitor,
                    Profile = profile,
                    LastSeenAt = fetchedAt,
                    ProfileFetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.Add(DefaultCacheDuration)
                },
                cancellationToken);
        }
    }

    private static bool HasRequiredProfileData(SolicitorProfile profile)
    {
        return profile.Review is not null;
    }

    private static IReadOnlyList<Solicitor> EnrichSolicitors(
        IReadOnlyList<Solicitor> solicitors,
        IReadOnlyDictionary<string, SolicitorProfile> profiles)
    {
        return solicitors.Select(solicitor =>
        {
            if (string.IsNullOrWhiteSpace(solicitor.ProfileSlug) ||
                !profiles.TryGetValue(solicitor.ProfileSlug, out var profile))
            {
                return solicitor;
            }

            return solicitor with
            {
                ContactDetails = profile.ContactDetails,
                AreasOfLaw = profile.AreasOfLaw,
                Review = profile.Review
            };
        }).ToArray();
    }

    private readonly record struct ProfileEnrichmentMetricsResult(
        ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>> Profiles,
        int FetchCount,
        int CacheHitCount,
        int CacheMissCount,
        string Status,
        TimeSpan Elapsed);
}
