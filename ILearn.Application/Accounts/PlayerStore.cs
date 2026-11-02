namespace ILearn.Application.Accounts;

public sealed class PlayerStore(IPlayerRepository repository, ICurrentPlayer current)
{
    public async Task<Player> Profile() => await repository.Profile((await current.Get()).PlayerId);
    public async Task Avatar(string avatar) => await repository.Avatar((await current.Get()).PlayerId, avatar);
    public async Task<Progress> Load() => await repository.Load((await current.Get()).PlayerId);
    public async Task Save(Progress progress) => await repository.Save((await current.Get()).PlayerId, progress);
}