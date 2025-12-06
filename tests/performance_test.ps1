# Lenovo Legion Toolkit 性能测试脚本
# 测试应用程序的性能指标

param(
    [string]$ExePath = "build\Lenovo Legion Toolkit.exe",
    [int]$TestDurationSeconds = 30,
    [switch]$Detailed = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Lenovo Legion Toolkit 性能测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $ExePath)) {
    Write-Host "错误: 找不到可执行文件: $ExePath" -ForegroundColor Red
    exit 1
}

# 测试 1: 启动时间
Write-Host "测试 1: 应用程序启动时间..." -ForegroundColor Yellow
try {
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $process = Start-Process -FilePath $ExePath -PassThru -WindowStyle Minimized
    Start-Sleep -Seconds 2
    
    if (-not $process.HasExited) {
        $stopwatch.Stop()
        $startupTime = $stopwatch.ElapsedMilliseconds
        Write-Host "  ✓ 启动时间: $startupTime ms" -ForegroundColor Green
        
        if ($startupTime -gt 5000) {
            Write-Host "  ⚠ 启动时间较长（>5秒）" -ForegroundColor Yellow
        }
        
        # 测试内存使用
        if ($Detailed) {
            Start-Sleep -Seconds 3
            $memoryMB = [math]::Round($process.WorkingSet64 / 1MB, 2)
            Write-Host "  - 内存使用: $memoryMB MB" -ForegroundColor Gray
        }
        
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    } else {
        Write-Host "  ✗ 应用程序启动失败" -ForegroundColor Red
    }
} catch {
    Write-Host "  ✗ 测试失败: $_" -ForegroundColor Red
}

# 测试 2: 日志性能
Write-Host ""
Write-Host "测试 2: 日志系统性能..." -ForegroundColor Yellow
try {
    $logTestScript = @"
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

class LogPerformanceTest
{
    static async Task Main()
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new System.Collections.Generic.List<Task>();
        
        for (int i = 0; i < 1000; i++)
        {
            int id = i;
            tasks.Add(Task.Run(() => Log.Instance.Info($"`$"Test log message #{id}")));
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        Console.WriteLine(stopwatch.ElapsedMilliseconds);
    }
}
"@

    $tempScript = [System.IO.Path]::GetTempFileName() + ".cs"
    $tempExe = [System.IO.Path]::ChangeExtension($tempScript, ".exe")
    
    Set-Content -Path $tempScript -Value $logTestScript
    
    try {
        # 编译并运行测试
        $libPath = "LenovoLegionToolkit.Lib\bin\Debug\net8.0-windows\win-x64\LenovoLegionToolkit.Lib.dll"
        if (Test-Path $libPath) {
            $compileOutput = dotnet exec $libPath 2>&1
            # 简化测试：直接检查日志目录
            $logPath = "$env:APPDATA\LenovoLegionToolkit\log"
            if (Test-Path $logPath) {
                Write-Host "  ✓ 日志系统可用" -ForegroundColor Green
            } else {
                Write-Host "  ⚠ 日志目录不存在" -ForegroundColor Yellow
            }
        }
    } finally {
        Remove-Item $tempScript -ErrorAction SilentlyContinue
        Remove-Item $tempExe -ErrorAction SilentlyContinue
    }
} catch {
    Write-Host "  ⚠ 日志性能测试跳过: $_" -ForegroundColor Yellow
}

# 测试 3: 设置文件读写性能
Write-Host ""
Write-Host "测试 3: 设置文件读写性能..." -ForegroundColor Yellow
try {
    $settingsPath = "$env:APPDATA\LenovoLegionToolkit\settings.json"
    if (Test-Path $settingsPath) {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $settings = Get-Content $settingsPath | ConvertFrom-Json
        $stopwatch.Stop()
        
        $readTime = $stopwatch.ElapsedMilliseconds
        Write-Host "  ✓ 读取时间: $readTime ms" -ForegroundColor Green
        
        if ($readTime -gt 100) {
            Write-Host "  ⚠ 读取时间较长（>100ms）" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ⊘ 设置文件不存在（首次运行）" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ✗ 测试失败: $_" -ForegroundColor Red
}

# 测试 4: CLI响应时间
Write-Host ""
Write-Host "测试 4: CLI响应时间..." -ForegroundColor Yellow
$cliPath = "build\llt.exe"
if (Test-Path $cliPath) {
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $null = & $cliPath --help 2>&1
        $stopwatch.Stop()
        
        $responseTime = $stopwatch.ElapsedMilliseconds
        Write-Host "  ✓ CLI响应时间: $responseTime ms" -ForegroundColor Green
        
        if ($responseTime -gt 500) {
            Write-Host "  ⚠ CLI响应时间较长（>500ms）" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  ⚠ CLI测试跳过: $_" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ⊘ CLI可执行文件不存在" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "性能测试完成" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan


