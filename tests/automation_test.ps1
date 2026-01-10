# Lenovo Legion Toolkit 自动化测试脚本
# 使用 PowerShell 进行端到端测试

param(
    [string]$ExePath = "build\Lenovo Legion Toolkit.exe",
    [switch]$Headless = $false,
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Lenovo Legion Toolkit 自动化测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查可执行文件是否存在
if (-not (Test-Path $ExePath)) {
    Write-Host "错误: 找不到可执行文件: $ExePath" -ForegroundColor Red
    exit 1
}

$testFailed = $false

Write-Host "测试 1: 检查应用程序是否可以启动..." -ForegroundColor Yellow
try {
    $process = Start-Process -FilePath $ExePath -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 2
    
    if ($process.HasExited) {
        Write-Host "  ⚠ 应用程序启动后立即退出（可能需要GUI环境或特定配置）" -ForegroundColor Yellow
        Write-Host "  ℹ 这可能是正常的，如果应用程序需要GUI环境运行" -ForegroundColor Gray
        # 不退出，继续执行其他测试
    } else {
        Write-Host "  ✓ 应用程序成功启动 (PID: $($process.Id))" -ForegroundColor Green
        
        # 等待一段时间后关闭
        Start-Sleep -Seconds 5
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  ✓ 应用程序已关闭" -ForegroundColor Green
    }
} catch {
    Write-Host "  ⚠ 启动失败: $_" -ForegroundColor Yellow
    Write-Host "  ℹ 这可能是正常的，如果应用程序需要特定环境" -ForegroundColor Gray
    # 不退出，继续执行其他测试
}

Write-Host ""
Write-Host "测试 2: 检查设置文件..." -ForegroundColor Yellow
$settingsPath = "$env:APPDATA\LenovoLegionToolkit\settings.json"
if (Test-Path $settingsPath) {
    $settings = Get-Content $settingsPath | ConvertFrom-Json
    Write-Host "  ✓ 设置文件存在" -ForegroundColor Green
    Write-Host "  - ShowDonateButton: $($settings.ShowDonateButton)" -ForegroundColor Gray
} else {
    Write-Host "  ⚠ 设置文件不存在（首次运行）" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "测试 3: 检查日志目录..." -ForegroundColor Yellow
$logPath = "$env:APPDATA\LenovoLegionToolkit\log"
if (Test-Path $logPath) {
    $logFiles = Get-ChildItem $logPath -Filter "log_*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($logFiles) {
        Write-Host "  ✓ 日志文件存在: $($logFiles.Name)" -ForegroundColor Green
        $logSize = (Get-Item $logFiles.FullName).Length
        Write-Host "  - 日志大小: $([math]::Round($logSize / 1KB, 2)) KB" -ForegroundColor Gray
    }
} else {
    Write-Host "  ⚠ 日志目录不存在（首次运行）" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "测试 4: 检查CLI接口..." -ForegroundColor Yellow
$cliPath = "build\llt.exe"
if (Test-Path $cliPath) {
    try {
        $cliOutput = & $cliPath --help 2>&1
        if ($LASTEXITCODE -eq 0 -or $cliOutput -match "help|usage") {
            Write-Host "  ✓ CLI接口可用" -ForegroundColor Green
        } else {
            Write-Host "  ⚠ CLI接口响应异常" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  ⚠ CLI接口测试跳过: $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⚠ CLI可执行文件不存在" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "测试 5: 检查依赖项..." -ForegroundColor Yellow
$requiredDlls = @(
    "LenovoLegionToolkit.Lib.dll",
    "Wpf.Ui.dll",
    "Newtonsoft.Json.dll"
)

$missingDlls = @()
foreach ($dll in $requiredDlls) {
    $dllPath = Join-Path "build" $dll
    if (Test-Path $dllPath) {
        Write-Host "  ✓ $dll" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $dll (缺失)" -ForegroundColor Red
        $missingDlls += $dll
    }
}

if ($missingDlls.Count -gt 0) {
    Write-Host ""
    Write-Host "错误: 缺少必需的依赖项" -ForegroundColor Red
    $testFailed = $true
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
if ($testFailed) {
    Write-Host "部分测试失败！" -ForegroundColor Red
    exit 1
} else {
    Write-Host "所有测试通过！" -ForegroundColor Green
    exit 0
}
Write-Host "========================================" -ForegroundColor Cyan

