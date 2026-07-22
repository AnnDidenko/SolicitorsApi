using System.Collections.Concurrent;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;
using SolicitorsApi.Infrastructure.SolicitorsCom.Clients;
using SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Gateways;

public class SolicitorsComSolicitorSearchGateway : ISolicitorSearchGateway
{
    private readonly ISolicitorSearchPageClient _searchPageClient;
    private readonly ISolicitorResultsParser _resultsParser;
    private readonly ISolicitorProfilePageClient _profilePageClient;
    private readonly ISolicitorProfileParser _profileParser;

    public SolicitorsComSolicitorSearchGateway(
        ISolicitorSearchPageClient searchPageClient,
        ISolicitorResultsParser resultsParser,
        ISolicitorProfilePageClient profilePageClient,
        ISolicitorProfileParser profileParser)
    {
        _searchPageClient = searchPageClient;
        _resultsParser = resultsParser;
        _profilePageClient = profilePageClient;
        _profileParser = profileParser;
    }

    public async Task<SolicitorSearchData> SearchAsync(
        IReadOnlyList<string> locations,
        AreaOfLaw? areaOfLaw,
        CancellationToken cancellationToken)
    {
        var solicitors = new List<Solicitor>();
        var locationResults = new List<LocationSearchResult>();
        var failures = new List<ScrapeFailure>();

        foreach (var location in locations)
        {
            try
            {
                var page = await _searchPageClient.GetSearchPageAsync(
                    location,
                    areaOfLaw,
                    cancellationToken);
                var parsed = _resultsParser.Parse(page.Html, page.RequestUri, location);

                solicitors.AddRange(parsed.Solicitors);
                locationResults.Add(new LocationSearchResult
                {
                    Location = location,
                    Count = parsed.Solicitors.Count
                });
            }
            catch (Exception exception) when (
                exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
            {
                failures.Add(new ScrapeFailure
                {
                    Location = location,
                    Code = "searchPageFailed",
                    Message = $"Solicitors could not be loaded for '{location}'. Please try again later."
                });
                locationResults.Add(new LocationSearchResult
                {
                    Location = location,
                    Count = 0
                });
            }
        }

        return new SolicitorSearchData
        {
            Solicitors = solicitors,
            LocationResults = locationResults,
            Failures = failures
        };
    }

    public async Task<IReadOnlyDictionary<string, SolicitorProfile>> GetProfilesAsync(
        IReadOnlyList<Solicitor> solicitors,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        var profiles = new ConcurrentDictionary<string, SolicitorProfile>(StringComparer.OrdinalIgnoreCase);
        var candidates = solicitors
            .Where(solicitor =>
                !string.IsNullOrWhiteSpace(solicitor.ProfileSlug) ||
                !string.IsNullOrWhiteSpace(solicitor.ProfileUrl))
            .GroupBy(GetProfileLookupValue, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        using var semaphore = new SemaphoreSlim(Math.Max(1, maxConcurrency));
        var tasks = candidates.Select(async solicitor =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                var lookupValue = GetProfileLookupValue(solicitor);
                var page = await _profilePageClient.GetProfilePageAsync(lookupValue, cancellationToken);
                var profile = _profileParser.Parse(page.Html, page.RequestUri);
                var key = string.IsNullOrWhiteSpace(solicitor.ProfileSlug)
                    ? profile.Slug
                    : solicitor.ProfileSlug;

                if (!string.IsNullOrWhiteSpace(key))
                {
                    profiles.TryAdd(key, profile);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        return profiles;
    }

    private static string GetProfileLookupValue(Solicitor solicitor)
    {
        if (!string.IsNullOrWhiteSpace(solicitor.ProfileSlug))
        {
            return solicitor.ProfileSlug;
        }

        return solicitor.ProfileUrl ?? string.Empty;
    }
}
