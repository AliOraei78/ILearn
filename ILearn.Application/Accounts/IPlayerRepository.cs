namespace ILearn.Application.Accounts;

public interface IPlayerRepository
{
    Task<Player> Profile(Guid owner);
    Task Avatar(Guid owner, string avatar);
    Task<Progress> Load(Guid owner);
    Task Save(Guid owner, Progress progress);
}
