namespace SolicitorsApi.Api.Contracts;

public sealed record AreaOfLawOption
{
    public string Name { get; init; } = "";

    public string Slug { get; init; } = "";

    public string SiteId { get; init; } = "";
}
