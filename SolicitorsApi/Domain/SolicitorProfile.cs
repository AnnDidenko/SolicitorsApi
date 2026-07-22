namespace SolicitorsApi.Domain;

public sealed record SolicitorProfile
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? ProfileUrl { get; init; }

    public SolicitorContactDetails ContactDetails { get; init; } = new();

    public IReadOnlyList<SolicitorOffice> Offices { get; init; } = [];

    public IReadOnlyList<AreaOfLaw> AreasOfLaw { get; init; } = [];

    public ReviewSummary? Review { get; init; }
}
