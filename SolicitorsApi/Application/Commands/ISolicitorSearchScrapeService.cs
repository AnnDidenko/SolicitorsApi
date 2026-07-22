using SolicitorsApi.Application.Ports;

namespace SolicitorsApi.Application.Commands;

public interface ISolicitorSearchScrapeService
{
    Task<ApplicationResult<SolicitorSearchData>> SearchAsync(
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken);
}
