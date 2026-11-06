namespace ILearn.Application.Accounts;

public interface ICurrentPlayer
{
    Task<SessionIdentity> Get();
}
