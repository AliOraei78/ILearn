using Npgsql;
namespace ILearn.Infrastructure.Accounts;

internal static class StorageErrors
{
    internal static async Task<T> Run<T>(Func<Task<T>> action) { try { return await action(); } catch (PostgresException ex) when (ex.SqlState == "23505") { throw new DuplicateAccountException(ex); } catch (NpgsqlException ex) { throw new AccountStorageException("Database operation failed.", ex); } }
    internal static async Task Run(Func<Task> action) => await Run(async () => { await action(); return true; });
}
