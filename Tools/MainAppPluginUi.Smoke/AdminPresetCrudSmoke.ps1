param(
    [string]$SmokeExePath,
    [string]$AppExePath,
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

    $content = $null
    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        try {
            $content = Get-Content -LiteralPath $FilePath -Raw -ErrorAction Stop
            break
        }
        catch [System.IO.IOException] {
            Start-Sleep -Milliseconds 300
        }
    }

    if ([string]::IsNullOrWhiteSpace($content)) {
        return $null
    }

    $match = [Regex]::Match($content, "(?m)^{0}: (.+)$" -f [Regex]::Escape($Key))
    if (-not $match.Success) {
        return $null
    }

    return $match.Groups[1].Value.Trim()
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

function Copy-LogIfReady {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        return $false
    }

    for ($attempt = 0; $attempt -lt 10; $attempt++) {
        try {
            Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force -ErrorAction Stop
            return $true
        }
        catch [System.IO.IOException] {
            Start-Sleep -Milliseconds 300
        }
    }

    return $false
}

$resultPath = Resolve-AbsolutePath $ResultPath
$logPath = Resolve-AbsolutePath $LogPath
$presetValidationResultPath = [System.IO.Path]::ChangeExtension($resultPath, '.preset.result.txt')
$presetValidationLogPath = [System.IO.Path]::ChangeExtension($logPath, '.preset.log.txt')
$presetValidationScriptPath = Join-Path $repoRoot 'Tools\PresetUiValidation\Run-PresetUiValidationElevated.ps1'

[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($resultPath)) | Out-Null
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($logPath)) | Out-Null

foreach ($path in @($resultPath, $logPath, $presetValidationResultPath, $presetValidationLogPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

Write-Result 'StartedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
Write-Result 'DelegatedTo' $presetValidationScriptPath
Write-Result 'TimeoutSeconds' $TimeoutSeconds

Push-Location $repoRoot
try {
    $delegatedArguments = @(
        '-ExecutionPolicy', 'Bypass',
        '-File', $presetValidationScriptPath,
        '-ResultPath', $presetValidationResultPath,
        '-LogPath', $presetValidationLogPath,
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

    $delegatedResultReady = Wait-ForFile -Path $presetValidationResultPath -TimeoutSeconds ($TimeoutSeconds + 30)
    $delegatedLogReady = Wait-ForFile -Path $presetValidationLogPath -TimeoutSeconds 10

    Write-Result 'DelegatedResultReady' $delegatedResultReady
    Write-Result 'DelegatedLogReady' ($delegatedLogReady -or (Test-Path -LiteralPath $presetValidationLogPath))

    if ($delegatedResultReady) {
        foreach ($field in @(
            'OriginalPresetCount',
            'CreatePresetExists',
            'CreateCountVerificationPassed',
            'CreateActiveVerificationPassed',
            'CreateNameVerificationPassed',
            'RenameCountVerificationPassed',
            'RenameActiveVerificationPassed',
            'RenameNameVerificationPassed',
            'DeleteMissingVerificationPassed',
            'DeleteCountVerificationPassed',
            'DeleteActiveVerificationPassed',
            'PersistedDeleteVerificationPassed',
            'PresetUiCrudVerificationPassed',
            'RestorePresetStateVerificationPassed',
            'OverallPassed'
        )) {
            $value = Get-ResultValue -FilePath $presetValidationResultPath -Key $field
            if ($null -ne $value) {
                Write-Result $field $value
            }
        }

        $overallPassed = Get-ResultValue -FilePath $presetValidationResultPath -Key 'OverallPassed'
        Write-Result 'SmokeOutcome' ($(if ($overallPassed -eq 'True') { 'PASS' } else { 'FAIL' }))
        Write-Result 'PresetCrudPassed' ($overallPassed -eq 'True')
    }
    else {
        Write-Result 'SmokeOutcome' 'FAIL'
        Write-Result 'PresetCrudPassed' 'False'
        Write-Result 'PresetValidationResultMissing' 'True'
    }

    if ($delegatedLogReady -or (Test-Path -LiteralPath $presetValidationLogPath)) {
        $logCopied = Copy-LogIfReady -SourcePath $presetValidationLogPath -DestinationPath $logPath
        Write-Result 'DelegatedLogCopied' $logCopied
    }
}
catch {
    Write-Result 'Exception' $_.Exception.ToString()
}
finally {
    Pop-Location
}

Write-Result 'FinishedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
