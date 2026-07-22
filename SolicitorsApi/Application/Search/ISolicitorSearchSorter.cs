using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Search;

public interface ISolicitorSearchSorter
{
    IReadOnlyList<Solicitor> Apply(
        IReadOnlyList<Solicitor> solicitors,
        SolicitorSearchSort sort);

    bool RequiresProfileReviewData(SolicitorSearchSort sort);
}
