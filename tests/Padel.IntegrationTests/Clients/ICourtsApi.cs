using Padel.Api.Features.Bookings;
using Padel.Api.Features.Courts;
using Padel.Domain.Courts;
using Refit;

namespace Padel.IntegrationTests.Clients;

public interface ICourtsApi
{
    [Post("/api/courts")]
    Task<IApiResponse<CreateCourtResponse>> Create([Body] CreateCourtRequest request);

    [Get("/api/courts")]
    Task<IApiResponse<ListCourtsResponse>> List([Query] CourtListParameters? parameters = null);

    [Get("/api/courts/{id}")]
    Task<IApiResponse<GetCourtResponse>> Get(Guid id);

    [Put("/api/courts/{id}/name")]
    Task<IApiResponse<RenameCourtResponse>> Rename(Guid id, [Body] RenameCourtRequest request, [Header("If-Match")] string? ifMatch = null);

    [Put("/api/courts/{id}/maintenance")]
    Task<IApiResponse<SetCourtMaintenanceResponse>> SetMaintenance(Guid id, [Body] SetCourtMaintenanceRequest request);

    [Post("/api/courts/{id}/retire")]
    Task<IApiResponse<RetireCourtResponse>> Retire(Guid id);

    [Get("/api/courts/{id}/bookings")]
    Task<IApiResponse<ListCourtBookingsResponse>> ListBookings(Guid id, [Query] BookingListParameters? parameters = null);
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
