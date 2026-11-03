"""Executable companion projects for version-specific language features."""
from author_csharp_v2 import UNITS
from csharp_content.projects import CONSOLE


def companion(number, code, output, explanation, *, files=None):
    UNITS[number]["files"] = {
        "Features.csproj": CONSOLE.replace("<Nullable>enable</Nullable>", "<Nullable>enable</Nullable>\n    <LangVersion>14</LangVersion>"),
        "Program.cs": code,
        "README.md": explanation + "\n\nدر پوشهٔ مستقل با .NET SDK 10 این فایل‌ها را بساز و dotnet run را اجرا کن. خروجی مورد انتظار:\n" + output,
        **(files or {}),
    }
    UNITS[number]["summary"] += " نمونهٔ تکمیلی قابل اجرا و توضیح تفاوت نسخه‌ها در فایل‌های پاسخ کامل کارگاه آمده است."


UNITS[16]["files"] = {
    "hello.cs": '''#:property TargetFramework=net10.0
#:property LangVersion=14
Console.WriteLine("file-based");
''',
    "README.md": "در پوشه‌ای بدون csproj، hello.cs را با SDK 10 اجرا کن: dotnet run hello.cs. دستورهای #: در ابتدای فایل تنظیمات build را تعیین می‌کنند؛ این‌ها #if نیستند. #:package Name@Version وابستگی و #:project مسیر csproj دیگر را اضافه می‌کند؛ این نمونه بدون وابستگی است. در پروژهٔ معمولی تنظیمات متناظر در csproj قرار دارند. خروجی file-based است. مرجع: https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps"
}
UNITS[16]["summary"] += " پاسخ کامل، نمونهٔ file-based با دستورهای #: و فرمان اجرای واقعی SDK 10 هم دارد."

companion(40, '''var counter = new Counter(1);
var alias = counter;
counter += 4;
++counter;
Console.WriteLine($"{counter.Value},{ReferenceEquals(counter, alias)}");
var max = new Number(int.MaxValue);
Console.WriteLine((max + new Number(1)).Value);
try { _ = checked(max + new Number(1)); }
catch (OverflowException) { Console.WriteLine("overflow"); }

sealed class Counter(int value)
{
    public int Value { get; private set; } = value;
    public void operator +=(int amount) => Value = checked(Value + amount);
    public void operator ++() => Value = checked(Value + 1);
}
readonly record struct Number(int Value)
{
    public static Number operator +(Number a, Number b) => new(unchecked(a.Value + b.Value));
    public static Number operator checked +(Number a, Number b) => new(checked(a.Value + b.Value));
}
''', "6,True\n-2147483648\noverflow",
"C# 14 اجازهٔ operator += و ++ نمونه‌ای با خروجی void می‌دهد. متغیر و alias این Counter به همان شیء اشاره می‌کنند و تغییر را هر دو می‌بینند. این انتخاب برای نوع تغییرپذیر آگاهانه است؛ برای نوع مقداری immutable معمولاً operator + ایستای بازگردانندهٔ مقدار تازه مناسب‌تر است. در Number، checked در محل فراخوانی نسخهٔ checked + را انتخاب می‌کند؛ بدنه هم محاسبهٔ checked دارد. نسخهٔ عادی صریحاً wrap می‌کند. با max=0 هر دو مسیر نتیجهٔ 1 دارند. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading")

companion(44, '''Console.WriteLine(nameof(List<>));
Console.WriteLine(typeof(List<>).IsGenericTypeDefinition);
''', "List\nTrue",
"nameof(List<>) از C# 14 نام نوع جنریک باز را در زمان کامپایل به رشتهٔ List تبدیل می‌کند. typeof(List<>) شیء Type مربوط به تعریف جنریک را می‌دهد؛ nameof جای reflection یا ساخت نوع بسته با MakeGenericType نیست. در نسخهٔ قدیمی‌تر nameof(List<int>) نیز List می‌دهد ولی باید آرگومان نوع بنویسی. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14")

