using Padel.Api.Features.Members;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Members;
using Padel.Domain.Members;
using Refit;

namespace Padel.IntegrationTests.Clients;

public interface IMembersApi
{
    [Post("/api/members")]
    Task<IApiResponse<MemberDto>> Register([Body] RegisterMemberCommand command);

    [Get("/api/members")]
    Task<IApiResponse<PagedResult<MemberDto>>> List([Query] MemberListParameters? parameters = null);

    [Get("/api/members/{id}")]
    Task<IApiResponse<MemberDto>> Get(Guid id);

    [Put("/api/members/{id}")]
    Task<IApiResponse<MemberDto>> UpdateProfile(Guid id, [Body] UpdateMemberProfileRequest request);

    [Post("/api/members/{id}/suspend")]
    Task<IApiResponse<MemberDto>> Suspend(Guid id, [Body] SuspendMemberRequest request);

    [Post("/api/members/{id}/reactivate")]
    Task<IApiResponse<MemberDto>> Reactivate(Guid id);
}

public sealed record MemberListParameters
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Sort { get; init; }
    public MemberStatus? Status { get; init; }
    public string? Search { get; init; }
}
