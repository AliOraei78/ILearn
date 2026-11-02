using Npgsql;
namespace ILearn.Infrastructure.Accounts;

internal static class AccountTransaction
{
    internal static async Task Lock(NpgsqlConnection connection, NpgsqlTransaction tx, string key) { await using var command = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,0))", connection, tx); command.Parameters.AddWithValue(key); await command.ExecuteNonQueryAsync(); }
    internal static async Task<DateTime> DatabaseTime(NpgsqlConnection connection, NpgsqlTransaction tx) { await using var clock = new NpgsqlCommand("SELECT clock_timestamp()", connection, tx); return (DateTime)(await clock.ExecuteScalarAsync())!; }
    internal static async Task RevokeCredentials(NpgsqlConnection connection, NpgsqlTransaction tx, Guid profile)
    {
        foreach (var sql in new[] { "UPDATE player_sessions SET revoked=true WHERE profile_id=$1", "UPDATE password_grants SET used=true WHERE profile_id=$1" })
        {
            await using var command = new NpgsqlCommand(sql, connection, tx); command.Parameters.AddWithValue(profile); await command.ExecuteNonQueryAsync();
        }
    }
}
