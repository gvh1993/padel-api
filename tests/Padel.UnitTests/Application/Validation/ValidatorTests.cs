using AwesomeAssertions;
using Padel.Application.Common.Querying;
using Padel.Application.Features.Bookings;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;
using Padel.UnitTests.Builders;

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
    public async Task RegisterMemberValidator_WhenValidRegistration_ShouldPass()
    {
        var result = await new RegisterMemberCommandValidator().ValidateAsync(ValidRegistration);

        result.IsValid.Should().BeTrue();
    }

    [Test]
    [Arguments("abc")]
    [Arguments("1234567")]
    [Arguments("0612345678x")]
    [Arguments("06+12345678")]
    public async Task RegisterMemberValidator_WhenInvalidPhone_ShouldReportPhone(string phone)
    {
        var result = await new RegisterMemberCommandValidator().ValidateAsync(ValidRegistration with { Phone = phone });

        result.Errors.Select(e => e.PropertyName).Should().Contain("Phone");
    }

    [Test]
    public async Task RegisterMemberValidator_WhenEmptyNamesAndInvalidEmail_ShouldReportEveryField()
    {
        var result = await new RegisterMemberCommandValidator().ValidateAsync(
            ValidRegistration with { FirstName = "", LastName = " ", Email = "not-an-email" });

        result.Errors.Select(e => e.PropertyName).Distinct().Should().BeEquivalentTo(new[] { "FirstName", "LastName", "Email" });
    }

    [Test]
    [Arguments(29)]
    [Arguments(181)]
    [Arguments(-60)]
    public async Task CreateBookingValidator_WhenInvalidDuration_ShouldReportEnd(int minutes)
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
    public async Task CreateBookingValidator_WhenEmptyIds_ShouldReportBothIds()
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
    [Arguments(0)]
    [Arguments(101)]
    public async Task ListQueryValidator_WhenInvalidPageSize_ShouldFail(int pageSize)
    {
        var result = await new ListCourtsQueryValidator().ValidateAsync(new ListCourtsQuery { PageSize = pageSize });

        result.Errors.Select(e => e.PropertyName).Should().Equal("PageSize");
    }

    [Test]
    public async Task ListQueryValidator_WhenCursorIsNotACursor_ShouldReportCursor()
    {
        var result = await new ListCourtsQueryValidator().ValidateAsync(new ListCourtsQuery { Cursor = "not-a-cursor" });

        result.Errors.Select(e => e.PropertyName).Should().Equal("Cursor");
    }

    [Test]
    public async Task ListQueryValidator_WhenCursorWasMadeForAnotherSort_ShouldReportCursor()
    {
        var court = CourtBuilder.New().Generate();
        var cursor = PageCursor.Encode(court, CourtSorting.Map.Resolve("name"));

        var sameSort = await new ListCourtsQueryValidator().ValidateAsync(new ListCourtsQuery { Cursor = cursor, Sort = "NAME" });
        var otherSort = await new ListCourtsQueryValidator().ValidateAsync(new ListCourtsQuery { Cursor = cursor, Sort = "-name" });

        sameSort.IsValid.Should().BeTrue();
        otherSort.Errors.Select(e => e.PropertyName).Should().Equal("Cursor");
    }

    [Test]
    public async Task ListQueryValidator_WhenUnknownSortField_ShouldListAllowedFields()
    {
        var result = await new ListMembersQueryValidator().ValidateAsync(new ListMembersQuery { Sort = "lastName,-password" });

        var error = result.Errors.Single();
        error.PropertyName.Should().Be("Sort");
        error.ErrorMessage.Should().Contain("password");
        error.ErrorMessage.Should().Contain("registeredOn");
    }

    [Test]
    public async Task ListBookingsValidator_WhenToBeforeFrom_ShouldReportTo()
    {
        var from = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await new ListBookingsQueryValidator().ValidateAsync(new ListBookingsQuery { From = from, To = from.AddDays(-1) });

        result.Errors.Select(e => e.PropertyName).Should().Contain("To");
    }
}
