using System.Collections.Concurrent;
using System.Security.Cryptography;
using ILearn.Accounts;
using ILearn.Application.Accounts;
using ILearn.Domain.Accounts;
using ILearn.Domain.Progress;
using ILearn.Infrastructure.Accounts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Security.Claims;

var connection = Environment.GetEnvironmentVariable("ILEARN_TEST_CONNECTION") ?? throw new InvalidOperationException("Set ILEARN_TEST_CONNECTION.");
var settings = new NpgsqlConnectionStringBuilder(connection);
if (settings.Database != "ilearn_security_qa" || settings.Host != "127.0.0.1") throw new InvalidOperationException("Only the isolated local security QA database is allowed.");
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:CodeKey"] = Environment.GetEnvironmentVariable("Auth__CodeKey") ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) }).Build();
await using var source = NpgsqlDataSource.Create(connection);
var schema = new SchemaInitializer(source);
await schema.Initialize();
var accounts = new PostgresAccounts(source, config, schema);
var sender = new MailFixture();
var verification = new EmailVerification(source, schema, config, sender);
const string password = "Quiet river birds sing 93!", replacement = "Bright mountain trail 42!";
var checks = new List<string>();
var runId = Guid.NewGuid().ToString("N");
string Email(string name) => name + "-" + runId + "@example.test";
void Check(bool valid, string name) { if (!valid) throw new Exception(name); checks.Add(name); Console.WriteLine("PASS " + name); }
async Task Reject<T>(Func<Task> action, string name) where T : Exception
{ try { await action(); } catch (T) { Check(true, name); return; } throw new Exception(name); }
async Task Sql(string sql, string email)
{ await using var cmd = source.CreateCommand(sql); cmd.Parameters.AddWithValue(email); await cmd.ExecuteNonQueryAsync(); }
async Task<long> Count(string sql, string email)
{ await using var cmd = source.CreateCommand(sql); cmd.Parameters.AddWithValue(email); return (long)(await cmd.ExecuteScalarAsync())!; }
async Task<Player> Register(string email, string name)
{
    await verification.Start(email, name, "avatar-01", "security-fixture");
    var token = await verification.Verify(email, sender.Codes[email]) ?? throw new Exception("No grant");
    return (await verification.SetPassword(token, password))!.Value.Player;
}

await using (var privileges = source.CreateCommand("SELECT rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls FROM pg_roles WHERE rolname=current_user"))
    Check(!(bool)(await privileges.ExecuteScalarAsync())!, "Application database role has no server administration privileges");

var proofEmail = "proof-" + Guid.NewGuid().ToString("N") + "@example.test";
await verification.Start(proofEmail, "Pending QA", "avatar-07", "proof-test");
Check(await Count("SELECT count(*) FROM profiles WHERE email=$1", proofEmail) == 0, "Unverified registration cannot reserve a permanent profile");
var proofGrant = await verification.Verify(proofEmail, sender.Codes[proofEmail]);
Check(proofGrant is not null && await Count("SELECT count(*) FROM profiles WHERE email=$1 AND email_confirmed AND avatar='avatar-07'", proofEmail) == 1 && await Count("SELECT count(*) FROM pending_signups WHERE email=$1", proofEmail) == 0, "Verified email creates profile and consumes pending registration atomically");
var first = await Register(Email("security-player"), "<img src=x onerror=alert(1)>");
var second = await Register(Email("second-player"), "Second QA player");
Check(await accounts.Login(Email("security-player") + "' OR '1'='1", password, "sql-test") is null, "SQL injection does not authenticate");
Check(await accounts.Login(Email("security-player"), "wrong", "login-test") is null, "Wrong password rejected");
Check((await accounts.Login(Email("security-player"), password, "login-test"))?.Id == first.Id, "Valid credentials authenticate");
await using (var cmd = source.CreateCommand("SELECT password_hash FROM profiles WHERE id=$1"))
{
    cmd.Parameters.AddWithValue(first.Id);
    var hash = (string)(await cmd.ExecuteScalarAsync())!;
    Check(hash != password && AccountPasswords.Hasher.VerifyHashedPassword(first, hash, password) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed, "Passwords stored as verifiable salted hashes");
}
var repo = new PlayerRepository(source);
var save = await repo.Load(first.Id); save.Entries["csharp/R001/test"] = new() { Draft = "private first-player draft" }; await repo.Save(first.Id, save);
Check((await repo.Load(second.Id)).Entries.Count == 0, "Progress isolated between accounts");
var stale = await repo.Load(first.Id); var fresh = await repo.Load(first.Id); await repo.Save(first.Id, fresh);
await Reject<InvalidOperationException>(() => repo.Save(first.Id, stale), "Stale saves rejected without overwriting data");
var session = new SessionIdentity(first.Id, await accounts.CreateSession(first.Id), first.Stamp);
Check(await accounts.ValidSession(session), "Fresh session valid");
var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, first.Id.ToString()), new Claim("session", session.SessionId.ToString()), new Claim("stamp", first.Stamp) }, "Cookies"));
var current = new CurrentPlayer(new State(principal), accounts);
Check((await new PlayerStore(repo, current).Load()).Entries.ContainsKey("csharp/R001/test"), "PlayerStore uses verified session owner");
await accounts.RevokeSession(session);
await Reject<UnauthorizedAccessException>(() => current.Get(), "Revoked live circuit rejected on the next store operation");
Check(!await accounts.ValidSession(session with { PlayerId = second.Id }), "Session cannot be replayed for another owner");
session = new(first.Id, await accounts.CreateSession(first.Id), first.Stamp);
await using (var cmd = source.CreateCommand("UPDATE player_sessions SET last_seen_at=clock_timestamp()-interval '25 hours' WHERE id=$1")) { cmd.Parameters.AddWithValue(session.SessionId); await cmd.ExecuteNonQueryAsync(); }
Check(!await accounts.ValidSession(session), "Idle session expires after 24 hours");
session = new(first.Id, await accounts.CreateSession(first.Id), first.Stamp);
await using (var cmd = source.CreateCommand("UPDATE player_sessions SET created_at=clock_timestamp()-interval '8 days' WHERE id=$1")) { cmd.Parameters.AddWithValue(session.SessionId); await cmd.ExecuteNonQueryAsync(); }
Check(!await accounts.ValidSession(session), "Absolute session expires after seven days");

