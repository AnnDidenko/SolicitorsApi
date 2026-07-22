namespace SolicitorsApi.Application.Commands;

public interface ISolicitorSearchRequestNormalizer
{
    Task<SolicitorSearchExecutionContext> NormalizeAsync(
        RunConveyancingSolicitorSearchCommand command,
        CancellationToken cancellationToken);
}
