namespace SolicitorsApi.Domain;

public sealed record PagedSearchRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}
