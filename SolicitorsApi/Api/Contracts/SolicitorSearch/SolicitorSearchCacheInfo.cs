namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorSearchCacheInfo
{
    public string Status { get; init; } = "Fresh";

    public bool UsedFallback { get; init; }

    public DateTimeOffset? FetchedAt { get; init; }

    public DateTimeOffset ServedAt { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}
