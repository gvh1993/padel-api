using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Padel.IntegrationTests.Clients;
using Refit;

namespace Padel.IntegrationTests.Infrastructure;

/// <summary>
/// Typed Refit clients for one caller (one bearer token).
/// </summary>
public sealed class PadelApi
{
    // Refit 17 is trimming-friendly and needs explicit type metadata; reflection is fine for tests.
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    private static readonly RefitSettings Settings = new(new SystemTextJsonContentSerializer(JsonOptions));

    public PadelApi(HttpClient httpClient)
    {
        Members = RestService.ForGenerated<IMembersApi>(httpClient, Settings);
        Courts = RestService.ForGenerated<ICourtsApi>(httpClient, Settings);
        Bookings = RestService.ForGenerated<IBookingsApi>(httpClient, Settings);
        Raw = RestService.ForGenerated<IRawApi>(httpClient, Settings);
    }

    public IMembersApi Members { get; }
    public ICourtsApi Courts { get; }
    public IBookingsApi Bookings { get; }

    /// <summary>Untyped access for content negotiation and documentation endpoints.</summary>
    public IRawApi Raw { get; }
}
