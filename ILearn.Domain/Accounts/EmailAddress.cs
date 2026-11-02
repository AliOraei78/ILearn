using System.Net.Mail;
namespace ILearn.Domain.Accounts;

public static class EmailAddress
{
    public static string Normalize(string value) { value = value.Trim(); if (value.Length > 254 || !MailAddress.TryCreate(value, out var address) || address.Address != value) throw new ArgumentException("یک ایمیل معتبر وارد کن."); return value.ToLowerInvariant(); }
}