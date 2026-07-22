namespace SolicitorsApi.Application;

public class SolicitorSearchSettings
{
    public const string SectionName = "SolicitorSearch";

    public IReadOnlyList<string> DefaultLocations { get; init; } = [];

    public int DefaultPageSize { get; init; }

    public int MaxLocations { get; init; }

    public int ProfileFetchConcurrency { get; init; }
}
