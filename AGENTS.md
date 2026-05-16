# Lenovo Legion Toolkit 开发指南 (AGENTS.md)
记住要尽可能使用能够用的skill
## 📋 项目概述

### 基本信息
- **项目名称**: Lenovo Legion Toolkit (LLT)
- **项目类型**: Windows WPF 桌面应用程序
- **开发语言**: C# (.NET 10)
- **目标平台**: Windows (x64)
- **主要功能**: 联想拯救者系列笔记本硬件控制和优化工具

### 🚀 开发流程要点
- **⚡ 重要**: 在准备合并或发布候选时，将**用户可见**且将随下一版本发布的变更整理进 `CHANGELOG.md` 的 `## [Unreleased]`；避免为未合并、未发布的中间稿逐条堆砌条目
- **📝 格式**: 中英文双语，分类清晰 (Added/Fixed/Improved)
- **🔗 参考**: 详见下方"更新日志维护指南"章节（含 `[Unreleased]` 工作流与不必记录的情形）

### 项目结构
```

## 包依赖管理 (Central Package Management)

本项目使用 **NuGet Central Package Management (CPM)** 来集中管理所有 NuGet 包版本。

### 文件位置
- `Directory.Packages.props` - 中央包版本定义文件（位于仓库根目录）

### 工作原理
- 所有包版本都在 `Directory.Packages.props` 中统一定义
- 各 `.csproj` 文件中只声明 `PackageReference`，**不包含版本号**
- 构建时 NuGet 自动从中央文件解析版本

### 添加新包依赖的步骤

1. **在 `Directory.Packages.props` 中添加包版本定义**：
```xml
<ItemGroup>
  <PackageVersion Include="PackageName" Version="x.y.z" />
</ItemGroup>
```

2. **在需要使用该包的项目 `.csproj` 中添加引用**（无需版本号）：
```xml
<ItemGroup>
  <PackageReference Include="PackageName" />
</ItemGroup>
```

3. **更新 CHANGELOG.md** 记录依赖变更

### 好处
✅ 避免版本冲突 - 所有项目使用统一版本  
✅ 简化更新 - 只需修改一处即可更新所有项目的依赖版本  
✅ 清晰透明 - 所有依赖版本一目了然  
✅ 可传递性固定 - 自动解决传递依赖的版本冲突

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
├── LenovoLegionToolkit.PerformanceTest/ # 性能测试
├── LenovoLegionToolkit.SpectrumTester/  # RGB键盘测试
├── LenovoLegionToolkit-Plugins/       # 插件系统（独立子模块）
│   ├── SDK/                          # 插件开发SDK
│   ├── Plugins/                      # 插件集合
│   │   ├── CustomMouse/              # 鼠标样式插件
│   │   ├── ShellIntegration/         # Shell集成插件
│   │   ├── NetworkAcceleration/      # 网络加速插件
│   │   └── ViveTool/                 # ViVeTool插件
│   └── Build/                        # 构建输出
├── Docs/                             # 项目文档
│   ├── ARCHITECTURE.md               # 系统架构文档
│   ├── DEPLOYMENT.md                 # 构建部署指南
│   ├── SECURITY.md                   # 安全政策
│   └── CODE_OF_CONDUCT.md            # 社区行为准则
└── Assets/                           # 资源文件
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
    --output ./Build

# 创建安装包（如果有相关脚本）
# 需要检查是否有相关的构建脚本或CI/CD配置
```

### 📋 CHANGELOG.md 快速更新
```bash
# 开发完成后的标准提交流程（合并/发版前整理用户可见变更即可，参见「更新日志维护指南」）
# 1. 更新 CHANGELOG.md（在 [Unreleased] 部分添加变更）
# 2. 提交变更
git add CHANGELOG.md
git commit -m "feat: [功能描述] / [功能描述英文和中文双语]"

# 3. 继续开发其他功能...
```

## 🔄 更新日志维护指南

### 开发流程中的 Changelog 更新

