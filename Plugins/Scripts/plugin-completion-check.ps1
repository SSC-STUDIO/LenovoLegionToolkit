param(
    [string[]]$PluginIds = @(),
    [string]$Configuration = "Release",
    [ValidateSet("contributor", "official-candidate", "official-release")]
    [string]$Profile = "official-candidate",
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [Alias("OutputJson")]
    [string]$JsonReportPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$cliProject = Join-Path $repoRoot "Tools\PluginTooling.Cli\PluginTooling.Cli.csproj"
$dotnet = if ($env:DOTNET_HOST_PATH) { $env:DOTNET_HOST_PATH } else { "dotnet" }

$arguments = @(
    "run",
    "--project", $cliProject,
    "--",
    "validate",
    "--repository-root", $repoRoot,
    "--profile", $Profile,
    "--configuration", $Configuration
)

if ($SkipBuild) {
    $arguments += "--skip-build"
}

if ($SkipTests) {
    $arguments += "--skip-tests"
}

if ($JsonReportPath) {
    $arguments += @("--json-report-path", $JsonReportPath)
}

$normalizedPluginIds = @()
foreach ($id in $PluginIds) {
    if ($id -match ",") {
        $normalizedPluginIds += $id.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    } elseif ($id) {
        $normalizedPluginIds += $id
    }
}

if ($normalizedPluginIds.Count -gt 0) {
    $arguments += @("--plugin-ids", ($normalizedPluginIds | Select-Object -Unique) -join ",")
}

& $dotnet @arguments
exit $LASTEXITCODE
