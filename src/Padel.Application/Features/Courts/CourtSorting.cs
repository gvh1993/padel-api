using Padel.Application.Common.Querying;
using Padel.Domain.Courts;

namespace Padel.Application.Features.Courts;

public static class CourtSorting
{
    public static readonly SortMap<Court> Map = new SortMap<Court>("name")
        .Add("name", court => court.Name)
        .Add("surface", court => court.Surface)
        .Add("status", court => court.Status);
}
