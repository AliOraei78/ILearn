param(
    [string]$Sdk = "$env:LOCALAPPDATA\Android\Sdk",
    [switch]$SkipPublish,
    [switch]$Qa
)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$workspace = Split-Path $project -Parent
$build = Join-Path $project 'build'
$publish = Join-Path $workspace 'artifacts\offline'
$output = Join-Path $workspace $(if ($Qa) { 'artifacts\ILearn-offline-qa.apk' } else { 'artifacts\ILearn-offline.apk' })
$tools = Join-Path $Sdk 'build-tools\35.0.0'
$platform = Join-Path $Sdk 'platforms\android-34\android.jar'
$javac = if ($env:JAVA_HOME) { Join-Path $env:JAVA_HOME 'bin\javac.exe' } else { 'javac' }
$keytool = if ($env:JAVA_HOME) { Join-Path $env:JAVA_HOME 'bin\keytool.exe' } else { 'keytool' }
function Invoke-Checked([string]$command, [string[]]$arguments) {
    & $command @arguments
    if ($LASTEXITCODE -ne 0) { throw "$command failed with exit code $LASTEXITCODE" }
}
foreach ($required in @("$tools\aapt2.exe", "$tools\d8.bat", "$tools\apksigner.bat", $platform)) {
    if (!(Test-Path -LiteralPath $required)) { throw "Android SDK tool missing: $required" }
}
if (!$SkipPublish) {
    if (Test-Path -LiteralPath $publish) {
        $resolvedPublish = (Resolve-Path -LiteralPath $publish).Path
        if ($resolvedPublish -ne "$workspace\artifacts\offline") { throw 'Unexpected publish directory.' }
        Remove-Item -LiteralPath $resolvedPublish -Recurse -Force
    }
    Invoke-Checked 'dotnet' @('publish', "$workspace\ILearn.Offline\ILearn.Offline.csproj", '-c', 'Release', '-m:1', '-o', $publish)
}
if (!(Test-Path -LiteralPath "$publish\wwwroot\_framework\blazor.webassembly.js")) { throw 'Publish the offline app first.' }
if (Test-Path -LiteralPath $build) {
    $resolved = (Resolve-Path -LiteralPath $build).Path
    if ($resolved -ne "$project\build") { throw 'Unexpected build directory.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
New-Item -ItemType Directory -Path "$build\classes", "$build\dex", "$build\assets", "$project\keys" -Force | Out-Null
& (Join-Path $workspace 'deployment/Protect-LocalSecrets.ps1')
Copy-Item -Path "$publish\wwwroot\*" -Destination "$build\assets" -Recurse
Get-ChildItem "$build\assets" -Recurse -File | Where-Object { $_.Extension -in '.br', '.gz' } | Remove-Item -Force
$sources = @(Get-ChildItem "$project\src\main\java" -Recurse -Filter '*.java' | ForEach-Object FullName)
Invoke-Checked $javac (@('--release', '8', '-encoding', 'UTF-8', '-classpath', $platform, '-d', "$build\classes") + $sources)
$classes = @(Get-ChildItem "$build\classes" -Recurse -Filter '*.class' | ForEach-Object FullName)
Invoke-Checked "$tools\d8.bat" (@('--release', '--min-api', '28', '--lib', $platform, '--output', "$build\dex") + $classes)
Invoke-Checked "$tools\aapt2.exe" @('compile', '--dir', "$project\src\main\res", '-o', "$build\resources.zip")
$linkArguments = @('link', '-o', "$build\unsigned.apk", '--manifest', "$project\src\main\AndroidManifest.xml", '-I', $platform, '--min-sdk-version', '28', '--target-sdk-version', '34', '--version-code', '3', '--version-name', '1.0.2', '-A', "$build\assets", "$build\resources.zip")
if ($Qa) { $linkArguments += '--debug-mode' }
Invoke-Checked "$tools\aapt2.exe" $linkArguments
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::Open("$build\unsigned.apk", [IO.Compression.ZipArchiveMode]::Update)
try {
    # Windows aapt2 emits backslashes for nested assets; Android AssetManager needs forward slashes.
    foreach ($entry in @($archive.Entries | Where-Object { $_.FullName.Contains('\') })) {
        $name = $entry.FullName.Replace('\', '/')
        $buffer = [IO.MemoryStream]::new()
        $source = $entry.Open()
        try { $source.CopyTo($buffer) } finally { $source.Dispose() }
        $entry.Delete()
        $replacement = $archive.CreateEntry($name, [IO.Compression.CompressionLevel]::Optimal).Open()
        try { $buffer.Position = 0; $buffer.CopyTo($replacement) } finally { $replacement.Dispose(); $buffer.Dispose() }
    }
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, "$build\dex\classes.dex", 'classes.dex', [IO.Compression.CompressionLevel]::Optimal) | Out-Null
}
finally { $archive.Dispose() }
Invoke-Checked "$tools\zipalign.exe" @('-f', '4', "$build\unsigned.apk", "$build\aligned.apk")
$keystore = "$project\keys\ilearn-release.p12"
$passwordFile = "$project\keys\password.txt"
if (!(Test-Path -LiteralPath $keystore)) {
    $password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    [IO.File]::WriteAllText($passwordFile, $password)
    Invoke-Checked $keytool @('-genkeypair', '-keystore', $keystore, '-storetype', 'PKCS12', '-storepass', $password, '-keypass', $password, '-alias', 'ilearn', '-keyalg', 'RSA', '-keysize', '3072', '-validity', '10000', '-dname', 'CN=ILearn Offline')
}
Invoke-Checked "$tools\apksigner.bat" @('sign', '--ks', $keystore, '--ks-pass', "file:$passwordFile", '--ks-key-alias', 'ilearn', '--out', $output, "$build\aligned.apk")
Invoke-Checked "$tools\apksigner.bat" @('verify', '--verbose', $output)
Write-Output "APK: $output"
Get-FileHash -LiteralPath $output -Algorithm SHA256
