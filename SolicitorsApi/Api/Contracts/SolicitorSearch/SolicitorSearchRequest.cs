using System.ComponentModel.DataAnnotations;

namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorSearchRequest : PagedSortedRequest
{
    public IReadOnlyList<string>? Locations { get; init; }

    public string? AreaOfLaw { get; init; }

    [Range(0, 5, ErrorMessage = "Minimum review score must be between 0 and 5.")]
    public decimal? MinimumReviewScore { get; init; }
}
