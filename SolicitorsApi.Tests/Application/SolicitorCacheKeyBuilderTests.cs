using SolicitorsApi.Application.Cache;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Tests.Application;

[TestFixture]
public class SolicitorCacheKeyBuilderTests
{
    [Test]
    public void ListCacheKey_NormalizesLocationAndAreaSlug()
    {
        var builder = new SolicitorListCacheKeyBuilder();

        var key = builder.Build(
            " London ",
            new AreaOfLaw { Name = "Conveyancing", Slug = " Conveyancing " });

        Assert.That(key, Is.EqualTo("london|conveyancing"));
    }

    [Test]
    public void ListCacheKey_DeduplicatesEquivalentLocations()
    {
        var builder = new SolicitorListCacheKeyBuilder();

        var keys = builder.BuildMany(
            [" London ", "london", "Birmingham"],
            new AreaOfLaw { Name = "Conveyancing", Slug = "conveyancing" });

        Assert.That(keys, Is.EqualTo(new[] { "london|conveyancing", "birmingham|conveyancing" }));
    }

    [Test]
    public void ListCacheKey_ExcludesResponseShapingInputs()
    {
        var builder = new SolicitorListCacheKeyBuilder();

        var key = builder.Build(
            "London",
            new AreaOfLaw { Name = "Conveyancing", Slug = "conveyancing" });

        Assert.That(key, Does.Not.Contain("page"));
        Assert.That(key, Does.Not.Contain("sort"));
        Assert.That(key, Does.Not.Contain("review"));
    }

    [Test]
    public void SolicitorIdentity_UsesProfileSlugBeforeOtherFields()
    {
        var builder = new SolicitorIdentityKeyBuilder();
        var solicitor = new Solicitor
        {
            Name = "Different Name",
            ProfileSlug = " A-Firm ",
            ProfileUrl = "https://example.test/other",
            ContactDetails = new SolicitorContactDetails
            {
                WebsiteUrl = "https://website.test",
                Phone = "020 0000"
            },
            Location = "London"
        };

        var key = builder.Build(solicitor);

        Assert.That(key, Is.EqualTo("slug:a-firm"));
    }

    [Test]
    public void SolicitorIdentity_UsesProfileUrlWhenSlugIsMissing()
    {
        var builder = new SolicitorIdentityKeyBuilder();
        var solicitor = new Solicitor
        {
            Name = "A Firm",
            ProfileUrl = " HTTPS://Example.Test/Profile?ID=123 ",
            ContactDetails = new SolicitorContactDetails
            {
                WebsiteUrl = "https://website.test",
                Phone = "020 0000"
            },
            Location = "London"
        };

        var key = builder.Build(solicitor);

        Assert.That(key, Is.EqualTo("url:/profile?id=123"));
    }

    [Test]
    public void SolicitorIdentity_ProfileUrlIgnoresArbitraryHost()
    {
        var builder = new SolicitorIdentityKeyBuilder();
        var first = new Solicitor
        {
            Name = "A Firm",
            ProfileUrl = "https://www.solicitors.com/profile/a-firm.html"
        };
        var second = first with
        {
            ProfileUrl = "https://unexpected.example/profile/a-firm.html"
        };

        Assert.That(builder.Build(first), Is.EqualTo(builder.Build(second)));
        Assert.That(builder.Build(first), Is.EqualTo("url:/profile/a-firm.html"));
    }

    [Test]
    public void SolicitorIdentity_UsesDeterministicFallbackWhenSlugAndUrlAreMissing()
    {
        var builder = new SolicitorIdentityKeyBuilder();
        var first = new Solicitor
        {
            Name = " A   Firm ",
            ContactDetails = new SolicitorContactDetails
            {
                WebsiteUrl = " HTTPS://Website.Test ",
                Phone = " 020   0000 "
            },
            Location = " London "
        };
        var second = first with
        {
            Name = "a firm",
            ContactDetails = new SolicitorContactDetails
            {
                WebsiteUrl = "https://website.test",
                Phone = "020 0000"
            },
            Location = "london"
        };

        Assert.That(builder.Build(first), Is.EqualTo(builder.Build(second)));
        Assert.That(builder.Build(first), Is.EqualTo("fallback|a firm|https://website.test|020 0000|london"));
    }
}
