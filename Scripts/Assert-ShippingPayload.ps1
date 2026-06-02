param(
    [Parameter(Mandatory = $true)]
    [string]$PayloadPath
)

$resolvedPath = Resolve-Path -LiteralPath $PayloadPath -ErrorAction SilentlyContinue
if (-not $resolvedPath) {
    Write-Error "Shipping payload directory not found: $PayloadPath"
    exit 1
}

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

$violations = @()
$files = Get-ChildItem -LiteralPath $resolvedPath.Path -Recurse -File -ErrorAction Stop
foreach ($file in $files) {
    $isForbidden = $false
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
            $violations += $file
            break
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Error "Shipping payload contains test or validation tool artifacts:"
    foreach ($violation in $violations | Sort-Object FullName) {
        Write-Error " - $($violation.FullName)"
    }

    exit 1
}

Write-Host "Shipping payload validation passed: no test or validation tool artifacts found in $($resolvedPath.Path)"
