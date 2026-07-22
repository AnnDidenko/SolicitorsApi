namespace SolicitorsApi.Domain;

public sealed record Solicitor
{
    public string Name { get; init; } = string.Empty;

    public string? Location { get; init; }

    public string? City { get; init; }

    public string? ProfileSlug { get; init; }

    public string? ProfileUrl { get; init; }

    public SolicitorContactDetails ContactDetails { get; init; } = new();

    public IReadOnlyList<AreaOfLaw> AreasOfLaw { get; init; } = [];

    public ReviewSummary? Review { get; init; }
}
