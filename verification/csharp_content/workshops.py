"""Complete resource, network, generator and host workshop references."""
from author_csharp_v2 import UNITS


def reference(number, code, rubric=None):
    UNITS[number]["reference"] = code.strip()
    if rubric:
        UNITS[number]["rubric"] = rubric


reference(74, '''
using Microsoft.Extensions.Logging;
var capture = new Capture();
using var factory = LoggerFactory.Create(builder => builder.AddProvider(capture));
var logger = factory.CreateLogger("Orders");
logger.LogWarning(new InvalidOperationException("storage"), "Order {OrderId} failed", 7);
if (capture.Values.Keys.Any(key => key.Contains("password", StringComparison.OrdinalIgnoreCase) || key.Contains("token", StringComparison.OrdinalIgnoreCase)))
    throw new Exception("Sensitive field");
Console.WriteLine($"{capture.Values["OrderId"]},{capture.Error?.GetType().Name}");
sealed class Capture : ILoggerProvider, ILogger
{
    public Dictionary<string, object?> Values { get; } = [];
    public Exception? Error { get; private set; }
    public ILogger CreateLogger(string categoryName) => this;
    public bool IsEnabled(LogLevel level) => true;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
    {
        Error = error;
        if (state is IEnumerable<KeyValuePair<string, object?>> fields)
            foreach (var field in fields) Values[field.Key] = field.Value;
    }
    public void Dispose() { }
}
''', "logger واقعی template ساختاری و exception را جدا دریافت کند. OrderId برابر 7 و نوع خطا InvalidOperationException حفظ شود؛ هیچ password یا token در fieldها و پیام ثبت نشود. provider تست از شبکه و حساب واقعی مستقل باشد.")

reference(75, '''
string directory = Path.Combine(Path.GetTempPath(), "ilearn-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    Check("memory", new MemoryStore());
    Check("file", new FileStore(Path.Combine(directory, "data.txt")));
}
finally { Directory.Delete(directory, true); }
static void Check(string name, IStore store)
{
    foreach (string value in new[] { "", "سلام" })
    {
        store.Write(value);
        if (store.Read() != value) throw new Exception("Round-trip contract");
    }
    Console.WriteLine(name + ":2");
}
interface IStore { void Write(string value); string Read(); }
sealed class MemoryStore : IStore
{
    string value = "";
    public void Write(string text) => value = text;
    public string Read() => value;
}
sealed class FileStore(string path) : IStore
{
    public void Write(string value) => File.WriteAllText(path, value, System.Text.Encoding.UTF8);
    public string Read() => File.ReadAllText(path, System.Text.Encoding.UTF8);
}
''', "قرارداد یکسان روی حافظه و فایل برای خالی و فارسی پاس شود؛ پوشهٔ منحصر به تست در finally پاک شود. تست نبود فایل و شکست نوشتن جدا باشد و دادهٔ واقعی کاربر لمس نشود.")

reference(79, '''
using System.Text.Json;
string directory = Path.Combine(Path.GetTempPath(), "ilearn-save-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    string path = Path.Combine(directory, "data.json");
    var store = new JsonStore(path);
    store.Save(new Save(1, [2, 3]));
    Save current = store.Load();
    Console.WriteLine(current.Items.Sum());
    try { new JsonStore(path, () => throw new IOException("injected")).Save(new Save(1, [9])); }
    catch (IOException) { }
    if (store.Load().Items.Sum() != 5) throw new Exception("Previous file lost");
    File.WriteAllText(path, "broken");
    try { current = store.Load(); } catch (JsonException) { }
    Console.WriteLine(current.Items.Sum());
}
finally { Directory.Delete(directory, true); }
sealed record Save(int Version, int[] Items);
sealed class JsonStore(string path, Action? beforeCommit = null)
{
    static Save Validate(Save? value)
    {
        if (value is null || value.Version != 1 || value.Items is null || value.Items.Length > 1000 || value.Items.Any(n => n < 0))
            throw new InvalidDataException("Invalid save");
        return value;
    }
    public Save Load()
    {
        if (new FileInfo(path).Length > 10000) throw new InvalidDataException("Too large");
        return Validate(JsonSerializer.Deserialize<Save>(File.ReadAllText(path)));
    }
    public void Save(Save data)
    {
        string json = JsonSerializer.Serialize(Validate(data));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, json);
            beforeCommit?.Invoke();
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
''', "round-trip جمع 5 بدهد. خطای تزریق‌شده پیش از commit فایل قبلی را حفظ کند؛ load خراب وضعیت در حافظه را تغییر ندهد. نسخهٔ ناشناخته،منفی و اندازهٔ زیاد رد شوند. جایگزینی فایل در این نمونه روی همان filesystem است و تضمین دوام در قطع برق نیست.")

