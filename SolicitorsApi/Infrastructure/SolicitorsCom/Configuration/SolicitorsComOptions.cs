namespace SolicitorsApi.Infrastructure.SolicitorsCom.Configuration;

public class SolicitorsComOptions
{
    public const string SectionName = "SolicitorsCom";

    public string BaseUrl { get; init; } = "https://www.solicitors.com";

    public string ConveyancingPath { get; init; } = "/conveyancing.html";

    public string AutocompletePath { get; init; } = "/scripts/locations.asp";

    public string PrepareSearchPath { get; init; } = "/prepare-search.asp";

    public int TimeoutSeconds { get; init; } = 30;
}
