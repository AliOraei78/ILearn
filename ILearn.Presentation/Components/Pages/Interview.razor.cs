using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ILearn.Components.Pages;

public partial class Interview
{
    [SupplyParameterFromQuery(Name = "subject")] public string? SubjectQuery { get; set; }
    [SupplyParameterFromQuery(Name = "category")] public string? CategoryQuery { get; set; }
    [SupplyParameterFromQuery(Name = "level")] public string? LevelQuery { get; set; }
    [SupplyParameterFromQuery(Name = "question")] public string? QuestionQuery { get; set; }
    Player? player;
    Progress? progress;
    InterviewSubject? subject;
    InterviewQuestion? question;
    List<InterviewQuestion> filtered = [];
    string category = "", level = "", saveStatus = "";
    string? error;
    bool busy, sessionExpired, focusError;
    long stateVersion, savedVersion;
    CancellationTokenSource? autosave, reviewCancellation;
    ElementReference errorNotice;
    Entry? entry => question is null ? null : GetEntry(question);
    IReadOnlyList<KeyValuePair<string, string>> QuestionOptions => filtered.Select(item => new KeyValuePair<string, string>(item.Activity.Id, item.Activity.Id + " · " + item.Activity.Title + (progress!.Entries.GetValueOrDefault(item.Key(subject!.Id))?.Complete == true ? " ✓" : ""))).ToList();
    int QuestionIndex => question is null ? -1 : filtered.IndexOf(question);
    int Xp => progress is null ? 0 : LearningService.Xp(Content.Lessons, progress, Content.CodeExercises);
    string LoginUrl => "/login?returnUrl=" + Uri.EscapeDataString(new Uri(Navigation.Uri).PathAndQuery);

