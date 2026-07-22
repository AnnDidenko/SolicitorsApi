using SolicitorsApi.Domain;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Clients;

public interface ISolicitorSearchPageClient
{
    Task<SolicitorsComPageResponse> GetHomePageAsync(CancellationToken cancellationToken);

    Task<SolicitorsComPageResponse> GetSearchPageAsync(
        string location,
        AreaOfLaw? areaOfLaw,
        CancellationToken cancellationToken);
}
