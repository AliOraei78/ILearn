using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
namespace ILearn.Infrastructure.Accounts;

public sealed class EmailVerification(NpgsqlDataSource source, SchemaInitializer database, IConfiguration config, IEmailSender sender) : IEmailVerification
{
    public static string Normalize(string value) => EmailAddress.Normalize(value);
    public static string NormalizeCode(string value) => string.Concat(value.Select(c => c is >= '\u06f0' and <= '\u06f9' ? (char)('0' + c - '\u06f0') : c is >= '\u0660' and <= '\u0669' ? (char)('0' + c - '\u0660') : c));
    internal string Hash(string value) => Convert.ToHexString(HMACSHA256.HashData(Convert.FromBase64String(config["Auth:CodeKey"] ?? throw new InvalidOperationException("Auth:CodeKey is required.")), Encoding.UTF8.GetBytes(value)));
    const string NextSendSql = """
  SELECT GREATEST(
   (SELECT max(sent_at)+interval '120 seconds' FROM email_deliveries WHERE email=$1),
   (SELECT next_send_at FROM email_challenges WHERE email=$1),
   (SELECT next_send_at FROM pending_signups WHERE email=$1),
   (SELECT next_send_at FROM password_resets WHERE email=$1))
  """;
    internal static async Task<DateTime> CheckCooldown(NpgsqlConnection connection, NpgsqlTransaction tx, string email) { var now = await DatabaseTime(connection, tx); await using var command = new NpgsqlCommand(NextSendSql, connection, tx); command.Parameters.AddWithValue(email); if (await command.ExecuteScalarAsync() is DateTime next && next > now) throw new VerificationLimit(Math.Max(1, (int)Math.Ceiling((next - now).TotalSeconds))); return now; }
    public async Task<(string Email, bool Delivered)> Start(string email, string? name, string? avatar, string ip)
    {
        return await StorageErrors.Run<(string Email, bool Delivered)>(async () =>
        {
            email = Normalize(email); name = name?.Trim(); if (name is not null && (name.Length is < 1 or > 50 || !Enumerable.Range(1, 48).Any(i => avatar == $"avatar-{i:00}"))) throw new ArgumentException("نام یا کاراکتر معتبر نیست.");
            await database.Initialize(); var code = RandomNumberGenerator.GetInt32(1000000).ToString("D6"); var ih = Hash(ip);
            await using (var connection = await source.OpenConnectionAsync())
            {
                await using var tx = await connection.BeginTransactionAsync(); await Lock(connection, tx, "ip:" + ih); await Lock(connection, tx, "email:" + email); var now = await CheckCooldown(connection, tx, email);
                await using (var limit = new NpgsqlCommand("SELECT count(*) FILTER(WHERE email=$1),count(*) FILTER(WHERE ip_hash=$2) FROM email_deliveries WHERE sent_at>$3", connection, tx))
                {
                    limit.Parameters.AddWithValue(email); limit.Parameters.AddWithValue(ih); limit.Parameters.AddWithValue(now.AddHours(-1)); await using var reader = await limit.ExecuteReaderAsync(); await reader.ReadAsync(); if (reader.GetInt64(0) >= 5 || reader.GetInt64(1) >= 100) throw new VerificationLimit(3600);
                }
                if (name is not null) { await using var profile = new NpgsqlCommand("INSERT INTO profiles(id,name,normalized_name,password_hash,avatar,email,security_stamp,email_confirmed) VALUES($1,$2,$3,NULL,$4,$5,$6,false) ON CONFLICT(email) DO NOTHING", connection, tx); profile.Parameters.AddWithValue(Guid.NewGuid()); profile.Parameters.AddWithValue(name); profile.Parameters.AddWithValue(name.ToUpperInvariant()); profile.Parameters.AddWithValue(avatar!); profile.Parameters.AddWithValue(email); profile.Parameters.AddWithValue(Guid.NewGuid().ToString("N")); await profile.ExecuteNonQueryAsync(); }
                await using (var challenge = new NpgsqlCommand("INSERT INTO email_challenges VALUES($1,$2,$3,$3,0) ON CONFLICT(email) DO UPDATE SET code_hash=$2,expires_at=$3,next_send_at=$3,attempts=0", connection, tx)) { challenge.Parameters.AddWithValue(email); challenge.Parameters.AddWithValue(Hash(email + ":" + code)); challenge.Parameters.AddWithValue(now.AddSeconds(120)); await challenge.ExecuteNonQueryAsync(); }
                await using (var log = new NpgsqlCommand("INSERT INTO email_deliveries VALUES($1,$2,$3)", connection, tx)) { log.Parameters.AddWithValue(email); log.Parameters.AddWithValue(ih); log.Parameters.AddWithValue(now); await log.ExecuteNonQueryAsync(); }
                await tx.CommitAsync();
            }
            try { await sender.Send(email, code); return (email, true); } catch (Exception ex) when (ex is SmtpException or TimeoutException or System.Net.Sockets.SocketException) { return (email, false); }
        });
    }
    public async Task<string?> Verify(string email, string code)
    {
        return await StorageErrors.Run<string?>(async () =>
        {
            email = Normalize(email); code = NormalizeCode(code); if (code.Length != 6 || !code.All(char.IsAsciiDigit)) return null; await database.Initialize(); await using var connection = await source.OpenConnectionAsync(); await using var tx = await connection.BeginTransactionAsync(); await Lock(connection, tx, "email:" + email); var now = await DatabaseTime(connection, tx);
            string expected; DateTime expires; int attempts;
            await using (var read = new NpgsqlCommand("SELECT code_hash,expires_at,attempts FROM email_challenges WHERE email=$1", connection, tx)) { read.Parameters.AddWithValue(email); await using var reader = await read.ExecuteReaderAsync(); if (!await reader.ReadAsync()) return null; expected = reader.GetString(0); expires = reader.GetDateTime(1); attempts = reader.GetInt32(2); }
            if (expected.Length == 0 || expires <= now || attempts >= 5) return null;
            if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expected), Convert.FromHexString(Hash(email + ":" + code)))) { await using var fail = new NpgsqlCommand("UPDATE email_challenges SET attempts=attempts+1 WHERE email=$1", connection, tx); fail.Parameters.AddWithValue(email); await fail.ExecuteNonQueryAsync(); await tx.CommitAsync(); return null; }
            Guid? profileId;
            await using (var confirm = new NpgsqlCommand("UPDATE profiles SET email_confirmed=true WHERE email=$1 RETURNING id", connection, tx)) { confirm.Parameters.AddWithValue(email); profileId = await confirm.ExecuteScalarAsync() as Guid?; }
            await using (var consume = new NpgsqlCommand("UPDATE email_challenges SET code_hash='' WHERE email=$1", connection, tx)) { consume.Parameters.AddWithValue(email); await consume.ExecuteNonQueryAsync(); }
            if (profileId is null) { await tx.CommitAsync(); throw new ArgumentException("اول ساخت حساب رو از صفحهٔ ثبت‌نام شروع کن."); }
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); await using (var grant = new NpgsqlCommand("INSERT INTO password_grants(token_hash,profile_id,expires_at) VALUES($1,$2,$3)", connection, tx)) { grant.Parameters.AddWithValue(Hash(token)); grant.Parameters.AddWithValue(profileId.Value); grant.Parameters.AddWithValue(now.AddMinutes(10)); await grant.ExecuteNonQueryAsync(); }
            await tx.CommitAsync(); return token;
        });
    }
    public async Task<(bool Initial, Player Player)?> SetPassword(string token, string password)
    {
        return await StorageErrors.Run<(bool Initial, Player Player)?>(async () =>
        {
            AccountPasswords.Validate(password); if (token.Length != 64) return null; await database.Initialize(); var hash = Hash(token); await using var connection = await source.OpenConnectionAsync(); await using var tx = await connection.BeginTransactionAsync(); await Lock(connection, tx, "grant:" + hash);
            string email; Guid id;
            await using (var grant = new NpgsqlCommand("SELECT p.id,p.email FROM password_grants g JOIN profiles p ON p.id=g.profile_id WHERE g.token_hash=$1 AND NOT g.used AND g.expires_at>clock_timestamp()", connection, tx)) { grant.Parameters.AddWithValue(hash); await using var reader = await grant.ExecuteReaderAsync(); if (!await reader.ReadAsync()) return null; id = reader.GetGuid(0); email = reader.GetString(1); }
            await Lock(connection, tx, "email:" + email);
            await using (var consume = new NpgsqlCommand("UPDATE password_grants SET used=true WHERE token_hash=$1 AND NOT used AND expires_at>clock_timestamp() RETURNING token_hash", connection, tx)) { consume.Parameters.AddWithValue(hash); if (await consume.ExecuteScalarAsync() is null) return null; }
            Player player; bool initial;
            await using (var profile = new NpgsqlCommand("SELECT name,avatar,password_hash IS NULL FROM profiles WHERE id=$1 FOR UPDATE", connection, tx)) { profile.Parameters.AddWithValue(id); await using var reader = await profile.ExecuteReaderAsync(); if (!await reader.ReadAsync()) return null; player = new(id, reader.GetString(0), reader.GetString(1), Guid.NewGuid().ToString("N")); initial = reader.GetBoolean(2); }
            await using (var update = new NpgsqlCommand("UPDATE profiles SET password_hash=$1,security_stamp=$2 WHERE id=$3", connection, tx)) { update.Parameters.AddWithValue(AccountPasswords.Hasher.HashPassword(player, password)); update.Parameters.AddWithValue(player.Stamp); update.Parameters.AddWithValue(id); await update.ExecuteNonQueryAsync(); }
            await AccountTransaction.RevokeCredentials(connection, tx, id);
            await using (var clear = new NpgsqlCommand("UPDATE login_failures SET email_hash='' WHERE email_hash=$1", connection, tx)) { clear.Parameters.AddWithValue(Hash(email)); await clear.ExecuteNonQueryAsync(); }
            await tx.CommitAsync(); return (initial, player);
        });
    }
    public async Task<int> Remaining(string email) { return await StorageErrors.Run<int>(async () => { await database.Initialize(); await using var query = source.CreateCommand("SELECT GREATEST(0,ceil(extract(epoch FROM (deadline-clock_timestamp()))))::integer FROM (" + NextSendSql + ") AS remaining(deadline)"); query.Parameters.AddWithValue(Normalize(email)); return (int)(await query.ExecuteScalarAsync())!; }); }
    public async Task<bool> HasGrant(string token) { return await StorageErrors.Run<bool>(async () => { if (token.Length != 64) return false; await database.Initialize(); await using var query = source.CreateCommand("SELECT EXISTS(SELECT 1 FROM password_grants WHERE token_hash=$1 AND NOT used AND expires_at>clock_timestamp())"); query.Parameters.AddWithValue(Hash(token)); return (bool)(await query.ExecuteScalarAsync())!; }); }
}
