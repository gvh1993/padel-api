using FluentValidation;
using Padel.Application.Common.Querying;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public sealed class ListCourtBookingsQueryValidator : ListQueryValidator<ListCourtBookingsQuery, Booking>
{
    public ListCourtBookingsQueryValidator() : base(BookingSorting.Map)
    {
        RuleFor(x => x.CourtId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.To).GreaterThan(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
    }
}
