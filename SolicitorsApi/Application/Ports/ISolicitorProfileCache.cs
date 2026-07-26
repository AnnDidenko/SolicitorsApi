using SolicitorsApi.Application.Cache;

namespace SolicitorsApi.Application.Ports;

public interface ISolicitorProfileCache
{
    Task<IReadOnlyDictionary<string, SolicitorProfileCacheRecord>> GetBySourceIdentitiesAsync(
        IReadOnlyCollection<string> sourceIdentities,
        CancellationToken cancellationToken);

    Task UpsertDiscoveredSolicitorsAsync(
        IReadOnlyList<SolicitorProfileCacheRecord> records,
        CancellationToken cancellationToken);

    Task UpsertProfileDetailsAsync(
        SolicitorProfileCacheRecord record,
        CancellationToken cancellationToken);
}
