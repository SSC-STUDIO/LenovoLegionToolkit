param(
    [string[]]$PluginIds = @(),
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [string]$JsonReportPath = ""
)

$ErrorActionPreference = "Stop"
$script:StepLogs = New-Object System.Collections.Generic.List[object]

function Write-Step {
    param(
        [string]$PluginId,
        [string]$Status,
        [string]$Message
    )

    $prefix = if ([string]::IsNullOrWhiteSpace($PluginId)) { "[global]" } else { "[$PluginId]" }
    $line = "$prefix [$Status] $Message"
    if ($Status -eq "FAIL") {
        Write-Host $line -ForegroundColor Red
    } elseif ($Status -eq "WARN") {
        Write-Host $line -ForegroundColor Yellow
    } else {
        Write-Host $line -ForegroundColor Green
    }

    $script:StepLogs.Add([pscustomobject]@{
        Timestamp = (Get-Date).ToString("o")
        PluginId = $PluginId
        Status = $Status
        Message = $Message
    }) | Out-Null
}

function Resolve-OutputPath {
    param(
        [xml]$ProjectXml,
        [string]$ProjectDirectory,
        [string]$BuildConfiguration
    )

    $outputPath = $null

    foreach ($propertyGroup in $ProjectXml.Project.PropertyGroup) {
        if (-not $propertyGroup.OutputPath) {
            continue
        }

        if (-not $propertyGroup.Condition) {
            if (-not $outputPath) {
                $outputPath = [string]$propertyGroup.OutputPath
            }
            continue
        }

        $conditionText = [string]$propertyGroup.Condition
        if ($conditionText.Contains("'`$(Configuration)' == '$BuildConfiguration'")) {
            return [System.IO.Path]::GetFullPath((Join-Path $ProjectDirectory ([string]$propertyGroup.OutputPath)))
        }
    }

    if ($outputPath) {
        return [System.IO.Path]::GetFullPath((Join-Path $ProjectDirectory $outputPath))
    }

    # Fallback to SDK default layout when OutputPath is not explicitly set.
    return [System.IO.Path]::GetFullPath((Join-Path $ProjectDirectory "bin\$BuildConfiguration"))
}

function Get-FirstNonEmptyNode {
    param(
        [xml]$ProjectXml,
        [string]$NodeName
    )

    foreach ($propertyGroup in $ProjectXml.Project.PropertyGroup) {
        $value = [string]$propertyGroup.$NodeName
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }
    }

    return $null
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$pluginsRoot = Join-Path $repoRoot "Plugins"
$storePath = Join-Path $repoRoot "store.json"

if (-not (Test-Path $storePath)) {
    throw "store.json not found at $storePath"
}

$hostDependencyPath = Join-Path $repoRoot "Dependencies\Host\LenovoLegionToolkit.Lib.dll"
if (-not (Test-Path $hostDependencyPath)) {
    throw "Missing host dependency: $hostDependencyPath. Run scripts/refresh-host-references.ps1 first."
}

$allProjectFiles = Get-ChildItem -Path $pluginsRoot -Recurse -Filter "*.csproj" -File
$blockedReferenceFound = $false
foreach ($projectFile in $allProjectFiles) {
    if (Select-String -Path $projectFile.FullName -Pattern "..\\..\\..\\LenovoLegionToolkit" -SimpleMatch -Quiet) {
        Write-Step -PluginId "" -Status "FAIL" -Message "Project has forbidden source dependency path: $($projectFile.FullName)"
        $blockedReferenceFound = $true
    }
}

if ($blockedReferenceFound) {
    throw "Found forbidden source dependency references to sibling LenovoLegionToolkit repository."
}

$store = Get-Content $storePath -Raw | ConvertFrom-Json
$storePlugins = @($store.plugins)
if ($storePlugins.Count -eq 0) {
    throw "No plugin entries found in store.json"
}

$targetPluginIds = if ($PluginIds.Count -gt 0) { $PluginIds } else { $storePlugins.id }
$manifestFiles = Get-ChildItem -Path $pluginsRoot -Recurse -File | Where-Object { $_.Name -ieq "plugin.json" }

