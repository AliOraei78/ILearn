"""Copyable project files for workshops that need more than one C# file."""
from author_csharp_v2 import UNITS

CONSOLE = '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
'''
TEST_PROJECT = '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>
</Project>
'''

UNITS[31]["files"] = {
    "Wallet.Tests.csproj": TEST_PROJECT,
    "WalletTests.cs": '''using Xunit;

public class WalletTests
{
    [Theory]
    [InlineData(0, false, 5)]
    [InlineData(-1, false, 5)]
    [InlineData(5, true, 0)]
    [InlineData(6, false, 5)]
    public void WithdrawalPreservesTheContract(int amount, bool expected, int remaining)
    {
        var wallet = new Wallet(5);
        bool actual = wallet.TryWithdraw(amount);
        Assert.Equal(expected, actual);
        Assert.Equal(remaining, wallet.Balance);
    }
}

public sealed class Wallet(int balance)
{
    public int Balance { get; private set; } = balance >= 0 ? balance : throw new ArgumentOutOfRangeException(nameof(balance));

    public bool TryWithdraw(int amount)
    {
        if (amount <= 0 || amount > Balance) return false;
        Balance -= amount;
        return true;
    }
}
''',
    "README.md": "در پوشهٔ تازه این دو فایل را بساز و dotnet test را اجرا کن. سپس شرط amount > Balance را به amount >= Balance تغییر بده؛ دادهٔ InlineData با amount=5 باید شکست بخورد. تغییر را برگردان و دوباره تست کن."
}

for number in (74, 119):
    UNITS[number]["files"] = {
        "Workshop.csproj": CONSOLE.replace("</Project>", '  <ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App" /></ItemGroup>\n</Project>'),
        "Program.cs": UNITS[number]["reference"],
        "README.md": "این فایل‌ها یک پروژهٔ مستقل .NET 10 هستند. dotnet run را اجرا کن؛ FrameworkReference کتابخانه‌های Extensions را بدون افزودن بسته به برنامهٔ ILearn در اختیار تمرین می‌گذارد. برای خروجی قابل تکرار dotnet run -c Release را هم اجرا کن."
    }

for number in (109, 110):
    UNITS[number]["files"] = {
        "UnsafeWorkshop.csproj": CONSOLE.replace("<Nullable>enable</Nullable>", "<Nullable>enable</Nullable>\n    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>"),
        "Program.cs": UNITS[number]["reference"],
        "README.md": "dotnet run -c Release را در پوشهٔ تازه اجرا کن. AllowUnsafeBlocks در فایل پروژه صریح است. این کد فقط buffer محدود خود نمونه را پردازش می‌کند. فراخوانی kernel32 در نمونهٔ interop فقط روی Windows اجرا می‌شود؛ حافظهٔ NativeMemory با آزادساز متناظر و SafeHandle مالکیت دارد."
    }

UNITS[77]["files"] = {
    "global.json": '{"sdk":{"version":"10.0.300","rollForward":"latestPatch","allowPrerelease":false}}',
    "Directory.Build.props": '''<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
''',
    "Library/Library.csproj": '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>',
    "Library/Calculator.cs": 'namespace Demo;\npublic static class Calculator { public static int Add(int a, int b) => checked(a + b); }',
    "Consumer/Consumer.csproj": CONSOLE.replace("</Project>", '  <ItemGroup><ProjectReference Include="../Library/Library.csproj" /></ItemGroup>\n</Project>'),
    "Consumer/Program.cs": 'Console.WriteLine(Demo.Calculator.Add(2, 3));',
    "README.md": "SDK این نمونه 10.0.300 است؛ global.json باید با SDK تأییدشدهٔ محیط خودت هماهنگ باشد. نخست dotnet restore Consumer/Consumer.csproj را اجرا و packages.lock.json هر پروژه را ثبت کن. سپس dotnet restore Consumer/Consumer.csproj --locked-mode و dotnet run --project Consumer --no-restore را اجرا کن. خروجی 5 است. پس از تغییر سازگار و ناسازگار API، مصرف‌کننده را دوباره build کن؛ source و binary compatibility را جدا توضیح بده."
}

UNITS[107]["files"] = {
    "Generator/Generator.csproj": '''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup>
    <Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath><Private>false</Private></Reference>
    <Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath><Private>false</Private></Reference>
  </ItemGroup>
</Project>
''',
    "Generator/HelloGenerator.cs": UNITS[107]["reference"],
    "Consumer/Consumer.csproj": CONSOLE.replace("</Project>", '  <ItemGroup><ProjectReference Include="../Generator/Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" /></ItemGroup>\n</Project>'),
    "Consumer/Program.cs": 'Console.WriteLine(Sample.Hello);\n[GenerateHello] partial class Sample { }',
    "README.md": "نمونهٔ آموزشی عمداً از Roslyn همان SDK استفاده می‌کند و به .NET 10 محدود است؛ بسته‌بندی generator عمومی نیاز به target و dependency سازگار با میزبان دارد. dotnet run --project Consumer باید Hello چاپ کند. partial را از Sample حذف و build کن؛ diagnostic IL001 باید ظاهر شود. سپس اصلاح کن. کد تولیدشده را با EmitCompilerGeneratedFiles مشاهده کن."
}

