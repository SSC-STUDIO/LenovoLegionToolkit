#Requires -Version 5.1
<#
.SYNOPSIS
  Runs the same fast test layers as Ci-tests.yml (Windows).

.DESCRIPTION
  Order: Security|Guard -> Fast unit tests.
  The Windows stateful suite is intentionally left to the full CI command.
##>
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
$windowsProject = 'UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj'
$fastProject = 'UniversalDeviceToolkit.Fast.Tests/UniversalDeviceToolkit.Fast.Tests.csproj'
$common = @('--framework', $tfm, '--configuration', $Configuration)
if ($NoBuild) { $common += '--no-build' }

function Invoke-TestLayer {
    param(
        [string] $Name,
        [string] $Project,
        [string[]] $Arguments,
        [string] $Trx
    )

    Write-Host "==> $Name" -ForegroundColor Cyan
    & dotnet test $Project @Arguments --logger "trx;LogFileName=$Trx"
    if ($LASTEXITCODE -ne 0) {
        throw "Test layer failed: $Name (exit $LASTEXITCODE)"
    }
}

if (-not $NoBuild) {
    Write-Host '==> Build solution (serial)' -ForegroundColor Cyan
    $env:MSBUILDDISABLENODEREUSE = '1'
    & dotnet build UniversalDeviceToolkit.sln --configuration $Configuration -m:1 --disable-build-servers
    if ($LASTEXITCODE -ne 0) { throw "Build failed (exit $LASTEXITCODE)" }
    $common = @('--framework', $tfm, '--configuration', $Configuration, '--no-build')
}

Invoke-TestLayer -Name 'Security + Guard' -Project $windowsProject -Arguments ($common + @('--filter', 'Category=Security|Category=Guard')) -Trx 'UniversalDeviceToolkit.Tests.SecurityGuard.trx'
Invoke-TestLayer -Name 'Fast unit tests' -Project $fastProject -Arguments $common -Trx 'UniversalDeviceToolkit.Fast.Tests.trx'

Write-Host 'Fast test layers passed.' -ForegroundColor Green
