from author_csharp_v2 import *

unit(44,
"جنریک نوع یا متدی را با پارامتر نوع تعریف می‌کند تا ایمنی نوع بدون نسخه‌های تکراری حفظ شود. T پارامتر نوع و int در Box<int> آرگومان نوع است. inference در متدها می‌تواند T را از آرگومان‌ها استنتاج کند، اما همیشه از نوع خروجی به‌تنهایی ممکن نیست. چند پارامتر نوع برای رابطهٔ چند داده مفیدند؛ default(T) با value/reference و قرارداد null مرتبط است. استفاده از object اطلاعات ایستا را کم و برای valueها ممکن است boxing بسازد. جنریک تضمین حذف همهٔ تخصیص‌ها نیست؛ خود Box<T> کلاس همچنان شیء است. در C#14 nameof(List<>) نام نوع جنریک باز را می‌پذیرد. عملیات مجاز روی T فقط از constraint یا قرارداد پایه می‌آید.",
'Console.WriteLine(Identity(7));\nstatic T Identity<T>(T value)=>value;', '7',
("در متد T Copy<T>(T x)، بدون constraint می‌توان x.Length خواند؟", "خیر؛ همهٔ Tها قرارداد Length ندارند.", "بله؛ T هر عضو دلخواه را دارد.", "فقط با تغییر نام T به U حل می‌شود.", "پارامتر نوع عملیات شناخته‌شدهٔ محدود دارد؛ constraint مناسب یا قرارداد ورودی لازم است."),
groups("پارامتر و آرگومان نوع", "کارت‌ها را بر اساس نقش آن‌ها در تعریف یا مصرف دسته‌بندی کن.", ["پارامتر نوع در تعریف", "آرگومان نوع در مصرف"], [["T در class Box<T>","U در Convert<T,U>"],["int در Box<int>","string در Box<string>"]]),
"نسخهٔ جنریک FirstOr برای آرایه بنویس که در خالی مقدار fallback برگرداند. برای int و string با نوع ایستا تست کن؛ ورود نوع نامتناسب را در نمونهٔ منفی build کن.",
'Console.WriteLine(FirstOr(new[]{3,4},0));\nConsole.WriteLine(FirstOr(Array.Empty<string>(),"none"));\nstatic T FirstOr<T>(T[] values,T fallback)=>values.Length==0?fallback:values[0];',
"خروجی 3 و none باشد. روش بدون cast به object و بدون فرض null بودن default(T) کار کند؛ نوع نامعتبر هنگام کامپایل رد شود.")

unit(45,
"where constraint عملیاتی را که generic body نیاز دارد مشخص می‌کند. class و class? مرجع و nullability، struct نوع مقداری غیرnullable، notnull قرارداد تحلیل null و new() سازندهٔ عمومی بدون پارامتر را بیان می‌کنند. constraint پایه یا interface عضوهای همان قرارداد را قابل دسترسی می‌کند. Enum و Delegate و unmanaged برای خانواده‌های خاص‌اند؛ unmanaged به معنای بدون منبع یا بدون هزینه نیست. ترتیب constraintها قواعد دارد و new() معمولاً پس از constraintهای دیگر می‌آید. struct با new() ترکیب نمی‌شود چون سازندگی لازم را پوشش می‌دهد. default constraint برای override یا پیاده‌سازی صریحِ دارای ابهام nullability است و constraint عمومی «هرچیزی» نیست. کمترین constraint کافی، مصرف‌کننده را بی‌دلیل محدود نمی‌کند.",
'Console.WriteLine(Make<Item>().Value);\nstatic T Make<T>() where T:new()=>new T();\nclass Item {public int Value=4;}', '4',
("برای فراخوانی instance عضو Read روی T چه constraintی کافی است؟", "where T : IReader", "فقط نام‌گذاری پارامتر به Reader", "where T : new() بدون قرارداد Read", "وجود سازنده، وجود Read را تضمین نمی‌کند؛ constraint باید عملیات واقعی بدنه را پوشش دهد."),
repair("قید کافی", "بدنه از value.Read() استفاده می‌کند؛ IReader قرارداد Read است و new لازم نیست.", ["where T : new()", "where T : IReader", "بدون قید"], [0], ["عضو Read شناخته می‌شود", "پیاده‌سازی بدون سازندهٔ عمومی قابل مصرف است"], [([0],[False,False]),([1],[True,True]),([2],[False,True])]),
"متد Read<T> با حداقل constraint بساز. یک Reader که سازندهٔ عمومی بدون پارامتر ندارد را بپذیر و نوع بدون IReader را در تست کامپایل رد کن.",
'Console.WriteLine(Read(new Reader("ok")));\nstatic string Read<T>(T value) where T:IReader=>value.Read();\ninterface IReader {string Read();}\nclass Reader(string text):IReader {public string Read()=>text;}',
"ok چاپ شود. new() بی‌دلیل افزوده نشود؛ بدنه فقط به قرارداد IReader وابسته باشد.")

