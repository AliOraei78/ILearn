namespace ILearn.Domain.Accounts;

public static class PasswordPolicy
{
    public static void Validate(string password)
    {
        var runes = password.EnumerateRunes().ToArray();
        if (runes.Length is < 6 or > 128 || !runes.Any(System.Text.Rune.IsLetter) || !runes.Any(System.Text.Rune.IsDigit) || !runes.Any(r => System.Text.Rune.IsPunctuation(r) || System.Text.Rune.IsSymbol(r))) throw new ArgumentException("رمز باید ۶ تا ۱۲۸ نویسه و شامل حرف، عدد و نماد باشه.");
    }

}