UNITS[112]["files"] = {
    "Publish.csproj": CONSOLE,
    "Program.cs": UNITS[112]["reference"],
    "README.md": "ابتدا dotnet run -c Release را اجرا کن. برای JIT وابسته به runtime: dotnet publish -c Release --self-contained false -o out/jit. برای self-contained، RID متناسب مثل win-x64 را با -r انتخاب کن. برای Native AOT: dotnet publish -c Release -r win-x64 -p:PublishAot=true -o out/aot؛ toolchain بومی و workload مناسب سیستم لازم است. هر خروجی را جدا اجرا کن؛ موفقیت JIT تأیید AOT نیست. این مثال JSON از metadata تولیدشدهٔ SampleContext استفاده می‌کند."
}
UNITS[112]["example"] = "// JIT build: dotnet run -c Release\n" + UNITS[112]["example"]

WORKER_USINGS = '''using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
'''
WORKER_TYPES = '''
public interface ITransform { int Apply(int value); }
public sealed class AboveTwo : ITransform { public int Apply(int value) => value > 2 ? value : 0; }
public sealed class DoubleValues : ITransform { public int Apply(int value) => checked(value * 2); }
public sealed class AlwaysFail : ITransform { public int Apply(int value) => throw new InvalidOperationException("injected"); }
public sealed class DataWorker(ITransform transform)
{
    public async Task<int> RunAsync(string input, string output, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
        var queue = Channel.CreateBounded<(int Index, int Value)>(2);
        var results = new ConcurrentDictionary<int, int>();
        async Task Produce()
        {
            try
            {
                await using var stream = File.OpenRead(input);
                if (stream.Length > 10000) throw new InvalidDataException("Input too large");
                int index = 0;
                await foreach (int value in JsonSerializer.DeserializeAsyncEnumerable<int>(stream, cancellationToken: stop.Token))
                {
                    if (value < 0 || index >= 1000) throw new InvalidDataException("Invalid item");
                    await queue.Writer.WriteAsync((index++, value), stop.Token);
                }
                queue.Writer.TryComplete();
            }
            catch (Exception error) { queue.Writer.TryComplete(error); stop.Cancel(); throw; }
        }
        async Task Consume()
        {
            try
            {
                await foreach (var item in queue.Reader.ReadAllAsync(stop.Token))
                {
                    int value = transform.Apply(item.Value);
                    if (!results.TryAdd(item.Index, value)) throw new InvalidDataException("Duplicate work");
                }
            }
            catch { stop.Cancel(); throw; }
        }
        Task producer = Produce();
        Task first = Consume(), second = Consume();
        try { await Task.WhenAll(producer, first, second); }
        finally { stop.Cancel(); }
        int[] values = results.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToArray();
        string temporary = output + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(values), token);
            token.ThrowIfCancellationRequested();
            File.Move(temporary, output, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return values.Sum();
    }
}
'''

