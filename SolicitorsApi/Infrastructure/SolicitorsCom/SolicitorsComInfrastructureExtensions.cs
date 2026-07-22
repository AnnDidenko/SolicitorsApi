using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Infrastructure.SolicitorsCom.Clients;
using SolicitorsApi.Infrastructure.SolicitorsCom.Configuration;
using SolicitorsApi.Infrastructure.SolicitorsCom.Gateways;
using SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;
using SolicitorsApi.Infrastructure.SolicitorsCom.Routing;

namespace SolicitorsApi.Infrastructure.SolicitorsCom;

public static class SolicitorsComInfrastructureExtensions
{
    public static IServiceCollection AddSolicitorsComInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SolicitorsComOptions>(
            configuration.GetSection(SolicitorsComOptions.SectionName));

        services.AddSingleton<SolicitorsComUrlBuilder>();
        services.AddHttpClient<SolicitorsComSearchPageClient>(ConfigureHttpClient);
        services.AddHttpClient<SolicitorsComProfilePageClient>(ConfigureHttpClient);
        services.AddHttpClient<SolicitorsComLocationSuggestionClient>(ConfigureHttpClient);

        services.AddScoped<ISolicitorResultsParser, SolicitorsComResultsParser>();
        services.AddScoped<ISolicitorProfileParser, SolicitorsComProfileParser>();
        services.AddScoped<ISolicitorSearchPageClient>(provider =>
            provider.GetRequiredService<SolicitorsComSearchPageClient>());
        services.AddScoped<ISolicitorProfilePageClient>(provider =>
            provider.GetRequiredService<SolicitorsComProfilePageClient>());
        services.AddScoped<ILocationSuggestionGateway>(provider =>
            provider.GetRequiredService<SolicitorsComLocationSuggestionClient>());
        services.AddScoped<IAreaOfLawOptionsGateway, SolicitorsComAreaOfLawOptionsGateway>();
        services.AddScoped<ISolicitorSearchGateway, SolicitorsComSolicitorSearchGateway>();
        services.AddScoped<ISolicitorProfileGateway, SolicitorsComSolicitorProfileGateway>();

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<SolicitorsComOptions>>().Value;

        client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd(
            "text/html,application/xhtml+xml,application/xml;q=0.9,application/json;q=0.8,*/*;q=0.7");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB,en;q=0.9");
    }
}
