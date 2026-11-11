using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using ILearn.Application.Learning;
using System.Text.Json;
namespace ILearn.Tests;

public class CurriculumTests
{
    static string ContentRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn"));

    static (IReadOnlyList<Lesson> Lessons, IReadOnlyList<string> Catalog) Load()
    {
        var curriculum = new Curriculum(new Environment { ContentRootPath = ContentRoot });
        return (curriculum.Lessons.Where(lesson => lesson.Legacy).ToList(), curriculum.Catalog);
    }

    [Fact]
    public void AChapterWithoutTeachingIsRejected()
    {
        string Read(string name) => File.ReadAllText(Path.Combine(ContentRoot, "Content", name));
        var teaching = JsonSerializer.Deserialize<Dictionary<string, LessonSession[]>>(Read("teaching.json"))!;
        Assert.True(teaching.Remove("S10"));
        Assert.Throws<InvalidDataException>(() =>
            new CurriculumData(name => name == "teaching.json" ? JsonSerializer.Serialize(teaching) : Read(name)));
    }

    [Fact]
    public void ExpandedTeachingRestoresPreviouslySavedSessions()
    {
        var curriculum = new Curriculum(new Environment { ContentRootPath = ContentRoot });
        var asyncProject = curriculum.Lessons.Single(lesson => lesson.Id == "R094");
        var finalProject = curriculum.Lessons.Single(lesson => lesson.Id == "R123");

        int inputDemo = asyncProject.GetSessionPosition(2);
        int delivery = finalProject.GetSessionPosition(6);

        Assert.Contains("Input failure observed", asyncProject.Sessions[inputDemo].Text);
        Assert.Contains("README", finalProject.Sessions[delivery].Text);
        Assert.True(inputDemo > 2);
        Assert.True(delivery > 6);
        Assert.Equal(2, asyncProject.GetSessionResumeIndex(inputDemo));
        Assert.Equal(6, finalProject.GetSessionResumeIndex(delivery));
    }

    [Fact]
    public void EveryPlayableActivityHasCompleteReference()
    {
        var curriculum = Load();
        Assert.Equal(curriculum.Catalog.Take(10).Select(line => line.Split(' ')[0]), curriculum.Lessons.Select(l => l.Id));
        Assert.True(curriculum.Lessons.Sum(l => l.Activities.Count) > 80);
        foreach (var lesson in curriculum.Lessons)
        {
            Assert.False(string.IsNullOrWhiteSpace(lesson.Summary));
            Assert.True(lesson.Activities.Count(a => !a.IsAi) >= 5);
            Assert.Contains(lesson.Activities, a => a.Kind == "workshop");
            foreach (var activity in lesson.Activities)
            {
                Assert.False(string.IsNullOrWhiteSpace(activity.Prompt), lesson.Id + "/" + activity.Id);
                Assert.False(string.IsNullOrWhiteSpace(activity.Solution), lesson.Id + "/" + activity.Id);
                Assert.False(string.IsNullOrWhiteSpace(activity.Hint), lesson.Id + "/" + activity.Id);
                Assert.False(string.IsNullOrWhiteSpace(activity.Explanation), lesson.Id + "/" + activity.Id);
                if (activity.Kind == "workshop") Assert.NotEmpty(activity.SolutionCode);
                Assert.Contains(activity.Kind, new[] { "choice", "slots", "order", "trace", "detective", "explain", "workshop", "concept", "experiment", "budget", "mission", "assessment", "deduction" });
                if (activity.Kind == "detective") Assert.True(activity.Clues.Length >= 2);
            }
        }
    }

    [Fact]
    public void AllAnswerKeysFitTheirControls()
    {
        foreach (var activity in Load().Lessons.SelectMany(l => l.Activities).Where(a => !a.IsAi && a.Kind != "mission"))
        {
            Assert.NotEmpty(activity.Answers);
            Assert.Equal(activity.Answers.Length, activity.Answers.Distinct().Count());
            foreach (var key in activity.Answers)
            {
                var parts = key.Split(':');
                Assert.InRange(int.Parse(parts[^1]), 0, activity.Options.Length - 1);
                if (parts.Length == 2) Assert.InRange(int.Parse(parts[0]), 0, activity.Rows.Length - 1);
            }
            if (activity.Kind is "slots" or "trace" or "assessment" or "deduction")
                Assert.Equal(Enumerable.Range(0, activity.Rows.Length), activity.Answers.Select(k => int.Parse(k.Split(':')[0])).Order());
            if (activity.Kind == "order") Assert.Equal(Enumerable.Range(0, activity.Options.Length), activity.Answers.Select(int.Parse).Order());
            if (activity.Kind == "choice" && !activity.Multiple) Assert.Single(activity.Answers);
            if (activity.Kind == "experiment")
            {
                Assert.NotEmpty(activity.Game!.Results);
                Assert.InRange(activity.Game.Limit, 1, activity.Game.Results.Length);
            }
        }
    }