await verification.Start(Email("blocked"), "Blocked QA", "avatar-01", "code-test");
var correct = sender.Codes[Email("blocked")];
for (var i = 0; i < 5; i++) Check(await verification.Verify(Email("blocked"), correct == "000000" ? "111111" : "000000") is null, "Incorrect OTP rejected " + (i + 1));
Check(await verification.Verify(Email("blocked"), correct) is null, "Correct OTP rejected after five failed guesses");
await verification.Start(Email("expired"), "Expired QA", "avatar-01", "code-test");
await Sql("UPDATE email_challenges SET expires_at=clock_timestamp()-interval '1 second' WHERE email=$1", Email("expired"));
Check(await verification.Verify(Email("expired"), sender.Codes[Email("expired")]) is null, "Expired OTP rejected");
async Task<bool> SendParallel() { try { await verification.Start(Email("parallel"), "Parallel QA", "avatar-01", "race-test"); return true; } catch (VerificationLimit) { return false; } }
Check((await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => SendParallel()))).Count(result => result) == 1 && sender.Sent[Email("parallel")] == 1, "Parallel email requests send exactly once");
var grant = await verification.Verify(Email("parallel"), sender.Codes[Email("parallel")]);
Check(grant is not null && await verification.Verify(Email("parallel"), sender.Codes[Email("parallel")]) is null, "OTP consumed once");
async Task<bool> SetParallel() => await verification.SetPassword(grant!, password) is not null;
Check((await Task.WhenAll(SetParallel(), SetParallel())).Count(result => result) == 1, "Password grant consumed once under concurrency");
await verification.Start(Email("expired-grant"), "Grant QA", "avatar-01", "grant-test");
var expiredGrant = await verification.Verify(Email("expired-grant"), sender.Codes[Email("expired-grant")]);
await Sql("UPDATE password_grants SET expires_at=clock_timestamp()-interval '1 second' WHERE profile_id=(SELECT id FROM profiles WHERE email=$1)", Email("expired-grant"));
Check(!await verification.HasGrant(expiredGrant!) && await verification.SetPassword(expiredGrant!, password) is null, "Expired password grant rejected");
for (var i = 0; i < 5; i++) await accounts.Login(Email("second-player"), "wrong", "lockout-test");
await Reject<VerificationLimit>(() => accounts.Login(Email("second-player"), password, "lockout-test"), "Account lockout enforced after five wrong passwords");

