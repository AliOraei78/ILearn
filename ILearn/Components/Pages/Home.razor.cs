using Microsoft.AspNetCore.Components;
namespace ILearn.Components.Pages;

public partial class Home
{

    Player? player; int screen; bool answerOpen; string answerText = "";
    async Task Lobby(int target) { CancelReview(); await Save(false); screen = target; }
    async Task SelectLesson(Lesson selected) { CancelReview(); await Save(false); lesson = selected; position = progress!.Positions.GetValueOrDefault(lesson.Id); showIntro = !progress.HasStarted(lesson.Id); screen = 2; Reset(); await Save(false); }
    async Task FullAnswer() { CancelReview(); answerOpen = true; if (entry.FullAnswer.Length > 0) { answerText = entry.FullAnswer; return; } answerText = "دارم توضیح ساده‌تر رو از مدل می‌گیرم…"; busy = true; var a = activity; var target = entry; var tokenSource = reviewCancellation = new(); try { var text = await Reviewer.Answer(a, tokenSource.Token); if (!tokenSource.IsCancellationRequested) { answerText = target.FullAnswer = "توضیح مدل: " + text; await Save(); } } catch (OperationCanceledException) { if (!tokenSource.IsCancellationRequested) answerText = "مدل دیر جواب داد؛ پاسخ مرجع درس بالاتر آماده است."; } catch { if (!tokenSource.IsCancellationRequested) answerText = "مدل در دسترس نیست؛ پاسخ مرجع درس رو بخون."; } finally { if (reviewCancellation == tokenSource) busy = false; } }
    Progress? progress; Lesson lesson = null!; Activity activity => lesson.Activities[position]; int position; Entry entry => GetEntry(activity);
    string Key => $"csharp/{lesson.Id}/{activity.Id}"; string? error; string saveStatus = ""; bool hint, busy; int step; List<int> order = []; HashSet<int> clues = [];
    CancellationTokenSource? autosave, reviewCancellation;
    int Xp => LearningService.Xp(Content.Lessons, progress!);
    int Completed => lesson.Activities.Count(a => GetEntry(a).Complete);
    int ReviewCount => progress!.Entries.Values.Count(e => e.NeedsReview);
    Entry GetEntry(Activity a) { var key = $"csharp/{lesson.Id}/{a.Id}"; if (!progress!.Entries.TryGetValue(key, out var value)) progress.Entries[key] = value = new(); return value; }
    bool showIntro;
    protected override async Task OnInitializedAsync() { try { player = await Store.Profile(); progress = await Store.Load(); lesson = Content.Lessons.FirstOrDefault(l => l.Id == progress.Lesson) ?? Content.Lessons[0]; position = Math.Clamp(progress.Position, 0, 7); showIntro = !progress.HasStarted(lesson.Id); Reset(); } catch { error = "سیوت باز نشد؛ اتصال دیتابیس رو بررسی کن و صفحه رو دوباره باز کن."; progress = null; } }
    async Task StartChallenges() { LearningService.Start(progress!, lesson.Id); showIntro = false; await Save(); }
    async Task ShowIntroduction() { CancelReview(); await Save(); showIntro = true; }
    void Reset() { answerOpen = false; answerText = ""; hint = false; step = 0; clues = []; order = Enumerable.Range(0, activity.Options.Length).Reverse().ToList(); if (activity.Kind == "order" && entry.Answers.Count == order.Count) { var saved = entry.Answers.Select(int.Parse).ToList(); if (saved.Order().SequenceEqual(order.Order())) order = saved; } }
    async Task Save(bool announce = true) { if (progress is null) return; try { progress.Lesson = lesson.Id; progress.Position = position; progress.Positions[lesson.Id] = position; await Store.Save(progress); saveStatus = announce && screen == 2 ? "ذخیره شد" : ""; } catch (Exception ex) { error = "ذخیره نشد؛ این صفحه رو نبند: " + ex.Message; } }
    async Task Navigate(int n) { CancelReview(); await Save(); position = n; Reset(); await Save(); }
    async Task ChangeLesson(ChangeEventArgs e) { CancelReview(); await Save(); lesson = Content.Lessons.First(l => l.Id == e.Value?.ToString()); position = 0; showIntro = !progress!.HasStarted(lesson.Id); Reset(); await Save(); }
    string SlotValue(int row) { var prefix = row + ":"; return entry.Answers.FirstOrDefault(x => x.StartsWith(prefix))?.Split(':')[1] ?? ""; }
    async Task SetSlot(int row, string value) { entry.Answers.RemoveAll(x => x.StartsWith(row + ":")); if (value != "") entry.Answers.Add(row + ":" + value); await Save(); }
    async Task Toggle(int i) { var value = i.ToString(); if (!activity.Multiple) entry.Answers.Clear(); if (!entry.Answers.Remove(value)) entry.Answers.Add(value); await Save(); }
    async Task Move(int row, int delta) { (order[row], order[row + delta]) = (order[row + delta], order[row]); entry.Answers = order.Select(i => i.ToString()).ToList(); await Save(); }
    async Task Check() { LearningService.Check(activity, entry, activity.Kind == "order" ? order.Select(i => i.ToString()) : entry.Answers, clues.Count); await Save(); }
    async Task DraftChanged(ChangeEventArgs e) { CancelReview(); entry.Draft = e.Value?.ToString() ?? ""; autosave?.Cancel(); autosave = new(); var token = autosave.Token; try { await Task.Delay(500, token); await Save(); } catch (OperationCanceledException) { } }
    async Task SelfComplete() { if (string.IsNullOrWhiteSpace(entry.Draft)) return; CancelReview(); LearningService.Complete(entry); await Save(); }
    void CancelReview() { reviewCancellation?.Cancel(); if (busy && answerOpen) answerText = "توضیح مدل لغو شد؛ پاسخ مرجع درس بالاتر آماده است."; busy = false; }
    async Task AskAi() { var target = entry; var task = activity; var contextLesson = lesson; var draft = target.Draft; busy = true; reviewCancellation?.Cancel(); var cancellation = reviewCancellation = new(); target.Feedback = ""; await Save(); try { var result = await Reviewer.Get(task, draft, cancellation.Token, contextLesson); if (cancellation.IsCancellationRequested || target.Draft != draft) return; target.Feedback = result.Feedback; target.NeedsReview = !result.sufficient; if (result.sufficient) { target.Complete = true; target.Source = "بررسی مدل"; } await Save(); } catch (OperationCanceledException) { if (!cancellation.IsCancellationRequested) target.Feedback = "زمان بررسی تمام شد. دوباره تلاش کن یا ثبت شخصی را بزن."; } catch (Exception ex) { target.Feedback = "Ollama در دسترس نیست یا پاسخ معتبر نداد. متن تو محفوظ است.\n" + ex.Message; } finally { if (reviewCancellation == cancellation) busy = false; await Save(); } }
    async Task OpenReview() { var next = LearningService.NextReview(Content.Lessons, progress!); if (next is null) { saveStatus = "فعلاً تمرینی برای مرور نداری."; return; } CancelReview(); await Save(); lesson = next.Value.Lesson; position = next.Value.Position; screen = 2; showIntro = false; Reset(); await Save(); }
    public async ValueTask DisposeAsync() { autosave?.Cancel(); reviewCancellation?.Cancel(); await Save(); autosave?.Dispose(); reviewCancellation?.Dispose(); }

}