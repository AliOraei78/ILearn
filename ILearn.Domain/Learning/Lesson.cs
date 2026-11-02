namespace ILearn.Domain.Learning;

public sealed class Lesson { public string Id { get; set; } = ""; public string Title { get; set; } = ""; public string Summary { get; set; } = ""; public List<Activity> Activities { get; set; } = []; }
