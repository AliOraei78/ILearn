using System.Net;
using ILearn.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ILearn.Tests;

public sealed class WebSecurityTests
{
    [Fact]
    public void SecurityTestsRunOnPatchedRuntime() => Assert.True(Environment.Version >= new Version(10, 0, 14), "Run security tests with .NET 10.0.14 or newer.");

    [Theory]
    [InlineData(false, false, "localhost", 400, false)]
    [InlineData(false, true, "localhost", 200, false)]
    [InlineData(false, true, "public.example", 200, false)]
    [InlineData(false, true, "public.example", 400, true)]
    [InlineData(true, false, "localhost", 200, false)]
    [InlineData(true, false, "127.0.0.1", 200, false)]
    [InlineData(true, false, "public.example", 400, false)]
    public async Task RequiresHttpsExceptForExplicitLocalMode(bool localHttp, bool forwardedHttps, string host, int status, bool untrusted)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration["Security:AllowLocalHttp"] = localHttp.ToString();
        builder.Configuration["AllowedHosts"] = "*";
        builder.AddWebSecurity();
        await using var app = builder.Build();
        if (untrusted) app.Use((context, next) => { context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10"); return next(context); });
        app.UseWebSecurity();
        app.MapGet("/", (HttpContext context) => Results.Content($"<html><script nonce=\"{context.Items["CspNonce"]}\"></script></html>", "text/html"));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Headers.Host = host;
        if (forwardedHttps) request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await client.SendAsync(request);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.False(response.Headers.Contains("Server"));
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("'unsafe-inline'; script", csp);
        if (status == 200)
        {
            if (forwardedHttps && host == "public.example") Assert.True(response.Headers.Contains("Strict-Transport-Security"));
            Assert.True(response.Headers.CacheControl!.NoStore);
            var html = await response.Content.ReadAsStringAsync();
            var nonce = html.Split("nonce=\"")[1].Split('"')[0];
            Assert.Contains($"'nonce-{nonce}'", csp);
            using var next = await client.GetAsync(address);
            Assert.NotEqual(csp, next.Headers.GetValues("Content-Security-Policy").Single());
        }
        await app.StopAsync();
    }
}
