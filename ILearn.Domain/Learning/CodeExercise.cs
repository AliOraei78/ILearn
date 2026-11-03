namespace ILearn.Domain.Learning;

public sealed class CodeExercise
{
    public string Subject { get; set; } = "";
    public string Technology { get; set; } = "";
    public string LessonId { get; set; } = "";
    public string LessonTitle { get; set; } = "";
    public string Template { get; set; } = "";
    public Activity Activity { get; set; } = new();
    public string Key => $"code-room/{Subject}/{LessonId}/{Activity.Id}";
    public int Xp => 10;
}

public sealed record CodeTemplate(string Id, string Title, string Description, bool CodeAnswer = true);

public static class CodeRoomTemplates
{
    public static IReadOnlyList<CodeTemplate> All { get; } =
    [
        new("repair", "رفع اشکال", "خرابی کد را برطرف کن و علت آن را توضیح بده."),
        new("optimize", "بهینه‌سازی", "با حفظ نتیجه، هزینه مشخص‌شده در مسئله را کاهش بده."),
        new("predict", "پیش‌بینی اجرا", "خروجی یا وضعیت نهایی داده‌ها را از روی کد تعیین کن."),
        new("implement", "ساخت از روی نیازمندی", "برای قرارداد داده‌شده، راه‌حل کامل بنویس."),
        new("complete", "تکمیل قطعه گمشده", "بخش حذف‌شده را با حفظ ساختار موجود تکمیل کن."),
        new("reorder", "مرتب‌سازی قطعه‌ها", "قطعه‌های ثابت را با رعایت وابستگی‌ها مرتب کن."),
        new("find-input", "پیدا کردن ورودی", "بدون تغییر کد، ورودی مناسب برای نتیجه هدف بساز."),
        new("design-tests", "تست‌سازی", "ورودی تست و نتیجه مورد انتظار هر مورد را بنویس."),
        new("infer-rule", "کشف قانون پنهان", "قاعده سازگار با نمونه‌ها را پیدا و پیاده‌سازی کن."),
        new("translate", "تبدیل شیوه پیاده‌سازی", "منطق موجود را با حفظ معنا در ابزار مقصد بازنویسی کن."),
        new("extract-contract", "استخراج قرارداد", "شرایط لازم و تضمین‌های رفتار کد را استخراج کن.", false),
        new("compare-solutions", "انتخاب و دفاع از راه‌حل", "راه‌حل مناسب شرایط مسئله را انتخاب و دلیل فنی بیاور.", false)
    ];

    public static CodeTemplate? Find(string id) => All.FirstOrDefault(template => template.Id == id);
}
