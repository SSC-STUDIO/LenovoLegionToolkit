#Requires -Version 5.1
<#
.SYNOPSIS
  Runs the same fail-fast test layers as Ci-tests.yml (Windows).

.DESCRIPTION
  Order: Security|Guard -> Plugin -> Unit (exclude Coverage) -> Smoke.
  Does not run the full suite or collect coverage; use `dotnet test` for that.

.PARAMETER Configuration
  Build configuration (default Release).

.PARAMETER NoBuild
  Skip rebuild (use after a successful solution build).
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [switch] $NoBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $repoRoot

$tfm = 'net10.0-windows10.0.26100.0'
$project = 'UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj'
$common = @('--framework', $tfm, '--configuration', $Configuration)
if ($NoBuild) { $common += '--no-build' }

function Invoke-Layer {
    param(
        [string] $Name,
        [string] $Filter,
        [string] $Trx
    )
    Write-Host "==> $Name" -ForegroundColor Cyan
    & dotnet test $project @common --filter $Filter --logger "trx;LogFileName=$Trx"
    if ($LASTEXITCODE -ne 0) {
        throw "Fail-fast layer failed: $Name (exit $LASTEXITCODE)"
    }
}

if (-not $NoBuild) {
    Write-Host '==> Build solution (serial)' -ForegroundColor Cyan
    $env:MSBUILDDISABLENODEREUSE = '1'
    & dotnet build UniversalDeviceToolkit.sln --configuration $Configuration -m:1 --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)" }
    $common = @('--framework', $tfm, '--configuration', $Configuration, '--no-build')
}

Invoke-Layer -Name 'Security + Guard' -Filter 'Category=Security|Category=Guard' -Trx 'UniversalDeviceToolkit.Tests.SecurityGuard.trx'
Invoke-Layer -Name 'Plugin' -Filter 'Category=Plugin' -Trx 'UniversalDeviceToolkit.Tests.Plugin.trx'
Invoke-Layer -Name 'Unit (exclude Coverage)' -Filter 'Category=Unit&Category!=Coverage' -Trx 'UniversalDeviceToolkit.Tests.Unit.trx'
Invoke-Layer -Name 'Smoke' -Filter 'Category=Smoke' -Trx 'UniversalDeviceToolkit.Tests.Smoke.trx'

Write-Host 'All fail-fast layers passed.' -ForegroundColor Green
