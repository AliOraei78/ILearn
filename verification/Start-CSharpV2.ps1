param([int]$Port = 5086, [int]$SmtpPort = 2531, [int]$ModelPort = 11436)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskSettings = @{}
Get-Content -LiteralPath (Join-Path $taskRoot '.env') | ForEach-Object {
    if ($_ -match '^([A-Z_]+)=(.*)$') { $taskSettings[$matches[1]] = $matches[2].Trim('"') }
}
$env:ConnectionStrings__ILearn = 'Host=127.0.0.1;Port=5441;Database=ilearn_csharp_v2;Username=ilearn;Password=' + $taskSettings['ILEARN_DB_PASSWORD'] + ';GssEncryptionMode=Disable'
$env:Auth__CodeKey = $taskSettings['AUTH_CODE_KEY']
$env:EmailSettings__Host = '127.0.0.1'
$env:EmailSettings__Port = $SmtpPort.ToString()
$env:EmailSettings__EnableSsl = 'false'
$env:EmailSettings__UserName = ''
$env:EmailSettings__Password = ''
$env:EmailSettings__FromEmail = 'qa@example.test'
$env:OpenRouter__Url = "http://127.0.0.1:$ModelPort/api/v1/chat/completions"
$env:OpenRouter__ApiKey = 'csharp-v2-local-fixture'
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:Logging__LogLevel__Default = 'Warning'
$taskPublish = Join-Path $taskRoot 'ILearn/bin/CSharpV2Audit'
& dotnet (Join-Path $taskPublish 'ILearn.dll') --urls "http://127.0.0.1:$Port" --contentRoot $taskPublish
