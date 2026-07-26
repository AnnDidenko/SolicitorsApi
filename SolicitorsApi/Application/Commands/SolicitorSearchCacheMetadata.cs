namespace SolicitorsApi.Application.Commands;

public sealed record SolicitorSearchCacheMetadata
{
    public SolicitorSearchCacheStatus Status { get; init; } = SolicitorSearchCacheStatus.Fresh;

    public bool UsedFallback { get; init; }

    public DateTimeOffset? FetchedAt { get; init; }

    public DateTimeOffset ServedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
