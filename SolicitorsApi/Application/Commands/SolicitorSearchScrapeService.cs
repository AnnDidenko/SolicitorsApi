using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;
using System.Diagnostics;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchScrapeService : ISolicitorSearchScrapeService
{
    private readonly ISolicitorSearchGateway _solicitorSearchGateway;
    private readonly ISearchPerformanceMetrics _metrics;

    public SolicitorSearchScrapeService(
        ISolicitorSearchGateway solicitorSearchGateway,
        ISearchPerformanceMetrics? metrics = null)
    {
        _solicitorSearchGateway = solicitorSearchGateway;
        _metrics = metrics ?? NoOpSearchPerformanceMetrics.Instance;
    }

    public async Task<ApplicationResult<SolicitorSearchData>> SearchAsync(
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var status = "success";

        var searchData = await _solicitorSearchGateway.SearchAsync(
            context.Locations,
            context.AreaOfLaw,
            cancellationToken);

        stopwatch.Stop();
        status = searchData.Failures.Count > 0 ? "partialFailure" : "success";
        _metrics.RecordListFetch(context.Locations.Count, status, stopwatch.Elapsed);

        return ApplicationResult<SolicitorSearchData>.Ok(
            new SolicitorSearchData
            {
                Solicitors = DeduplicateSolicitors(searchData.Solicitors),
                LocationResults = searchData.LocationResults,
                Failures = searchData.Failures
            });
    }

    private static IReadOnlyList<Solicitor> DeduplicateSolicitors(IReadOnlyList<Solicitor> solicitors)
    {
        return solicitors
            .GroupBy(GetDeduplicationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static string GetDeduplicationKey(Solicitor solicitor)
    {
        if (!string.IsNullOrWhiteSpace(solicitor.ProfileSlug))
        {
            return solicitor.ProfileSlug;
        }

        if (!string.IsNullOrWhiteSpace(solicitor.ProfileUrl))
        {
            return solicitor.ProfileUrl;
        }

        return $"{solicitor.Name}|{solicitor.ContactDetails.WebsiteUrl}|{solicitor.ContactDetails.Phone}";
    }
}
