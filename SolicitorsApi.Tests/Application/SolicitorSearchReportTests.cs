using SolicitorsApi.Application.Reports;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Tests.Application;

[TestFixture]
public class SolicitorSearchReportTests
{
    [Test]
    public void ReportBuilder_CalculatesCountsCompletenessAndReviewSummary()
    {
        var builder = new SolicitorSearchReportBuilder();
        var solicitors = new[]
        {
            Solicitor("A Firm", "London", "Conveyancing", 4.5m, hasEmail: true),
            Solicitor("B Firm", "London", "Family", 3.5m),
            Solicitor("C Firm", "Leeds", "Conveyancing", null)
        };
        var locations = new[]
        {
            new LocationSearchResult { Location = "London", Count = 2 },
            new LocationSearchResult { Location = "Bristol", Count = 0 }
        };

        var report = builder.Build(solicitors, locations);

        Assert.Multiple(() =>
        {
            Assert.That(report.TotalSolicitors, Is.EqualTo(3));
            Assert.That(report.CountsByLocation["London"], Is.EqualTo(2));
            Assert.That(report.CountsByAreaOfLaw["Conveyancing"], Is.EqualTo(2));
            Assert.That(report.LocationsWithNoResults, Is.EqualTo(new[] { "Bristol" }));
            Assert.That(report.ContactCompleteness["Phone"], Is.EqualTo(3));
            Assert.That(report.ContactCompleteness["EmailUrl"], Is.EqualTo(1));
            Assert.That(report.ReviewScoreSummary.Minimum, Is.EqualTo(3.5m));
            Assert.That(report.ReviewScoreSummary.Maximum, Is.EqualTo(4.5m));
            Assert.That(report.ReviewScoreSummary.Average, Is.EqualTo(4.0m));
        });
    }

    private static Solicitor Solicitor(
        string name,
        string location,
        string areaOfLaw,
        decimal? reviewScore,
        bool hasEmail = false)
    {
        return new Solicitor
        {
            Name = name,
            Location = location,
            City = location,
            AreasOfLaw = [new AreaOfLaw { Name = areaOfLaw, Slug = areaOfLaw.ToLowerInvariant() }],
            ContactDetails = new SolicitorContactDetails
            {
                Phone = "020 0000",
                EmailUrl = hasEmail ? "/enquiry-form.asp" : null,
                WebsiteUrl = "https://example.test",
                Address = $"{location} office"
            },
            Review = reviewScore.HasValue
                ? new ReviewSummary { Score = reviewScore }
                : null
        };
    }
}
