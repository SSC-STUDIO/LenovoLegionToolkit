# Plugin Test Runner
# This script tests all plugins to ensure they are working correctly

param(
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"
$totalTests = 0
$passedTests = 0
$failedTests = 0

function Write-TestResult($name, $result, $message = "") {
    $totalTests++
    if ($result) {
        $passedTests++
        Write-Host "✅ PASS: $name" -ForegroundColor Green
    } else {
        $failedTests++
        Write-Host "❌ FAIL: $name" -ForegroundColor Red
        if ($message) {
            Write-Host "   $message" -ForegroundColor Red
        }
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LenovoLegionToolkit Plugins Test Runner" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: SDK Build
Write-Host "Testing SDK Build..." -ForegroundColor Yellow
try {
    $sdkBuild = dotnet build "SDK\SDK.csproj" -c Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-TestResult "SDK Build" $true
    } else {
        Write-TestResult "SDK Build" $false $sdkBuild
    }
} catch {
    Write-TestResult "SDK Build" $false $_.Exception.Message
}

# Test 2: CustomMouse Plugin Build
Write-Host "`nTesting CustomMouse Plugin Build..." -ForegroundColor Yellow
try {
    $mouseBuild = dotnet build "plugins\CustomMouse\CustomMouse.csproj" -c Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-TestResult "CustomMouse Build" $true
    } else {
        Write-TestResult "CustomMouse Build" $false $mouseBuild
    }
} catch {
    Write-TestResult "CustomMouse Build" $false $_.Exception.Message
}

# Test 3: ShellIntegration Plugin Build
Write-Host "`nTesting ShellIntegration Plugin Build..." -ForegroundColor Yellow
try {
    $shellBuild = dotnet build "plugins\ShellIntegration\ShellIntegration.csproj" -c Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-TestResult "ShellIntegration Build" $true
    } else {
        Write-TestResult "ShellIntegration Build" $false $shellBuild
    }
} catch {
    Write-TestResult "ShellIntegration Build" $false $_.Exception.Message
}

# Test 4: CustomMouse Unit Tests
Write-Host "`nTesting CustomMouse Unit Tests..." -ForegroundColor Yellow
try {
    $testResults = dotnet test "plugins\CustomMouse.Tests\CustomMouse.Tests.csproj" --no-build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-TestResult "CustomMouse Unit Tests" $true
    } else {
        Write-TestResult "CustomMouse Unit Tests" $false $testResults
    }
} catch {
    Write-TestResult "CustomMouse Unit Tests" $false $_.Exception.Message
}

# Test 5: Plugin Manifest Validation
Write-Host "`nTesting Plugin Manifests..." -ForegroundColor Yellow

$plugins = @(
    @{Name="CustomMouse"; Path="plugins\CustomMouse\plugin.json"},
    @{Name="ShellIntegration"; Path="plugins\ShellIntegration\plugin.json"}
)

foreach ($plugin in $plugins) {
    try {
        if (Test-Path $plugin.Path) {
            $manifest = Get-Content $plugin.Path | ConvertFrom-Json
            $valid = $true
            $errors = @()
            
            if (-not $manifest.id) { $valid = $false; $errors += "Missing 'id'" }
            if (-not $manifest.name) { $valid = $false; $errors += "Missing 'name'" }
            if (-not $manifest.version) { $valid = $false; $errors += "Missing 'version'" }
            if (-not $manifest.author) { $valid = $false; $errors += "Missing 'author'" }
            if (-not $manifest.entryPoint) { $valid = $false; $errors += "Missing 'entryPoint'" }
            
            if ($valid) {
                Write-TestResult "$($plugin.Name) Manifest" $true
            } else {
                Write-TestResult "$($plugin.Name) Manifest" $false ($errors -join ", ")
            }
        } else {
            Write-TestResult "$($plugin.Name) Manifest" $false "File not found"
        }
    } catch {
        Write-TestResult "$($plugin.Name) Manifest" $false $_.Exception.Message
    }
}

# Test 6: Solution Build
Write-Host "`nTesting Full Solution Build..." -ForegroundColor Yellow
try {
    $solutionBuild = dotnet build "LenovoLegionToolkit-Plugins.sln" -c Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-TestResult "Full Solution Build" $true
    } else {
        Write-TestResult "Full Solution Build" $false $solutionBuild
    }
} catch {
    Write-TestResult "Full Solution Build" $false $_.Exception.Message
}

# Test 7: Plugin DLL Generation
Write-Host "`nTesting Plugin DLL Generation..." -ForegroundColor Yellow

$pluginDlls = @(
    @{Name="CustomMouse"; Path="plugins\CustomMouse\bin\Release\net10.0\LenovoLegionToolkit.Plugins.CustomMouse.dll"},
    @{Name="ShellIntegration"; Path="plugins\ShellIntegration\bin\Release\net10.0\LenovoLegionToolkit.Plugins.ShellIntegration.dll"}
)

foreach ($dll in $pluginDlls) {
    if (Test-Path $dll.Path) {
        $fileInfo = Get-Item $dll.Path
        Write-TestResult "$($dll.Name) DLL Generated" $true "Size: $([math]::Round($fileInfo.Length/1KB, 2)) KB"
    } else {
        Write-TestResult "$($dll.Name) DLL Generated" $false "File not found"
    }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passedTests/$totalTests)*100, 2))%" -ForegroundColor Yellow

if ($failedTests -eq 0) {
    Write-Host "`n✅ All plugins are working correctly!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ Some tests failed. Please review the errors above." -ForegroundColor Red
    exit 1
}
