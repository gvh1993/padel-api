using Padel.Api.Features.Courts;
using Padel.Application.Features.Bookings;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Courts;
using Padel.Domain.Courts;
using Refit;

namespace Padel.IntegrationTests.Clients;

public interface ICourtsApi
{
    [Post("/api/courts")]
    Task<IApiResponse<CourtDto>> Create([Body] CreateCourtCommand command);

    [Get("/api/courts")]
    Task<IApiResponse<PagedResult<CourtDto>>> List([Query] CourtListParameters? parameters = null);

    [Get("/api/courts/{id}")]
    Task<IApiResponse<CourtDto>> Get(Guid id);

    [Put("/api/courts/{id}/name")]
    Task<IApiResponse<CourtDto>> Rename(Guid id, [Body] RenameCourtRequest request);

    [Put("/api/courts/{id}/maintenance")]
    Task<IApiResponse<CourtDto>> SetMaintenance(Guid id, [Body] SetCourtMaintenanceRequest request);

    [Post("/api/courts/{id}/retire")]
    Task<IApiResponse<CourtDto>> Retire(Guid id);

    [Get("/api/courts/{id}/bookings")]
    Task<IApiResponse<PagedResult<BookingDto>>> ListBookings(Guid id, [Query] BookingListParameters? parameters = null);
}

public sealed record CourtListParameters
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Sort { get; init; }
    public CourtStatus? Status { get; init; }
    public string? Surface { get; init; }
    public string? Search { get; init; }
}
