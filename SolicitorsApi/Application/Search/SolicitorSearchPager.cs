using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Search;

public class SolicitorSearchPager : ISolicitorSearchPager
{
    public PagedSearchResult<Solicitor> Apply(
        IReadOnlyList<Solicitor> solicitors,
        int page,
        int pageSize)
    {
        var items = solicitors
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedSearchResult<Solicitor>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = solicitors.Count
        };
    }
}