reference(85, '''
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
var handler = new Fake([(HttpStatusCode.ServiceUnavailable, ""), (HttpStatusCode.OK, """{"Value":3}""")]);
using var http = new HttpClient(handler);
int pauses = 0;
var api = new Api(http, (_, token) => { token.ThrowIfCancellationRequested(); pauses++; return Task.CompletedTask; });
Console.WriteLine($"{await api.ReadAsync(CancellationToken.None)},{handler.Calls},{pauses}");
using var badHttp = new HttpClient(new Fake([(HttpStatusCode.OK, "broken")]));
try { await new Api(badHttp, (_, _) => Task.CompletedTask).ReadAsync(CancellationToken.None); throw new Exception("Expected invalid JSON"); }
catch (JsonException) { }
using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
try { await api.ReadAsync(cancelled.Token); throw new Exception("Expected cancellation"); }
catch (OperationCanceledException) { }
sealed record Item(int Value);
sealed class Api(HttpClient http, Func<int, CancellationToken, Task> backoff)
{
    public async Task<int> ReadAsync(CancellationToken token)
    {
        for (int attempt = 0; ; attempt++)
        {
            using var response = await http.GetAsync("https://example.test/item", HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < 1)
            {
                await backoff(attempt + 1, token);
                continue;
            }
            response.EnsureSuccessStatusCode();
            var item = await response.Content.ReadFromJsonAsync<Item>(cancellationToken: token);
            if (item is null || item.Value < 0) throw new InvalidDataException("item");
            return item.Value;
        }
    }
}
sealed class Fake((HttpStatusCode Status, string Body)[] steps) : HttpMessageHandler
{
    public int Calls { get; private set; }
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var step = steps[Math.Min(Calls++, steps.Length - 1)];
        return Task.FromResult(new HttpResponseMessage(step.Status) { Content = new StringContent(step.Body) });
    }
}
''', "GET فقط یک retry برای 503 داشته باشد؛ خروجی 3,2,1 باشد. JSON خراب و لغو مسیر مستقل داشته باشند. در برنامهٔ واقعی backoff محدود و tokenپذیر تزریق شود؛ تست از تأخیر و شبکهٔ واقعی مستقل است و client تزریق‌شده را Api مالک نیست.")

reference(93, '''
using System.Diagnostics;
var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
using var stop = new CancellationTokenSource();
async Task Worker()
{
    entered.SetResult();
    await release.Task.WaitAsync(stop.Token);
}
Task worker = Worker();
await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
stop.Cancel();
try { await worker; throw new Exception("Expected cancellation"); }
catch (OperationCanceledException) { Console.WriteLine("cancelled"); }
release.SetResult();
var names = new List<string>();
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == "ILearn.Sample",
    Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
    ActivityStopped = activity => names.Add(activity.OperationName)
};
ActivitySource.AddActivityListener(listener);
using var source = new ActivitySource("ILearn.Sample");
using (var parent = source.StartActivity("parent")) { using var child = source.StartActivity("child"); }
Console.WriteLine(string.Join(",", names));
''', "worker با signal صریح وارد انتظار شود و لغو بدون sleep مشاهده شود. task زیرینِ gate هم تعیین تکلیف شود. listener پیش از span فعال و در پایان آزاد شود؛ child,parent ثبت شوند و secret وارد trace نشود.")

