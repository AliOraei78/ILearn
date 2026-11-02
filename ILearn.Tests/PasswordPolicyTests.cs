namespace ILearn.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Abcd1!")]
    [InlineData("سلام۱!")]
    [InlineData("Long-test-password-12!")]
    public void AcceptsRightPixelPasswordPolicy(string password) => AccountPasswords.Validate(password);
    [Theory]
    [InlineData("abc1!")]
    [InlineData("abcdef")]
    [InlineData("Abcdef!")]
    [InlineData("Abcde12")]
    public void RejectsMissingPolicyParts(string password) => Assert.Throws<ArgumentException>(() => AccountPasswords.Validate(password));
}
