using SolicitorsApi.Api.Mappers;
using SolicitorsApi.Application.Commands;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Tests.Api;

[TestFixture]
public class SolicitorSearchResponseMappingTests
{
    [Test]
    public void ToResponse_MapsCacheMetadata()
    {
        var fetchedAt = DateTimeOffset.Parse("2026-07-21T10:00:00Z");
        var servedAt = DateTimeOffset.Parse("2026-07-21T10:05:00Z");
        var expiresAt = DateTimeOffset.Parse("2026-07-22T10:00:00Z");
        var result = new SolicitorSearchResult
        {
            SearchedAt = servedAt,
            Sort = new SolicitorSearchSort(),
            Paging = new PagedSearchResult<Solicitor>(),
            Cache = new SolicitorSearchCacheMetadata
            {
                Status = SolicitorSearchCacheStatus.Fallback,
                UsedFallback = true,
                FetchedAt = fetchedAt,
                ServedAt = servedAt,
                ExpiresAt = expiresAt
            }
        };

        var response = result.ToResponse();

        Assert.Multiple(() =>
        {
            Assert.That(response.Cache, Is.Not.Null);
            Assert.That(response.Cache!.Status, Is.EqualTo("Fallback"));
            Assert.That(response.Cache.UsedFallback, Is.True);
            Assert.That(response.Cache.FetchedAt, Is.EqualTo(fetchedAt));
            Assert.That(response.Cache.ServedAt, Is.EqualTo(servedAt));
            Assert.That(response.Cache.ExpiresAt, Is.EqualTo(expiresAt));
        });
    }
}
