using System.Net;
using ILearn.Components.Learning;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ILearn.Tests;

public class LearningTextTests
{
    static async Task<string> Render(string text)
    {
        await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
        return await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<LearningText>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Text"] = text }))).ToHtmlString());
    }

    [Fact]
    public async Task PersianProseAndLatinFragmentsKeepIndependentDirections()
    {
        var html = WebUtility.HtmlDecode(await Render("تغییر Console به console در C# درست نیست."));
        Assert.Contains("dir=\"rtl\"", html);
        Assert.Contains("<bdi dir=\"ltr\">Console</bdi>", html);
        Assert.Contains("<bdi dir=\"ltr\">C#</bdi>", html);
    }

    [Fact]
    public async Task IdentifiersAndArithmeticKeepTheirLogicalOrder()
    {
        var html = WebUtility.HtmlDecode(await Render("سؤال C001-03 و حاصل 9 - 2 و مقدار -1 را بررسی کن."));
        Assert.Contains("<bdi dir=\"ltr\">C001-03</bdi>", html);
        Assert.Contains("<bdi dir=\"ltr\">9 - 2</bdi>", html);
        Assert.Contains("<bdi dir=\"ltr\">-1</bdi>", html);
    }

    [Fact]
    public async Task PercentagesStayTogetherAndUsePersianDigits()
    {
        var html = WebUtility.HtmlDecode(await Render("درستی پاسخ: 100٪"));
        Assert.Contains("<bdi dir=\"ltr\">۱۰۰٪</bdi>", html);
    }

    [Fact]
    public async Task FencedCodeIsLtrAndUntrustedMarkupIsNeverExecuted()
    {
        var html = await Render("توضیح\n```csharp\nConsole.WriteLine(4 + 6);\n```\n<img src=x onerror=alert(1)>");
        Assert.Contains("<pre tabindex=\"0\" dir=\"ltr\"><code>Console.WriteLine(4 + 6);</code></pre>", WebUtility.HtmlDecode(html));
        Assert.DoesNotContain("<img", html);
        Assert.Contains("&lt;", html);
    }
}
