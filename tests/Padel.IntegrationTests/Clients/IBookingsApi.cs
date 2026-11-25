using Padel.Api.Features.Bookings;
using Padel.Application.Features.Bookings;
using Padel.Application.Common.Querying;
using Padel.Domain.Bookings;
using Refit;

namespace Padel.IntegrationTests.Clients;

public interface IBookingsApi
{
    [Post("/api/bookings")]
    Task<IApiResponse<BookingDto>> Create([Body] CreateBookingCommand command);

    [Get("/api/bookings")]
    Task<IApiResponse<PagedResult<BookingDto>>> List([Query] BookingListParameters? parameters = null);

    [Get("/api/bookings/{id}")]
    Task<IApiResponse<BookingDto>> Get(Guid id);

    [Post("/api/bookings/{id}/cancel")]
    Task<IApiResponse<BookingDto>> Cancel(Guid id, [Body] CancelBookingRequest request);

    [Post("/api/bookings/{id}/complete")]
    Task<IApiResponse<BookingDto>> Complete(Guid id);

    [Post("/api/bookings/{id}/no-show")]
    Task<IApiResponse<BookingDto>> MarkNoShow(Guid id);
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