unit(46,
"IEquatable<T> برابری نوع‌دار و IComparable<T> ترتیب طبیعی را تعریف می‌کنند. IEqualityComparer<T> سیاست equality و hash بیرونی و IComparer<T> سیاست مرتب‌سازی بیرونی است؛ ترتیب و برابری هدف یکسانی ندارند. Comparer<T>.Default و EqualityComparer<T>.Default از قراردادهای موجود نوع استفاده می‌کنند. برای string سیاست Ordinal یا فرهنگی باید متناسب با داده انتخاب شود. دو شیء ممکن است در یک comparer مرتب‌سازی هم‌رتبه ولی در equality متفاوت باشند. comparer برابری باید hash سازگار بدهد. تغییر سیاست یک collection پس از ساخت راه امنی برای اصلاح کلیدها نیست؛ قرارداد را پیش از درج انتخاب کن.",
'Console.WriteLine(StringComparer.OrdinalIgnoreCase.Equals("A","a"));', 'True',
("برای دو ترتیب صعودی و نزولی بدون تغییر نوع چه می‌سازی؟", "دو IComparer<T> بیرونی", "دو کلاس کاملاً کپی‌شده از مدل", "GetHashCode تصادفی", "سیاست ترتیب بیرونی با comparer جدا می‌شود؛ equality مدل لازم نیست با هر sort عوض شود."),
analogy("سیاست مقایسه", "جفت مرجع: IComparer به مرتب‌سازی. جفت دوم را با رابطهٔ «قرارداد به مسئولیت» بساز.", ["قرارداد به مسئولیت", "هم‌معنی", "IEqualityComparer", "برابری و hash", "IDisposable", "مرتب‌سازی"], [[0,2,3]]),
"دو آرایهٔ نام را با StringComparer.Ordinal و OrdinalIgnoreCase مرتب و برابری A و a را تحت هر سیاست تست کن. hash دو رشتهٔ برابر تحت سیاست دوم هم یکسان باشد.",
'string[] names={"b","A","a"};\nArray.Sort(names,StringComparer.Ordinal);\nConsole.WriteLine(string.Join(",",names));\nvar comparer=StringComparer.OrdinalIgnoreCase;\nif(!comparer.Equals("A","a") || comparer.GetHashCode("A")!=comparer.GetHashCode("a")) throw new Exception("comparer");\nConsole.WriteLine("passed");',
"ترتیب Ordinal برابر A,a,b باشد. equality و hash سیاست دوم سازگار باشند و تفاوت سیاست زبانی با شناسهٔ فنی توضیح داده شود.")

