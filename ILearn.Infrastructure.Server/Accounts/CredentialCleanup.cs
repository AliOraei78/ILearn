using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ILearn.Infrastructure.Accounts;

public sealed class CredentialCleanup(NpgsqlDataSource source, SchemaInitializer schema, ILogger<CredentialCleanup> logger) : BackgroundService
{
    public async Task Clean(CancellationToken ct)
    {
        await schema.Initialize();
        // All live verification, cooldown, lockout and session windows end before these retention cutoffs.
        await using var command = source.CreateCommand("""
            DELETE FROM login_failures WHERE at<clock_timestamp()-interval '1 day';
            DELETE FROM email_deliveries WHERE sent_at<clock_timestamp()-interval '1 day';
            DELETE FROM email_challenges WHERE expires_at<clock_timestamp()-interval '1 day';
            DELETE FROM password_grants WHERE expires_at<clock_timestamp()-interval '1 day';
            DELETE FROM pending_signups WHERE expires_at<clock_timestamp();
            DELETE FROM password_resets WHERE expires_at<clock_timestamp()-interval '1 day';
            DELETE FROM player_sessions WHERE created_at<clock_timestamp()-interval '8 days' OR last_seen_at<clock_timestamp()-interval '2 days';
            """);
        await command.ExecuteNonQueryAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try { await Clean(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (NpgsqlException ex) { logger.LogWarning(ex, "Expired credential cleanup failed; it will retry in one hour."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
