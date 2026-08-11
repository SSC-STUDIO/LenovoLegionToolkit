param(
    [string]$ResultPath,
    [string]$LogPath,
    [int]$TimeoutSeconds = 240,
    [switch]$SkipElevationCheck,
    [switch]$SkipUiSmoke,
    [switch]$SkipGodModeBatch,
    [switch]$SkipPowerModeDirect,
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

function Invoke-ValidationScript {
    param(
        [string]$Name,
        [string]$ScriptPath,
        [string[]]$Arguments,
        [int]$WaitTimeoutSeconds
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'powershell.exe'
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true
    $startInfo.Arguments = Join-ProcessArguments -Arguments (@(
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', $ScriptPath
    ) + $Arguments)

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $null = $process.Start()
    Write-Result "$Name.ProcessId" ([string]$process.Id)

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    $timedOut = -not $process.WaitForExit($WaitTimeoutSeconds * 1000)
    if ($timedOut) {
        Write-Result "$Name.TimedOut" 'True'
        try {
            $process.Kill($true)
        }
        catch {
            Write-Result "$Name.KillError" $_.Exception.Message
        }
    }

    $null = $process.WaitForExit()
    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    $exitCode = if ($timedOut) { '<timed-out>' } else { [string]$process.ExitCode }

    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
        Add-Content -LiteralPath $logPath -Value ("[{0}:stdout]" -f $Name)
        Add-Content -LiteralPath $logPath -Value $stdout.Trim()
    }

    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
        Add-Content -LiteralPath $logPath -Value ("[{0}:stderr]" -f $Name)
        Add-Content -LiteralPath $logPath -Value $stderr.Trim()
    }

    Write-Result "$Name.ExitCode" $exitCode
    return (-not $timedOut) -and $process.ExitCode -eq 0
}

function Write-SelectedChildResults {
    param(
        [string]$Prefix,
        [string]$FilePath,
        [string[]]$Keys
    )

    foreach ($key in $Keys) {
        $value = Get-ResultValue -FilePath $FilePath -Key $key
        if ($null -ne $value) {
            Write-Result "$Prefix.$key" $value
        }
    }
}

$repoRoot = Resolve-RepositoryRoot -Path $RepoRoot
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$resultPath = if ($ResultPath) { Resolve-AbsolutePath $ResultPath } else { Join-Path $repoRoot ("Tools\HardwareValidation\PerformanceEffectVerification-{0}.result.txt" -f $timestamp) }
$logPath = if ($LogPath) { Resolve-AbsolutePath $LogPath } else { Join-Path $repoRoot ("Tools\HardwareValidation\PerformanceEffectVerification-{0}.log.txt" -f $timestamp) }
$artifactRoot = Join-Path ([System.IO.Path]::GetDirectoryName($resultPath)) ("PerformanceEffectVerification-{0}" -f $timestamp)

[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($resultPath)) | Out-Null
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($logPath)) | Out-Null
[System.IO.Directory]::CreateDirectory($artifactRoot) | Out-Null

foreach ($path in @($resultPath, $logPath)) {
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
        '-ResultPath', $resultPath,
        '-LogPath', $logPath,
        '-TimeoutSeconds', [string]$TimeoutSeconds,
        '-SkipElevationCheck'
    )

    if ($SkipUiSmoke.IsPresent) { $arguments += '-SkipUiSmoke' }
    if ($SkipGodModeBatch.IsPresent) { $arguments += '-SkipGodModeBatch' }
    if ($SkipPowerModeDirect.IsPresent) { $arguments += '-SkipPowerModeDirect' }

    Start-Process -FilePath 'powershell.exe' -Verb RunAs -WorkingDirectory $repoRoot -ArgumentList (Join-ProcessArguments -Arguments $arguments) -Wait
    exit 0
}

