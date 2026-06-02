param(
    [string]$HardwareValidationDllPath,
    [ValidateSet('StatusCheck', 'CpuVerify', 'BatchDefault')]
    [string]$Scenario,
    [string]$Command = 'godmode',
    [string[]]$CommandArguments = @('verify-current-preset', 'CPULongTermPowerLimit', '1'),
    [string]$ResultPath,
    [string]$LogPath,
    [int]$TimeoutSeconds = 180,
    [switch]$SkipElevationCheck
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

function Resolve-Scenario {
    param([string]$SelectedScenario)

    switch ($SelectedScenario) {
        'StatusCheck' {
            return @{
                Command = 'godmode'
                CommandArguments = @('status')
            }
        }
        'CpuVerify' {
            return @{
                Command = 'godmode'
                CommandArguments = @('verify-current-preset', 'CPULongTermPowerLimit', '1')
            }
        }
        'BatchDefault' {
            return @{
                Command = 'godmode'
                CommandArguments = @('verify-current-preset-batch', 'CPULongTermPowerLimit', 'GPUConfigurableTGP', 'GPUTemperatureLimit')
            }
        }
        default {
            return $null
        }
    }
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

function Get-VerificationLine {
    param(
        [string]$Content,
        [string]$Pattern
    )

    if ([string]::IsNullOrWhiteSpace($Content)) {
        return $null
    }

    $match = [System.Text.RegularExpressions.Regex]::Match($Content, "(?m)^$Pattern\s*:\s*(.+)$")
    if ($match.Success) {
        return $match.Groups[1].Value.Trim()
    }

    return $null
}

if ($CommandArguments.Count -eq 1 -and -not [string]::IsNullOrWhiteSpace($CommandArguments[0]) -and $CommandArguments[0].Contains(',')) {
    $CommandArguments = @($CommandArguments[0].Split(',', [System.StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() })
}

if (-not [string]::IsNullOrWhiteSpace($env:UDT_HARDWARE_VALIDATION_COMMAND_ARGUMENTS_JSON)) {
    $decodedArguments = ConvertFrom-Json -InputObject $env:UDT_HARDWARE_VALIDATION_COMMAND_ARGUMENTS_JSON
    if ($decodedArguments -is [System.Array]) {
        $CommandArguments = @($decodedArguments | ForEach-Object { [string]$_ })
    }
    elseif ($null -ne $decodedArguments) {
        $CommandArguments = @([string]$decodedArguments)
    }
    else {
        $CommandArguments = @()
    }
}

if (-not [string]::IsNullOrWhiteSpace($Scenario)) {
    $resolvedScenario = Resolve-Scenario $Scenario
    if ($null -ne $resolvedScenario) {
        $Command = $resolvedScenario.Command
        $CommandArguments = $resolvedScenario.CommandArguments
    }
}

$hardwareValidationDll = Resolve-AbsolutePath $HardwareValidationDllPath

if (-not $hardwareValidationDll -or
    -not $hardwareValidationDll.EndsWith('.dll', [System.StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $hardwareValidationDll)) {
    $hardwareValidationDll = Join-Path $repoRoot 'Tools\HardwareValidation\bin\Release\net10.0-windows10.0.26100.0\HardwareValidation.dll'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$resultPath = if ($ResultPath) { Resolve-AbsolutePath $ResultPath } else { Join-Path $repoRoot ("Tools\HardwareValidation\HardwareValidation-{0}.result.txt" -f $timestamp) }
$logPath = if ($LogPath) { Resolve-AbsolutePath $LogPath } else { Join-Path $repoRoot ("Tools\HardwareValidation\HardwareValidation-{0}.log.txt" -f $timestamp) }
$stdoutLogPath = "$logPath.stdout"
$stderrLogPath = "$logPath.stderr"

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $SkipElevationCheck -and -not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $commandArgumentsJson = ConvertTo-Json -Compress -InputObject @($CommandArguments)
    [Environment]::SetEnvironmentVariable('UDT_HARDWARE_VALIDATION_COMMAND_ARGUMENTS_JSON', $commandArgumentsJson, 'Process')
    $arguments = @(
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-HardwareValidationDllPath', $hardwareValidationDll,
        '-Scenario', $Scenario,
        '-Command', $Command,
        '-ResultPath', $resultPath,
        '-LogPath', $logPath,
        '-TimeoutSeconds', $TimeoutSeconds
    )

    $quotedArguments = ($arguments | ForEach-Object { Quote-NativeArgument ([string]$_) }) -join ' '
    Start-Process -FilePath 'powershell.exe' -Verb RunAs -WorkingDirectory $repoRoot -ArgumentList $quotedArguments
    exit 0
}

[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($resultPath)) | Out-Null
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($logPath)) | Out-Null

foreach ($path in @($resultPath, $logPath, $stdoutLogPath, $stderrLogPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

Write-Result 'StartedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
Write-Result 'IsAdmin' ([string]$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
Write-Result 'SkipElevationCheck' ([string]$SkipElevationCheck.IsPresent)
Write-Result 'HardwareValidationDllPath' $hardwareValidationDll
Write-Result 'Command' $Command
Write-Result 'CommandArguments' ($CommandArguments -join ' ')
Write-Result 'TimeoutSeconds' $TimeoutSeconds

Push-Location $repoRoot
try {
    $nativeArguments = @($hardwareValidationDll, $Command) + $CommandArguments
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.Arguments = (($nativeArguments | ForEach-Object { Quote-NativeArgument $_ }) -join ' ')
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $null = $process.Start()
    Write-Result 'ProcessId' $process.Id

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()

    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) {
        Write-Result 'TimedOut' 'True'
        try {
            $process.Kill($true)
        }
        catch {
            Write-Result 'KillError' $_.Exception.Message
        }
    }

    $null = $process.WaitForExit()

    $stdoutContent = $stdoutTask.GetAwaiter().GetResult()
    $stderrContent = $stderrTask.GetAwaiter().GetResult()
    Set-Content -LiteralPath $stdoutLogPath -Value $stdoutContent -Encoding UTF8
    Set-Content -LiteralPath $stderrLogPath -Value $stderrContent -Encoding UTF8
    $combinedLogContent = (($stdoutContent, $stderrContent) -join [Environment]::NewLine).Trim()
    Set-Content -LiteralPath $logPath -Value $combinedLogContent -Encoding UTF8

    $exitCodeString = if ($timedOut) { '<timed-out>' } else { [string]$process.ExitCode }
    Write-Result 'ExitCode' $exitCodeString

    foreach ($field in @(
        'Capability',
        'BatchCapabilities',
        'BatchCapabilityCount',
        'BatchAfterSmartFanMode',
        'BatchPassedCount',
        'BatchVerificationPassed',
        'BatchRestoredSmartFanMode',
        'BatchRestoreVerificationPassed',
        'OriginalPresetValue',
        'BeforeHardwareValue',
        'RequestedPresetValue',
        'RequestedHardwareDelta',
        'PersistedPresetValue',
        'AfterHardwareValue',
        'HardwareValueDelta',
        'HardwareValueChanged',
        'AfterSmartFanMode',
        'PersistedVerificationPassed',
        'HardwareVerificationPassed',
        'MeasuredVerificationPassed',
        'RestoredPresetValue',
        'RestoredHardwareValue',
        'RestoredHardwareDeltaFromBefore',
        'RestoredSmartFanMode',
        'RestoreVerificationPassed',
        'BatchMeasuredChangedCount',
        'BatchMeasuredDeltas',
        'BatchMeasuredChangeObserved'
    )) {
        $value = Get-VerificationLine -Content $combinedLogContent -Pattern $field
        if ($null -ne $value) {
            Write-Result $field $value
        }
    }

    $hardwarePassed = Get-VerificationLine -Content $combinedLogContent -Pattern 'HardwareVerificationPassed'
    $restorePassed = Get-VerificationLine -Content $combinedLogContent -Pattern 'RestoreVerificationPassed'
    $measuredPassed = Get-VerificationLine -Content $combinedLogContent -Pattern 'MeasuredVerificationPassed'
    $persistedPassed = Get-VerificationLine -Content $combinedLogContent -Pattern 'PersistedVerificationPassed'
    $isVerifyCurrentPreset =
        $Command -eq 'godmode' -and
        $CommandArguments.Count -gt 0 -and
        $CommandArguments[0] -eq 'verify-current-preset'
    $isVerifyCurrentPresetBatch =
        $Command -eq 'godmode' -and
        $CommandArguments.Count -gt 0 -and
        $CommandArguments[0] -eq 'verify-current-preset-batch'
    $batchPassed = Get-VerificationLine -Content $combinedLogContent -Pattern 'BatchVerificationPassed'
    $batchRestorePassed = Get-VerificationLine -Content $combinedLogContent -Pattern 'BatchRestoreVerificationPassed'
    $batchMeasuredChangeObserved = Get-VerificationLine -Content $combinedLogContent -Pattern 'BatchMeasuredChangeObserved'
    $overallPassed = if ($isVerifyCurrentPreset) {
        (-not $timedOut -and $process.ExitCode -eq 0) -and
        $persistedPassed -eq 'True' -and
        $hardwarePassed -eq 'True' -and
        $measuredPassed -eq 'True' -and
        $restorePassed -eq 'True'
    }
    elseif ($isVerifyCurrentPresetBatch) {
        (-not $timedOut -and $process.ExitCode -eq 0) -and
        $batchPassed -eq 'True' -and
        $batchMeasuredChangeObserved -eq 'True' -and
        $batchRestorePassed -eq 'True'
    }
    else {
        -not $timedOut -and $process.ExitCode -eq 0
    }
    Write-Result 'OverallPassed' $overallPassed
}
catch {
    Write-Result 'Exception' $_.Exception.ToString()
}
finally {
    Pop-Location
}

Write-Result 'FinishedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
