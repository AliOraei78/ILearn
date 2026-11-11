from author_csharp_v2 import *

unit(56,
"List<T> مجموعهٔ ترتیبی با اندازهٔ قابل تغییر است. Count تعداد عنصرهای واقعی و Capacity ظرفیت فضای رزروشده است؛ index معتبر به Count محدود است. افزودن ممکن است آرایهٔ داخلی را بزرگ و داده‌ها را جابه‌جا کند؛ درج وسط هم هزینهٔ جابه‌جایی دارد. ICollection قرارداد تعداد و عملیات عمومی و IList قرارداد index می‌دهد؛ نسخه‌های read-only فقط توانایی مصرف‌کننده را محدود می‌کنند. آرایه برای طول ثابت و List برای تغییر تعداد مناسب‌اند. ArrayList قدیمی object می‌گیرد و برای value typeها boxing و cast ایجاد می‌کند؛ جنریک معمولاً ایمن‌تر است. ظرفیت پیش‌فرض یا الگوریتم رشد دقیق را قرارداد همیشگی زبان فرض نکن.",
'var values=new List<int>(10) {2,4};\nConsole.WriteLine($"{values.Count},{values.Capacity}");', '2,10',
("List با Count=2 و Capacity=10 کدام indexها را می‌پذیرد؟", "0 و 1", "0 تا 9", "فقط 10", "Capacity فضای رزرو است، نه تعداد خانه‌های دارای عنصر قابل دسترسی."),
counter("ظرفیت، عنصر نیست", "ادعا: هر index کوچک‌تر از Capacity در List قابل خواندن است. Count=2 و Capacity=10.", ["index=1","index=2","index=9"], [("عنصر دوم","قابل خواندن",False,"این خانه کمتر از Count است."),("ArgumentOutOfRangeException","قابل خواندن",True,"نخستین خانهٔ بیرون Count است."),("ArgumentOutOfRangeException","قابل خواندن",True,"فضای رزرو عنصر عمومی ایجاد نکرده است.")]),
"لیست با ظرفیت 8 بساز، دو عنصر اضافه و یکی حذف کن. Count و index معتبر را تست کن؛ نسخهٔ read-only view را از snapshot مستقل جدا توضیح بده.",
'var values=new List<int>(8) {3,5};values.RemoveAt(0);\nif(values.Count!=1 || values[0]!=5) throw new Exception("list");\nConsole.WriteLine($"{values.Count},{values.Capacity}");\ntry {_=values[1];} catch(ArgumentOutOfRangeException) {Console.WriteLine("range");}',
"Count=1 و Capacity=8 بماند، index یک رد شود. حذف عنصر الزاماً ظرفیت را کم نمی‌کند؛ خروجی و مرز واقعی تست شوند.")

unit(57,
"IEnumerable<T> امکان گرفتن enumerator و IEnumerator<T> پروتکل MoveNext و Current را می‌دهد. پیش از MoveNext موفق و پس از پایان، خواندن Current قرارداد معتبر عمومی نیست. foreach معمولاً این پروتکل را دنبال و enumerator قابل Dispose را در خروج، حتی break، آزاد می‌کند. foreach pattern می‌تواند بدون پیاده‌سازی صریح interface هم با اعضای مناسب کار کند. تغییر ساختاری بعضی collectionها هنگام پیمایش enumerator را نامعتبر می‌کند؛ از تضمین عمومی برای همهٔ نوع‌ها پرهیز کن. sequence را با collection ذخیره‌شده یکی ندان: ممکن است هر پیمایش کار تازه‌ای انجام دهد.",
'using var iterator=((IEnumerable<int>)new[]{2,4}).GetEnumerator();\nwhile(iterator.MoveNext()) Console.WriteLine(iterator.Current);', '2\n4',
("پیش از خواندن Current اولین عنصر چه لازم است؟", "MoveNext موفق", "فقط new کردن یک آرایهٔ دیگر", "خواندن Count از هر IEnumerable", "پروتکل enumerator با MoveNext به عنصر جاری معتبر می‌رسد؛ IEnumerable الزاماً Count ندارد."),
constraints("پروتکل پیمایش", "این sequence یک عنصر دارد؛ ترتیب یک پیمایش و آزادسازی را بچین.", ["GetEnumerator", "MoveNext=true", "خواندن Current", "MoveNext=false", "Dispose"], [[0,1],[1,2],[2,3],[3,4]]),
"یک sequence سفارشی یا iterator با منبع fake بساز. پیمایش عادی و break پس از اولین عنصر را تست کن و آزادسازی را مشاهده کن. تغییر List حین پیمایش را در تست منفی جدا بررسی کن.",
'foreach(int n in Values()) {Console.WriteLine(n);break;}\nstatic IEnumerable<int> Values()\n{\n    try {yield return 1;yield return 2;}\n    finally {Console.WriteLine("disposed");}\n}',
"1 و disposed چاپ شود؛ break cleanup را حذف نکند. پروتکل و قرارداد Current بدون اتکا به رفتار تصادفی پیاده‌سازی توضیح داده شود.")

