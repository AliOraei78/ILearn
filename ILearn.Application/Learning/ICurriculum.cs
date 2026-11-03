namespace ILearn.Application.Learning;

public interface ICurriculum { List<Lesson> Lessons { get; } List<CodeExercise> CodeExercises { get; } List<InterviewSubject> Interviews { get; } string[] Catalog { get; } }
