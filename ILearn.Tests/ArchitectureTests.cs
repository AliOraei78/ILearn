using System.Xml.Linq;
using ILearn.Application.Learning;
using ILearn.Components.Pages;

namespace ILearn.Tests;

public class ArchitectureTests
{
    [Fact]
    public void InnerLayersDoNotReferenceAdapters()
    {
        foreach (var assembly in new[] { typeof(Activity).Assembly, typeof(ILearn.Application.Learning.ICurriculum).Assembly })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Microsoft.AspNetCore") || a.Name.StartsWith("Npgsql") || a.Name == "ILearn.Infrastructure" || a.Name == "ILearn");
    }

    public static TheoryData<string, string[]> ProjectDependencies => new()
    {
        { "ILearn.Domain", [] },
        { "ILearn.Application", ["ILearn.Domain"] },
        { "ILearn.Infrastructure", ["ILearn.Application"] },
        { "ILearn.Infrastructure.Server", ["ILearn.Application", "ILearn.Infrastructure"] },
        { "ILearn.Presentation", ["ILearn.Application"] },
        { "ILearn", ["ILearn.Application", "ILearn.Infrastructure", "ILearn.Infrastructure.Server", "ILearn.Presentation"] },
        { "ILearn.Offline", ["ILearn.Application", "ILearn.Infrastructure", "ILearn.Presentation"] }
    };

    static XDocument Project(string name) => XDocument.Load(Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "../../../../", name, name + ".csproj")));

    [Theory]
    [MemberData(nameof(ProjectDependencies))]
    public void ProjectReferencesFollowLayerBoundaries(string name, string[] expected)
    {
        var references = Project(name).Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(reference.Attribute("Include")!.Value.Replace('\\', '/')));
        Assert.Equal(expected.Order(), references.Order());
    }

    [Theory]
    [MemberData(nameof(ProjectDependencies))]
    public void ProjectsShareAssembliesInsteadOfCompilingOtherProjectsSource(string name, string[] _)
    {
        var sourceLinks = Project(name).Descendants()
            .Where(element => element.Name.LocalName is "Compile" or "RazorComponent")
            .SelectMany(element => (element.Attribute("Include")?.Value ?? "").Split(';'));
        Assert.DoesNotContain(sourceLinks, path => Path.IsPathRooted(path) || path.Replace('\\', '/').StartsWith("../", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainAndApplicationDependOnlyOnTheRuntimeAndDomain()
    {
        foreach (var assembly in new[] { typeof(Activity).Assembly, typeof(ICurriculum).Assembly })
            Assert.All(assembly.GetReferencedAssemblies(), reference =>
                Assert.True(reference.Name!.StartsWith("System", StringComparison.Ordinal) || reference.Name == "ILearn.Domain", reference.Name));
    }

    [Fact]
    public void SharedPresentationDoesNotReferenceHostsOrInfrastructure()
    {
        Assert.DoesNotContain(typeof(Home).Assembly.GetReferencedAssemblies(), reference =>
            reference.Name == "ILearn" || reference.Name == "ILearn.Offline" ||
            reference.Name!.StartsWith("ILearn.Infrastructure", StringComparison.Ordinal) || reference.Name.StartsWith("Npgsql", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("ILearn.Infrastructure")]
    [InlineData("ILearn.Presentation")]
    [InlineData("ILearn.Offline")]
    public void BrowserProjectsDoNotRequireTheServerRuntime(string name)
    {
        Assert.DoesNotContain(Project(name).Descendants("FrameworkReference"),
            reference => reference.Attribute("Include")?.Value == "Microsoft.AspNetCore.App");
        Assert.DoesNotContain(Project(name).Descendants("PackageReference"),
            reference => reference.Attribute("Include")?.Value.StartsWith("Npgsql", StringComparison.Ordinal) == true);
    }
    [Fact]
    public void RepeatedCompletionDoesNotDuplicateXp()
    {
        var activity = new Activity { Id = "a", Answers = ["0"] }; var lesson = new Lesson { Id = "S05", Activities = [activity] }; var entry = new Entry(); var progress = new Progress(); progress.Entries["csharp/S05/a"] = entry;
        ILearn.Application.Learning.LearningService.Check(activity, entry, ["0"], 0); ILearn.Application.Learning.LearningService.Check(activity, entry, ["0"], 0);
        Assert.Equal(10, ILearn.Application.Learning.LearningService.Xp([lesson], progress));
    }
}
