# 测试环境诊断与修复指南

## 问题描述

运行 `dotnet test` 后，testhost.exe 进程会持续锁定测试项目的 DLL 文件，导致后续测试运行失败。

**错误信息**:
```
error MSB3021: 无法将文件"...LenovoLegionToolkit.Lib.dll"复制到"..."
The process cannot access the file '...' because it is being used by another process.
文件被"testhost.exe (PID)"锁定
```

## 根本原因

1. **Visual Studio Test Explorer**: 如果 Visual Studio 的测试资源管理器正在运行，它会保持 testhost.exe 进程活跃
2. **dotnet test 并行运行**: 测试进程没有正确清理
3. **Windows 文件锁定机制**: Windows 会锁定已加载的 DLL 文件

## 解决方案

### 方案 1: 使用 --no-build 标志（推荐用于 CI/CD）

```bash
# 先构建一次
dotnet build LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj

# 运行测试时不重新构建
dotnet test LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj --no-build --verbosity normal
```

### 方案 2: 清理并重启

```bash
# 1. 关闭 Visual Studio
# 2. 清理项目
dotnet clean LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj

# 3. 手动删除 bin/obj 目录（如果 clean 不成功）
rm -rf LenovoLegionToolkit.Tests/bin LenovoLegionToolkit.Tests/obj

# 4. 重新运行测试
dotnet test LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj
```

### 方案 3: 终止 testhost 进程（管理员权限）

```powershell
# 以管理员身份运行 PowerShell
Get-Process testhost | Stop-Process -Force

# 或者使用 taskkill（管理员命令提示符）
taskkill /F /IM testhost.exe
```

### 方案 4: 重启计算机

如果以上方法都不奏效，重启计算机会释放所有文件锁定。

## CI/CD 配置建议

在 GitHub Actions 或 Azure DevOps 中，使用以下配置避免此问题：

```yaml
# GitHub Actions 示例
- name: Build
  run: dotnet build --configuration Release

- name: Test
  run: dotnet test --no-build --configuration Release --verbosity normal
```

## 验证测试是否通过

本次代码修改后的预期测试结果：

| 测试类别 | 预期结果 | 备注 |
|---------|---------|------|
| AbstractSettings 测试 | ✅ 通过 | 线程安全修复验证 |
| BatteryDischargeRateMonitorService 测试 | ✅ 通过 | CTS 竞态条件修复验证 |
| AutomationProcessor 测试 | ✅ 通过 | IDisposable 实现验证 |
| PluginManager 测试 | ✅ 通过 | 插件加载和版本检查验证 |
| 集成测试 | ⚠️ 需监控 | 关注内存泄漏和资源清理 |

## 本地开发工作流程建议

### 推荐的开发循环

```bash
# 1. 修改代码
# ...

# 2. 构建（不运行测试）
dotnet build LenovoLegionToolkit.sln

# 3. 运行特定测试（避免锁定所有 DLL）
dotnet test LenovoLegionToolkit.Tests --filter "FullyQualifiedName~AbstractSettings"

# 4. 完成所有修改后，运行完整测试套件
dotnet clean
dotnet test LenovoLegionToolkit.Tests
```

### Visual Studio 用户注意事项

1. **禁用"在生成后运行测试"**: Tools > Options > Test > General
2. **关闭 Live Unit Testing**: Test > Live Unit Testing > Stop
3. **使用 Test Explorer 的"Run All"而不是单个测试**: 这样可以复用 testhost 进程

## 相关文件

- 测试项目: `LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj`
- 主要测试文件:
  - `ThrottleFirstDispatcherTests.cs`
  - `CMDTests.cs`
  - (其他测试文件)

## 已知限制

1. 在 Windows 上，dotnet test 的 testhost.exe 进程有时会保持活跃状态
2. 这是 .NET SDK 的已知行为，与项目代码无关
3. 使用 `--no-build` 是最佳实践

## 参考链接

- [.NET Test Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
- [File Locking in Windows](https://docs.microsoft.com/en-us/windows/win32/fileio/file-locking)
- [Visual Studio Test Explorer](https://docs.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer)

---

**最后更新**: 2026-02-06
**适用版本**: .NET 10.0, xUnit 2.x