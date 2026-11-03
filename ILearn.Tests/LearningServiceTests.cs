using ILearn.Application.Learning;
namespace ILearn.Tests;

public class LearningServiceTests
{
    [Theory]
    [InlineData("choice","درست بود. توضیح قدیمی","درست بود. توضیح جاری")]
    [InlineData("choice","هنوز درست نیست. راهنمای قدیمی","هنوز درست نیست. راهنمای جاری")]
    [InlineData("choice","ابتدا پاسخ را انتخاب کن.","ابتدا پاسخ را انتخاب کن.")]
    [InlineData("workshop","درست بود. بازخورد مدل","درست بود. بازخورد مدل")]
    public void SavedFeedbackUsesCurrentTeachingCopyWithoutChangingStoredWork(string kind,string saved,string expected)
    {
        var activity=new Activity{Kind=kind,Explanation="توضیح جاری",Hint="راهنمای جاری"};
        var entry=new Entry{Feedback=saved,Complete=true,Draft="saved draft"};
        Assert.Equal(expected,LearningService.CurrentFeedback(activity,entry));
        Assert.Equal(saved,entry.Feedback);Assert.True(entry.Complete);Assert.Equal("saved draft",entry.Draft);
    }
    [Fact] public void DetectiveRequiresTwoClues() { var e = new Entry(); LearningService.Check(new Activity { Kind = "detective", Answers = ["0"] }, e, ["0"], 1); Assert.False(e.Complete); }
    [Fact] public void OrderMustMatchExactly() { var e = new Entry(); LearningService.Check(new Activity { Kind = "order", Answers = ["0", "1"] }, e, ["1", "0"], 0); Assert.True(e.NeedsReview); Assert.False(e.Complete); }
    [Fact] public void BlankDraftCannotBeSelfCompleted() { var e = new Entry { Draft = " " }; LearningService.Complete(e); Assert.False(e.Complete); }
    [Fact] public void SelfCompletionReplacesAnEarlierRejectedReview() { var e = new Entry { Draft = "reviewed locally", NeedsReview = true, FeedbackError = true, Feedback = "پاسخ قبلی نیاز به اصلاح دارد." }; LearningService.Complete(e); Assert.True(e.Complete); Assert.False(e.NeedsReview); Assert.False(e.FeedbackError); Assert.Equal("ثبت شخصی", e.Source); Assert.Equal("تکمیل با ارزیابی شخصی ثبت شد.", e.Feedback); }
    [Fact] public void ReviewFindsFirstFlaggedExercise() { var l = new Lesson { Id = "S05", Activities = [new Activity { Id = "a" }] }; var p = new Progress(); p.Entries["csharp/S05/a"] = new Entry { NeedsReview = true }; Assert.Equal(0, LearningService.NextReview([l], p)!.Value.Position); }
    [Fact] public void MissingInputIsValidationWithoutAReviewMistake() { var e=new Entry();LearningService.Check(new Activity{Answers=["0"]},e,[],0);Assert.True(e.FeedbackError);Assert.False(e.NeedsReview);Assert.False(e.Complete); }
    [Fact] public void CorrectingAnAnswerClearsTheErrorState() { var e=new Entry();var a=new Activity{Answers=["0"]};LearningService.Check(a,e,["1"],0);Assert.True(e.FeedbackError);Assert.True(e.NeedsReview);LearningService.Check(a,e,["0"],0);Assert.False(e.FeedbackError);Assert.False(e.NeedsReview);Assert.True(e.Complete); }
    [Theory][InlineData("explain")][InlineData("workshop")]
    public void WrittenActivitiesCannotBeGradedWithAnEmptyAnswerKey(string kind) { var e=new Entry();LearningService.Check(new Activity{Kind=kind},e,[],0);Assert.False(e.Complete);Assert.True(e.FeedbackError); }
}
