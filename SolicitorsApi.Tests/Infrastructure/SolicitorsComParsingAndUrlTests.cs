using Microsoft.Extensions.Options;
using SolicitorsApi.Domain;
using SolicitorsApi.Infrastructure.SolicitorsCom.Configuration;
using SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;
using SolicitorsApi.Infrastructure.SolicitorsCom.Routing;

namespace SolicitorsApi.Tests.Infrastructure;

[TestFixture]
public class SolicitorsComParsingAndUrlTests
{
    [Test]
    public void UrlBuilder_BuildsCityAndAreaOfLawUrlsAndRestrictsProfileHosts()
    {
        var builder = CreateUrlBuilder();

        var homeUrl = builder.BuildHomeUri();
        var prepareSearchUrl = builder.BuildPrepareSearchUri();
        var cityUrl = builder.BuildCitySearchUri("Newcastle upon Tyne");
        var areaUrl = builder.BuildAreaOfLawSearchUri(
            new AreaOfLaw { Name = "Agricultural Law", Slug = "agricultural-law" },
            "London");

        Assert.Multiple(() =>
        {
            Assert.That(homeUrl.ToString(), Is.EqualTo("https://www.solicitors.com/"));
            Assert.That(prepareSearchUrl.ToString(), Is.EqualTo("https://www.solicitors.com/prepare-search.asp"));
            Assert.That(cityUrl.ToString(), Is.EqualTo("https://www.solicitors.com/newcastle-upon-tyne-solicitors.html"));
            Assert.That(areaUrl.ToString(), Is.EqualTo("https://www.solicitors.com/agricultural-law+london.html"));
            Assert.Throws<InvalidOperationException>(() => builder.BuildProfileUri("https://evil.test/profile.html"));
        });
    }

    [Test]
    public void ResultsParser_ParsesListSnippetAndAreaOfLawOptions()
    {
        var html = LoadFixture("list-results.html");
        var parser = new SolicitorsComResultsParser();

        var result = parser.Parse(html, new Uri("https://www.solicitors.com/london-solicitors.html"), "London");

        Assert.Multiple(() =>
        {
            Assert.That(result.AreaOfLawOptions.Select(option => option.Name), Is.EqualTo(new[] { "Conveyancing", "Family" }));
            Assert.That(result.AreaOfLawOptions.First().SiteId, Is.EqualTo("192"));
            Assert.That(result.AreaOfLawOptions.First().Slug, Is.EqualTo("conveyancing"));
            Assert.That(result.Solicitors, Has.Count.EqualTo(1));
            Assert.That(result.Solicitors[0].Name, Is.EqualTo("Smith & Co"));
            Assert.That(result.Solicitors[0].ProfileSlug, Is.EqualTo("smith-law"));
            Assert.That(result.Solicitors[0].ContactDetails.Phone, Is.EqualTo("020123456"));
            Assert.That(result.Solicitors[0].ContactDetails.Address, Is.EqualTo("10 High Street & Yard London"));
            Assert.That(result.Solicitors[0].Review!.Score, Is.EqualTo(2.5m));
            Assert.That(result.Solicitors[0].Review!.Count, Is.EqualTo(12));
        });
    }

    [Test]
    public void ProfileParser_ParsesProfileSnippet()
    {
        var html = LoadFixture("profile.html");
        var parser = new SolicitorsComProfileParser();

        var profile = parser.Parse(html, new Uri("https://www.solicitors.com/smith-law.html"));

        Assert.Multiple(() =>
        {
            Assert.That(profile.Name, Is.EqualTo("Smith Law"));
            Assert.That(profile.Slug, Is.EqualTo("smith-law"));
            Assert.That(profile.ContactDetails.Phone, Is.EqualTo("020123456"));
            Assert.That(profile.ContactDetails.EmailUrl, Is.EqualTo("https://www.solicitors.com/enquiry-form.asp?id=1"));
            Assert.That(profile.ContactDetails.WebsiteUrl, Is.EqualTo("https://smith.example"));
            Assert.That(profile.ContactDetails.Address, Is.EqualTo("1 Sample Street London"));
            Assert.That(profile.AreasOfLaw.Select(area => area.Name), Is.EqualTo(new[] { "Conveyancing" }));
            Assert.That(profile.Review!.Score, Is.EqualTo(4.7m));
            Assert.That(profile.Review!.Count, Is.EqualTo(8));
            Assert.That(profile.Offices, Has.Count.EqualTo(1));
            Assert.That(profile.Offices[0].Review!.Score, Is.EqualTo(4.5m));
        });
    }

    private static SolicitorsComUrlBuilder CreateUrlBuilder()
    {
        return new SolicitorsComUrlBuilder(Options.Create(new SolicitorsComOptions()));
    }

    private static string LoadFixture(string fileName)
    {
        return File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fileName));
    }
}
