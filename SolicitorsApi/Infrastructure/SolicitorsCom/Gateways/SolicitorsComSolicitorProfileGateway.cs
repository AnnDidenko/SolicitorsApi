using System.Net;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;
using SolicitorsApi.Infrastructure.SolicitorsCom.Clients;
using SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Gateways;

public class SolicitorsComSolicitorProfileGateway : ISolicitorProfileGateway
{
    private readonly ISolicitorProfilePageClient _profilePageClient;
    private readonly ISolicitorProfileParser _profileParser;

    public SolicitorsComSolicitorProfileGateway(
        ISolicitorProfilePageClient profilePageClient,
        ISolicitorProfileParser profileParser)
    {
        _profilePageClient = profilePageClient;
        _profileParser = profileParser;
    }

    public async Task<SolicitorProfile?> GetProfileAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        try
        {
            var page = await _profilePageClient.GetProfilePageAsync(slug, cancellationToken);

            return _profileParser.Parse(page.Html, page.RequestUri);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
