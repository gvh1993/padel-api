// Local development environment: PostgreSQL, Keycloak and the API, with the Aspire dashboard
// for logs, traces, metrics and health. Run with `dotnet run --project src/Padel.AppHost` (or `aspire run`).
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var database = postgres.AddDatabase("padel");

// Fixed port, so the realm URL (token issuer) stays the same between runs. Aspire serves Keycloak over HTTPS
// with the ASP.NET Core development certificate (`dotnet dev-certs https --trust`).
// The realm is imported on first start; the data volume keeps users and their member_id attributes.
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithDataVolume()
    .WithRealmImport("./Realms")
    .WithLifetime(ContainerLifetime.Persistent);

builder.AddDotnetProject("api", "../Padel.Api/Padel.Api.csproj")
    .WithReference(database)
    .WaitFor(database)
    .WaitFor(keycloak)
    .WithEnvironment("Keycloak__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/padel"))
    .WithHttpHealthCheck("/health/ready")
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Scalar (API documentation)";
        url.Url = "/scalar";
    });

builder.Build().Run();
