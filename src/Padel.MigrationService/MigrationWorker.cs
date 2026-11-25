using System.Diagnostics;
using Padel.Infrastructure;

namespace Padel.MigrationService;

/// <summary>Migrates the database once, then stops the application (exit code 1 on failure).</summary>
internal sealed partial class MigrationWorker(
    IServiceProvider services,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    private static readonly ActivitySource ActivitySource = new("Padel.MigrationService");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var activity = ActivitySource.StartActivity("Migrate database");

        try
        {
            await services.MigrateDatabaseAsync(stoppingToken);
            LogMigrated();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            activity?.AddException(exception);
            activity?.SetStatus(ActivityStatusCode.Error);
            LogFailed(exception);
            Environment.ExitCode = 1;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "The database is up to date")]
    private partial void LogMigrated();

    [LoggerMessage(Level = LogLevel.Critical, Message = "Migrating the database failed")]
    private partial void LogFailed(Exception exception);
}
