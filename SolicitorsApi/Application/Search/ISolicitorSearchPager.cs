using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Search;

public interface ISolicitorSearchPager
{
    PagedSearchResult<Solicitor> Apply(
        IReadOnlyList<Solicitor> solicitors,
        int page,
        int pageSize);
}