**原则**: 完成一项重要功能、用户可见修复或准备发版时，更新主程序或各插件独立的 `CHANGELOG.md`；日常迭代以「合并/发布前整理」为主，而非每个本地草稿都落一条

#### 📋 更新时机
- ✅ **功能完成时**: 新功能实现并测试通过后（准备进入共享分支或发版前写入 `[Unreleased]`）
- ✅ **Bug修复时**: 用户可见或已发布版本中的缺陷修复并验证后
- ✅ **重构完成时**: 大型重构或对外行为/性能有影响的优化完成后
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

#### ⚠️ 不需要记录的内容

**原则**: CHANGELOG 记录的是**用户可见**的变更，而不是开发过程中的内部修复。

以下内容**不需要**记录在 CHANGELOG 中：
- ❌ 开发过程中遇到的 bug 修复（实现新功能时顺便修复的 bug）
- ❌ 代码重构、变量重命名等内部改进
- ❌ 合并重复的条目或清理格式

以下内容**应该**记录：
- ✅ 新功能首次发布
- ✅ 用户可见的 bug 修复
- ✅ 下一版本发现的上一版本 bug 修复
- ✅ 重要的配置或行为变更

#### `[Unreleased]` 工作流（与「不必记录」的补充说明）

- **`[Unreleased]` 不是开发日记 / `[Unreleased]` is not a dev diary**：同一主题（例如一次迁移、一个功能）在**尚未对外发布**的迭代中，若问题在**首次进入共享历史之前**（如首次 commit、合并进主分支之前）就已修掉，且**用户从未接触到**错误对外行为，则**不必**为这次自修再单独加一条 CHANGELOG。
- **概括优于碎条 / Prefer one summary line**：迁移或大型改动最终对外说明时，用**一条**概括性条目（例如「迁移到 X 并保持行为一致」）即可，而不是每个中间 commit 一条。
- **与现有规则一致 / Same bar as above**：这属于「实现过程中的附带修复」的延伸；已发布版本中的缺陷、用户可见变更、安全与破坏性变更仍必须记录。

**示例**:
```markdown
# 正确示例
- 新增插件系统支持动态加载 / Plugin system supports dynamic loading  ✅

# 不需要记录（开发过程中的内部修复）
- 修复了插件 DLL 命名问题 / Fixed DLL naming issue  ❌
  (这是实现插件系统功能时的附带修复，不影响用户)
```

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
- [ ] 是否影响用户体验且将随下一版本发布？→ 需要更新 CHANGELOG.md（`[Unreleased]`）
- [ ] 是否修复了已发布版本或用户可见的重要 bug？→ 需要更新 CHANGELOG.md
- [ ] 是否新增了对外功能？→ 需要更新 CHANGELOG.md
- [ ] 纯内部重构或未进入共享历史的自修？→ 参见「`[Unreleased]` 工作流」，通常不必逐条记录

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

## 📦 Release 发布物标准（强制）

### 1) 主程序仓库 `LenovoLegionToolkit` 每个新版本必须携带

#### 仓库内必须更新的文件
- `Directory.Build.props`：版本号必须更新为目标版本（`X.Y.Z`）。
- `CHANGELOG.md`：将本次发布内容从 `[Unreleased]` 归档到 `## [X.Y.Z] - YYYY-MM-DD`。
- `README.md` / `README_zh-hans.md`：若功能入口、安装方式或兼容性发生变化必须同步更新。

#### GitHub Release 必须附带的资产文件
- 主程序安装包或压缩包（至少一种）：
  - `LenovoLegionToolkit_vX.Y.Z_win-x64.zip` 或
  - `LenovoLegionToolkit_vX.Y.Z_Setup.exe`
- 校验文件：
  - `LenovoLegionToolkit_vX.Y.Z_SHA256.txt`
- 可选调试符号（仅调试发布需要）：
  - `LenovoLegionToolkit_vX.Y.Z_symbols.zip`

#### 标签规范
- Git Tag 必须使用：`vX.Y.Z`
- GitHub Release 标题必须包含同版本号：`Lenovo Legion Toolkit vX.Y.Z`

