using System.ComponentModel.DataAnnotations;

namespace SolicitorsApi.Api.Contracts;

public abstract record PagedSortedRequest
{
    public SortOption? Sort { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than zero.")]
    public int Page { get; init; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "Page size must be greater than zero.")]
    public int PageSize { get; init; } = 10;
}