for number, policy, expected in ((94, "DoubleValues", 18), (123, "AboveTwo", 7)):
    main = '''
string directory = Path.Combine(Path.GetTempPath(), "ilearn-worker-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    string input = Path.Combine(directory, "input.json"), output = Path.Combine(directory, "output.json");
    await File.WriteAllTextAsync(input, "[2,3,4]");
    int sum = await new DataWorker(new POLICY()).RunAsync(input, output, CancellationToken.None);
    if (sum != EXPECTED) throw new Exception("Result");
    Console.WriteLine(sum);
    string previous = await File.ReadAllTextAsync(output);
    try { await new DataWorker(new AlwaysFail()).RunAsync(input, output, CancellationToken.None); throw new Exception("Expected failure"); }
    catch (InvalidOperationException) { }
    if (await File.ReadAllTextAsync(output) != previous) throw new Exception("Previous output lost");
    Console.WriteLine("preserved");
    using var stop = new CancellationTokenSource(); stop.Cancel();
    try { await new DataWorker(new POLICY()).RunAsync(input, output, stop.Token); throw new Exception("Expected cancellation"); }
    catch (OperationCanceledException) { Console.WriteLine("cancelled"); }
}
finally { Directory.Delete(directory, true); }
'''.replace("POLICY", policy).replace("EXPECTED", str(expected))
    UNITS[number]["reference"] = WORKER_USINGS + main + WORKER_TYPES
    UNITS[number]["rubric"] = f"خروجی {expected}،preserved،cancelled باشد. فایل JSON واقعی با سقف طول و تعداد،صف bounded و دو consumer پردازش شود. شکست هر سمت token مشترک را لغو و همهٔ taskها awaited شوند. خروجی فقط پس از موفقیت کامل با فایل موقت جایگزین شود؛ خطا و لغو دادهٔ قبلی را تغییر ندهند. تست malformed JSON،خالی،منفی،محدودیت و توقف میانهٔ کار هم اجرا شود."
    UNITS[number]["files"] = {
        "Worker/Worker.csproj": CONSOLE,
        "Worker/DataWorker.cs": WORKER_USINGS + WORKER_TYPES,
        "Worker/Program.cs": f'''if (args.Length != 2)
{{
    Console.Error.WriteLine("Usage: Worker input.json output.json");
    return 2;
}}
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {{ e.Cancel = true; stop.Cancel(); }};
try
{{
    int total = await new DataWorker(new {policy}()).RunAsync(args[0], args[1], stop.Token);
    Console.WriteLine(total);
    return 0;
}}
catch (OperationCanceledException) {{ Console.Error.WriteLine("Cancelled"); return 130; }}
catch (Exception error) when (error is IOException or System.Text.Json.JsonException or InvalidDataException or InvalidOperationException or OverflowException)
{{ Console.Error.WriteLine("Processing failed: " + error.GetType().Name); return 1; }}
''',
        "input.json": "[2,3,4]",
        "README.md": f"dotnet run --project Worker -- input.json output.json باید {expected} چاپ کند و آرایهٔ خروجی مرتب به ترتیب ورودی باشد. Ctrl+C درخواست لغو می‌دهد. خروجی شکست کد غیرصفر دارد؛ فایل قبلی در شکست پردازش حفظ می‌شود. تمام فایل‌ها را در پوشهٔ تازه بساز و از فایل اصلی شخصی برای تست استفاده نکن. معیار benchmark و trace با محیط واقعی اجرا و گزارش شود؛ هیچ عدد سرعتی از قبل تضمین نشده است."
    }

UNITS[123]["files"].update({
    "Tests/Tests.csproj": TEST_PROJECT.replace("</Project>", '  <ItemGroup><ProjectReference Include="../Worker/Worker.csproj" /></ItemGroup>\n</Project>'),
    "Tests/WorkerTests.cs": '''using Xunit;

public class WorkerTests
{
    [Theory]
    [InlineData("[]", 0)]
    [InlineData("[2,3,4]", 7)]
    public async Task ValidDataProducesTheExpectedReport(string json, int expected)
    {
        using var files = new TestFiles();
        await File.WriteAllTextAsync(files.Input, json);
        Assert.Equal(expected, await new DataWorker(new AboveTwo()).RunAsync(files.Input, files.Output, CancellationToken.None));
    }

    [Theory]
    [InlineData("broken")]
    [InlineData("[-1]")]
    public async Task BadInputPreservesPreviousOutput(string json)
    {
        using var files = new TestFiles();
        await File.WriteAllTextAsync(files.Input, json);
        await File.WriteAllTextAsync(files.Output, "previous");
        await Assert.ThrowsAnyAsync<Exception>(() => new DataWorker(new AboveTwo()).RunAsync(files.Input, files.Output, CancellationToken.None));
        Assert.Equal("previous", await File.ReadAllTextAsync(files.Output));
    }

    [Fact]
    public async Task CancellationAndWorkerFailureAreObserved()
    {
        using var files = new TestFiles();
        await File.WriteAllTextAsync(files.Input, "[2,3,4]");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DataWorker(new AboveTwo()).RunAsync(files.Input, files.Output, stop.Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new DataWorker(new AlwaysFail()).RunAsync(files.Input, files.Output, CancellationToken.None));
    }
}

sealed class TestFiles : IDisposable
{
    readonly string directory = Path.Combine(Path.GetTempPath(), "ilearn-contract-" + Guid.NewGuid().ToString("N"));
    public string Input => Path.Combine(directory, "input.json");
    public string Output => Path.Combine(directory, "output.json");
    public TestFiles() => Directory.CreateDirectory(directory);
    public void Dispose() => Directory.Delete(directory, true);
}
'''
})

