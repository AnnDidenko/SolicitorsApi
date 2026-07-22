using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Reports;

public interface ISolicitorSearchReportBuilder
{
    SolicitorSearchReport Build(
        IReadOnlyList<Solicitor> solicitors,
        IReadOnlyList<LocationSearchResult> locationResults);
}
