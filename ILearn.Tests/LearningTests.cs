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
        await new Reviewer(new HttpClient(handler), new ConfigurationBuilder().Build()).Get(activity, "پاسخ کامل", CancellationToken.None, new Lesson { Id = "S05", Title = "متدها" });
        using var body = JsonDocument.Parse(handler.Body);
        using var context = JsonDocument.Parse(body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
        Assert.Equal(activity.Prompt, context.RootElement.GetProperty("question").GetString());
        Assert.Equal(activity.Code, context.RootElement.GetProperty("code").GetString());
        Assert.Equal(activity.Explanation, context.RootElement.GetProperty("rubric").GetString());
        Assert.Equal("پاسخ کامل", context.RootElement.GetProperty("studentAnswer").GetString());
        Assert.Equal("S05", context.RootElement.GetProperty("lessonId").GetString());
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"score\":-1}")]
    [InlineData("{\"score\":101}")]
    [InlineData("{\"score\":70.5}")]
    public async Task InvalidGradeIsRetriedAndRejected(string content)
    {
        var handler = new CaptureHandler(content);
        await Assert.ThrowsAsync<InvalidDataException>(() => new Reviewer(new HttpClient(handler), new ConfigurationBuilder().Build()).Get(new Activity(), "text", CancellationToken.None));
        Assert.Equal(2, handler.Calls);
    }
    sealed class CaptureHandler(string? content = null) : HttpMessageHandler
    {
        public string Body = ""; public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++; Body = await request.Content!.ReadAsStringAsync(token);
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { message = new { content = content ?? JsonSerializer.Serialize(new Review(100, "درست است", [], "ادامه")) } }), Encoding.UTF8, "application/json") };
        }
    }
    static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn"));
    static List<Lesson> Lessons => JsonSerializer.Deserialize<List<Lesson>>(File.ReadAllText(Path.Combine(Root, "Content", "lessons.json")))!;

    [Fact]
    public void CurriculumHasFifteenDistinctGamesAndThreeCompleteSessions()
    {
        Assert.Equal(44, File.ReadAllLines(Path.Combine(Root, "Content", "catalog.txt")).Length);
        Assert.Equal(15, Lessons.Where(l => l.Id != "S08").SelectMany(l => l.Activities.Take(5)).Select(a => a.Id).Distinct().Count());
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
        var game = Lessons[2].Activities[4];
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
        var reviewer = new Reviewer(new HttpClient(handler), new ConfigurationBuilder().Build());
        var result = await reviewer.Get(Lessons[0].Activities[5], "answer", CancellationToken.None);
        Assert.True(result.sufficient);
        Assert.Equal(retry ? 2 : 1, handler.Calls);
    }
    [Fact]
    public async Task ReviewerHonorsCancellation()
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new Reviewer(new HttpClient(new ReplyHandler(false)), new ConfigurationBuilder().Build()).Get(Lessons[0].Activities[5], "text", cancellation.Token));
    }
    sealed class ReplyHandler(bool malformedFirst) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested(); Calls++;
            var content = malformedFirst && Calls == 1 ? "broken" : JsonSerializer.Serialize(new Review(100, "خوب بود", [], "ادامه بده"));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { message = new { content } }), Encoding.UTF8, "application/json") });
        }
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("English only")]
    public async Task ReviewerRejectsMissingOrNonPersianSummary(string? summary)
    {
        var handler = new FixedReplyHandler(summary, HttpStatusCode.OK);
        var reviewer = new Reviewer(new HttpClient(handler), new ConfigurationBuilder().Build());
        await Assert.ThrowsAsync<InvalidDataException>(() => reviewer.Get(Lessons[0].Activities[5], "text", CancellationToken.None));
        Assert.Equal(2, handler.Calls);
    }
    [Fact]
    public async Task ReviewerSurfacesServiceFailure()
    {
        var reviewer = new Reviewer(new HttpClient(new FixedReplyHandler("متن", HttpStatusCode.ServiceUnavailable)), new ConfigurationBuilder().Build());
        await Assert.ThrowsAsync<HttpRequestException>(() => reviewer.Get(Lessons[0].Activities[5], "text", CancellationToken.None));
    }
    sealed class FixedReplyHandler(string? summary, HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var content = JsonSerializer.Serialize(new { score = 100, summary, issues = Array.Empty<string>(), nextHint = "ادامه" });
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(JsonSerializer.Serialize(new { message = new { content } }), Encoding.UTF8, "application/json") });
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
