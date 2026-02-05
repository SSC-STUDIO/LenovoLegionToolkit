# Lenovo Legion Toolkit 开发指南 (AGENTS.md)

## 📋 项目概述

### 基本信息
- **项目名称**: Lenovo Legion Toolkit (LLT)
- **项目类型**: Windows WPF 桌面应用程序
- **开发语言**: C# (.NET 8.0)
- **目标平台**: Windows (x64)
- **主要功能**: 联想拯救者系列笔记本硬件控制和优化工具

### 🚀 开发流程要点
- **⚡ 重要**: 每完成功能/修复后立即更新 CHANGELOG.md
- **📝 格式**: 中英文双语，分类清晰 (Added/Fixed/Improved)
- **🔗 参考**: 详见下方"更新日志维护指南"章节

### 项目结构
```

## 升级到 .NET 10 的说明

- 本仓库已完成对主要项目的迁移到 `net10.0-windows`（Lib, WPF, CLI, Macro 等），以利用最新的运行时和语言特性。
- 为保证向后兼容，请同时将所有引用这些项目的子项目或测试项目也更新为 `net10.0-windows`。
- 迁移过程中已引入 `IDelayProvider` 抽象以便替换生产中的 `Task.Delay`（可在测试中注入快速实现），请参阅 `LenovoLegionToolkit.Lib/Utils/IDelayProvider.cs` 和 `LenovoLegionToolkit.Lib/Utils/DelayProvider.cs`。

迁移后注意事项：
- 本地构建时请确保已安装支持 .NET 10 的 SDK（例如 `dotnet --list-sdks` 能看到 10.x 版本）。
- 若 CI 或其它项目仍使用旧目标框架，请同步更新以避免项目引用冲突（NU1201）。

LenovoLegionToolkit/
├── LenovoLegionToolkit.WPF/          # 主应用程序 (WPF UI)
├── LenovoLegionToolkit.Lib/          # 核心业务逻辑库
├── LenovoLegionToolkit.Lib.Automation/ # 自动化功能库
├── LenovoLegionToolkit.Lib.Macro/     # 宏功能库
├── LenovoLegionToolkit.CLI/           # 命令行工具
├── LenovoLegionToolkit.CLI.Lib/       # CLI 核心库
├── LenovoLegionToolkit.Tests/         # 单元测试
├── plugins/                           # 插件系统
│   ├── SDK/                          # 插件开发SDK
│   ├── NetworkAcceleration/         # 网络加速插件
│   ├── ViveTool/                     # ViVeTool插件
│   └── Tools/                        # 工具插件
├── ShellIntegration/                  # Shell集成模块
└── assets/                           # 资源文件
```

## 🔧 构建命令

### 开发环境构建
```bash
# 清理并构建整个解决方案
dotnet clean LenovoLegionToolkit.sln
dotnet build LenovoLegionToolkit.sln --configuration Debug

# 发布版本构建
dotnet build LenovoLegionToolkit.sln --configuration Release

# 仅构建主应用程序
dotnet build LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj --configuration Release
```

### 测试命令
```bash
# 运行所有单元测试
dotnet test LenovoLegionToolkit.Tests/LenovoLegionToolkit.Tests.csproj --framework net10.0-windows

# 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"

# 运行特定测试
dotnet test --filter "TestCategory=Unit"
```

### 打包和发布
```bash
# 发布为自包含可执行文件
dotnet publish LenovoLegionToolkit.WPF/LenovoLegionToolkit.WPF.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output ./publish

# 创建安装包（如果有相关脚本）
# 需要检查是否有相关的构建脚本或CI/CD配置
```

### 📋 CHANGELOG.md 快速更新
```bash
# 开发完成后的标准提交流程
# 1. 更新 CHANGELOG.md（在 [Unreleased] 部分添加变更）
# 2. 提交变更
git add CHANGELOG.md
git commit -m "feat: [功能描述] / [功能描述英文]"

# 3. 继续开发其他功能...
```

## 🔄 更新日志维护指南

### 开发流程中的 Changelog 更新

**原则**: 每完成一个重要的功能开发或bug修复后，立即更新主程序或每个插件独立的CHANGELOG.md

#### 📋 更新时机
- ✅ **功能完成时**: 新功能实现并测试通过后
- ✅ **Bug修复时**: 重要bug修复并验证后
- ✅ **重构完成时**: 大型重构或代码优化完成后
- ✅ **版本发布前**: 发布候选版本时检查完整性

