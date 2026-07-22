namespace SolicitorsApi.Domain;

public sealed record SolicitorSearchSort
{
    public SolicitorSearchSortField Field { get; init; } = SolicitorSearchSortField.SolicitorName;

    public SortDirection Direction { get; init; } = SortDirection.Ascending;
}
