using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Reports;

public class SolicitorSearchReportBuilder : ISolicitorSearchReportBuilder
{
    public SolicitorSearchReport Build(
        IReadOnlyList<Solicitor> solicitors,
        IReadOnlyList<LocationSearchResult> locationResults)
    {
        var reviewScores = GetReviewScores(solicitors);

        return new SolicitorSearchReport
        {
            TotalSolicitors = solicitors.Count,
            CountsByLocation = CountByLocation(solicitors),
            CountsByAreaOfLaw = CountByAreaOfLaw(solicitors),
            LocationsWithNoResults = GetLocationsWithNoResults(locationResults),
            ContactCompleteness = CalculateContactCompleteness(solicitors),
            ReviewScoreSummary = CalculateReviewScoreSummary(reviewScores)
        };
    }

    private static IReadOnlyDictionary<string, int> CountByLocation(IReadOnlyList<Solicitor> solicitors)
    {
        return solicitors
            .Where(solicitor => !string.IsNullOrWhiteSpace(solicitor.Location))
            .GroupBy(solicitor => solicitor.Location!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, int> CountByAreaOfLaw(IReadOnlyList<Solicitor> solicitors)
    {
        return solicitors
            .SelectMany(solicitor => solicitor.AreasOfLaw)
            .Where(areaOfLaw => !string.IsNullOrWhiteSpace(areaOfLaw.Name))
            .GroupBy(areaOfLaw => areaOfLaw.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetLocationsWithNoResults(
        IReadOnlyList<LocationSearchResult> locationResults)
    {
        return locationResults
            .Where(result => result.Count == 0)
            .Select(result => result.Location)
            .Where(location => !string.IsNullOrWhiteSpace(location))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, int> CalculateContactCompleteness(
        IReadOnlyList<Solicitor> solicitors)
    {
        return new Dictionary<string, int>
        {
            ["Phone"] = solicitors.Count(solicitor => !string.IsNullOrWhiteSpace(solicitor.ContactDetails.Phone)),
            ["EmailUrl"] = solicitors.Count(solicitor => !string.IsNullOrWhiteSpace(solicitor.ContactDetails.EmailUrl)),
            ["WebsiteUrl"] = solicitors.Count(solicitor => !string.IsNullOrWhiteSpace(solicitor.ContactDetails.WebsiteUrl)),
            ["Address"] = solicitors.Count(solicitor => !string.IsNullOrWhiteSpace(solicitor.ContactDetails.Address))
        };
    }

    private static IReadOnlyList<decimal> GetReviewScores(IReadOnlyList<Solicitor> solicitors)
    {
        return solicitors
            .Select(solicitor => solicitor.Review?.Score)
            .OfType<decimal>()
            .ToArray();
    }

    private static ReviewScoreSummary CalculateReviewScoreSummary(IReadOnlyList<decimal> reviewScores)
    {
        return new ReviewScoreSummary
        {
            Minimum = reviewScores.Count == 0 ? null : reviewScores.Min(),
            Maximum = reviewScores.Count == 0 ? null : reviewScores.Max(),
            Average = reviewScores.Count == 0 ? null : reviewScores.Average()
        };
    }
}
