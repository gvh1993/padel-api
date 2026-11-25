using AwesomeAssertions;
using Padel.Application.Features.Bookings;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;

namespace Padel.UnitTests.Application.Validation;

public class ValidatorTests
{
    private static RegisterMemberCommand ValidRegistration => new()
    {
        FirstName = "Gert-Jan",
        LastName = "De Vries",
        Email = "gj@example.com",
        Phone = "+31 (0)10-123 4567",
    };

    [Test]
    public async Task Given_ValidRegistration_RegisterMemberValidator_Should_Pass()
    {
        var result = await new RegisterMemberCommandValidator().ValidateAsync(ValidRegistration);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    [Arguments("abc")]
    [Arguments("1234567")]
    [Arguments("0612345678x")]
    [Arguments("06+12345678")]
    public async Task Given_InvalidPhone_RegisterMemberValidator_Should_ReportPhone(string phone)
    {
        var result = await new RegisterMemberCommandValidator().ValidateAsync(ValidRegistration with { Phone = phone });

        result.Errors.Select(e => e.PropertyName).Should().Contain("Phone");
    }

    [Test]
    public async Task Given_EmptyNamesAndInvalidEmail_RegisterMemberValidator_Should_ReportEveryField()
    {
        var result = await new RegisterMemberCommandValidator().ValidateAsync(
            ValidRegistration with { FirstName = "", LastName = " ", Email = "not-an-email" });

        result.Errors.Select(e => e.PropertyName).Distinct().Should().BeEquivalentTo(new[] { "FirstName", "LastName", "Email" });
    }

    [Test]
    [Arguments(29)]
    [Arguments(181)]
    [Arguments(-60)]
    public async Task Given_InvalidDuration_CreateBookingValidator_Should_ReportEnd(int minutes)
    {
        var start = new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero);

        var result = await new CreateBookingCommandValidator().ValidateAsync(new CreateBookingCommand
        {
            MemberId = Guid.NewGuid(),
            CourtId = Guid.NewGuid(),
            Start = start,
            End = start.AddMinutes(minutes),
        });

        result.Errors.Select(e => e.PropertyName).Should().Contain("End");
    }

    [Test]
    public async Task Given_EmptyIds_CreateBookingValidator_Should_ReportBothIds()
    {
        var start = new DateTimeOffset(2026, 12, 1, 10, 0, 0, TimeSpan.Zero);

        var result = await new CreateBookingCommandValidator().ValidateAsync(new CreateBookingCommand
        {
            MemberId = Guid.Empty,
            CourtId = Guid.Empty,
            Start = start,
            End = start.AddHours(1),
        });

        result.Errors.Select(e => e.PropertyName).Should().BeEquivalentTo(new[] { "MemberId", "CourtId" });
    }

    [Test]
    [Arguments(0, 20)]
    [Arguments(1, 0)]
    [Arguments(1, 101)]
    public async Task Given_InvalidPaging_ListQueryValidator_Should_Fail(int page, int pageSize)
    {
        var result = await new ListCourtsQueryValidator().ValidateAsync(new ListCourtsQuery { Page = page, PageSize = pageSize });

        result.IsValid.Should().BeFalse();
    }

    [Test]
    public async Task Given_UnknownSortField_ListQueryValidator_Should_ListAllowedFields()
    {
        var result = await new ListMembersQueryValidator().ValidateAsync(new ListMembersQuery { Sort = "lastName,-password" });

        var error = result.Errors.Single();
        error.PropertyName.Should().Be("Sort");
        error.ErrorMessage.Should().Contain("password");
        error.ErrorMessage.Should().Contain("registeredOn");
    }

    [Test]
    public async Task Given_ToBeforeFrom_ListBookingsValidator_Should_ReportTo()
    {
        var from = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await new ListBookingsQueryValidator().ValidateAsync(new ListBookingsQuery { From = from, To = from.AddDays(-1) });

        result.Errors.Select(e => e.PropertyName).Should().Contain("To");
    }
}
