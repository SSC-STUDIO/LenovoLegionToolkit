param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("settings-only", "feature-settings", "runtime-optimization")]
    [string]$Template,
    [Parameter(Mandatory = $true)]
    [string]$FolderName,
    [Parameter(Mandatory = $true)]
    [string]$PluginId,
    [Parameter(Mandatory = $true)]
    [string]$DisplayName,
    [string]$Author = $env:USERNAME,
    [string]$Description = "",
    [string]$MinimumHostVersion = "3.6.14",
    [switch]$Official
)

$ErrorActionPreference = "Stop"

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$cliProject = Join-Path $repoRoot "Tools\PluginTooling.Cli\PluginTooling.Cli.csproj"
$dotnet = if ($env:DOTNET_HOST_PATH) { $env:DOTNET_HOST_PATH } else { "dotnet" }

$arguments = @(
    "run",
    "--project", $cliProject,
    "--",
    "new",
    "--repository-root", $repoRoot,
    "--template", $Template,
    "--folder", $FolderName,
    "--id", $PluginId,
    "--name", $DisplayName,
    "--author", $Author,
    "--min-llt-version", $MinimumHostVersion
)

if ($Description) {
    $arguments += @("--description", $Description)
}

if ($Official) {
    $arguments += "--official"
}

& $dotnet @arguments
exit $LASTEXITCODE
