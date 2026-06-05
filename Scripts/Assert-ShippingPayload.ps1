param(
    [Parameter(Mandatory = $true)]
    [string]$PayloadPath
)

$ErrorActionPreference = 'Stop'

$resolvedPath = Resolve-Path -LiteralPath $PayloadPath -ErrorAction SilentlyContinue
if (-not $resolvedPath) {
    Write-Error "Shipping payload directory not found: $PayloadPath"
    exit 1
}

$pathTrimChars = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
$payloadRoot = $resolvedPath.Path.TrimEnd($pathTrimChars)

$forbiddenExactNames = @(
    'SpectrumTester.exe',
    'SpectrumTester.dll',
    'SpectrumTester.deps.json',
    'SpectrumTester.runtimeconfig.json'
)

$forbiddenNamePrefixes = @(
    'UniversalDeviceToolkit.Tests',
    'UniversalDeviceToolkit.CrossPlatform.Tests',
    'UniversalDeviceToolkit.PerformanceTest',
    'MainAppPluginUi.Smoke',
    'LanguagePackUi.Smoke',
    'LanguagePackInstallProgressSmoke',
    'VisualRegression.Smoke',
    'HardwareValidation',
    'PresetUiValidation',
    'SensorInventoryDump',
    'testhost',
    'xunit.'
)

$forbiddenPathSegments = @(
    'Tools',
    'Tests'
)

$forbiddenNamePatterns = @(
    '*.Tests.*',
    '*.Smoke.*',
    '*Validation*',
    '*TestHost*'
)

$forbiddenBinaryMarkers = @(
    'UDT_APPDATA_OVERRIDE'
)

$violations = @()
$files = Get-ChildItem -LiteralPath $resolvedPath.Path -Recurse -File -ErrorAction Stop
foreach ($file in $files) {
    $isForbidden = $false

    $relativePath = $file.FullName
    if ($relativePath.StartsWith($payloadRoot, [StringComparison]::OrdinalIgnoreCase)) {
        $relativePath = $relativePath.Substring($payloadRoot.Length).TrimStart($pathTrimChars)
    }

    $pathSegments = $relativePath -split '[\\/]'
    foreach ($segment in $pathSegments) {
        foreach ($forbiddenSegment in $forbiddenPathSegments) {
            if ([string]::Equals($segment, $forbiddenSegment, [StringComparison]::OrdinalIgnoreCase)) {
                $isForbidden = $true
                break
            }
        }

        if ($isForbidden) { break }
    }

    if ($isForbidden) {
        $violations += $file
        continue
    }

    foreach ($forbiddenName in $forbiddenExactNames) {
        if ([string]::Equals($file.Name, $forbiddenName, [StringComparison]::OrdinalIgnoreCase)) {
            $isForbidden = $true
            break
        }
    }

    if ($isForbidden) {
        $violations += $file
        continue
    }

    foreach ($prefix in $forbiddenNamePrefixes) {
        if ($file.Name.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $isForbidden = $true
            $violations += $file
            break
        }
    }

    if ($isForbidden) {
        continue
    }

    foreach ($pattern in $forbiddenNamePatterns) {
        if ($file.Name -like $pattern) {
            $isForbidden = $true
            $violations += $file
            break
        }
    }

    if (-not $isForbidden) {
        foreach ($marker in $forbiddenBinaryMarkers) {
            if (Select-String -LiteralPath $file.FullName -SimpleMatch $marker -Quiet) {
                $violations += $file
                break
            }
        }
    }
}

if ($violations.Count -gt 0) {
    [Console]::Error.WriteLine("Shipping payload contains test or validation tool artifacts:")
    foreach ($violation in $violations | Sort-Object FullName) {
        [Console]::Error.WriteLine(" - $($violation.FullName)")
    }

    exit 1
}

Write-Host "Shipping payload validation passed: no test or validation tool artifacts found in $($resolvedPath.Path)"
