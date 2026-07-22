namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorSearchResultItem
{
    public string Name { get; init; } = "";

    public string? Location { get; init; }

    public string? City { get; init; }

    public string? ProfileSlug { get; init; }

    public string? ProfileUrl { get; init; }

    public SolicitorContactDetails ContactDetails { get; init; } = new();

    public ReviewSummary? Review { get; init; }
}
