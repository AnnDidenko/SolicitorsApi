using SolicitorsApi.Application.Commands;
using SolicitorsApi.Application.Queries;
using SolicitorsApi.Application.Reports;
using SolicitorsApi.Application.Search;

namespace SolicitorsApi.Application;

public static class SolicitorSearchApplicationExtensions
{
    public static IServiceCollection AddSolicitorSearchApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SolicitorSearchSettings>(
            configuration.GetSection(SolicitorSearchSettings.SectionName));

        services.AddScoped<GetConveyancingSearchDefaultsHandler>();
        services.AddScoped<GetLocationSuggestionsHandler>();
        services.AddScoped<GetSolicitorProfileHandler>();
        services.AddScoped<RunConveyancingSolicitorSearchHandler>();
        services.AddScoped<IAreaOfLawOptionsProvider, AreaOfLawOptionsProvider>();
        services.AddScoped<IConveyancingSearchDefaultsService, ConveyancingSearchDefaultsService>();
        services.AddScoped<ILocationSuggestionService, LocationSuggestionService>();
        services.AddScoped<ISolicitorProfileService, SolicitorProfileService>();
        services.AddScoped<ISolicitorSearchRequestValidator, SolicitorSearchRequestValidator>();
        services.AddScoped<ISolicitorSearchRequestNormalizer, SolicitorSearchRequestNormalizer>();
        services.AddScoped<ISolicitorSearchScrapeService, SolicitorSearchScrapeService>();
        services.AddScoped<ISolicitorSearchProfileEnricher, SolicitorSearchProfileEnricher>();
        services.AddScoped<ISolicitorSearchResultFactory, SolicitorSearchResultFactory>();
        services.AddScoped<ISolicitorSearchFilter, SolicitorSearchFilter>();
        services.AddScoped<ISolicitorSearchPager, SolicitorSearchPager>();
        services.AddScoped<ISolicitorSearchSorter, SolicitorSearchSorter>();
        services.AddScoped<ISolicitorSearchReportBuilder, SolicitorSearchReportBuilder>();

        return services;
    }
}
