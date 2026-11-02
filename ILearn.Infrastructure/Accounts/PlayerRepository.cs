using Npgsql;
using System.Text.Json;
namespace ILearn.Infrastructure.Accounts;

public sealed class PlayerRepository(NpgsqlDataSource source) : IPlayerRepository
{
    readonly SemaphoreSlim gate = new(1, 1);
    public async Task<Player> Profile(Guid owner)
    {
        return await StorageErrors.Run<Player>(async () =>
        {
            await using var command = source.CreateCommand("SELECT name,avatar FROM profiles WHERE id=$1");
            var id = owner; command.Parameters.AddWithValue(id);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw new UnauthorizedAccessException();
            return new(id, reader.GetString(0), reader.GetString(1));
        });
    }
    public async Task Avatar(Guid owner, string avatar)
    {
        await StorageErrors.Run(async () =>
        {
            if (!Enumerable.Range(1, 48).Any(i => avatar == $"avatar-{i:00}")) throw new ArgumentException();
            await using var command = source.CreateCommand("UPDATE profiles SET avatar=$1 WHERE id=$2");
            command.Parameters.AddWithValue(avatar); command.Parameters.AddWithValue(owner);
            await command.ExecuteNonQueryAsync();
        });
    }
    public async Task<Progress> Load(Guid owner)
    {
        return await StorageErrors.Run<Progress>(async () =>
        {
            var id = owner;
            await using var insert = source.CreateCommand("INSERT INTO progress(profile_id,data) VALUES($1,'{}') ON CONFLICT DO NOTHING");
            insert.Parameters.AddWithValue(id); await insert.ExecuteNonQueryAsync();
            await using var command = source.CreateCommand("SELECT data::text,revision FROM progress WHERE profile_id=$1");
            command.Parameters.AddWithValue(id);
            await using var reader = await command.ExecuteReaderAsync(); await reader.ReadAsync();
            var result = JsonSerializer.Deserialize<Progress>(reader.GetString(0)) ?? throw new InvalidDataException();
            result.Revision = reader.GetInt64(1); return result;
        });
    }
    public async Task Save(Guid owner, Progress progress)
    {
        await StorageErrors.Run(async () =>
        {
            await gate.WaitAsync();
            try
            {
                await using var command = source.CreateCommand("UPDATE progress SET data=$1::jsonb,revision=revision+1 WHERE profile_id=$2 AND revision=$3 RETURNING revision");
                command.Parameters.AddWithValue(JsonSerializer.Serialize(progress));
                command.Parameters.AddWithValue(owner); command.Parameters.AddWithValue(progress.Revision);
                var revision = await command.ExecuteScalarAsync();
                if (revision is null) throw new InvalidOperationException("سیوت توی تب دیگه تغییر کرده. نوشته‌ات اینجا باقی مونده؛ قبل از بستن صفحه نگهش دار.");
                progress.Revision = (long)revision;
            }
            finally { gate.Release(); }
        });
    }
}
