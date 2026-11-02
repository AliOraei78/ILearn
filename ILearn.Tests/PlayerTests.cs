namespace ILearn.Tests;

public class PlayerTests
{
    [Theory]
    [InlineData(0, 1, "تازه‌وارد")]
    [InlineData(10, 2, "تازه‌وارد")]
    [InlineData(190, 20, "تازه‌وارد")]
    [InlineData(200, 21, "کدآموز")]
    [InlineData(300, 31, "کدآموز")]
    public void LevelBoundaries(int xp, int level, string title)
    { Assert.Equal(level, Levels.Number(xp)); Assert.Equal(title, Levels.Title(xp)); }
    [Fact]
    public void ViewingAnswerDoesNotCompleteOrChangeDraft()
    { var entry = new Entry { Draft = "پاسخ من" }; entry.FullAnswer = "توضیح مدل"; Assert.False(entry.Complete); Assert.Equal("پاسخ من", entry.Draft); }
}
