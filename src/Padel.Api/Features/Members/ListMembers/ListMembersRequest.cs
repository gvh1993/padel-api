using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Padel.Api.Http;
using Padel.Application.Common.Querying;
using Padel.Domain.Members;

namespace Padel.Api.Features.Members;

/// <summary>Query string of <c>GET /api/members</c>.</summary>
public sealed record ListMembersRequest(
    [Description(QueryDescriptions.Page)][FromQuery(Name = "page")] int Page = 1,
    [Description(QueryDescriptions.PageSize)][FromQuery(Name = "pageSize")] int PageSize = ListQuery.DefaultPageSize,
    [Description(QueryDescriptions.Sort)][FromQuery(Name = "sort")] string? Sort = null,
    [Description("Only members with this status.")][FromQuery(Name = "status")] MemberStatus? Status = null,
    [Description("Case-insensitive match on first name, last name or email.")][FromQuery(Name = "search")] string? Search = null);
