using SolicitorsApi.Api.Contracts;
using SolicitorsApi.Application.Queries;

namespace SolicitorsApi.Api.Mappers;

internal static class LocationSuggestionMappingExtensions
{
    public static LocationSuggestion ToResponse(this LocationSuggestionResult suggestion)
    {
        return new LocationSuggestion
        {
            Title = suggestion.Title,
            Text = suggestion.Text
        };
    }
}