#### 🎯 更新内容分类

**新增 / Added**
- 新功能特性
- 新的API或接口
- 新的配置选项
- 新的插件或工具

**修复 / Fixed**
- Bug修复
- 崩溃问题解决
- 兼容性问题修复
- 安全问题修复

**改进 / Improved**
- 性能优化
- UI/UX改进
- 代码重构
- 文档更新

#### 📝 更新步骤

1. **定位版本段**: 在 `## [Unreleased]` 部分添加条目
2. **选择分类**: 根据变更类型选择合适的分类
3. **编写描述**: 使用中英文双语格式
4. **保持格式**: 遵循现有的格式规范
5. **验证完整**: 检查语法和格式正确性

#### ✏️ 书写规范

**格式模板**:
```markdown
- [功能描述] / [功能描述英文]
```

**示例**:
```markdown
- 插件系统支持动态加载 / Plugin system supports dynamic loading
- 修复GPU模式切换失败问题 / Fixed GPU mode switching failure
- 优化应用启动性能 / Improved application startup performance
```

#### 🚀 发布前的检查清单

- [ ] 所有重要变更都已记录在 CHANGELOG.md
- [ ] 描述准确反映实际变更
- [ ] 中英文格式一致
- [ ] 版本号更新正确
- [ ] 发布日期已填写

#### 📚 示例工作流程

```bash
# 1. 开发功能
git checkout -b feature/new-plugin-system
# ... 编码实现 ...

# 2. 完成后更新 CHANGELOG.md
# 编辑 CHANGELOG.md，在 [Unreleased] 部分添加:
# ### Added / 新增
# - 插件系统支持动态加载 / Plugin system supports dynamic loading

# 3. 提交变更
git add CHANGELOG.md
git commit -m "feat: Add plugin system with dynamic loading"
git push origin feature/new-plugin-system

# 4. 合并到主分支
git checkout master
git merge feature/new-plugin-system

# 5. 发布时
# 将 [Unreleased] 的内容移动到具体版本号下
```

#### 🎯 自动化提醒

**提交信息模板**:
```
<type>(<scope>): <description>

# Type: feat, fix, improve, docs, refactor, test, chore
# Scope: plugins, ui, performance, security, etc.
# Description: Brief description of the change
```

**提交后检查清单**:
- [ ] 是否影响用户体验？→ 需要更新 CHANGELOG.md
- [ ] 是否修复了重要bug？→ 需要更新 CHANGELOG.md  
- [ ] 是否新增了功能？→ 需要更新 CHANGELOG.md
- [ ] 是否是代码重构？→ 可选择性更新 CHANGELOG.md

#### 📈 CHANGELOG.md 维护技巧

1. **保持简洁**: 只记录用户可见的重要变更
2. **分类清晰**: 合理使用 Added/Fixed/Improved 分类
3. **双语一致**: 确保中英文含义对应
4. **版本控制**: 发布时将 Unreleased 内容移动到具体版本
5. **定期整理**: 避免累积过多未分类的变更

#### ⚠️ 常见错误避免

❌ **不要做**:
- 记录每个小的代码修改
- 使用过于技术化的描述
- 忘记更新中英文对照
- 在发布前才匆忙整理

✅ **应该做**:
- 实时更新，保持最新状态
- 使用用户友好的描述
- 保持格式一致性
- 定期检查完整性

---

## 📝 代码风格指南

### C# 命名约定
- **类名**: PascalCase (例: `PowerModeController`)
- **方法名**: PascalCase (例: `SetPowerModeAsync`)
- **属性名**: PascalCase (例: `IsEnabled`)
- **字段名**: 
  - 私有字段: _camelCase (例: `_logger`)
  - 常量: PascalCase (例: `MaxRetryCount`)
- **变量名**: camelCase (例: `currentMode`)
- **接口名**: 以 'I' 开头 (例: `IDeviceController`)

### 代码组织
```csharp
// 推荐的文件结构
namespace LenovoLegionToolkit.Lib.Controllers
{
    public class PowerModeController
    {
        private readonly ILogger _logger;
        private const int MaxRetryCount = 3;

        public PowerModeController(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<bool> SetPowerModeAsync(PowerMode mode)
        {
            // 实现
        }
    }
}
```

### Async/Await 模式
```csharp
// 正确的异步模式
public async Task<Result> OperationAsync()
{
    try
    {
        var result = await _service.DoWorkAsync();
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Operation failed");
        throw;
    }
}

// ConfigureAwait(false) 用于库代码
public async Task<Data> GetDataAsync()
{
    var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
    return await response.Content.ReadFromJsonAsync<Data>().ConfigureAwait(false);
}
```

