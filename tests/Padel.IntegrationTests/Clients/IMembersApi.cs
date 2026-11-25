using Padel.Api.Features.Members;
using Padel.Domain.Members;
using Refit;

namespace Padel.IntegrationTests.Clients;

public interface IMembersApi
{
    [Post("/api/members")]
    Task<IApiResponse<RegisterMemberResponse>> Register([Body] RegisterMemberRequest request);

    [Get("/api/members")]
    Task<IApiResponse<ListMembersResponse>> List([Query] MemberListParameters? parameters = null);

    [Get("/api/members/{id}")]
    Task<IApiResponse<GetMemberResponse>> Get(Guid id);

    [Put("/api/members/{id}")]
    Task<IApiResponse<UpdateMemberProfileResponse>> UpdateProfile(Guid id, [Body] UpdateMemberProfileRequest request);

    [Post("/api/members/{id}/suspend")]
    Task<IApiResponse<SuspendMemberResponse>> Suspend(Guid id, [Body] SuspendMemberRequest request);

    [Post("/api/members/{id}/reactivate")]
    Task<IApiResponse<ReactivateMemberResponse>> Reactivate(Guid id);
}

public sealed record MemberListParameters
{
    public int? Page { get; init; }
    public int? PageSize { get; init; }
    public string? Sort { get; init; }
    public MemberStatus? Status { get; init; }
    public string? Search { get; init; }
}
