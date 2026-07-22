using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SolicitorsApi.Api.Middleware;

namespace SolicitorsApi.Tests.Api;

[TestFixture]
public class ApiExceptionHandlingMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_ReturnsFailedDependencyForSolicitorsComHttpFailures()
    {
        var response = await InvokeMiddlewareAsync(
            new HttpRequestException("Name resolution failed.", null, HttpStatusCode.BadGateway));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status424FailedDependency));
            Assert.That(response.Title, Is.EqualTo("solicitorsComRequestFailed"));
            Assert.That(response.Detail, Is.EqualTo("Solicitors.com could not be reached. Please try again later."));
        });
    }

    [Test]
    public async Task InvokeAsync_ReturnsReadableInternalServerErrorForUnexpectedFailures()
    {
        var response = await InvokeMiddlewareAsync(new InvalidOperationException("Sensitive internal detail."));

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(response.Title, Is.EqualTo("unexpectedError"));
            Assert.That(response.Detail, Is.EqualTo("Something went wrong while processing the request. Please try again later."));
        });
    }

    private static async Task<ProblemResponse> InvokeMiddlewareAsync(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ApiExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var document = await JsonDocument.ParseAsync(context.Response.Body);
        var root = document.RootElement;

        return new ProblemResponse(
            context.Response.StatusCode,
            root.GetProperty("title").GetString() ?? string.Empty,
            root.GetProperty("detail").GetString() ?? string.Empty);
    }

    private readonly record struct ProblemResponse(
        int StatusCode,
        string Title,
        string Detail);
}
