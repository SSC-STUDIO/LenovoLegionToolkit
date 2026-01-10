# Lenovo Legion Toolkit 自动化测试

本目录包含 Lenovo Legion Toolkit 的自动化测试脚本和测试用例。

## 测试结构

### 单元测试 (xUnit)
- `LenovoLegionToolkit.Tests/` - 使用 xUnit 框架的单元测试
  - `ApplicationSettingsTests.cs` - 测试设置管理
  - `LogTests.cs` - 测试日志系统
  - `CMDTests.cs` - 测试命令行执行
  - `AbstractSensorsControllerTests.cs` - 测试传感器控制器
  - `WindowsOptimizationServiceTests.cs` - 测试Windows优化服务

### PowerShell 自动化测试脚本
- `automation_test.ps1` - 基础自动化测试
  - 测试应用程序启动
  - 检查设置文件
  - 验证日志系统
  - 检查CLI接口
  - 验证依赖项

- `integration_test.ps1` - 集成测试
  - 应用程序启动和响应性
  - 设置文件读写
  - 日志系统功能
  - CLI接口测试
  - 资源文件验证

- `run_all_tests.ps1` - 完整测试套件
  - 运行所有测试
  - 生成测试报告
  - 支持选择性运行

## 使用方法

### 运行所有测试
```powershell
.\tests\run_all_tests.ps1
```

### 运行特定测试
```powershell
# 只运行单元测试
.\tests\run_all_tests.ps1 -SkipIntegrationTests -SkipAutomationTests

# 只运行集成测试
.\tests\run_all_tests.ps1 -SkipUnitTests -SkipAutomationTests

# 只运行自动化测试
.\tests\run_all_tests.ps1 -SkipUnitTests -SkipIntegrationTests
```

### 运行单个测试脚本
```powershell
# 运行自动化测试
.\tests\automation_test.ps1

# 运行集成测试
.\tests\integration_test.ps1

# 运行单元测试（使用 dotnet test）
dotnet test LenovoLegionToolkit.Tests\LenovoLegionToolkit.Tests.csproj
```

### 详细输出
```powershell
.\tests\run_all_tests.ps1 -Verbose
```

## 测试覆盖

### 已测试的功能
- ✅ 应用程序设置管理
- ✅ 日志系统
- ✅ 命令行执行 (CMD)
- ✅ 赞助按钮显示控制
- ✅ 应用程序启动和关闭
- ✅ CLI接口可用性
- ✅ 资源文件完整性

### 待测试的功能
- ⏳ 传感器数据缓存（需要硬件支持）
- ⏳ Windows优化服务（需要管理员权限）
- ⏳ UI自动化测试（需要UI自动化框架）
- ⏳ 性能测试（已有PerformanceTest项目）

## 注意事项

1. **权限要求**: 某些测试可能需要管理员权限
2. **硬件依赖**: 部分测试需要特定的Lenovo硬件支持
3. **环境要求**: 确保已构建项目（`build` 目录存在）
4. **首次运行**: 某些测试在首次运行时可能会跳过（设置文件不存在等）

## 持续集成

这些测试脚本可以集成到 CI/CD 流程中：

```yaml
# GitHub Actions 示例
- name: Run Tests
  run: |
    dotnet test
    powershell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1
```

## 贡献

添加新测试时，请遵循以下规范：
1. 单元测试使用 xUnit 框架
2. 使用 FluentAssertions 进行断言
3. 使用 Moq 进行模拟
4. PowerShell 脚本使用统一的输出格式
5. 添加适当的错误处理


