using Padel.Infrastructure;
using Padel.MigrationService;

// Applies the EF Core migrations and exits. Aspire starts the API only after it completed;
// in production, run it as a separate step (for example a Kubernetes Job) before rolling out the API.
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddPadelDatabase(builder.Configuration);
builder.Services.AddHostedService<MigrationWorker>();

await builder.Build().RunAsync();
