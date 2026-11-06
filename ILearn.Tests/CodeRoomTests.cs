using System.Text.Json;
using ILearn.Application.Learning;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace ILearn.Tests;

public sealed class CodeRoomTests
{
    static Curriculum Load() => new(new TestEnvironment());

    [Fact]
    public void AllTemplatesHaveAuthoredExercisesAndEveryCurrentChapterHasPractice()
    {
        var curriculum = Load();
        Assert.Equal(12, CodeRoomTemplates.All.Count);
        Assert.Equal(CodeRoomTemplates.All.Select(template => template.Id).Order(), curriculum.CodeExercises.Select(exercise => exercise.Template).Distinct().Order());
        Assert.Equal(curriculum.CodeExercises.Count, curriculum.CodeExercises.Select(exercise => exercise.Key).Distinct().Count());
        Assert.Equal(new[] { "csharp", "redis", "sql" }, curriculum.CodeExercises.Select(exercise => exercise.Subject).Distinct().Order());
        foreach (var lesson in curriculum.Lessons.Where(lesson => !lesson.Legacy))
        {
            var practice = Assert.Single(curriculum.CodeExercises, exercise => exercise.Subject == "csharp" && exercise.LessonId == lesson.Id && exercise.Activity.Id == "workshop");
            Assert.Same(lesson.Activities.Single(activity => activity.Id == "workshop"), practice.Activity);
            Assert.Equal("implement", practice.Template);
            foreach (var stage in lesson.Activities.Where(activity => activity.Kind == "workshop"))
                Assert.Contains(curriculum.CodeExercises, exercise => ReferenceEquals(exercise.Activity, stage));
        }
        Assert.All(curriculum.CodeExercises, exercise =>
        {
            Assert.NotEmpty(exercise.Activity.Prompt);
            Assert.NotEmpty(exercise.Activity.Explanation);
            Assert.NotEmpty(exercise.Activity.Solution);
            Assert.NotNull(CodeRoomTemplates.Find(exercise.Template));
        });
    }

    [Fact]
    public void RoomProgressAndXpStaySeparateFromTrainingAndSurviveSerialization()
    {
        var curriculum = Load();
        var practice = curriculum.CodeExercises.First(exercise => exercise.LessonId == "R001");
        var progress = new Progress { Lesson = "R008", Position = 2, CodeRoomExercise = practice.Key };
        progress.Entries[practice.Key] = new Entry { Complete = true, Draft = "saved code", Source = "بررسی مدل" };
        Assert.Equal(practice.Xp, LearningService.Xp(curriculum.Lessons, progress, curriculum.CodeExercises));
        Assert.False(progress.HasStarted(practice.LessonId));
        progress.Entries[practice.Key].Complete = true;
        Assert.Equal(practice.Xp, LearningService.Xp(curriculum.Lessons, progress, curriculum.CodeExercises));
        var restored = JsonSerializer.Deserialize<Progress>(JsonSerializer.Serialize(progress))!;
        Assert.Equal("R008", restored.Lesson);
        Assert.Equal(2, restored.Position);
        Assert.Equal(practice.Key, restored.CodeRoomExercise);
        Assert.Equal("saved code", restored.Entries[practice.Key].Draft);
        Assert.Equal(practice.Xp, LearningService.Xp(curriculum.Lessons, restored, curriculum.CodeExercises));
        restored.Entries[$"csharp/{practice.LessonId}/{practice.Activity.Id}"] = new Entry { Complete = true };
        Assert.Equal(practice.Xp + practice.Activity.Xp, LearningService.Xp(curriculum.Lessons, restored, curriculum.CodeExercises));
    }

    [Fact]
    public void OldSaveHasNoRoomSelectionOrUnexpectedXp()
    {
        var curriculum = Load();
        var progress = JsonSerializer.Deserialize<Progress>("""{"Lesson":"R002","Position":1,"Entries":{"csharp/R001/workshop":{"Complete":true}}}""")!;
        Assert.Empty(progress.CodeRoomExercise);
        Assert.Equal(30, LearningService.Xp(curriculum.Lessons, progress, curriculum.CodeExercises));
    }

    sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../ILearn"));
        public string WebRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
