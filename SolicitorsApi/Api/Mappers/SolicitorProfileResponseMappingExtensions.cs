using SolicitorsApi.Api.Contracts;
using Domain = SolicitorsApi.Domain;

namespace SolicitorsApi.Api.Mappers;

internal static class SolicitorProfileResponseMappingExtensions
{
    public static SolicitorProfileResponse ToResponse(this Domain.SolicitorProfile profile)
    {
        return new SolicitorProfileResponse
        {
            Name = profile.Name,
            Slug = profile.Slug,
            ProfileUrl = profile.ProfileUrl,
            ContactDetails = profile.ContactDetails.ToResponse(),
            Offices = profile.Offices.Select(ToResponse).ToArray(),
            AreasOfLaw = profile.AreasOfLaw.Select(area => area.Name).ToArray(),
            Review = profile.Review.ToResponse()
        };
    }

    private static SolicitorOffice ToResponse(this Domain.SolicitorOffice office)
    {
        return new SolicitorOffice
        {
            Name = office.Name,
            Address = office.Address,
            Phone = office.Phone,
            Review = office.Review.ToResponse()
        };
    }
}
