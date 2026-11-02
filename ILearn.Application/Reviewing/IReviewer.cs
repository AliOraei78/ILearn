namespace ILearn.Application.Reviewing;

public interface IReviewer { Task<string> Answer(Activity activity, CancellationToken ct); Task<Review> Get(Activity activity, string draft, CancellationToken ct, Lesson? lesson = null); }