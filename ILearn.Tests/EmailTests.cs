namespace ILearn.Tests;

public class EmailTests
{
    [Theory]
    [InlineData(" TEST@Example.com ", "test@example.com")]
    [InlineData("a+b@example.com", "a+b@example.com")]
    public void NormalizeEmail(string input, string expected) => Assert.Equal(expected, EmailVerification.Normalize(input));
    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("User <a@example.com>")]
    [InlineData("a@example.com\r\nBcc: victim@example.com")]
    public void RejectInvalidEmail(string input) => Assert.Throws<ArgumentException>(() => EmailVerification.Normalize(input));
}
