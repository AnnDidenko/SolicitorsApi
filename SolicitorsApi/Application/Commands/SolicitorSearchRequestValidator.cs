using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Ports;

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

        errors.AddRange(await ValidateLocationsExistAsync(context, cancellationToken));

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

        foreach (var location in context.Locations)
        {
            if (location.Length < 3)
            {
                errors.Add(new ApplicationError(
                    "locationTooShort",
                    $"Location '{location}' must contain at least three characters.",
                    nameof(RunConveyancingSolicitorSearchCommand.Locations)));
                continue;
            }

            var suggestions = await _locationSuggestionGateway.GetSuggestionsAsync(
                location[..3],
                cancellationToken);
            var exists = suggestions.Any(suggestion =>
                string.Equals(suggestion.Title, location, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                errors.Add(new ApplicationError(
                    "locationNotFound",
                    $"City '{location}' does not exist.",
                    nameof(RunConveyancingSolicitorSearchCommand.Locations)));
            }
        }

        return errors;
    }
}
