using SolicitorsApi.Application.Cache;
using SolicitorsApi.Application.Commands;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Tests.Application;

[TestFixture]
public class SolicitorCacheModelTests
{
    [Test]
    public void SearchCacheMetadata_DefaultsToFreshNonFallbackStatus()
    {
        var metadata = new SolicitorSearchCacheMetadata();

        Assert.Multiple(() =>
        {
            Assert.That(metadata.Status, Is.EqualTo(SolicitorSearchCacheStatus.Fresh));
            Assert.That(metadata.UsedFallback, Is.False);
        });
    }

    [Test]
    public void ListCacheEntry_StoresSegmentDataAndTimestamps()
    {
        var fetchedAt = DateTimeOffset.Parse("2026-07-21T10:00:00Z");
        var expiresAt = fetchedAt.AddHours(24);
        var entry = new SolicitorListCacheEntry
        {
            Location = "London",
            AreaOfLawSlug = "conveyancing",
            Solicitors = [new Solicitor { Name = "A Firm", Location = "London" }],
            LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }],
            FetchedAt = fetchedAt,
            ExpiresAt = expiresAt
        };

        Assert.Multiple(() =>
        {
            Assert.That(entry.Location, Is.EqualTo("London"));
            Assert.That(entry.AreaOfLawSlug, Is.EqualTo("conveyancing"));
            Assert.That(entry.Solicitors.Single().Name, Is.EqualTo("A Firm"));
            Assert.That(entry.LocationResults.Single().Count, Is.EqualTo(1));
            Assert.That(entry.FetchedAt, Is.EqualTo(fetchedAt));
            Assert.That(entry.ExpiresAt, Is.EqualTo(expiresAt));
        });
    }

    [Test]
    public void ProfileCacheRecord_StoresIdentitySolicitorProfileAndStalenessMetadata()
    {
        var lastSeenAt = DateTimeOffset.Parse("2026-07-21T10:00:00Z");
        var profileFetchedAt = lastSeenAt.AddMinutes(5);
        var expiresAt = lastSeenAt.AddHours(24);
        var record = new SolicitorProfileCacheRecord
        {
            SourceIdentity = "a-firm",
            Solicitor = new Solicitor { Name = "A Firm", ProfileSlug = "a-firm" },
            Profile = new SolicitorProfile { Name = "A Firm", Slug = "a-firm" },
            LastSeenAt = lastSeenAt,
            ProfileFetchedAt = profileFetchedAt,
            ExpiresAt = expiresAt
        };

        Assert.Multiple(() =>
        {
            Assert.That(record.SourceIdentity, Is.EqualTo("a-firm"));
            Assert.That(record.Solicitor.ProfileSlug, Is.EqualTo("a-firm"));
            Assert.That(record.Profile!.Slug, Is.EqualTo("a-firm"));
            Assert.That(record.LastSeenAt, Is.EqualTo(lastSeenAt));
            Assert.That(record.ProfileFetchedAt, Is.EqualTo(profileFetchedAt));
            Assert.That(record.ExpiresAt, Is.EqualTo(expiresAt));
        });
    }
}