$manifestById = @{}
foreach ($manifestFile in $manifestFiles) {
    try {
        $manifest = Get-Content $manifestFile.FullName -Raw | ConvertFrom-Json
        if ($manifest.id -and -not $manifestById.ContainsKey($manifest.id)) {
            $manifestById[$manifest.id] = @{
                Manifest = $manifest
                Path = $manifestFile.FullName
                Directory = $manifestFile.DirectoryName
            }
        }
    } catch {
        Write-Step -PluginId "" -Status "WARN" -Message "Failed to parse manifest file: $($manifestFile.FullName)"
    }
}

$results = New-Object System.Collections.Generic.List[object]
$globalFailures = 0
$globalWarnings = 0

foreach ($pluginId in $targetPluginIds) {
    $pluginFailures = 0
    $pluginWarnings = 0

    $storeEntry = $storePlugins | Where-Object { $_.id -eq $pluginId } | Select-Object -First 1
    if (-not $storeEntry) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "Plugin not found in store.json"
        $globalFailures++
        continue
    }

    if (-not $manifestById.ContainsKey($pluginId)) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "plugin.json not found in Plugins/* for id '$pluginId'"
        $globalFailures++
        continue
    }

    $manifestInfo = $manifestById[$pluginId]
    $manifest = $manifestInfo.Manifest
    $pluginDir = $manifestInfo.Directory

    Write-Step -PluginId $pluginId -Status "PASS" -Message "Manifest found at $($manifestInfo.Path)"

    if (-not $manifest.version) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "plugin.json missing version"
        $pluginFailures++
    }

    if ($manifest.version -ne $storeEntry.version) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "Version mismatch: plugin.json=$($manifest.version), store.json=$($storeEntry.version)"
        $pluginFailures++
    } else {
        Write-Step -PluginId $pluginId -Status "PASS" -Message "Version aligned ($($manifest.version))"
    }

    if (-not $manifest.minLLTVersion) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "plugin.json missing minLLTVersion"
        $pluginFailures++
    } elseif ($manifest.minLLTVersion -ne $storeEntry.minLLTVersion) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "minLLTVersion mismatch: plugin.json=$($manifest.minLLTVersion), store.json=$($storeEntry.minLLTVersion)"
        $pluginFailures++
    } else {
        Write-Step -PluginId $pluginId -Status "PASS" -Message "minLLTVersion aligned ($($manifest.minLLTVersion))"
    }

    if ($manifest.version -and ($manifest.version -notmatch '^\d+\.\d+\.\d+([\-+][0-9A-Za-z\.-]+)?$')) {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "Version is not SemVer-like: $($manifest.version)"
        $pluginWarnings++
    }

    $projectFile = Get-ChildItem -Path $pluginDir -Filter "*.csproj" -File | Select-Object -First 1
    if (-not $projectFile) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "No .csproj file found in plugin directory"
        $pluginFailures++
        $globalFailures += $pluginFailures
        continue
    }

    $projectXml = [xml](Get-Content $projectFile.FullName)
    $projectVersion = Get-FirstNonEmptyNode -ProjectXml $projectXml -NodeName "Version"
    $assemblyName = Get-FirstNonEmptyNode -ProjectXml $projectXml -NodeName "AssemblyName"
    if (-not $assemblyName) {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile.Name)
    }

    if ($projectVersion) {
        if ($projectVersion -ne $manifest.version) {
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Version mismatch: csproj=$projectVersion, plugin.json=$($manifest.version)"
            $pluginFailures++
        } else {
            Write-Step -PluginId $pluginId -Status "PASS" -Message "csproj version aligned ($projectVersion)"
        }
    } else {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "No explicit <Version> in csproj (could be inherited)."
        $pluginWarnings++
    }

    $changelogPath = Join-Path $pluginDir "CHANGELOG.md"
    if (-not (Test-Path $changelogPath)) {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "Missing plugin CHANGELOG.md"
        $pluginFailures++
    } else {
        Write-Step -PluginId $pluginId -Status "PASS" -Message "CHANGELOG.md present"
    }

    if (-not $SkipBuild) {
        Write-Step -PluginId $pluginId -Status "PASS" -Message "Building $($projectFile.Name) ($Configuration)"
        & dotnet build $projectFile.FullName -c $Configuration --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Build failed"
            $pluginFailures++
        }
    } else {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "Build skipped by parameter"
        $pluginWarnings++
    }

    if (-not $SkipBuild) {
        $resolvedOutputPath = Resolve-OutputPath -ProjectXml $projectXml -ProjectDirectory $pluginDir -BuildConfiguration $Configuration
        $expectedDll = Join-Path $resolvedOutputPath "$assemblyName.dll"
        $outputManifest = Join-Path $resolvedOutputPath "plugin.json"

        if (-not (Test-Path $expectedDll)) {
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Missing output DLL: $expectedDll"
            $pluginFailures++
        } else {
            Write-Step -PluginId $pluginId -Status "PASS" -Message "Output DLL present ($assemblyName.dll)"
        }

        if (-not (Test-Path $outputManifest)) {
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Missing output plugin.json: $outputManifest"
            $pluginFailures++
        } else {
            Write-Step -PluginId $pluginId -Status "PASS" -Message "Output plugin.json present"
        }
    } else {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "Output artifact checks skipped because build is skipped"
        $pluginWarnings++
    }

    $testProjectDirectory = Join-Path $pluginsRoot "$([System.IO.Path]::GetFileName($pluginDir)).Tests"
    $testProjectFile = if (Test-Path $testProjectDirectory) {
        Get-ChildItem -Path $testProjectDirectory -Filter "*.csproj" -File | Select-Object -First 1
    } else {
        $null
    }

    if (-not $SkipTests -and $testProjectFile) {
        Write-Step -PluginId $pluginId -Status "PASS" -Message "Running tests: $($testProjectFile.Name)"
        & dotnet test $testProjectFile.FullName -c $Configuration --nologo
        if ($LASTEXITCODE -ne 0) {
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Tests failed"
            $pluginFailures++
        }
    } elseif ($testProjectFile) {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "Tests skipped by parameter"
        $pluginWarnings++
    } else {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "No sibling *.Tests project found (optional)"
        $pluginWarnings++
    }

    $results.Add([pscustomobject]@{
        PluginId = $pluginId
        Failures = $pluginFailures
        Warnings = $pluginWarnings
        Status = if ($pluginFailures -eq 0) { "PASS" } else { "FAIL" }
    }) | Out-Null

    $globalFailures += $pluginFailures
    $globalWarnings += $pluginWarnings
}

