using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
namespace ILearn.Infrastructure.Accounts;

public static class AccountPasswords
{
    public static readonly PasswordHasher<Player> Hasher = new(Options.Create(new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3, IterationCount = 220000 }));
    internal static readonly Player Dummy = new(Guid.Empty, "", "");
    internal static readonly string DummyHash = Hasher.HashPassword(Dummy, "dummy-password-for-timing-only");
    public static void Validate(string password) => PasswordPolicy.Validate(password);
}