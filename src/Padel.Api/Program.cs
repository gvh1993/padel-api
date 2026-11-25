using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Padel.Api.Authentication;
using Padel.Api.ErrorHandling;
using Padel.Api.Http;
using Padel.Api.OpenApi;
using Padel.Application;
using Padel.Domain.Bookings;
using Padel.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry, health checks, service discovery and HTTP resilience (Padel.ServiceDefaults).
builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddOptions<BookingPolicy>().BindConfiguration("Club:Bookings");
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Report binding failures (e.g. an unknown enum value in the query string) as problem details.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

// RFC 9457 problem details for every error, with the trace id to find it in the logs.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddPadelAuthentication();
builder.Services.AddPadelForwardedHeaders();
builder.Services.AddPadelRateLimiting();
builder.Services.AddPadelApiVersioning();

foreach (var version in ApiVersioning.Versions)
{
    builder.Services.AddOpenApi(ApiVersioning.DocumentName(version), options => options.AddDocumentTransformer<PadelDocumentTransformer>());
}

var app = builder.Build();

// First, so everything after it (logging, rate limiting, links) sees the client's address and scheme.
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapDefaultEndpoints();

if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    var keycloak = app.Services.GetRequiredService<IOptions<KeycloakOptions>>().Value;

    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options
        .WithTitle("Padel API")
        .AddPreferredSecuritySchemes(PadelDocumentTransformer.SecuritySchemeName)
        .AddAuthorizationCodeFlow(PadelDocumentTransformer.SecuritySchemeName, flow => flow
            .WithClientId(keycloak.DocumentationClientId)
            .WithPkce(Pkce.Sha256)
            .WithSelectedScopes(["openid"]))
        .EnablePersistentAuthentication())
        .AllowAnonymous();
}

app.MapVersionedApi().MapEndpoints();


await app.RunAsync();

/// <summary>Entry point; public so integration tests can use <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
