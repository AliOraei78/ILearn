using Npgsql;
namespace ILearn.Infrastructure.Accounts;

public sealed partial class PostgresAccounts(NpgsqlDataSource source, IConfiguration config, SchemaInitializer schema) : IAccounts { public Task Initialize() => schema.Initialize(); }