using Padel.Api.Features.Bookings;
using Padel.Domain.Bookings;
using Refit;

namespace Padel.IntegrationTests.Clients;

public interface IBookingsApi
{
    [Post("/api/bookings")]
    Task<IApiResponse<CreateBookingResponse>> Create([Body] CreateBookingRequest request, [Header("Idempotency-Key")] string? idempotencyKey = null);

    [Get("/api/bookings")]
    Task<IApiResponse<ListBookingsResponse>> List([Query] BookingListParameters? parameters = null);

    [Get("/api/bookings/{id}")]
    Task<IApiResponse<GetBookingResponse>> Get(Guid id);

    [Post("/api/bookings/{id}/cancel")]
    Task<IApiResponse<CancelBookingResponse>> Cancel(Guid id, [Body] CancelBookingRequest request);

    [Post("/api/bookings/{id}/complete")]
    Task<IApiResponse<CompleteBookingResponse>> Complete(Guid id);

    [Post("/api/bookings/{id}/no-show")]
    Task<IApiResponse<MarkBookingNoShowResponse>> MarkNoShow(Guid id);
}

public sealed record BookingListParameters
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Sort { get; init; }
    public Guid? MemberId { get; init; }
    public Guid? CourtId { get; init; }
    public BookingStatus? Status { get; init; }

    [Query(Format = "o")]
    public DateTimeOffset? From { get; init; }

    [Query(Format = "o")]
    public DateTimeOffset? To { get; init; }
}
