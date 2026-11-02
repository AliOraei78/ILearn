namespace ILearn.Tests;

public class ArchitectureTests
{
    [Fact]
    public void InnerLayersDoNotReferenceAdapters()
    {
        foreach (var assembly in new[] { typeof(Activity).Assembly, typeof(ILearn.Application.Learning.ICurriculum).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.AspNetCore") || a.Name.StartsWith("Npgsql") || a.Name == "ILearn.Infrastructure" || a.Name == "ILearn");
    }
    [Fact]
    public void RepeatedCompletionDoesNotDuplicateXp()
    {
        var activity = new Activity { Id = "a", Answers = ["0"] }; var lesson = new Lesson { Id = "S05", Activities = [activity] }; var entry = new Entry(); var progress = new Progress(); progress.Entries["csharp/S05/a"] = entry;
        ILearn.Application.Learning.LearningService.Check(activity, entry, ["0"], 0); ILearn.Application.Learning.LearningService.Check(activity, entry, ["0"], 0);
        Assert.Equal(10, ILearn.Application.Learning.LearningService.Xp([lesson], progress));
    }
}
