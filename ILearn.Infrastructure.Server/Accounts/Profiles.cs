using Npgsql;
using Microsoft.AspNetCore.Identity;
namespace ILearn.Infrastructure.Accounts;

public sealed partial class PostgresAccounts
{
    public async Task<Player> UpdateProfile(SessionIdentity principal, string name, string currentPassword, string newPassword)
    {
        return await StorageErrors.Run<Player>(async () =>
        {
            name = name.Trim();
            if (name.Length is < 1 or > 50 || currentPassword.Length > 256) throw new ArgumentException("نام یا طول رمز معتبر نیست.");
            if (newPassword.Length > 0) AccountPasswords.Validate(newPassword);
            await using var connection = await source.OpenConnectionAsync(); await using var transaction = await connection.BeginTransactionAsync();
            var id = principal.PlayerId;
            string hash, avatar, stamp;
            await using (var read = new NpgsqlCommand("SELECT password_hash,avatar,security_stamp FROM profiles WHERE id=$1 FOR UPDATE", connection, transaction))
            {
                read.Parameters.AddWithValue(id); await using var reader = await read.ExecuteReaderAsync(); if (!await reader.ReadAsync()) throw new UnauthorizedAccessException();
                hash = reader.GetString(0); avatar = reader.GetString(1); stamp = reader.GetString(2);
            }
            var player = new Player(id, name, avatar, stamp);
            if (stamp != principal.Stamp || AccountPasswords.Hasher.VerifyHashedPassword(player, hash, currentPassword) == PasswordVerificationResult.Failed) throw new ArgumentException("رمز فعلی درست نیست.");
            if (newPassword.Length > 0)
            {
                hash = AccountPasswords.Hasher.HashPassword(player, newPassword); stamp = Guid.NewGuid().ToString("N");
                await RevokeCredentials(connection, transaction, id);
            }
            await using var update = new NpgsqlCommand("UPDATE profiles SET name=$1,normalized_name=$2,password_hash=$3,security_stamp=$4 WHERE id=$5", connection, transaction);
            update.Parameters.AddWithValue(name); update.Parameters.AddWithValue(name.ToUpperInvariant()); update.Parameters.AddWithValue(hash); update.Parameters.AddWithValue(stamp); update.Parameters.AddWithValue(id);
            await update.ExecuteNonQueryAsync(); await transaction.CommitAsync(); return player with { Stamp = stamp };
        });
    }
}
