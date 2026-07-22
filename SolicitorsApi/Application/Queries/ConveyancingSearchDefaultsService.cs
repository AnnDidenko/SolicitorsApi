using Microsoft.Extensions.Options;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Queries;

public class ConveyancingSearchDefaultsService : IConveyancingSearchDefaultsService
{
    private readonly SolicitorSearchSettings _settings;
    private readonly IAreaOfLawOptionsProvider _areaOfLawOptionsProvider;

    public ConveyancingSearchDefaultsService(
        IOptions<SolicitorSearchSettings> options,
        IAreaOfLawOptionsProvider areaOfLawOptionsProvider)
    {
        _settings = options.Value;
        _areaOfLawOptionsProvider = areaOfLawOptionsProvider;
    }

    public async Task<ApplicationResult<ConveyancingSearchDefaults>> GetAsync(CancellationToken cancellationToken)
    {
        var areaOfLawOptions = await _areaOfLawOptionsProvider.GetAsync(cancellationToken);
        var defaults = new ConveyancingSearchDefaults
        {
            DefaultLocations = _settings.DefaultLocations,
            AreaOfLawOptions = areaOfLawOptions,
            SortFields = Enum.GetNames<SolicitorSearchSortField>(),
            SortDirections = Enum.GetNames<SortDirection>(),
            DefaultPageSize = _settings.DefaultPageSize
        };

        return ApplicationResult<ConveyancingSearchDefaults>.Ok(defaults);
    }
}