await Sql("DELETE FROM email_deliveries WHERE email=$1", Email("security-player"));
await Sql("UPDATE email_challenges SET next_send_at=clock_timestamp()-interval '1 second' WHERE email=$1", Email("security-player"));
session = new(first.Id, await accounts.CreateSession(first.Id), first.Stamp);
await verification.Start(Email("security-player"), null, null, "reset-test");
var resetGrant = await verification.Verify(Email("security-player"), sender.Codes[Email("security-player")]);
var reset = await verification.SetPassword(resetGrant!, replacement);
Check(reset is { Initial: false } && !await accounts.ValidSession(session), "Password recovery revokes existing sessions");
Check(await accounts.Login(Email("security-player"), password, "reset-test") is null && (await accounts.Login(Email("security-player"), replacement, "reset-test"))?.Id == first.Id, "Recovery replaces the old password");
var updated = reset!.Value.Player;
var changedSession = new SessionIdentity(updated.Id, await accounts.CreateSession(updated.Id), updated.Stamp);
await Reject<ArgumentException>(() => accounts.UpdateProfile(changedSession, "QA", "wrong", password), "Profile changes require the current password");
await accounts.UpdateProfile(changedSession, "<img src=x onerror=alert(1)>", replacement, password);
Check(!await accounts.ValidSession(changedSession), "Profile password change revokes existing sessions");
Check((await repo.Load(first.Id)).Entries["csharp/R001/test"].Draft == "private first-player draft", "Credential changes preserve private progress");
var retention = "retention-" + Guid.NewGuid().ToString("N");
await Sql("INSERT INTO login_failures VALUES($1,'retention-test',clock_timestamp()-interval '2 days'),($1,'retention-test',clock_timestamp())", retention);
await Sql("INSERT INTO email_deliveries VALUES($1,'retention-test',clock_timestamp()-interval '2 days'),($1,'retention-test',clock_timestamp())", retention + "@example.test");
await Sql("INSERT INTO pending_signups(email,name,password_hash,code_hash,expires_at,next_send_at) VALUES($1,'expired fixture','','',clock_timestamp()-interval '1 day',clock_timestamp()-interval '1 day')", retention + "@example.test");
using var cleanup = new CredentialCleanup(source, schema, Microsoft.Extensions.Logging.Abstractions.NullLogger<CredentialCleanup>.Instance);
await cleanup.Clean(default);
Check(await Count("SELECT count(*) FROM login_failures WHERE email_hash=$1", retention) == 1 && await Count("SELECT count(*) FROM email_deliveries WHERE email=$1", retention + "@example.test") == 1, "Cleanup removes old authentication logs and preserves active rate-limit windows");
Check(await Count("SELECT count(*) FROM pending_signups WHERE email=$1", retention + "@example.test") == 0 && (await repo.Load(first.Id)).Entries["csharp/R001/test"].Draft == "private first-player draft", "Expired registration cleanup never deletes profiles or progress");
foreach (var (fixtureEmail, fixturePassword) in new[] { ("security-player@example.test", password), ("legacy-player@example.test", "Abcd1!") })
{
await using var fixture = source.CreateCommand("""
    INSERT INTO profiles(id,name,normalized_name,password_hash,avatar,email,security_stamp,email_confirmed)
    VALUES($1,$2,$3,$4,'avatar-01',$6,$5,true)
    ON CONFLICT(email) DO UPDATE SET name=$2,normalized_name=$3,password_hash=$4,security_stamp=$5,email_confirmed=true
    """);
    var player = new Player(Guid.NewGuid(), "<img src=x onerror=alert(1)>", "avatar-01", Guid.NewGuid().ToString("N"));
    fixture.Parameters.AddWithValue(player.Id); fixture.Parameters.AddWithValue(player.Name); fixture.Parameters.AddWithValue(player.Name.ToUpperInvariant());
    fixture.Parameters.AddWithValue(AccountPasswords.Hasher.HashPassword(player, fixturePassword)); fixture.Parameters.AddWithValue(player.Stamp); fixture.Parameters.AddWithValue(fixtureEmail);
    await fixture.ExecuteNonQueryAsync();
}
Console.WriteLine($"PASS {checks.Count} security database checks; synthetic browser account ready.");

sealed class MailFixture : IEmailSender
{
    public ConcurrentDictionary<string, string> Codes = new();
    public ConcurrentDictionary<string, int> Sent = new();
    public Task Send(string email, string code) { Codes[email] = code; Sent.AddOrUpdate(email, 1, (_, n) => n + 1); return Task.CompletedTask; }
}
sealed class State(ClaimsPrincipal principal) : AuthenticationStateProvider
{ public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal)); }