unit(47,
"static abstract در interface قرارداد عضو static نوع را بیان می‌کند؛ فراخوانی از پارامتر نوع دارای constraint انجام می‌شود، نه از نمونهٔ interface. static virtual می‌تواند پیاده‌سازی پیش‌فرض داشته باشد. قید self-referential مثل T:INumber<T> اجازه می‌دهد عملیات عددی روی همان T بسته شود. INumber<T> خانواده‌ای از عملیات عددی استاندارد را برای generic math می‌دهد؛ همهٔ الگوریتم‌های عددی فقط با همین قید معنی‌دار نیستند. این dispatch با virtual روی instance متفاوت است و جای ساخت شیء را نمی‌گیرد. نوع ناسازگار باید در کامپایل رد شود؛ semantics سرریز، تقسیم و دقت هنوز به نوع عددی وابسته‌اند.",
'using System.Numerics;\nConsole.WriteLine(Add(2,3));\nstatic T Add<T>(T a,T b) where T:INumber<T> => a+b;', '5',
("عضو static abstract از کجا قابل فراخوانی قراردادی است؟", "از پارامتر نوع دارای constraint مناسب", "از هر object بدون constraint", "فقط از متغیر dynamic", "نوع generic با constraint عضو static را تضمین می‌کند؛ نمونهٔ interface حامل dispatch ایستای عمومی نیست."),
domino("زنجیرهٔ قرارداد عددی", "مسیر از الگوریتم جنریک به جمع ایمن از نظر نوع را کامل کن؛ اتصال‌ها جهت دارند.", ["متد Add<T>", "قید T:INumber<T>", "عضو + در قرارداد", "a+b از نوع T", "cast به dynamic"], [[0,1],[1,2],[2,3],[0,4]], 3, [1,2], limit=4),
"Sum<T> روی آرایه با INumber<T> بنویس. برای int،decimal و آرایهٔ خالی تست کن؛ string را در نمونهٔ منفی رد کن و سرریز int را با سیاست مشخص توضیح بده.",
'using System.Numerics;\nConsole.WriteLine(Sum(new[]{2,3}));\nConsole.WriteLine(Sum(new[]{1.5m,2.5m}));\nConsole.WriteLine(Sum(Array.Empty<int>()));\nstatic T Sum<T>(T[] values) where T:INumber<T> {T result=T.Zero;foreach(T value in values) result=checked(result+value);return result;}',
"خروجی‌ها 5،4.0 یا معادل عددی 4،و 0 باشند. T.Zero و عملیات از constraint بیایند؛ overflow با checked پنهان نشود.")

unit(48,
"delegate قرارداد یک فراخوانی شامل پارامترها و خروجی است. method group نام متد سازگار را به delegate تبدیل می‌کند. Func خروجی دارد، Action خروجی ندارد و Predicate<T> یک bool برای T برمی‌گرداند. multicast چند delegate را به ترتیب فراخوانی می‌کند؛ برای delegate دارای خروجی، نتیجهٔ آخرین فراخوانیِ کامل‌شده برمی‌گردد. اگر یکی exception بدهد، بدون رسیدگی مستقل ادامهٔ فهرست تضمین نمی‌شود. invocation list امکان مشاهدهٔ تک‌تک handlerها را می‌دهد، اما سیاست خطا باید صریح باشد. constraint Delegate برای نوع‌های delegate است و هر رفتار قابل اجرا را خودکار امن یا بدون capture نمی‌کند.",
'Func<int,int> operation=Double;\nConsole.WriteLine(operation(4));\nstatic int Double(int n)=>n*2;', '8',
("multicast از دو Func<int> با خروجی 1 و 2 و بدون خطا چه برمی‌گرداند؟", "2، خروجی آخرین delegate", "آرایهٔ خودکار [1,2]", "همیشه مجموع 3", "multicast نتیجه‌های میانی را جمع نمی‌کند؛ برای تجمیع باید invocation list را صریح پردازش کرد."),
simulation("دو فراخوانی، یک خروجی", "ترتیب invocation list ثابت است: اول 1، سپس 2. فراخوانی‌ها را کامل کن.", ["اجرای handler بعدی", "بازگرداندن مجموع فرضی"], ["هنوز خروجی نیست","handler اول=1","handler دوم=2، نتیجهٔ نهایی=2","قرارداد غلط"], [[1,3],[2,3],[-1,-1],[-1,-1]], [2], limit=2),
"محاسبه را به Apply(int,Func<int,int>) بده؛ دو رفتار متفاوت تست کن. multicast خروجی‌دار بساز و نتیجهٔ آخرین handler را با سیاست جمع‌کردن صریح مقایسه کن.",
'Console.WriteLine(Apply(3,n=>n*2));\nFunc<int> first=()=>1,second=()=>2;\nFunc<int> combined=first+second;\nConsole.WriteLine(combined());\nint total=0;foreach(Func<int> handler in combined.GetInvocationList()) total+=handler();\nConsole.WriteLine(total);\nstatic int Apply(int n,Func<int,int> operation)=>operation(n);',
"خروجی‌ها 6،2،3 باشند. رفتار exception یک handler هم در تست جدا روشن شود؛ multicast با تجمیع اشتباه نشود.")

