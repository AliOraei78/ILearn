using ILearn.Accounts;
namespace ILearn.Tests;

public class AccountNavigationTests
{
    [Theory]
    [InlineData(null, "/lobby")]
    [InlineData("/profile", "/profile")]
    [InlineData("/lobby?lesson=S08&activity=3", "/lobby?lesson=S08&activity=3")]
    [InlineData("/code-room?subject=redis&template=predict", "/code-room?subject=redis&template=predict")]
    [InlineData("/interview", "/interview")]
    [InlineData("/interview?subject=csharp&question=C017", "/interview?subject=csharp&question=C017")]
    [InlineData("/code-room/../login", "/lobby")]
    [InlineData("/interview\\example.com", "/lobby")]
    [InlineData("https://example.com/profile", "/lobby")]
    [InlineData("//example.com/profile", "/lobby")]
    [InlineData("/lobby\\example.com", "/lobby")]
    [InlineData("/profile\r\nLocation: https://example.com", "/lobby")]
    [InlineData("/login", "/lobby")]
    public void ReturnDestinationIsLimitedToLocalApplicationPages(string? input, string expected)
        => Assert.Equal(expected, AccountEndpoints.ReturnDestination(input));

    [Theory]
    [InlineData("۰۱۲۳۴۵", "012345")]
    [InlineData("٠١٢٣٤٥", "012345")]
    [InlineData("۰١2۳۴۵", "012345")]
    [InlineData("123456", "123456")]
    [InlineData("12345x", "12345x")]
    public void VerificationAcceptsLocalDigitsWithoutAcceptingOtherCharacters(string input, string expected)
        => Assert.Equal(expected, ILearn.Infrastructure.Accounts.EmailVerification.NormalizeCode(input));
}
