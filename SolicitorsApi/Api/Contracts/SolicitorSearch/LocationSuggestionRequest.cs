using System.ComponentModel.DataAnnotations;

namespace SolicitorsApi.Api.Contracts;

public sealed record LocationSuggestionRequest
{
    [Required]
    [MinLength(3, ErrorMessage = "Location suggestion query must contain at least three characters.")]
    public string Query { get; init; } = string.Empty;
}