unit(49,
"lambda تابع بی‌نام بیانی یا بلوکی می‌سازد و در context مناسب به delegate یا expression tree تبدیل می‌شود. anonymous method با delegate نحو قدیمی‌تر همان خانواده است. capture متغیر بیرونی، محل آن متغیر را نگه می‌دارد؛ صرفاً عکس مقدار لحظهٔ ساخت نیست. به همین دلیل lambdaهای حلقهٔ for می‌توانند یک شمارندهٔ مشترک نهایی را ببینند. کپی محلی در هر دور یا static lambda با ورودی صریح می‌تواند قرارداد را روشن کند. static local function هم capture ناخواسته را منع می‌کند. natural type و پارامتر اختیاری lambda قواعد نسخه‌ای دارند؛ در C#14 برخی modifierهای پارامتر با شکل ساده‌تر قابل نوشتن‌اند. تخصیص closure باید اندازه‌گیری شود، نه از روی کوتاهی lambda حدس زده شود.",
'int value=2; Func<int> read=()=>value;\nvalue=7;Console.WriteLine(read());', '7',
("lambda متغیر محلی captureشده را چه می‌بیند؟", "محل متغیر و مقدار هنگام فراخوانی آن", "همیشه کپی مقدار زمان تعریف lambda", "همیشه default نوع", "closure محل مشترک را نگه می‌دارد؛ برای snapshot باید کپی مستقل آگاهانه ساخته شود."),
repair("تعمیر capture حلقه", "سه تابع باید 0 و 1 و 2 بدهند؛ فقط receiver تابع را انتخاب کن. local در هر دور از i کپی شده است.", ["()=>i", "()=>local", "()=>3"], [0], ["تابع اول 0", "تابع دوم 1", "تابع سوم 2"], [([0],[False,False,False]),([1],[True,True,True]),([2],[False,False,False])]),
"سه Func<int> در حلقه بساز. نسخهٔ دارای capture مستقیم i را با نسخهٔ کپی محلی مقایسه کن. یک static lambda هم با ورودی صریح بنویس.",
'var actions=new Func<int>[3];\nfor(int i=0;i<3;i++) {int copy=i;actions[i]=()=>copy;}\nConsole.WriteLine(string.Join(",",actions.Select(f=>f())));\nFunc<int,int> twice=static n=>n*2;Console.WriteLine(twice(4));',
"0,1,2 و 8 چاپ شود. نسخهٔ خراب 3,3,3 در تست منفی نشان داده شود؛ static lambda به متغیر بیرون وابسته نباشد.")

unit(50,
"event انتشار رویداد را از اشتراک جدا می‌کند: بیرون نوع معمولاً فقط += و -= مجاز است، نه پاک‌کردن فهرست یا Invoke. EventHandler قرارداد رایج sender و EventArgs دارد. publisher delegate مشترک را نگه می‌دارد، پس subscriber ممکن است تا زمان unsubscribe ناخواسته زنده بماند. برای جداشدن lambda باید همان delegate ذخیره شود؛ نوشتن lambda ظاهراً مشابه معمولاً شیء اشتراک قبلی نیست. add/remove سفارشی می‌تواند سیاست ذخیره را کنترل کند. event به‌تنهایی thread safety، ترتیب بین threadها یا مدیریت خطای subscriber را تضمین نمی‌کند. مالک عمر اشتراک باید در قرارداد روشن باشد.",
'var source=new Source();int calls=0;\nEventHandler handler=(_,_)=>calls++;\nsource.Changed+=handler;source.Raise();source.Changed-=handler;source.Raise();\nConsole.WriteLine(calls);\nclass Source {public event EventHandler? Changed;public void Raise()=>Changed?.Invoke(this,EventArgs.Empty);}', '1',
("برای unsubscribe یک lambda چه نگه داری؟", "همان delegate ثبت‌شده", "فقط متن lambda در یک string", "نام نوع publisher بدون handler", "عمر اشتراک با delegate واقعی مرتبط است؛ lambda تازه الزاماً همان اشتراک نیست."),
simulation("عمر مشترک", "ابتدا handler مشترک است. هدف: یک فراخوانی و سپس جداشدن مشترک.", ["Raise", "Unsubscribe"], ["مشترک،calls=0","مشترک،calls=1","جداشده،calls=1","جداشده،calls=0"], [[1,3],[-1,2],[2,-1],[3,-1]], [2], limit=3),
"notifier با event بساز. اشتراک، یک بار انتشار، unsubscribe و انتشار دوباره را تست کن. قرارداد exception مشترک و مالک جداکردن آن را بنویس.",
'var source=new Source();int calls=0;EventHandler handler=(_,_)=>calls++;\nsource.Changed+=handler;try {source.Raise();} finally {source.Changed-=handler;}\nsource.Raise();if(calls!=1) throw new Exception("subscription");\nConsole.WriteLine("passed");\nclass Source {public event EventHandler? Changed;public void Raise()=>Changed?.Invoke(this,EventArgs.Empty);}',
"مشترک جداشده صدا نخورد و شمارنده دقیقاً 1 باشد. cleanup حتی در خطای مسیر مصرف انجام شود؛ event با delegate عمومی قابل overwrite جایگزین نشود.")

