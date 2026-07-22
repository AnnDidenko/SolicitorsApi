namespace SolicitorsApi.Domain;

public sealed record PagedSearchResult<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = [];

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }
}
