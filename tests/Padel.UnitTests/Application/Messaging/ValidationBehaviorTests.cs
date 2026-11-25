using AwesomeAssertions;
using FluentValidation;
using NSubstitute;
using Padel.Application.Common.Messaging;
using Padel.Application.Common.Behaviors;
using Padel.Application.Features.Members;
using Padel.Domain.Common;

namespace Padel.UnitTests.Application.Messaging;

public class ValidationBehaviorTests
{
    private static readonly MemberDto Dto = new() { FirstName = "ok" };

    private readonly RequestHandlerDelegate<MemberDto> _next = Substitute.For<RequestHandlerDelegate<MemberDto>>();

    public ValidationBehaviorTests() => _next().Returns(Dto);

    [Test]
    public async Task Handle_WhenValidRequest_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>([new RegisterMemberCommandValidator()]);
        var command = new RegisterMemberCommand { FirstName = "Jan", LastName = "Jansen", Email = "jan@example.com" };

        var result = await behavior.Handle(command, _next, CancellationToken.None);

        result.ShouldSucceed().Should().BeSameAs(Dto);
        await _next.Received(1)();
    }

    [Test]
    public async Task Handle_WhenInvalidRequest_ShouldFailWithErrorsPerPropertyAndNotCallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>([new RegisterMemberCommandValidator()]);
        var command = new RegisterMemberCommand { FirstName = "", LastName = "Jansen", Email = "nope" };
        var error = await behavior.Handle(command, _next, CancellationToken.None).ShouldFail().Is<ValidationError>();

        error.FieldErrors.Keys.Should().BeEquivalentTo(new[] { "FirstName", "Email" });
        await _next.DidNotReceive()();
    }

    [Test]
    public async Task Handle_GivenNoValidators_ShouldCallNext()
    {
        var behavior = new ValidationBehavior<RegisterMemberCommand, MemberDto>(Array.Empty<IValidator<RegisterMemberCommand>>());
        var command = new RegisterMemberCommand { FirstName = "", LastName = "", Email = "" };

        var result = await behavior.Handle(command, _next, CancellationToken.None);

        result.ShouldSucceed().Should().BeSameAs(Dto);
    }
}
