namespace ILearn.Domain.Accounts;

public static class PasswordPolicy
{
    public static void Validate(string password)
    {
        if (password.Length > 256) throw new ArgumentException("رمز باید حداکثر ۱۲۸ نویسه باشد.");
        var runes = password.EnumerateRunes().ToArray();
        if (runes.Length is < 15 or > 128) throw new ArgumentException("رمز باید ۱۵ تا ۱۲۸ نویسه باشد؛ یک عبارت طولانی و غیرقابل‌حدس انتخاب کن.");
        if (runes.Distinct().Count() == 1 || Common.Contains(password)) throw new ArgumentException("این رمز رایج یا قابل‌حدس است؛ یک عبارت متفاوت انتخاب کن.");
    }

    static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "passwordpassword", "password123456789", "123456789012345", "1234567890123456",
        "qwertyuiopasdfgh", "qwertyuiop123456", "iloveyouiloveyou", "ilearn1234567890"
    };

}
