using SolicitorsApi.Application.Cache;

namespace SolicitorsApi.Application.Ports;

public interface ISolicitorSearchCache
{
    Task<SolicitorListCacheEntry?> GetListSegmentAsync(
        string location,
        string? areaOfLawSlug,
        CancellationToken cancellationToken);

    Task StoreListSegmentAsync(
        SolicitorListCacheEntry entry,
        CancellationToken cancellationToken);
}
