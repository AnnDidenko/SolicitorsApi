using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Cache;

public sealed record SolicitorProfileCacheRecord
{
    public string SourceIdentity { get; init; } = string.Empty;

    public Solicitor Solicitor { get; init; } = new();

    public SolicitorProfile? Profile { get; init; }

    public DateTimeOffset LastSeenAt { get; init; }

    public DateTimeOffset? ProfileFetchedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }
}
