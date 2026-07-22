namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorSearchDefaultsResponse
{
    public IReadOnlyList<string> DefaultLocations { get; init; } = [];

    public IReadOnlyList<AreaOfLawOption> AreaOfLawOptions { get; init; } = [];

    public IReadOnlyList<string> SortFields { get; init; } = [];

    public IReadOnlyList<string> SortDirections { get; init; } = [];

    public int DefaultPageSize { get; init; }
}