### 2) 插件仓库 `LenovoLegionToolkit-Plugins` 每个新版本必须携带

#### 仓库内必须更新的文件
- `store.json`：必须更新 `lastUpdated`、插件 `version`、`downloadUrl`、`changelog`。
- `plugins/<PluginName>/plugin.json`：`version` 与 `minLLTVersion` 必须与发布一致。
- `plugins/<PluginName>/CHANGELOG.md`：必须新增本插件本次发布条目。

#### GitHub Release 必须附带的资产文件
- 每个插件一个 zip 包，命名必须稳定：
  - `<plugin-id>-vX.Y.Z.zip`
- zip 内必须至少包含：
  - 主插件 DLL
  - `plugin.json`

### 3) Release 正文格式（中英双语，必须）

使用以下模板：

```markdown
# Lenovo Legion Toolkit vX.Y.Z

Release date: YYYY-MM-DD

## Added / 新增
- 中文描述 / English description

## Fixed / 修复
- 中文描述 / English description

## Improved / 改进
- 中文描述 / English description

## Downloads / 下载
- LenovoLegionToolkit_vX.Y.Z_win-x64.zip
- LenovoLegionToolkit_vX.Y.Z_SHA256.txt

## Compatibility / 兼容性
- Minimum LLT Version: X.Y.Z（插件发布时必须写）
- OS: Windows 10/11 x64

## Verification / 校验
- `CertUtil -hashfile <file> SHA256`
```

### 4) 发版前最终核对（必须全部满足）
- [ ] Tag 为 `vX.Y.Z` 且指向正确提交
- [ ] Release 资产文件齐全且命名符合规范
- [ ] `CHANGELOG.md` 与 Release 正文内容一致
- [ ] 主程序/插件版本号与元数据一致（`Directory.Build.props`、`store.json`、`plugin.json`）
- [ ] 下载链接可访问，SHA256 可校验

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
- [ ] **CHANGELOG.md**：凡用户可见、将随下一版本发布的变更已写入 `[Unreleased]`（参见「更新日志维护指南」；未发布前的迭代自修不必逐条堆砌）
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

## 🔌 插件系统架构

### 项目结构

插件系统采用**独立仓库 + 插件市场**模式：

```
LenovoLegionToolkit/                          # 主项目仓库
├── LenovoLegionToolkit.WPF/                  # 主应用程序 (WPF UI)
├── LenovoLegionToolkit.Lib/                  # 核心业务逻辑库
│   └── Plugins/                              # 插件管理模块
│       ├── PluginLoader.cs                   # 插件加载器
│       ├── VersionChecker.cs                  # 版本兼容性检查
│       ├── StoreClient.cs                     # 插件市场客户端
│       ├── UpdateManager.cs                   # 更新管理器
│       ├── Models/                           # 数据模型
│       └── Exceptions/                       # 自定义异常
│
LenovoLegionToolkit-Plugins/                  # 独立仓库 (插件)
├── plugins/
│   ├── SDK/                                  # 插件开发 SDK
│   ├── CustomMouse/
│   ├── NetworkAcceleration/
│   ├── ShellIntegration/
│   └── ViveTool/
├── store.json                                # 插件市场元数据 (GitHub Pages)
└── .github/Workflows/
    └── ci.yml                                # CI/CD 自动构建
```

### 仓库关系

| 项目 | 仓库位置 | 远程地址 |
|------|---------|---------|
| **主项目** | `LenovoLegionToolkit/` | github.com/SSC-STUDIO/LenovoLegionToolkit.git |
| **插件项目** | `LenovoLegionToolkit-Plugins/` | github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins.git |

### 核心设计原则

1. **SDK 内置**: 主程序自带 SDK，插件只需实现 `IPlugin` 接口
2. **独立发布**: 插件独立于主程序发布版本
3. **版本兼容**: 插件声明最低支持的主程序版本 (`minLLTVersion`)
4. **动态加载**: 运行时从 `%APPDATA%` 目录加载插件 DLL
5. **自动更新**: 支持手动检查、启动时检查、自动后台检查三种更新策略