unit(51,
"covariance اجازه می‌دهد producer نوع خاص‌تر در جای producer پایه مصرف شود؛ contravariance جهت consumer را برعکس می‌کند. out و in در اعلان interface یا delegate جهت مجاز پارامتر نوع را بیان می‌کنند و با out/ref پارامتر متد یکی نیستند. این تبدیل‌های variance برای reference typeهاست و مثلاً IEnumerable<int> خودکار IEnumerable<object> نمی‌شود. نوعی که هم تولید و هم مصرف می‌کند معمولاً invariant می‌ماند؛ List<Cat> را نمی‌توان List<Animal> کرد. covariance قدیمی آرایه می‌تواند در نوشتن نوع ناسازگار ArrayTypeMismatchException بدهد. ایمنی compile-time generic را با این رفتار آرایه اشتباه نگیر.",
'IEnumerable<string> names=new[]{"A"};\nIEnumerable<object> objects=names;\nConsole.WriteLine(objects.First());', 'A',
("چرا List<Cat> معمولاً List<Animal> نیست؟", "مصرف‌کننده می‌توانست Dog به لیست Cat اضافه کند.", "چون Cat هرگز Animal نیست.", "چون تمام generics فقط value type می‌پذیرند.", "ظرف mutable هم تولید و هم مصرف می‌کند؛ covariance آزاد، نوشتن نوع نامعتبر را ممکن می‌کرد."),
analogy("جهت producer", "جفت مرجع: Cat به Animal با رابطهٔ «خاص به عام». همان جهت را برای producer فقط‌خواندنی حفظ کن.", ["خاص به عام", "عام به خاص", "IEnumerable<Cat>", "IEnumerable<Animal>", "List<Animal>", "List<Cat>"], [[0,2,3]]),
"تبدیل IEnumerable<string> به IEnumerable<object> و Action<object> به Action<string> را تست کن. تبدیل List<string> به List<object> را در نمونهٔ منفی build کن.",
'IEnumerable<string> source=new[]{"A"};IEnumerable<object> output=source;\nConsole.WriteLine(output.First());\nAction<object> consume=Console.WriteLine;Action<string> textConsumer=consume;\ntextConsumer("B");',
"A و B چاپ شوند. جهت consumer برعکس producer توضیح داده و محدودیت reference typeها ثبت شود.")

unit(52,
"partial type یک نوع را میان چند بخش کامپایل تقسیم می‌کند؛ خروجی چند کلاس مستقل نیست. partial method قواعد declaration و implementation دارد؛ بعضی شکل‌های بدون دسترسی صریح و void می‌توانند implementation اختیاری داشته باشند، اما شکل‌های دارای قرارداد عمومی الزام بیشتری دارند. C#13 partial property و indexer و C#14 partial constructor و event را اضافه کرده‌اند. #if/#define/#undef حضور کد در ساخت را کنترل می‌کنند؛ #nullable و #pragma هشدار و context، #line نگاشت خط و #error/#warning diagnostic ایجاد می‌کنند. #region فقط سازمان ویرایشگر است. تغییر پیکربندی باید رفتار تست‌شده و قابل پیش‌بینی داشته باشد؛ خاموش‌کردن هشدار جای اصلاح دلیل آن نیست.",
'Console.WriteLine(new Item().Value());\npartial class Item {public int Value()=>Read();}\npartial class Item {private int Read()=>4;}', '4',
("دو بخش partial class Item پس از build چند نوع Item می‌سازند؟", "یک نوع ترکیب‌شده", "دو نوع مستقل با انتخاب تصادفی", "هیچ نوعی مگر زمان runtime", "partial تقسیم منبع در زمان کامپایل است؛ reflection زمان اجرا دو نوع مستقل نمی‌بیند."),
groups("کار directive", "کارت‌ها را به شرطی‌سازی حضور کد و تنظیم diagnostic تقسیم کن.", ["حضور کد", "diagnostic و context"], [["#if DEBUG","#define FEATURE"],["#nullable enable","#pragma warning"]]),
"یک partial type را در دو فایل بساز. با #if FEATURE خروجی را تغییر بده؛ هر دو پیکربندی build و تست شوند. یک warning را با علت رفع کن، نه با حذف سراسری هشدارها.",
'Console.WriteLine(new Item().Value());\npartial class Item {public int Value()=>Read();}\npartial class Item\n{\n    private int Read()\n    {\n#if FEATURE\n        return 8;\n#else\n        return 4;\n#endif\n    }\n}',
"بدون FEATURE مقدار 4 و با آن 8 باشد؛ هر دو فایل در یک namespace و assembly باشند. تفاوت declaration و implementation در اعضای partial توضیح داده شود.")

