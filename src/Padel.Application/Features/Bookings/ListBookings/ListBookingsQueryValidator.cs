using FluentValidation;
using Padel.Application.Common.Querying;
using Padel.Domain.Bookings;

namespace Padel.Application.Features.Bookings;

public sealed class ListBookingsQueryValidator : ListQueryValidator<ListBookingsQuery, Booking>
{
    public ListBookingsQueryValidator() : base(BookingSorting.Map)
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.To).GreaterThan(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
    }
}
