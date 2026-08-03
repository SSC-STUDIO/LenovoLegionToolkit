#Requires -Version 5.1
<#
.SYNOPSIS
  Run the full Universal Device Toolkit UI + backend performance suite.

.DESCRIPTION
  1. Builds WPF (Release|x64) and PerformanceTest
  2. Runs backend micro-benchmarks (UniversalDeviceToolkit.PerformanceTest)
  3. Runs per-surface UI navigation timing (Tools/UiPerformance.Smoke)
  4. Optionally samples runtime counters via dotnet-counters if available
  5. Writes a combined report under _ui_perf_out/

.EXAMPLE
  .\Scripts\Run-UiPerformanceSuite.ps1
  .\Scripts\Run-UiPerformanceSuite.ps1 -Configuration Debug -Iterations 3 -KeepApp
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [ValidateRange(1, 10)]
    [int] $Iterations = 2,

    [switch] $KeepApp,

    [switch] $SkipBuild,

    [switch] $SkipBackend,

    [switch] $SkipCounters,

    [string] $OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
$RepoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $RepoRoot 'UniversalDeviceToolkit.sln'))) {
    # Script lives in UniversalDeviceToolkit/Scripts
    if (Test-Path (Join-Path $PSScriptRoot '..\UniversalDeviceToolkit.sln')) {
        $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
}

Set-Location $RepoRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepoRoot '_ui_perf_out'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runDir = Join-Path $OutputDirectory $timestamp
New-Item -ItemType Directory -Force -Path $runDir | Out-Null

Write-Host "=== UDT UI Performance Suite ===" -ForegroundColor Cyan
Write-Host "Repo:   $RepoRoot"
Write-Host "Out:    $runDir"
Write-Host "Config: $Configuration  Iterations: $Iterations"
Write-Host ""

function Invoke-Checked {
    param([string]$Title, [scriptblock]$Block)
    Write-Host ">>> $Title" -ForegroundColor Yellow
    & $Block
    if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) {
        throw "$Title failed with exit code $LASTEXITCODE"
    }
}

# --- Build ---
if (-not $SkipBuild) {
    Invoke-Checked 'Build WPF (x64)' {
        dotnet build (Join-Path $RepoRoot 'UniversalDeviceToolkit.WPF\UniversalDeviceToolkit.WPF.csproj') `
            -c $Configuration -p:Platform=x64 --nologo -v q
    }
    if (-not $SkipBackend) {
        Invoke-Checked 'Build PerformanceTest' {
            dotnet build (Join-Path $RepoRoot 'UniversalDeviceToolkit.PerformanceTest\UniversalDeviceToolkit.PerformanceTest.csproj') `
                -c $Configuration --nologo -v q
        }
    }
    Invoke-Checked 'Build UiPerformance.Smoke' {
        dotnet build (Join-Path $RepoRoot 'Tools\UiPerformance.Smoke\UiPerformance.Smoke.csproj') `
            -c $Configuration -p:Platform=x64 --nologo -v q
    }
}

# --- Backend microbenches ---
$backendLog = Join-Path $runDir 'backend-performance.txt'
if (-not $SkipBackend) {
    Write-Host ">>> Backend PerformanceTest" -ForegroundColor Yellow
    $backendOut = Join-Path $runDir 'backend'
    $backendReport = Join-Path $runDir 'backend-performance-report.txt'
    New-Item -ItemType Directory -Force -Path $backendOut | Out-Null
    Push-Location $backendOut
    try {
        dotnet run --project (Join-Path $RepoRoot 'UniversalDeviceToolkit.PerformanceTest\UniversalDeviceToolkit.PerformanceTest.csproj') `
            -c $Configuration --no-build -- --output $backendReport 2>&1 | Tee-Object -FilePath $backendLog
    } finally {
        Pop-Location
    }
} else {
    "Skipped" | Set-Content $backendLog
}

# --- UI surface walk ---
Write-Host ">>> UiPerformance.Smoke (all surfaces)" -ForegroundColor Yellow
$uiArgs = @(
    'run', '--project', (Join-Path $RepoRoot 'Tools\UiPerformance.Smoke\UiPerformance.Smoke.csproj'),
    '-c', $Configuration, '-p:Platform=x64', '--no-build', '--',
    '--repo-root', $RepoRoot,
    '--out', (Join-Path $runDir 'ui'),
    '--configuration', $Configuration,
    '--iterations', "$Iterations"
)
if ($KeepApp) { $uiArgs += '--keep-app' }

