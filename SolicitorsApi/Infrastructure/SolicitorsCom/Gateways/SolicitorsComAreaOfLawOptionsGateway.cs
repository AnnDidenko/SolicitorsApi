using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;
using SolicitorsApi.Infrastructure.SolicitorsCom.Clients;
using SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Gateways;

public class SolicitorsComAreaOfLawOptionsGateway : IAreaOfLawOptionsGateway
{
    private readonly ISolicitorSearchPageClient _searchPageClient;
    private readonly ISolicitorResultsParser _resultsParser;

    public SolicitorsComAreaOfLawOptionsGateway(
        ISolicitorSearchPageClient searchPageClient,
        ISolicitorResultsParser resultsParser)
    {
        _searchPageClient = searchPageClient;
        _resultsParser = resultsParser;
    }

    public async Task<IReadOnlyList<AreaOfLaw>> GetAreaOfLawOptionsAsync(CancellationToken cancellationToken)
    {
        var page = await _searchPageClient.GetHomePageAsync(cancellationToken);

        return _resultsParser.ParseAreaOfLawOptions(page.Html);
    }
}
