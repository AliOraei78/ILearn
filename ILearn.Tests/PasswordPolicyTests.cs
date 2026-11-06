namespace ILearn.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("a long private phrase")]
    [InlineData("یک عبارت بلند و شخصی")]
    [InlineData("Long-test-password-12!")]
    public void AcceptsLongPassphrases(string password) => AccountPasswords.Validate(password);
    [Theory]
    [InlineData("abc1!")]
    [InlineData("abcdef")]
    [InlineData("Abcdef!")]
    [InlineData("Abcde12")]
    [InlineData("passwordpassword")]
    [InlineData("123456789012345")]
    [InlineData("aaaaaaaaaaaaaaa")]
    public void RejectsShortOrPredictablePasswords(string password) => Assert.Throws<ArgumentException>(() => AccountPasswords.Validate(password));

    [Fact]
    public void CountsUnicodeCodePointsAndRejectsOversizedInput()
    {
        AccountPasswords.Validate(string.Concat(Enumerable.Repeat("🦊", 14)) + "x");
        Assert.Throws<ArgumentException>(() => AccountPasswords.Validate(string.Concat(Enumerable.Repeat("🦊", 7)) + "x"));
        AccountPasswords.Validate(new string('a', 127) + "b");
        Assert.Throws<ArgumentException>(() => AccountPasswords.Validate(new string('a', 128) + "b"));
    }
}
