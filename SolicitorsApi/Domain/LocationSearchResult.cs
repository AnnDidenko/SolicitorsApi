namespace SolicitorsApi.Domain;

public sealed record LocationSearchResult
{
    public string Location { get; init; } = string.Empty;

    public int Count { get; init; }

    public bool UsedDefaultLocation { get; init; }
}