companion(49, '''Parser parse = (text, out result) => int.TryParse(text, out result);
Console.WriteLine($"{parse("12", out int value)},{value}");
Console.WriteLine($"{parse("bad", out value)},{value}");
delegate bool Parser(string text, out int result);
''', "True,12\nFalse,0",
"در C# 14 پارامتر lambda دارای out می‌تواند نوعش را از delegate هدف بگیرد. پیش از آن شکل (string text, out int result) لازم بود. out همچنان باید در همهٔ مسیرهای بازگشت مقدار بگیرد؛ این کوتاه‌نویسی قرارداد پارامتر را عوض نمی‌کند. ورودی نامعتبر هم آزمایش شده و result مقدار صفر خروجی TryParse دارد. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions")

companion(52, '''var meter = new Meter(3);
int changes = 0;
meter.Changed += () => changes++;
meter.Value = 4;
Console.WriteLine($"{meter[0]},{changes}");

public partial class Meter
{
    public partial Meter(int value);
    public partial int Value { get; set; }
    public partial int this[int index] { get; }
    public partial event Action? Changed;
}
public partial class Meter
{
    Action? changed;
    public partial Meter(int value) { Value = value; }
    public partial int Value
    {
        get => field;
        set { field = value; changed?.Invoke(); }
    }
    public partial int this[int index] => index == 0 ? Value : throw new IndexOutOfRangeException();
    public partial event Action? Changed
    {
        add => changed += value;
        remove => changed -= value;
    }
}
''', "4,1",
"تعریف و پیاده‌سازی اعضا در دو بخش یک نوع partial قرار می‌گیرند؛ این‌ها دو کلاس مستقل نیستند. property و indexer جزئی از C# 13 و constructor و event جزئی از C# 14 پشتیبانی می‌شوند. event در بخش پیاده‌سازی add/remove واقعی دارد؛ constructor تنها یک پیاده‌سازی دارد. field در property، backing field تولیدشدهٔ کامپایلر در C# 14 است. با انتقال بخش دوم به فایل Meter.Implementation.cs رفتار عوض نمی‌شود. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/partial")

companion(99, '''ReadOnlySpan<int> numbers = [1, 2, 3];
var view = new BufferView(numbers);
Console.WriteLine(Read(view));
Console.WriteLine(Sum(numbers));
static int Read<T>(T buffer) where T : IBuffer, allows ref struct => buffer.Total();
static int Sum(scoped ReadOnlySpan<int> values)
{
    int sum = 0;
    foreach (int value in values) sum += value;
    return sum;
}
interface IBuffer { int Total(); }
readonly ref struct BufferView(ReadOnlySpan<int> values) : IBuffer
{
    readonly ReadOnlySpan<int> values = values;
    public int Total()
    {
        int sum = 0;
        foreach (int value in values) sum += value;
        return sum;
    }
}
''', "6\n6",
"از C# 13 یک ref struct می‌تواند interface را پیاده کند. تبدیل آن به متغیر IBuffer به boxing نیاز دارد و مجاز نیست؛ فراخوانی generic با قید IBuffer و allows ref struct این تبدیل را لازم ندارد. allows یک anti-constraint است و پیاده‌سازی generic باید قواعد ref safety را برای T رعایت کند. scoped پارامتر Sum مانع فرار span از فراخوانی است؛ مجوز طولانی‌تر کردن عمر stack نیست. فایل Escape.cs.txt عمداً خطای کامپایل دارد: آن را به Escape.cs تغییر نام بده و dotnet build را اجرا کن؛ بازگرداندن span روی stack محلی باید رد شود. سپس فایل را حذف کن. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/ref-struct",
files={"Escape.cs.txt": '''static class InvalidEscape
{
    public static ReadOnlySpan<int> Escape()
    {
        Span<int> local = stackalloc int[1];
        return local;
    }
}
'''})

