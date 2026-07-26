using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchRequestValidator : ISolicitorSearchRequestValidator
{
    private readonly SolicitorSearchSettings _settings;
    private readonly ILocationSuggestionGateway _locationSuggestionGateway;

    public SolicitorSearchRequestValidator(
        IOptions<SolicitorSearchSettings> options,
        ILocationSuggestionGateway locationSuggestionGateway)
    {
        _settings = options.Value;
        _locationSuggestionGateway = locationSuggestionGateway;
    }

    public async Task<IReadOnlyList<ApplicationError>> ValidateAsync(
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        var errors = new List<ApplicationError>();
        var locationErrors = await ValidateLocationsExistAsync(context, cancellationToken);

        if (context.Locations.Count > _settings.MaxLocations)
        {
            errors.Add(new ApplicationError(
                "maxLocationsExceeded",
                $"At most {_settings.MaxLocations} locations can be searched at once.",
                nameof(command.Locations)));
        }

        if (!string.IsNullOrWhiteSpace(command.AreaOfLaw) && context.AreaOfLaw is null)
        {
            errors.Add(new ApplicationError(
                "unsupportedAreaOfLaw",
                "The supplied area of law is not supported.",
                nameof(command.AreaOfLaw)));
        }

        errors.AddRange(locationErrors);

        return errors;
    }

    private async Task<IReadOnlyList<ApplicationError>> ValidateLocationsExistAsync(
        SolicitorSearchExecutionContext context,
        CancellationToken cancellationToken)
    {
        if (context.UsedDefaultLocations)
        {
            return [];
        }

        var errors = new List<ApplicationError>();
        var validLocations = new List<string>();
        var nonBlockingFailures = new List<ScrapeFailure>();

        foreach (var location in context.Locations)
        {
            if (location.Length < 3)
            {
                AddInvalidLocation(
                    errors,
                    nonBlockingFailures,
                    location,
                    "locationTooShort",
                    $"Location '{location}' must contain at least three characters.");
                continue;
            }

            var suggestions = await _locationSuggestionGateway.GetSuggestionsAsync(
                location[..3],
                cancellationToken);
            var exists = suggestions.Any(suggestion =>
                string.Equals(suggestion.Title, location, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                AddInvalidLocation(
                    errors,
                    nonBlockingFailures,
                    location,
                    "locationNotFound",
                    $"City '{location}' does not exist.");
                continue;
            }

            validLocations.Add(location);
        }

        if (validLocations.Count == 0)
        {
            return errors;
        }

        context.Locations = validLocations;
        context.NonBlockingFailures = nonBlockingFailures;

        return [];
    }

    private static void AddInvalidLocation(
        List<ApplicationError> errors,
        List<ScrapeFailure> nonBlockingFailures,
        string location,
        string code,
        string message)
    {
        errors.Add(new ApplicationError(
            code,
            message,
            nameof(RunConveyancingSolicitorSearchCommand.Locations)));
        nonBlockingFailures.Add(new ScrapeFailure
        {
            Location = location,
            Code = code,
            Message = message
        });
    }
}
