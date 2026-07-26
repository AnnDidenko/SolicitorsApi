using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Cache;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;
using SolicitorsApi.Infrastructure.Cache;
using SolicitorsApi.Infrastructure.SolicitorsCom;

namespace SolicitorsApi.Tests.Infrastructure;

[TestFixture]
public class MemorySolicitorSearchCacheTests
{
    [Test]
    public async Task SearchCache_ReturnsStoredUnexpiredListSegment()
    {
        var cache = new MemorySolicitorSearchCache(Options.Create(new SolicitorSearchCacheOptions()));
        var fetchedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        await cache.StoreListSegmentAsync(
            new SolicitorListCacheEntry
            {
                Location = "London",
                AreaOfLawSlug = "conveyancing",
                Solicitors = [new Solicitor { Name = "A Firm", Location = "London" }],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }],
                FetchedAt = fetchedAt
            },
            CancellationToken.None);

        var entry = await cache.GetListSegmentAsync(" london ", " Conveyancing ", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Solicitors.Single().Name, Is.EqualTo("A Firm"));
            Assert.That(entry.ExpiresAt, Is.EqualTo(fetchedAt.AddHours(24)));
        });
    }

    [Test]
    public async Task SearchCache_DoesNotReturnExpiredListSegment()
    {
        var cache = new MemorySolicitorSearchCache(Options.Create(new SolicitorSearchCacheOptions
        {
            ListTimeToLiveHours = -1
        }));

        await cache.StoreListSegmentAsync(
            new SolicitorListCacheEntry { Location = "London", FetchedAt = DateTimeOffset.UtcNow },
            CancellationToken.None);

        var entry = await cache.GetListSegmentAsync("London", null, CancellationToken.None);

        Assert.That(entry, Is.Null);
    }

    [Test]
    public async Task SearchCache_DoesNotShareEntriesAcrossLocationOrAreaSegments()
    {
        var cache = new MemorySolicitorSearchCache(Options.Create(new SolicitorSearchCacheOptions()));

        await cache.StoreListSegmentAsync(
            new SolicitorListCacheEntry
            {
                Location = "London",
                AreaOfLawSlug = "conveyancing",
                Solicitors = [new Solicitor { Name = "London Conveyancing Firm", Location = "London" }]
            },
            CancellationToken.None);

        var sameSegment = await cache.GetListSegmentAsync("London", "conveyancing", CancellationToken.None);
        var differentLocation = await cache.GetListSegmentAsync("Birmingham", "conveyancing", CancellationToken.None);
        var differentArea = await cache.GetListSegmentAsync("London", "family", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(sameSegment, Is.Not.Null);
            Assert.That(differentLocation, Is.Null);
            Assert.That(differentArea, Is.Null);
        });
    }

    [Test]
    public async Task ProfileCache_ReturnsUnexpiredRecordsAndPreservesProfileWhenDiscoveryUpdatesSolicitor()
    {
        var cache = new MemorySolicitorProfileCache(Options.Create(new SolicitorSearchCacheOptions()));
        var fetchedAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        await cache.UpsertProfileDetailsAsync(
            new SolicitorProfileCacheRecord
            {
                SourceIdentity = "slug:a-firm",
                Solicitor = new Solicitor { Name = "A Firm", ProfileSlug = "a-firm" },
                Profile = new SolicitorProfile { Name = "A Firm", Slug = "a-firm" },
                ProfileFetchedAt = fetchedAt
            },
            CancellationToken.None);
        await cache.UpsertDiscoveredSolicitorsAsync(
            [
                new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:a-firm",
                    Solicitor = new Solicitor { Name = "A Firm Updated", ProfileSlug = "a-firm" },
                    LastSeenAt = DateTimeOffset.UtcNow
                }
            ],
            CancellationToken.None);

        var records = await cache.GetBySourceIdentitiesAsync(["slug:a-firm"], CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(records, Contains.Key("slug:a-firm"));
            Assert.That(records["slug:a-firm"].Solicitor.Name, Is.EqualTo("A Firm Updated"));
            Assert.That(records["slug:a-firm"].Profile!.Slug, Is.EqualTo("a-firm"));
        });
    }

    [Test]
    public async Task ProfileCache_PrunesOldestEntriesWhenMaximumCountIsExceeded()
    {
        var cache = new MemorySolicitorProfileCache(Options.Create(new SolicitorSearchCacheOptions
        {
            MaxEntries = 1
        }));

        await cache.UpsertDiscoveredSolicitorsAsync(
            [
                new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:old",
                    Solicitor = new Solicitor { Name = "Old" },
                    LastSeenAt = DateTimeOffset.UtcNow.AddMinutes(-10)
                },
                new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:new",
                    Solicitor = new Solicitor { Name = "New" },
                    LastSeenAt = DateTimeOffset.UtcNow
                }
            ],
            CancellationToken.None);

        var records = await cache.GetBySourceIdentitiesAsync(["slug:old", "slug:new"], CancellationToken.None);

        Assert.That(records.Keys, Is.EqualTo(new[] { "slug:new" }));
    }

    [Test]
    public async Task ProfileCache_DoesNotReturnExpiredRecords()
    {
        var cache = new MemorySolicitorProfileCache(Options.Create(new SolicitorSearchCacheOptions
        {
            ProfileTimeToLiveHours = -1
        }));

        await cache.UpsertProfileDetailsAsync(
            new SolicitorProfileCacheRecord
            {
                SourceIdentity = "slug:expired",
                Solicitor = new Solicitor { Name = "Expired", ProfileSlug = "expired" },
                Profile = new SolicitorProfile { Name = "Expired", Slug = "expired" },
                ProfileFetchedAt = DateTimeOffset.UtcNow
            },
            CancellationToken.None);

        var records = await cache.GetBySourceIdentitiesAsync(["slug:expired"], CancellationToken.None);

        Assert.That(records, Is.Empty);
    }

    [Test]
    public void InfrastructureRegistration_ResolvesCachePorts()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SolicitorsCom:BaseUrl"] = "https://www.solicitors.com",
                ["SolicitorsCom:TimeoutSeconds"] = "30"
            })
            .Build();

        services.AddLogging();
        services.AddSolicitorsComInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<ISolicitorSearchCache>(), Is.TypeOf<MemorySolicitorSearchCache>());
            Assert.That(provider.GetRequiredService<ISolicitorProfileCache>(), Is.TypeOf<MemorySolicitorProfileCache>());
        });
    }
}
