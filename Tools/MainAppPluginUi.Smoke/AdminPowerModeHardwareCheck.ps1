param(
    [string]$ResultPathOverride,
    [string]$LogPathOverride,
    [int]$TimeoutSeconds = 180,
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

function Resolve-AbsolutePath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    return $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Path)
}

function Resolve-RepositoryRoot {
    param([string]$Path)

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($Path)) {
        $candidates += $Path
    }

    $candidates += (Join-Path $PSScriptRoot '..\..')
    $candidates += (Get-Location).Path

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        try {
            $resolved = Resolve-AbsolutePath $candidate
        }
        catch {
            continue
        }

        if (Test-Path -LiteralPath (Join-Path $resolved 'UniversalDeviceToolkit.sln')) {
            return $resolved
        }
    }

    throw 'Could not resolve repository root. Pass -RepoRoot pointing at UniversalDeviceToolkit.sln.'
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

$repoRoot = Resolve-RepositoryRoot -Path $RepoRoot
$resultPath = if ($ResultPathOverride) { Resolve-AbsolutePath $ResultPathOverride } elseif ($env:UDT_SMOKE_RESULT_PATH) { Resolve-AbsolutePath $env:UDT_SMOKE_RESULT_PATH } else { Join-Path $repoRoot 'Tools\MainAppPluginUi.Smoke\AdminPowerModeHardwareCheck.result.txt' }
$logPath = if ($LogPathOverride) { Resolve-AbsolutePath $LogPathOverride } elseif ($env:UDT_SMOKE_LOG_PATH) { Resolve-AbsolutePath $env:UDT_SMOKE_LOG_PATH } else { Join-Path $repoRoot 'Tools\MainAppPluginUi.Smoke\AdminPowerModeHardwareCheck.smoke.txt' }
$hardwareValidationResultPath = [System.IO.Path]::ChangeExtension($resultPath, '.hardware.result.txt')
$hardwareValidationLogPath = [System.IO.Path]::ChangeExtension($logPath, '.hardware.log.txt')
$hardwareValidationScriptPath = Join-Path $repoRoot 'Tools\HardwareValidation\Run-HardwareValidationElevated.ps1'
$uiSmokeResultPath = [System.IO.Path]::ChangeExtension($resultPath, '.ui.result.txt')
$uiSmokeLogPath = [System.IO.Path]::ChangeExtension($logPath, '.ui.log.txt')

[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($resultPath)) | Out-Null
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($logPath)) | Out-Null

