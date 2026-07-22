namespace SolicitorsApi.Domain;

public sealed record AreaOfLaw
{
    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string? SiteId { get; init; }
}