unit(58,
"yield return تولید هر عنصر را تا درخواست بعدی مصرف‌کننده متوقف می‌کند و yield break پایان sequence را اعلام می‌کند. فراخوانی iterator معمولاً بدنه را کامل اجرا نمی‌کند؛ شروع و exceptionهای بدنه ممکن است تا MoveNext عقب بیفتند. این deferred execution است. finally هنگام اتمام یا Dispose پیمایش می‌تواند cleanup را انجام دهد؛ فقط ساختن enumerable منبعی را که هنوز وارد بدنه نشده باز نمی‌کند. چندبار پیمایش معمولاً iterator را چندبار اجرا می‌کند. state machine مدل پشت این رفتار است، اما جزئیات allocation به کامپایلر و runtime وابسته است. برای validation فوری می‌توان wrapper عادی ساخت که iterator داخلی را برگرداند.",
'var values=Values();Console.WriteLine("created");\nforeach(int n in values) Console.WriteLine(n);\nstatic IEnumerable<int> Values() {Console.WriteLine("started");yield return 2;}', 'created\nstarted\n2',
("exception داخل بدنهٔ iterator معمولاً چه زمان دیده می‌شود؟", "هنگام پیمایش مسیر مربوط", "حتماً هنگام انتساب متغیر enumerable", "هرگز؛ yield خطا را حذف می‌کند", "بدنهٔ iterator deferred است؛ برای خطای فوری باید validation را بیرون iterator داخلی انجام داد."),
experiment("زمان اجرای منبع", "مشاهدات منبع شمارنده‌دار را باز کن.", ["ساخت enumerable: شمارنده 0", "یک پیمایش کامل: شمارنده 1", "دو پیمایش مستقل: شمارنده 2"], ["بدنه با پیمایش اجرا می‌شود و ممکن است تکرار شود.", "فراخوانی اولیه همیشه تمام خروجی را ذخیره می‌کند.", "yield هر خطا را نادیده می‌گیرد."], 0),
"iterator اعداد 1 و 2 با شمارندهٔ شروع بساز. قبل و بعد از دو پیمایش شمارنده را تست کن؛ با ToArray نشان بده snapshot بعدی دیگر منبع را دوباره اجرا نمی‌کند.",
'int starts=0;var source=Values();\nif(starts!=0) throw new Exception("eager");\n_ = source.ToArray(); _ = source.ToArray();\nConsole.WriteLine(starts);\nint[] snapshot=source.ToArray();foreach(int n in snapshot) {} foreach(int n in snapshot) {}\nConsole.WriteLine(starts);\nIEnumerable<int> Values() {starts++;yield return 1;yield return 2;}',
"شمارنده پس از دو پیمایش 2 و پس از ساخت یک snapshot و دو مصرف آن 3 باشد. زمان خطا و cleanup در خروج زودهنگام هم تست شود.")

