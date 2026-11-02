namespace ILearn.Application.Accounts;

public interface IEmailSender { Task Send(string email, string code); }
