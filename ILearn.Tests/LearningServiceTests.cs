using ILearn.Application.Learning;
namespace ILearn.Tests;

public class LearningServiceTests
{
    [Fact] public void DetectiveRequiresTwoClues() { var e = new Entry(); LearningService.Check(new Activity { Kind = "detective", Answers = ["0"] }, e, ["0"], 1); Assert.False(e.Complete); }
    [Fact] public void OrderMustMatchExactly() { var e = new Entry(); LearningService.Check(new Activity { Kind = "order", Answers = ["0", "1"] }, e, ["1", "0"], 0); Assert.True(e.NeedsReview); Assert.False(e.Complete); }
    [Fact] public void BlankDraftCannotBeSelfCompleted() { var e = new Entry { Draft = " " }; LearningService.Complete(e); Assert.False(e.Complete); }
    [Fact] public void ReviewFindsFirstFlaggedExercise() { var l = new Lesson { Id = "S05", Activities = [new Activity { Id = "a" }] }; var p = new Progress(); p.Entries["csharp/S05/a"] = new Entry { NeedsReview = true }; Assert.Equal(0, LearningService.NextReview([l], p)!.Value.Position); }
}
