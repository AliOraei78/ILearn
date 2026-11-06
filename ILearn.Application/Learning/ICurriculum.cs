namespace ILearn.Application.Learning;

public interface ICurriculum
{
    IReadOnlyList<Lesson> Lessons { get; }
    IReadOnlyList<CodeExercise> CodeExercises { get; }
    IReadOnlyList<InterviewSubject> Interviews { get; }
    IReadOnlyList<string> Catalog { get; }
}
