using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Http;
using Padel.Application.Common.Querying;
using Padel.Domain.Courts;

namespace Padel.Api.Features.Courts;

/// <summary>Query string of <c>GET /api/courts</c>.</summary>
public sealed record ListCourtsRequest(
    [Description(QueryDescriptions.Page)][FromQuery(Name = "page")] int Page = 1,
    [Description(QueryDescriptions.PageSize)][FromQuery(Name = "pageSize")] int PageSize = ListQuery.DefaultPageSize,
    [Description(QueryDescriptions.Sort)][FromQuery(Name = "sort")] string? Sort = null,
    [Description("Only courts with this status.")][FromQuery(Name = "status")] CourtStatus? Status = null,
    [Description("Case-insensitive exact match on the surface, e.g. `glass`.")][FromQuery(Name = "surface")] string? Surface = null,
    [Description("Case-insensitive match on the court name.")][FromQuery(Name = "search")] string? Search = null);
