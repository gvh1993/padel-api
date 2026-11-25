using FluentValidation;
using Padel.Application.Common.Behaviors;
using Padel.Application.Features.Members;
using Padel.Domain.Common;

namespace Padel.UnitTests.Application.Messaging;

public class ValidationBehaviorTests
{
    private static readonly MemberDto Dto = new() { FirstName = "ok" };

    private static Task<Result<MemberDto>> Next() => Task.FromResult<Result<MemberDto>>(Dto);

    [Test]
    public async Task Given_ValidRequest_Handle_Should_CallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>([new RegisterMemberCommandValidator()]);
        var command = new RegisterMemberCommand { FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" };

        var result = await behavior.Handle(command, Next, CancellationToken.None);

        await Assert.That(result.ShouldSucceed()).IsSameReferenceAs(Dto);
    }

    [Test]
    public async Task Given_InvalidRequest_Handle_Should_FailWithErrorsPerPropertyAndNotCallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>([new RegisterMemberCommandValidator()]);
        var command = new RegisterMemberCommand { FirstName = "", LastName = "Jansen", Email = "nope" };
        var nextCalled = false;

        var error = await behavior.Handle(
            command,
            () =>
            {
                nextCalled = true;
                return Next();
            },
            CancellationToken.None).ShouldFail().Is<ValidationError>();

        await Assert.That(error.FieldErrors.Keys).IsEquivalentTo(["FirstName", "Email"]);
        await Assert.That(nextCalled).IsFalse();
    }

    [Test]
    public async Task Given_NoValidators_Handle_Should_CallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>(Array.Empty<IValidator<RegisterMemberCommand>>());
        var command = new RegisterMemberCommand { FirstName = "", LastName = "", Email = "" };

        var result = await behavior.Handle(command, Next, CancellationToken.None);

        await Assert.That(result.ShouldSucceed()).IsSameReferenceAs(Dto);
    }
}
