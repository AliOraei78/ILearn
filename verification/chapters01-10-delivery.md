# فصل‌های ۱ تا ۱۰ ILearn

محدوده: جلسه‌های S01 تا S10 در `csharp-mastery-roadmap.md`، همان فصل‌های شماره‌دار رابط پروژه. تاریخ بررسی: 2026-11-02.

## نتیجه

۱۰ فصل، هرکدام ۸ چالش و ۱۰۰ XP، پیاده‌سازی شده است. جمع مسیر: ۵۱ بازی با پاسخ‌نامه، ۱۹ تمرین توضیحی، ۱۰ کارگاه و ۱۰۰۰ XP. فصل‌های S01 تا S04 و S09 تا S10 تازه‌اند؛ S05 تا S08 با حفظ شناسه‌ها تکمیل شده‌اند. جلسه‌های S11 تا S44 در فهرست آینده باقی می‌مانند.

مقدمه اول مفهوم را با یک مثال کوچک توضیح می‌دهد؛ سپس بازیکن نتیجه را پیش‌بینی، شرط را اصلاح یا سرنخ‌ها را ترکیب می‌کند. تعریف‌ها در خدمت حل مسئله‌اند. هر معما راهنما، علت پاسخ و فرصت دوباره دارد. کارگاه‌ها پاسخ کد و معیار مشخص دارند؛ اجرای کد کاربر یا استفاده از مدل شرط تکمیل نیست. نمونه‌کدهای مرجع در بررسی مستقل کامپایل و اجرا شده‌اند.

## تطبیق با roadmap

| فصل | تمام مفاهیم خواسته‌شده | محل یادگیری و تمرین |
|---|---|---|
| S01 | CLI، CLR، CTS، CLS، BCL، JIT، IL/MSIL، AOT در برابر JIT، ساختار Assembly | مقدمه؛ `pipeline` مسیر ترجمه؛ `crew` مسئولیت‌ها؛ `assembly` شامل IL/metadata/manifest؛ `deployment`؛ `terminal`؛ دو روایت و اولین برنامه |
| S02 | نوع‌های پایه، Value/Reference، مقدمات Stack/Heap | `types`؛ `copies`؛ `memory` و پرهیز از ادعای «همهٔ value typeها روی Stack»؛ `alias`؛ `defaults`؛ تغییرناپذیری string و کارگاه دو کپی |
| S03 | Implicit/Explicit، is/as، Boxing/Unboxing و هزینه، Operators، Checked/Unchecked | `casts`؛ `boxed` و نوع دقیق Unboxing؛ `type-lab`؛ `operators`؛ `overflow` و فرق ثابت با متغیر؛ روایت‌ها و کارگاه تشخیص int |
| S04 | Pattern Matching Switch، Switch Expression، Guard Clause، Loops، Loop Invariant | `switch` با الگوهای مقایسه‌ای؛ `explain2` با الگوی نوع و when؛ `guards`؛ `loop`؛ `boundary`؛ `invariant`؛ توضیح while/do/while، break/continue/return و کارگاه foreach |
| S05 | in/out/ref، params Collections در C# 13، Overloading، Local Functions | `predict`، `keys`، `pairs`؛ `explain2` با نمونهٔ اجرایی params IEnumerable<int>، تابع محلی و دو overload؛ کارگاه محاسبهٔ مبلغ |
| S06 | Encapsulation، Access Modifiers، Classes، Constructors، Primary Constructors در C# 12/13 | مقدمه شامل شش سطح دسترسی و شکل‌های کلاس؛ `compiler`، `bug`، `sort`، `surgery`، `tests`؛ دو توضیح و نمونهٔ اجرایی primary constructor؛ کارگاه Order |
| S07 | virtual/override/new، abstract/sealed، Base Calling، VTable | پنج بازی تشخیص رفتار؛ `explain1` برای اطلاعات نوع و VTable؛ `explain2` با base.Name() و sealed override؛ مقدمهٔ base constructor؛ کارگاه سیاست تخفیف |
| S08 | Interface Segregation، Explicit Implementation، Default Interface Methods، Static Abstract Members | شش بازی قرارداد؛ روایت جداسازی؛ نمونهٔ کامپایل‌شدهٔ Make<T> با where T : IFactory<T> و T.Create() در مرجع `assessment`؛ کارگاه مخزن دو قراردادی |
| S09 | struct، تخصیص حافظه، خطر تغییرپذیری، readonly struct، Passing by ref | `value-copy`؛ `pass-lab`؛ `allocation`؛ `ref-key`؛ `readonly`؛ مثال آرایهٔ مشترک و کپی دفاعی in؛ کارگاه Position |
| S10 | record class/struct، Value-based Equality، with، Immutability | `equality`؛ `with-key`؛ `shallow`؛ `record-forms`؛ `array-equality`؛ دو روایت و کارگاه PlayerScore/Cell |

پیش‌نیازهای پیشنهادی در مقدمه‌اند. متن S08 مفاهیم محدودیت نوع در مثال static abstract را پیش از فصل ژنریک‌ها به‌اندازهٔ نیاز همان مثال توضیح می‌دهد. S03 هم out و قالب متد کارگاه را به‌عنوان خروجی معرفی می‌کند؛ تمرین کامل پارامترها در S05 است.

## حفظ داده و ساختار

