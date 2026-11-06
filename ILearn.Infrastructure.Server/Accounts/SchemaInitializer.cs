using Npgsql;
using Microsoft.AspNetCore.Identity;
namespace ILearn.Infrastructure.Accounts;

public sealed class SchemaInitializer(NpgsqlDataSource source)
{
    readonly SemaphoreSlim initialization = new(1, 1);
    bool initialized;
    public async Task Initialize()
    {
        await initialization.WaitAsync();
        try
        {
            if (initialized) return;
            await using var schemaConnection = await source.OpenConnectionAsync();
            await using var schemaTransaction = await schemaConnection.BeginTransactionAsync();
            await using (var schemaLock = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended('ILearn.Schema',0))", schemaConnection, schemaTransaction)) { await schemaLock.ExecuteNonQueryAsync(); }
            await using var command = new NpgsqlCommand("""
            CREATE TABLE IF NOT EXISTS profiles (
              id uuid PRIMARY KEY, name text NOT NULL, normalized_name text UNIQUE NOT NULL,
              password_hash text NOT NULL, avatar text NOT NULL);
            CREATE TABLE IF NOT EXISTS progress (
              profile_id uuid PRIMARY KEY REFERENCES profiles(id), data jsonb NOT NULL,
              revision bigint NOT NULL DEFAULT 0);
            ALTER TABLE profiles ADD COLUMN IF NOT EXISTS email text;
            ALTER TABLE profiles ADD COLUMN IF NOT EXISTS security_stamp text NOT NULL DEFAULT '';
            CREATE UNIQUE INDEX IF NOT EXISTS profiles_email ON profiles(email);
            CREATE TABLE IF NOT EXISTS pending_signups (
              email text PRIMARY KEY, name text NOT NULL, password_hash text NOT NULL,
              code_hash text NOT NULL, expires_at timestamptz NOT NULL,
              next_send_at timestamptz NOT NULL, attempts integer NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS email_deliveries (
              email text NOT NULL, ip_hash text NOT NULL, sent_at timestamptz NOT NULL);
            CREATE INDEX IF NOT EXISTS email_deliveries_sent ON email_deliveries(sent_at);
            CREATE INDEX IF NOT EXISTS email_deliveries_email_sent ON email_deliveries(email,sent_at);
            CREATE TABLE IF NOT EXISTS password_resets (
              email text PRIMARY KEY, code_hash text NOT NULL, original_stamp text NOT NULL,
              expires_at timestamptz NOT NULL,next_send_at timestamptz NOT NULL,attempts integer NOT NULL DEFAULT 0);
            ALTER TABLE profiles ALTER COLUMN password_hash DROP NOT NULL;
            ALTER TABLE profiles ADD COLUMN IF NOT EXISTS email_confirmed boolean NOT NULL DEFAULT false;
            ALTER TABLE profiles DROP CONSTRAINT IF EXISTS profiles_normalized_name_key;
            CREATE UNIQUE INDEX IF NOT EXISTS profiles_legacy_name ON profiles(normalized_name) WHERE email IS NULL;
            UPDATE profiles SET email_confirmed=true WHERE password_hash IS NOT NULL AND email IS NOT NULL AND NOT email_confirmed;
            CREATE TABLE IF NOT EXISTS email_challenges (
              email text PRIMARY KEY, code_hash text NOT NULL, expires_at timestamptz NOT NULL,
              next_send_at timestamptz NOT NULL,attempts integer NOT NULL DEFAULT 0);
            ALTER TABLE pending_signups ADD COLUMN IF NOT EXISTS avatar text NOT NULL DEFAULT 'avatar-01';
            CREATE TABLE IF NOT EXISTS player_sessions (
              id uuid PRIMARY KEY,profile_id uuid NOT NULL REFERENCES profiles(id),
              created_at timestamptz NOT NULL,last_seen_at timestamptz NOT NULL,revoked boolean NOT NULL DEFAULT false);
            CREATE INDEX IF NOT EXISTS player_sessions_profile ON player_sessions(profile_id);
            CREATE TABLE IF NOT EXISTS password_grants (
              token_hash text PRIMARY KEY,profile_id uuid NOT NULL REFERENCES profiles(id),
              expires_at timestamptz NOT NULL,used boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS login_failures (
              email_hash text NOT NULL,ip_hash text NOT NULL,at timestamptz NOT NULL);
            CREATE INDEX IF NOT EXISTS login_failures_at ON login_failures(at);
            """, schemaConnection, schemaTransaction);
            await command.ExecuteNonQueryAsync();
            await schemaTransaction.CommitAsync();
            initialized = true;
        }
        finally { initialization.Release(); }
    }
}