unit(59,
"Dictionary<TKey,TValue> نگاشت کلید به مقدار و HashSet<T> عضویت بدون تکرار را با equality comparer مشخص می‌دهند. TryGetValue نبود کلید را بدون exception معمول خواندن مستقیم مدیریت می‌کند. union،intersection و difference در HashSet معنای عملیات مجموعه‌اند. hash برای محدودکردن جست‌وجوست؛ collision ممکن و equality همچنان لازم است. هزینهٔ مورد انتظار lookup اغلب نزدیک ثابت است، اما بدترین حالت و رشد ظرفیت را نباید نادیده گرفت. کلیدی که hash یا equality آن پس از درج عوض شود قرارداد جست‌وجو را می‌شکند. برای واژه‌ها سیاست مقایسهٔ حروف و فرهنگ را قبل از درج مشخص کن.",
'var words=new HashSet<string>(StringComparer.OrdinalIgnoreCase) {"A","a"};\nConsole.WriteLine(words.Count);', '1',
("برای شمارش واژه بدون تفاوت حروف چه چیز باید از آغاز مشخص باشد؟", "comparer یکسان برای کلیدها", "ترتیب چاپ خروجی فقط", "عدد تصادفی برای GetHashCode هر بار", "سیاست برابری در Dictionary/HashSet تعیین می‌کند کدام کلیدها یکی‌اند؛ hash باید پایدار و سازگار بماند."),
groups("ظرف مناسب", "سناریوها را به نگاشت کلید/مقدار و عضویت یکتا تقسیم کن.", ["Dictionary", "HashSet"], [["تعداد هر واژه","امتیاز هر شناسه"],["آیا واژه دیده شده؟","حذف نام‌های تکراری"]]),
"واژه‌های A,a,B را با OrdinalIgnoreCase بشمار. کلید مفقود C را با TryGetValue بررسی کن؛ HashSet همان ورودی دو عضو داشته باشد.",
'var counts=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);\nforeach(string word in new[]{"A","a","B"}) {counts.TryGetValue(word,out int count);counts[word]=count+1;}\nConsole.WriteLine(counts["a"]);Console.WriteLine(counts.TryGetValue("C",out _));\nConsole.WriteLine(new HashSet<string>(counts.Keys,StringComparer.OrdinalIgnoreCase).Count);',
"خروجی‌ها 2،False،2 باشند. نبود کلید با عدد صفر ذخیره‌شده اشتباه نشود؛ comparer در مسیرها ثابت بماند.")

unit(60,
"Queue ترتیب FIFO، Stack ترتیب LIFO و LinkedList پیوند بین گره‌ها را ارائه می‌کنند؛ درج گرهٔ شناخته‌شده با جست‌وجوی آن فرق هزینه دارد. SortedList،SortedDictionary و SortedSet با comparer ترتیب کلید را نگه می‌دارند و هزینهٔ درج،جست‌وجو و حافظه یکسان ندارند. PriorityQueue کمترین اولویت را زودتر خارج می‌کند و برای اولویت برابر تضمین عمومی FIFO ندارد. انتخاب باید از عملیات غالب بیاید: صف کار،undo و اولویت زمان‌بندی نیازهای متفاوت‌اند. مجموعهٔ کوچک گاهی با آرایه و sort ساده کافی است. پیچیدگی مجانبی تنها معیار نیست؛ اندازه و الگوی مصرف واقعی را هم ثبت کن.",
'var queue=new Queue<int>();queue.Enqueue(1);queue.Enqueue(2);\nConsole.WriteLine(queue.Dequeue());', '1',
("برای undo آخرین عمل کدام قرارداد مناسب‌تر است؟", "Stack با LIFO", "Queue با FIFO", "HashSet بدون ترتیب", "undo معمولاً آخرین تغییر را اول برمی‌گرداند؛ این همان ترتیب LIFO است."),
analogy("صف و ترتیب", "جفت مرجع: Queue به FIFO. همان رابطهٔ «ساختار به ترتیب خروج» را در جفت دوم بساز.", ["ساختار به ترتیب خروج", "عضو به مقدار", "Stack", "LIFO", "HashSet", "FIFO"], [[0,2,3]]),
"سه کار با اولویت 3،1،2 را در PriorityQueue قرار بده و ترتیب خروج را تست کن. برای دو اولویت برابر ترتیب خاصی مطالبه نکن؛ اگر FIFO لازم است tie-breaker صریح اضافه کن.",
'var jobs=new PriorityQueue<string,int>();jobs.Enqueue("C",3);jobs.Enqueue("A",1);jobs.Enqueue("B",2);\nwhile(jobs.TryDequeue(out string? job,out int priority)) Console.WriteLine($"{job}:{priority}");',
"A:1،B:2،C:3 چاپ شوند. قرارداد اولویت برابر در تست به رفتار مستندنشدۀ داخلی وابسته نباشد.")

