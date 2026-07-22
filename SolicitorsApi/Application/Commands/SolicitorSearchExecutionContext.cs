using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchExecutionContext
{
    public IReadOnlyList<string> Locations { get; init; } = [];

    public bool UsedDefaultLocations { get; init; }

    public AreaOfLaw? AreaOfLaw { get; init; }

    public SolicitorSearchSort Sort { get; init; } = new();
}
