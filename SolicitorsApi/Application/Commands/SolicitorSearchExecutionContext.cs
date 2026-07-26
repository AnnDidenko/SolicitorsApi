using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchExecutionContext
{
    public IReadOnlyList<string> Locations { get; set; } = [];

    public bool UsedDefaultLocations { get; init; }

    public AreaOfLaw? AreaOfLaw { get; init; }

    public SolicitorSearchSort Sort { get; init; } = new();

    public IReadOnlyList<ScrapeFailure> NonBlockingFailures { get; set; } = [];
}
