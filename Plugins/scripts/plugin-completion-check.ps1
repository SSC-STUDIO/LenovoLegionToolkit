param(
    [string[]]$PluginIds = @(),
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$OfficialOnly,
    [Alias("OutputJson")]
    [string]$JsonReportPath = ""
)

# Normalize PluginIds to handle both space-separated array and comma-separated single string
$normalizedPluginIds = @()
foreach ($id in $PluginIds) {
    if ($id -match ',') {
        $normalizedPluginIds += $id.Split(',') | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    } else {
        $normalizedPluginIds += $id
    }
}
$PluginIds = $normalizedPluginIds

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

function Normalize-TextValue {
    param(
        [object]$Value
    )

    if ($null -eq $Value) {
        return ""
    }

    return [string]$Value
}

function Compare-StringField {
    param(
        [string]$PluginId,
        [string]$FieldName,
        [object]$ManifestValue,
        [object]$StoreValue,
        [ref]$FailureCount
    )

    $manifestText = Normalize-TextValue -Value $ManifestValue
    $storeText = Normalize-TextValue -Value $StoreValue

    if ([string]::IsNullOrWhiteSpace($manifestText)) {
        Write-Step -PluginId $PluginId -Status "FAIL" -Message "plugin.json missing $FieldName"
        $FailureCount.Value++
        return
    }

    if ([string]::IsNullOrWhiteSpace($storeText)) {
        Write-Step -PluginId $PluginId -Status "FAIL" -Message "store.json missing $FieldName"
        $FailureCount.Value++
        return
    }

    if ($manifestText -ne $storeText) {
        Write-Step -PluginId $PluginId -Status "FAIL" -Message "$FieldName mismatch: plugin.json=$manifestText, store.json=$storeText"
        $FailureCount.Value++
        return
    }

    Write-Step -PluginId $PluginId -Status "PASS" -Message "$FieldName aligned ($manifestText)"
}

function Compare-BoolField {
    param(
        [string]$PluginId,
        [string]$FieldName,
        [object]$ManifestValue,
        [object]$StoreValue,
        [ref]$FailureCount
    )

    if ($null -eq $ManifestValue) {
        Write-Step -PluginId $PluginId -Status "FAIL" -Message "plugin.json missing $FieldName"
        $FailureCount.Value++
        return
    }

    if ($null -eq $StoreValue) {
        Write-Step -PluginId $PluginId -Status "FAIL" -Message "store.json missing $FieldName"
        $FailureCount.Value++
        return
    }

    $manifestBool = [bool]$ManifestValue
    $storeBool = [bool]$StoreValue
    if ($manifestBool -ne $storeBool) {
        Write-Step -PluginId $PluginId -Status "FAIL" -Message "$FieldName mismatch: plugin.json=$manifestBool, store.json=$storeBool"
        $FailureCount.Value++
        return
    }

    Write-Step -PluginId $PluginId -Status "PASS" -Message "$FieldName aligned ($manifestBool)"
}

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$pluginsRoot = Join-Path $repoRoot "Plugins"
$storePath = Join-Path $repoRoot "store.json"
$templatePluginDir = Join-Path $pluginsRoot "Template"
$officialPluginIds = @(
    "custom-mouse",
    "network-acceleration",
    "shell-integration",
    "vive-tool"
)

# Plugin ID aliases for backward compatibility (alias -> canonical id)
$pluginIdAliases = @{
    "vivetool" = "vive-tool"
}

function Resolve-PluginIdAlias {
    param([string]$PluginId)
    $canonicalId = $pluginIdAliases[$PluginId.ToLowerInvariant()]
    if ($canonicalId) {
        return $canonicalId
    }
    return $PluginId
}

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

