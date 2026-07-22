using SolicitorsApi.Domain;
using SolicitorsApi.Infrastructure.SolicitorsCom.Routing;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Clients;

public class SolicitorsComSearchPageClient : ISolicitorSearchPageClient
{
    private readonly HttpClient _httpClient;
    private readonly SolicitorsComUrlBuilder _urlBuilder;

    public SolicitorsComSearchPageClient(
        HttpClient httpClient,
        SolicitorsComUrlBuilder urlBuilder)
    {
        _httpClient = httpClient;
        _urlBuilder = urlBuilder;
    }

    public Task<SolicitorsComPageResponse> GetHomePageAsync(CancellationToken cancellationToken)
    {
        return GetPageAsync(_urlBuilder.BuildHomeUri(), cancellationToken);
    }

    public async Task<SolicitorsComPageResponse> GetSearchPageAsync(
        string location,
        AreaOfLaw? areaOfLaw,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(areaOfLaw?.SiteId))
        {
            return await PostSearchFormAsync(location, areaOfLaw.SiteId, cancellationToken);
        }

        var requestUri = areaOfLaw is null
            ? _urlBuilder.BuildCitySearchUri(location)
            : _urlBuilder.BuildAreaOfLawSearchUri(areaOfLaw, location);

        return await GetPageAsync(requestUri, cancellationToken);
    }

    private async Task<SolicitorsComPageResponse> PostSearchFormAsync(
        string location,
        string areaOfLawSiteId,
        CancellationToken cancellationToken)
    {
        var requestUri = _urlBuilder.BuildPrepareSearchUri();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["did"] = areaOfLawSiteId.Trim(),
            ["location"] = location.Trim()
        });
        using var response = await _httpClient.PostAsync(requestUri, content, cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        response.EnsureSuccessStatusCode();

        return new SolicitorsComPageResponse
        {
            RequestUri = response.RequestMessage?.RequestUri ?? requestUri,
            StatusCode = (int)response.StatusCode,
            Html = html
        };
    }

    private async Task<SolicitorsComPageResponse> GetPageAsync(
        Uri requestUri,
        CancellationToken cancellationToken)
    {
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