- شناسه و ترتیب هشت چالش فصل‌های قبلی تغییر نکرده است. ذخیره‌های دارای Lesson قبلی همان فصل و XP را نگه می‌دارند؛ حساب تازه از S01 شروع می‌کند.
- فصل‌های جدید پاسخ و مقدمه را در خود `lessons.json` دارند. بازنویسی‌های قبلی `introductions.json` و `revisions.json` همچنان اعمال می‌شوند. اعتبار کامل محتوای بارگذاری‌شده در تست بررسی می‌شود.
- تعداد فصل‌های معرفی عمومی و موضوع‌ها از ICurriculum خوانده می‌شود؛ متن «چهار فصل» حذف شده است.
- چیدمان UI، موتور بازی، وابستگی‌های runtime، تنظیم حساب و schema دیتابیس تغییر نکرده‌اند. رنگ، فونت، کنترل‌ها و ENERGY 1 / RHYTHM 1 / MOTION 1 مطابق DESIGN.md حفظ شده‌اند.

## شواهد بررسی

| بررسی | نتیجهٔ ثبت‌شده |
|---|---|
| `dotnet build ILearn.slnx --no-restore -m:1 -nodeReuse:false` | PASS؛ صفر warning و صفر error |
| `dotnet test ILearn.slnx --no-build --no-restore -m:1` | PASS؛ ۸۴ تست، صفر شکست یا skip |
| `python verification/check-curriculum.py` | PASS؛ ۴۱ بررسی کامپایل/اجرا با محدودیت زبان C# 13، شامل همهٔ ۲۰ قطعهٔ کد مرجع و هر ۱۰ کارگاه |
| `dotnet publish ILearn/ILearn.csproj --no-restore -c Release -o ILearn/bin/CurriculumCheck -m:1` | PASS؛ اجرای مرورگر از خروجی کامل Production |
| `node verification/curriculum-flow.cjs` | PASS؛ ۸۲ گروه، شامل حل هر ۸۰ چالش در Chrome، باز کردن راهنما و مرجع، ۱۰ مقدمه، پاسخ ناقص، ۳۴ جلسهٔ آینده، reload، متن کارگاه، انتخابگر موبایل و XP تکرارنشدنی |
| چیدمان | PASS؛ ۳۶۸ اندازه‌گیری در عرض‌های ۳۲۰، ۳۹۰، ۷۶۸ و ۱۳۶۶؛ بدون بیرون‌زدگی سند یا کنترل و بدون هدف کوچک‌تر از ۴۳px |
| خطاهای مرورگر | PASS؛ صفر pageerror و صفر خطای بارگذاری فایل CSS/JS/font/SVG |
| پیشرفت | PASS؛ هر فصل ۸/۸، همهٔ فصل‌ها تکمیل‌شده و ۱۰۰۰ XP پس از reload؛ تکرار جواب XP اضافه نکرد |
| `docker compose up -d --build ilearn` | PASS؛ برنامهٔ محلی روی 5081 بازسازی شد و volume دیتابیس حفظ شد |
| `node verification/live-ux-smoke.cjs` | PASS؛ ۱۱ گروه و ۴۶ اندازهٔ صفحه، بدون خطای مرورگر؛ ده فصل در لابی، ورود حساب تست، مسیرهای قبلی و حفظ XP |
| محتوای کانتینر | PASS؛ SHA-256 هر سه فایل lessons/introductions/revisions با میزبان برابر است |

آزمون مرورگر با حساب ساخته‌شده فقط در دیتابیس مستقل `ilearn_curriculum_qa`، SMTP محلی ۲۵۲۹ و برنامهٔ QA روی `127.0.0.1:5085` انجام شد. کلیدها از `.env` خوانده شدند و در گزارش یا خروجی تست نیامدند. هیچ ایمیل واقعی ارسال نشد.

فایل‌های محلی نتیجه: `verification/curriculum-examples.json` و `verification/curriculum-browser.json`. این خروجی‌ها مانند اسکرین‌شات‌ها در gitignore هستند. نمونه‌های بصری بررسی‌شده: `curriculum-S10-shallow-mobile.png` و `curriculum-complete-desktop.png`. گزارش‌های قدیمی، نتایج زمان خودشان را ثبت می‌کنند؛ محدودهٔ چهار فصل برای `ui-audit.cjs` صریح نگه داشته شده و بررسی جامع ده فصل در اسکریپت جدید است.

برای تکرار مرورگر، خروجی publish را با ConnectionStrings__ILearn متصل به یک دیتابیس جدا، تنظیم SMTP محلی بالا و Auth__CodeKey معتبر اجرا کن؛ سپس اسکریپت مرورگر را بزن. PLAYWRIGHT_MODULE مسیر نصب موجود Playwright را عوض می‌کند. ILEARN_CURRICULUM_URL هم نشانی برنامهٔ QA را تعیین می‌کند.

## مبنای صحت مفاهیم

توضیح‌ها با مستندات اصلی و اجرای کد تطبیق داده شدند: [مسیر اجرای مدیریت‌شده](https://learn.microsoft.com/en-us/dotnet/standard/managed-execution-process)، [سیستم نوع‌ها](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/)، [پارامترها و params](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/method-parameters)، [struct](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct) و [record](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record).

بررسی فعلی صحت همهٔ متن‌های آزاد کاربر را تضمین نمی‌کند؛ تکمیل شخصی انتخاب بازیکن است و مدل اختیاری ممکن است اشتباه کند. آزمایش جدید روی Chrome ویندوز انجام شده؛ دستگاه واقعی، Firefox و Safari در این نوبت بررسی نشده‌اند.
