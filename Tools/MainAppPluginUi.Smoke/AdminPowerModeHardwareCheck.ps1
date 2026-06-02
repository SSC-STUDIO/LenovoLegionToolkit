param(
    [string]$ResultPathOverride,
    [string]$LogPathOverride,
    [int]$TimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
$repoRoot = 'D:\EliuaK_Csy\Working-Paper\My-Program\UniversalDeviceToolkit'

function Resolve-AbsolutePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Write-Result {
    param(
        [string]$Label,
        [string]$Value
    )

    Add-Content -LiteralPath $resultPath -Value ("{0}: {1}" -f $Label, $Value)
}

function Get-ResultValue {
    param(
        [string]$FilePath,
        [string]$Key
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        return $null
    }

    $line = Select-String -LiteralPath $FilePath -Pattern ("^{0}: " -f [Regex]::Escape($Key)) | Select-Object -Last 1
    if (-not $line) {
        return $null
    }

    return ($line.Line -replace ("^{0}: " -f [Regex]::Escape($Key)), '').Trim()
}

function Wait-ForFile {
    param(
        [string]$Path,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $Path) {
            return $true
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

$resultPath = if ($ResultPathOverride) { Resolve-AbsolutePath $ResultPathOverride } elseif ($env:UDT_SMOKE_RESULT_PATH) { Resolve-AbsolutePath $env:UDT_SMOKE_RESULT_PATH } else { Join-Path $repoRoot 'Tools\MainAppPluginUi.Smoke\AdminPowerModeHardwareCheck.result.txt' }
$logPath = if ($LogPathOverride) { Resolve-AbsolutePath $LogPathOverride } elseif ($env:UDT_SMOKE_LOG_PATH) { Resolve-AbsolutePath $env:UDT_SMOKE_LOG_PATH } else { Join-Path $repoRoot 'Tools\MainAppPluginUi.Smoke\AdminPowerModeHardwareCheck.smoke.txt' }
$hardwareValidationResultPath = [System.IO.Path]::ChangeExtension($resultPath, '.hardware.result.txt')
$hardwareValidationLogPath = [System.IO.Path]::ChangeExtension($logPath, '.hardware.log.txt')
$hardwareValidationScriptPath = Join-Path $repoRoot 'Tools\HardwareValidation\Run-HardwareValidationElevated.ps1'

[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($resultPath)) | Out-Null
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($logPath)) | Out-Null

foreach ($path in @($resultPath, $logPath, $hardwareValidationResultPath, $hardwareValidationLogPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

Write-Result 'StartedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
Write-Result 'DelegatedTo' $hardwareValidationScriptPath
Write-Result 'Scenario' 'CpuVerify'
Write-Result 'TimeoutSeconds' $TimeoutSeconds

Push-Location $repoRoot
try {
    $delegatedArguments = @(
        '-ExecutionPolicy', 'Bypass',
        '-File', $hardwareValidationScriptPath,
        '-Scenario', 'CpuVerify',
        '-ResultPath', $hardwareValidationResultPath,
        '-LogPath', $hardwareValidationLogPath,
        '-TimeoutSeconds', $TimeoutSeconds
    )

    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $delegatedArguments -PassThru -WindowStyle Hidden
    Write-Result 'DelegatedProcessId' $process.Id

    if (-not $process.WaitForExit(($TimeoutSeconds + 30) * 1000)) {
        Write-Result 'DelegatedTimedOut' 'True'
        try {
            Stop-Process -Id $process.Id -Force
        }
        catch {
            Write-Result 'DelegatedKillError' $_.Exception.Message
        }
    }

    $delegatedExitCode = if ($process.HasExited) { [string]$process.ExitCode } else { '<running-killed>' }
    Write-Result 'DelegatedExitCode' $delegatedExitCode

    $delegatedResultReady = Wait-ForFile -Path $hardwareValidationResultPath -TimeoutSeconds ($TimeoutSeconds + 30)
    $delegatedLogReady = Wait-ForFile -Path $hardwareValidationLogPath -TimeoutSeconds 5

    Write-Result 'DelegatedResultReady' $delegatedResultReady
    Write-Result 'DelegatedLogReady' $delegatedLogReady

    if ($delegatedResultReady) {
        $beforeHardwareValue = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BeforeHardwareValue'
        $afterHardwareValue = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'AfterHardwareValue'
        $requestedHardwareDelta = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'RequestedHardwareDelta'
        $hardwareValueDelta = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'HardwareValueDelta'
        $hardwareValueChanged = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'HardwareValueChanged'
        $afterPowerMode = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'AfterSmartFanMode'
        $persistedPassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'PersistedVerificationPassed'
        $hardwarePassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'HardwareVerificationPassed'
        $measuredPassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'MeasuredVerificationPassed'
        $restorePassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'RestoreVerificationPassed'
        $overallPassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'OverallPassed'
        $capability = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'Capability'

        if ($null -ne $capability) {
            Write-Result 'Capability' $capability
        }
        if ($null -ne $beforeHardwareValue) {
            Write-Result 'BeforeCpuLongTerm' $beforeHardwareValue
        }
        if ($null -ne $afterHardwareValue) {
            Write-Result 'AfterCpuLongTerm' $afterHardwareValue
        }
        if ($null -ne $requestedHardwareDelta) {
            Write-Result 'RequestedCpuLongTermDelta' $requestedHardwareDelta
        }
        if ($null -ne $hardwareValueDelta) {
            Write-Result 'MeasuredCpuLongTermDelta' $hardwareValueDelta
        }
        if ($null -ne $hardwareValueChanged) {
            Write-Result 'MeasuredCpuLongTermChanged' $hardwareValueChanged
        }
        if ($null -ne $afterPowerMode) {
            Write-Result 'AfterPowerMode' $afterPowerMode
        }
        if ($null -ne $persistedPassed) {
            Write-Result 'PersistedVerificationPassed' $persistedPassed
        }
        if ($null -ne $hardwarePassed) {
            Write-Result 'HardwareVerificationPassed' $hardwarePassed
        }
        if ($null -ne $measuredPassed) {
            Write-Result 'MeasuredVerificationPassed' $measuredPassed
        }
        if ($null -ne $restorePassed) {
            Write-Result 'RestoreVerificationPassed' $restorePassed
        }
        if ($null -ne $overallPassed) {
            Write-Result 'HardwareValidationPassed' $overallPassed
        }
    }
    else {
        Write-Result 'HardwareValidationPassed' 'False'
        Write-Result 'HardwareValidationResultMissing' 'True'
    }

    if ($delegatedLogReady) {
        Copy-Item -LiteralPath $hardwareValidationLogPath -Destination $logPath -Force
    }
}
catch {
    Write-Result 'Exception' $_.Exception.ToString()
}
finally {
    Pop-Location
}

Write-Result 'FinishedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
