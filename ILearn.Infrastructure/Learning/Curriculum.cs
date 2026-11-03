using System.Text.Json;
namespace ILearn.Infrastructure.Learning;

public sealed class Curriculum : ICurriculum
{
    public Curriculum(IWebHostEnvironment env)
    {
        Lessons = Load(env);
        Interviews = LoadInterviews(env);
        Catalog = File.ReadAllLines(Path.Combine(env.ContentRootPath, "Content", "catalog.txt"));
        CodeExercises = JsonSerializer.Deserialize<List<CodeExercise>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", "code-room.json")))
            ?? throw new InvalidDataException("Code room content is empty.");
        CodeExercises.AddRange(Lessons.Where(lesson => !lesson.Legacy).SelectMany(lesson => lesson.Activities
            .Where(activity => activity.Kind == "workshop")
            .Select(activity => new CodeExercise { Subject = "csharp", Technology = "C#", LessonId = lesson.Id, LessonTitle = lesson.Title, Template = "implement", Activity = activity })));
        if (CodeExercises.Select(exercise => exercise.Key).Distinct().Count() != CodeExercises.Count ||
            CodeRoomTemplates.All.Any(template => !CodeExercises.Any(exercise => exercise.Template == template.Id)))
            throw new InvalidDataException("Code room keys must be unique and all templates must have an exercise.");
        foreach (var exercise in CodeExercises)
            if (string.IsNullOrWhiteSpace(exercise.Subject) || string.IsNullOrWhiteSpace(exercise.Technology) ||
                string.IsNullOrWhiteSpace(exercise.LessonId) || string.IsNullOrWhiteSpace(exercise.LessonTitle) ||
                string.IsNullOrWhiteSpace(exercise.Activity.Id) || string.IsNullOrWhiteSpace(exercise.Activity.Title) ||
                string.IsNullOrWhiteSpace(exercise.Activity.Prompt) || string.IsNullOrWhiteSpace(exercise.Activity.Explanation) ||
                string.IsNullOrWhiteSpace(exercise.Activity.Solution) || CodeRoomTemplates.Find(exercise.Template) is null ||
                (exercise.Subject == "csharp" && !Lessons.Any(lesson => lesson.Id == exercise.LessonId)))
                throw new InvalidDataException("Incomplete code room exercise: " + exercise.Key);
    }

    public List<Lesson> Lessons { get; }
    public List<CodeExercise> CodeExercises { get; }
    public List<InterviewSubject> Interviews { get; }
    static List<InterviewSubject> LoadInterviews(IWebHostEnvironment env)
    {
        var subjects = JsonSerializer.Deserialize<List<InterviewSubject>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", "interviews.json")))
            ?? throw new InvalidDataException("Interview content is empty.");
        if (subjects.Count == 0 || subjects.Select(subject => subject.Id).Distinct().Count() != subjects.Count)
            throw new InvalidDataException("Interview subjects must be present and unique.");
        foreach (var subject in subjects)
        {
            if (string.IsNullOrWhiteSpace(subject.Id) || string.IsNullOrWhiteSpace(subject.Title) ||
                string.IsNullOrWhiteSpace(subject.SelectionNote) || !DateOnly.TryParseExact(subject.ReviewedOn, "yyyy-MM-dd", out _) ||
                subject.Questions.Count != 100 || subject.Sources.Count == 0 ||
                subject.Sources.Select(source => source.Id).Distinct().Count() != subject.Sources.Count ||
                subject.Questions.Select(question => question.Activity.Id).Distinct().Count() != 100 ||
                subject.Questions.Select(question => question.Activity.Prompt).Distinct().Count() != 100)
                throw new InvalidDataException("Invalid interview subject: " + subject.Id);
            foreach (var source in subject.Sources)
                if (string.IsNullOrWhiteSpace(source.Id) || string.IsNullOrWhiteSpace(source.Title) ||
                    !Uri.TryCreate(source.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps ||
                    source.Role is not ("selection" or "verification"))
                    throw new InvalidDataException("Invalid interview source: " + source.Id);
            foreach (var question in subject.Questions)
                if (string.IsNullOrWhiteSpace(question.Category) || question.Level is not ("مقدماتی" or "متوسط" or "پیشرفته") ||
                    string.IsNullOrWhiteSpace(question.Activity.Id) || string.IsNullOrWhiteSpace(question.Activity.Title) ||
                    question.Activity.Kind != "explain" || string.IsNullOrWhiteSpace(question.Activity.Prompt) ||
                    string.IsNullOrWhiteSpace(question.Activity.Explanation) || string.IsNullOrWhiteSpace(question.Activity.Solution) ||
                    string.IsNullOrWhiteSpace(question.Activity.Hint) ||
                    !new[] { "selection", "verification" }.All(role => question.SourceIds.Any(id => subject.Sources.Any(source => source.Id == id && source.Role == role))) ||
                    question.SourceIds.Any(id => !subject.Sources.Any(source => source.Id == id)))
                    throw new InvalidDataException("Incomplete interview question: " + question.Key(subject.Id));
        }
        return subjects;
    }
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
            if (!activity.IsAi && activity.Game is null && activity.Puzzle is null) activity.Solution += "\nانتخاب‌های درست:\n" + string.Join("\n", activity.Answers.Select(key =>
            {
                var parts = key.Split(':');
                return parts.Length == 2 ? activity.Rows[int.Parse(parts[0])] + ": " + activity.Options[int.Parse(parts[1])] : activity.Options[int.Parse(key)];
            }));
        }
        var roadmap = JsonSerializer.Deserialize<List<Lesson>>(File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", "csharp-v2.json")))
            ?? throw new InvalidDataException("C# curriculum is empty.");
        if (!roadmap.Select(l => l.Id).SequenceEqual(Enumerable.Range(1, 123).Select(i => $"R{i:000}")))
            throw new InvalidDataException("C# units must cover R001 through R123 in order.");
        foreach (var lesson in roadmap)
        {
            if (lesson.Phase is < 1 or > 11 || lesson.Activities.Count < 4 || lesson.Activities.Select(a => a.Id).Distinct().Count() != lesson.Activities.Count ||
                lesson.Prerequisites.Any(id => string.CompareOrdinal(id, lesson.Id) >= 0 || !roadmap.Any(l => l.Id == id)))
                throw new InvalidDataException("Invalid C# unit: " + lesson.Id);
            foreach (var activity in lesson.Activities)
            {
                if (string.IsNullOrWhiteSpace(activity.Prompt) || string.IsNullOrWhiteSpace(activity.Solution) || string.IsNullOrWhiteSpace(activity.Hint))
                    throw new InvalidDataException("Incomplete activity: " + lesson.Id + "/" + activity.Id);
                if (activity.Puzzle is not null) Puzzles.Validate(activity);
            }
        }
        return [.. roadmap, .. lessons];
    }
    public string[] Catalog { get; }
}