Write-Result 'StartedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
Write-Result 'RepositoryRoot' $repoRoot
Write-Result 'IsAdmin' ([string]$principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
Write-Result 'SkipElevationCheck' ([string]$SkipElevationCheck.IsPresent)
Write-Result 'Scenario' 'PerformanceEffectVerification'
Write-Result 'ArtifactRoot' $artifactRoot
Write-Result 'TimeoutSeconds' $TimeoutSeconds
Write-Result 'SkipUiSmoke' ([string]$SkipUiSmoke.IsPresent)
Write-Result 'SkipGodModeBatch' ([string]$SkipGodModeBatch.IsPresent)
Write-Result 'SkipPowerModeDirect' ([string]$SkipPowerModeDirect.IsPresent)

$hardwareScript = Join-Path $repoRoot 'Tools\HardwareValidation\Run-HardwareValidationElevated.ps1'

Push-Location $repoRoot
try {
    $batchResultPath = Join-Path $artifactRoot 'godmode-batch.result.txt'
    $batchLogPath = Join-Path $artifactRoot 'godmode-batch.log.txt'
    $directPowerModeResultPath = Join-Path $artifactRoot 'power-mode-direct.result.txt'
    $directPowerModeLogPath = Join-Path $artifactRoot 'power-mode-direct.log.txt'

    $overallChecks = [System.Collections.Generic.List[bool]]::new()

    if (-not $SkipGodModeBatch.IsPresent) {
        $batchProcessPassed = Invoke-ValidationScript `
            -Name 'GodModeBatchHardware' `
            -ScriptPath $hardwareScript `
            -Arguments @(
                '-RepoRoot', $repoRoot,
                '-Scenario', 'BatchDefault',
                '-ResultPath', $batchResultPath,
                '-LogPath', $batchLogPath,
                '-TimeoutSeconds', [string]$TimeoutSeconds,
                '-SkipElevationCheck'
            ) `
            -WaitTimeoutSeconds ($TimeoutSeconds + 60)

        Write-Result 'GodModeBatchHardware.ResultPath' $batchResultPath
        Write-Result 'GodModeBatchHardware.LogPath' $batchLogPath
        Write-SelectedChildResults -Prefix 'GodModeBatchHardware' -FilePath $batchResultPath -Keys @(
            'BatchCapabilities',
            'BatchPassedCount',
            'BatchMeasuredChangedCount',
            'BatchMeasuredDeltas',
            'BatchMeasuredChangeObserved',
            'BatchPowerModeObservedGodMode',
            'BatchVerificationPassed',
            'BatchRestoreVerificationPassed',
            'OverallPassed'
        )

        $batchOverall = Get-ResultValue -FilePath $batchResultPath -Key 'OverallPassed'
        $overallChecks.Add($batchProcessPassed -and $batchOverall -eq 'True')
    }

    if (-not $SkipPowerModeDirect.IsPresent) {
        $directProcessPassed = Invoke-ValidationScript `
            -Name 'DirectPowerModeHardware' `
            -ScriptPath $hardwareScript `
            -Arguments @(
                '-RepoRoot', $repoRoot,
                '-Scenario', 'PowerModeVerify',
                '-ResultPath', $directPowerModeResultPath,
                '-LogPath', $directPowerModeLogPath,
                '-TimeoutSeconds', [string]$TimeoutSeconds,
                '-SkipElevationCheck'
            ) `
            -WaitTimeoutSeconds ($TimeoutSeconds + 60)

        Write-Result 'DirectPowerModeHardware.ResultPath' $directPowerModeResultPath
        Write-Result 'DirectPowerModeHardware.LogPath' $directPowerModeLogPath
        Write-SelectedChildResults -Prefix 'DirectPowerModeHardware' -FilePath $directPowerModeResultPath -Keys @(
            'BeforeSmartFanMode',
            'RequestedSmartFanMode',
            'AfterSmartFanMode',
            'PowerModeDelta',
            'MeasuredPowerModeChangeObserved',
            'PowerModeVerificationPassed',
            'RestoreVerificationPassed',
            'OverallPassed'
        )

        $directOverall = Get-ResultValue -FilePath $directPowerModeResultPath -Key 'OverallPassed'
        $overallChecks.Add($directProcessPassed -and $directOverall -eq 'True')
    }

    if ($overallChecks.Count -eq 0) {
        throw 'No verification checks were selected.'
    }

    $overallPassed = -not $overallChecks.Contains($false)
    Write-Result 'OverallPassed' ([string]$overallPassed)
}
catch {
    Write-Result 'Exception' $_.Exception.ToString()
    Write-Result 'OverallPassed' 'False'
}
finally {
    Pop-Location
}

Write-Result 'FinishedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
