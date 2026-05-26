#Requires -Version 5.1
<#
.SYNOPSIS
  Start local mock catalog server, run LanguagePackUi.Smoke against it, then stop the server.
#>
param(
    [string[]] $SmokeArgs = @("--local"),
    [int] $Port = 18765
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$serverScript = Join-Path $PSScriptRoot "Start-MockCatalogServer.ps1"
$smokeProject = Join-Path $repoRoot "Tools\LanguagePackUi.Smoke\LanguagePackUi.Smoke.csproj"

$serverJob = Start-Job -ScriptBlock {
    param($script, $port)
    & $script -Port $port
} -ArgumentList $serverScript, $Port

try {
    $catalogUrl = "http://127.0.0.1:$Port/catalog.json"
    $deadline = (Get-Date).AddSeconds(30)
    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-WebRequest -Uri $catalogUrl -UseBasicParsing -TimeoutSec 2 | Out-Null
            Write-Host "[orchestrator] Mock catalog ready: $catalogUrl"
            break
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    dotnet run --project $smokeProject -c Debug -- @SmokeArgs
    exit $LASTEXITCODE
}
finally {
    Stop-Job $serverJob -ErrorAction SilentlyContinue
    Remove-Job $serverJob -Force -ErrorAction SilentlyContinue
}