$storePluginIds = @($storePlugins | ForEach-Object { [string]$_.id })
if ($OfficialOnly) {
    $unexpectedStoreIds = @($storePluginIds | Where-Object { $_ -notin $officialPluginIds })
    if ($unexpectedStoreIds.Count -gt 0) {
        foreach ($unexpectedStoreId in $unexpectedStoreIds) {
            Write-Step -PluginId "" -Status "FAIL" -Message "store.json contains non-official plugin entry: $unexpectedStoreId"
        }
        throw "store.json contains plugin entries outside the official plugin set."
    }

    $missingStoreIds = @($officialPluginIds | Where-Object { $_ -notin $storePluginIds })
    if ($missingStoreIds.Count -gt 0) {
        foreach ($missingStoreId in $missingStoreIds) {
            Write-Step -PluginId "" -Status "FAIL" -Message "store.json missing official plugin entry: $missingStoreId"
        }
        throw "store.json is missing one or more official plugin entries."
    }
}

$targetPluginIds = if ($PluginIds.Count -gt 0) {
    $PluginIds | ForEach-Object { Resolve-PluginIdAlias -PluginId $_ }
} elseif ($OfficialOnly) {
    $officialPluginIds
} else {
    $storePluginIds
}
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
                FileName = $manifestFile.Name
            }
        }
    } catch {
        Write-Step -PluginId "" -Status "WARN" -Message "Failed to parse manifest file: $($manifestFile.FullName)"
    }
}

