namespace SolicitorsApi.Infrastructure.SolicitorsCom.Clients;

public class SolicitorsComPageResponse
{
    public Uri RequestUri { get; init; } = new("https://www.solicitors.com", UriKind.Absolute);

    public int StatusCode { get; init; }

    public string Html { get; init; } = string.Empty;
}
