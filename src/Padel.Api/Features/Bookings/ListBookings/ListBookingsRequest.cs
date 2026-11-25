using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Http;
using Padel.Application.Common.Querying;
using Padel.Domain.Bookings;

namespace Padel.Api.Features.Bookings;

/// <summary>Query string of <c>GET /api/v1/bookings</c>.</summary>
public sealed record ListBookingsRequest(
    [Description(QueryDescriptions.Cursor)][FromQuery(Name = "cursor")] string? Cursor = null,
    [Description(QueryDescriptions.PageSize)][FromQuery(Name = "pageSize")] int PageSize = ListQuery.DefaultPageSize,
    [Description(QueryDescriptions.Sort)][FromQuery(Name = "sort")] string? Sort = null,
    [Description("Only bookings of this member. Ignored for members, who only see their own bookings.")][FromQuery(Name = "memberId")] Guid? MemberId = null,
    [Description("Only bookings on this court.")][FromQuery(Name = "courtId")] Guid? CourtId = null,
    [Description("Only bookings with this status.")][FromQuery(Name = "status")] BookingStatus? Status = null,
    [Description("Only bookings that end after this moment.")][FromQuery(Name = "from")] DateTimeOffset? From = null,
    [Description("Only bookings that start before this moment.")][FromQuery(Name = "to")] DateTimeOffset? To = null);