### 存储路径

```
%APPDATA%\LenovoLegionToolkit\plugins\
├── installed\                                 # 已安装插件
│   ├── CustomMouse\
│   │   ├── LenovoLegionToolkit.Plugins.CustomMouse.dll
│   │   └── plugin.json
│   └── ShellIntegration\
│       ├── LenovoLegionToolkit.Plugins.ShellIntegration.dll
│       └── plugin.json
├── updates\                                   # 待安装更新
└── store.json                                 # 缓存的市场元数据
```

### GitHub 资源结构

```
LenovoLegionToolkit-Plugins/
├── store.json                                # 插件市场元数据 (GitHub Pages gh-pages 分支)
├── plugins/
│   ├── CustomMouse/
│   │   ├── plugin.json
│   │   └── LenovoLegionToolkit.Plugins.CustomMouse.dll
│   └── ShellIntegration/
│       ├── plugin.json
│       └── LenovoLegionToolkit.Plugins.ShellIntegration.dll
└── releases/                                 # GitHub Releases
    ├── custom-mouse-v1.0.0.zip
    └── shell-integration-v1.0.0.zip
```

### store.json 格式

```json
{
  "lastUpdated": "2026-02-06T12:00:00Z",
  "plugins": [
    {
      "id": "custom-mouse",
      "name": "Custom Mouse",
      "description": "Apply custom Windows 11 cursor styles",
      "author": "LLT Team",
      "version": "1.0.0",
      "minLLTVersion": "2.14.0",
      "downloadUrl": "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/download/custom-mouse-v1.0.0/custom-mouse-v1.0.0.zip",
      "changelog": "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/releases/tag/custom-mouse-v1.0.0"
    }
  ]
}
```

### plugin.json 格式 (每个插件内嵌)

```json
{
  "id": "custom-mouse",
  "name": "Custom Mouse",
  "version": "1.0.0",
  "minLLTVersion": "2.14.0",
  "author": "LLT Team",
  "repository": "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins",
  "issues": "https://github.com/SSC-STUDIO/LenovoLegionToolkit-Plugins/issues"
}
```

### 更新策略

| 策略 | 触发方式 | 实现方式 |
|------|---------|---------|
| **启动时检查** | 每次启动应用 | 后台异步检查，不阻塞 UI |
| **手动更新** | 用户点击按钮 | 立即检查，显示更新列表 |
| **自动更新** | 后台定时检查 | 每 24 小时或每周检查 |

### 插件项目配置

所有插件项目必须满足以下要求：

1. **目标框架**: `net10.0-windows`
2. **引用 SDK**: 引用 `LenovoLegionToolkit.Plugins.SDK` (PrivateAssets=All)
3. **输出路径**: `Build/plugins/{PluginName}/`
4. **内嵌 plugin.json**: 设置为 EmbeddedResource 或 Copy to Output Directory
5. **版本号格式**: `X.Y.Z` (语义化版本)

### CI/CD 发布流程

```yaml
# .github/Workflows/release.yml
name: Release Plugins

on:
  push:
    branches: [main]
    paths:
      - 'plugins/**'
  release:
    types: [created]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      
      - name: Build Plugins
        run: |
          dotnet build plugins/SDK --configuration Release
          dotnet build plugins/CustomMouse --configuration Release
          dotnet build plugins/ShellIntegration --configuration Release
      
      - name: Create Release ZIPs
        run: |
          # 为每个插件创建 zip 包
          Compress-Archive -Path plugins/CustomMouse/build/* -DestinationPath releases/custom-mouse-v${{ steps.version.outputs.custom-mouse }}.zip
      
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: releases/*.zip
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
      
      - name: Update store.json
        run: |
          # 更新 store.json 版本信息
          # 推送到 gh-pages 分支
```

---

*本文档将随项目发展持续更新，最后更新时间: 2026-02-06*
