# Lenovo Legion Toolkit 完整测试套件
# 运行所有自动化测试

param(
    [switch]$SkipUnitTests = $false,
    [switch]$SkipIntegrationTests = $false,
    [switch]$SkipAutomationTests = $false,
    [switch]$SkipPerformanceTests = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Lenovo Legion Toolkit - 完整测试套件                  ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath

# 切换到项目根目录
Push-Location $rootPath

$testResults = @{
    UnitTests = $null
    IntegrationTests = $null
    AutomationTests = $null
    PerformanceTests = $null
}

try {
    # 1. 运行单元测试
    if (-not $SkipUnitTests) {
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "运行单元测试..." -ForegroundColor Yellow
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        
        try {
            $unitTestOutput = dotnet test LenovoLegionToolkit.Tests\LenovoLegionToolkit.Tests.csproj --verbosity minimal 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ 单元测试通过" -ForegroundColor Green
                $testResults.UnitTests = $true
            } else {
                Write-Host "✗ 单元测试失败" -ForegroundColor Red
                if ($Verbose) {
                    Write-Host $unitTestOutput
                }
                $testResults.UnitTests = $false
            }
        } catch {
            Write-Host "✗ 单元测试执行出错: $_" -ForegroundColor Red
            $testResults.UnitTests = $false
        }
        Write-Host ""
    } else {
        Write-Host "⊘ 跳过单元测试" -ForegroundColor Yellow
        Write-Host ""
    }

    # 2. 运行集成测试
    if (-not $SkipIntegrationTests) {
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "运行集成测试..." -ForegroundColor Yellow
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        
        $integrationTestScript = Join-Path $scriptPath "integration_test.ps1"
        if (Test-Path $integrationTestScript) {
            try {
                & $integrationTestScript
                if ($LASTEXITCODE -eq 0) {
                    $testResults.IntegrationTests = $true
                } else {
                    $testResults.IntegrationTests = $false
                }
            } catch {
                Write-Host "✗ 集成测试执行出错: $_" -ForegroundColor Red
                $testResults.IntegrationTests = $false
            }
        } else {
            Write-Host "⚠ 集成测试脚本不存在: $integrationTestScript" -ForegroundColor Yellow
            $testResults.IntegrationTests = $null
        }
        Write-Host ""
    } else {
        Write-Host "⊘ 跳过集成测试" -ForegroundColor Yellow
        Write-Host ""
    }

    # 3. 运行自动化测试
    if (-not $SkipAutomationTests) {
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "运行自动化测试..." -ForegroundColor Yellow
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        
        $automationTestScript = Join-Path $scriptPath "automation_test.ps1"
        if (Test-Path $automationTestScript) {
            try {
                & $automationTestScript
                if ($LASTEXITCODE -eq 0) {
                    $testResults.AutomationTests = $true
                } else {
                    $testResults.AutomationTests = $false
                }
            } catch {
                Write-Host "✗ 自动化测试执行出错: $_" -ForegroundColor Red
                $testResults.AutomationTests = $false
            }
        } else {
            Write-Host "⚠ 自动化测试脚本不存在: $automationTestScript" -ForegroundColor Yellow
            $testResults.AutomationTests = $null
        }
        Write-Host ""
    } else {
        Write-Host "⊘ 跳过自动化测试" -ForegroundColor Yellow
        Write-Host ""
    }

    # 4. 运行性能测试
    if (-not $SkipPerformanceTests) {
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        Write-Host "运行性能测试..." -ForegroundColor Yellow
        Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
        
        $performanceTestScript = Join-Path $scriptPath "performance_test.ps1"
        if (Test-Path $performanceTestScript) {
            try {
                & $performanceTestScript -Detailed:$Verbose
                if ($LASTEXITCODE -eq 0) {
                    $testResults.PerformanceTests = $true
                } else {
                    $testResults.PerformanceTests = $false
                }
            } catch {
                Write-Host "✗ 性能测试执行出错: $_" -ForegroundColor Red
                $testResults.PerformanceTests = $false
            }
        } else {
            Write-Host "⚠ 性能测试脚本不存在: $performanceTestScript" -ForegroundColor Yellow
            $testResults.PerformanceTests = $null
        }
        Write-Host ""
    } else {
        Write-Host "⊘ 跳过性能测试" -ForegroundColor Yellow
        Write-Host ""
    }

    # 输出最终结果
    Write-Host "╔══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║  测试结果摘要                                             ║" -ForegroundColor Cyan
    Write-Host "╚══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    
    $allPassed = $true
    $anyRun = $false

    if ($testResults.UnitTests -ne $null) {
        $anyRun = $true
        if ($testResults.UnitTests) {
            Write-Host "单元测试:   ✓ 通过" -ForegroundColor Green
        } else {
            Write-Host "单元测试:   ✗ 失败" -ForegroundColor Red
            $allPassed = $false
        }
    }

    if ($testResults.IntegrationTests -ne $null) {
        $anyRun = $true
        if ($testResults.IntegrationTests) {
            Write-Host "集成测试:   ✓ 通过" -ForegroundColor Green
        } else {
            Write-Host "集成测试:   ✗ 失败" -ForegroundColor Red
            $allPassed = $false
        }
    }

    if ($testResults.AutomationTests -ne $null) {
        $anyRun = $true
        if ($testResults.AutomationTests) {
            Write-Host "自动化测试: ✓ 通过" -ForegroundColor Green
        } else {
            Write-Host "自动化测试: ✗ 失败" -ForegroundColor Red
            $allPassed = $false
        }
    }

    if ($testResults.PerformanceTests -ne $null) {
        $anyRun = $true
        if ($testResults.PerformanceTests) {
            Write-Host "性能测试:   ✓ 通过" -ForegroundColor Green
        } else {
            Write-Host "性能测试:   ✗ 失败" -ForegroundColor Red
            $allPassed = $false
        }
    }

    Write-Host ""

    if (-not $anyRun) {
        Write-Host "⚠ 没有运行任何测试" -ForegroundColor Yellow
        exit 0
    }

    if ($allPassed) {
        Write-Host "✓ 所有测试通过！" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "✗ 部分测试失败！" -ForegroundColor Red
        exit 1
    }

} finally {
    Pop-Location
}

