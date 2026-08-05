param(
    [string]$PluginsRepositoryRoot = "",
    [string]$HostSourceDir = "",
    [Parameter(Mandatory = $true)]
    [string]$DestinationPath,
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

function Resolve-PluginsRepositoryRoot {
    param([string]$ExplicitRoot)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        $resolved = Resolve-Path -LiteralPath $ExplicitRoot -ErrorAction Stop
        return $resolved.Path.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
    }

    $coreRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $candidates = @(
        (Join-Path $coreRoot "Plugins")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath (Join-Path $candidate "UniversalDeviceToolkit.Plugins.sln")) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }

    throw "Plugin repository not found. Pass -PluginsRepositoryRoot explicitly."
}

function Resolve-HostSourceDirectory {
    param(
        [string]$ExplicitSource,
        [string]$CoreRepositoryRoot
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitSource)) {
        return (Resolve-Path -LiteralPath $ExplicitSource -ErrorAction Stop).Path
    }

    $buildOutput = Join-Path $CoreRepositoryRoot "Build"
    if (Test-Path -LiteralPath (Join-Path $buildOutput "UniversalDeviceToolkit.Lib.dll")) {
        return $buildOutput
    }

    throw "Host build output not found. Publish the host first or pass -HostSourceDir."
}

$coreRepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$pluginsRoot = Resolve-PluginsRepositoryRoot -ExplicitRoot $PluginsRepositoryRoot
$hostSource = Resolve-HostSourceDirectory -ExplicitSource $HostSourceDir -CoreRepositoryRoot $coreRepositoryRoot
$destinationRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DestinationPath)

if (-not (Test-Path -LiteralPath $destinationRoot)) {
    New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
}

$ensureHostDependencies = Join-Path $pluginsRoot "Scripts\ensure-host-dependencies.ps1"
if (-not (Test-Path -LiteralPath $ensureHostDependencies)) {
    throw "Missing plugin tooling script: $ensureHostDependencies"
}

Write-Host "Refreshing plugin host dependencies from $hostSource"
& $ensureHostDependencies -SourceDir $hostSource -ForceRefresh
if ($LASTEXITCODE -ne 0) {
    throw "ensure-host-dependencies.ps1 failed with exit code $LASTEXITCODE."
}

$sdkProject = Join-Path $pluginsRoot "SDK\Runtime\UniversalDeviceToolkit.Plugins.SDK.csproj"
$sharedProject = Join-Path $pluginsRoot "Shared\UniversalDeviceToolkit.Plugins.Shared.csproj"

Write-Host "Restoring plugin SDK and Shared"
# The monorepo root owns the central package versions. Keep CPM enabled so the
# migrated SDK and Shared projects cannot fall back to unversioned NuGet defaults.
dotnet restore $sdkProject -p:ManagePackageVersionsCentrally=true
if ($LASTEXITCODE -ne 0) { throw "Plugin SDK restore failed." }

dotnet restore $sharedProject -p:ManagePackageVersionsCentrally=true
if ($LASTEXITCODE -ne 0) { throw "Plugin Shared restore failed." }

Write-Host "Building plugin SDK and Shared ($Configuration)"
dotnet build $sdkProject -c $Configuration --no-restore -p:ManagePackageVersionsCentrally=true
if ($LASTEXITCODE -ne 0) { throw "Plugin SDK build failed." }

dotnet build $sharedProject -c $Configuration --no-restore -p:ManagePackageVersionsCentrally=true
if ($LASTEXITCODE -ne 0) { throw "Plugin Shared build failed." }

$runtimeFiles = @(
    @{
        Source = Join-Path $pluginsRoot ".build\shared\UniversalDeviceToolkit.Plugins.Shared.dll"
        Name = "UniversalDeviceToolkit.Plugins.Shared.dll"
    },
    @{
        Source = Join-Path $pluginsRoot ".build\sdk\UniversalDeviceToolkit.Plugins.SDK.dll"
        Name = "UniversalDeviceToolkit.Plugins.SDK.dll"
    }
)

foreach ($runtimeFile in $runtimeFiles) {
    if (-not (Test-Path -LiteralPath $runtimeFile.Source)) {
        throw "Required plugin runtime file was not produced: $($runtimeFile.Source)"
    }

    $targetPath = Join-Path $destinationRoot $runtimeFile.Name
    Copy-Item -LiteralPath $runtimeFile.Source -Destination $targetPath -Force
    Write-Host "Copied $($runtimeFile.Name) -> $targetPath"
}

Write-Host "Plugin runtime assets are ready in $destinationRoot"
