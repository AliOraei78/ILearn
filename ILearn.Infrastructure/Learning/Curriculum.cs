using System.Text.Json;
namespace ILearn.Infrastructure.Learning;

public sealed class Curriculum(IWebHostEnvironment env) : ICurriculum
{
    public List<Lesson> Lessons { get; } = Load(env);
    static List<Lesson> Load(IWebHostEnvironment env)
    {
        var lessons = JsonSerializer.Deserialize<List<Lesson>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", "lessons.json")))!;
        var revisions = JsonSerializer.Deserialize<Dictionary<string, Activity>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", "revisions.json")))!;
        var introductions = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", "introductions.json")))!;
        foreach (var lesson in lessons) if (introductions.TryGetValue(lesson.Id, out var introduction)) lesson.Summary = introduction;
        foreach (var lesson in lessons) foreach (var activity in lesson.Activities)
        {
            if (revisions.TryGetValue(lesson.Id + "/" + activity.Id, out var revision))
            {
                activity.Prompt = revision.Prompt; activity.Solution = revision.Solution; activity.SolutionCode = revision.SolutionCode;
            }
            if (!activity.IsAi && activity.Game is null) activity.Solution += "\nانتخاب‌های درست:\n" + string.Join("\n", activity.Answers.Select(key =>
            {
                var parts = key.Split(':');
                return parts.Length == 2 ? activity.Rows[int.Parse(parts[0])] + ": " + activity.Options[int.Parse(parts[1])] : activity.Options[int.Parse(key)];
            }));
        }
        return lessons;
    }
    public string[] Catalog { get; } = File.ReadAllLines(Path.Combine(env.ContentRootPath, "Content", "catalog.txt"));
}