unit(61,
"read-only view فقط عملیات قابل انجام از آن مرجع را محدود می‌کند؛ تغییر منبع ممکن است در view دیده شود. snapshot کپیِ یک لحظه است، ولی عنصرهای مرجعی آن هنوز می‌توانند مشترک باشند. ImmutableArray/List/Dictionary تغییر ساختاری را با نسخهٔ تازه بیان می‌کنند؛ immutability ظرف با عمق عنصرها یکی نیست. builder برای ساخت مرحله‌ای و تبدیل نهایی مناسب است. FrozenSet/Dictionary برای دادهٔ عمدتاً ثابت و lookupهای بعد از ساخت طراحی شده‌اند؛ هزینهٔ ساخت و نوع داده باید در انتخاب دیده شود. هیچ نام read-only به‌تنهایی thread safety تمام گراف شیء را تضمین نمی‌کند.",
'var source=new List<int>{1};var view=source.AsReadOnly();int[] snapshot=source.ToArray();\nsource[0]=9;Console.WriteLine($"{view[0]},{snapshot[0]}");', '9,1',
("ReadOnlyCollection از یک List چه چیزی را تضمین نمی‌کند؟", "ثابت ماندن داده در صورت تغییر لیست اصلی", "نداشتن Add از طریق همان view", "خواندن عناصر موجود", "view مسیر نوشتن را محدود می‌کند، نه مالکیت یا تغییرات منبع را."),
counter("فقط‌خواندنی، ثابت نیست", "ادعا: AsReadOnly روی List یک snapshot مستقل می‌سازد.", ["تغییر source[0] پس از ساخت view", "تغییر source[0] پس از ToArray"], [("view مقدار تازه را نشان می‌دهد","view مقدار قدیمی دارد",True,"view به منبع زنده متصل است."),("آرایهٔ کپی مقدار قدیمی دارد","snapshot مقدار قدیمی دارد",False,"ToArray کپی ساختاری مستقل می‌سازد.")]),
"یک read-only view و یک snapshot از List بساز و پس از تغییر منبع تفاوت را تست کن. FrozenDictionary از تنظیمات بساز؛ تغییر dictionary اصلی نباید دادهٔ frozen را عوض کند.",
'using System.Collections.Frozen;\nvar source=new Dictionary<string,int>{{"a",1}};var frozen=source.ToFrozenDictionary();\nsource["a"]=9;Console.WriteLine(frozen["a"]);\nvar list=new List<int>{1};var view=list.AsReadOnly();var snapshot=list.ToArray();list[0]=7;\nConsole.WriteLine($"{view[0]},{snapshot[0]}");',
"1 و 7,1 چاپ شود. استقلال ساختار از تغییرناپذیری عمیق عنصرها جدا بیان شود؛ هزینهٔ ساخت frozen به‌عنوان رایگان فرض نشود.")