$uiLog = Join-Path $runDir 'ui-performance.txt'
& dotnet @uiArgs 2>&1 | Tee-Object -FilePath $uiLog
$uiExit = $LASTEXITCODE

# --- Optional runtime counters (requires app still running or re-launch) ---
$countersNote = Join-Path $runDir 'counters-note.txt'
if (-not $SkipCounters) {
    $dotnetCounters = Get-Command dotnet-counters -ErrorAction SilentlyContinue
    if ($dotnetCounters) {
        $proc = Get-Process -Name 'Universal Device Toolkit' -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($proc) {
            Write-Host ">>> dotnet-counters sample PID=$($proc.Id) (20s)" -ForegroundColor Yellow
            $counterOut = Join-Path $runDir 'dotnet-counters.txt'
            $job = Start-Process -FilePath $dotnetCounters.Source `
                -ArgumentList @('monitor', '--process-id', "$($proc.Id)", '--refresh-interval', '1', 'System.Runtime') `
                -NoNewWindow -PassThru -RedirectStandardOutput $counterOut -RedirectStandardError (Join-Path $runDir 'dotnet-counters.err.txt')
            Start-Sleep -Seconds 20
            if (-not $job.HasExited) { Stop-Process -Id $job.Id -Force -ErrorAction SilentlyContinue }
            "Sampled System.Runtime for 20s → $counterOut" | Set-Content $countersNote
        } else {
            @"
dotnet-counters is installed but the app is not running.
Re-run with -KeepApp, then:
  dotnet-counters monitor --process-id <pid> System.Runtime Microsoft.Windows.Desktop.App.WPF
"@ | Set-Content $countersNote
        }
    } else {
        @"
dotnet-counters not found. Install with:
  dotnet tool install -g dotnet-counters

Other tools:
  - Visual Studio → Debug → Performance Profiler (CPU / Allocation / UI)
  - PerfView: PerfView /nogui collect /MaxCollectSec:60
  - Windows Performance Recorder (WPR) + WPA
"@ | Set-Content $countersNote
    }
}

# --- Combined summary ---
$summaryPath = Join-Path $runDir 'SUITE-SUMMARY.md'
$uiReportMd = Join-Path $runDir 'ui\ui-perf-report.md'
$uiReportJson = Join-Path $runDir 'ui\ui-perf-report.json'

$summary = @()
$summary += '# UDT Performance Suite Summary'
$summary += ''
$summary += "- Timestamp: $timestamp"
$summary += "- Configuration: $Configuration"
$summary += "- UI exit code: $uiExit"
$summary += "- Output: ``$runDir``"
$summary += ''
$summary += '## Artifacts'
$summary += ''
$summary += "| Artifact | Path |"
$summary += "|---|---|"
$summary += "| Backend log | ``backend-performance.txt`` |"
$summary += "| UI log | ``ui-performance.txt`` |"
$summary += "| UI report (md) | ``ui/ui-perf-report.md`` |"
$summary += "| UI report (json) | ``ui/ui-perf-report.json`` |"
$summary += "| Counters note | ``counters-note.txt`` |"
$summary += ''
$summary += '## Manual deep-dive tools'
$summary += ''
$summary += '1. **Visual Studio Diagnostic Tools** — attach to Universal Device Toolkit, enable CPU + .NET Allocations + Events'
$summary += '2. **dotnet-trace** — `dotnet-trace collect -p <pid> --providers Microsoft-Windows-DotNETRuntime`'
$summary += '3. **PerfView** — GC / CPU stacks while flipping every sidebar page'
$summary += '4. **WPF Perforator / Visual Studio Live Visual Tree** — layout passes and binding storms'
$summary += '5. **Windows Performance Analyzer** — GPU composition / DWM if frame drops suspected'
$summary += ''

if (Test-Path $uiReportMd) {
    $summary += '## UI report excerpt'
    $summary += ''
    $summary += (Get-Content $uiReportMd -Raw)
}

$summary -join "`n" | Set-Content -Path $summaryPath -Encoding UTF8

Write-Host ""
Write-Host "=== Suite complete ===" -ForegroundColor Green
Write-Host "Summary: $summaryPath"
if (Test-Path $uiReportMd) { Write-Host "UI report: $uiReportMd" }

if ($uiExit -ne 0) { exit $uiExit }
exit 0
