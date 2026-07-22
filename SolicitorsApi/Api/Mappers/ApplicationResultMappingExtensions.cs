using Microsoft.AspNetCore.Mvc;
using SolicitorsApi.Application;

namespace SolicitorsApi.Api.Mappers;

internal static class ApplicationResultMappingExtensions
{
    public static ActionResult<TContract> ToActionResult<TValue, TContract>(
        this ApplicationResult<TValue> result,
        Func<TValue, TContract> onSuccess)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return onSuccess(result.Value);
        }

        if (result.StatusCode == StatusCodes.Status400BadRequest)
        {
            var errors = result.Errors
                .GroupBy(error => error.Field ?? error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Message).ToArray());

            return new BadRequestObjectResult(new ValidationProblemDetails(errors));
        }

        var firstError = result.Errors.FirstOrDefault();

        return new ObjectResult(new ProblemDetails
        {
            Status = result.StatusCode,
            Title = firstError?.Code ?? "requestFailed",
            Detail = firstError?.Message
        })
        {
            StatusCode = result.StatusCode
        };
    }
}
