# نقشهٔ راه جامع C# از صفر تا پیشرفته

نسخهٔ سند: ۲.۰ · تاریخ بازنگری: ۲۰۲۶-۱۱-۰۲ · مبنای فنی: C# 14 و .NET 10

این مسیر برای کسی نوشته شده که پیش‌تر برنامه‌نویسی نکرده است. آموزش از ساخت و اجرای اولین برنامه شروع می‌شود و تا طراحی کتابخانه، همزمانی، مدیریت حافظه، تولید کد و ارتباط با کد بومی ادامه دارد. کار با فایل، پوشه، ویرایشگر و خط فرمان نیز در شروع مسیر توضیح داده می‌شود.

«مبتنی بر استاندارد» در این سند یعنی تطبیق مباحث زبان با مرجع رسمی C#، استفاده از قابلیت‌های پایدار و رعایت پیش‌نیازهای آموزشی. Microsoft ترتیب اجباری یا تعداد فصل مشخصی برای یک دورهٔ کامل تعیین نکرده است؛ ترتیب و تمرین‌های این سند، طراحی آموزشی ILearn هستند. مبنای نسخهٔ پایدار، [C# 14 روی .NET 10](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) است.

<a id="scope"></a>
## ۱. دامنه، سطح‌ها و معنای جامعیت

- پیش‌نیاز برنامه‌نویسی: ندارد. اصطلاح فنی، مخفف و شکل تازهٔ نوشتن کد باید هنگام اولین استفاده توضیح داده شود.
- هستهٔ مسیر: زبان C#، کتابخانه‌های پرکاربرد .NET، حل مسئله، اشکال‌زدایی، آزمون، ساخت و انتشار، async، CLR، حافظه و قابلیت‌های پیشرفتهٔ زبان.
- گسترش حرفه‌ای: اصول طراحی، الگوهای طراحی و معماری در فاز ۱۱. این بخش پس از هستهٔ زبان می‌آید و پیش‌نیاز شروع یادگیری نیست.
- جامعیت یعنی پوشش خانواده‌های اصلی قابلیت‌های پایدار C# و توانایی استفاده و ارزیابی آن‌ها. حفظ تمام overloadها و APIهای .NET هدف دوره نیست.
- ASP.NET Core، EF Core و SQL، Blazor، MAUI، Unity، رایانش توزیع‌شده و زیرساخت ابری مسیرهای تخصصی بعدی هستند. پیش‌نیاز عمومی این دوره محسوب نمی‌شوند.
- قابلیت‌های Preview، Experimental و جزئیات مستندنشدۀ runtime، معیار اتمام این نسخه نیستند. هنگام خواندن مرجع آنلاین باید برچسب نسخه بررسی شود.

این نقشه ۱۲۳ واحد آموزشی در ۱۱ فاز دارد. هر واحد می‌تواند چند درس کوتاه داشته باشد؛ تعداد تمرین‌ها از هدف آموزشی می‌آید و به هشت چالش محدود نیست.

| سطح | واحدها | نتیجهٔ قابل مشاهده |
|---|---|---|
| شروع از صفر و مبانی | R001 تا R020 | ساخت یک برنامهٔ کنسول با ورودی معتبر، شرط، حلقه، آرایه و متد |
| شیءگرایی پایه | R021 تا R032 | مدل‌سازی مسئله با کلاس، قرارداد، مدیریت خطا و آزمون |
| زبان مدرن و کاربرد روزمره | R033 تا R079 | انتخاب نوع مناسب، جنریک، LINQ، ذخیره‌سازی و کتابخانه‌های پایه |
| زبان و runtime پیشرفته | R080 تا R114 | کد async، همزمانی کنترل‌شده، تحلیل حافظه، تولید کد، interop و انتشار |
| طراحی و تحویل حرفه‌ای | R115 تا R123 | تصمیم معماری مستدل، آزمون و تحویل یک پروژهٔ قابل نگهداری |

<a id="current-state"></a>
## ۲. تفاوت برنامهٔ آموزشی با پیاده‌سازی فعلی

کدهای R001 تا R123، ترتیب آموزشی نسخهٔ دوم این سند هستند. کدهای S01 تا S44، شناسه‌های نسخهٔ قبلی در برنامه و فایل‌های پیشرفت‌اند. این دو مجموعه نباید هم‌معنی فرض شوند.

در پیاده‌سازی نسخهٔ دوم، تمام R001 تا R123 در ۱۱ فاز و ۵۰۵ چالش به برنامه افزوده شده‌اند. محتوای اجرایی در [csharp-v2.json](ILearn/Content/csharp-v2.json)، تطبیق واحدها در [ماتریس پوشش](verification/csharp-v2-coverage.md) و شواهد بررسی در [گزارش تحویل](verification/csharp-v2-delivery.md) قرار دارد. فهرست فازها و جست‌وجو، دسترسی به تمام واحدها را فراهم می‌کنند.

S01 تا S10 با ۸۰ چالش و ۱۰۰۰ XP در آرشیو مسیر قبلی حفظ شده‌اند. S11 تا S44 عنوان‌های کاتالوگ تاریخی‌اند و واحدهای مسیر جدید با آن‌ها جایگزین عددی نشده‌اند. جدول تطبیق انتهای سند همچنان رابطهٔ آموزشی دو نسخه را نشان می‌دهد؛ تکمیل واحد S به‌معنای تکمیل R نیست.

حساب تازه از R001 آغاز می‌کند. حساب موجود می‌تواند درس S قبلی را ادامه دهد؛ کلیدهای قبلی پیشرفت تغییر نکرده‌اند. مسیر R دارای ۷۵۱۰ XP مستقل است و XP معتبر آرشیو به مجموع آن اضافه می‌شود. تغییر ترتیب آموزش با مهاجرت حدسی شناسه‌ها انجام نشده است. ثبت شخصی یا امتیاز، همچنان مدرک صحت کد و تسلط عملی نیست.

<a id="method"></a>
## ۳. روش آموزش و عبور از هر واحد