foreach ($path in @($resultPath, $logPath, $hardwareValidationResultPath, $hardwareValidationLogPath, $uiSmokeResultPath, $uiSmokeLogPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

Write-Result 'StartedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
Write-Result 'RepositoryRoot' $repoRoot
Write-Result 'DelegatedTo' $hardwareValidationScriptPath
Write-Result 'Scenario' 'PowerModeUiAndHardwareVerify'
Write-Result 'UiSmokeScenario' 'power-mode'
Write-Result 'UiSmokePowerModeHardwareVerify' 'True'
Write-Result 'HardwareValidationScenario' 'PowerModeVerify'
Write-Result 'UiSmokeResultPath' $uiSmokeResultPath
Write-Result 'UiSmokeLogPath' $uiSmokeLogPath
Write-Result 'HardwareValidationResultPath' $hardwareValidationResultPath
Write-Result 'HardwareValidationLogPath' $hardwareValidationLogPath
Write-Result 'TimeoutSeconds' $TimeoutSeconds

Push-Location $repoRoot
try {
    $smokeProject = Join-Path $repoRoot 'Tools\MainAppPluginUi.Smoke\MainAppPluginUi.Smoke.csproj'
    $smokeProcessStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $smokeProcessStartInfo.FileName = 'dotnet'
    $smokeProcessStartInfo.WorkingDirectory = $repoRoot
    $smokeProcessStartInfo.UseShellExecute = $false
    $smokeProcessStartInfo.RedirectStandardOutput = $true
    $smokeProcessStartInfo.RedirectStandardError = $true
    $smokeProcessStartInfo.CreateNoWindow = $true
    $smokeProcessStartInfo.ArgumentList.Add('run')
    $smokeProcessStartInfo.ArgumentList.Add('--project')
    $smokeProcessStartInfo.ArgumentList.Add($smokeProject)
    $smokeProcessStartInfo.ArgumentList.Add('--')
    $smokeProcessStartInfo.ArgumentList.Add('--repo-root')
    $smokeProcessStartInfo.ArgumentList.Add($repoRoot)
    $smokeProcessStartInfo.ArgumentList.Add('--scenario')
    $smokeProcessStartInfo.ArgumentList.Add('power-mode')
    $smokeProcessStartInfo.ArgumentList.Add('--disable-animations')
    $smokeProcessStartInfo.ArgumentList.Add('--screenshots')
    $smokeProcessStartInfo.ArgumentList.Add('failures')
    $smokeProcessStartInfo.ArgumentList.Add('--power-mode-hardware-verify')

    $smokeProcess = [System.Diagnostics.Process]::new()
    $smokeProcess.StartInfo = $smokeProcessStartInfo
    $null = $smokeProcess.Start()
    Write-Result 'UiSmokeProcessId' $smokeProcess.Id
    $smokeStdoutTask = $smokeProcess.StandardOutput.ReadToEndAsync()
    $smokeStderrTask = $smokeProcess.StandardError.ReadToEndAsync()
    $smokeTimedOut = -not $smokeProcess.WaitForExit($TimeoutSeconds * 1000)
    if ($smokeTimedOut) {
        Write-Result 'UiSmokeTimedOut' 'True'
        try {
            $smokeProcess.Kill($true)
        }
        catch {
            Write-Result 'UiSmokeKillError' $_.Exception.Message
        }
    }
    $null = $smokeProcess.WaitForExit()
    $smokeStdout = $smokeStdoutTask.GetAwaiter().GetResult()
    $smokeStderr = $smokeStderrTask.GetAwaiter().GetResult()
    Set-Content -LiteralPath $uiSmokeLogPath -Value (($smokeStdout, $smokeStderr) -join [Environment]::NewLine).Trim() -Encoding UTF8
    $uiSmokeExitCode = if ($smokeTimedOut) { '<timed-out>' } else { [string]$smokeProcess.ExitCode }
    $uiSmokePassed = (-not $smokeTimedOut) -and $smokeProcess.ExitCode -eq 0
    Write-Result 'UiSmokeExitCode' $uiSmokeExitCode
    Write-Result 'UiSmokePassed' ([string]$uiSmokePassed)
    Write-Result 'UiSmokeHardwareVerificationRequested' 'True'

    $delegatedArguments = @(
        '-ExecutionPolicy', 'Bypass',
        '-File', $hardwareValidationScriptPath,
        '-RepoRoot', $repoRoot,
        '-Scenario', 'PowerModeVerify',
        '-ResultPath', $hardwareValidationResultPath,
        '-LogPath', $hardwareValidationLogPath,
        '-TimeoutSeconds', $TimeoutSeconds
    )

    $process = Start-Process -FilePath 'powershell.exe' -ArgumentList $delegatedArguments -PassThru -WindowStyle Hidden -WorkingDirectory $repoRoot
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
        $beforePowerMode = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BeforeSmartFanMode'
        $requestedPowerMode = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'RequestedSmartFanMode'
        $afterPowerMode = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'AfterSmartFanMode'
        $powerModeChanged = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'MeasuredPowerModeChangeObserved'
        $powerModePassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'PowerModeVerificationPassed'
        $powerModeRestored = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'RestoreVerificationPassed'
        $hardwareOverallPassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'OverallPassed'
        if ($null -ne $beforePowerMode) {
            Write-Result 'BeforePowerMode' $beforePowerMode
        }
        if ($null -ne $requestedPowerMode) {
            Write-Result 'RequestedPowerMode' $requestedPowerMode
        }
        if ($null -ne $afterPowerMode) {
            Write-Result 'AfterPowerMode' $afterPowerMode
        }
        if ($null -ne $powerModeChanged) {
            Write-Result 'MeasuredPowerModeChanged' $powerModeChanged
        }
        if ($null -ne $powerModePassed) {
            Write-Result 'PowerModeVerificationPassed' $powerModePassed
        }
        if ($null -ne $powerModeRestored) {
            Write-Result 'PowerModeRestorePassed' $powerModeRestored
        }
        if ($null -ne $hardwareOverallPassed) {
            Write-Result 'HardwareValidationPassed' $hardwareOverallPassed
        }
        Write-Result 'OverallPassed' ([string]($uiSmokePassed -and $hardwareOverallPassed -eq 'True'))
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
