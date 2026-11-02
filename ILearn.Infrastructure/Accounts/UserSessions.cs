using Microsoft.AspNetCore.Identity;
using Npgsql;
namespace ILearn.Infrastructure.Accounts;

public sealed partial class PostgresAccounts
{
    string Hash(string value) => Convert.ToHexString(System.Security.Cryptography.HMACSHA256.HashData(Convert.FromBase64String(config["Auth:CodeKey"]!), System.Text.Encoding.UTF8.GetBytes(value)));
    public async Task<Guid> CreateSession(Guid profileId)
    {
        return await StorageErrors.Run<Guid>(async () =>
        {
            var id = Guid.NewGuid(); await using var command = source.CreateCommand("INSERT INTO player_sessions(id,profile_id,created_at,last_seen_at) VALUES($1,$2,clock_timestamp(),clock_timestamp())"); command.Parameters.AddWithValue(id); command.Parameters.AddWithValue(profileId); await command.ExecuteNonQueryAsync(); return id;
        });
    }
    public async Task RevokeSession(SessionIdentity principal)
    {
        await StorageErrors.Run(async () =>
        {
            var id = principal.SessionId;
            await using var command = source.CreateCommand("UPDATE player_sessions SET revoked=true WHERE id=$1"); command.Parameters.AddWithValue(id); await command.ExecuteNonQueryAsync();
        });
    }
    public async Task<bool> ValidSession(SessionIdentity principal)
    {
        return await StorageErrors.Run<bool>(async () =>
        {
            await Initialize(); var session = principal.SessionId; var profile = principal.PlayerId;
            await using var command = source.CreateCommand("""
   UPDATE player_sessions s SET last_seen_at=clock_timestamp() FROM profiles p
   WHERE s.id=$1 AND s.profile_id=$2 AND p.id=s.profile_id AND NOT s.revoked
   AND s.created_at>clock_timestamp()-interval '7 days'
   AND s.last_seen_at>clock_timestamp()-interval '24 hours' AND p.security_stamp=$3
   RETURNING s.id
   """); command.Parameters.AddWithValue(session); command.Parameters.AddWithValue(profile); command.Parameters.AddWithValue(principal.Stamp); return await command.ExecuteScalarAsync() is Guid;
        });
    }
    public async Task<Player?> Login(string identifier, string password, string ip)
    {
        return await StorageErrors.Run<Player?>(async () =>
        {
            if (identifier.Length > 254 || password.Length > 256) return null; identifier = identifier.Trim().ToLowerInvariant();
            await Initialize(); var eh = Hash(identifier); var ih = Hash(ip);
            await using var connection = await source.OpenConnectionAsync(); await using var tx = await connection.BeginTransactionAsync();
            await AccountTransaction.Lock(connection, tx, "ip:" + ih); await AccountTransaction.Lock(connection, tx, "email:" + identifier);
            var now = await AccountTransaction.DatabaseTime(connection, tx);
            await using (var limit = new NpgsqlCommand("SELECT count(*) FILTER(WHERE email_hash=$1),min(at) FILTER(WHERE email_hash=$1),count(*) FILTER(WHERE ip_hash=$2),min(at) FILTER(WHERE ip_hash=$2) FROM login_failures WHERE at>$3", connection, tx))
            {
                limit.Parameters.AddWithValue(eh); limit.Parameters.AddWithValue(ih); limit.Parameters.AddWithValue(now.AddMinutes(-15)); await using var reader = await limit.ExecuteReaderAsync(); await reader.ReadAsync();
                var deadline = now; if (reader.GetInt64(0) >= 5) deadline = reader.GetDateTime(1).AddMinutes(15); if (reader.GetInt64(2) >= 50 && reader.GetDateTime(3).AddMinutes(15) > deadline) deadline = reader.GetDateTime(3).AddMinutes(15); if (deadline > now) throw new VerificationLimit(Math.Max(1, (int)Math.Ceiling((deadline - now).TotalSeconds)));
            }
            Player? player = null; string? hash = null; bool confirmed = false;
            await using (var query = new NpgsqlCommand("SELECT id,name,avatar,security_stamp,password_hash,email_confirmed FROM profiles WHERE email=$1", connection, tx))
            {
                query.Parameters.AddWithValue(identifier); await using var reader = await query.ExecuteReaderAsync(); if (await reader.ReadAsync()) { player = new(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)); hash = reader.IsDBNull(4) ? null : reader.GetString(4); confirmed = reader.GetBoolean(5); }
            }
            var result = AccountPasswords.Hasher.VerifyHashedPassword(player ?? AccountPasswords.Dummy, hash ?? AccountPasswords.DummyHash, password);
            if (player is null || hash is null || !confirmed || result == PasswordVerificationResult.Failed) { await using var fail = new NpgsqlCommand("INSERT INTO login_failures VALUES($1,$2,$3)", connection, tx); fail.Parameters.AddWithValue(eh); fail.Parameters.AddWithValue(ih); fail.Parameters.AddWithValue(now); await fail.ExecuteNonQueryAsync(); await tx.CommitAsync(); return null; }
            await using (var clear = new NpgsqlCommand("UPDATE login_failures SET email_hash='' WHERE email_hash=$1 AND at>$2", connection, tx)) { clear.Parameters.AddWithValue(eh); clear.Parameters.AddWithValue(now.AddMinutes(-15)); await clear.ExecuteNonQueryAsync(); }
            if (result == PasswordVerificationResult.SuccessRehashNeeded) { await using var rehash = new NpgsqlCommand("UPDATE profiles SET password_hash=$1 WHERE id=$2 AND password_hash=$3", connection, tx); rehash.Parameters.AddWithValue(AccountPasswords.Hasher.HashPassword(player, password)); rehash.Parameters.AddWithValue(player.Id); rehash.Parameters.AddWithValue(hash); await rehash.ExecuteNonQueryAsync(); }
            await tx.CommitAsync(); return player;
        });
    }
}