    [Fact]
    public void EveryChapterCanBeCompletedAndReplayedWithoutExtraXp()
    {
        var curriculum = Load();
        var progress = new Progress();
        foreach (var lesson in curriculum.Lessons)
        {
            LearningService.Start(progress, lesson.Id);
            foreach (var activity in lesson.Activities)
            {
                var entry = new Entry { Draft = activity.SolutionCode.Length > 0 ? activity.SolutionCode : activity.Solution };
                progress.Entries[$"csharp/{lesson.Id}/{activity.Id}"] = entry;
                if (activity.IsAi) LearningService.Complete(entry);
                else
                {
                    PrepareGame(activity, entry);
                    LearningService.Check(activity, entry, activity.Answers, activity.Clues.Length);
                    LearningService.Check(activity, entry, activity.Answers, activity.Clues.Length);
                }
                Assert.True(entry.Complete, lesson.Id + "/" + activity.Id + ": " + entry.Feedback);
                Assert.False(entry.NeedsReview);
            }
        }
        var expected = curriculum.Lessons.Sum(lesson => lesson.Activities.Sum(activity => activity.Xp));
        Assert.Equal(expected, LearningService.Xp(curriculum.Lessons, progress));
        var restored = JsonSerializer.Deserialize<Progress>(JsonSerializer.Serialize(progress))!;
        Assert.Equal(expected, LearningService.Xp(curriculum.Lessons, restored));
        Assert.All(curriculum.Lessons, l => Assert.True(restored.HasStarted(l.Id)));
    }

    [Fact]
    public void WrongOrIncompletePuzzleAnswersNeverComplete()
    {
        foreach (var activity in Load().Lessons.SelectMany(l => l.Activities).Where(a => !a.IsAi))
        {
            var entry = new Entry();
            if (activity.Kind == "concept") MiniGames.Record(activity, entry.GameState, Enumerable.Range(0, activity.Options.Length).First(i => !activity.Answers.Contains(i.ToString())));
            if (activity.Kind == "experiment") MiniGames.Record(activity, entry.GameState, 0);
            var incorrect = activity.Kind == "order" ? activity.Answers.Reverse().ToArray() : new[] { "invalid" };
            LearningService.Check(activity, entry, incorrect, activity.Clues.Length);
            Assert.False(entry.Complete, activity.Id);
        }
    }

    [Fact]
    public void FreshProgressStartsAtOneAndExistingProgressRetainsItsChapter()
    {
        Assert.Equal("R001", new Progress().Lesson);
        var old = JsonSerializer.Deserialize<Progress>("""{"Lesson":"S08","Position":4,"Entries":{"csharp/S08/assessment":{"Complete":true}},"IntroductionsRead":["csharp/S05"]}""")!;
        Assert.Equal("S08", old.Lesson);
        Assert.Equal(4, old.Position);
        Assert.True(old.HasStarted("S08"));
        Assert.True(old.HasStarted("S05"));
        Assert.Empty(old.IntroductionSessions);
        Assert.Equal(10, LearningService.Xp(Load().Lessons, old));
    }

    [Fact]
    public void SavingATeachingSessionDoesNotStartTheChallenges()
    {
        var progress = new Progress();
        progress.IntroductionSessions["R001"] = 2;

        var restored = JsonSerializer.Deserialize<Progress>(JsonSerializer.Serialize(progress))!;

        Assert.Equal(2, restored.IntroductionSessions["R001"]);
        Assert.False(restored.HasStarted("R001"));
    }

    static void PrepareGame(Activity activity, Entry entry)
    {
        if (activity.Kind == "concept") MiniGames.Record(activity, entry.GameState, int.Parse(activity.Answers[0]));
        if (activity.Kind == "experiment") MiniGames.Record(activity, entry.GameState, 0);
        if (activity.Kind == "deduction") foreach (var answer in activity.Answers) entry.GameState.Marks[answer] = 1;
        if (activity.Kind == "mission")
        {
            var path = FindSuccess(activity.Game!, 0, activity.Game!.Limit);
            Assert.NotNull(path);
            foreach (var choice in path) Assert.True(MiniGames.Record(activity, entry.GameState, choice));
        }
    }

    static List<int>? FindSuccess(MiniGame game, int node, int remaining)
    {
        if (game.Nodes[node].Success) return [];
        if (remaining == 0) return null;
        for (int i = 0; i < game.Nodes[node].Next.Length; i++)
        {
            var tail = FindSuccess(game, game.Nodes[node].Next[i], remaining - 1);
            if (tail is not null) return [i, .. tail];
        }
        return null;
    }
    sealed class Environment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ILearn"; public string EnvironmentName { get; set; } = "Testing"; public string ContentRootPath { get; set; } = ""; public string WebRootPath { get; set; } = ""; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider(); public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
