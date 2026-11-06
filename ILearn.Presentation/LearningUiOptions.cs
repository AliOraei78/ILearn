namespace ILearn.Presentation;

public sealed class LearningUiOptions
{
    public bool LocalProfile { get; init; }
    public TimeSpan AutosaveDelay => LocalProfile ? TimeSpan.Zero : TimeSpan.FromMilliseconds(500);
}