unit(53,
"extension method عضو واقعی نوع را تغییر نمی‌دهد؛ متد static با receiver دارای this در یک static class است و با نحو instance قابل فراخوانی می‌شود. extension جنریک می‌تواند روی قراردادهای عمومی کار کند. عضو instance سازگار معمولاً بر extension مقدم است؛ using و overload resolution در دسترسی و انتخاب اثر دارند. C#14 extension block با receiver نام‌دار می‌تواند property و method نمونه‌ای و با receiver نوعی عضو static یا operator گسترشی تعریف کند. این قابلیت دسترسی private به نوع اصلی نمی‌دهد. null receiver بسته به بدنه ممکن است پذیرفته یا رد شود، پس قراردادش باید روشن باشد. API کوتاه نباید هزینه یا side effect پنهان کند.",
'Console.WriteLine("hi".Twice());\nstatic class TextExtensions {public static string Twice(this string value)=>value+value;}', 'hihi',
("اگر نوع اصلی عضو instance سازگار با همان نام داشته باشد کدام معمولاً مقدم است؟", "عضو instance", "همیشه extension", "همیشه خطای تکرار عضو نوع اصلی", "extension راه حل fallback در حل عضو است و عضو واقعی سازگار را بی‌صدا override نمی‌کند."),
domino("از receiver تا extension", "مسیر resolve یک extension قابل دسترسی را از فراخوانی تا بدنه کامل کن.", ["فراخوانی value.Twice()", "نبود عضو instance سازگار", "extension در scope", "متد static سازگار", "دسترسی خودکار private"], [[0,1],[1,2],[2,3],[1,4]], 3, [1,2], limit=4),
"extension block در C#14 برای string بنویس که property IsBlank داشته باشد. رشتهٔ خالی، فاصله و متن را تست کن؛ قرارداد null را صریح انتخاب کن.",
'Console.WriteLine(" ".IsBlank);Console.WriteLine("x".IsBlank);\nstatic class TextExtensions\n{\n    extension(string? text) {public bool IsBlank=>string.IsNullOrWhiteSpace(text);}\n}',
"True و False چاپ شود؛ null طبق قرارداد true باشد. نمونه با C#14 پایدار کامپایل و عضو instance هم‌نام در تست جدا بررسی شود.")