### 资源管理
```csharp
// 使用 using 语句管理资源
public async Task ProcessFileAsync(string filePath)
{
    await using var stream = new FileStream(filePath, FileMode.Open);
    await using var reader = new StreamReader(stream);
    
    var content = await reader.ReadToEndAsync();
    // 处理内容
}

// 实现 IDisposable 的类
public class DeviceController : IDisposable
{
    private readonly IntPtr _deviceHandle;
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 释放托管资源
            }
            
            // 释放非托管资源
            if (_deviceHandle != IntPtr.Zero)
            {
                CloseDevice(_deviceHandle);
            }
            
            _disposed = true;
        }
    }
}
```

## 📦 Import/Using 约定

### Using 语句组织
```csharp
// System 命名空间（按字母顺序）
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

// Microsoft 命名空间
using Microsoft.Extensions.Logging;

// 第三方库
using Autofac;

// 项目内部命名空间（按字母顺序）
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Models;
```

### 依赖注入约定
```csharp
// 在 Program.cs 或Startup.cs 中注册依赖
builder.RegisterType<PowerModeController>().As<IPowerModeController>().SingleInstance();
builder.RegisterType<FanController>().As<IFanController>().InstancePerLifetime();

// 构造函数注入
public class MainWindowViewModel
{
    private readonly IPowerModeController _powerModeController;
    private readonly IFanController _fanController;

    public MainWindowViewModel(
        IPowerModeController powerModeController,
        IFanController fanController)
    {
        _powerModeController = powerModeController;
        _fanController = fanController;
    }
}
```

## ⚠️ 错误处理模式

### 异常处理策略
```csharp
// 1. 记录并重新抛出
public async Task SetPowerModeAsync(PowerMode mode)
{
    try
    {
        await _hardwareController.SetModeAsync(mode);
    }
    catch (HardwareException ex)
    {
        _logger.LogError(ex, "Failed to set power mode to {Mode}", mode);
        throw new PowerModeException($"Cannot set power mode to {mode}", ex);
    }
}

// 2. 返回 Result 模式
public async Task<Result<bool>> TrySetPowerModeAsync(PowerMode mode)
{
    try
    {
        await _hardwareController.SetModeAsync(mode);
        return Result.Success(true);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to set power mode to {Mode}", mode);
        return Result.Failure<bool>(ex.Message);
    }
}

// 3. 自定义异常
public class PowerModeException : Exception
{
    public PowerMode? TargetMode { get; }
    
    public PowerModeException(string message) : base(message) { }
    
    public PowerModeException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public PowerModeException(PowerMode targetMode, string message) 
        : base(message) 
    {
        TargetMode = targetMode;
    }
}
```

### 重试机制
```csharp
public async Task<T> WithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex) when (attempt < maxRetries && IsRetryableException(ex))
        {
            _logger.LogWarning(ex, "Operation failed on attempt {Attempt}, retrying...", attempt);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
        }
    }
    
    // 最后一次尝试，不捕获异常
    return await operation();
}
```

## 🧪 测试指南

### 单元测试
```csharp
// 使用 xUnit + Moq + FluentAssertions
public class PowerModeControllerTests
{
    private readonly Mock<IHardwareController> _mockHardwareController;
    private readonly Mock<ILogger> _mockLogger;
    private readonly PowerModeController _controller;

    public PowerModeControllerTests()
    {
        _mockHardwareController = new Mock<IHardwareController>();
        _mockLogger = new Mock<ILogger>();
        _controller = new PowerModeController(_mockLogger.Object);
    }

    [Fact]
    public async Task SetPowerModeAsync_ShouldCallHardwareController()
    {
        // Arrange
        var mode = PowerMode.Performance;
        
        // Act
        await _controller.SetPowerModeAsync(mode);
        
        // Assert
        _mockHardwareController.Verify(x => x.SetModeAsync(mode), Times.Once);
    }

    [Theory]
    [InlineData(PowerMode.Quiet, true)]
    [InlineData(PowerMode.Balanced, true)]
    [InlineData(PowerMode.Performance, false)]
    public async Task SetPowerModeAsync_WithBattery_ShouldRespectRestrictions(
        PowerMode mode, bool expectedResult)
    {
        // Arrange
        // 设置模拟状态
        
        // Act
        var result = await _controller.SetPowerModeAsync(mode);
        
        // Assert
        result.Should().Be(expectedResult);
    }
}
```

