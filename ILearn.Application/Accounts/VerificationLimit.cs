namespace ILearn.Application.Accounts;

public sealed class VerificationLimit(int seconds) : Exception("برای تلاش دوباره کمی صبر کن.") { public int Seconds { get; } = seconds; }
