using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Padel.Application.Common;
using Padel.Application.Common.Notifications;
using Padel.Application.Features.Bookings;
using Padel.Application.Features.Courts;
using Padel.Application.Features.Members;
using Padel.Infrastructure.Features.Bookings;
using Padel.Infrastructure.Features.Courts;
using Padel.Infrastructure.Features.Members;
using Padel.Infrastructure.Notifications;
using Padel.Infrastructure.Persistence;
using Padel.Infrastructure.Persistence.Outbox;
using Padel.Infrastructure.Telemetry;

namespace Padel.Infrastructure;

public static class InfrastructureServiceCollection
{
    /// <summary>Name of the connection string; the Aspire AppHost provides it as <c>ConnectionStrings__padel</c>.</summary>
    public const string ConnectionStringName = "padel";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily so configuration added later (e.g. by integration tests) is picked up.
        services.AddDbContext<PadelDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.")));

        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ICourtRepository, CourtRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUnitOfWork, PadelUnitOfWork>();

        // Domain events are stored with the change and handled after the commit.
        services.AddOptions<OutboxOptions>().BindConfiguration(OutboxOptions.SectionName);
        services.AddHostedService<OutboxProcessor>();
        services.TryAddSingleton<INotificationSender, LoggingNotificationSender>();

        // "ready" is the readiness tag of the service defaults: no traffic until the database answers.
        services.AddHealthChecks().AddDbContextCheck<PadelDbContext>("database", tags: ["ready"]);
        services.AddPadelTelemetry();

        return services;
    }

    public static async Task MigrateDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<PadelDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }
}
