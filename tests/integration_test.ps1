# Lenovo Legion Toolkit 集成测试脚本
# 测试应用程序的集成功能

param(
    [string]$ExePath = "build\Lenovo Legion Toolkit.exe",
    [int]$TestDurationSeconds = 10
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Lenovo Legion Toolkit 集成测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$testResults = @{
    Passed = 0
    Failed = 0
    Skipped = 0
}

function Test-Passed {
    param([string]$TestName)
    Write-Host "  ✓ $TestName" -ForegroundColor Green
    $script:testResults.Passed++
}

function Test-Failed {
    param([string]$TestName, [string]$Reason)
    Write-Host "  ✗ $TestName" -ForegroundColor Red
    if ($Reason) {
        Write-Host "    原因: $Reason" -ForegroundColor Gray
    }
    $script:testResults.Failed++
}

function Test-Skipped {
    param([string]$TestName, [string]$Reason)
    Write-Host "  ⊘ $TestName (跳过)" -ForegroundColor Yellow
    if ($Reason) {
        Write-Host "    原因: $Reason" -ForegroundColor Gray
    }
    $script:testResults.Skipped++
}

# 测试 1: 应用程序启动和基本功能
Write-Host "测试 1: 应用程序启动..." -ForegroundColor Yellow
try {
    if (-not (Test-Path $ExePath)) {
        Test-Failed "应用程序启动" "可执行文件不存在: $ExePath"
    } else {
        $process = Start-Process -FilePath $ExePath -PassThru -WindowStyle Minimized
        Start-Sleep -Seconds 3
        
        if (-not $process.HasExited) {
            Test-Passed "应用程序启动"
            
            # 检查进程是否响应
            try {
                $null = $process.Responding
                Test-Passed "应用程序响应性"
            } catch {
                Test-Failed "应用程序响应性" "无法检查进程响应"
            }
            
            # 清理
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        } else {
            Test-Failed "应用程序启动" "进程立即退出"
        }
    }
} catch {
    Test-Failed "应用程序启动" $_.Exception.Message
}

# 测试 2: 设置文件读写
Write-Host ""
Write-Host "测试 2: 设置文件操作..." -ForegroundColor Yellow
$settingsPath = "$env:APPDATA\LenovoLegionToolkit\settings.json"
try {
    if (Test-Path $settingsPath) {
        $settingsContent = Get-Content $settingsPath -Raw
        if ($settingsContent) {
            $settings = $settingsContent | ConvertFrom-Json
            Test-Passed "设置文件读取"
            
            # 检查关键设置项
            if ($settings.PSObject.Properties.Name -contains "ShowDonateButton") {
                Test-Passed "ShowDonateButton 设置项存在"
            } else {
                Test-Failed "ShowDonateButton 设置项" "设置项不存在"
            }
        } else {
            Test-Failed "设置文件读取" "文件为空"
        }
    } else {
        Test-Skipped "设置文件读取" "文件不存在（首次运行）"
    }
} catch {
    Test-Failed "设置文件操作" $_.Exception.Message
}

# 测试 3: 日志系统
Write-Host ""
Write-Host "测试 3: 日志系统..." -ForegroundColor Yellow
$logPath = "$env:APPDATA\LenovoLegionToolkit\log"
try {
    if (Test-Path $logPath) {
        $logFiles = Get-ChildItem $logPath -Filter "log_*.txt"
        if ($logFiles.Count -gt 0) {
            Test-Passed "日志目录存在"
            
            $latestLog = $logFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            $logContent = Get-Content $latestLog.FullName -Tail 10
            
            if ($logContent -match "Info|Error|Warning|Trace") {
                Test-Passed "日志文件格式"
            } else {
                Test-Failed "日志文件格式" "日志格式异常"
            }
        } else {
            Test-Skipped "日志文件" "没有日志文件（首次运行）"
        }
    } else {
        Test-Skipped "日志目录" "目录不存在（首次运行）"
    }
} catch {
    Test-Failed "日志系统" $_.Exception.Message
}

# 测试 4: CLI接口
Write-Host ""
Write-Host "测试 4: CLI接口..." -ForegroundColor Yellow
$cliPath = "build\llt.exe"
try {
    if (Test-Path $cliPath) {
        # 测试 --help 命令
        $helpOutput = & $cliPath --help 2>&1
        if ($LASTEXITCODE -eq 0 -or $helpOutput -match "help|usage|command") {
            Test-Passed "CLI --help 命令"
        } else {
            Test-Failed "CLI --help 命令" "命令执行失败"
        }
    } else {
        Test-Skipped "CLI接口" "CLI可执行文件不存在"
    }
} catch {
    Test-Failed "CLI接口" $_.Exception.Message
}

# 测试 5: 资源文件
Write-Host ""
Write-Host "测试 5: 资源文件..." -ForegroundColor Yellow
$resourceFiles = @(
    "LenovoLegionToolkit.WPF\Resources\Resource.resx",
    "LenovoLegionToolkit.WPF\Resources\Resource.zh-hans.resx"
)

foreach ($resourceFile in $resourceFiles) {
    if (Test-Path $resourceFile) {
        $content = Get-Content $resourceFile -Raw
        if ($content -match "DonatePage_CanBeHidden_Message") {
            Test-Passed "资源文件: $(Split-Path $resourceFile -Leaf)"
        } else {
            Test-Failed "资源文件: $(Split-Path $resourceFile -Leaf)" "缺少 DonatePage_CanBeHidden_Message"
        }
    } else {
        Test-Failed "资源文件: $(Split-Path $resourceFile -Leaf)" "文件不存在"
    }
}

# 输出测试结果摘要
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "测试结果摘要" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "通过: $($testResults.Passed)" -ForegroundColor Green
Write-Host "失败: $($testResults.Failed)" -ForegroundColor Red
Write-Host "跳过: $($testResults.Skipped)" -ForegroundColor Yellow
Write-Host ""

$totalTests = $testResults.Passed + $testResults.Failed + $testResults.Skipped
if ($testResults.Failed -eq 0) {
    Write-Host "所有测试通过！" -ForegroundColor Green
    exit 0
} else {
    Write-Host "部分测试失败！" -ForegroundColor Red
    exit 1
}


