using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using ILearn.Application.Accounts;
using ILearn.Configuration;
using Microsoft.Extensions.Configuration;

namespace ILearn.Tests;

public sealed class ReviewerSecurityTests
{
    static IConfiguration Config(string? url = null) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    { ["OpenRouter:ApiKey"] = "synthetic-test-key", ["OpenRouter:Url"] = url }).Build();

    sealed class Reply(Func<CancellationToken, Task<HttpResponseMessage>> action) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) { Calls++; return action(ct); }
    }
    static HttpResponseMessage Response(string content) => new(HttpStatusCode.OK)
    { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } })) };

    [Theory]
    [InlineData("http://openrouter.ai/api/v1/chat/completions")]
    [InlineData("https://example.test/api/v1/chat/completions")]
    [InlineData("https://openrouter.ai.evil.test/api/v1/chat/completions")]
    [InlineData("https://user@openrouter.ai/api/v1/chat/completions")]
    [InlineData("https://openrouter.ai:444/api/v1/chat/completions")]
    [InlineData("https://openrouter.ai/api/v1/chat/completions?next=evil")]
    public async Task NeverSendsCredentialsToOtherEndpoints(string url)
    {
        var handler = new Reply(_ => Task.FromResult(Response("متن")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new Reviewer(new(handler), Config(url)).Answer(new(), default));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task RejectsHugeDraftBeforeSending()
    {
        var handler = new Reply(_ => Task.FromResult(Response("متن")));
        await Assert.ThrowsAsync<ArgumentException>(() => new Reviewer(new(handler), Config()).Get(new(), new string('x', 16001), default));
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData("summary")]
    [InlineData("issues")]
    [InlineData("nextHint")]
    [InlineData("nullIssue")]
    public async Task RejectsMalformedOrOversizedFeedback(string field)
    {
        var content = JsonSerializer.Serialize(new { score = 100, summary = field == "summary" ? new string('ف', 501) : "درست", issues = field == "nullIssue" ? new string?[] { null } : new[] { new string('ف', field == "issues" ? 251 : 1) }, nextHint = new string('ف', field == "nextHint" ? 301 : 1) });
        var handler = new Reply(_ => Task.FromResult(Response(content)));
        await Assert.ThrowsAsync<InvalidDataException>(() => new Reviewer(new(handler), Config()).Get(new(), "test", default));
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RejectsLargeResponsesWithOrWithoutContentLength(bool knownLength)
    {
        var handler = new Reply(_ =>
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = knownLength ? new ByteArrayContent(new byte[65537]) : new StreamContent(new Chunked(new byte[65537])) });
        });
        await Assert.ThrowsAsync<InvalidDataException>(() => new Reviewer(new(handler), Config()).Answer(new(), default));
    }

    sealed class Chunked(byte[] bytes) : MemoryStream(bytes) { public override bool CanSeek => false; }

    sealed class Identity(Guid id) : ICurrentPlayer
    {
        public bool Revoked;
        public Task<SessionIdentity> Get() => Revoked ? throw new UnauthorizedAccessException() : Task.FromResult(new SessionIdentity(id, id, "stamp"));
    }

    [Fact]
    public async Task RevokedSessionsNeverReachTheProvider()
    {
        var handler = new Reply(_ => Task.FromResult(Response("متن")));
        using var limiter = PartitionedRateLimiter.Create<Guid, Guid>(id => RateLimitPartition.GetNoLimiter(id));
        var reviewer = new WebReviewer(new Identity(Guid.NewGuid()) { Revoked = true }, new(new(handler), Config()), limiter);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reviewer.Answer(new(), default));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task LimitsAreSharedAcrossCircuitsAndSeparateBetweenPlayers()
    {
        var release = new TaskCompletionSource();
        var handler = new Reply(async ct => { await release.Task.WaitAsync(ct); return Response("پاسخ فارسی"); });
        using var limiter = PartitionedRateLimiter.Create<Guid, Guid>(id => RateLimitPartition.GetConcurrencyLimiter(id, _ => new() { PermitLimit = 1, QueueLimit = 0 }));
        var identity = new Identity(Guid.NewGuid());
        var first = new WebReviewer(identity, new(new(handler), Config()), limiter);
        var second = new WebReviewer(identity, new(new(handler), Config()), limiter);
        var other = new WebReviewer(new Identity(Guid.NewGuid()), new(new(handler), Config()), limiter);
        var pending = first.Answer(new(), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => second.Answer(new(), default));
        var otherPending = other.Answer(new(), default);
        Assert.Equal(2, handler.Calls);
        release.SetResult();
        await Task.WhenAll(pending, otherPending);
        await second.Answer(new(), default);
    }
}
