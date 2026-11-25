using System.Text;
using Bogus;

namespace Padel.UnitTests.Builders;

/// <summary>The fixed moments the unit tests use, and generated values the builders share.</summary>
internal static class TestData
{
    /// <summary>"Now" in the unit tests: 25 November 2026, 09:00 UTC.</summary>
    public static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);

    /// <summary>26 November 2026, 10:00 UTC (11:00 in Amsterdam): within opening hours and well before the cancellation notice.</summary>
    public static readonly DateTimeOffset Tomorrow10 = new(2026, 11, 26, 10, 0, 0, TimeSpan.Zero);

    /// <summary>A valid, unique address at a reserved example domain, based on the name.</summary>
    public static string Email(Faker faker, string firstName, string lastName) =>
        $"{Slug(firstName)}.{Slug(lastName)}.{faker.Random.AlphaNumeric(6)}@{faker.PickRandom("example.com", "example.nl", "example.org")}";

    /// <summary>A Dutch mobile number (06 and eight digits).</summary>
    public static string MobileNumber(Faker faker) => faker.Random.ReplaceNumbers("06########");

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
