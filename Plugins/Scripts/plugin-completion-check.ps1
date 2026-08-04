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
$toolingScript = Join-Path $repoRoot "Scripts\Invoke-PluginTooling.ps1"

$arguments = @(
    "validate",
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
    $pluginIdsValue = (($normalizedPluginIds | Select-Object -Unique) -join ",")
    $arguments += @("--plugin-ids", $pluginIdsValue)
}

& powershell -NoProfile -ExecutionPolicy Bypass -File $toolingScript @arguments
exit $LASTEXITCODE
