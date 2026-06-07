param(
    [string]$ResultPathOverride,
    [string]$LogPathOverride,
    [int]$TimeoutSeconds = 180,
    [switch]$SkipElevationCheck,
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

function Get-LogValue {
    param(
        [string]$FilePath,
        [string]$Key
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        return $null
    }

    $pattern = "^\[main-smoke\]\s+{0}: " -f [Regex]::Escape($Key)
    $line = Select-String -LiteralPath $FilePath -Pattern $pattern | Select-Object -Last 1
    if (-not $line) {
        return $null
    }

    return ($line.Line -replace $pattern, '').Trim()
}

function Quote-NativeArgument {
    param([string]$Value)

    if ($null -eq $Value) {
        return '""'
    }

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    return '"' + ($Value -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
}

function Join-ProcessArguments {
    param([string[]]$Arguments)

    return ($Arguments | ForEach-Object { Quote-NativeArgument ([string]$_) }) -join ' '
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

function Get-DescendantProcessIds {
    param([int]$ParentProcessId)

    $processes = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Select-Object ProcessId, ParentProcessId)
    $queue = [System.Collections.Generic.Queue[int]]::new()
    $ids = [System.Collections.Generic.List[int]]::new()
    $queue.Enqueue($ParentProcessId)

    while ($queue.Count -gt 0) {
        $currentParentId = $queue.Dequeue()
        foreach ($process in $processes | Where-Object { $_.ParentProcessId -eq $currentParentId }) {
            $processId = [int]$process.ProcessId
            $ids.Add($processId)
            $queue.Enqueue($processId)
        }
    }

    return $ids
}

function Stop-DescendantProcesses {
    param([int]$ParentProcessId)

    $processIds = @(Get-DescendantProcessIds -ParentProcessId $ParentProcessId)
    [array]::Reverse($processIds)
    foreach ($processId in $processIds) {
        try {
            Stop-Process -Id $processId -Force -ErrorAction Stop
            Write-Result 'StoppedDescendantProcessId' ([string]$processId)
        }
        catch {
            Write-Result 'StopDescendantProcessError' ("{0}: {1}" -f $processId, $_.Exception.Message)
        }
    }
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

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $SkipElevationCheck.IsPresent -and -not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $arguments = @(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-RepoRoot', $repoRoot,
        '-ResultPathOverride', $resultPath,
        '-LogPathOverride', $logPath,
        '-TimeoutSeconds', [string]$TimeoutSeconds,
        '-SkipElevationCheck'
    )

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -WorkingDirectory $repoRoot -ArgumentList (Join-ProcessArguments -Arguments $arguments) -Wait
    exit 0
}

Write-Result 'StartedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
Write-Result 'RepositoryRoot' $repoRoot
Write-Result 'IsAdmin' ([string]$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
Write-Result 'SkipElevationCheck' ([string]$SkipElevationCheck.IsPresent)
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
    $smokeProcessArguments = @(
        'run',
        '--project', $smokeProject,
        '--',
        '--repo-root', $repoRoot,
        '--scenario', 'power-mode',
        '--disable-animations',
        '--screenshots', 'failures',
        '--power-mode-hardware-verify'
    )
    $smokeProcessStartInfo.Arguments = Join-ProcessArguments -Arguments $smokeProcessArguments

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
    Stop-DescendantProcesses -ParentProcessId $smokeProcess.Id
    Set-Content -LiteralPath $uiSmokeLogPath -Value (($smokeStdout, $smokeStderr) -join [Environment]::NewLine).Trim() -Encoding UTF8
    $uiSmokeExitCode = if ($smokeTimedOut) { '<timed-out>' } else { [string]$smokeProcess.ExitCode }
    $uiSmokePassed = (-not $smokeTimedOut) -and $smokeProcess.ExitCode -eq 0
    Write-Result 'UiSmokeExitCode' $uiSmokeExitCode
    Write-Result 'UiSmokePassed' ([string]$uiSmokePassed)
    Write-Result 'UiSmokeHardwareVerificationRequested' 'True'

    $uiBeforePowerMode = Get-LogValue -FilePath $uiSmokeLogPath -Key 'BeforeSmartFanMode'
    $uiRequestedPowerMode = Get-LogValue -FilePath $uiSmokeLogPath -Key 'RequestedSmartFanMode'
    $uiAfterPowerMode = Get-LogValue -FilePath $uiSmokeLogPath -Key 'AfterSmartFanMode'
    $uiPowerModeDelta = Get-LogValue -FilePath $uiSmokeLogPath -Key 'PowerModeDelta'
    $uiPowerModeChanged = Get-LogValue -FilePath $uiSmokeLogPath -Key 'UiPowerModeHardwareChanged'
    $uiPowerModePassed = Get-LogValue -FilePath $uiSmokeLogPath -Key 'UiPowerModeHardwareVerificationPassed'
    $uiPowerModeRestored = Get-LogValue -FilePath $uiSmokeLogPath -Key 'UiPowerModeHardwareRestorePassed'
    $uiHardwareOverallPassed = Get-LogValue -FilePath $uiSmokeLogPath -Key 'PowerModeHardwareOverallPassed'

    foreach ($field in @(
        @{ Label = 'UiBeforePowerMode'; Value = $uiBeforePowerMode },
        @{ Label = 'UiRequestedPowerMode'; Value = $uiRequestedPowerMode },
        @{ Label = 'UiAfterPowerMode'; Value = $uiAfterPowerMode },
        @{ Label = 'UiPowerModeDelta'; Value = $uiPowerModeDelta },
        @{ Label = 'UiPowerModeChanged'; Value = $uiPowerModeChanged },
        @{ Label = 'UiPowerModeVerificationPassed'; Value = $uiPowerModePassed },
        @{ Label = 'UiPowerModeRestorePassed'; Value = $uiPowerModeRestored },
        @{ Label = 'UiPowerModeHardwareOverallPassed'; Value = $uiHardwareOverallPassed }
    )) {
        if ($null -ne $field.Value) {
            Write-Result $field.Label $field.Value
        }
    }

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
        Write-Result 'OverallPassed' ([string](
            $uiSmokePassed -and
            $uiPowerModeChanged -eq 'True' -and
            $uiPowerModePassed -eq 'True' -and
            $uiPowerModeRestored -eq 'True' -and
            $uiHardwareOverallPassed -eq 'True' -and
            $hardwareOverallPassed -eq 'True'))
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
