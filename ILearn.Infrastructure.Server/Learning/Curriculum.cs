namespace ILearn.Infrastructure.Learning;

public sealed class Curriculum(IWebHostEnvironment env)
    : CurriculumData(name => File.ReadAllText(Path.Combine(env.ContentRootPath, "Content", name)));
