using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public interface ISolicitorSearchResultFactory
{
    SolicitorSearchResult Create(
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchExecutionContext context,
        IReadOnlyList<Solicitor> solicitors,
        SolicitorSearchData searchData);
}
