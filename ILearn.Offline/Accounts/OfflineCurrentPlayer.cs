namespace ILearn.Offline.Accounts;

public sealed class OfflineCurrentPlayer : ICurrentPlayer
{
    internal static readonly Guid Owner = Guid.Parse("11111111-1111-4111-8111-111111111111");
    public Task<SessionIdentity> Get() => Task.FromResult(new SessionIdentity(Owner, Owner, "offline"));
}
