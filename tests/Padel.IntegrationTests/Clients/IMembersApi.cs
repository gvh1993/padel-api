using Padel.Api.Features.Members;
using Padel.Domain.Members;
using Refit;

namespace Padel.IntegrationTests.Clients;

public interface IMembersApi
{
    [Post("/api/v1/members")]
    Task<IApiResponse<RegisterMemberResponse>> Register([Body] RegisterMemberRequest request);

    [Get("/api/v1/members")]
    Task<IApiResponse<ListMembersResponse>> List([Query] MemberListParameters? parameters = null);

    [Get("/api/v1/members/{id}")]
    Task<IApiResponse<GetMemberResponse>> Get(Guid id);

    [Put("/api/v1/members/{id}")]
    Task<IApiResponse<UpdateMemberProfileResponse>> UpdateProfile(Guid id, [Body] UpdateMemberProfileRequest request);

    [Post("/api/v1/members/{id}/suspend")]
    Task<IApiResponse<SuspendMemberResponse>> Suspend(Guid id, [Body] SuspendMemberRequest request);

    [Post("/api/v1/members/{id}/reactivate")]
    Task<IApiResponse<ReactivateMemberResponse>> Reactivate(Guid id);
}

public sealed record MemberListParameters
{
    public string? Cursor { get; init; }
    public int? PageSize { get; init; }
    public string? Sort { get; init; }
    public MemberStatus? Status { get; init; }
    public string? Search { get; init; }
}
