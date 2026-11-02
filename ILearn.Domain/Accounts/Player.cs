namespace ILearn.Domain.Accounts;

public sealed record Player(Guid Id, string Name, string Avatar, string Stamp = "");
