namespace SolicitorsApi.Application.Commands;

public interface ISolicitorSearchRequestValidator
{
    Task<IReadOnlyList<ApplicationError>> ValidateAsync(
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken);
}
