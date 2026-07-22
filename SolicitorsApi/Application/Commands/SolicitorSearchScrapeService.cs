using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchScrapeService : ISolicitorSearchScrapeService
{
    private readonly ISolicitorSearchGateway _solicitorSearchGateway;

    public SolicitorSearchScrapeService(ISolicitorSearchGateway solicitorSearchGateway)
    {
        _solicitorSearchGateway = solicitorSearchGateway;
    }

    public async Task<ApplicationResult<SolicitorSearchData>> SearchAsync(
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        var searchData = await _solicitorSearchGateway.SearchAsync(
            context.Locations,
            context.AreaOfLaw,
            cancellationToken);

        if (searchData.Failures.Count > 0)
        {
            return ApplicationResult<SolicitorSearchData>.FailedDependency(
                searchData.Failures.Select(failure => new ApplicationError(
                    failure.Code,
                    failure.Message,
                    failure.Location)).ToArray());
        }

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
