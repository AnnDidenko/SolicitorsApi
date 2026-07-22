using System.ComponentModel.DataAnnotations;

namespace SolicitorsApi.Api.Contracts;

public sealed record SortOption
{
    [RegularExpression(
        "^(|SolicitorName|City|Location|ReviewScore|ReviewCount)$",
        ErrorMessage = "The supplied sort field is not supported.")]
    public string Field { get; init; } = "SolicitorName";

    [RegularExpression(
        "^(|Ascending|Descending)$",
        ErrorMessage = "The supplied sort direction is not supported.")]
    public string Direction { get; init; } = "Ascending";
}
