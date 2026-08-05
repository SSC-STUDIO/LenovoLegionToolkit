param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ToolArgs = @()
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$configuration = if ($env:LLT_PLUGIN_TOOLING_CONFIGURATION) { $env:LLT_PLUGIN_TOOLING_CONFIGURATION } else { "Release" }
$dotnet = if ($env:DOTNET_HOST_PATH) { $env:DOTNET_HOST_PATH } else { "dotnet" }

$projectPath = Join-Path $repoRoot "Tooling\PluginTooling.Cli\PluginTooling.Cli.csproj"
$publishRoot = Join-Path $repoRoot ".build\tooling"
$publishDir = Join-Path $publishRoot "PluginTooling.Cli"
$stampPath = Join-Path $publishDir ".publish-stamp"
$exePath = Join-Path $publishDir "PluginTooling.Cli.exe"
$lockPath = Join-Path $publishRoot ".plugin-tooling.lock"
$toolingPublishLockStream = $null

function Get-InputLastWriteUtc {
    $inputs = @(
        (Join-Path $repoRoot "Directory.Build.props"),
        (Join-Path $repoRoot "Directory.Build.targets"),
        (Join-Path $repoRoot "Tooling\PluginTooling.Cli\PluginTooling.Cli.csproj"),
        (Join-Path $repoRoot "Tooling\PluginTooling.Core\PluginTooling.Core.csproj"),
        (Join-Path $repoRoot "SDK\Runtime\UniversalDeviceToolkit.Plugins.SDK.csproj")
    )

    $inputs += Get-ChildItem -Path (Join-Path $repoRoot "Tooling\PluginTooling.Cli") -Filter "*.cs" -File -Recurse | ForEach-Object { $_.FullName }
    $inputs += Get-ChildItem -Path (Join-Path $repoRoot "Tooling\PluginTooling.Core") -Filter "*.cs" -File -Recurse | ForEach-Object { $_.FullName }
    $inputs += Get-ChildItem -Path (Join-Path $repoRoot "SDK\Runtime") -Filter "*.cs" -File -Recurse | ForEach-Object { $_.FullName }

    $latest = [DateTime]::MinValue
    foreach ($inputPath in $inputs) {
        if (-not (Test-Path $inputPath)) {
            continue
        }

        $item = Get-Item $inputPath
        if ($item.LastWriteTimeUtc -gt $latest) {
            $latest = $item.LastWriteTimeUtc
        }
    }

    return $latest
}

function Test-PublishRequired {
    if (-not (Test-Path $exePath)) {
        return $true
    }

    if (-not (Test-Path $stampPath)) {
        return $true
    }

    $stamp = Get-Item $stampPath
    return (Get-InputLastWriteUtc) -gt $stamp.LastWriteTimeUtc
}

function Enter-ToolingPublishLock {
    New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
    $deadline = (Get-Date).AddMinutes(5)

    while ($true) {
        try {
            $script:toolingPublishLockStream = [System.IO.File]::Open(
                $lockPath,
                [System.IO.FileMode]::CreateNew,
                [System.IO.FileAccess]::ReadWrite,
                [System.IO.FileShare]::None)

            $lockBytes = [System.Text.Encoding]::UTF8.GetBytes("pid=$PID; created=$([DateTimeOffset]::UtcNow.ToString("O"))")
            $script:toolingPublishLockStream.Write($lockBytes, 0, $lockBytes.Length)
            $script:toolingPublishLockStream.Flush()
            return
        }
        catch {
            if (Test-Path $lockPath) {
                $lock = Get-Item $lockPath
                if ($lock.LastWriteTimeUtc -lt [DateTime]::UtcNow.AddMinutes(-10)) {
                    Remove-Item -LiteralPath $lockPath -Recurse -Force -ErrorAction SilentlyContinue
                    continue
                }
            }

            if ((Get-Date) -gt $deadline) {
                throw "Timed out waiting for plugin tooling publish lock: $lockPath"
            }

            Start-Sleep -Milliseconds 250
        }
    }
}

function Exit-ToolingPublishLock {
    if ($null -ne $script:toolingPublishLockStream) {
        $script:toolingPublishLockStream.Dispose()
        $script:toolingPublishLockStream = $null
    }

    if (Test-Path $lockPath) {
        Remove-Item -LiteralPath $lockPath -Force -ErrorAction SilentlyContinue
    }
}

Enter-ToolingPublishLock
try {
    if (Test-PublishRequired) {
        New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
        & $dotnet publish $projectPath -c $configuration -o $publishDir --nologo
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }

        Set-Content -LiteralPath $stampPath -Value ([DateTimeOffset]::UtcNow.ToString("O")) -Encoding utf8
    }
}
finally {
    Exit-ToolingPublishLock
}

if (-not (Test-Path $exePath)) {
    throw "Plugin tooling executable was not created: $exePath"
}

$arguments = @($ToolArgs)
if (-not ($arguments | Where-Object { $_ -ieq "--repository-root" })) {
    $arguments += @("--repository-root", $repoRoot)
}

& $exePath @arguments
exit $LASTEXITCODE
