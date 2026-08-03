#Requires -Version 5.1
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

if ($env:OS -ne 'Windows_NT') {
    throw 'Windows stateful tests require a Windows runner.'
}

if (-not (Test-Path -LiteralPath (Join-Path $PWD 'UniversalDeviceToolkit.sln'))) {
    throw "Repository root was not found at '$PWD'."
}

$dotnet = Get-Command dotnet -ErrorAction Stop
$sdkList = (& $dotnet.Source --list-sdks | Out-String)
if ($LASTEXITCODE -ne 0 -or $sdkList -notmatch '(?m)^10\.0\.') {
    throw "The .NET 10 SDK is required for Windows tests. Installed SDKs:`n$sdkList"
}

$tempRoot = [System.IO.Path]::GetTempPath()
$probeId = [guid]::NewGuid().ToString('N')
$probeFile = Join-Path $tempRoot "udt-test-preflight-$probeId.tmp"
$registryPath = "HKCU:\Software\UniversalDeviceToolkit\TestPreflight-$probeId"

try {
    [System.IO.File]::WriteAllText($probeFile, 'UniversalDeviceToolkit test preflight')
    if ((Get-Content -LiteralPath $probeFile -Raw) -ne 'UniversalDeviceToolkit test preflight') {
        throw "Temporary file probe did not round-trip at '$probeFile'."
    }

    New-Item -Path $registryPath -Force | Out-Null
    New-ItemProperty -Path $registryPath -Name 'Probe' -Value $probeId -PropertyType String -Force | Out-Null
    if ((Get-ItemPropertyValue -Path $registryPath -Name 'Probe') -ne $probeId) {
        throw 'HKCU registry probe did not round-trip.'
    }

    Write-Host "Windows test preflight passed: .NET 10, temp I/O, and HKCU access are available."
}
finally {
    $cleanupFailures = @()

    if (Test-Path -LiteralPath $probeFile) {
        try { Remove-Item -LiteralPath $probeFile -Force -ErrorAction Stop }
        catch { $cleanupFailures += "Could not remove '$probeFile': $($_.Exception.Message)" }
    }

    if (Test-Path -LiteralPath $registryPath) {
        try { Remove-Item -LiteralPath $registryPath -Recurse -Force -ErrorAction Stop }
        catch { $cleanupFailures += "Could not remove '$registryPath': $($_.Exception.Message)" }
    }

    if ($cleanupFailures.Count -gt 0) {
        throw ($cleanupFailures -join [Environment]::NewLine)
    }
}