UNITS[122]["files"] = {
    "Library/Library.csproj": '<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <PackageId>ILearn.Sample</PackageId>\n    <Version>1.0.0</Version>\n  </PropertyGroup>\n</Project>\n',
    "Library/Smoke.cs": '''namespace Demo;

public static class Smoke
{
    public static int Add(int first, int second) => checked(first + second);
}
''',
    "Runner/Runner.csproj": CONSOLE.replace("</Project>", '  <ItemGroup><ProjectReference Include="../Library/Library.csproj" /></ItemGroup>\n</Project>'),
    "Runner/Program.cs": 'Console.WriteLine(Demo.Smoke.Add(2, 3));\n',
    "Tests/Tests.csproj": TEST_PROJECT.replace("</Project>", '  <ItemGroup><ProjectReference Include="../Library/Library.csproj" /></ItemGroup>\n</Project>'),
    "Tests/SmokeTests.cs": '''using Xunit;

public class SmokeTests
{
    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-2, 2, 0)]
    public void AddsWithThePublicContract(int first, int second, int expected)
    {
        Assert.Equal(expected, Demo.Smoke.Add(first, second));
    }
}
''',
    "Directory.Build.props": '<Project><PropertyGroup><RestorePackagesWithLockFile>true</RestorePackagesWithLockFile></PropertyGroup></Project>',
    "verify.ps1": '''$ErrorActionPreference = 'Stop'
function Check-Exit { if ($LASTEXITCODE -ne 0) { throw "Command failed: $LASTEXITCODE" } }
dotnet restore Runner/Runner.csproj --locked-mode
Check-Exit
dotnet restore Tests/Tests.csproj --locked-mode
Check-Exit
dotnet build Runner/Runner.csproj -c Release --no-restore
Check-Exit
dotnet test Tests/Tests.csproj -c Release --no-restore
Check-Exit
dotnet format Runner/Runner.csproj --verify-no-changes --no-restore
Check-Exit
dotnet pack Library/Library.csproj -c Release --no-build -o artifacts/packages
Check-Exit
dotnet publish Runner/Runner.csproj -c Release --no-build -o artifacts/app
Check-Exit
$result = dotnet artifacts/app/Runner.dll
Check-Exit
if ($result -ne '5') { throw 'Published artifact failed its smoke test' }
''',
    ".github/workflows/ci.yml": '''name: Verify
on: [push, pull_request]
permissions:
  contents: read
jobs:
  verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7
      - uses: actions/setup-dotnet@v6
        with:
          dotnet-version: '10.0.300'
      - run: pwsh -File verify.ps1
''',
    "README.md": "pwsh -File verify.ps1 گیت‌های واقعی restore locked/build/test/format/pack/publish و اجرای artifact را انجام می‌دهد. فایل‌های packages.lock.json همراه نمونه‌اند؛ تغییر گراف وابستگی باید آگاهانه بازبینی و ثبت شود. بسته‌ها را برای انتشار عمومی فقط پس از بازبینی version و release note ارسال کن. workflow نمونه مطابق مستندات actions/checkout و actions/setup-dotnet نوشته شده؛ اجرای محلی شاهد اجرا در GitHub نیست."
}

from pathlib import Path
for number in (77, 122):
    lock_root = Path(__file__).resolve().parents[2] / "ILearn/Content/WorkshopLocks" / f"R{number:03}"
    if lock_root.exists():
        for lock in sorted(lock_root.rglob("packages.lock.json")):
            UNITS[number]["files"][lock.relative_to(lock_root).as_posix()] = lock.read_text(encoding="utf-8")

UNITS[74]["workshop"] = "برای عملیات سفارش از ILogger با structured template،شناسهٔ سفارش و exception استفاده کن. provider تست fieldها را ثبت کند و ثابت کن password و token وارد آن‌ها نشده‌اند. پروژهٔ مستقل و provider مرجع در پاسخ کامل موجود است."
UNITS[79]["workshop"] = "دفتر کار JSON نسخه‌دار با ذخیره،بازگشایی و رد فایل خراب بساز. خطای پیش از commit را تزریق کن و ثابت کن فایل سالم قبلی و وضعیت در حافظه حفظ می‌شوند. اندازه،نسخه و invariant داده را پیش از commit بررسی کن."
UNITS[103]["workshop"] = "دو parser هم‌رفتار را در Release اندازه‌گیری کن. SDK،داده،warm-up،زمان و allocation را ثبت کن. آزمایش سادهٔ مرجع عددهای واقعی همان اجرا را چاپ می‌کند؛ برای نتیجهٔ آماری از BenchmarkDotNet استفاده کن و تفاوت در محدودهٔ نویز را بهبود قطعی ننام."
UNITS[123]["workshop"] = "ابزار پردازش JSON فایل‌محور با لغو،صف bounded و دو consumer بساز. سیاست پردازش از I/O جدا باشد؛ ورودی خراب،منفی،بزرگ،توقف و خطای worker را تست کن. خروجی قبلی در شکست حفظ شود. فایل‌های پروژه و تست پاسخ مرجع را اجرا کن و README اجرا،گزارش واقعی benchmark و دلیل معماری خودت را تحویل بده."
