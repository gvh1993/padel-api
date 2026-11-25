using System.ComponentModel;
using Padel.Api.Authentication;
using Padel.Api.Http;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Members;
using Padel.Domain.Members;

namespace Padel.Api.Features.Members;

internal static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder routes)
    {
        var members = routes.MapFeatureGroup("/api/members", "Members");

        members.MapPost("/", async (RegisterMemberCommand command, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var member = await dispatcher.Send<RegisterMemberCommand, MemberDto>(command, cancellationToken);
                return TypedResults.CreatedAtRoute(member, "GetMember", new { id = member.Id });
            })
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("RegisterMember")
            .WithSummary("Registers a new member.")
            .WithDescription("Admin only.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status409Conflict);

        members.MapGet("/", async (
                [AsParameters] Paging paging,
                [Description("Only members with this status.")] MemberStatus? status,
                [Description("Case-insensitive match on first name, last name or email.")] string? search,
                IRequestDispatcher dispatcher,
                CancellationToken cancellationToken) =>
            {
                var query = new ListMembersQuery
                {
                    Page = paging.Page,
                    PageSize = paging.PageSize,
                    Sort = paging.Sort,
                    Status = status,
                    Search = search,
                };

                return TypedResults.Ok(await dispatcher.Send<ListMembersQuery, PagedResult<MemberDto>>(query, cancellationToken));
            })
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("ListMembers")
            .WithSummary("Lists members with paging, sorting and filtering.")
            .WithDescription("Admin only. Sort fields: firstName, lastName, email, status, registeredOn.")
            .ProducesValidationProblems();

        members.MapGet("/{id:guid}", async (Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await dispatcher.Send<GetMemberQuery, MemberDto>(new GetMemberQuery(id), cancellationToken)))
            .WithName("GetMember")
            .WithSummary("Gets a member.")
            .WithDescription("Admins, or the member itself.")
            .ProducesProblems(StatusCodes.Status404NotFound);

        members.MapPut("/{id:guid}", async (Guid id, UpdateMemberProfileRequest request, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
            {
                var command = new UpdateMemberProfileCommand
                {
                    Id = id,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    Phone = request.Phone,
                };

                return TypedResults.Ok(await dispatcher.Send<UpdateMemberProfileCommand, MemberDto>(command, cancellationToken));
            })
            .WithName("UpdateMemberProfile")
            .WithSummary("Updates the name, email and phone of a member.")
            .WithDescription("Admins, or the member itself.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status409Conflict);

        members.MapPost("/{id:guid}/suspend", async (Guid id, SuspendMemberRequest? request, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await dispatcher.Send<SuspendMemberCommand, MemberDto>(new SuspendMemberCommand(id, request?.Reason), cancellationToken)))
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("SuspendMember")
            .WithSummary("Suspends a member. Suspended members cannot book.")
            .WithDescription("Admin only. Suspending a suspended member has no effect.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound);

        members.MapPost("/{id:guid}/reactivate", async (Guid id, IRequestDispatcher dispatcher, CancellationToken cancellationToken) =>
                TypedResults.Ok(await dispatcher.Send<ReactivateMemberCommand, MemberDto>(new ReactivateMemberCommand(id), cancellationToken)))
            .RequireAuthorization(PadelAuthorization.AdminPolicy)
            .WithName("ReactivateMember")
            .WithSummary("Reactivates a suspended member.")
            .WithDescription("Admin only.")
            .ProducesProblems(StatusCodes.Status404NotFound);

        return routes;
    }
}
