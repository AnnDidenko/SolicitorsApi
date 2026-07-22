namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorOffice
{
    public string? Name { get; init; }

    public string? Address { get; init; }

    public string? Phone { get; init; }

    public ReviewSummary? Review { get; init; }
}
