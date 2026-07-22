using SolicitorsApi.Infrastructure.SolicitorsCom.Routing;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Clients;

public class SolicitorsComProfilePageClient : ISolicitorProfilePageClient
{
    private readonly HttpClient _httpClient;
    private readonly SolicitorsComUrlBuilder _urlBuilder;

    public SolicitorsComProfilePageClient(
        HttpClient httpClient,
        SolicitorsComUrlBuilder urlBuilder)
    {
        _httpClient = httpClient;
        _urlBuilder = urlBuilder;
    }

    public async Task<SolicitorsComPageResponse> GetProfilePageAsync(
        string slugOrUrl,
        CancellationToken cancellationToken)
    {
        var requestUri = _urlBuilder.BuildProfileUri(slugOrUrl);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();

        return new SolicitorsComPageResponse
        {
            RequestUri = requestUri,
            StatusCode = (int)response.StatusCode,
            Html = html
        };
    }
}
