using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
namespace ILearn.Tests;

public class CurriculumTests
{
    [Fact]
    public void EveryPlayableActivityHasCompleteReference()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn"));
        var curriculum = new Curriculum(new Environment { ContentRootPath = root });
        Assert.Equal(24, curriculum.Lessons.Sum(l => l.Activities.Count));
        foreach (var lesson in curriculum.Lessons) { Assert.NotEmpty(lesson.Summary); foreach (var activity in lesson.Activities) { Assert.NotEmpty(activity.Solution); Assert.NotEmpty(activity.Prompt); if (activity.Kind == "workshop") Assert.NotEmpty(activity.SolutionCode); } }
    }
    sealed class Environment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ILearn"; public string EnvironmentName { get; set; } = "Testing"; public string ContentRootPath { get; set; } = ""; public string WebRootPath { get; set; } = ""; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider(); public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
