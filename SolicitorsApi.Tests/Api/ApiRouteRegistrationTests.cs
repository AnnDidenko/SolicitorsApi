using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SolicitorsApi.Api.Controllers;
using SolicitorsApi.Application;

namespace SolicitorsApi.Tests.Endpoints;

[TestFixture]
public class ApiRouteRegistrationTests
{
    [Test]
    public void Controllers_RegisterExpectedRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SolicitorSearch:DefaultLocations:0"] = "London",
                ["SolicitorSearch:DefaultPageSize"] = "10",
                ["SolicitorSearch:MaxLocations"] = "10",
                ["SolicitorSearch:ProfileFetchConcurrency"] = "4"
            })
            .Build();

        builder.Services.AddSolicitorSearchApplication(configuration);
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(SolicitorSearchController).Assembly);
        var app = builder.Build();

        app.MapControllers();

        var routePatterns = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();

        Assert.That(
            routePatterns,
            Is.SupersetOf(new[]
            {
                "api/solicitors/conveyancing/defaults",
                "api/solicitors/conveyancing/search",
                "api/solicitors/locations/suggestions",
                "api/solicitors/{slug}"
            }));
    }
}
