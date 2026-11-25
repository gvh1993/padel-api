using FluentValidation;
using Padel.Application.Common.Behaviors;
using Padel.Application.Common;
using Padel.Application.Features.Members;

namespace Padel.UnitTests.Application.Messaging;

public class ValidationBehaviorTests
{
    private static readonly MemberDto Response = new() { FirstName = "ok" };

    [Test]
    public async Task Given_ValidRequest_Handle_Should_CallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>([new RegisterMemberCommandValidator()]);
        var command = new RegisterMemberCommand { FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" };

        var result = await behavior.Handle(command, () => Task.FromResult(Response), CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(Response);
    }

    [Test]
    public async Task Given_InvalidRequest_Handle_Should_ThrowWithErrorsPerPropertyAndNotCallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>([new RegisterMemberCommandValidator()]);
        var command = new RegisterMemberCommand { FirstName = "", LastName = "Jansen", Email = "nope" };
        var nextCalled = false;

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => behavior.Handle(
            command,
            () =>
            {
                nextCalled = true;
                return Task.FromResult(Response);
            },
            CancellationToken.None));

        await Assert.That(exception!.Errors.Keys).IsEquivalentTo(["FirstName", "Email"]);
        await Assert.That(nextCalled).IsFalse();
    }

    [Test]
    public async Task Given_NoValidators_Handle_Should_CallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>(Array.Empty<IValidator<RegisterMemberCommand>>());
        var command = new RegisterMemberCommand { FirstName = "", LastName = "", Email = "" };

        var result = await behavior.Handle(command, () => Task.FromResult(Response), CancellationToken.None);

        await Assert.That(result).IsSameReferenceAs(Response);
    }
}
