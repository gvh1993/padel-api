using System.Text;
using Bogus;

namespace Padel.IntegrationTests.Builders;

/// <summary>Generated values the request builders share. Everything is unique, so tests can share the database.</summary>
internal static class TestData
{
    /// <summary>A valid, unique address at a reserved example domain, based on the name.</summary>
    public static string Email(Faker faker, string firstName, string lastName) =>
        $"{Slug(firstName)}.{Slug(lastName)}.{Guid.NewGuid():N}@{faker.PickRandom("example.com", "example.nl", "example.org")}";

    /// <summary>A Dutch mobile number (06 and eight digits).</summary>
    public static string MobileNumber(Faker faker) => faker.Random.ReplaceNumbers("06########");

    /// <summary>A whole-hour start time, <paramref name="daysAhead"/> days from now (UTC).</summary>
    public static DateTimeOffset FutureSlot(int daysAhead = 7, int hour = 10)
    {
        var today = DateTimeOffset.UtcNow;
        return new DateTimeOffset(today.Year, today.Month, today.Day, hour, 0, 0, TimeSpan.Zero).AddDays(daysAhead);
    }

    // Lower case ASCII letters and digits only: "Jan-Willem van 't Hof" becomes "janwillemvanthof".
    private static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
            }
        }

        return builder.Length > 0 ? builder.ToString() : "member";
    }
}
