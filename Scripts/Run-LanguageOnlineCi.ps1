#Requires -Version 5.1
<#
.SYNOPSIS
  Phase A CI helper: Online-like WPF build (pruned satellites on a copy) + mock catalog backend install smoke.
#>
param(
    [string] $Configuration = "Release",
    [string] $Platform = "x64",
    [string] $Culture = "de",
    [int] $Port = 18765,
    [switch] $SkipPrune,
    [switch] $SkipBuild
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $dir = $PSScriptRoot
    while ($dir) {
        if (Test-Path (Join-Path $dir "UniversalDeviceToolkit.sln")) { return $dir }
        $dir = Split-Path $dir -Parent
    }
    throw "Repository root not found from $PSScriptRoot"
}

$repoRoot = Get-RepoRoot
Set-Location $repoRoot

$wpfProject = Join-Path $repoRoot "UniversalDeviceToolkit.WPF\UniversalDeviceToolkit.WPF.csproj"
$smokeProject = Join-Path $repoRoot "Tools\LanguagePackUi.Smoke\LanguagePackUi.Smoke.csproj"
$serverScript = Join-Path $repoRoot "Tools\LanguagePackMockBackend\Start-MockCatalogServer.ps1"
$pruneScript = Join-Path $repoRoot "Scripts\Prune-ShippingFootprint.ps1"

if (-not $SkipBuild) {
    Write-Host "[lang-online-ci] Building WPF ($Configuration|$Platform)..."
    $env:MSBUILDDISABLENODEREUSE = "1"
    dotnet build $wpfProject -c $Configuration -p:Platform=$Platform -m:1 --nologo
    if ($LASTEXITCODE -ne 0) { throw "WPF build failed with exit $LASTEXITCODE" }
}

$buildRuntimeDir = Get-ChildItem -Path (Join-Path $repoRoot "UniversalDeviceToolkit.WPF\bin") -Filter "Universal Device Toolkit.exe" -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1 -ExpandProperty DirectoryName

if (-not $buildRuntimeDir) {
    throw "WPF output not found under UniversalDeviceToolkit.WPF\bin"
}

Write-Host "[lang-online-ci] Build runtime: $buildRuntimeDir"

# Work on a disposable Online-like copy so prune never destroys the main build tree.
$onlineStaging = Join-Path $env:TEMP ("udt-online-ci-" + [guid]::NewGuid().ToString("N"))
Write-Host "[lang-online-ci] Staging Online-like payload: $onlineStaging"
New-Item -ItemType Directory -Path $onlineStaging -Force | Out-Null
robocopy $buildRuntimeDir $onlineStaging /E /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit $LASTEXITCODE" }

$runtimeDir = $onlineStaging

if (-not $SkipPrune) {
    Write-Host "[lang-online-ci] Pruning satellites to Online-like set (en + $Culture)..."
    & $pruneScript -PayloadPath $runtimeDir -AllowedCultures "en;$Culture"
}

# Prefer Start-Process over Start-Job: job runspaces hide HttpListener/startup errors on GHA
# and Get-RuntimeDirectory historically missed bin\x64\Release paths.
$serverLog = Join-Path $env:TEMP ("udt-mock-catalog-" + [guid]::NewGuid().ToString("N") + ".log")
$serverErr = "$serverLog.err"
$pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
if ($pwshCmd) { $pwshExe = $pwshCmd.Source }
else { $pwshExe = (Get-Command powershell).Source }

Write-Host "[lang-online-ci] Starting mock catalog server (log: $serverLog)"
$serverProcess = Start-Process -FilePath $pwshExe -ArgumentList @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-File", $serverScript,
    "-Port", "$Port",
    "-Culture", $Culture
) -PassThru -WindowStyle Hidden `
    -RedirectStandardOutput $serverLog `
    -RedirectStandardError $serverErr

try {
    $catalogUrl = "http://127.0.0.1:$Port/catalog.json"
    $deadline = (Get-Date).AddSeconds(90)
    $ready = $false
    while ((Get-Date) -lt $deadline) {
        if ($serverProcess.HasExited) {
            Write-Host "[lang-online-ci] Mock catalog process exited early (code $($serverProcess.ExitCode))"
            break
        }
        try {
            Invoke-WebRequest -Uri $catalogUrl -UseBasicParsing -TimeoutSec 2 | Out-Null
            $ready = $true
            Write-Host "[lang-online-ci] Mock catalog ready: $catalogUrl"
            break
        }
        catch {
            Start-Sleep -Milliseconds 400
        }
    }
    if (-not $ready) {
        Write-Host "[lang-online-ci] --- mock catalog stdout ---"
        if (Test-Path $serverLog) { Get-Content -LiteralPath $serverLog -ErrorAction SilentlyContinue | Out-Host }
        Write-Host "[lang-online-ci] --- mock catalog stderr ---"
        if (Test-Path $serverErr) { Get-Content -LiteralPath $serverErr -ErrorAction SilentlyContinue | Out-Host }
        throw "Mock catalog server did not become ready on port $Port"
    }

    Write-Host "[lang-online-ci] Running LanguagePackUi.Smoke --backend-only --local --culture $Culture"
    & dotnet run --project $smokeProject -c $Configuration -- `
        --backend-only --local --culture $Culture --catalog-url $catalogUrl --app-dir $runtimeDir
    if ($LASTEXITCODE -ne 0) {
        throw "Language pack backend-only smoke failed with exit $LASTEXITCODE"
    }

    Write-Host "[lang-online-ci] PASS: Online-like build + mock catalog install"
    exit 0
}
finally {
    if ($serverProcess -and -not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    }
    try { Remove-Item -LiteralPath $onlineStaging -Recurse -Force -ErrorAction SilentlyContinue } catch { }
    try { Remove-Item -LiteralPath $serverLog, $serverErr -Force -ErrorAction SilentlyContinue } catch { }
}
