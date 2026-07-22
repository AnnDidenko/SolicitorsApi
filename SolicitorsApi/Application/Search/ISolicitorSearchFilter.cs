using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Search;

public interface ISolicitorSearchFilter
{
    IReadOnlyList<Solicitor> Apply(
        IReadOnlyList<Solicitor> solicitors,
        decimal? minimumReviewScore);
}