ترتیب یادگیری، ابتدا عملی و سپس تحلیلی است. مانند [آموزش رسمی بدون پیش‌نیاز Microsoft](https://learn.microsoft.com/en-us/training/paths/get-started-c-sharp-part-1/)، یادگیرنده پیش از ورود به جزئیات CLR با کد ساده کار می‌کند.

1. هدف مشخص: یادگیرنده بداند در پایان چه کاری می‌تواند انجام دهد.
2. توضیح کوتاه و واژه‌نامه: مفهوم جدید با فارسی ساده، بدون فرض دانش آموزش‌داده‌نشده معرفی شود.
3. مثال هدایت‌شده: ابتدا یک نمونهٔ کوچک با ورودی و خروجی روشن بررسی شود.
4. تمرین تدریجی: پیش‌بینی خروجی، اصلاح خطا و نوشتن کد مستقل؛ نوع تمرین با مبحث انتخاب شود.
5. بازخورد: علت پاسخ، خطای رایج و راهنمای مرحله‌ای ارائه شود؛ مرجع پاسخ جای تمرین مستقل را نگیرد.
6. ارزیابی مستقل: یک مسئله با داده یا شرط تازه حل شود و موارد مرزی مرتبط بررسی شوند.
7. مرور: مطالب قبلی در پروژهٔ فاز بعد دوباره استفاده شوند. سؤال مصاحبه پس از آموزش، ابزار مرور است.

پیش‌نیاز عمومی هر واحد، واحدهای قبلی مسیر است. ستون «پیش‌نیاز کلیدی» موارد ضروری برای مرور را نشان می‌دهد؛ هیچ کد آن ستون نباید به واحد آینده اشاره کند. برای مسیرهای جبرانی می‌توان از همین ستون و جدول تطبیق استفاده کرد.

استفاده از API آماده مجاز است، به شرط اینکه شکل فراخوانی و قرارداد لازم همان‌جا توضیح داده شود. برای نمونه، مفهوم سادهٔ Attribute با `[Flags]` معرفی می‌شود و ساخت Attribute و خواندن metadata بعداً آموزش داده می‌شود. شناخت نتیجهٔ یک قابلیت و بررسی پیاده‌سازی داخلی آن، دو عمق متفاوت‌اند.

معیار پایان واحد: توضیح مفهوم با مثال خود یادگیرنده، حل مستقل تمرین، بررسی حالت‌های مرزی و در کارگاه کدنویسی، کامپایل و اجرای واقعی کد. پیش از R031 می‌توان از خروجی مشخص و assert استفاده کرد؛ پس از آن آزمون خودکار هم لازم است.

بررسی AI کمک آموزشی است و ممکن است خطا کند. در محصول فعلی کد کاربر اجرا نمی‌شود؛ «ثبت شخصی تکمیل» و دریافت XP به‌تنهایی مدرک صحت کد یا تحقق اهداف این نقشه نیستند. ارزیابی اجرای کد باید در محیط توسعهٔ یادگیرنده یا اجراگر ایزولهٔ آموزشی انجام شود.

<a id="phase-01"></a>
## فاز ۱. اولین برنامه، داده و جریان اجرا

خروجی فاز: خواندن کد کوتاه، پیش‌بینی نتیجه و ساخت برنامه‌ای با داده‌های ساده، شرط، حلقه و آرایه. مرجع شروع: [اولین برنامهٔ C#](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/tutorials/hello-world).

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R001 | محیط کار و اولین برنامه | ندارد | فایل و پوشه، ویرایشگر و ترمینال، نصب SDK، تفاوت SDK و Runtime، ساخت پروژهٔ console، top-level statements، `Console.WriteLine`، `dotnet new/build/run` | پروژهٔ تازه بساز، پیام را تغییر بده و خروجی build و run را تشخیص بده |
| R002 | خواندن و نوشتن نحو | R001 | statement و expression، شناسه و keyword، حساسیت به بزرگی حروف، literal، سمی‌کالن و بلوک، comment، قالب‌بندی، خواندن اولین diagnostic | چند خطای نوشتاری را از پیام کامپایلر پیدا و اصلاح کن |
| R003 | متغیر، نوع و مقدار اولیه | R002 | تعریف و انتساب، `var` و نوع ثابت، `const`، محدودهٔ سادهٔ متغیر، definite assignment، `default`، انواع عددی، `bool` و `char`، تفاوت مقدار با نام متغیر | جدول مقدار متغیرها را پس از چند انتساب بساز؛ متغیر مقداردهی‌نشده را اصلاح کن |
| R004 | عدد، محاسبه و تبدیل عددی | R003 | عملگرهای حسابی و انتساب مرکب، افزایش و کاهش، اولویت و شرکت‌پذیری، تقسیم صحیح، `%`، literalهای عددی، implicit/explicit، افت دقت، `checked/unchecked`، تفاوت سرریز integer و decimal | محاسبهٔ قیمت و تقسیم را برای ورودی مرزی و تبدیل دارای افت داده بررسی کن |
| R005 | متن و نویسه | R003 | `string` و `char`، اتصال متن، escape از جمله `\e`، متن verbatim و raw، interpolation، قالب عدد، طول متن، تغییرناپذیری سادهٔ string، فرق متن خالی و فاصله | رسید متنی چندخطی بساز و قالب عدد و نویسه‌های escape را توضیح بده |
| R006 | مقایسه و منطق | R004, R005 | `bool`، برابری و مقایسهٔ پایه، منطق AND/OR/NOT، short-circuit، اثر جانبی در شرط، پرانتز، عملگر شرطی `?:` | شرط اعتبار چند ورودی را بنویس و با جدول حالت‌ها آزمایش کن |
| R007 | انتخاب مسیر | R006 | `if/else if/else`، شرط‌های تو‌در‌تو، switch با مقدار ثابت، `case/default`، تفاوت switch statement با expression ساده، دسترسی‌پذیری شاخه‌ها | قوانین تخفیف را با if و switch پیاده کن و مرز هر بازه را بررسی کن |
| R008 | تکرار | R007 | `for`، `while`، `do/while`، `break/continue`، حلقهٔ بی‌پایان، شمارنده و جمع‌کننده، ردگیری دستی اجرا | جمع و شمارش بازه را با سه شکل حلقه بساز و شرط اولیهٔ false را مقایسه کن |
| R009 | آرایهٔ یک‌بعدی | R008 | تعریف و مقداردهی آرایه، index صفرمبنا، `Length`، خواندن و تغییر عنصر، مرز معتبر، آرایهٔ خالی، اشارهٔ مقدماتی به اشتراک آرایه | کوچک‌ترین و بزرگ‌ترین مقدار را بدون LINQ پیدا کن؛ ورودی خالی را مشخص کن |
| R010 | پیمایش و آرایه‌های بیشتر | R009 | `foreach`، حلقهٔ تو‌در‌تو، آرایهٔ مستطیلی و jagged، Index و Range با `^` و `..`، تفاوت تغییر عنصر با تغییر متغیر پیمایش | جدول امتیاز دوبعدی را پردازش کن و خطای off-by-one را با ورودی مرزی پیدا کن |

<a id="phase-02"></a>
## فاز ۲. متد، ورودی معتبر و حل مسئله

خروجی فاز: یک برنامهٔ کنسول مستقل بدون نیاز به شیءگرایی یا LINQ. مبنای مباحث پایه، [انواع توکار](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types) و [عملگرهای C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/) است.

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R011 | متد و تجزیهٔ مسئله | R010 | ورودی و خروجی، `void/return`، ارسال مقدار، scope و طول عمر محلی، local function، overload، پارامتر نام‌دار و اختیاری، expression-bodied method | برنامهٔ محاسبهٔ مبلغ را به متدهای کوچک تقسیم کن؛ overload نامعتبر را تشخیص بده |
| R012 | قرارداد پارامترها | R011 | `ref/out/in` در تعریف و فراخوانی، مقداردهی الزامی out، discard، `params` آرایه‌ای، ترتیب ارزیابی آرگومان، محدودیت overload با ref و out | متد Try با خروجی out بنویس و ارسال کپی را با ref مقایسه کن |
| R013 | نبود مقدار و null پایه | R011 | مفهوم `null`، فرق با صفر و متن خالی، `int?` و nullable value type، `HasValue/Value`، `string?` در حد خواندن ورودی، بررسی null، `?.`، `??` و `??=` | ورودی اختیاری را بدون dereference ناامن پردازش کن و حالت نبود مقدار را مستقل نگه دار |
| R014 | ورودی کنسول و اعتبارسنجی | R012, R013 | `Console.ReadLine`، پایان ورودی، `Parse/TryParse/Convert`، تبدیل متن و عدد، culture در ورودی عدد، guard clause، حلقهٔ تلاش مجدد، مرز معتبر | برنامه‌ای بساز که ورودی نامعتبر و پایان ورودی را مدیریت کند و قیمت صفر را اشتباه رد نکند |
| R015 | خطا و اشکال‌زدایی اولیه | R014 | تفاوت خطای compile/runtime/logic، breakpoint، step و watch، stack trace، `try/catch/finally` پایه، بازپرتاب، assert، محدود کردن catch به خطای قابل رسیدگی | خطای منطق و خطای اجرا را جدا تشخیص بده و بدون پنهان کردن خطا اصلاح کن |
| R016 | ساختار پروژه و نام‌ها | R001, R011 | `.csproj` و solution، TFM، namespace معمول و file-scoped، `using` و alias، global/implicit/static using، `nameof`، reference و package پایه، restore، Debug/Release، `global.json`، مقدمهٔ file-based apps و directiveهای آن | کد را در چند فایل و namespace سازمان بده و وابستگی یک package را شناسایی کن |
| R017 | کار عملی با متن | R005, R010, R013 | جست‌وجو و برش، `Split/Join/Replace/Trim`، مقایسهٔ Ordinal و culture، `StringBuilder`، قالب‌بندی و parse متن، مقدمات UTF-16 و فرق char با نویسهٔ دیده‌شده | متن ورودی را پاک‌سازی و خروجی را بدون اتکا به تنظیم زبان دستگاه مقایسه کن |
| R018 | enum، بیت و Flags | R004, R007 | `enum` و مقدار زیرین، cast و اعتبار مقدار، عملگرهای بیتی و shift از جمله `>>>`، ماسک بیت، `[Flags]`، مفهوم سادهٔ Attribute آماده، تفاوت bitwise و short-circuit | مجوزهای ترکیبی بساز و بیت‌های مجاز، ناشناخته و مقدار صفر را آزمایش کن |
| R019 | الگوریتم و سنجش درستی | R010, R011, R015 | شکستن مسئله، ردگیری، ناوردای حلقه، جست‌وجوی خطی و دودویی، مرتب‌سازی ساده، بازگشت و شرط توقف، رشد زمان و حافظه در حد مقدماتی، شناخت label و `goto` برای خواندن کد قدیمی | الگوریتم جست‌وجو را روی خالی، تک‌عضوی و دادهٔ مرزی بررسی و هزینهٔ آن را توضیح بده |
| R020 | پروژهٔ مبانی | R014, R017, R018, R019 | ترکیب ورودی، شرط، حلقه، آرایه و متد؛ نام‌گذاری، جداسازی محاسبه از نمایش، اعتبارسنجی و آزمون مرزی | دفتر امتیاز یا ماشین‌حساب منودار بساز؛ همهٔ مسیرهای خروج و ورودی نامعتبر را بررسی کن |

<a id="phase-03"></a>
## فاز ۳. کلاس، شیءگرایی و کد قابل آزمون

خروجی فاز: مدل کوچک دارای دادهٔ معتبر و وابستگی قابل تعویض. مرجع زبان: [اعضای کلاس، struct و record](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/).

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R021 | کلاس و شیء | R020 | تعریف class، field و method، نمونه‌سازی با `new`، `this`، عضو instance و static، مرجع و اشتراک شیء، ساختار صریح `Main(string[] args)` در کنار top-level | دو شیء مستقل بساز و اشتراک یک مرجع را با ساختن شیء تازه مقایسه کن |
| R022 | property و مقداردهی | R021 | `get/set`، auto-property، سطح دسترسی accessor، `init/required`، object initializer، target-typed new، field در برابر property، `readonly` و `static readonly`، property دارای `field` در C# 14 | مدل سفارش بساز که خواندن مبلغ آزاد و تغییر آن کنترل‌شده باشد؛ محدودیت init را نشان بده |
| R023 | سازنده و چرخهٔ ساخت | R022 | سازندهٔ معمول و overload، زنجیرهٔ `this(...)`، سازندهٔ static، ترتیب مقداردهی، primary constructor، capture پارامتر و تفاوت آن با property، رد ورودی نامعتبر هنگام ساخت | مدل را با سازندهٔ معمول و primary constructor بنویس و قرارداد ورودی یکسان را حفظ کن |
| R024 | دسترسی پایه و سازمان نوع‌ها | R016, R023 | public/private/internal، دسترسی نوع و عضو، nested type، static class، محدودیت نوع file-local و تفاوت آن با access modifier؛ modifierهای وابسته به وراثت در R026 تکمیل می‌شوند | مرز دسترسی را با دو پروژه بررسی کن؛ خطاهای کامپایل مورد انتظار را ثبت کن |
| R025 | کپسوله‌سازی و ترکیب | R023, R024 | invariant شیء، رفتار در کنار داده، composition و aggregation، شیء تغییرپذیر و تغییرناپذیر، افشای دادهٔ داخلی، وابستگی صریح | حساب یا سبدی بساز که تغییر نامعتبر از بیرون و از متدهای خودش ممکن نباشد |
| R026 | وراثت و تبدیل مرجع | R025 | رابطهٔ is-a، یک کلاس پایه، `base` و سازندهٔ پایه، protected/protected internal/private protected، upcast/downcast، `is/as`، `object`، تفاوت تبدیل نوع با ساخت شیء | یک سلسله‌مراتب کوچک بساز؛ دسترسی فرزند در دو assembly و تبدیل موفق، ناموفق و null را بررسی کن |
| R027 | چندریختی | R026 | `virtual/override/new`، dispatch بر اساس نوع واقعی، abstract class/member، sealed class/override، covariant return، خطر فراخوانی virtual در سازنده | رفتار یک مرجع پایه را در override و hiding مقایسه کن؛ رفتار پایه را با base گسترش بده |
| R028 | اینترفیس پایه | R027 | قرارداد، پیاده‌سازی ضمنی و صریح، چند interface، قرارداد کوچک، تفاوت با abstract class، default interface method، محدودیت دسترسی از مرجع کلاس | یک مصرف‌کنندهٔ فقط‌خواندنی را با دو پیاده‌سازی قابل تعویض آزمایش کن |
| R029 | استثنا و قرارداد خطا | R015, R028 | سلسله‌مراتب Exception، custom exception، `throw` expression، `throw;` در برابر `throw ex;`، filter با `when`، حفظ علت خطا، مرز catch، انتخاب Try در برابر exception | خطای دامنه را با خطای ورودی و خطای زیرساخت جدا کن و stack trace را حفظ کن |
| R030 | مالکیت منبع و using | R028, R029 | `IDisposable`، `using` statement/declaration، `try/finally`، مالکیت و زمان آزادسازی، فرق using برای namespace و منبع، تفاوت آزادسازی منبع با پاک‌سازی حافظه | یک منبع آزمایشی را در مسیر موفق و استثنا دقیقاً طبق قرارداد آزاد کن |
| R031 | آزمون واحد اولیه | R029, R030 | پروژهٔ تست، یک چارچوب مانند xUnit، Arrange/Act/Assert، assert مناسب، دادهٔ مرزی و تست پارامتری، fake ساده، اجرای `dotnet test`، تفکیک تست منطق از I/O | برای قوانین مدل تست بنویس و ثابت کن یک تغییر اشتباه در شرط، تست را خراب می‌کند |
| R032 | پروژهٔ شیءگرایی | R025, R028, R031 | مدل، قرارداد، validation، خطا، ترکیب و چندریختی متناسب با نیاز؛ استفاده از آرایه در صورت نیاز به چند شیء | سامانهٔ کوچک سفارش یا موجودی بساز؛ مصرف‌کننده را بدون تغییر منطق با fake آزمایش کن |

<a id="phase-04"></a>
## فاز ۴. سیستم نوع، null و مدل‌سازی داده

خروجی فاز: انتخاب آگاهانهٔ class، struct، record و قرارداد برابری. مبنای ایمنی null، [Nullable Reference Types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types) است؛ NRT بررسی زمان کامپایل است و جای validation ورودی را نمی‌گیرد.

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R033 | رفتار انواع و Boxing | R026, R032 | value/reference semantics، کپی مقدار و کپی مرجع، aliasing، `object`، `typeof/GetType`، Boxing/Unboxing با نوع دقیق، object در برابر var، مقدمهٔ محل نگهداری بدون قانون نادرست stack/heap | کپی عدد، آرایه و شیء را ردگیری کن و unboxing نامعتبر را از تبدیل عددی جدا کن |
| R034 | struct و readonly | R033 | struct و محدودیت وراثت، field/property/method، interface، سازنده و default، parameterless constructor و auto-default، readonly struct/member، کپی دفاعی در in، خطر struct تغییرپذیر | تفاوت new و default و تغییر کپی struct دارای عضو مرجعی را با تست نشان بده |
| R035 | record و immutability | R022, R034 | record class/struct و readonly record struct، عضو موقعیتی، init، equality تولیدشده، `with`، کپی سطحی، محدودیت وراثت رکورد، تغییرپذیری اعضای تو‌در‌تو | مدل امتیاز و مختصات بساز؛ برابری داده را از یکسان بودن مرجع جدا کن |
| R036 | null پیشرفته و قرارداد API | R013, R035 | NRT context و flow analysis، nullable value در برابر reference annotation، `!`، `?.` و `?[]`، `??/??=`، عملگرهای lifted، null-conditional assignment در C# 14، annotationهای آمادهٔ nullability | هشدار null را با اصلاح قرارداد برطرف کن؛ نشان بده `!` کنترل زمان اجرا نیست |
| R037 | Tuple، Deconstruction و نوع بی‌نام | R011, R035 | ValueTuple و نام عنصرها، خروجی چندمقداری، tuple equality، `Deconstruct`، discard، anonymous type و مرز استفاده، alias برای انواع از جمله tuple در C# 12 | خروجی چندمقداری را دریافت کن و مدل دارای Deconstruct را بدون از دست دادن قرارداد باز کن |
| R038 | Pattern Matching کامل | R018, R036, R037 | constant/type/declaration، relational، property و extended property، positional/tuple، list/slice، `and/or/not`، var/discard، guard با when، switch expression و پوشش حالت‌ها | قوانین چندنوعی را با pattern بنویس؛ null، مرز عددی و حالت پوشش‌داده‌نشده را بررسی کن |
| R039 | برابری و Hash Code | R033, R035 | `ReferenceEquals`، static/virtual Equals، `==/!=`، قرارداد GetHashCode، برابری struct و record، اثر عضو آرایه، کلید تغییرپذیر، برابری ترتیبی و محتوایی | یک value object را طوری آزمایش کن که برابری، تقارن، تعدی و سازگاری hash رعایت شود |
| R040 | عملگر و تبدیل سفارشی | R034, R039 | operator overload، جفت‌های مقایسه، implicit/explicit، checked user-defined operator/conversion، true/false، عملگرهای compound assignment و instance increment/decrement در C# 14 | نوع عددی کوچک بساز؛ تبدیل دارای افت داده و رفتار checked را با آزمون مشخص کن |
| R041 | Indexer و مقداردهی عضوها | R022, R034 | `this[...]`، get/set و overload indexer، object و index initializer، تفاوت مقداردهی property و indexer، implicit Index در initializer در C# 13، تفاوت indexer با آرایه | کلاس چندخانه‌ای بساز که محدودهٔ index را بررسی کند و initializer آن روشن باشد |
| R042 | dynamic و اتصال دیرهنگام | R026, R033 | تفاوت dynamic با var و object، runtime binding، خطای دیرهنگام، مرز استفاده در interop، هزینهٔ از دست رفتن بررسی ایستا | یک فراخوانی نامعتبر را یک بار با نوع ایستا و یک بار با dynamic مقایسه کن |
| R043 | پروژهٔ مدل‌سازی داده | R036, R038, R039, R040, R041 | انتخاب نوع بر اساس معنا، null و خطا، کپی عمیق و سطحی، برابری و تغییرناپذیری، تست قرارداد | مدل مبلغ یا مختصات بساز و انتخاب class/struct/record و قرارداد برابری را مستند کن |

<a id="phase-05"></a>
## فاز ۵. جنریک، رفتار تابعی و قراردادهای پیشرفته

خروجی فاز: ساخت اجزای type-safe و ترکیب رفتار بدون تکرار کد. مرجع: [Generic Types and Methods](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics).

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R044 | استفاده و ساخت Generics | R028, R043 | type parameter/argument، کلاس و متد جنریک، inference، چند پارامتر، default(T)، nameof برای جنریک باز در C# 14، ایمنی نوع، تفاوت با object و نبود تضمین حذف همهٔ allocationها | دو پیاده‌سازی تکراری را به یک جزء جنریک تبدیل کن و خطای نوع را هنگام کامپایل نشان بده |
| R045 | Constraints | R044 | where، class/class?، struct، notnull، new()، base/interface، Enum، unmanaged در حد طبقه‌بندی نوع، ترتیب constraintها، محدودیت دسترسی عضو، default constraint در override | کمترین constraint لازم را انتخاب کن و چند نوع نامعتبر را به‌عنوان تست کامپایل بررسی کن |
| R046 | مقایسهٔ جنریک | R039, R045 | `IEquatable<T>`، `IComparable<T>`، `IEqualityComparer<T>`، `IComparer<T>`، Comparer/EqualityComparer پیش‌فرض، string comparer، ارتباط equality و hashing | یک نوع را با دو سیاست مرتب‌سازی و یک سیاست برابری تست کن |
| R047 | Static Abstract و Generic Math | R028, R045 | static abstract/static virtual در interface، فراخوانی از type parameter، self-referential constraint، `INumber<T>`، فرق dispatch ایستا و مجازی روی نمونه | الگوریتم عددی جنریک یا factory جنریک بساز و نوع ناسازگار را در کامپایل رد کن |
| R048 | Delegate و Method Group | R044 | delegate سفارشی، method group، Func/Action/Predicate، پارامتر و خروجی، invocation list، multicast و رفتار خطا/مقدار برگشتی، constraint از نوع Delegate | رفتار محاسبه را به متد بده و نتیجهٔ چند subscriber یا چند delegate را بررسی کن |
| R049 | Lambda و Closure | R011, R048 | lambda بیانی و بلوکی، anonymous method، type inference و natural type، پارامتر اختیاری lambda، static lambda/local function، capture و دام حلقه، modifiers پارامتر ساده در C# 14 | خطای capture را پیدا کن و نمونهٔ بدون capture را با حفظ رفتار بنویس |
| R050 | Event و عمر اشتراک | R030, R048, R049 | event در برابر delegate، EventHandler، publisher/subscriber، add/remove سفارشی، unsubscribe، نگهداری ناخواستهٔ subscriber، invocation و محدودیت thread safety | notifier بساز که مشترک جداشده را صدا نزند و مالک اشتراک روشن باشد |
| R051 | Variance | R044, R048 | covariance/contravariance/invariance، in/out در interface و delegate، تفاوت با ref/out پارامتر، محدودیت reference type، خطر covariance آرایه | تبدیل producer و consumer را بررسی کن و نمونهٔ نامعتبر را توضیح بده |
| R052 | Partial و تنظیم کامپایل | R016, R023, R041, R050 | partial type/method، partial property/indexer در C# 13، partial constructor/event در C# 14، تفاوت declaration و implementation، `#if/#define/#undef`، `#nullable/#pragma/#line/#error/#warning/#region` | یک نوع را بین دو فایل تقسیم کن و دو پیکربندی ساخت قابل پیش‌بینی داشته باش |
| R053 | Extension Methods و Members | R024, R044, R049 | static class و receiver با this، extension جنریک، قواعد انتخاب عضو، instance member در برابر extension، extension block و property/static member/operator در C# 14 | یک API موجود را بدون تغییر نوع اصلی توسعه بده و تعارض نام عضو را بررسی کن |
| R054 | ترکیب رفتار و سبک تابعی | R035, R048, R049, R053 | تابع خالص، side effect، higher-order function، composition، دادهٔ تغییرناپذیر، مدل سادهٔ Result/Option با نوع‌های آموخته‌شده، تصمیم دربارهٔ exception در برابر نتیجه | زنجیرهٔ validation بساز و نتیجهٔ شکست را بدون پنهان کردن علت برگردان |
| R055 | پروژهٔ اجزای قابل استفادهٔ مجدد | R046, R047, R050, R054 | ترکیب جنریک، قرارداد، delegate، event و extension؛ استفاده از آرایه پیش از کالکشن‌های تخصصی | موتور قوانین کوچک بساز؛ رفتار قابل تعویض و جداسازی subscriber را با تست نشان بده |

<a id="phase-06"></a>
## فاز ۶. کالکشن، پیمایش و LINQ

خروجی فاز: انتخاب ساختمان داده و نوشتن query با درک زمان اجرا و هزینه. مبنا، [LINQ رسمی C#](https://learn.microsoft.com/en-us/dotnet/csharp/linq/) و [خانواده‌های عملگر استاندارد](https://learn.microsoft.com/en-us/dotnet/csharp/linq/standard-query-operators/) است.

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R056 | List و قرارداد کالکشن | R044, R055 | `List<T>`، Count/Capacity، رشد و جابه‌جایی، ICollection/IList و نسخه‌های read-only، انتخاب آرایه در برابر لیست، شناخت ArrayList قدیمی و Boxing | فهرست قابل ویرایش بساز و تفاوت Count، Capacity و index معتبر را آزمایش کن |
| R057 | پروتکل پیمایش | R028, R044, R056 | IEnumerable/IEnumerator و نسخه‌های جنریک، GetEnumerator/MoveNext/Current، foreach pattern، Dispose هنگام پیمایش، تغییر کالکشن در حین پیمایش | یک sequence سفارشی قابل پیمایش بساز و خروج زودهنگام را بررسی کن |
| R058 | Iterator و اجرای تنبل | R029, R030, R057 | `yield return/yield break`، deferred execution، state machine در حد رفتار، زمان وقوع exception، finally و Dispose، multiple enumeration | sequence تنبل بساز و تعداد اجرای منبع را در یک و دو پیمایش اندازه بگیر |
| R059 | Dictionary و HashSet | R019, R046, R056 | key/value، TryGetValue، equality comparer، HashSet و عملیات مجموعه، collision، رشد ظرفیت، هزینهٔ مورد انتظار و بدترین حالت، کلید تغییرپذیر | شمارش واژه و حذف تکرار را با سیاست برابری مشخص بساز و کلید بد را آشکار کن |
| R060 | کالکشن‌های تخصصی | R019, R046, R059 | Queue/Stack/LinkedList، SortedList/SortedDictionary/SortedSet، PriorityQueue، هزینهٔ جست‌وجو و افزودن، انتخاب ساختار بر اساس عملیات | صف کار، تاریخچه و صف اولویت را برای سه نیاز متفاوت انتخاب و آزمایش کن |
| R061 | Read-only، Immutable و Frozen | R035, R056, R059 | view فقط‌خواندنی در برابر دادهٔ تغییرناپذیر، ReadOnlyCollection، ImmutableArray/List/Dictionary، builder، FrozenSet/Dictionary، snapshot، مرز تضمین ایمنی داده | نشان بده read-only view از تغییر منبع جلوگیری نمی‌کند و snapshot مستقل چه تفاوتی دارد |
| R062 | نحو مدرن کالکشن | R041, R044, R057, R061 | collection initializer و قرارداد IEnumerable/Add، collection expression و spread در C# 12، target typing، initializer در برابر expression، `params` collections در C# 13، overload و هزینهٔ materialization | یک API params را با آرایه و مجموعهٔ مجاز بنویس و اثر نوع مقصد را بررسی کن |
| R063 | LINQ پایه و Query Syntax | R049, R053, R058, R059 | method/query syntax، from/where/select/orderby/let، projection و anonymous type، Where/Select/OrderBy/ThenBy، Any/All/Contains، ToArray/ToList | گزارش فیلتر و مرتب‌شده بساز و query را بدون تغییر معنا به شکل دیگر بنویس |
| R064 | خانواده‌های عملگر LINQ | R063 | GroupBy/ToLookup، Join/GroupJoin/SelectMany، Aggregate و تجمیع عددی، Zip/Chunk، set و Byها، partition، element و Defaultها، generation و conversion، مرز عملگرهای موجود در .NET هدف | داده‌های دو مجموعه را join و گروه‌بندی کن؛ حالت خالی، تکراری و تک‌عضوی را مشخص کن |
| R065 | رفتار و هزینهٔ LINQ | R058, R064 | lazy/eager، streaming/buffering، تکرار query، ترتیب فیلتر و projection، side effect، sort/group buffer، تفاوت Count/Any بر اساس نیاز، materialization در مرز مناسب | query دارای پیمایش تکراری را با حفظ خروجی اصلاح و دفعات اجرای منبع را ثبت کن |
| R066 | Expression Tree و IQueryable | R044, R049, R064 | Expression در برابر delegate، node و composition، Compile، IQueryable و provider، ترجمه در برابر اجرای محلی، محدودیت lambdaهای قابل تبدیل، مرز client/provider | یک شرط قابل ترکیب بساز و فرق عبارت قابل ترجمه با تابع دلخواه محلی را توضیح بده |
| R067 | پروژهٔ تحلیل داده | R059, R060, R061, R065, R066 | انتخاب کالکشن، کلید و comparer، pipeline query، دادهٔ خالی، هزینه و تست مستقل از دیتابیس | گزارش فروش یا امتیاز بساز؛ خروجی و تعداد پیمایش‌ها را روی دادهٔ بزرگ‌تر بررسی کن |

<a id="phase-07"></a>
## فاز ۷. کتابخانه‌های پایه، I/O و کیفیت

خروجی فاز: برنامهٔ کنسول دارای ذخیره‌سازی و آزمون قابل تکرار. منابع: [File and Stream I/O](https://learn.microsoft.com/en-us/dotnet/standard/io/)، [آزمون در .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/) و [Dependency Injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview).

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R068 | زمان، شناسه و عددهای کتابخانه‌ای | R034, R046, R067 | DateTime/DateTimeOffset، DateOnly/TimeOnly، TimeSpan، UTC و منطقهٔ زمانی/DST، TimeProvider برای تست، Guid، Math و rounding، BigInteger و شناخت Int128/UInt128/Half | موعد زمانی را بدون وابستگی به ساعت دستگاه تست کن و فرق شناسه با ترتیب زمانی را نشان بده |
| R069 | Unicode، Encoding و Regex | R017, R038, R068 | UTF-8/UTF-16، byte در برابر char، Rune و text element، normalization، مقایسه و culture، Regex و گروه‌ها، timeout و ورودی بلند، UTF-8 literal با u8 | متن فارسی و ایموجی را درست پیمایش کن و الگوی جست‌وجو را با ورودی نامعتبر آزمایش کن |
| R070 | فایل، پوشه و مسیر | R029, R030, R069 | File/Directory و Infoها، Path، مسیر نسبی/مطلق، تفاوت سیستم‌عامل‌ها، encoding، دسترسی و خطای I/O، محدود کردن مسیر ورودی و نوشتن ایمن فایل | داده را در پوشهٔ آزمایشی ذخیره کن و فایل مفقود، مسیر نامعتبر و قطع نوشتن را بررسی کن |
| R071 | Stream و دادهٔ باینری | R030, R070 | Stream/FileStream/MemoryStream، reader/writer متن و باینری، seek و position، buffering، ownership، partial read، compression با GZip/Zip، تفاوت byte و text | فایل را بدون فرض خواندن یک‌باره پردازش کن و آزادسازی منبع را روی خطا بسنج |
| R072 | JSON و قالب داده | R035, R036, R071 | System.Text.Json، DTO و options، naming و null، enum و تاریخ، JsonDocument/JsonNode، converter، polymorphism مجاز، چرخهٔ مرجع، تغییر نسخهٔ داده، آشنایی با XML و LINQ to XML | داده را round-trip کن و فایل قدیمی، JSON خراب و فیلد نامعتبر را تست کن |
| R073 | Configuration و DI پایه | R028, R030, R044, R072 | constructor injection دستی، composition root، IServiceCollection/Provider، lifetimeها، scope و disposal، configuration از فایل و environment، options و validation پایه | منبع ذخیره‌سازی را با تنظیمات انتخاب کن و منطق برنامه را با fake اجرا کن |
| R074 | Logging و عیب‌یابی برنامه | R029, R073 | ILogger، level و category، structured template در برابر interpolation، exception logging، scope، جلوگیری از ثبت دادهٔ حساس، ارتباط log با خطای قابل رسیدگی | خطا را با context کافی ثبت کن و ثابت کن دادهٔ حساس در خروجی نیست |
| R075 | آزمون داده و وابستگی | R031, R068, R072, R073 | تست پارامتری، boundary، fake/stub/mock و انتخاب حداقل ابزار، integration test با فایل موقت، زمان قابل کنترل، آزمون قرارداد، مفهوم property-based testing | ذخیره‌ساز را بدون حساب واقعی، شبکهٔ واقعی یا ساعت واقعی آزمایش کن |
| R076 | کیفیت و خوانایی کد | R036, R075 | nullable warnings، analyzer، EditorConfig، dotnet format، نام‌گذاری، اندازهٔ متد، refactoring با حفظ رفتار، XML documentation و cref، coverage در برابر کیفیت آزمون | کد مبهم را با تست محافظ بازنویسی کن و هشدارها را با علت رفع کن |
| R077 | وابستگی و ساخت قابل تکرار | R016, R073, R076 | NuGet مستقیم/گذرا، SemVer، restore، lock file، central package management، Directory.Build.props، SDK pinning، تفاوت سازگاری source/binary، .NET و .NET Framework در نگهداری کد قدیمی | کتابخانه و مصرف‌کننده را با SDK مشخص بساز و یک تغییر ناسازگار API را تشخیص بده |
| R078 | ابزار خط فرمان قابل استفاده | R014, R016, R073, R074 | args و گزینه‌ها، help، stdout/stderr، exit code، environment، validation، مدیریت خطا در مرز برنامه، جداسازی UI کنسول از منطق | برنامه را با آرگومان معتبر، آرگومان ناقص و ورودی هدایت‌شده اجرا و خروجی را بررسی کن |
| R079 | پروژهٔ برنامهٔ پایدار | R070, R071, R072, R075, R078 | ذخیره‌سازی، بازگشایی، خطا، تنظیمات، log، تست مستقل، مهاجرت سادهٔ قالب داده | دفتر کار یا موجودی فایل‌محور بساز؛ ذخیره و بازیابی و بازیابی از دادهٔ خراب را بررسی کن |

<a id="phase-08"></a>
## فاز ۸. Async، شبکه و همزمانی

خروجی فاز: برنامه‌ای که منتظر I/O می‌ماند، لغو می‌شود و همزمانی را محدود می‌کند. مرجع‌های اصلی: [async/await](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)، [Async Streams](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream) و [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels).

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R080 | Task و async/await | R029, R044, R071, R079 | asynchronous در برابر parallel و blocking، Task/Task<T>، await، async Main، I/O-bound و CPU-bound، propagation خطا، محدودیت async void، کاربرد Task.Run | عملیات I/O را بدون Result/Wait بنویس و نشان بده async الزاماً thread تازه نمی‌سازد |
| R081 | ترکیب کارهای async | R080 | شروع کار در برابر await، اجرای ترتیبی و همزمان، WhenAll/WhenAny، مشاهدهٔ خطای task برنده و بقیه، AggregateException، شناخت ContinueWith و Unwrap در کد موجود | چند کار موفق/ناموفق را ترکیب کن و نتیجه و همهٔ خطاهای لازم را مشاهده کن |
| R082 | لغو و Timeout | R081 | CancellationToken/Source، لغو تعاونی، linked token، CancelAfter، OperationCanceledException، Task.WaitAsync، تفاوت timeout انتظار با لغو عملیات، disposal | عملیات را قبل، حین و بعد از پایان لغو کن و ثابت کن timeout لزوماً کار زیرین را متوقف نمی‌کند |
| R083 | منابع async و قرارداد بازگشت | R030, R080, R082 | IAsyncDisposable و await using، ValueTask در حد قرارداد مصرف، await در catch/finally، cleanup روی لغو، پرهیز از sync-over-async و fire-and-forget بی‌مالک | منبع async را در مسیر موفق، خطا و لغو آزاد کن و exception را گم نکن |
| R084 | Async Streams | R058, R082, R083 | IAsyncEnumerable/IAsyncEnumerator، await foreach، async iterator، backpressure ناشی از مصرف، WithCancellation، EnumeratorCancellation، خروج زودهنگام و disposal | صفحه‌های داده را تدریجی تولید و مصرف کن و لغو و break مصرف‌کننده را بررسی کن |
| R085 | HTTP و Resilience | R072, R073, R082, R084 | URI و HTTP پایه، status و header، HttpClient و طول عمر، IHttpClientFactory، streaming و JSON، DNS و TLS در حد مصرف، retry/backoff و idempotency، timeout و لغو | client قابل تست بساز؛ خطای HTTP، پاسخ خراب، timeout و retry مجاز را با handler آزمایشی بررسی کن |
| R086 | Thread و ThreadPool | R080, R082 | Thread، foreground/background، ThreadPool، task در برابر thread، scheduling و starvation، ThreadLocal و مقدمهٔ ExecutionContext، blocking در سرور | کاری CPU-bound را از I/O-bound جدا کن و اثر محدود کردن workerها را توضیح بده |
| R087 | Synchronization و قفل | R030, R086 | race condition، critical section، lock/Monitor، System.Threading.Lock در C# 13، Mutex و Semaphore/SemaphoreSlim، ReaderWriterLockSlim، ترتیب قفل، عدم await داخل lock، async gate | شمارندهٔ مشترک و دسترسی محدود به منبع را بدون race و با لغو درست بساز |
| R088 | مدل حافظه و Atomics | R086, R087 | atomicity در برابر visibility و ordering، Interlocked، Volatile و volatile، memory barrier، رابطه با CPU cache، compare-exchange، محدودیت lock-free و خطر ABA | یک عملیات مرکب نادرست را پیدا کن و فقط جایی که مناسب است با Interlocked اصلاح کن |
| R089 | Concurrent Collections | R059, R060, R087 | ConcurrentDictionary/Queue/Stack/Bag، BlockingCollection، compound operation، چندبار اجرا شدن value factory، thread safety کالکشن در برابر مقدارهای داخل آن | cache یا شمارش همزمان بساز و نشان بده GetOrAdd تضمین اجرای یک‌بارهٔ factory نیست |
| R090 | Channels و Producer/Consumer | R082, R084, R089 | bounded/unbounded، writer/reader، completion و خطا، cancellation، backpressure، سیاست پر شدن، مالکیت worker، پرهیز از ادعای عمومی lock-free بودن همهٔ Channelها | صف bounded بساز؛ کندی مصرف‌کننده، لغو و خطای تولیدکننده را بدون از دست دادن پایان کار تست کن |
| R091 | Parallel و PLINQ | R065, R086, R087, R090 | Parallel.For/ForEach/ForEachAsync، MaxDegreeOfParallelism، PLINQ و ترتیب، partition، هزینهٔ همزمانی، دادهٔ مشترک، تفکیک CPU و I/O | پردازش مستقل چند ورودی را محدود کن و نتیجه را با نسخهٔ ترتیبی یکسان نگه دار |
| R092 | Context و Deadlock | R081, R086, R087 | SynchronizationContext، ExecutionContext، AsyncLocal، TaskScheduler، ConfigureAwait، تفاوت کد کتابخانه و برنامه، deadlock و thread-pool starvation، جریان context | یک سناریوی sync-over-async را تحلیل کن و بدون توصیهٔ همگانی ConfigureAwait(false) اصلاح کن |
| R093 | تست و مشاهده‌پذیری همزمانی | R068, R074, R075, R092 | هماهنگی کنترل‌شدهٔ تست، زمان قابل کنترل، ثبت و بازتولید race، پرهیز از تست متکی به sleep، Activity/ActivitySource، EventSource و counters، trace و OpenTelemetry در حد اتصال و خواندن | لغو و محدودیت همزمانی را بدون تأخیر تصادفی تست کن و جریان یک درخواست را در trace دنبال کن |
| R094 | پروژهٔ Async | R085, R090, R091, R093 | pipeline دارای I/O، صف محدود، خطا و لغو، مصرف تدریجی، shutdown و تست | دریافت‌کننده یا پردازشگر داده بساز؛ توقف، خطای یک کار و کندی مقصد نباید منابع یا task رهاشده ایجاد کند |

<a id="phase-09"></a>
## فاز ۹. CLR، حافظه و کارایی

خروجی فاز: توضیح رفتار داخلی و اثبات بهبود با اندازه‌گیری. جزئیات runtime از تضمین زبان جدا نگه داشته می‌شوند. مراجع: [Garbage Collection](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/)، [Memory and Span](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/) و [BenchmarkDotNet](https://benchmarkdotnet.org/articles/overview.html).

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R095 | مسیر اجرای مدیریت‌شده | R016, R033, R077, R094 | compiler و IL/MSIL، CLR، CTS و CLS، BCL، CLI در معنای استاندارد و خط فرمان، assembly/metadata/manifest، JIT، tiered compilation و PGO، JIT در برابر AOT | سفر یک متد از source تا اجرا را توضیح بده و تفاوت قرارداد زبان با بهینه‌سازی JIT را مشخص کن |
| R096 | چیدمان و عمر داده | R027, R033, R034, R049, R095 | stack frame، managed heap، GC root و liveness، object header/MethodTable در حد runtime، alignment، دادهٔ درون شیء و آرایه، closure allocation، boxing، dispatch و devirtualization | چند تخصیص و کپی پنهان را پیدا کن و از ادعای همهٔ value typeها روی stack پرهیز کن |
| R097 | GC و نگهداری ناخواسته | R050, R096 | نسل‌ها، SOH/LOH/POH، root و collection، promotion و compaction، server/workstation/background GC، pinning، WeakReference، memory leak مدیریت‌شده، حدود GC.Collect | علت زنده ماندن یک subscriber یا cache را از نمودار ارجاع‌ها و ابزار تشخیص بده |
| R098 | Dispose و Finalization عمیق | R030, R083, R097 | finalizer، Dispose Pattern، SuppressFinalize، SafeHandle و مالکیت handle، تفاوت حافظه و منبع، managed/unmanaged، async disposal و مالکیت وابستگی | یک wrapper منبع را در سناریوی dispose تکراری و خطای ساخت بررسی کن و نیاز به finalizer را استدلال کن |
| R099 | By-ref و Ref Safety | R012, R034, R045, R084, R096 | ref local/return، ref readonly و ref readonly parameter، ref conditional، ref field، scoped و escape analysis، ref struct، interface و allows ref struct در C# 13، محدودیت عبور از await/yield و Boxing | کد دارای reference فراری را هنگام کامپایل رد کن و مصرف امن ref struct را نشان بده |
| R100 | Span و Memory | R071, R099 | Span/ReadOnlySpan، Memory/ReadOnlyMemory، slicing، ArraySegment، مالکیت و طول عمر، parse بدون رشتهٔ میانی، تبدیل‌های ضمنی Span در C# 14، read-only view در برابر immutability | parser کوچک بساز که view را پس از پایان عمر منبع نگه ندارد و خروجی درست بدهد |
| R101 | Pooling و تخصیص | R061, R097, R100 | ArrayPool/MemoryPool، IMemoryOwner، stackalloc در مسیر امن، برگشت منبع در finally، پاک کردن دادهٔ حساس، inline array در C# 12، انتخاب ساختار بدون تضمین زودهنگام zero-allocation | buffer اجاره کن؛ در موفقیت، خطا و لغو آن را برگردان و تخصیص را اندازه بگیر |
| R102 | Binary، Sockets و Pipelines | R071, R082, R085, R090, R100 | TCP/UDP و Sockets/TcpClient در حد پروتکل، partial read/write، framing و endian، BinaryPrimitives، ReadOnlySequence/SequenceReader، IBufferWriter، PipeReader/Writer، AdvanceTo و backpressure | پیام چندتکه را از stream بخوان؛ buffer را درست آزاد کن و صفر بودن همهٔ کپی‌ها را فرض نگیر |
| R103 | Benchmark و Profiling | R093, R095, R097, R101, R102 | baseline و فرضیه، build با Release، warm-up، BenchmarkDotNet، زمان/throughput/allocation، dotnet-counters/trace/gcdump/dump، sampling و trace، اثر JIT و GC، گزارش قابل بازتولید | دو نسخهٔ هم‌رفتار را در شرایط ثبت‌شده مقایسه کن و بهبود واقعی را از نویز جدا کن |
| R104 | پشت صحنهٔ Async و Iterator | R058, R080, R083, R092, R095, R103 | lowering و state machine، IAsyncStateMachine/MoveNext، awaiter pattern و custom awaitable، TaskCompletionSource و continuation، ValueTask/IValueTaskSource و محدودیت مصرف، allocation و context | یک سناریوی Task و ValueTask را درست مصرف کن و با مشاهدهٔ خروجی کامپایلر علت هزینه را توضیح بده |
| R105 | پروژهٔ کارایی | R099, R100, R101, R102, R103, R104 | رفتار ثابت، بهینه‌سازی بر اساس پروفایل، مالکیت buffer، خطا و لغو، مقایسهٔ هزینه و پیچیدگی نگهداری | parser یا pipeline قبلی را بهبود بده؛ آزمون‌ها، ورودی‌ها و اندازه‌گیری قبل و بعد را تحویل بده |

<a id="phase-10"></a>
## فاز ۱۰. Metadata، تولید کد، Interop و انتشار

خروجی فاز: ساخت کتابخانه و شناخت محدودیت‌های reflection، کد ناامن و AOT. مراجع: [Roslyn SDK](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/) و [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R106 | Attribute و Reflection | R018, R036, R044, R052, R095 | custom و generic attribute، AttributeUsage و target، caller information و CallerArgumentExpression، Obsolete/Conditional/Experimental، Type/MemberInfo، BindingFlags، Activator و invocation، generic metadata، Reflection.Emit و محدودیت محیط | metadata یک مدل را بخوان و هزینه و خطر ساخت نوع یا اجرای عضو از ورودی دلخواه را بررسی کن |
| R107 | Roslyn و Source Generator | R052, R066, R072, R106 | syntax/semantic model، analyzer و code fix، incremental source generator، diagnostic، خروجی deterministic، نمونه‌های تولید کد JSON/Regex/logging، فرق تولید زمان build با reflection زمان اجرا | generator کوچک همراه تست ورودی و diagnostic بساز و کد تولیدشده را بازبینی کن |
| R108 | الگوهای پیشرفتهٔ کامپایلر | R049, R053, R062, R100, R106, R107 | custom interpolated string handler، CollectionBuilder، attributeهای لازم برای قرارداد کامپایلر، overload resolution و priority در C# 13، تأثیر نوع مقصد و نسخهٔ زبان، بررسی IL تولیدشده | یک extension یا builder را با حداقل قرارداد لازم بساز و overload انتخاب‌شده را با تست مشخص کن |
| R109 | Unsafe و Pointer | R034, R099, R100, R106 | unsafe context، pointer و address/dereference، sizeof، fixed و pinning، pointer arithmetic، fixed-size buffer، stackalloc ناامن، nint/nuint و IntPtr/UIntPtr، function pointer و calling convention، Unsafe class | یک buffer محدود را در محیط تمرین پردازش کن و مسئولیت bounds و طول عمر را مستند کن |
| R110 | Native Interop | R098, R106, R107, R109 | P/Invoke، extern، DllImport/LibraryImport، marshalling، blittable و layout/packing، delegate در برابر function pointer، NativeMemory، SafeHandle، UnmanagedCallersOnly، هزینه و تفاوت پلتفرم | یک API بومی کوچک متناسب با سیستم‌عامل را wrap کن؛ آزادسازی و نگهداری callback را بررسی کن |
| R111 | Assembly و بارگذاری | R016, R024, R095, R106 | assembly identity/version، reference در برابر load، AssemblyLoadContext و unload، plugin و ارجاع زنده، resource و satellite assembly، InternalsVisibleTo، extern alias، strong name در برابر مرز امنیتی، شناخت AppDomain قدیمی | یک کتابخانهٔ معمول و یک plugin آزمایشی را از نظر وابستگی و امکان unload مقایسه کن |
| R112 | مدل‌های انتشار و AOT | R077, R095, R107, R110, R111 | framework-dependent/self-contained، RID، single-file، ReadyToRun، trimming و analyzers، Native AOT، محدودیت dynamic code/reflection/loading، annotationهای trimming، file-based app directives، باقی ماندن GC در AOT | برنامه را برای یک هدف مشخص منتشر کن و هشدارهای trimming/AOT را با علت رسیدگی کن |
| R113 | قرارداد عمومی، سازگاری و امنیت | R029, R036, R072, R074, R077, R085, R106, R112 | طراحی API و نسخه‌بندی، XML docs و obsolete migration، مرز validation، ورودی نامطمئن و serialization، منابع محدود، Random در برابر RandomNumberGenerator، API استاندارد رمزنگاری، پرهیز از اجرای کد یا بارگذاری نوع دلخواه | کتابخانه‌ای را از نظر دادهٔ خراب، تغییر API، افشای اطلاعات و مصرف بیش از حد منبع بررسی کن |
| R114 | پروژهٔ زبان پیشرفته | R105, R107, R108, R110, R112, R113 | کتابخانهٔ آزموده، تولید کد، خوانایی و سازگاری، انتشار JIT/AOT با قرارداد مشخص، مستند کردن محدودیت پلتفرم | کتابخانه و مصرف‌کنندهٔ کوچک تحویل بده؛ مسیر سازگار با AOT را از plugin پویا جدا و هر خروجی را آزمایش کن |

<a id="phase-11"></a>
## فاز ۱۱. طراحی و تحویل حرفه‌ای

این فاز گسترش کاربردی مسیر است. دانستن تمام الگوها شرط نوشتن اولین برنامه نیست و استفاده از یک الگو باید مسئلهٔ واقعی را حل کند. مبنای بخش معماری، [راهنمای معماری Microsoft](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures) است؛ یک معماری واحد برای همهٔ پروژه‌ها تجویز نمی‌شود.

| کد | عنوان | پیش‌نیاز کلیدی | ریزمباحث الزامی | تمرین قابل ارزیابی |
|---|---|---|---|---|
| R115 | SOLID و Refactoring | R025, R028, R031, R054, R114 | SRP/OCP/LSP/ISP/DIP، composition، قرارداد رفتاری، coupling/cohesion، حداقل abstraction لازم، YAGNI، بازطراحی با تست | یک وابستگی نامناسب و یک نقض قرارداد را با کمترین تغییر و تست محافظ اصلاح کن |
| R116 | الگوهای Creational | R047, R073, R087, R115 | Factory Method، Abstract Factory، Builder، Prototype، Singleton، Lazy<T>، مالکیت و thread safety و مقایسه با DI | برای دو مسئله الگوی مناسب انتخاب کن و توضیح بده کجا factory یا singleton لازم نیست |
| R117 | الگوهای Structural | R028, R053, R115, R116 | Adapter، Bridge، Composite، Decorator، Facade، Flyweight، Proxy؛ هزینهٔ لایه‌ها و طول عمر دادهٔ مشترک | بدون تغییر مصرف‌کننده یک adapter و یک decorator قابل تست بساز |
| R118 | الگوهای Behavioral | R048, R050, R057, R115, R117 | Strategy، Observer، Command، Chain of Responsibility، Mediator، Iterator، State، Template Method، Visitor، Memento، Interpreter؛ مقایسهٔ سناریو و حداقل پیاده‌سازی | دو رفتار جایگزین و یک گردش کار را مدل کن؛ انتخاب الگو و راه ساده‌تر را مقایسه کن |
| R119 | DI و Host پیشرفته | R073, R082, R093, R115 | Generic Host، BackgroundService و shutdown، scope و async disposal، captive dependency، factory و keyed service، options validation/reload، composition root و lifetime diagnostics | worker قابل توقف بساز که dependency با عمر کوتاه را در singleton نگه ندارد |
| R120 | معماری برنامه | R079, R094, R113, R115, R119 | مرز دامنه و زیرساخت، layered/Clean Architecture، Vertical Slice، CQS/CQRS، مدل دامنه، Repository/UnitOfWork و trade-offها، حدود transaction، پرهیز از پخش زودهنگام برنامه | دو چیدمان را برای یک مسئله مقایسه و انتخاب وابستگی‌ها و مرز تراکنش را مستند کن |
| R121 | رویداد و Reactive | R050, R084, R090, R092, R118 | IObservable/IObserver، عمر Subscribe، hot/cold، error/completion، event در برابر async stream/channel، آشنایی انتخابی با System.Reactive و weak event، محدودیت backpressure | یک جریان داده را با دو مدل مقایسه کن و unsubscribe و خطا را بدون نشت منبع تست کن |
| R122 | تحویل قابل تکرار | R075, R076, R077, R103, R112, R120 | build/test/format/pack/publish در CI، نسخه و release note، نمونهٔ مصرف API، بررسی سازگاری، تحلیل وابستگی، گزارش benchmark و trace، بازبینی کد | کتابخانه و برنامه را در محیط تازه بساز و با دستورها و دادهٔ مشخص قابل بازتولید تحویل بده |
| R123 | پروژه و ارزیابی نهایی | R114, R118, R119, R120, R121, R122 | کاربرد متناسب زبان، قرارداد و خطا، JSON/I/O، async و لغو، تست، تحلیل هزینه، تصمیم طراحی و مستندات؛ مرور مصاحبه پس از تسلط عملی | یک ابزار پردازش داده یا worker بساز؛ تست، خروجی اجرای واقعی، گزارش کارایی و دلیل انتخاب‌ها را ارائه کن |

<a id="milestones"></a>
## ۴. نقاط سنجش و پروژه‌های پیوسته

پروژه‌ها می‌توانند روی یک مسئلهٔ ثابت مانند «دفتر سفارش» رشد کنند. هر فاز رفتار قبلی را با آزمون حفظ می‌کند؛ برای نمایش هر ویژگی زبان، پروژهٔ اصلی با abstraction اضافی پیچیده نمی‌شود و آزمایش کوچک مستقل هم مجاز است.

| پایان | تحویل | شرط عبور |
|---|---|---|
| R010 | پردازش آرایه و جدول امتیاز | شرط و حلقه درست؛ ورودی خالی و index مرزی بررسی شده باشد |
| R020 | برنامهٔ کنسول منودار | دادهٔ نامعتبر، پایان ورودی و خروج کاربر، رفتار مشخص داشته باشند |
| R032 | مدل شیءگرا | invariantها و رفتار چندریختی با آزمون مستقل تأیید شوند |
| R043 | مدل داده و برابری | نوع، کپی، null و قرارداد equality توجیه و آزمایش شوند |
| R055 | جزء جنریک و رویداد | ایمنی نوع و عمر subscription روشن باشند |
| R067 | گزارش و تحلیل | نتیجه و زمان اجرای query قابل توضیح؛ دادهٔ خالی و تکراری آزموده باشند |
| R079 | برنامهٔ فایل‌محور | round-trip، فایل خراب و خطای I/O بدون تخریب دادهٔ سالم مدیریت شوند |
| R094 | پردازشگر async | لغو، خطا، سقف همزمانی و پایان workerها آزموده باشند |
| R105 | نسخهٔ بهینه‌شده | رفتار یکسان و اندازه‌گیری بازتولیدپذیر قبل و بعد وجود داشته باشد |
| R114 | کتابخانهٔ پیشرفته | تست و خروجی publish و محدودیت reflection/interop/AOT مستند باشند |
| R123 | پروژهٔ نهایی | فرد بتواند کد را تغییر دهد، خطا را پیدا کند و تصمیم طراحی را دفاع کند |

معیارهای ارزیابی برای همهٔ سطح‌ها: درستی رفتار، توضیح علت، رسیدگی به موارد مرزی، خوانایی و استقلال در حل مسئله. در سطح پیشرفته، مدیریت منبع، لغو، همزمانی و اندازه‌گیری هم اضافه می‌شوند. نمرهٔ ثابت AI یا صرفاً مشاهدهٔ پاسخ مرجع، جای این شواهد را نمی‌گیرد.

<a id="coverage"></a>
## ۵. ماتریس کنترل پوشش زبان

این ماتریس برای بازبینی جامعیت است. عنوان هر واحد باید در محتوای آینده به مثال و تمرین تبدیل شود؛ وجود کلمه در جدول به‌تنهایی آموزش کامل محسوب نمی‌شود. مرجع کنترل: [C# Language Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/).

| حوزه | محل پوشش |
|---|---|
| نحو، literal، شناسه، comment، statement/expression، scope و definite assignment | R002 تا R004، R011، R016 |
| خانوادهٔ انواع عددی و bool/char/string/object/dynamic، نوع بومی، value/reference و Boxing | R003 تا R005، R013، R026، R033، R034، R042، R068، R109 |
| تمام خانواده‌های عملگر: حسابی، منطقی، بیتی، مقایسه، انتساب، تبدیل، null، index/range، type و overload | R004، R006، R010، R013، R018، R026، R033، R036، R039 تا R041، R099، R109 |
| انتخاب و تکرار، switch، break/continue/return، goto، throw و exception filter | R007، R008، R010 تا R015، R019، R029، R038 |
| آرایه، متد، overload، named/optional/params، ref/out/in، local function و بازگشت | R009 تا R012، R019، R049، R062، R099 |
| namespace، usingها، alias/extern alias، top-level/Main، فایل و directiveهای کامپایلر | R001، R016، R021، R024، R037، R052، R111، R112 |
| تمام انواع عضو: field/constant، property، method، constructor، event، indexer، operator، nested type و finalizer | R021 تا R024، R040، R041، R050، R052، R098 |
| class/static/abstract/sealed، inheritance، polymorphism و interface | R021 تا R028، R047، R051، R115 |
| struct/readonly، record، with، init/required، tuple/deconstruction، anonymous type و patternها | R022، R034 تا R038، R063، R099 |
| Generics، constraints، variance، static abstract، Generic Math و مقایسهٔ جنریک | R044 تا R047، R051، R099 |
| Delegate، anonymous method، lambda، capture، extension، partial و event | R048 تا R055، R096، R108، R121 |
| Enumeration، yield، LINQ query syntax، expression tree و IQueryable | R057، R058، R063 تا R066 |
| async/await و awaitable، Task/ValueTask، async iterator/disposal، لغو و خطا | R080 تا R085، R092، R094، R104 |
| synchronization، volatile و atomics، concurrent collections و parallelism | R086 تا R093 |
| Attribute، metadata، caller info، reflection، Roslyn و source generation | R018، R036، R106 تا R108، R111 |
| resource ownership، GC، Span/Memory، ref safety، pooling، unsafe و interop | R030، R083، R095 تا R110 |
| فرهنگ و Unicode، زمان، فایل/stream، JSON/XML، شبکه و API امن | R017، R068 تا R074، R078، R085، R102، R113 |
| Debugging، آزمون، analyzer، build/package، سازگاری و publish | R015، R016، R031، R075 تا R077، R093، R103، R111 تا R114، R122 |

<a id="versions"></a>
## ۶. قابلیت‌های نسخه‌های جدید در جای مناسب

قابلیت جدید پس از پیش‌نیازش آموزش داده می‌شود؛ یادگیرنده برای شروع، مجبور به یادگیری همهٔ شکل‌های نوشتن یک مفهوم نیست. جزئیات ادغام نسخه‌ها در این جدول به [C# 12](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)، [C# 13](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13) و [C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14) ارجاع دارند.

| نسخه | قابلیت و محل |
|---|---|
| C# 12 | primary constructor: R023؛ alias برای هر نوع مجاز: R037؛ default parameter در lambda: R049؛ collection expression: R062؛ ref readonly parameter: R099؛ inline array: R101؛ ExperimentalAttribute برای تشخیص API آزمایشی: R106 |
| C# 13 | escape با `\e`: مرور literal در R005؛ implicit Index در initializer: R041؛ بهبود natural type برای method group: R049؛ partial property/indexer: R052؛ params collections: R062؛ System.Threading.Lock: R087؛ ref و unsafe در iterator/async با محدودیت suspension و allows ref struct/interface: R099 و R109؛ overload priority: R108 |
| C# 14 | field-backed property: R022؛ null-conditional assignment: R036؛ instance increment/decrement و compound assignment: R040؛ nameof جنریک باز: R044؛ modifier پارامتر سادهٔ lambda: R049؛ partial constructor/event: R052؛ extension members: R053؛ implicit Span conversion: R100؛ file-based app directiveها: R016 و R112 |

در تدریس عملگرهای سفارشی، قواعد declaration و استثناهای C# 14 باید با [مرجع Operator Overloading](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading) کنترل شوند. keywordهای preview که در مرجع عمومی ظاهر می‌شوند، خودکار وارد مبنای پایدار دوره نمی‌شوند.

<a id="compatibility"></a>
## ۷. تطبیق نسخهٔ قبلی S01 تا S44 با مسیر جدید

این جدول، نگاشت موضوع است؛ نگاشت خودکار XP، معادل‌سازی مهارت یا تأیید پیاده‌سازی نیست. محتوای موجود S01 تا S10 بخشی از چند R را پوشش می‌دهد و برای مبتدی باید با مبانی و تمرین‌های نسخهٔ دوم تکمیل شود. وضعیت هر S از برنامهٔ فعلی خوانده می‌شود؛ محتوای آموزشی جدید هنگام تولید جداگانه تأیید خواهد شد.

| شناسهٔ قبلی | موضوع قبلی | محل در نسخهٔ دوم | وضعیت برنامه در زمان بازنگری |
|---|---|---|---|
| S01 | اکوسیستم و اجرای کد | R001، R016، R095، R111، R112 | محتوای موجود |
| S02 | انواع و حافظهٔ اولیه | R003 تا R005، R009، R013، R033، R096 | محتوای موجود |
| S03 | تبدیل، عملگر و Boxing | R004، R006، R026، R033، R040 | محتوای موجود |
| S04 | جریان کنترل | R006 تا R010، R014، R019، R038 | محتوای موجود |
| S05 | متد و پارامتر | R011، R012، R049، R062، R099 | محتوای موجود |
| S06 | کلاس و کپسوله‌سازی | R021 تا R026 | محتوای موجود |
| S07 | وراثت و چندریختی | R026، R027، R095، R096 | محتوای موجود |
| S08 | اینترفیس | R028، R045، R047، R115 | محتوای موجود |
| S09 | struct | R033، R034، R099 | محتوای موجود |
| S10 | record و برابری | R022، R035، R039، R043، R046 | محتوای موجود |
| S11 | Generics و constraints | R044، R045 | عنوان آینده |
| S12 | Variance | R051 | عنوان آینده |
| S13 | NRT | R013، R036 | عنوان آینده |
| S14 | Pattern Matching | R037، R038 | عنوان آینده |
| S15 | Delegate و Lambda | R048، R049، R054، R096 | عنوان آینده |
| S16 | Event | R050، R097، R121 | عنوان آینده |
| S17 | کالکشن پایه | R009، R010، R019، R046، R056، R059 | عنوان آینده |
| S18 | ساختمان دادهٔ تخصصی | R060 | عنوان آینده |
| S19 | Immutable و Read-only | R061 | عنوان آینده |
| S20 | Enumeration و LINQ | R057، R058، R063، R065 | عنوان آینده |
| S21 | LINQ پیشرفته و Expression Tree | R064، R066 | عنوان آینده |
| S22 | Exception | R015، R029 | عنوان آینده |
| S23 | Logging و Observability | R074، R093 | عنوان آینده |
| S24 | حافظهٔ CLR | R095، R096 | عنوان آینده |
| S25 | GC | R097 | عنوان آینده |
| S26 | Dispose | R030، R083، R098 | عنوان آینده |
| S27 | Span و Memory | R099، R100 | عنوان آینده |
| S28 | Ref Struct و Pooling | R099، R101 | عنوان آینده |
| S29 | Unsafe و Interop | R109، R110 | عنوان آینده |
| S30 | Thread و Synchronization | R086، R087 | عنوان آینده |
| S31 | Lock | R087 | عنوان آینده |
| S32 | Lock-free و Atomics | R088 | عنوان آینده |
| S33 | TAP و Async | R080، R081، R083، R104 | عنوان آینده |
| S34 | Context و Deadlock | R092 | عنوان آینده |
| S35 | Channels، Parallel و Reactive | R090، R091، R121 | عنوان آینده |
| S36 | Attribute و Reflection | R106، R111 | عنوان آینده |
| S37 | Roslyn و Native AOT | R107، R108، R112، R114 | عنوان آینده |
| S38 | Stream و Pipelines | R070، R071، R100، R102 | عنوان آینده |
| S39 | JSON و شبکه | R072، R085، R102، R107، R113 | عنوان آینده |
| S40 | SOLID | R115 | عنوان آینده |
| S41 | Creational و Structural | R116، R117 | عنوان آینده |
| S42 | Behavioral | R118، R121 | عنوان آینده |
| S43 | DI | R073، R119 | عنوان آینده |
| S44 | معماری و ارزیابی | R120، R122، R123 | عنوان آینده |

هنگام پیاده‌سازی نسخهٔ دوم، ابتدا واحدهای پایهٔ R001 تا R020 تکمیل شوند. برای استفاده از درس قدیمی در یک R، پیش‌نیاز، متن معرفی، مثال، تمرین مستقل و معیار ارزیابی همان R بررسی شود. گزارش‌های قدیمی، شواهد همان نسخه و زمان خود هستند.

<a id="maintenance"></a>
## ۸. قواعد نگهداری و تأیید این نقشه

- هر مبحث جدید باید جای مشخص، پیش‌نیاز قبلی و تمرین قابل ارزیابی داشته باشد؛ از افزودن یک عنوان در انتهای مسیر بدون بررسی وابستگی‌ها پرهیز شود.
- برای قابلیت‌های موجود، ابتدا شکل پایه و سپس عمق پیشرفته آموزش داده شود؛ مثلاً Exception در R015 و R029، Dispose در R030 و R098.
- قرارداد زبان، API کتابخانه و جزئیات runtime با هم اشتباه نشوند. System.Threading.Lock یک نوع است؛ همهٔ Channels بدون قفل نیستند؛ Pipelines به‌تنهایی تضمین حذف همهٔ کپی‌ها نمی‌دهد.
- نمونه‌ها با نسخهٔ زبان و TFM ثبت‌شده کامپایل و اجرا شوند. نمونهٔ عمداً نامعتبر باید خطای مورد انتظار داشته باشد.
- نمودار پیش‌نیاز بدون چرخه بماند؛ همهٔ شناسه‌های ارجاع‌شده تعریف شده باشند. تمام ۴۴ موضوع قدیمی باید در نگاشت باقی بمانند.
- عنوان «آماده» تنها پس از تولید و بازبینی محتوا و بررسی اجرای تمرین‌ها استفاده شود. طراحی سرفصل، تولید محتوا، آزمون محصول و تسلط یادگیرنده چهار نتیجهٔ جدا هستند.
- به‌روزرسانی نسخهٔ زبان با بررسی breaking changeها، قابلیت‌های پایدار و اثر آموزشی انجام شود؛ نسخهٔ Preview به‌صورت ضمنی پیش‌نیاز نشود.

<a id="sources"></a>
## ۹. منابع مرجع

منابع زیر معیار صحت فنی‌اند؛ ترتیب واحدها و پروژه‌ها متعلق به این سند است. صفحه‌های عمومی Microsoft ممکن است Preview نسخهٔ بعد را هم نمایش دهند، بنابراین محدودیت C# 14 این نسخه باید رعایت شود.

- زبان و نسخه: [مرجع C#](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/)، [keywordها](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/)، [عملگرها](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/)، [C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14).
- شروع بدون پیش‌زمینه: [اولین برنامه](https://learn.microsoft.com/en-us/dotnet/csharp/tour-of-csharp/tutorials/hello-world)، [مسیر مقدماتی بخش اول](https://learn.microsoft.com/en-us/training/paths/get-started-c-sharp-part-1/)، [بخش دوم](https://learn.microsoft.com/en-us/training/paths/get-started-c-sharp-part-2/).
- نوع و قرارداد: [Object-oriented C#](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/)، [Generics](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics)، [Null Safety](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/null-safety/nullable-reference-types).
- داده و کیفیت: [LINQ](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)، [I/O](https://learn.microsoft.com/en-us/dotnet/standard/io/)، [Testing](https://learn.microsoft.com/en-us/dotnet/core/testing/)، [DI](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/overview).
- همزمانی: [Async](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/)، [Async Streams](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream)، [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels).
- اجرای پیشرفته: [GC](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/)، [Memory/Span](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)، [Roslyn](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)، [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/).
