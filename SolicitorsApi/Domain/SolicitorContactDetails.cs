namespace SolicitorsApi.Domain;

public sealed record SolicitorContactDetails
{
    public string? Phone { get; init; }

    public string? EmailUrl { get; init; }

    public string? WebsiteUrl { get; init; }

    public string? Address { get; init; }
}
