using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Padel.Api.Authentication;
using Padel.Api.ErrorHandling;
using Padel.Api.Features.Bookings;
using Padel.Api.Features.Courts;
using Padel.Api.Features.Members;
using Padel.Api.Http;
using Padel.Api.OpenApi;
using Padel.Application;
using Padel.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry, health checks, service discovery and HTTP resilience (Padel.ServiceDefaults).
builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Report binding failures (e.g. an unknown enum value in the query string) as problem details.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

// RFC 9457 problem details. The XML writer must be registered first: the first writer that accepts the request wins.
builder.Services.AddSingleton<IProblemDetailsWriter, XmlProblemDetailsWriter>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddPadelAuthentication();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<PadelDocumentTransformer>());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

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

app.MapMemberEndpoints();
app.MapCourtEndpoints();
app.MapBookingEndpoints();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.MigrateDatabaseAsync();
}

await app.RunAsync();

/// <summary>Entry point; public so integration tests can use <c>WebApplicationFactory&lt;Program&gt;</c>.</summary>
public partial class Program;