if (Test-Path $templatePluginDir) {
    $templateManifestPath = Join-Path $templatePluginDir "plugin.json"
    if (Test-Path $templateManifestPath) {
        Write-Step -PluginId "" -Status "FAIL" -Message "Template plugin must not ship a plugin.json manifest: $templateManifestPath"
        throw "Template plugin must stay out of the official manifest set."
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
    $pluginFolderName = [System.IO.Path]::GetFileName($pluginDir)

    Write-Step -PluginId $pluginId -Status "PASS" -Message "Manifest found at $($manifestInfo.Path)"

    if ($manifestInfo.FileName -cne "plugin.json") {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "Manifest file must be named plugin.json"
        $pluginFailures++
    } else {
        Write-Step -PluginId $pluginId -Status "PASS" -Message "Manifest file name is plugin.json"
    }

    Compare-StringField -PluginId $pluginId -FieldName "id" -ManifestValue $manifest.id -StoreValue $storeEntry.id -FailureCount ([ref]$pluginFailures)
    Compare-StringField -PluginId $pluginId -FieldName "name" -ManifestValue $manifest.name -StoreValue $storeEntry.name -FailureCount ([ref]$pluginFailures)
    Compare-StringField -PluginId $pluginId -FieldName "version" -ManifestValue $manifest.version -StoreValue $storeEntry.version -FailureCount ([ref]$pluginFailures)
    Compare-StringField -PluginId $pluginId -FieldName "author" -ManifestValue $manifest.author -StoreValue $storeEntry.author -FailureCount ([ref]$pluginFailures)
    Compare-StringField -PluginId $pluginId -FieldName "minLLTVersion" -ManifestValue $manifest.minLLTVersion -StoreValue $storeEntry.minLLTVersion -FailureCount ([ref]$pluginFailures)
    Compare-BoolField -PluginId $pluginId -FieldName "isSystemPlugin" -ManifestValue $manifest.isSystemPlugin -StoreValue $storeEntry.isSystemPlugin -FailureCount ([ref]$pluginFailures)

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

    if ($projectFile.BaseName -ne "LenovoLegionToolkit.Plugins.$pluginFolderName") {
        Write-Step -PluginId $pluginId -Status "FAIL" -Message "Project file name should be LenovoLegionToolkit.Plugins.$pluginFolderName.csproj"
        $pluginFailures++
    } else {
        Write-Step -PluginId $pluginId -Status "PASS" -Message "Project file naming aligned ($($projectFile.Name))"
    }

    $projectXml = [xml](Get-Content $projectFile.FullName)
    $projectVersion = Get-FirstNonEmptyNode -ProjectXml $projectXml -NodeName "Version"
    $projectAuthors = Get-FirstNonEmptyNode -ProjectXml $projectXml -NodeName "Authors"
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

    if (-not [string]::IsNullOrWhiteSpace($projectAuthors)) {
        if ($projectAuthors -ne $manifest.author) {
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Author mismatch: csproj=$projectAuthors, plugin.json=$($manifest.author)"
            $pluginFailures++
        } else {
            Write-Step -PluginId $pluginId -Status "PASS" -Message "csproj author aligned ($projectAuthors)"
        }
    } else {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "No explicit <Authors> in csproj (using shared default)."
        $pluginWarnings++
    }

    $attributePattern = [regex]::Escape("id: `"$pluginId`"")
    $pluginSourceFiles = Get-ChildItem -Path $pluginDir -Filter "*.cs" -File
    $attributeSourceFile = $pluginSourceFiles |
        Where-Object { Select-String -Path $_.FullName -Pattern $attributePattern -Quiet } |
        Select-Object -First 1

    if ($attributeSourceFile) {
        $sourceText = Get-Content $attributeSourceFile.FullName -Raw
        $attributeFailures = 0

        foreach ($field in @(
            @{ Pattern = "name: `"$([regex]::Escape([string]$manifest.name))`""; Label = "name" },
            @{ Pattern = "version: `"$([regex]::Escape([string]$manifest.version))`""; Label = "version" },
            @{ Pattern = "author: `"$([regex]::Escape([string]$manifest.author))`""; Label = "author" },
            @{ Pattern = "MinimumHostVersion\s*=\s*`"$([regex]::Escape([string]$manifest.minLLTVersion))`""; Label = "MinimumHostVersion" }
        )) {
            if ($sourceText -notmatch $field.Pattern) {
                Write-Step -PluginId $pluginId -Status "WARN" -Message "Plugin attribute may be out of sync for $($field.Label): $($attributeSourceFile.Name)"
                $pluginWarnings++
                $attributeFailures++
            }
        }

        if ($attributeFailures -eq 0) {
            Write-Step -PluginId $pluginId -Status "PASS" -Message "Plugin attribute metadata aligned ($($attributeSourceFile.Name))"
        }
    } else {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "Plugin attribute source not found for manifest id"
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
        $forbiddenOutputs = @(
            "*.deps.json",
            "*.runtimeconfig.json",
            "Lenovo Legion Toolkit.*",
            "LenovoLegionToolkit.WPF.*",
            "LenovoLegionToolkit.Lib.*"
        )

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

        $forbiddenMatches = @()
        foreach ($forbiddenPattern in $forbiddenOutputs) {
            $forbiddenMatches += @(Get-ChildItem -Path $resolvedOutputPath -Filter $forbiddenPattern -File -ErrorAction SilentlyContinue)
        }

        if ($forbiddenMatches.Count -gt 0) {
            $forbiddenNames = $forbiddenMatches | Select-Object -ExpandProperty Name | Sort-Object -Unique
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Forbidden output files present: $($forbiddenNames -join ', ')"
            $pluginFailures++
        } else {
            Write-Step -PluginId $pluginId -Status "PASS" -Message "Output directory cleaned for release packaging"
        }
    } else {
        Write-Step -PluginId $pluginId -Status "WARN" -Message "Output artifact checks skipped because build is skipped"
        $pluginWarnings++
    }

    $testProjectDirectory = Join-Path $pluginsRoot "$pluginFolderName.Tests"
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
        if ($OfficialOnly) {
            Write-Step -PluginId $pluginId -Status "FAIL" -Message "Missing sibling *.Tests project"
            $pluginFailures++
        } else {
            Write-Step -PluginId $pluginId -Status "WARN" -Message "Missing sibling *.Tests project"
            $pluginWarnings++
        }
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
        officialOnly = [bool]$OfficialOnly
        officialPluginIds = @($officialPluginIds)
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
