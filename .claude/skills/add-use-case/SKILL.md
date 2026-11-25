---
name: add-use-case
description: Add a new use case (command or query) to the padel API as a vertical slice, with its handler, validator, endpoint, request/response contracts and tests. Use when asked to add an endpoint, an operation or a feature action, such as "members can change their phone number" or "list the free slots of a court".
---

# Add a use case

A use case is one vertical slice through Application and Api, with tests. The architecture tests check the naming and the structure. Follow an existing slice of the same kind: `RenameCourt` (command), `GetCourt` (query by id) or `ListCourts` (list).

## 1. Decide the shape

- **Name:** `<Verb><Noun>`, for example `ChangeMemberPhone`. The same name is used for the command or query, its handler and the endpoint.
- **Command or query:** a command changes state and saves through `IUnitOfWork`; a query only reads.
- **Who may call it:** admin only (`PadelAuthorization.AdminPolicy`), the member themselves (`currentUser.CanAccessMember(memberId)` in the handler), or every signed-in user.
- **Domain rule:** does the rule belong to an aggregate? Put it in the aggregate as a method that returns `Result`, and give it a unit test in `tests/Padel.UnitTests/Domain`.
- **Concurrency:** can two requests break the rule at the same time? Then it also needs a database constraint (`/add-migration`, ADR 6).

## 2. Application: `src/Padel.Application/Features/<Feature>/<UseCase>/`

`<UseCase>Command.cs` holds the request and its handler:

```csharp
public sealed record ChangeMemberPhoneCommand(Guid Id, string? Phone, uint? ExpectedVersion = null) : IRequest<MemberDto>;

public sealed class ChangeMemberPhoneCommandHandler(IMemberRepository members, IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeMemberPhoneCommand, MemberDto>
{
    public async Task<Result<MemberDto>> Handle(ChangeMemberPhoneCommand request, CancellationToken cancellationToken)
    {
        var member = await members.GetByIdAsync(request.Id, cancellationToken);
        if (member is null)
        {
            return ApplicationErrors.NotFound<Member>(request.Id);
        }

        if (member.MatchesVersion(request.ExpectedVersion) is Error stale)
        {
            return stale;
        }

        if (member.ChangePhone(request.Phone) is Error error)
        {
            return error;
        }

        return await unitOfWork.SaveChangesAsync(cancellationToken) is Error saveError ? saveError : member.ToDto();
    }
}
```

`<UseCase>CommandValidator.cs` covers the input shape only, not the business rules. Reuse the rules in `Common/Validation/RuleBuilderExtensions.cs` (`ValidPhone()`, `ValidCourtName()`, ...).

- Expected failures are `Error` values, never exceptions.
- Use `TimeProvider` for the current time.
- A list query derives from `ListQuery`, has a validator that derives from `ListQueryValidator`, and uses `ListOptions<T>.From(request, <Feature>Sorting.Map)`.

Handlers and validators are found automatically; there is nothing to register.

## 3. Api: `src/Padel.Api/Features/<Feature>/<UseCase>/`

- `<UseCase>Request.cs`: the body (a record with `required` properties) or, for lists, the query string with `[AsParameters]`. Leave it out when the route has all the input.
- `<UseCase>Response.cs`: a record with `internal static <UseCase>Response From(<Dto> dto)`. Never return the application's DTO itself.
- `<UseCase>Endpoint.cs`:

```csharp
internal sealed class ChangeMemberPhoneEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapMembersGroup()
            .MapPut("/{id:guid}/phone", HandleAsync)
            .WithName("ChangeMemberPhone")
            .WithSummary("Changes the phone number of a member.")
            .WithDescription("Members can change their own phone number; admins anyone's.")
            .ProducesValidationProblems()
            .ProducesProblems(StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);

    private static async Task<Results<Ok<ChangeMemberPhoneResponse>, ProblemHttpResult>> HandleAsync(
        Guid id,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        HttpResponse response,
        ChangeMemberPhoneRequest request,
        IRequestDispatcher dispatcher,
        CancellationToken cancellationToken) =>
        (await dispatcher.Send<ChangeMemberPhoneCommand, MemberDto>(new ChangeMemberPhoneCommand(id, request.Phone, ETags.ParseIfMatch(ifMatch)), cancellationToken))
            .ToHttpResult(member => response.WithETag(member.Version, TypedResults.Ok(ChangeMemberPhoneResponse.From(member))));
}
```

- Map onto the feature group (`MapMembersGroup()`); it already sits under `/api/v{version}` and documents 401, 403 and 429. Never write `/api/v1` in an endpoint.
- List only the error status codes this endpoint can return in `ProducesProblems`.
- A new kind of `Error` also needs a status code in `Http/ResultExtensions.cs`.

## 4. Tests

- **Domain** (if you added a rule): `tests/Padel.UnitTests/Domain/<Feature>/`.
- **Handler:** `tests/Padel.UnitTests/Application/<Feature>/<Feature>HandlerTests.cs`, with `HandlerFixture` and the builders: the success path, every `Error` the handler can return, and the authorization. Use NSubstitute only to check an interaction or to make a save fail.
- **Validator:** `tests/Padel.UnitTests/Application/Validation/ValidatorTests.cs`.
- **Integration:** add the call to the Refit client in `tests/Padel.IntegrationTests/Clients/I<Feature>Api.cs` (path `/api/v1/...`), then test the HTTP contract in `<Feature>ApiTests.cs`: the status code, the response body and the problem details. Create your own data with unique names or emails.
- Names: `Action_GivenState_ShouldOutcome` or `Action_WhenInput_ShouldOutcome`.

## 5. Finish

1. `dotnet build PadelApi.slnx`: zero warnings (the build treats them as errors).
2. `dotnet test --solution PadelApi.slnx`: the architecture tests check the naming and the slice rules.
3. Update the README when the change affects the API conventions or the booking rules.
