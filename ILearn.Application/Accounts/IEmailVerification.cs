namespace ILearn.Application.Accounts;

public interface IEmailVerification
{
    Task<(string Email, bool Delivered)> Start(string email, string? name, string? avatar, string ip);
    Task<string?> Verify(string email, string code);
    Task<(bool Initial, Player Player)?> SetPassword(string token, string password);
    Task<int> Remaining(string email);
    Task<bool> HasGrant(string token);
}
