namespace SolicitorsApi.Infrastructure.SolicitorsCom.Clients;

public interface ISolicitorProfilePageClient
{
    Task<SolicitorsComPageResponse> GetProfilePageAsync(
        string slugOrUrl,
        CancellationToken cancellationToken);
}
