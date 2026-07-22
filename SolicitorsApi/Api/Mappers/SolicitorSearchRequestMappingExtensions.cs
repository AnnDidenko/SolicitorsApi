using SolicitorsApi.Api.Contracts;
using SolicitorsApi.Application.Commands;
using Domain = SolicitorsApi.Domain;

namespace SolicitorsApi.Api.Mappers;

internal static class SolicitorSearchRequestMappingExtensions
{
    public static RunConveyancingSolicitorSearchCommand ToCommand(this SolicitorSearchRequest request)
    {
        return new RunConveyancingSolicitorSearchCommand
        {
            Locations = request.Locations,
            AreaOfLaw = request.AreaOfLaw,
            MinimumReviewScore = request.MinimumReviewScore,
            Sort = request.Sort is null
                ? null
                : new SolicitorSearchSortRequest
                {
                    Field = request.Sort.Field,
                    Direction = request.Sort.Direction
                },
            Paging = new Domain.PagedSearchRequest
            {
                Page = request.Page,
                PageSize = request.PageSize
            }
        };
    }
}
