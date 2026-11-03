using System.Net;
using System.Text;
using System.Text.Json;
using ILearn.Application.Learning;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace ILearn.Tests;

public sealed class InterviewTests
{
    static Curriculum Load() => new(new TestEnvironment());

    [Fact]
    public void CSharpHasOneHundredDistinctAuthoredQuestionsWithSelectionAndTechnicalSources()
    {
        var bank = Assert.Single(Load().Interviews);
        Assert.Equal("csharp", bank.Id);
        Assert.Equal("C#", bank.Title);
        Assert.Equal("2026-11-03", bank.ReviewedOn);
        Assert.Contains("گزینش تحریری", bank.SelectionNote);
        Assert.Equal(Enumerable.Range(1, 100).Select(i => $"C{i:000}"), bank.Questions.Select(q => q.Activity.Id));
        Assert.Equal(100, bank.Questions.Select(q => q.Activity.Title).Distinct().Count());
        Assert.Equal(100, bank.Questions.Select(q => q.Activity.Prompt).Distinct().Count());
        Assert.Equal(10, bank.Questions.Select(q => q.Category).Distinct().Count());
        Assert.All(bank.Questions.GroupBy(q => q.Category), group => Assert.Equal(10, group.Count()));
        Assert.Equal(new[] { "متوسط", "مقدماتی", "پیشرفته" }.Order(), bank.Questions.Select(q => q.Level).Distinct().Order());
        Assert.All(bank.Questions, question =>
        {
            var activity = question.Activity;
            Assert.Equal("explain", activity.Kind);
            Assert.True(activity.Prompt.Length >= 70, activity.Id);
            Assert.True(activity.Solution.Length >= 170, activity.Id);
            Assert.Equal(3, activity.Explanation.Split('\n').Length);
            Assert.NotEmpty(activity.Hint);
            Assert.Empty(activity.Answers);
            Assert.Empty(activity.Options);
            Assert.DoesNotContain("به‌زودی", activity.Prompt + activity.Solution);
            Assert.All(question.SourceIds, id => Assert.Contains(bank.Sources, source => source.Id == id));
            var references = bank.Sources.Where(source => question.SourceIds.Contains(source.Id)).ToList();
            Assert.Contains(references, source => source.Role == "selection" && new Uri(source.Url).Host == "www.linkedin.com");
            Assert.Contains(references, source => source.Role == "verification" && new Uri(source.Url).Host is "learn.microsoft.com" or "devblogs.microsoft.com");
        });
    }

    [Fact]
    public void InterviewDraftVerdictAndSelectionSurviveWithoutChangingOtherWork()
    {
        var content = Load(); var question = content.Interviews[0].Questions[0];
        var progress = new Progress { Lesson = "R008", Position = 2, CodeRoomExercise = "code-room/csharp/R001/workshop", InterviewQuestion = question.Key("csharp") };
        progress.Entries["csharp/R008/explain"] = new Entry { Draft = "academy" };
        progress.Entries[progress.CodeRoomExercise] = new Entry { Draft = "room" };
        progress.Entries[progress.InterviewQuestion] = new Entry { Draft = "مصاحبه", Complete = true, Source = "بررسی مدل", Feedback = "درست؛ 100٪" };
        var restored = JsonSerializer.Deserialize<Progress>(JsonSerializer.Serialize(progress))!;
        Assert.Equal("interview/csharp/C001", restored.InterviewQuestion);
        Assert.Equal("مصاحبه", restored.Entries[restored.InterviewQuestion].Draft);
        Assert.True(restored.Entries[restored.InterviewQuestion].Complete);
        Assert.Equal("درست؛ 100٪", restored.Entries[restored.InterviewQuestion].Feedback);
        Assert.Equal("academy", restored.Entries["csharp/R008/explain"].Draft);
        Assert.Equal("room", restored.Entries[restored.CodeRoomExercise].Draft);
        Assert.Equal("R008", restored.Lesson); Assert.Equal(2, restored.Position);
        Assert.Equal(0, LearningService.Xp(content.Lessons, restored, content.CodeExercises));
        Assert.Empty(JsonSerializer.Deserialize<Progress>("""{"Lesson":"R001","Entries":{}}""")!.InterviewQuestion);
    }

    [Fact]
    public async Task EveryInterviewReviewReceivesItsOwnQuestionRubricAndReference()
    {
        var handler = new CaptureHandler();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenRouter:ApiKey"] = "interview-fixture" }).Build();
        var reviewer = new Reviewer(new HttpClient(handler), config);
        foreach (var question in Load().Interviews[0].Questions)
        {
            var activity = question.Activity;
            var review = await reviewer.Get(activity, "پاسخ داوطلب", CancellationToken.None, technology: "C#", template: "interview");
            Assert.True(review.sufficient);
            using var body = JsonDocument.Parse(handler.Body);
            using var context = JsonDocument.Parse(body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
            var data = context.RootElement;
            Assert.Equal(activity.Id, data.GetProperty("activityId").GetString());
            Assert.Equal(activity.Prompt, data.GetProperty("question").GetString());
            Assert.Equal(activity.Code, data.GetProperty("code").GetString());
            Assert.Equal(activity.Explanation, data.GetProperty("rubric").GetString());
            Assert.Equal(activity.Solution, data.GetProperty("referenceAnswer").GetString());
            Assert.Equal("پاسخ داوطلب", data.GetProperty("studentAnswer").GetString());
            Assert.Equal("interview", data.GetProperty("template").GetString());
            Assert.Contains("مصاحبهٔ تشریحی", data.GetProperty("templateDescription").GetString());
        }
        Assert.Equal(100, handler.Calls);
    }

    sealed class CaptureHandler : HttpMessageHandler
    {
        public string Body = ""; public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++; Body = await request.Content!.ReadAsStringAsync(ct);
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = JsonSerializer.Serialize(new Review(100, "پاسخ با معیار سازگار است.", [], "ادامه بده.")) } } } }), Encoding.UTF8, "application/json") };
        }
    }
    sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn"));
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
