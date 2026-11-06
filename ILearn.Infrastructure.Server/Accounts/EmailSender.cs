using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
namespace ILearn.Infrastructure.Accounts;

public sealed class EmailSender(IConfiguration config, IWebHostEnvironment environment) : IEmailSender
{
    public async Task Send(string email, string code)
    {
        if (string.IsNullOrWhiteSpace(config["EmailSettings:Host"]) || !MailAddress.TryCreate(config["EmailSettings:FromEmail"], out var from)) throw new SmtpException("SMTP is not configured.");
        using var smtp = new SmtpClient(config["EmailSettings:Host"], config.GetValue("EmailSettings:Port", 587)) { EnableSsl = config.GetValue("EmailSettings:EnableSsl", true), Credentials = new NetworkCredential(config["EmailSettings:UserName"], config["EmailSettings:Password"]) };
        using var message = new MailMessage { From = new MailAddress(from.Address, "ILearn"), Subject = "کد تأیید ایمیلت در ILearn", SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8 };
        var html = (await File.ReadAllTextAsync(Path.Combine(environment.ContentRootPath, "Content", "VerificationEmail.html"))).Replace("{{code}}", WebUtility.HtmlEncode(code));
        var text = $"سلام قهرمان!\nبرای تأیید ایمیل و ساخت یا بازیابی رمز ILearn، این کد را در صفحهٔ بازی وارد کن:\n{code}\nاین کد تا ۲ دقیقه معتبر است و فقط یک بار استفاده می‌شود.\nاگر این درخواست را انجام ندادی، نیازی به اقدامی نیست. کد را در اختیار دیگران نگذار.";
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, Encoding.UTF8, MediaTypeNames.Text.Plain)); message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, Encoding.UTF8, MediaTypeNames.Text.Html)); message.To.Add(email); await smtp.SendMailAsync(message).WaitAsync(TimeSpan.FromSeconds(8));
    }
}
