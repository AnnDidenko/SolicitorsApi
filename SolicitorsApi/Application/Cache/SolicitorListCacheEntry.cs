using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Cache;

public sealed record SolicitorListCacheEntry
{
    public string Location { get; init; } = string.Empty;

    public string? AreaOfLawSlug { get; init; }

    public IReadOnlyList<Solicitor> Solicitors { get; init; } = [];

    public IReadOnlyList<LocationSearchResult> LocationResults { get; init; } = [];

    public DateTimeOffset FetchedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }
}
