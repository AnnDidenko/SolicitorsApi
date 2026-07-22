using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SolicitorsApi.Application.Commands;
using SolicitorsApi.Application.Queries;
using SolicitorsApi.Api.Contracts;
using SolicitorsApi.Api.Mappers;

namespace SolicitorsApi.Api.Controllers;

[ApiController]
[Route("api/solicitors")]
[Produces("application/json")]
public class SolicitorSearchController : ControllerBase
{
    [HttpGet("conveyancing/defaults")]
    [ProducesResponseType<SolicitorSearchDefaultsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SolicitorSearchDefaultsResponse>> GetConveyancingDefaults(
        GetConveyancingSearchDefaultsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetConveyancingSearchDefaultsQuery(), cancellationToken);

        return result.ToActionResult(defaults => defaults.ToResponse());
    }

    [HttpPost("conveyancing/search")]
    [ProducesResponseType<SolicitorSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status424FailedDependency)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<SolicitorSearchResponse>> SearchConveyancingSolicitors(
        SolicitorSearchRequest request,
        RunConveyancingSolicitorSearchHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            request.ToCommand(),
            cancellationToken);

        return result.ToActionResult(searchResult => searchResult.ToResponse());
    }

    [HttpGet("locations/suggestions")]
    [ProducesResponseType<IReadOnlyList<LocationSuggestion>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<IReadOnlyList<LocationSuggestion>>> GetLocationSuggestions(
        [FromQuery] LocationSuggestionRequest request,
        GetLocationSuggestionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetLocationSuggestionsQuery { Query = request.Query },
            cancellationToken);

        return result.ToActionResult(
            suggestions => (IReadOnlyList<LocationSuggestion>)suggestions
                .Select(suggestion => suggestion.ToResponse())
                .ToArray());
    }

    [HttpGet("{slug}")]
    [ProducesResponseType<SolicitorProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<SolicitorProfileResponse>> GetSolicitorProfile(
        [Required]
        [RegularExpression(".*\\S.*", ErrorMessage = "Solicitor profile slug is required.")]
        string slug,
        GetSolicitorProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetSolicitorProfileQuery { Slug = slug },
            cancellationToken);

        return result.ToActionResult(profile => profile.ToResponse());
    }
}
