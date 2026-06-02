param(
    [string]$ResultPath,
    [string]$LogPath,
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

$resultPath = if ($ResultPath) { Resolve-AbsolutePath $ResultPath } else { Join-Path $repoRoot 'Tools\MainAppPluginUi.Smoke\AdminDirectHardwareSmoke.result.txt' }
$logPath = if ($LogPath) { Resolve-AbsolutePath $LogPath } else { Join-Path $repoRoot 'Tools\MainAppPluginUi.Smoke\AdminDirectHardwareSmoke.smoke.txt' }
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
Write-Result 'Scenario' 'BatchDefault'
Write-Result 'TimeoutSeconds' $TimeoutSeconds

Push-Location $repoRoot
try {
    $delegatedArguments = @(
        '-ExecutionPolicy', 'Bypass',
        '-File', $hardwareValidationScriptPath,
        '-Scenario', 'BatchDefault',
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
        $overallPassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'OverallPassed'
        $batchPassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BatchVerificationPassed'
        $batchRestorePassed = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BatchRestoreVerificationPassed'
        $batchCapabilities = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BatchCapabilities'
        $batchMeasuredChangedCount = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BatchMeasuredChangedCount'
        $batchMeasuredDeltas = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BatchMeasuredDeltas'
        $batchMeasuredChangeObserved = Get-ResultValue -FilePath $hardwareValidationResultPath -Key 'BatchMeasuredChangeObserved'

        if ($null -ne $batchCapabilities) {
            Write-Result 'HardwareValidationBatchCapabilities' $batchCapabilities
        }
        if ($null -ne $batchPassed) {
            Write-Result 'HardwareValidationBatchPassed' $batchPassed
        }
        if ($null -ne $batchMeasuredChangedCount) {
            Write-Result 'HardwareValidationBatchMeasuredChangedCount' $batchMeasuredChangedCount
        }
        if ($null -ne $batchMeasuredDeltas) {
            Write-Result 'HardwareValidationBatchMeasuredDeltas' $batchMeasuredDeltas
        }
        if ($null -ne $batchMeasuredChangeObserved) {
            Write-Result 'HardwareValidationBatchMeasuredChangeObserved' $batchMeasuredChangeObserved
        }
        if ($null -ne $batchRestorePassed) {
            Write-Result 'HardwareValidationBatchRestorePassed' $batchRestorePassed
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
