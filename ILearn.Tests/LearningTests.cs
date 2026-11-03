using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
namespace ILearn.Tests;

public sealed class LearningTests
{
    [Theory]
    [InlineData(0, "غلط")]
    [InlineData(69, "غلط")]
    [InlineData(70, "تقریباً درست")]
    [InlineData(99, "تقریباً درست")]
    [InlineData(100, "درست")]
    public void GradeStartsFeedback(int score, string verdict)
    {
        var review = new Review(score, "خلاصه", [], "راهنما");
        Assert.Equal(verdict, review.Verdict);
        Assert.StartsWith(verdict + "؛ برآورد درستی پاسخ: " + score + "٪", review.Feedback);
    }
    [Fact]
    public void OldProgressKeepsWorkAndSkipsIntroOnlyForStartedLesson()
    {
        var progress = JsonSerializer.Deserialize<Progress>("""{"Version":1,"Lesson":"S05","Position":7,"Entries":{"csharp/S05/workshop":{"Draft":"code","Complete":true}}}""")!;
        Assert.True(progress.HasStarted("S05"));
        Assert.False(progress.HasStarted("S06"));
        Assert.Equal(7, progress.Position);
        Assert.True(new Progress { Lesson = "S05", Position = 4 }.HasStarted("S05"));
        Assert.False(new Progress().HasStarted("S05"));
        progress.IntroductionsRead.Add("csharp/S06");
        Assert.True(progress.HasStarted("S06"));
    }
    [Fact]
    public void MiniGameHistoryAndMarksCountAsStartedWork()
    {
        var progress = new Progress();
        progress.Entries["csharp/S08/concept"] = new Entry { GameState = new MiniGameState { History = [0] } };
        Assert.True(progress.HasStarted("S08"));
        Assert.False(progress.HasStarted("S07"));
        progress.Entries.Clear();
        progress.Entries["csharp/S08/deduction"] = new Entry { GameState = new MiniGameState { Marks = new() { ["0:0"] = 1 } } };
        Assert.True(progress.HasStarted("S08"));
    }
    [Fact]
    public async Task ReviewerSendsFullContext()
    {
        var activity = new Activity { Id = "sample", Title = "عنوان", Kind = "workshop", Prompt = "صورت کامل سؤال", Code = "int x = 0;", Explanation = "معیار دقیق" };
        var handler = new CaptureHandler();
        await new Reviewer(new HttpClient(handler), ReviewerConfig).Get(activity, "پاسخ کامل", CancellationToken.None, new Lesson { Id = "S05", Title = "متدها" });
        using var body = JsonDocument.Parse(handler.Body);
        using var context = JsonDocument.Parse(body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
        Assert.Equal(activity.Prompt, context.RootElement.GetProperty("question").GetString());
        Assert.Equal(activity.Code, context.RootElement.GetProperty("code").GetString());
        Assert.Equal(activity.Explanation, context.RootElement.GetProperty("rubric").GetString());
        Assert.Equal("پاسخ کامل", context.RootElement.GetProperty("studentAnswer").GetString());
        Assert.Equal("S05", context.RootElement.GetProperty("lessonId").GetString());
    }
    [Theory]
    [InlineData("PostgreSQL", "optimize")]
    [InlineData("Redis", "predict")]
    public async Task ReviewerReceivesTechnologyTemplateAndReferenceWithoutImposingCSharp(string technology, string template)
    {
        var activity = new Activity { Prompt = "صورت مسئله", Explanation = "معیار", Solution = "پاسخ مرجع", SolutionCode = "reference" };
        var handler = new CaptureHandler();
        await new Reviewer(new HttpClient(handler), ReviewerConfig).Get(activity, "پاسخ", CancellationToken.None, technology: technology, template: template);
        using var body = JsonDocument.Parse(handler.Body);
        var messages = body.RootElement.GetProperty("messages");
        Assert.DoesNotContain("تو مدرس دقیق C#", messages[0].GetProperty("content").GetString());
        using var context = JsonDocument.Parse(messages[1].GetProperty("content").GetString()!);
        Assert.Equal(technology, context.RootElement.GetProperty("technology").GetString());
        Assert.Equal(template, context.RootElement.GetProperty("template").GetString());
        Assert.Equal(CodeRoomTemplates.Find(template)!.Description, context.RootElement.GetProperty("templateDescription").GetString());
        Assert.Equal(activity.Solution, context.RootElement.GetProperty("referenceAnswer").GetString());
        Assert.Equal(activity.SolutionCode, context.RootElement.GetProperty("referenceCode").GetString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"score\":-1}")]
    [InlineData("{\"score\":101}")]
    [InlineData("{\"score\":70.5}")]
    public async Task InvalidGradeIsRetriedAndRejected(string content)
    {
        var handler = new CaptureHandler(content);
        await Assert.ThrowsAsync<InvalidDataException>(() => new Reviewer(new HttpClient(handler), ReviewerConfig).Get(new Activity(), "text", CancellationToken.None));
        Assert.Equal(2, handler.Calls);
    }
    sealed class CaptureHandler(string? content = null) : HttpMessageHandler
    {
        public string Body = ""; public int Calls; public Uri? Url; public string? Authorization;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Body = await request.Content!.ReadAsStringAsync(token); Url = request.RequestUri; Authorization = request.Headers.Authorization?.ToString();
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = content ?? JsonSerializer.Serialize(new Review(100, "درست است", [], "ادامه")) } } } }), Encoding.UTF8, "application/json") };
        }
    }
    static readonly IConfiguration ReviewerConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["OpenRouter:ApiKey"] = "fixture-review-key",
        ["OpenRouter:Model"] = "unexpected-model"
    }).Build();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothModelActionsUseOnlyTheFixedFreeModel(bool explain)
    {
        var handler = new CaptureHandler(explain ? "این توضیح پاسخ مرجع است." : null);
        var reviewer = new Reviewer(new HttpClient(handler), ReviewerConfig);
        var activity = new Activity { Prompt = "سؤال", Solution = "پاسخ مرجع", SolutionCode = "int x = 0;", ReferenceFiles=new(){["Program.cs"]="Console.WriteLine(3);"} };
        if (explain) Assert.Equal("این توضیح پاسخ مرجع است.", await reviewer.Answer(activity, CancellationToken.None));
        else Assert.Equal(100, (await reviewer.Get(activity, "پاسخ", CancellationToken.None)).score);
        Assert.Equal(1, handler.Calls);
        Assert.Equal("https://openrouter.ai/api/v1/chat/completions", handler.Url?.ToString());
        Assert.Equal("Bearer fixture-review-key", handler.Authorization);
        Assert.DoesNotContain("fixture-review-key", handler.Body);
        using var body = JsonDocument.Parse(handler.Body);
        var payload = body.RootElement;
        Assert.Equal("nvidia/nemotron-3.5-super-vl:free", payload.GetProperty("model").GetString());
        Assert.False(payload.TryGetProperty("models", out _));
        Assert.False(payload.TryGetProperty("route", out _));
        Assert.False(payload.GetProperty("provider").GetProperty("allow_fallbacks").GetBoolean());
        foreach (var price in payload.GetProperty("provider").GetProperty("max_price").EnumerateObject()) Assert.Equal(0, price.Value.GetInt32());
        Assert.False(payload.GetProperty("reasoning").GetProperty("enabled").GetBoolean());
        if (explain)
        {
            Assert.False(payload.TryGetProperty("response_format", out _));
            using var context = JsonDocument.Parse(payload.GetProperty("messages")[1].GetProperty("content").GetString()!);
            Assert.Equal(activity.Solution, context.RootElement.GetProperty("answer").GetString());
            Assert.Equal(activity.SolutionCode, context.RootElement.GetProperty("code").GetString());
            Assert.Equal(activity.ReferenceFiles["Program.cs"],context.RootElement.GetProperty("referenceFiles").GetProperty("Program.cs").GetString());
        }
        else
        {
            var format = payload.GetProperty("response_format");
            Assert.Equal("json_schema", format.GetProperty("type").GetString());
            Assert.True(format.GetProperty("json_schema").GetProperty("strict").GetBoolean());
            Assert.False(format.GetProperty("json_schema").GetProperty("schema").GetProperty("additionalProperties").GetBoolean());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingApiKeyNeverSendsAModelRequest(bool explain)
    {
        var handler = new CaptureHandler();
        var reviewer = new Reviewer(new HttpClient(handler), new ConfigurationBuilder().Build());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            if (explain) await reviewer.Answer(new Activity(), CancellationToken.None);
            else await reviewer.Get(new Activity(), "پاسخ", CancellationToken.None);
        });
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("English only")]
    public async Task ExplanationRejectsEmptyOrNonPersianReplies(string content)
    {
        var handler = new CaptureHandler(content);
        await Assert.ThrowsAsync<InvalidDataException>(() => new Reviewer(new HttpClient(handler), ReviewerConfig).Answer(new Activity(), CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }
    static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn"));
    static List<Lesson> Lessons => JsonSerializer.Deserialize<List<Lesson>>(File.ReadAllText(Path.Combine(Root, "Content", "lessons.json")))!;

    [Fact]
    public void CurriculumHasTenCompleteSessions()
    {
        Assert.Equal(44, File.ReadAllLines(Path.Combine(Root, "Content", "catalog.txt")).Length);
        Assert.Equal(Enumerable.Range(1, 10).Select(n => $"S{n:00}"), Lessons.Select(l => l.Id));
        foreach (var lesson in Lessons)
        {
            Assert.Equal(8, lesson.Activities.Count);
            Assert.Equal(100, lesson.Activities.Sum(a => a.Xp));
            Assert.Equal(lesson.Activities.Count, lesson.Activities.Select(a => a.Id).Distinct().Count());
            Assert.All(lesson.Activities, a => Assert.False(string.IsNullOrWhiteSpace(a.Explanation)));
            foreach (var game in lesson.Activities.Where(a => !a.IsAi && a.Game is null))
            {
                Assert.NotEmpty(game.Answers);
                Assert.True(game.Check(game.Answers));
                Assert.False(game.Check([]));
                if (game.Kind is "slots" or "trace") Assert.Equal(game.Rows.Length, game.Answers.Length);
                foreach (var key in game.Answers)
                {
                    int option = int.Parse(key.Contains(':') ? key.Split(':')[1] : key);
                    Assert.InRange(option, 0, game.Options.Length - 1);
                }
            }
        }
    }
    [Fact]
    public void MultiSelectRequiresExactSetAndIgnoresSelectionOrder()
    {
        var game = Lessons.Single(l => l.Id == "S07").Activities.Single(a => a.Id == "domino");
        Assert.True(game.Check(["2", "0"]));
        Assert.False(game.Check(["0"]));
        Assert.False(game.Check(["0", "1", "2"]));
    }
    [Fact]
    public void CompiledExamplesMatchAuthoredAnswers()
    {
        int n = 4; Add(ref n); Assert.Equal(7, n);
        static void Add(ref int x) => x += 3;
        BasePolicy a = new Vip(); Vip b = new();
        Assert.Equal("Vip", a.Name());
        Assert.Equal("Vip", b.Name());
        BasePolicy hidden = new HiddenVip();
        Assert.Equal("Base", hidden.Name());
        Assert.Equal("Vip", new HiddenVip().Name());
        Assert.True(0 >= 0);
    }
    class BasePolicy { public virtual string Name() => "Base"; }
    sealed class Vip : BasePolicy { public override string Name() => "Vip"; }
    sealed class HiddenVip : BasePolicy { public new string Name() => "Vip"; }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReviewerParsesResponseAndRetriesMalformedJson(bool retry)
    {
        var handler = new ReplyHandler(retry);
        var reviewer = new Reviewer(new HttpClient(handler), ReviewerConfig);
        var result = await reviewer.Get(Lessons[0].Activities[5], "answer", CancellationToken.None);
        Assert.True(result.sufficient);
        Assert.Equal(retry ? 2 : 1, handler.Calls);
    }
    [Fact]
    public async Task ReviewerHonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new Reviewer(new HttpClient(new ReplyHandler(false)), ReviewerConfig).Get(Lessons[0].Activities[5], "text", cancellation.Token));
    }
    sealed class ReplyHandler(bool malformedFirst) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); Calls++;
            var content = malformedFirst && Calls == 1 ? "broken" : JsonSerializer.Serialize(new Review(100, "خوب بود", [], "ادامه بده"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } }), Encoding.UTF8, "application/json") });
        }
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("English only")]
    public async Task ReviewerRejectsMissingOrNonPersianSummary(string? summary)
    {
        var handler = new FixedReplyHandler(summary, HttpStatusCode.OK);
        var reviewer = new Reviewer(new HttpClient(handler), ReviewerConfig);
        await Assert.ThrowsAsync<InvalidDataException>(() => reviewer.Get(Lessons[0].Activities[5], "text", CancellationToken.None));
        Assert.Equal(2, handler.Calls);
    }
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.PaymentRequired)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ReviewerSurfacesServiceFailureWithoutFallback(HttpStatusCode status)
    {
        var handler = new FixedReplyHandler("متن", status);
        var reviewer = new Reviewer(new HttpClient(handler), ReviewerConfig);
        await Assert.ThrowsAsync<HttpRequestException>(() => reviewer.Get(Lessons[0].Activities[5], "text", CancellationToken.None));
        Assert.Equal(1, handler.Calls);
    }
    sealed class FixedReplyHandler(string? summary, HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var content = JsonSerializer.Serialize(new { score = 100, summary, issues = Array.Empty<string>(), nextHint = "ادامه" });
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } }), Encoding.UTF8, "application/json") });
        }
    }
    sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = "";
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
