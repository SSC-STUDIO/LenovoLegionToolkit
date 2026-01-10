# 测试方法指南

本项目提供了多种测试方法，您可以根据需要选择适合的方式来运行测试。

## 0. 解决PowerShell脚本执行问题

如果您在运行PowerShell脚本时遇到以下错误：
```
.\tests\run_all_tests.ps1 : File cannot be loaded because running scripts is disabled on this system.
```

这是因为PowerShell的执行策略限制导致的。您可以使用以下命令临时更改执行策略：

```powershell
# 临时允许当前PowerShell会话执行脚本
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process

# 或者使用以下命令直接运行脚本，不修改执行策略
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1
```

## 1. 单元测试

### 方法1：直接使用 dotnet test 命令

这是运行单元测试的最直接方式，适用于快速验证代码变更。

```powershell
# 在项目根目录运行所有单元测试
dotnet test

# 运行特定测试项目
dotnet test LenovoLegionToolkit.Tests

# 显示详细测试结果
dotnet test --verbosity detailed

# 生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 方法2：使用测试脚本（推荐用于完整测试套件）

项目提供了一个完整的测试脚本，用于运行所有测试类型，包括单元测试、集成测试、自动化测试和性能测试。

```powershell
# 方式1：先临时更改执行策略，再运行脚本
Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process
.\tests\run_all_tests.ps1

# 方式2：直接运行脚本，不修改执行策略
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1

# 仅运行单元测试，跳过其他测试类型
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1 -SkipIntegrationTests -SkipAutomationTests -SkipPerformanceTests

# 显示详细测试输出
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1 -Verbose
```

## 2. 完整测试套件

### 运行所有测试类型

```powershell
# 直接运行所有测试，不修改执行策略
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1
```

### 选择性运行测试类型

```powershell
# 跳过集成测试
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1 -SkipIntegrationTests

# 跳过自动化测试和性能测试
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1 -SkipAutomationTests -SkipPerformanceTests

# 仅运行性能测试
PowerShell -ExecutionPolicy Bypass -File .\tests\run_all_tests.ps1 -SkipUnitTests -SkipIntegrationTests -SkipAutomationTests
```

## 3. 单个测试脚本

如果您需要单独运行特定类型的测试，可以直接调用相应的测试脚本：

```powershell
# 运行集成测试
PowerShell -ExecutionPolicy Bypass -File .\tests\integration_test.ps1

# 运行自动化测试
PowerShell -ExecutionPolicy Bypass -File .\tests\automation_test.ps1

# 运行性能测试
PowerShell -ExecutionPolicy Bypass -File .\tests\performance_test.ps1

# 运行UI自动化测试（需要Python环境）
python .\tests\ui_automation_test.py
```

## 4. 测试结果

- 测试运行完成后，会显示详细的测试结果摘要
- 对于失败的测试，可以使用 `-Verbose` 参数查看详细输出
- 测试结果也会记录在 `tests/` 目录下的测试结果文件中

## 5. 什么时候使用哪种方法？

| 使用场景 | 推荐方法 |
|---------|---------|
| 快速验证代码变更 | `dotnet test` |
| 本地开发过程中的测试 | `dotnet test` |
| 提交代码前的完整验证 | `.\tests\run_all_tests.ps1` |
| CI/CD 流水线中的测试 | `.\tests\run_all_tests.ps1` |
| 仅运行特定类型的测试 | 相应的单个测试脚本 |

## 6. 测试覆盖率

要生成测试覆盖率报告，可以使用以下命令：

```powershell
# 生成覆盖率报告（XML格式）
dotnet test --collect:"XPlat Code Coverage"

# 生成多种格式的覆盖率报告
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura,lcov,json
```

覆盖率报告将生成在 `TestResults` 目录下，您可以使用 Visual Studio Code 的 Coverage Gutters 扩展或其他覆盖率工具查看。

## 7. 测试配置

测试配置文件位于 `tests/test_config.json`，您可以根据需要修改配置参数。

---

通过统一使用这些测试方法，您可以确保项目的测试覆盖率和代码质量，同时提高开发效率。