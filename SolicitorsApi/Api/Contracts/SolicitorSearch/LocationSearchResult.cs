namespace SolicitorsApi.Api.Contracts;

public sealed record LocationSearchResult
{
    public string Location { get; init; } = "";

    public int Count { get; init; }
}
