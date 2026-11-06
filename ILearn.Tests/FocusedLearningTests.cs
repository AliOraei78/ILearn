using System.Net;
using System.Text.Json;
using ILearn.Application.Learning;
using ILearn.Application.Reviewing;
using Microsoft.Extensions.Configuration;

namespace ILearn.Tests;

public class FocusedLearningTests
{
    static readonly string ContentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn/Content"));
    static CurriculumData Content() => new(name => File.ReadAllText(Path.Combine(ContentRoot, name)));
    static Reviewer Create(HttpMessageHandler handler) => new(new(handler), new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["OpenRouter:ApiKey"] = "fixture-key" }).Build());

    [Fact]
    public async Task AllAuthoredCompleteReferencesReceiveFullCreditWithoutAModel()
    {
        var content = Content();
        var reviewer = Create(new RejectNetwork());
        var activities = content.Lessons.SelectMany(l => l.Activities).Where(a => a.IsAi)
            .Concat(content.Interviews.SelectMany(s => s.Questions).Select(q => q.Activity))
            .Concat(content.CodeExercises.Select(e => e.Activity)).Distinct();
        foreach (var activity in activities)
        {
            Assert.Contains(activity.AnswerFormat, new[] { "code", "text" });
            var draft = activity.AnswerFormat == "code" ? activity.SolutionCode : activity.Solution;
            Assert.NotEmpty(draft);
            var result = await reviewer.Get(activity, "\r\n" + draft.Replace("\n", "\r\n") + "\r\n", default);
            Assert.Equal(100, result.score);
            Assert.True(result.ReferenceMatch, activity.Id);
            Assert.Equal("تطبیق پاسخ مرجع", result.Source);
        }
    }

    [Fact]
    public void APrimaryFileAloneCannotMatchAMultiFileReference()
    {
        var activity = new Activity
        {
            AnswerFormat = "code",
            SolutionCode = "Main();",
            ReferenceFiles = new() { ["Library.cs"] = "void Main() { }" }
        };
        Assert.False(activity.MatchesReference(activity.SolutionCode));
    }

    [Fact]
    public async Task AcademyAndMultiFileReviewAlwaysReceiveTheCompleteReference()
    {
        var activity = new Activity { Id = "stage", Prompt = "فقط قرارداد این مرحله", Kind = "workshop", AnswerFormat = "code", Explanation = "معیار همین مرحله", Solution = "پاسخ مرجع", SolutionCode = "reference code", ReferenceFiles = new() { ["Library.cs"] = "library reference", ["Tests.cs"] = "test reference" } };
        var handler = new Capture();
        var result = await Create(handler).Get(activity, "different valid answer", default);
        Assert.False(result.ReferenceMatch);
        using var context = JsonDocument.Parse(handler.Context);
        Assert.Equal(activity.Solution, context.RootElement.GetProperty("referenceAnswer").GetString());
        Assert.Equal(activity.SolutionCode, context.RootElement.GetProperty("referenceCode").GetString());
        Assert.Equal(2, context.RootElement.GetProperty("referenceFiles").EnumerateObject().Count());
        Assert.Contains("نمی‌تواند شرطی فراتر", context.RootElement.GetProperty("instruction").GetString());
    }

    [Fact]
    public async Task PartialOrDifferentReferenceStillGoesToTheReviewer()
    {
        var activity = Content().Lessons.Single(l => l.Id == "R002").Activities.Single(a => a.Id == "workshop");
        var handler = new Capture();
        await Create(handler).Get(activity, "Console.WriteLine(9);", default);
        Assert.Equal(1, handler.Calls);
        Assert.Contains("Console.WriteLine(9);", handler.Context);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => Create(new RejectNetwork()).Get(activity, activity.SolutionCode, cancellation.Token));
    }

    [Fact]
    public void ChoiceControlsNeverAskForWrittenExplanationsAndStagesStaySmall()
    {
        var content = Content();
        foreach (var activity in content.Lessons.SelectMany(l => l.Activities).Where(a => a.Kind == "choice"))
        {
            Assert.DoesNotContain("توضیح بده", activity.Prompt);
            Assert.DoesNotMatch(@"بنویس(?![\u0600-\u06ff])", activity.Prompt);
        }
        var syntax = content.Lessons.Single(l => l.Id == "R002");
        var practice = syntax.Activities.Where(a => a.Id.StartsWith("workshop")).ToList();
        Assert.Equal(4, practice.Count);
        Assert.DoesNotContain("diagnostic", practice[0].Prompt);
        Assert.DoesNotContain("statement", practice[0].Explanation);
        foreach (var activity in content.Lessons.Where(l => !l.Legacy).SelectMany(l => l.Activities).Where(a => a.IsAi))
        {
            Assert.InRange(activity.Prompt.Length, 10, 500);
            Assert.InRange(activity.SolutionCode.Split('\n').Length, 1, 32);
            Assert.DoesNotContain("با یک مثال از خودت", activity.Prompt);
            Assert.DoesNotContain("خروجی واقعی", activity.Prompt);
        }
    }

    sealed class RejectNetwork : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => throw new Exception("An exact reference must not call a model.");
    }
    sealed class Capture : HttpMessageHandler
    {
        public string Context = "";
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Context = body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!;
            return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content = """{"score":100,"summary":"پاسخ معتبر است.","issues":[],"nextHint":"ادامه بده.","ReferenceMatch":true}""" } } } })) };
        }
    }
}
