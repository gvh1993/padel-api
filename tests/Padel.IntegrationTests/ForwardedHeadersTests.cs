using System.Net;
using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Padel.IntegrationTests.Builders;
using Padel.IntegrationTests.Infrastructure;

namespace Padel.IntegrationTests;

[ClassDataSource<PadelApiFactory>(Shared = SharedType.PerTestSession)]
public class ForwardedHeadersTests(PadelApiFactory factory)
{
    private static readonly IPAddress ProxyAddress = IPAddress.Parse("10.1.2.3");

    [Test]
    public async Task CreateCourt_WhenTrustedProxyForwardsHttps_ShouldLinkWithHttps()
    {
        await using var api = BehindProxy(trustedNetwork: "10.0.0.0/8");

        var location = await CreateCourtAsync(api);

        location.Scheme.Should().Be("https");
    }

    [Test]
    public async Task CreateCourt_WhenUnknownProxyForwardsHttps_ShouldIgnoreTheHeader()
    {
        await using var api = BehindProxy(trustedNetwork: "192.168.0.0/16");

        var location = await CreateCourtAsync(api);

        location.Scheme.Should().Be("http");
    }

    /// <summary>An API host whose requests come from <see cref="ProxyAddress"/>, trusting <paramref name="trustedNetwork"/>.</summary>
    private WebApplicationFactory<Program> BehindProxy(string trustedNetwork) =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("ForwardedHeaders:KnownNetworks:0", trustedNetwork)
            .ConfigureTestServices(services => services.AddSingleton<IStartupFilter>(new RemoteAddressFilter(ProxyAddress))));

    private async Task<Uri> CreateCourtAsync(WebApplicationFactory<Program> api)
    {
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", await factory.Tokens.AdminAsync());
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        using var response = await client.PostAsJsonAsync("api/v1/courts", CreateCourtRequestBuilder.New().Generate(), PadelApi.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return response.Headers.Location!;
    }

    /// <summary>The test server has no remote address; this gives every request the proxy's.</summary>
    private sealed class RemoteAddressFilter(IPAddress address) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
