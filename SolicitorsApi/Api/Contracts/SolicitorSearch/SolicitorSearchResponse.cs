namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorSearchResponse
{
    public DateTimeOffset SearchedAt { get; init; }

    public IReadOnlyList<string> Locations { get; init; } = [];

    public string? AreaOfLaw { get; init; }

    public SolicitorSearchFilters Filters { get; init; } = new();

    public SortOption Sort { get; init; } = new();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public IReadOnlyList<SolicitorSearchResultItem> Solicitors { get; init; } = [];

    public IReadOnlyList<LocationSearchResult> LocationResults { get; init; } = [];

    public SolicitorSearchReport Report { get; init; } = new();

    public IReadOnlyList<ScrapeFailure> Failures { get; init; } = [];

    public SolicitorSearchCacheInfo? Cache { get; init; }
}
