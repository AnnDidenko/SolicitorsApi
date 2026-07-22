using Microsoft.AspNetCore.Mvc;

namespace SolicitorsApi.Api.Middleware;

public class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = IsSolicitorsComRequestFailure(context, exception)
            ? CreateSolicitorsComProblemDetails(exception)
            : CreateUnexpectedProblemDetails();

        LogException(exception, problemDetails);

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static bool IsSolicitorsComRequestFailure(HttpContext context, Exception exception)
    {
        return exception is HttpRequestException ||
            exception is TaskCanceledException && !context.RequestAborted.IsCancellationRequested;
    }

    private static ProblemDetails CreateSolicitorsComProblemDetails(Exception exception)
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status424FailedDependency,
            Title = "solicitorsComRequestFailed",
            Detail = exception is TaskCanceledException
                ? "Solicitors.com did not respond in time. Please try again later."
                : "Solicitors.com could not be reached. Please try again later."
        };
    }

    private static ProblemDetails CreateUnexpectedProblemDetails()
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "unexpectedError",
            Detail = "Something went wrong while processing the request. Please try again later."
        };
    }

    private void LogException(Exception exception, ProblemDetails problemDetails)
    {
        if (problemDetails.Status == StatusCodes.Status424FailedDependency)
        {
            _logger.LogWarning(exception, "Solicitors.com request failed.");

            return;
        }

        _logger.LogError(exception, "Unhandled API request failed.");
    }
}
