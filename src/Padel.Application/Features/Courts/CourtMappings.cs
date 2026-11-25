using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public static class CourtMappings
{
    public static CourtDto ToDto(this Court court) => new()
    {
        Id = court.Id,
        Version = court.Version,
        Name = court.Name,
        Surface = court.Surface,
        Status = court.Status,
    };
}
