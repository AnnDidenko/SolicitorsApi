using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Search;

public class SolicitorSearchSorter : ISolicitorSearchSorter
{
    public IReadOnlyList<Solicitor> Apply(
        IReadOnlyList<Solicitor> solicitors,
        SolicitorSearchSort sort)
    {
        if (sort.Field is SolicitorSearchSortField.ReviewScore)
        {
            var reviewOrdered = sort.Direction is SortDirection.Descending
                ? solicitors
                    .OrderBy(solicitor => solicitor.Review?.Score is null)
                    .ThenByDescending(solicitor => solicitor.Review?.Score)
                    .ThenByDescending(solicitor => solicitor.Review?.Count ?? 0)
                    .ThenBy(solicitor => solicitor.Name)
                : solicitors
                    .OrderBy(solicitor => solicitor.Review?.Score is null)
                    .ThenBy(solicitor => solicitor.Review?.Score)
                    .ThenByDescending(solicitor => solicitor.Review?.Count ?? 0)
                    .ThenBy(solicitor => solicitor.Name);

            return reviewOrdered.ToArray();
        }

        if (sort.Field is SolicitorSearchSortField.ReviewCount)
        {
            return SortByNullableReviewValue(
                solicitors,
                sort.Direction,
                solicitor => solicitor.Review?.Count);
        }

        Func<Solicitor, object?> selector = sort.Field switch
        {
            SolicitorSearchSortField.City => solicitor => solicitor.City,
            SolicitorSearchSortField.Location => solicitor => solicitor.Location,
            _ => solicitor => solicitor.Name
        };

        var ordered = sort.Direction is SortDirection.Descending
            ? solicitors.OrderByDescending(selector).ThenBy(solicitor => solicitor.Name)
            : solicitors.OrderBy(selector).ThenBy(solicitor => solicitor.Name);

        return ordered.ToArray();
    }

    public bool RequiresProfileReviewData(SolicitorSearchSort sort)
    {
        return sort.Field is SolicitorSearchSortField.ReviewScore or SolicitorSearchSortField.ReviewCount;
    }

    private static IReadOnlyList<Solicitor> SortByNullableReviewValue<T>(
        IReadOnlyList<Solicitor> solicitors,
        SortDirection direction,
        Func<Solicitor, T?> selector)
        where T : struct, IComparable<T>
    {
        var ordered = direction is SortDirection.Descending
            ? solicitors
                .OrderBy(solicitor => selector(solicitor) is null)
                .ThenByDescending(solicitor => selector(solicitor))
                .ThenBy(solicitor => solicitor.Name)
            : solicitors
                .OrderBy(solicitor => selector(solicitor) is null)
                .ThenBy(solicitor => selector(solicitor))
                .ThenBy(solicitor => solicitor.Name);

        return ordered.ToArray();
    }
}
