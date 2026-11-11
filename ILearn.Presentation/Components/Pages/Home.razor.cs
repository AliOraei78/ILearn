using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
namespace ILearn.Components.Pages;

public partial class Home
{

    Player? player; int screen; bool answerOpen; string answerText = "";
    [SupplyParameterFromQuery(Name="view")] public string? View { get; set; }
    [SupplyParameterFromQuery(Name="lesson")] public string? Chapter { get; set; }
    [SupplyParameterFromQuery(Name="activity")] public string? Challenge { get; set; }
    [SupplyParameterFromQuery(Name="session")] public string? TeachingSession { get; set; }
    string LessonUrl(Lesson value, int? index = null, bool intro = false, int? session = null) =>
        "/lobby?lesson=" + Uri.EscapeDataString(value.Id) + (index is not null ? "&activity=" + index : "") +
        (intro ? "&view=intro" : "") + (session is not null ? "&session=" + session : "");
    async Task Lobby(int target) { CancelReview(); await Save(false); if(error is not null)return; Navigation.NavigateTo(target == 1 ? "/lobby?view=chapters" : "/lobby"); }
    async Task SelectLesson(Lesson selected) { CancelReview(); await Save(false); if(error is not null)return; Navigation.NavigateTo(LessonUrl(selected)); }
    void FullAnswer() { if(answerOpen && explaining) CancelReview(); answerOpen = !answerOpen; answerText = entry.FullAnswer; }
    async Task ExplainAnswer() { if(sessionExpired||busy)return;CancelReview();await Save(false);if(error is not null)return;answerText = "دارم توضیح ساده‌تر رو از مدل می‌گیرم…"; busy = explaining = true; var a = activity; var target = entry; var tokenSource = reviewCancellation = new(); try { var text = await Reviewer.Answer(a, tokenSource.Token); if (!tokenSource.IsCancellationRequested) { answerText = target.FullAnswer = "توضیح مدل: " + text; await Save(); } } catch (OperationCanceledException) { if (!tokenSource.IsCancellationRequested) answerText = "مدل دیر جواب داد؛ پاسخ مرجع درس بالاتر آماده است."; } catch { if (!tokenSource.IsCancellationRequested) answerText = "مدل در دسترس نیست؛ پاسخ مرجع درس رو بخون."; } finally { if (reviewCancellation == tokenSource) busy = explaining = false; } }
    Progress? progress; Lesson lesson = null!; Activity activity => lesson.Activities[position]; int position; Entry entry => GetEntry(activity);
    string Key => $"csharp/{lesson.Id}/{activity.Id}"; string? error; string saveStatus = ""; bool hint, busy, explaining, sessionExpired, focusError; int step; List<int> order = []; HashSet<int> clues = [];
    ElementReference errorNotice;
    string LoginUrl => "/login?returnUrl=" + Uri.EscapeDataString(new Uri(Navigation.Uri).PathAndQuery);
    void RequireLogin() { if(!sessionExpired)focusError=true;sessionExpired=true;CancelReview();error="نشست حسابت پایان یافته است. اگر پاسخ ذخیره‌نشده‌ای داری، پیش از ورود دوباره آن را کپی کن."; }
    protected override async Task OnAfterRenderAsync(bool firstRender){if(focusError){focusError=false;await errorNotice.FocusAsync();}}
    CancellationTokenSource? autosave, reviewCancellation;
    long draftVersion, savedDraftVersion;
    int Xp => progress is null ? 0 : LearningService.Xp(Content.Lessons, progress, Content.CodeExercises);
    int Completed => lesson.Activities.Count(a => GetEntry(a).Complete);
    Lesson? NextLesson => Content.Lessons.Where(l => l.Legacy == lesson.Legacy).SkipWhile(l => l.Id != lesson.Id).Skip(1).FirstOrDefault();
    int ReviewCount => progress!.Entries.Count(pair => pair.Key.StartsWith("csharp/", StringComparison.Ordinal) && pair.Value.NeedsReview);
    Entry GetEntry(Activity a) { var key = $"csharp/{lesson.Id}/{a.Id}"; if (!progress!.Entries.TryGetValue(key, out var value)) progress.Entries[key] = value = new(); return value; }
    bool showIntro;
    int sessionPosition;
    protected override async Task OnInitializedAsync() { try { player = await Store.Profile(); progress = await Store.Load(); lesson = Content.Lessons.FirstOrDefault(l => l.Id == progress.Lesson) ?? Content.Lessons[0]; position = Math.Clamp(progress.Position, 0, lesson.Activities.Count-1); showIntro = !progress.HasStarted(lesson.Id); Reset(); } catch(UnauthorizedAccessException) {RequireLogin();progress=null;} catch { error = !Experience.LocalProfile?"سیوت باز نشد؛ اتصال دیتابیس رو بررسی کن و صفحه رو دوباره باز کن.":"سیو محلی باز نشد؛ صفحه را دوباره باز کن یا از پروفایل پشتیبان را بازیابی کن. دادهٔ قبلی محفوظ است."; progress = null; } }
    protected override async Task OnParametersSetAsync()
    {
        if (progress is null) return;
        CancelReview();
        var requested = Content.Lessons.FirstOrDefault(l => string.Equals(l.Id, Chapter, StringComparison.OrdinalIgnoreCase));
        lesson = requested ?? Content.Lessons.FirstOrDefault(l => l.Id == progress.Lesson) ?? Content.Lessons[0];
        position = Math.Clamp(requested is not null && int.TryParse(Challenge, out var index) ? index : progress.Positions.GetValueOrDefault(lesson.Id, progress.Lesson == lesson.Id ? progress.Position : 0), 0, lesson.Activities.Count-1);
        screen = requested is not null ? 2 : View == "chapters" ? 1 : 0;
        showIntro = View == "intro" || !progress.HasStarted(lesson.Id);
        var remember = screen == 2 && (progress.Lesson != lesson.Id || progress.Position != position);
        sessionPosition = 0;
        if (screen == 2 && showIntro)
        {
            var known = progress.IntroductionSessions.TryGetValue(lesson.Id, out var savedSession);
            var selected = int.TryParse(TeachingSession, out var number) && number > 0 ? number - 1 : savedSession;
            sessionPosition = lesson.GetSessionPosition(selected);
            var resumeIndex = lesson.GetSessionResumeIndex(sessionPosition);
            if (!known || savedSession != resumeIndex)
            {
                progress.IntroductionSessions[lesson.Id] = resumeIndex;
                remember = true;
            }
        }
        Reset(); if (remember) await Save(false);
        if(Chapter is not null && requested is null) saveStatus = "فصل درخواستی پیدا نشد؛ یکی از فصل‌های آماده را انتخاب کن.";
    }
    async Task StartChallenges() { LearningService.Start(progress!, lesson.Id); await Save(); if(error is not null)return; showIntro = false; Navigation.NavigateTo(LessonUrl(lesson, position)); }
    async Task ShowIntroduction()
    {
        CancelReview();
        await Save();
        if (error is not null) return;
        var related = Array.FindIndex(lesson.Sessions, session => session.ActivityIds.Contains(activity.Id));
        GoToSession(related);
    }
    void GoToSession(int index) => Navigation.NavigateTo(LessonUrl(lesson, intro: true, session: lesson.GetSessionResumeIndex(index) + 1));
    void Reset() { answerOpen = false; answerText = ""; hint = false; step = Math.Clamp(entry.Step, 0, Math.Max(0,activity.Rows.Length-1)); clues = activity.Kind == "detective" ? entry.GameState.History.Where(i => i >= 0 && i < activity.Clues.Length).Take(2).ToHashSet() : []; order = Enumerable.Range(0, activity.Options.Length).Reverse().ToList(); if (activity.Kind == "order" && entry.Answers.Count == order.Count) { var saved = entry.Answers.Select(x => int.TryParse(x, out var value) ? value : -1).ToList(); if (saved.Order().SequenceEqual(order.Order())) order = saved; } }
    async Task Save(bool announce = true) { if (progress is null) return; var savingDraftVersion=draftVersion; saveStatus = ""; try { progress.Lesson = lesson.Id; progress.Position = position; progress.Positions[lesson.Id] = position; await Store.Save(progress);savedDraftVersion=Math.Max(savedDraftVersion,savingDraftVersion); error = null;sessionExpired=false; saveStatus = announce && screen == 2 ? "ذخیره شد" : ""; } catch(UnauthorizedAccessException) {RequireLogin();} catch (Exception ex) { error = "ذخیره نشد؛ این صفحه رو نبند: " + ex.Message; } }
    async Task Navigate(int n) { CancelReview(); await Save(); if(error is not null)return; Navigation.NavigateTo(LessonUrl(lesson, Math.Clamp(n, 0, lesson.Activities.Count-1))); }
    async Task StepChanged(int n) { step = entry.Step = Math.Clamp(n, 0, activity.Rows.Length-1); await Save(); }
    async Task OpenClue(int n) { if(clues.Count >= 2 || n < 0 || n >= activity.Clues.Length) return; clues.Add(n); entry.GameState.History = clues.Order().ToList(); entry.Feedback = ""; await Save(); }
    string SlotValue(int row) { var prefix = row + ":"; return entry.Answers.FirstOrDefault(x => x.StartsWith(prefix))?.Split(':')[1] ?? ""; }
    async Task SetSlot(int row, string value) { entry.Feedback = ""; entry.Answers.RemoveAll(x => x.StartsWith(row + ":")); if (value != "") entry.Answers.Add(row + ":" + value); await Save(); }
    async Task Toggle(int i) { entry.Feedback = ""; var value = i.ToString(); if (!activity.Multiple) entry.Answers.Clear(); if (!entry.Answers.Remove(value)) entry.Answers.Add(value); await Save(); }
    async Task Move(int row, int delta) { entry.Feedback = ""; (order[row], order[row + delta]) = (order[row + delta], order[row]); entry.Answers = order.Select(i => i.ToString()).ToList(); await Save(); }
    async Task Check() { if(sessionExpired)return;LearningService.Check(activity, entry, activity.Kind == "order" ? order.Select(i => i.ToString()) : entry.Answers, clues.Count); await Save(); }
    async Task DraftChanged(ChangeEventArgs e) { CancelReview(); entry.Feedback = ""; entry.Draft = e.Value?.ToString() ?? "";draftVersion++;saveStatus="در حال ذخیرهٔ متن…"; autosave?.Cancel(); autosave?.Dispose(); autosave = new(); var token = autosave.Token; try { await Task.Delay(Experience.AutosaveDelay, token); await Save(); } catch (OperationCanceledException) { } }
    async Task SelfComplete() { if (sessionExpired || string.IsNullOrWhiteSpace(entry.Draft)) return; CancelReview(); LearningService.Complete(entry); await Save(); }
    void CancelReview() { reviewCancellation?.Cancel(); if (explaining && answerOpen) answerText = "توضیح مدل لغو شد؛ پاسخ مرجع درس بالاتر آماده است."; busy = explaining = false; }
    async Task AskAi()
    {
        if(sessionExpired||busy)return;
        var target = entry; var task = activity; var contextLesson = lesson; var draft = target.Draft;
        busy = true; reviewCancellation?.Cancel(); var cancellation = reviewCancellation = new(); target.Feedback = "";
        await Save();if(error is not null){CancelReview();return;}
        try {
            var result = await Reviewer.Get(task, draft, cancellation.Token, contextLesson);
            if (cancellation.IsCancellationRequested || target.Draft != draft) return;
            target.Feedback = result.Feedback; target.FeedbackError = target.NeedsReview = !result.sufficient;
            if (result.sufficient) { target.Complete = true; target.Source = result.Source; }
            await Save();
        } catch (OperationCanceledException) {
            if (!cancellation.IsCancellationRequested) { target.FeedbackError = true; target.Feedback = "زمان بررسی تمام شد. دوباره تلاش کن یا ثبت شخصی را بزن."; }
        } catch (Exception ex) {
            if (!cancellation.IsCancellationRequested) {
                Logger.LogWarning(ex,"Optional answer review failed");target.FeedbackError = true;
                target.Feedback = "مدل در دسترس نیست یا پاسخ معتبر نداد. متن تو محفوظ است. دوباره تلاش کن یا تکمیل را شخصی ثبت کن.";
            }
        } finally { if (reviewCancellation == cancellation) busy = false; await Save(); }
    }
    async Task OpenReview() { var next = LearningService.NextReview(Content.Lessons, progress!); if (next is null) { saveStatus = "فعلاً تمرینی برای مرور نداری."; return; } CancelReview(); await Save(); if(error is not null)return; Navigation.NavigateTo(LessonUrl(next.Value.Lesson, next.Value.Position)); }
    async Task BeforeInternalNavigation(LocationChangingContext context)
    {
        CancelReview();
        if (draftVersion == savedDraftVersion || sessionExpired) return;
        autosave?.Cancel();
        await Save(false);
        if (error is not null) context.PreventNavigation();
    }
    public async ValueTask DisposeAsync() { autosave?.Cancel(); reviewCancellation?.Cancel(); if(draftVersion!=savedDraftVersion)await Save(false); autosave?.Dispose(); reviewCancellation?.Dispose(); }

}
