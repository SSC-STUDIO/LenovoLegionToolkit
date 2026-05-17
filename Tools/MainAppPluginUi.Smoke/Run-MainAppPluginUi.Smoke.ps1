param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot,
    [string]$PluginIds = 'shell-integration,custom-mouse',
    [string]$PluginSources = 'shell-integration=online,custom-mouse=local',
    [ValidateSet('', 'custom', 'shell-local', 'combo-local', 'driver-download', 'system-optimization')]
    [string]$Scenario = 'custom',
    [ValidateSet('system', 'light', 'dark')]
    [string]$Theme = 'system',
    [ValidateSet('off', 'failures', 'always')]
    [string]$ScreenshotMode = 'always',
    [string]$ArtifactRoot = '',
    [string]$DotnetExecutable = 'dotnet',
    [switch]$Watch,
    [int]$StepDelayMs = 1200,
    [int]$SuccessHoldMs = 5000,
    [int]$FailureHoldMs = 15000,
    [switch]$KeepArtifacts
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not [Environment]::UserInteractive) {
    throw 'MainAppPluginUi.Smoke requires an interactive Windows session. Run it on a dedicated UI runner, not as a background service session.'
}

$resolvedRepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
$artifactBase = if ([string]::IsNullOrWhiteSpace($ArtifactRoot)) {
    Join-Path ([System.IO.Path]::GetTempPath()) ("llt-main-ui-smoke-{0:yyyyMMdd-HHmmss}" -f [DateTime]::Now)
}
else {
    [System.IO.Path]::GetFullPath($ArtifactRoot)
}

$screenshotDirectory = Join-Path $artifactBase 'screenshots'
$logPath = Join-Path $artifactBase 'main-app-plugin-ui-smoke.log'
$metadataPath = Join-Path $artifactBase 'run-metadata.json'
$smokeOutputRoot = Join-Path $resolvedRepositoryRoot 'Tools\MainAppPluginUi.Smoke\bin\Release'
$smokeDllPath = Get-ChildItem -Path $smokeOutputRoot -Filter 'MainAppPluginUi.Smoke.dll' -Recurse -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

New-Item -ItemType Directory -Path $artifactBase -Force | Out-Null
New-Item -ItemType Directory -Path $screenshotDirectory -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($smokeDllPath) -or -not (Test-Path $smokeDllPath)) {
    throw "Smoke tool was not built: $smokeDllPath"
}

$commandArguments = @(
    $smokeDllPath,
    '--repo-root', $resolvedRepositoryRoot,
    '--theme', $Theme,
    '--screenshots', $ScreenshotMode,
    '--screenshot-dir', $screenshotDirectory
)

if ($Scenario -and $Scenario -ne 'custom') {
    $commandArguments += @('--scenario', $Scenario)
}
else {
    $commandArguments += @('--plugin', $PluginIds, '--plugin-source', $PluginSources)
}

if ($Watch.IsPresent) {
    $commandArguments += '--watch'
    $commandArguments += @('--step-delay-ms', $StepDelayMs, '--success-hold-ms', $SuccessHoldMs, '--failure-hold-ms', $FailureHoldMs)
}

if ($KeepArtifacts.IsPresent) {
    $commandArguments += '--keep-artifacts'
}

$metadata = [ordered]@{
    repositoryRoot = $resolvedRepositoryRoot
    pluginIds = $PluginIds
    pluginSources = $PluginSources
    scenario = $Scenario
    theme = $Theme
    screenshotMode = $ScreenshotMode
    artifactRoot = $artifactBase
    screenshotDirectory = $screenshotDirectory
    smokeDllPath = $smokeDllPath
    dotnetExecutable = $DotnetExecutable
    watch = $Watch.IsPresent
    stepDelayMs = $StepDelayMs
    successHoldMs = $SuccessHoldMs
    failureHoldMs = $FailureHoldMs
    keepArtifacts = $KeepArtifacts.IsPresent
    startedAt = [DateTimeOffset]::Now.ToString('O')
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -Path $metadataPath -Encoding UTF8

Write-Host "[main-smoke-runner] Artifact root: $artifactBase"
Write-Host "[main-smoke-runner] Screenshot directory: $screenshotDirectory"
Write-Host "[main-smoke-runner] Command: $DotnetExecutable $($commandArguments -join ' ')"

$output = & $DotnetExecutable @commandArguments 2>&1
$exitCode = $LASTEXITCODE

$output | Tee-Object -FilePath $logPath

$metadata.finishedAt = [DateTimeOffset]::Now.ToString('O')
$metadata.exitCode = $exitCode
$metadata.logPath = $logPath
$metadata | ConvertTo-Json -Depth 5 | Set-Content -Path $metadataPath -Encoding UTF8

if ($exitCode -ne 0) {
    throw "MainAppPluginUi.Smoke failed with exit code $exitCode. See $logPath"
}