unit(62,
"collection initializer روی نوع قابل پیمایش با Add مناسب، فراخوانی‌های افزودن را کوتاه می‌کند. collection expression با [] و spread با .. از C#12 بر اساس نوع مقصد مجموعه را می‌سازد؛ var x=[] بدون مقصد کافی نیست. initializer و expression قواعد و هزینهٔ یکسانی ندارند. از C#13 params می‌تواند مجموعه‌های پشتیبانی‌شده از جمله ReadOnlySpan و interfaceهای مشخص را بپذیرد؛ تمام IEnumerable دلخواه الزاماً قرارداد ساخت مناسب ندارند. انتخاب overload با نوع مقصد و نسخهٔ زبان مرتبط است. materialization ممکن است تخصیص یا کپی داشته باشد؛ نحو کوتاه تضمین zero-allocation نیست.",
'int[] first=[1,2];int[] all=[..first,3];\nConsole.WriteLine(string.Join(",",all));', '1,2,3',
("چرا var values = []; به‌تنهایی کافی نیست؟", "collection expression به نوع مقصد نیاز دارد.", "چون C# هیچ آرایهٔ خالی ندارد.", "چون spread فقط در JSON معتبر است.", "شکل [] نوع collection مستقل ذاتی ندارد؛ مقصدی مثل int[] یا List<int> باید معلوم باشد."),
repair("نوع مقصد گمشده", "متن مقداردهی [] است. نوع مقصد را طوری انتخاب کن که سه عدد صحیح ذخیره شوند.", ["var", "int[]", "int"], [0], ["collection expression مقصد دارد", "سه عنصر قابل نگهداری‌اند"], [([0],[False,False]),([1],[True,True]),([2],[False,False])], code="TYPE values = [1,2,3];"),
"متد Sum با params ReadOnlySpan<int> بنویس و با آرگومان‌های جدا،آرایه و collection expression تست کن. خروجی و انتخاب overload را روشن نگه دار.",
'Console.WriteLine(Sum(1,2,3));int[] values=[4,5];Console.WriteLine(Sum(values));Console.WriteLine(Sum([2,3]));\nstatic int Sum(params ReadOnlySpan<int> values) {int result=0;foreach(int n in values) result+=n;return result;}',
"6،9،5 چاپ شود. نمونه با C#13 یا بالاتر کامپایل و تفاوت params آرایه‌ای با قرارداد جدید توضیح داده شود.")

unit(63,
"LINQ روی sequenceها فیلتر،projection و ترتیب را با API مشترک بیان می‌کند. Where عضوهای مطابق شرط و Select شکل خروجی را انتخاب می‌کنند؛ OrderBy و ThenBy ترتیب اصلی و تکمیلی‌اند. query syntax با from/where/select/orderby/let به فراخوانی عملگرها تبدیل می‌شود و زبان اجرایی جدا نیست. Any وجود،All برقرار بودن شرط برای همه و Contains عضویت را می‌سنجد؛ All روی دنبالهٔ خالی true است. ToArray/ToList نتیجه را materialize می‌کنند. بیشتر queryها تا پیمایش اجرا نمی‌شوند، پس تغییر منبع یا side effect زمان مشاهده را عوض می‌کند. برای مثال کوچک ابتدا خروجی را دستی ردگیری کن.",
'int[] values={3,1,2};\nConsole.WriteLine(string.Join(",",values.Where(n=>n>1).OrderBy(n=>n).Select(n=>n*10)));', '20,30',
("All روی sequence خالی چه می‌دهد؟", "True؛ مورد نقض‌کننده‌ای وجود ندارد.", "False در همهٔ APIها", "همیشه exception", "قرارداد All بررسی نبود مورد ناقض است؛ اگر وجود عنصر هم لازم است Any را جدا بررسی کن."),
pipeline("ساخت query", "از [3,1,2] به [2,3] برس. فیلتر >1 و مرتب‌سازی هر دو لازم‌اند و می‌توانند در دو ترتیب معتبر بیایند.", ["Where n>1","OrderBy n","Take 1"], ["[3,1,2]","[3,2]","[1,2,3]","[2,3]","[3]","[1]"], [[1,2,4],[1,3,4],[3,2,5],[3,3,-1],[4,4,4],[-1,5,5]], [3], limit=3),
"گزارش اعداد زوج [5,2,4,1] به ترتیب نزولی بساز و با query syntax هم بنویس. خروجی دو شکل با SequenceEqual یکسان و ورودی خالی بی‌خطا باشد.",
'int[] values={5,2,4,1};\nvar first=values.Where(n=>n%2==0).OrderByDescending(n=>n).ToArray();\nvar second=(from n in values where n%2==0 orderby n descending select n).ToArray();\nif(!first.SequenceEqual(second)) throw new Exception("query");\nConsole.WriteLine(string.Join(",",first));',
"4,2 چاپ شود. query و method syntax معنا و خروجی یکسان داشته باشند؛ زمان materialization صریح باشد.")

