param(
    [string]$PresetUiValidationExePath,
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

$presetUiValidationPath = Resolve-AbsolutePath $PresetUiValidationExePath
if (-not $presetUiValidationPath -or -not (Test-Path -LiteralPath $presetUiValidationPath)) {
    $presetUiValidationPath = Join-Path $repoRoot 'Tools\PresetUiValidation\bin\Release\net10.0-windows10.0.26100.0\PresetUiValidation.dll'
}

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$resultPath = if ($ResultPath) { Resolve-AbsolutePath $ResultPath } else { Join-Path $repoRoot ("Tools\PresetUiValidation\PresetUiValidation-{0}.result.txt" -f $timestamp) }
$logPath = if ($LogPath) { Resolve-AbsolutePath $LogPath } else { Join-Path $repoRoot ("Tools\PresetUiValidation\PresetUiValidation-{0}.log.txt" -f $timestamp) }
$validatorResultPath = [System.IO.Path]::ChangeExtension($resultPath, '.validator.result.txt')

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $arguments = @(
        '-ExecutionPolicy', 'Bypass',
        '-File', $PSCommandPath,
        '-PresetUiValidationExePath', $presetUiValidationPath,
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

foreach ($path in @($resultPath, $logPath, $validatorResultPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

Write-Result 'StartedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
Write-Result 'IsAdmin' 'True'
Write-Result 'PresetUiValidationExePath' $presetUiValidationPath
Write-Result 'TimeoutSeconds' $TimeoutSeconds

Push-Location $repoRoot
try {
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.Arguments = ((@($presetUiValidationPath, "--result-file=$validatorResultPath") | ForEach-Object { Quote-NativeArgument $_ }) -join ' ')
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $null = $process.Start()
    Write-Result 'ValidatorProcessId' $process.Id

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
    $combinedLogContent = (($stdoutContent, $stderrContent) -join [Environment]::NewLine).Trim()
    Set-Content -LiteralPath $logPath -Value $combinedLogContent -Encoding UTF8

    $exitCodeString = if ($timedOut) { '<timed-out>' } else { [string]$process.ExitCode }
    Write-Result 'ValidatorExitCode' $exitCodeString
    $validatorResultReady = Wait-ForFile -Path $validatorResultPath -TimeoutSeconds 5
    Write-Result 'ValidatorResultReady' $validatorResultReady

    if ($validatorResultReady) {
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
            'RestorePresetStateVerificationPassed'
        )) {
            $value = Get-ResultValue -FilePath $validatorResultPath -Key $field
            if ($null -ne $value) {
                Write-Result $field $value
            }
        }
    }

    $overallPassed =
        (-not $timedOut -and $process.ExitCode -eq 0) -and
        (Get-ResultValue -FilePath $validatorResultPath -Key 'PresetUiCrudVerificationPassed') -eq 'True' -and
        (Get-ResultValue -FilePath $validatorResultPath -Key 'RestorePresetStateVerificationPassed') -eq 'True'

    Write-Result 'OverallPassed' $overallPassed
}
catch {
    Write-Result 'Exception' $_.Exception.ToString()
}
finally {
    Pop-Location
}

Write-Result 'FinishedAtUtc' ([DateTimeOffset]::UtcNow.ToString('O'))
