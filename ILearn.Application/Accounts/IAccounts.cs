namespace ILearn.Application.Accounts;

public interface IAccounts
{
    Task<Player> UpdateProfile(SessionIdentity principal, string name, string currentPassword, string newPassword);
    Task<Guid> CreateSession(Guid profileId);
    Task RevokeSession(SessionIdentity principal);
    Task<bool> ValidSession(SessionIdentity principal);
    Task<Player?> Login(string identifier, string password, string ip);
}