unit(64,
"GroupBy گروه‌ها را هنگام پیمایش می‌سازد و ToLookup نمای گروه‌بندی materialized برای lookup می‌دهد. Join تطبیق کلیدها و GroupJoin گروه متناظر را می‌سازد؛ SelectMany دنباله‌های تو‌در‌تو را تخت می‌کند. Aggregate و Sum/Min/Max/Average قرارداد خالی متفاوت دارند. Zip کوتاه‌ترین دنباله را جفت،Chunk بخش‌های اندازه‌دار می‌سازد. Distinct/Union/Intersect/Except و Byهای متناظر سیاست کلید دارند؛ Take/Skip بخش‌بندی و First/Single و Defaultها تعداد مورد انتظار را بیان می‌کنند. Range/Repeat/Empty تولید و Cast/OfType تبدیل یا فیلتر نوع انجام می‌دهند. نام عملگر باید با قرارداد خالی،تکراری و چندتایی سنجیده شود.",
'int[][] rows={new[]{1,2},new[]{3}};\nConsole.WriteLine(string.Join(",",rows.SelectMany(row=>row)));', '1,2,3',
("برای دقیقاً یک نتیجه، کدام قرارداد وجود چند نتیجه را خطا می‌داند؟", "Single", "First", "Take(1)", "Single بر یکتایی اصرار می‌کند؛ First فقط اولین را می‌گیرد و وجود بقیه را رد نمی‌کند."),
groups("شکل خروجی LINQ", "کارت‌ها را به خروجی تخت و خروجی گروه‌بندی‌شده تقسیم کن.", ["تخت یا تک‌دنباله", "گروه‌بندی"], [["SelectMany","Concat"],["GroupBy","ToLookup"]]),
"سفارش‌ها را بر اساس CustomerId با مشتری‌ها join کن و جمع مبلغ هر مشتری را بساز. مشتری بدون سفارش و شناسهٔ تکراری را با قرارداد روشن تست کن.",
'var customers=new[]{(Id:1,Name:"A"),(Id:2,Name:"B")};\nvar orders=new[]{(CustomerId:1,Amount:3),(CustomerId:1,Amount:4)};\nvar report=customers.GroupJoin(orders,c=>c.Id,o=>o.CustomerId,(c,items)=>new{c.Name,Total=items.Sum(o=>o.Amount)});\nforeach(var row in report) Console.WriteLine($"{row.Name}:{row.Total}");',
"A:7 و B:0 چاپ شوند. قرارداد کلید تکراری و دادهٔ خالی مستند و تست شود؛ GroupJoin مشتری بدون سفارش را حذف نکند.")

