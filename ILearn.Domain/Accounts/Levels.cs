namespace ILearn.Domain.Accounts;

public static class Levels
{
    public static int Number(int xp) => 1 + xp / 10;
    public static string Title(int xp) => Number(xp) <= 20 ? "تازه‌وارد" : "کدآموز";
}