unit(54,
"تابع خالص برای ورودی یکسان خروجی یکسان می‌دهد و اثر جانبی مشاهده‌پذیر ندارد. higher-order function تابع می‌گیرد یا برمی‌گرداند و composition خروجی یک رفتار را به بعدی وصل می‌کند. تغییرناپذیری داده reasoning را ساده می‌کند اما جای بررسی منبع و خطا نیست. Result یا Option یک مدل صریح برای موفقیت یا نبود داده است؛ نباید هم‌زمان مقدار موفق و خطای ناسازگار آزادانه بسازد. exception برای خطای غیرمنتظره یا قرارداد خاص و Result برای شکست مورد انتظار می‌تواند مناسب باشد. بازگرداندن false بدون علت همیشه کافی نیست؛ حفظ علت شکست از پنهان‌کردن خطا مهم‌تر است.",
'Func<int,int> twice=static x=>x*2;Func<int,int> add=static x=>x+1;\nConsole.WriteLine(add(twice(3)));', '7',
("کدام تابع با ورودی عددی معین خالص‌تر است؟", "x => x * 2 بدون دسترسی به وضعیت بیرون", "تابعی که هر بار ساعت واقعی را می‌خواند", "تابعی که در شمارندهٔ global می‌نویسد", "وابستگی به زمان یا وضعیت پنهان و تغییر بیرونی، تکرارپذیری از روی ورودی را از بین می‌برد."),
argument("علت شکست را نگه دار", "اعتبارسنجی دادهٔ کاربر می‌تواند شکست مورد انتظار داشته باشد. استدلال مرتبط را بساز.", ["Result دارای علت مناسب است", "هر شکست را موفقیت بنامیم", "مصرف‌کننده باید پیام خطا را نشان دهد", "نام متد کوتاه است", "شکست مورد انتظار باید بدون گم‌شدن علت منتقل شود", "همهٔ exceptionها ممنوع‌اند"], [[0,2,4]]),
"زنجیرهٔ Trim،بررسی خالی و parse عدد مثبت بساز. نتیجه دارای موفقیت و علت شکست باشد. null،متن خراب،صفر و عدد معتبر را تست کن؛ side effect در تابع validation نداشته باش.",
'foreach(string? text in new string?[]{null,"bad","0"," 3 "})\n{var result=Parse(text);Console.WriteLine(result.Success?result.Value.ToString():result.Error);}\nstatic Result Parse(string? text)\n{\n    if(string.IsNullOrWhiteSpace(text)) return new(false,0,"empty");\n    if(!int.TryParse(text.Trim(),out int n)) return new(false,0,"format");\n    return n>0?new(true,n,""):new(false,0,"range");\n}\nreadonly record struct Result(bool Success,int Value,string Error);',
"empty،format،range،3 چاپ شود. تابع مستقل از Console باشد؛ وضعیت نامعتبر Result در API عمومی با factory یا نوع مناسب محدود شود.")

unit(55,
"جزء قابل استفادهٔ مجدد باید یک قرارداد واقعی را حل کند. موتور قوانین کوچک می‌تواند predicateهای نوع‌دار را روی داده اجرا کند و نتیجهٔ قابل توضیح بدهد. generic ایمنی نوع، delegate رفتار تعویض‌پذیر و event اعلان تغییر را می‌دهد؛ ترکیب همهٔ این‌ها فقط با مصرف مشخص ارزش دارد. extension برای راحتی مصرف است و نباید وضعیت اصلی را مخفی کند. subscriber مالک عمر دارد و پس از unsubscribe نباید فراخوانی شود. برای نگهداری چند قانون هنوز آرایه کافی است. پیش از تعمیم بیشتر، تست با دو نوع داده و یک وابستگی fake لازم است.",
'Func<int,bool>[] rules={n=>n>0,n=>n<10};\nConsole.WriteLine(rules.All(rule=>rule(5)));', 'True',
("برای اثبات قابل استفاده بودن موتور قوانین چه بررسی مفیدتر است؟", "دو نوع داده و رفتارهای تعویض‌پذیر با همان قرارداد", "افزودن ده type parameter بدون مصرف", "فقط تست نام کلاس", "تعمیم باید در مصرف واقعی و تست ایمنی نوع دیده شود؛ تعداد abstraction معیار کیفیت نیست."),
budget("جزء حداقلی", "موتور باید نوع‌امن، رفتارپذیر و بدون اشتراک رهاشده باشد.", ["Rule<T> با Func<T,bool>", "مالک unsubscribe", "تست دو نوع داده", "reflection دلخواه"], [1,1,1,5], [["نوع","رفتار"],["عمر"],["اثبات"],[]], ["نوع","رفتار","عمر","اثبات"], 3, [0,1,2]),
"RuleSet<T> با آرایهٔ predicate بساز. برای int و string استفاده کن. رویداد Evaluated یک بار اعلام شود و handler جداشده دوباره صدا نخورد؛ خطای predicate پنهان نشود.",
'var rules=new RuleSet<int>(new Func<int,bool>[] {n=>n>0,n=>n<10});\nint calls=0;EventHandler handler=(_,_)=>calls++;rules.Evaluated+=handler;\nif(!rules.Check(5)) throw new Exception("rules");rules.Evaluated-=handler;rules.Check(2);\nConsole.WriteLine(calls);\nclass RuleSet<T>(Func<T,bool>[] rules)\n{\n    public event EventHandler? Evaluated;\n    public bool Check(T value) {bool valid=true;foreach(var rule in rules) if(!rule(value)) {valid=false;break;} Evaluated?.Invoke(this,EventArgs.Empty);return valid;}\n}',
"calls برابر 1 باشد، دادهٔ مردود نتیجهٔ false بدهد و خطای واقعی predicate بلعیده نشود. تست string و عمر اشتراک هم تحویل داده شود.")