unit(65,
"lazy/eager زمان شروع و streaming/buffering نیاز به نگهداری ورودی را توصیف می‌کنند؛ این دو محور یکی نیستند. Where می‌تواند streaming باشد ولی OrderBy پیش از خروجی مرتب،داده را buffer می‌کند. چندبار مصرف query ممکن است منبع را چندبار اجرا کند و side effect یا هزینه را تکرار کند. ترتیب فیلتر و projection فقط وقتی با حفظ معنا قابل جابه‌جایی است که وابستگی داده و side effect اجازه دهند. Any برای پرسش وجود روشن‌تر از شمارش کامل است، هرچند بعضی Countها از قرارداد collection بهینه می‌شوند. materialization در مرز درست snapshot می‌دهد اما حافظه مصرف می‌کند. بهینه‌سازی را با خروجی ثابت و تعداد پیمایش اندازه بگیر.",
'int reads=0;IEnumerable<int> Source(){reads++;yield return 1;yield return 2;}\nvar query=Source().Where(n=>n>0);_ = query.Count();_ = query.ToArray();\nConsole.WriteLine(reads);', '2',
("برای جلوگیری از اجرای دوبارهٔ منبع پرهزینه در دو مصرف چه می‌توان کرد؟", "یک بار materialize و همان snapshot را مصرف کرد.", "فقط نام متغیر query را عوض کرد.", "همیشه OrderBy اضافه کرد.", "snapshot نتیجه را یک بار می‌سازد؛ باید هزینهٔ حافظه و نیاز به تازگی داده را هم در نظر گرفت."),
spectrum("شمارش شروع منبع", "query تنبل از منبع شمارنده‌دار ساخته شده؛ Count و سپس ToArray روی همان query اجرا می‌شوند.", "تعداد شروع بدنهٔ iterator را بشمار. در این نمونه Count و ToArray هر کدام query را یک بار می‌پیمایند.", 0, 6, 2, 2, ["فقط ساخت query: 0", "یک پیمایش: 1"]),
"منبع شمارنده‌دار را یک بار ToArray کن و سپس Count و مجموع snapshot را بخوان. ثابت کن یک بار منبع اجرا شده و نتیجه با نسخهٔ تکرارشونده برابر است.",
'int starts=0;IEnumerable<int> Source(){starts++;yield return 2;yield return 4;}\nint[] snapshot=Source().ToArray();\nConsole.WriteLine($"{snapshot.Length},{snapshot.Sum()},{starts}");\nif(starts!=1) throw new Exception("enumeration");',
"2,6,1 چاپ شود؛ snapshot برای دو مصرف کافی باشد و side effect منبع تکرار نشود.")

unit(66,
"delegate رفتار قابل فراخوانی و Expression<TDelegate> درخت توصیف آن رفتار است. nodeها پارامتر،constant و operation را مدل می‌کنند؛ Compile درخت را به delegate محلی تبدیل می‌کند. IQueryable درخت را همراه provider حمل می‌کند تا آن را به عملیات مقصد ترجمه کند؛ هر متد دلخواه C# قابل ترجمه به SQL یا سرویس دیگر نیست. تغییر مرز به IEnumerable ادامه را محلی می‌کند و می‌تواند دادهٔ زیادی منتقل کند. هنگام ترکیب predicateها باید پارامترهای سازگار و محدودیت nodeها را رعایت کرد؛ چسباندن متن expression ابزار امنی نیست. lambda بلوکی و همهٔ قابلیت‌های جدید زبان الزاماً expression tree پشتیبانی‌شده ندارند.",
'using System.Linq.Expressions;\nExpression<Func<int,bool>> positive=n=>n>0;\nConsole.WriteLine(positive.Compile()(3));', 'True',
("آیا هر Func دلخواه به‌صورت خودکار توسط provider به SQL ترجمه می‌شود؟", "خیر؛ provider فقط قراردادها و nodeهای پشتیبانی‌شده را ترجمه می‌کند.", "بله؛ هر کد C# همان SQL است.", "فقط با افزودن async همهٔ ترجمه‌ها ممکن می‌شوند.", "درخت عبارت توصیف است و قابلیت ترجمه محدود به provider؛ Compile اجرای محلی می‌سازد."),
questions("رفتار یا توصیف؟", "یک شرط فیلتر با یکی از سه نمایش نگهداری شده است. با پرسش‌ها قابلیت‌های آن را کشف و نمایش پنهان را تشخیص بده.", ["Func<int,bool>", "Expression<Func<int,bool>>", "string کد"], ["آیا ساختار node قابل بازدید است؟","آیا Compile رسمی به delegate دارد؟"], [[False,False],[True,True],[False,False]], 1, limit=2),
"predicate عدد بزرگ‌تر از 3 را به‌صورت Expression بساز و Compile کن. همان شرط را روی دادهٔ محلی تست کن؛ در گزارش توضیح بده چرا این تست ترجمهٔ یک provider واقعی را ثابت نمی‌کند.",
'using System.Linq.Expressions;\nvar parameter=Expression.Parameter(typeof(int),"n");\nvar body=Expression.GreaterThan(parameter,Expression.Constant(3));\nvar tree=Expression.Lambda<Func<int,bool>>(body,parameter);\nvar predicate=tree.Compile();\nConsole.WriteLine($"{predicate(3)},{predicate(4)}");',
"False,True چاپ شود؛ مرز برابر 3 تست شود. درخت با همان ParameterExpression ساخته شود؛ ادعای پشتیبانی SQL بدون تست provider مطرح نشود.")