companion(100, '''int[] numbers = [1, -2, 3];
Console.WriteLine(numbers.CountPositive());
ReadOnlySpan<int> view = numbers;
numbers[0] = 42;
Console.WriteLine(view[0]);
static class SpanExtensions
{
    public static int CountPositive(this ReadOnlySpan<int> values)
    {
        int count = 0;
        foreach (int value in values) if (value > 0) count++;
        return count;
    }
}
''', "2\n42",
"تبدیل‌های ضمنی Span در C# 14 در انتخاب receiver متد extension هم شرکت می‌کنند؛ آرایه مستقیماً CountPositive با receiver از نوع ReadOnlySpan<int> را صدا می‌زند. برای نسخه‌های قبلی می‌توان ابتدا متغیر ReadOnlySpan<int> ساخت و متد را روی آن فراخواند. view کپی آرایه نیست؛ فقط نوشتن از مسیر view محدود است. تغییر آرایهٔ اصلی عدد 42 را از همان view قابل خواندن می‌کند. از فقط‌خواندنی بودن، thread safety یا immutable بودن مالک نتیجه نگیر. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14")

companion(106, '''using System.Reflection;
using System.Runtime.CompilerServices;
Console.WriteLine(typeof(Numbers).GetCustomAttribute<ElementTypeAttribute<int>>()!.Type.Name);
int value = -1;
try { Guard(value > 0); }
catch (ArgumentException error) { Console.WriteLine(error.ParamName); }
static void Guard(bool condition, [CallerArgumentExpression(nameof(condition))] string? expression = null)
{
    if (!condition) throw new ArgumentException("Contract failed", expression);
}
[AttributeUsage(AttributeTargets.Class)]
sealed class ElementTypeAttribute<T> : Attribute { public Type Type => typeof(T); }
[ElementType<int>]
sealed class Numbers;
''', "Int32\nvalue > 0",
"generic attribute از C# 11 نوع اطلاعات را در آرگومان جنریک نگه می‌دارد؛ در محل استفاده نوع باید بسته و معتبر باشد، نه type parameter باز یک متد. این attribute روی کلاس اعمال شده و با reflection خوانده می‌شود. CallerArgumentExpression متن آرگومان فراخوانی Guard را در زمان کامپایل می‌گذارد؛ expression توسط متد دوباره ارزیابی نمی‌شود. caller می‌تواند پارامتر اختیاری را صریح بدهد، پس متن آن دادهٔ قابل اعتماد امنیتی نیست. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information")

companion(108, '''using System.Runtime.CompilerServices;
using System.Text;
Console.WriteLine(Render($"N={3}"));
Console.WriteLine(Api.Pick(1));
static string Render(TextHandler handler) => handler.ToString();
static class Api
{
    [OverloadResolutionPriority(1)]
    public static string Pick(long value) => "long";
    public static string Pick(int value) => "int";
}

[InterpolatedStringHandler]
public ref struct TextHandler
{
    readonly StringBuilder builder;
    public TextHandler(int literalLength, int formattedCount) => builder = new(literalLength);
    public void AppendLiteral(string value) => builder.Append(value);
    public void AppendFormatted<T>(T value) => builder.Append(value);
    public override string ToString() => builder.ToString();
}
''', "N=3\nlong",
"interpolated string handler از C# 10 تبدیل رشتهٔ interpolation به فراخوانی‌های AppendLiteral و AppendFormatted را ممکن می‌کند. این نمونه عمداً ساده است و StringBuilder تخصیص دارد؛ ادعای zero allocation ندارد. OverloadResolutionPriority از C# 13 اولویت انتخاب overload قابل اعمال را تغییر می‌دهد؛ overload long با اولویت 1 بر int با اولویت پیش‌فرض ترجیح می‌گیرد. این attribute برای نویسندهٔ API است؛ حذفش خروجی را به int تغییر می‌دهد و باید با آزمون سازگاری همراه باشد. مرجع: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13")
