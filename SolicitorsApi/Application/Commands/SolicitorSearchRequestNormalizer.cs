using Microsoft.Extensions.Options;
using SolicitorsApi.Domain;
using SolicitorsApi.Application.Queries;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchRequestNormalizer : ISolicitorSearchRequestNormalizer
{
    private readonly SolicitorSearchSettings _settings;
    private readonly IAreaOfLawOptionsProvider _areaOfLawOptionsProvider;

    public SolicitorSearchRequestNormalizer(
        IOptions<SolicitorSearchSettings> options,
        IAreaOfLawOptionsProvider areaOfLawOptionsProvider)
    {
        _settings = options.Value;
        _areaOfLawOptionsProvider = areaOfLawOptionsProvider;
    }

    public async Task<SolicitorSearchExecutionContext> NormalizeAsync(
        RunConveyancingSolicitorSearchCommand command,
        CancellationToken cancellationToken)
    {
        var areaOfLawOptions = await _areaOfLawOptionsProvider.GetAsync(cancellationToken);

        return new SolicitorSearchExecutionContext
        {
            Locations = NormalizeLocations(command.Locations),
            UsedDefaultLocations = IsMissingOrEmpty(command.Locations),
            AreaOfLaw = ResolveAreaOfLaw(command.AreaOfLaw, areaOfLawOptions),
            Sort = ResolveSort(command.Sort)
        };
    }

    private IReadOnlyList<string> NormalizeLocations(IReadOnlyList<string>? locations)
    {
        if (IsMissingOrEmpty(locations))
        {
            return _settings.DefaultLocations;
        }

        return locations!
            .Select(location => location.Trim())
            .Where(location => !string.IsNullOrWhiteSpace(location))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsMissingOrEmpty(IReadOnlyList<string>? locations)
    {
        return locations is null || locations.All(string.IsNullOrWhiteSpace);
    }

    private static AreaOfLaw? ResolveAreaOfLaw(
        string? areaOfLaw,
        IReadOnlyList<AreaOfLaw> areaOfLawOptions)
    {
        if (string.IsNullOrWhiteSpace(areaOfLaw))
        {
            return null;
        }

        var normalized = areaOfLaw.Trim();

        return areaOfLawOptions.FirstOrDefault(option =>
            string.Equals(option.Name, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(option.Slug, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static SolicitorSearchSort ResolveSort(SolicitorSearchSortRequest? sort)
    {
        if (sort is null)
        {
            return new SolicitorSearchSort();
        }

        var field = Enum.TryParse<SolicitorSearchSortField>(sort.Field, true, out var parsedField)
            ? parsedField
            : SolicitorSearchSortField.SolicitorName;

        var direction = Enum.TryParse<SortDirection>(sort.Direction, true, out var parsedDirection)
            ? parsedDirection
            : SortDirection.Ascending;

        return new SolicitorSearchSort
        {
            Field = field,
            Direction = direction
        };
    }
}
