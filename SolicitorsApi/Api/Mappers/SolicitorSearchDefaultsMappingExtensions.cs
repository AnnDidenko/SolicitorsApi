using SolicitorsApi.Api.Contracts;
using SolicitorsApi.Application.Queries;
using Domain = SolicitorsApi.Domain;

namespace SolicitorsApi.Api.Mappers;

internal static class SolicitorSearchDefaultsMappingExtensions
{
    public static SolicitorSearchDefaultsResponse ToResponse(this ConveyancingSearchDefaults defaults)
    {
        return new SolicitorSearchDefaultsResponse
        {
            DefaultLocations = defaults.DefaultLocations,
            AreaOfLawOptions = defaults.AreaOfLawOptions.Select(ToResponse).ToArray(),
            SortFields = defaults.SortFields,
            SortDirections = defaults.SortDirections,
            DefaultPageSize = defaults.DefaultPageSize
        };
    }

    private static AreaOfLawOption ToResponse(this Domain.AreaOfLaw areaOfLaw)
    {
        return new AreaOfLawOption
        {
            Name = areaOfLaw.Name,
            Slug = areaOfLaw.Slug,
            SiteId = areaOfLaw.SiteId ?? string.Empty
        };
    }
}