unit(67,
"پروژهٔ تحلیل داده باید انتخاب collection،سیاست کلید و query را با نیاز واقعی هماهنگ کند. دادهٔ خالی و تکراری پیش از گزارش قرارداد می‌خواهند. materialization در مرز ورودی می‌تواند تکرار پردازش را کنترل کند؛ اگر داده بسیار بزرگ است هزینهٔ حافظه را هم بسنج. گزارش باید از دیتابیس مستقل قابل تست باشد و ترتیب خروجی ثابت برای snapshot تست داشته باشد. comparer واژه یا شناسه از آغاز معلوم باشد. اندازهٔ ورودی بزرگ‌تر برای سنجش تعداد پیمایش مفید است، اما زمان‌گیری تک‌اجرای کوچک مدرک کارایی نیست. query خوانا با دو مرحلهٔ روشن اغلب بهتر از عبارت فشردهٔ مبهم است.",
'var rows=new[]{("A",2),("B",1),("A",3)};\nforeach(var group in rows.GroupBy(r=>r.Item1).OrderBy(g=>g.Key)) Console.WriteLine($"{group.Key}:{group.Sum(r=>r.Item2)}");', 'A:5\nB:1',
("برای گزارش تکرارپذیر کدام دادهٔ قرارداد ضروری است؟", "comparer کلید و ترتیب خروجی مشخص", "ترتیب تصادفی Dictionary در همهٔ runtimeها", "فقط رنگ جدول", "نتیجه باید با سیاست برابری و ترتیب روشن مستقل از محیط تولید شود."),
budget("گزارش کافی", "نیازها: کلید ثابت،خروجی مرتب،یک پیمایش منبع. هزینه‌ها فرضی‌اند.", ["comparer صریح", "GroupBy و OrderBy", "snapshot یک‌باره", "دیتابیس واقعی برای هر تست"], [1,1,1,5], [["کلید"],["ترتیب"],["پیمایش"],[]], ["کلید","ترتیب","پیمایش"], 3, [0,1,2]),
"گزارش فروش با کلید بدون تفاوت حروف بساز. A و a جمع شوند؛ ورودی خالی گزارش خالی بدهد. منبع را شمارش کن و ثابت کن فقط یک بار خوانده شده است.",
'int starts=0;IEnumerable<(string Key,int Amount)> Source(){starts++;yield return ("A",2);yield return ("a",3);}\nvar report=Source().GroupBy(r=>r.Key,StringComparer.OrdinalIgnoreCase).Select(g=>(Key:g.Key,Total:g.Sum(r=>r.Amount))).OrderBy(r=>r.Key,StringComparer.Ordinal).ToArray();\nforeach(var row in report) Console.WriteLine($"{row.Key}:{row.Total}");\nConsole.WriteLine(starts);',
"A:5 و 1 چاپ شود. ورودی خالی و کلید تکراری تست شوند؛ هیچ وابستگی دیتابیس در منطق گزارش نباشد.")
