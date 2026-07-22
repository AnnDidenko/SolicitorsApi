using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Queries;

public class ConveyancingSearchDefaults
{
    public IReadOnlyList<string> DefaultLocations { get; init; } = [];

    public IReadOnlyList<AreaOfLaw> AreaOfLawOptions { get; init; } = [];

    public IReadOnlyList<string> SortFields { get; init; } = [];

    public IReadOnlyList<string> SortDirections { get; init; } = [];

    public int DefaultPageSize { get; init; }
}
