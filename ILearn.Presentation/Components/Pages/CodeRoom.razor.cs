using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ILearn.Components.Pages;

public partial class CodeRoom
{
    [SupplyParameterFromQuery(Name = "subject")] public string? SubjectQuery { get; set; }
    [SupplyParameterFromQuery(Name = "chapter")] public string? ChapterQuery { get; set; }
    [SupplyParameterFromQuery(Name = "template")] public string? TemplateQuery { get; set; }
    [SupplyParameterFromQuery(Name = "exercise")] public string? ExerciseQuery { get; set; }

    Player? player;
    Progress? progress;
    CodeExercise? exercise;
    List<CodeExercise> filtered = [];
    string subject = "csharp", chapter = "", template = "", saveStatus = "";
    string? error;
    bool busy, sessionExpired, focusError;
    long stateVersion, savedVersion;
    CancellationTokenSource? autosave, reviewCancellation;
    ElementReference errorNotice;
    Entry? entry => exercise is null ? null : GetEntry(exercise);
    CodeTemplate? Format => exercise is null ? null : CodeRoomTemplates.Find(exercise.Template);
    IEnumerable<CodeExercise> SubjectExercises => Content.CodeExercises.Where(item => item.Subject == subject);
    IReadOnlyList<KeyValuePair<string, string>> ExerciseOptions => filtered.Select(item => new KeyValuePair<string, string>(item.Key, item.LessonId + " · " + item.Activity.Title + (progress!.Entries.GetValueOrDefault(item.Key)?.Complete == true ? " ✓" : ""))).ToList();
    IReadOnlyList<KeyValuePair<string, string>> ChapterOptions => new[] { new KeyValuePair<string, string>("", "همه فصل‌ها") }.Concat(SubjectExercises.GroupBy(item => item.LessonId).OrderBy(group => group.Key).Select(group => new KeyValuePair<string, string>(group.Key, group.Key + " · " + group.First().LessonTitle))).ToList();
    int Xp => progress is null ? 0 : LearningService.Xp(Content.Lessons, progress, Content.CodeExercises);
    string LoginUrl => "/login?returnUrl=" + Uri.EscapeDataString(new Uri(Navigation.Uri).PathAndQuery);

    Entry GetEntry(CodeExercise item)
    {
        if (!progress!.Entries.TryGetValue(item.Key, out var value)) progress.Entries[item.Key] = value = new();
        return value;
    }

    protected override Task OnInitializedAsync() => Load();
    async Task Load()
    {
        try { player = await Store.Profile(); progress = await Store.Load(); error = null; }
        catch (UnauthorizedAccessException) { RequireLogin(); }
        catch { error = "تمرین‌ها باز نشدند؛ اتصال را بررسی کن و دوباره تلاش کن."; }
    }

    async Task Reload() { await Load(); await OnParametersSetAsync(); }

    protected override async Task OnParametersSetAsync()
    {
        if (progress is null) return;
        CancelReview();
        var requested = Content.CodeExercises.FirstOrDefault(item => item.Key == ExerciseQuery);
        var remembered = Content.CodeExercises.FirstOrDefault(item => item.Key == progress.CodeRoomExercise);
        subject = SubjectQuery ?? requested?.Subject ?? remembered?.Subject ?? "csharp";
        var invalidSubject = !Content.CodeExercises.Any(item => item.Subject == subject);
        if (invalidSubject) subject = "csharp";
        chapter = ChapterQuery ?? "";
        template = TemplateQuery ?? "";
        filtered = SubjectExercises.Where(item => (chapter.Length == 0 || item.LessonId == chapter) &&
            (template.Length == 0 || item.Template == template)).ToList();
        exercise = filtered.FirstOrDefault(item => item.Key == ExerciseQuery) ??
            (ExerciseQuery is null ? filtered.FirstOrDefault(item => item.Key == progress.CodeRoomExercise) : null) ??
            filtered.FirstOrDefault(item => item.Subject == "csharp" && item.LessonId == progress.Lesson) ?? filtered.FirstOrDefault();
        if (exercise is not null && progress.CodeRoomExercise != exercise.Key)
        {
            progress.CodeRoomExercise = exercise.Key;
            stateVersion++;
            await Save(false);
        }
        saveStatus = ExerciseQuery is not null && exercise?.Key != ExerciseQuery ? "تمرین درخواستی پیدا نشد؛ تمرین‌های آماده نمایش داده شدند." :
            invalidSubject ? "موضوع درخواستی پیدا نشد؛ تمرین‌های C# نمایش داده شدند." : "";
    }

    string Url(string selectedSubject, string selectedChapter, string selectedTemplate, string? key = null) =>
        "/code-room?subject=" + Uri.EscapeDataString(selectedSubject) +
        (selectedChapter.Length > 0 ? "&chapter=" + Uri.EscapeDataString(selectedChapter) : "") +
        (selectedTemplate.Length > 0 ? "&template=" + Uri.EscapeDataString(selectedTemplate) : "") +
        (key is not null ? "&exercise=" + Uri.EscapeDataString(key) : "");

