using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public interface ISolicitorSearchProfileEnricher
{
    Task<ApplicationResult<IReadOnlyList<Solicitor>>> EnrichIfRequiredAsync(
        IReadOnlyList<Solicitor> solicitors,
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchSort sort,
        CancellationToken cancellationToken);
}