### 集成测试
```csharp
// 集成测试需要实际的硬件或模拟环境
[Trait("Category", "Integration")]
public class HardwareIntegrationTests
{
    [Fact]
    public async Task RealHardware_SetPowerMode_ShouldUpdateSystem()
    {
        // 需要实际硬件环境的测试
        // 注意：这类测试可能需要特殊环境标记
    }
}
```

### 端到端测试
```csharp
// 使用 UI 自动化测试框架（如 FlaUI）
[Trait("Category", "E2E")]
public class ApplicationE2ETests
{
    [Fact]
    public void LaunchApplication_ShouldShowMainWindow()
    {
        // 启动应用程序并验证主窗口
    }
    
    [Fact]
    public void ChangePowerMode_ShouldUpdateUI()
    {
        // 模拟用户操作并验证UI变化
    }
}
```

## 📚 文档要求

### 代码注释标准
```csharp
/// <summary>
/// 设置设备的电源模式
/// </summary>
/// <param name="mode">要设置的电源模式</param>
/// <returns>设置是否成功</returns>
/// <exception cref="PowerModeException">当设置失败时抛出</exception>
/// <remarks>
/// 此方法会自动同步Windows电源计划和性能模式
/// </remarks>
/// <example>
/// <code>
/// var controller = new PowerModeController(logger);
/// var success = await controller.SetPowerModeAsync(PowerMode.Performance);
/// </code>
/// </example>
public async Task<bool> SetPowerModeAsync(PowerMode mode)
{
    // 实现
}
```

### README 和变更日志
- **README.md**: 保持与现有格式一致，包含安装、使用、FAQ等
- **CHANGELOG.md**: 每个版本必须记录变更，使用语义化版本号
- **API文档**: 复杂API需要提供使用示例

## 🔍 代码审查清单

### 提交前检查
- [ ] 代码遵循项目命名约定
- [ ] 异常处理正确且一致
- [ ] 资源正确释放（IDisposable）
- [ ] 异步操作正确使用ConfigureAwait(false)（库代码）
- [ ] 日志记录适当且信息充分
- [ ] 没有调试代码（Console.WriteLine等）
- [ ] 敏感信息不提交（密钥、密码等）

### 性能检查
- [ ] 避免不必要的异步调用
- [ ] 合理使用缓存
- [ ] 避免UI线程阻塞
- [ ] 内存使用优化

### 安全检查
- [ ] 输入验证充分
- [ ] 权限检查适当
- [ ] 不存在SQL注入、XSS等漏洞
- [ ] 敏感数据加密存储

## 🚫 避免的提交模式（基于历史问题分析）

### 重复提交问题
- ❌ **避免重复更新相同文件**：如多次"Update plugin store"提交
- ❌ **避免UI组件的增量修改**：如分别更新tooltip、icon、button
- ❌ **避免相同变量的重复修复**：如多次修复isInstalled变量

### 提交信息问题
- ❌ **避免夸大修复范围**：避免使用"Fix all"、"Fixed all"等描述
- ❌ **避免不准确的分类**：将代码修复标记为docs提交
- ❌ **避免模糊的修改描述**：如"Updated UI"、"Fixed issues"

### 版本管理问题
- ❌ **避免频繁的版本bump**：没有实质性功能变更时不要bump版本
- ❌ **避免不规范的版本号**：使用X.Y.Z格式，避免3.15这样的非标准版本

### 正确的提交模式
✅ **合并相关修改**：将UI改进合并为功能性提交
✅ **准确的描述**：提交信息要与实际修改内容匹配
✅ **合适的分类**：使用正确的前缀（feat/fix/chore/docs/refactor）
✅ **具体的变更**：明确说明修改了什么文件、解决了什么问题

## 🏷️ 版本控制策略

### 语义化版本控制 (SemVer)
```
主版本号.次版本号.修订号 (X.Y.Z)

主版本号 (X): 不兼容的API修改
次版本号 (Y): 向下兼容的功能性新增
修订号 (Z): 向下兼容的问题修正
```

### 版本示例
- `2.14.0` - 新功能发布（如插件系统）
- `2.14.1` - Bug修复版本
- `3.0.0` - 重大版本更新（不兼容变更）

### 分支策略
```
main (生产)
├── develop (开发主分支)
├── feature/xxx (功能分支)
├── hotfix/xxx (紧急修复)
└── release/x.x.x (发布准备)
```