    async Task Go(string url)
    {
        CancelReview(); autosave?.Cancel();
        if (stateVersion != savedVersion && !await Save(false)) return;
        Navigation.NavigateTo(url);
    }
    Task ChangeSubject(ChangeEventArgs e) => Go(Url(e.Value?.ToString() ?? "csharp", "", template));
    Task ChangeChapter(ChangeEventArgs e) => Go(Url(subject, e.Value?.ToString() ?? "", template));
    Task ChangeTemplate(ChangeEventArgs e) => Go(Url(subject, chapter, e.Value?.ToString() ?? ""));
    Task ChangeExercise(ChangeEventArgs e) => Go(Url(subject, chapter, template, e.Value?.ToString()));
    Task ClearFilters() => Go(Url(Content.CodeExercises.Any(item => item.Subject == subject) ? subject : "csharp", "", ""));

    async Task<bool> Save(bool announce = true)
    {
        if (progress is null || sessionExpired) return false;
        var version = stateVersion;
        try
        {
            await Store.Save(progress);
            savedVersion = Math.Max(savedVersion, version);
            error = null;
            saveStatus = announce ? "ذخیره شد" : "";
            return true;
        }
        catch (UnauthorizedAccessException) { RequireLogin(); }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Code room progress save failed");
            error = "ذخیره نشد؛ نوشته‌ات در این صفحه باقی است. پیش از بستن صفحه، دوباره تلاش کن یا متن را کپی کن.";
        }
        return false;
    }

    async Task DraftChanged(ChangeEventArgs e)
    {
        if (entry is null || sessionExpired) return;
        CancelReview();
        var value = e.Value?.ToString() ?? "";
        entry.Draft = value[..Math.Min(value.Length, 16000)];
        entry.Feedback = ""; entry.FeedbackError = false;
        stateVersion++;
        saveStatus = "در حال ذخیرهٔ متن…";
        autosave?.Cancel(); autosave?.Dispose(); autosave = new();
        var token = autosave.Token;
        try { await Task.Delay(Experience.AutosaveDelay, token); await Save(); }
        catch (OperationCanceledException) { }
    }

    Task UseStarter() => entry is not null && entry.Draft.Length == 0
        ? DraftChanged(new ChangeEventArgs { Value = exercise!.Activity.Code }) : Task.CompletedTask;

    void CancelReview() { reviewCancellation?.Cancel(); busy = false; }

    async Task SelfComplete()
    {
        if (!Experience.LocalProfile || busy || sessionExpired || entry is null || string.IsNullOrWhiteSpace(entry.Draft)) return;
        LearningService.Complete(entry); stateVersion++; await Save();
    }

    async Task AskAi()
    {
        if (busy || sessionExpired || exercise is null || entry is null || string.IsNullOrWhiteSpace(entry.Draft)) return;
        var task = exercise; var target = entry; var draft = target.Draft;
        busy = true; target.Feedback = "";
        autosave?.Cancel();
        var cancellation = reviewCancellation = new();
        var token = cancellation.Token;
        try
        {
            if (!await Save()) return;
            if (token.IsCancellationRequested) return;
            var lesson = new Lesson { Id = task.LessonId, Title = task.LessonTitle };
            var result = await Reviewer.Get(task.Activity, draft, token, lesson, task.Technology, task.Template);
            if (token.IsCancellationRequested || exercise != task || target.Draft != draft) return;
            target.Feedback = result.Feedback;
            target.FeedbackError = target.NeedsReview = !result.sufficient;
            if (result.sufficient) { target.Complete = true; target.Source = result.Source; }
            stateVersion++;
            await Save();
        }
        catch (OperationCanceledException)
        {
            if (!token.IsCancellationRequested) await ReviewFailed(target, "زمان بررسی تمام شد. متن محفوظ است؛ دوباره تلاش کن.");
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                Logger.LogWarning(ex, "Code room answer review failed");
                await ReviewFailed(target, "هوش مصنوعی در دسترس نیست یا پاسخ معتبر نداد. متن محفوظ است؛ بررسی را دوباره انجام بده.");
            }
        }
        finally
        {
            if (reviewCancellation == cancellation) { busy = false; reviewCancellation = null; }
            cancellation.Dispose();
        }
    }

    async Task ReviewFailed(Entry target, string message)
    {
        target.Feedback = message; target.FeedbackError = true;
        stateVersion++;
        await Save();
    }

    void RequireLogin()
    {
        sessionExpired = true; focusError = true; CancelReview();
        error = "نشست حسابت پایان یافته است. پیش از ورود دوباره، پاسخ ذخیره‌نشده را کپی کن.";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (focusError) { focusError = false; await errorNotice.FocusAsync(); }
    }

    async Task BeforeInternalNavigation(LocationChangingContext context)
    {
        CancelReview(); autosave?.Cancel();
        if (!sessionExpired && stateVersion != savedVersion && !await Save(false)) context.PreventNavigation();
    }

    public async ValueTask DisposeAsync()
    {
        autosave?.Cancel(); CancelReview();
        if (!sessionExpired && stateVersion != savedVersion) await Save(false);
        autosave?.Dispose();
    }
}
