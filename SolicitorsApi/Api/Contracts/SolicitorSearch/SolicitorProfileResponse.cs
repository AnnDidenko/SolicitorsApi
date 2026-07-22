namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorProfileResponse
{
    public string Name { get; init; } = "";

    public string Slug { get; init; } = "";

    public string? ProfileUrl { get; init; }

    public SolicitorContactDetails ContactDetails { get; init; } = new();

    public IReadOnlyList<SolicitorOffice> Offices { get; init; } = [];

    public IReadOnlyList<string> AreasOfLaw { get; init; } = [];

    public ReviewSummary? Review { get; init; }
}