### 提交信息格式
```
<类型>(<范围>): <描述>

类型:
- feat: 新功能
- fix: Bug修复
- docs: 文档更新
- style: 代码格式调整
- refactor: 重构
- test: 测试相关
- chore: 构建过程或辅助工具的变动

示例:
feat(plugins): 添加插件自动更新功能
fix(power-mode): 修复切换性能模式时的异常
docs(readme): 更新安装说明
```

## ✅ 发布检查清单

### 发布前准备
- [ ] 所有测试通过（单元测试、集成测试）
- [ ] 代码审查完成
- [ ] 文档更新完成
- [ ] 版本号正确更新
- [ ] CHANGELOG.md 更新
- [ ] 性能测试通过（如适用）
- [ ] 安全扫描通过（如适用）

### 构建验证
- [ ] Debug构建成功
- [ ] Release构建成功
- [ ] 资源文件正确复制
- [ ] 依赖项版本正确
- [ ] 安装包正常生成

### 测试验证
- [ ] 全新安装测试
- [ ] 升级安装测试
- [ ] 卸载测试
- [ ] 核心功能验证
- [ ] 兼容性测试（多个Windows版本）
- [ ] 性能回归测试

### 发布后
- [ ] GitHub Release 创建
- [ ] 下载链接验证
- [ ] 自动更新机制验证
- [ ] 社区通知（Discord、QQ频道等）
- [ ] 监控用户反馈

## ⚡ 开发者日常工作流程 (快速参考)

### 🔄 每日开发循环

```bash
# 1. 开始新功能开发
git checkout -b feature/your-feature-name
# ... 编码实现 ...

# 2. 完成功能后
# a. 更新 CHANGELOG.md
# b. 运行测试
dotnet test
# c. 提交变更
git add .
git commit -m "feat(scope): Description / 描述"

# 3. 创建 PR 和合并
git push origin feature/your-feature-name
# ... 创建 Pull Request ...
# 合并后
git checkout master
git pull
```

### 📋 开发检查清单 (每个 PR 前检查)

#### 代码质量 ✅
- [ ] 代码遵循项目命名约定
- [ ] 异常处理正确且一致  
- [ ] 资源正确释放（IDisposable）
- [ ] 异步操作使用 ConfigureAwait(false)（库代码）
- [ ] 日志记录适当且信息充分
- [ ] 没有调试代码（Console.WriteLine等）

#### 测试和构建 ✅
- [ ] 所有单元测试通过：`dotnet test`
- [ ] Release 构建成功：`dotnet build -c Release`
- [ ] 核心逻辑有测试覆盖
- [ ] 手动测试关键功能

#### 文档和变更日志 ✅
- [ ] **CHANGELOG.md 已更新** ⭐ (最重要！)
- [ ] 中英文格式一致
- [ ] 描述准确反映实际变更
- [ ] 分类正确 (Added/Fixed/Improved)

### 🚀 常用 Git 命令速查

```bash
# 分支操作
git checkout -b feature/branch-name    # 创建并切换分支
git branch -d branch-name              # 删除本地分支
git push origin --delete branch-name    # 删除远程分支

# 提交操作
git add .                              # 添加所有变更
git commit -m "type(scope): desc"       # 规范提交信息
git commit --amend                     # 修改最后一次提交

# 同步操作
git fetch origin                        # 获取远程更新
git rebase origin/master               # 变基到最新主分支
git merge branch-name                   # 合并分支

# 撤销操作
git reset --soft HEAD~1               # 撤销最后一次提交（保留变更）
git reset --hard HEAD~1               # 撤销最后一次提交（丢弃变更）
git checkout -- file.txt              # 撤销文件修改
```

## 📚 补充文档索引

除本指南外，项目还提供以下补充文档：

| 文档 | 说明 |
|------|------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | 系统架构、组件说明、数据流程图 |
| [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) | 构建、测试、CI/CD、发布流程 |
| [docs/SECURITY.md](docs/SECURITY.md) | 安全政策、漏洞报告流程、最佳实践 |
| [docs/CODE_OF_CONDUCT.md](docs/CODE_OF_CONDUCT.md) | 社区行为准则、贡献标准 |
| [README.md](README.md) | 主用户文档（英文） |
| [README_zh-hans.md](README_zh-hans.md) | 主用户文档（中文） |

---

*本文档将随项目发展持续更新，最后更新时间: 2026-02-05*
