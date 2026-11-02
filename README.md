# ILearn

کارگاه C# با Blazor Server، .NET 10 و ذخیرهٔ مستقل PostgreSQL برای هر حساب.

## ساختار

راهنمای قواعد، کنترل‌ها و امتیازدهی همهٔ بازی‌ها: [MINI-GAMES.md](MINI-GAMES.md).

گزارش نهایی بررسی ظاهر، رفتار و اندازه‌ها: [ممیزی UI/UX](verification/ux-audit-2026-11-02.md).

- `ILearn.Domain`: مدل‌ها و قواعد مستقل از میزبان.
- `ILearn.Application`: قرارداد سرویس‌ها، هویت جاری و قواعد یادگیری.
- `ILearn.Infrastructure`: PostgreSQL، راه‌اندازی schema، SMTP، محتوای فایل و Ollama.
- `ILearn`: رابط Blazor، مؤلفه‌های تمرین، endpointهای حساب و composition root.
- `ILearn.Tests`: تست قواعد، محتوا، بازبینی AI و جهت وابستگی‌ها.
- `verification`: ابزار حساب تست، SMTP محلی و بررسی مرورگر.

وابستگی لایه‌های داخلی به میزبان و دیتابیس ممنوع است. تراکنش‌های حساب و قفل‌های PostgreSQL در Infrastructure باقی می‌مانند. وضعیت نمایش لابی در code-behind و ورودی تمرین‌ها در مؤلفه‌های مستقل است.

## اجرا و بررسی

تنظیمات اتصال و حساب‌ها: [POSTGRES.md](POSTGRES.md). تنظیم SMTP و جریان تأیید: [EMAIL-SETUP.md](EMAIL-SETUP.md).

```powershell
dotnet run --project ILearn --urls http://127.0.0.1:5080
dotnet build
dotnet test
docker compose up -d --build
```

Docker روی پورت 5081 اجرا می‌شود. دادهٔ حساب‌ها در volume اختصاصی PostgreSQL است؛ پشتیبان آن با pg_dump گرفته می‌شود. کلیدهای کوکی و اعتبارنامهٔ حساب تست در App_Data باقی می‌مانند و وارد image نمی‌شوند.

ورود فقط با ایمیل است. ذخیره‌ساز فایل و ورود قدیمی حذف شده‌اند؛ فایل‌های قدیمی به حساب‌ها وارد نمی‌شوند. قالب JSON پیشرفت و شناسه‌های محتوا حفظ شده‌اند. بررسی Ollama اختیاری است و کد کاربر اجرا نمی‌شود.

پاکسازی حساب‌های تأییدشده یک‌باره و دستی است؛ هرگز هنگام اجرای برنامه انجام نمی‌شود. اسکریپت `verification/cleanup-approved-accounts.sql` به شناسه‌های مشخص محدود است و اجرای مجدد روی دادهٔ تغییرکرده را رد می‌کند.