reference(103, '''
using System.Diagnostics;
const string input = "key=42";
if (Baseline(input) != WithSpan(input)) throw new Exception("Different behavior");
for (int i = 0; i < 100; i++) { _ = Baseline(input); _ = WithSpan(input); }
Console.WriteLine("equivalent");
Measure("baseline", Baseline);
Measure("span", WithSpan);
static int Baseline(string text)
{
    int index = text.IndexOf('=');
    if (index <= 0) throw new FormatException();
    return int.Parse(text[(index + 1)..]);
}
static int WithSpan(string text)
{
    int index = text.IndexOf('=');
    if (index <= 0) throw new FormatException();
    return int.Parse(text.AsSpan(index + 1));
}
static void Measure(string name, Func<string, int> parser)
{
    long before = GC.GetAllocatedBytesForCurrentThread();
    long start = Stopwatch.GetTimestamp();
    int checksum = 0;
    for (int i = 0; i < 10000; i++) checksum = checked(checksum + parser("key=42"));
    long ticks = Stopwatch.GetTimestamp() - start;
    long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
    if (checksum != 420000) throw new Exception("checksum");
    Console.WriteLine($"{name} ticks: {ticks}; bytes: {bytes}");
}
''', "دو parser هم‌رفتار باشند و checksum برابر 420000 بماند. زمان و allocation واقعاً اندازه‌گیری شوند؛ مقادیر به دستگاه وابسته‌اند و این آزمایش ساده جای تحلیل آماری BenchmarkDotNet نیست. Release،SDK،داده و warm-up همراه گزارش ثبت شوند؛ هیچ درصد بهبود ثابت ادعا نشود.")

reference(107, '''
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
[Generator]
public sealed class HelloGenerator : IIncrementalGenerator
{
    static readonly DiagnosticDescriptor Invalid = new("IL001", "Unsupported target", "Type '{0}' must be a non-generic partial class in the global namespace", "ILearn", DiagnosticSeverity.Error, true);
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(output => output.AddSource("GenerateHelloAttribute.g.cs", "[global::System.AttributeUsage(global::System.AttributeTargets.Class)] internal sealed class GenerateHelloAttribute : global::System.Attribute {}"));
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            "GenerateHelloAttribute",
            static (node, _) => node is ClassDeclarationSyntax,
            static (target, _) => (Node: (ClassDeclarationSyntax)target.TargetNode, Symbol: (INamedTypeSymbol)target.TargetSymbol));
        context.RegisterSourceOutput(targets, static (output, target) =>
        {
            if (!target.Node.Modifiers.Any(SyntaxKind.PartialKeyword) || target.Symbol.Arity != 0 || !target.Symbol.ContainingNamespace.IsGlobalNamespace || target.Symbol.ContainingType is not null)
            {
                output.ReportDiagnostic(Diagnostic.Create(Invalid, target.Node.Identifier.GetLocation(), target.Symbol.Name));
                return;
            }
            output.AddSource(target.Symbol.Name + ".Hello.g.cs", "partial class " + target.Node.Identifier.Text + " { public const string Hello = \\\"Hello\\\"; }");
        });
    }
}
''', "ورودی [GenerateHello] partial class Sample یک عضو Hello دریافت کند؛ کلاس غیرpartial diagnostic IL001 بدهد. خروجی از syntax/semantic معتبر و deterministic بیاید و محدودیت نمونه به کلاس global غیرجنریک صریح باشد. تست driver هر دو ورودی را بررسی کند.")

reference(110, '''
using System.Runtime.InteropServices;
var buffer = new NativeBuffer(4);
Console.WriteLine(buffer.FillAndSum());
buffer.Dispose(); buffer.Dispose();
Console.WriteLine(NativeBuffer.Released);
if (OperatingSystem.IsWindows() && Native.GetCurrentProcessId() == 0) throw new Exception("native call");
unsafe sealed class NativeBuffer : SafeHandle
{
    readonly int length;
    public static int Released;
    public NativeBuffer(int length) : base(IntPtr.Zero, ownsHandle: true)
    {
        if (length <= 0 || length > 1024) throw new ArgumentOutOfRangeException(nameof(length));
        this.length = length;
        SetHandle((IntPtr)NativeMemory.Alloc((nuint)length));
        if (IsInvalid) throw new OutOfMemoryException();
    }
    public override bool IsInvalid => handle == IntPtr.Zero;
    public int FillAndSum()
    {
        bool added = false;
        try { DangerousAddRef(ref added); var bytes = new Span<byte>((void*)handle, length); bytes.Fill(2); return bytes[0] + bytes[^1]; }
        finally { if (added) DangerousRelease(); }
    }
    protected override bool ReleaseHandle() { NativeMemory.Free((void*)handle); Released++; return true; }
}
static class Native
{
    [DllImport("kernel32.dll", ExactSpelling = true)]
    internal static extern uint GetCurrentProcessId();
}
''', "خروجی 4 و 1 باشد؛ حافظه با SafeHandle مالکیت داشته و Dispose تکراری دوباره آزاد نکند. pointer در محدودهٔ DangerousAddRef بماند. P/Invoke ویندوز فقط روی Windows اجرا شود و روی پلتفرم دیگر صریح اجرا نشود؛ allocator و آزادساز متناظر باشند.")

