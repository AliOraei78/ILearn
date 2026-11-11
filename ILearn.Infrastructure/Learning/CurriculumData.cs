using System.Text.Json;
namespace ILearn.Infrastructure.Learning;

public class CurriculumData : ICurriculum
{
    public CurriculumData(Func<string, string> read)
    {
        Lessons = Load(read).AsReadOnly();
        Interviews = LoadInterviews(read).AsReadOnly();
        Catalog = Array.AsReadOnly(read("catalog.txt").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        var exercises = JsonSerializer.Deserialize<List<CodeExercise>>(read("code-room.json"))
            ?? throw new InvalidDataException("Code room content is empty.");
        exercises.AddRange(Lessons.Where(lesson => !lesson.Legacy).SelectMany(lesson => lesson.Activities
            .Where(activity => activity.Kind == "workshop")
            .Select(activity => new CodeExercise { Subject = "csharp", Technology = "C#", LessonId = lesson.Id, LessonTitle = lesson.Title, Template = "implement", Activity = activity })));
        CodeExercises = exercises.AsReadOnly();
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

    public IReadOnlyList<Lesson> Lessons { get; }
    public IReadOnlyList<CodeExercise> CodeExercises { get; }
    public IReadOnlyList<InterviewSubject> Interviews { get; }
    static List<InterviewSubject> LoadInterviews(Func<string, string> read)
    {
        var subjects = JsonSerializer.Deserialize<List<InterviewSubject>>(read("interviews.json"))
            ?? throw new InvalidDataException("Interview content is empty.");
        if (subjects.Count == 0 || subjects.Select(subject => subject.Id).Distinct().Count() != subjects.Count)
            throw new InvalidDataException("Interview subjects must be present and unique.");
        foreach (var subject in subjects)
        {
            if (string.IsNullOrWhiteSpace(subject.Id) || string.IsNullOrWhiteSpace(subject.Title) ||
                string.IsNullOrWhiteSpace(subject.SelectionNote) || !DateOnly.TryParseExact(subject.ReviewedOn, "yyyy-MM-dd", out _) ||
                subject.Questions.Count == 0 || subject.Sources.Count == 0 ||
                subject.Sources.Select(source => source.Id).Distinct().Count() != subject.Sources.Count ||
                subject.Questions.Select(question => question.Activity.Id).Distinct().Count() != subject.Questions.Count ||
                subject.Questions.Select(question => question.Activity.Prompt).Distinct().Count() != subject.Questions.Count)
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
    static List<Lesson> Load(Func<string, string> read)
    {
        var lessons = JsonSerializer.Deserialize<List<Lesson>>(read("lessons.json"))!;
        var revisions = JsonSerializer.Deserialize<Dictionary<string, Activity>>(read("revisions.json"))!;
        var introductions = JsonSerializer.Deserialize<Dictionary<string, string>>(read("introductions.json"))!;
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
        var roadmap = JsonSerializer.Deserialize<List<Lesson>>(read("csharp-v2.json"))
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
        List<Lesson> curriculum = [.. roadmap, .. lessons];
        var teaching = JsonSerializer.Deserialize<Dictionary<string, LessonSession[]>>(read("teaching.json"))
            ?? throw new InvalidDataException("Teaching content is empty.");
        if (teaching.Count != curriculum.Count)
            throw new InvalidDataException("Teaching content must cover every curriculum chapter.");
        foreach (var lesson in curriculum)
        {
            if (!teaching.TryGetValue(lesson.Id, out var sessions) || sessions is null || sessions.Length < 2)
                throw new InvalidDataException("Invalid teaching sessions for " + lesson.Id);
            if (sessions.Any(s => s is null || string.IsNullOrWhiteSpace(s.Title) || string.IsNullOrWhiteSpace(s.Goal) ||
                string.IsNullOrWhiteSpace(s.Text) || string.IsNullOrWhiteSpace(s.CommonMistake) ||
                string.IsNullOrWhiteSpace(s.Question) || string.IsNullOrWhiteSpace(s.Feedback) || s.ActivityIds is null ||
                s.Options is null || s.Options.Length < 2 || s.Options.Any(string.IsNullOrWhiteSpace) ||
                s.Options.Distinct().Count() != s.Options.Length || s.CorrectOption < 0 || s.CorrectOption >= s.Options.Length))
                throw new InvalidDataException("Incomplete teaching session in " + lesson.Id);
            if (!sessions.SelectMany(s => s.ActivityIds).Order().SequenceEqual(lesson.Activities.Select(a => a.Id).Order()))
                throw new InvalidDataException("Teaching sessions must cover each activity once in " + lesson.Id);
            lesson.Sessions = sessions;
            var resumeIndices = Enumerable.Range(0, sessions.Length).Select(lesson.GetSessionResumeIndex).ToArray();
            if (resumeIndices.Any(index => index is < 0 or int.MaxValue) || resumeIndices.Distinct().Count() != sessions.Length)
                throw new InvalidDataException("Teaching session resume indices must be valid and unique in " + lesson.Id);
            if (sessions.Any(s => s.Practice is { } practice &&
                (string.IsNullOrWhiteSpace(practice.Prompt) || string.IsNullOrWhiteSpace(practice.Criteria))))
                throw new InvalidDataException("Incomplete independent practice in " + lesson.Id);
        }
        return curriculum;
    }
    public IReadOnlyList<string> Catalog { get; }
}
