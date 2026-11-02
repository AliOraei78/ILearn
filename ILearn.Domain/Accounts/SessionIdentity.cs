namespace ILearn.Domain.Accounts;

public sealed record SessionIdentity(Guid PlayerId, Guid SessionId, string Stamp);