reference(119, '''
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
var builder = Host.CreateApplicationBuilder();
builder.Logging.ClearProviders();
builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true }));
builder.Services.AddSingleton(Channel.CreateBounded<int>(2));
builder.Services.AddSingleton<WorkStatus>();
builder.Services.AddScoped<Processor>();
builder.Services.AddHostedService<Worker>();
using var host = builder.Build();
await host.StartAsync();
var queue = host.Services.GetRequiredService<Channel<int>>();
await queue.Writer.WriteAsync(2); await queue.Writer.WriteAsync(3); queue.Writer.Complete();
var status = host.Services.GetRequiredService<WorkStatus>();
await status.Done.Task.WaitAsync(TimeSpan.FromSeconds(5));
await host.StopAsync();
Console.WriteLine($"{status.Total},{status.Closed}");
sealed class WorkStatus
{
    public int Total;
    public int Closed;
    public TaskCompletionSource Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
sealed class Processor(WorkStatus status) : IAsyncDisposable
{
    public void Process(int value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Interlocked.Add(ref status.Total, value); }
    public ValueTask DisposeAsync() { Interlocked.Increment(ref status.Closed); return ValueTask.CompletedTask; }
}
sealed class Worker(Channel<int> queue, IServiceScopeFactory scopes, WorkStatus status) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (int value in queue.Reader.ReadAllAsync(stoppingToken))
            {
                await using var scope = scopes.CreateAsyncScope();
                scope.ServiceProvider.GetRequiredService<Processor>().Process(value);
            }
            status.Done.TrySetResult();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { status.Done.TrySetCanceled(stoppingToken); }
        catch (Exception error) { status.Done.TrySetException(error); throw; }
    }
}
''', "5,2 چاپ شود؛ هر پیام scope مستقل و async cleanup داشته باشد. singleton worker فقط IServiceScopeFactory را نگه دارد. پایان کار await و shutdown مشاهده شود؛ لغو و fault با gate تست شوند و در ساخت provider گزینهٔ ValidateScopes فعال باشد.")

reference(121, '''
var source = new Source(); var first = new Counter();
var subscription = source.Subscribe(first); source.Emit(1); subscription.Dispose(); source.Emit(2);
source.Complete(); var late = new Counter(); using var afterEnd = source.Subscribe(late); source.Emit(3);
Console.WriteLine($"{first.Count},{late.Completed}");
var failed = new Source(); var observer = new Counter(); using var connection = failed.Subscribe(observer);
failed.Fail(new InvalidOperationException()); failed.Emit(4); failed.Complete();
Console.WriteLine($"{observer.Count},{observer.Errors}");
sealed class Counter : IObserver<int>
{
    public int Count; public int Completed; public int Errors;
    public void OnNext(int value) => Count++;
    public void OnCompleted() => Completed++;
    public void OnError(Exception error) => Errors++;
}
// shortcut: this source is single-threaded, add a synchronization policy before concurrent publishers.
sealed class Source : IObservable<int>
{
    readonly List<IObserver<int>> observers = [];
    bool stopped; Exception? error;
    public IDisposable Subscribe(IObserver<int> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (stopped) { if (error is null) observer.OnCompleted(); else observer.OnError(error); return new Subscription(() => { }); }
        observers.Add(observer); return new Subscription(() => observers.Remove(observer));
    }
    public void Emit(int value) { if (!stopped) foreach (var observer in observers.ToArray()) observer.OnNext(value); }
    public void Complete() => End(null);
    public void Fail(Exception failure) { ArgumentNullException.ThrowIfNull(failure); End(failure); }
    void End(Exception? failure)
    {
        if (stopped) return; stopped = true; error = failure;
        var snapshot = observers.ToArray(); observers.Clear();
        foreach (var observer in snapshot) { if (failure is null) observer.OnCompleted(); else observer.OnError(failure); }
    }
}
sealed class Subscription(Action remove) : IDisposable
{
    bool disposed;
    public void Dispose() { if (disposed) return; disposed = true; remove(); }
}
''', "1,1 و 0,1 چاپ شود. unsubscribe و Dispose تکراری امن باشند؛ پس از terminal event هیچ OnNext تازه ارسال نشود و مشترک دیررس همان پایان یا خطا را ببیند. نمونه تک‌thread است و backpressure عمومی را ادعا نمی‌کند.")

import csharp_content.projects