    Entry GetEntry(InterviewQuestion item)
    {
        var key = item.Key(subject!.Id);
        if (!progress!.Entries.TryGetValue(key, out var value)) progress.Entries[key] = value = new();
        return value;
    }
    protected override Task OnInitializedAsync() => Load();
    async Task Load()
    {
        try { player = await Store.Profile(); progress = await Store.Load(); error = null; }
        catch (UnauthorizedAccessException) { RequireLogin(); }
        catch { error = "سؤال‌ها باز نشدند؛ اتصال را بررسی کن و دوباره تلاش کن."; }
    }
    async Task Reload() { await Load(); await OnParametersSetAsync(); }
    protected override async Task OnParametersSetAsync()
    {
        if (progress is null) return;
        CancelReview();
        subject = Content.Interviews.FirstOrDefault(item => item.Id == (SubjectQuery ?? "csharp")) ?? Content.Interviews[0];
        category = CategoryQuery ?? ""; level = LevelQuery ?? "";
        filtered = subject.Questions.Where(item => (category.Length == 0 || item.Category == category) && (level.Length == 0 || item.Level == level)).ToList();
        question = filtered.FirstOrDefault(item => item.Activity.Id == QuestionQuery) ??
            (QuestionQuery is null ? filtered.FirstOrDefault(item => item.Key(subject.Id) == progress.InterviewQuestion) : null) ?? filtered.FirstOrDefault();
        if (question is not null && progress.InterviewQuestion != question.Key(subject.Id))
        {
            progress.InterviewQuestion = question.Key(subject.Id); stateVersion++;
            await Save(false);
        }
        saveStatus = QuestionQuery is not null && question?.Activity.Id != QuestionQuery ? "سؤال درخواستی پیدا نشد؛ سؤال‌های آماده نمایش داده شدند." : "";
    }
    string Url(string selectedSubject, string selectedCategory = "", string selectedLevel = "", string? id = null) =>
        "/interview?subject=" + Uri.EscapeDataString(selectedSubject) +
        (selectedCategory.Length > 0 ? "&category=" + Uri.EscapeDataString(selectedCategory) : "") +
        (selectedLevel.Length > 0 ? "&level=" + Uri.EscapeDataString(selectedLevel) : "") +
        (id is not null ? "&question=" + Uri.EscapeDataString(id) : "");
    async Task Go(string url)
    {
        CancelReview(); autosave?.Cancel();
        if (stateVersion != savedVersion && !await Save(false)) return;
        Navigation.NavigateTo(url);
    }
    Task ChangeSubject(ChangeEventArgs e) => Go(Url(e.Value?.ToString() ?? "csharp"));
    Task ChangeCategory(ChangeEventArgs e) => Go(Url(subject!.Id, e.Value?.ToString() ?? "", level));
    Task ChangeLevel(ChangeEventArgs e) => Go(Url(subject!.Id, category, e.Value?.ToString() ?? ""));
    Task ChangeQuestion(ChangeEventArgs e) => Go(Url(subject!.Id, category, level, e.Value?.ToString()));
    Task ClearFilters() => Go(Url(subject!.Id));
    Task Move(int offset)
    {
        var index = QuestionIndex + offset;
        return index >= 0 && index < filtered.Count ? Go(Url(subject!.Id, category, level, filtered[index].Activity.Id)) : Task.CompletedTask;
    }
    async Task<bool> Save(bool announce = true)
    {
        if (progress is null || sessionExpired) return false;
        var version = stateVersion;
        try
        {
            await Store.Save(progress); savedVersion = Math.Max(savedVersion, version);
            error = null; saveStatus = announce ? "ذخیره شد" : ""; return true;
        }
        catch (UnauthorizedAccessException) { RequireLogin(); }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Interview progress save failed");
            error = "ذخیره نشد؛ نوشته‌ات در این صفحه باقی است. پیش از بستن صفحه، دوباره تلاش کن یا متن را کپی کن.";
        }
        return false;
    }
    async Task DraftChanged(ChangeEventArgs e)
    {
        if (entry is null || sessionExpired) return;
        CancelReview(); var value = e.Value?.ToString() ?? "";
        entry.Draft = value[..Math.Min(value.Length, 16000)]; entry.Feedback = ""; entry.FeedbackError = false;
        stateVersion++; saveStatus = "در حال ذخیرهٔ پاسخ…";
        autosave?.Cancel(); autosave?.Dispose(); autosave = new(); var token = autosave.Token;
        try { await Task.Delay(Experience.AutosaveDelay, token); await Save(); } catch (OperationCanceledException) { }
    }
    void CancelReview() { reviewCancellation?.Cancel(); busy = false; }
    async Task SelfComplete()
    {
        if (!Experience.LocalProfile || busy || sessionExpired || entry is null || string.IsNullOrWhiteSpace(entry.Draft)) return;
        LearningService.Complete(entry); stateVersion++; await Save();
    }
    async Task AskAi()
    {
        if (busy || sessionExpired || question is null || entry is null || string.IsNullOrWhiteSpace(entry.Draft)) return;
        var task = question; var target = entry; var draft = target.Draft;
        busy = true; target.Feedback = ""; autosave?.Cancel();
        var cancellation = reviewCancellation = new(); var token = cancellation.Token;
        try
        {
            if (!await Save() || token.IsCancellationRequested) return;
            var result = await Reviewer.Get(task.Activity, draft, token, technology: subject!.Title, template: "interview");
            if (token.IsCancellationRequested || question != task || target.Draft != draft) return;
            target.Feedback = result.Feedback; target.FeedbackError = target.NeedsReview = !result.sufficient;
            if (result.sufficient) { target.Complete = true; target.Source = result.Source; }
            stateVersion++; await Save();
        }
        catch (OperationCanceledException)
        {
            if (!token.IsCancellationRequested) await ReviewFailed(target, "زمان بررسی تمام شد. پاسخ محفوظ است؛ دوباره تلاش کن.");
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                Logger.LogWarning(ex, "Interview answer review failed");
                await ReviewFailed(target, "هوش مصنوعی در دسترس نیست یا پاسخ معتبر نداد. پاسخ محفوظ است؛ دوباره بررسی کن.");
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
        target.Feedback = message; target.FeedbackError = true; stateVersion++; await Save();
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