Write-Host ""
Write-Host "=== Plugin Completion Check Summary ===" -ForegroundColor Cyan
$results | Sort-Object PluginId | Format-Table -AutoSize
Write-Host "Total plugins checked: $($results.Count)"
Write-Host "Total failures: $globalFailures"
Write-Host "Total warnings: $globalWarnings"

if (-not [string]::IsNullOrWhiteSpace($JsonReportPath)) {
    $resolvedReportPath = if ([System.IO.Path]::IsPathRooted($JsonReportPath)) {
        $JsonReportPath
    } else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $JsonReportPath))
    }

    $reportDirectory = [System.IO.Path]::GetDirectoryName($resolvedReportPath)
    if (-not [string]::IsNullOrWhiteSpace($reportDirectory)) {
        New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
    }

    $pluginResults = @(foreach ($result in $results) { $result }) | Sort-Object PluginId
    $stepResults = @(foreach ($step in $script:StepLogs) { $step })

    $report = [pscustomobject]@{
        generatedAt = (Get-Date).ToString("o")
        repositoryRoot = $repoRoot
        configuration = $Configuration
        skipBuild = [bool]$SkipBuild
        skipTests = [bool]$SkipTests
        pluginIds = @($targetPluginIds)
        totals = [pscustomobject]@{
            pluginCount = $results.Count
            failures = $globalFailures
            warnings = $globalWarnings
        }
        plugins = $pluginResults
        steps = $stepResults
    }

    $report | ConvertTo-Json -Depth 12 | Set-Content -Path $resolvedReportPath -Encoding UTF8
    Write-Host "JSON report written to: $resolvedReportPath" -ForegroundColor Cyan
}

if ($globalFailures -gt 0) {
    exit 1
}

exit 0
