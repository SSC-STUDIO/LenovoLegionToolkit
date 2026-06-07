# Universal Device Toolkit 开发者指南 (AGENTS.md)

本文档为 AI Agent 和开发者提供项目开发规范和指南

## 项目概览

### 基本信息
- **项目名称**: Universal Device Toolkit (UDT)
- **技术框架**: Windows WPF 桌面应用程序
- **编程语言**: C# (.NET 10)
- **目标平台**: Windows (x64)
- **许可证**: GPL-3.0

### 更新日志维护
- **维护位置**: 在 `CHANGELOG.md` 的 `## [Unreleased]` 部分记录将发布的**用户可见**更改
- **分类标准**: 使用标准分类 (Added/Fixed/Improved)
- **编写风格**: 使用双语格式 `[English] / [中文]`，每个 `[Unreleased]` 条目只需一行总结

### 规范说明

## 中心包管理 (Central Package Management)

本项目使用 **NuGet Central Package Management (CPM)** 集中管理 NuGet 依赖

### 核心文件
- `Directory.Packages.props` - 集中定义所有包的版本

### 使用方法
- 在 `Directory.Packages.props` 中定义包版本
- 在各 `.csproj` 文件中使用 `PackageReference` 仅引用**包名**
- 所有项目共享相同的包版本

### 依赖项维护流程
1. **添加包版本**: 在 `Directory.Packages.props` 添加版本定义
```xml
<ItemGroup>
  <PackageVersion Include="PackageName" Version="x.y.z" />
</ItemGroup>
```

2. **在项目中使用**: 在 `.csproj` 中仅引用包名
```xml
<ItemGroup>
  <PackageReference Include="PackageName" />
</ItemGroup>
```

3. **更新 CHANGELOG.md** 记录变更

### 禁止事项
- 禁止在多个项目中引用不同版本的同一包
- 禁止在项目中硬编码包版本号
- 禁止使用 PackageVersion 属性

## .NET 10 特殊说明
- 所有 Lib、WPF、CLI、Macro 项目必须使用 `net10.0-windows` 目标框架
- 跨平台诊断工具使用 `net10.0` (无 Windows 后缀)
- 所有异步代码必须使用 `ConfigureAwait(false)`
- 提供 `IDelayProvider` 接口用于测试，替代直接使用 `Task.Delay`
  - 实现类: `UniversalDeviceToolkit.Lib/Utils/IDelayProvider.cs`
  - 默认实现: `UniversalDeviceToolkit.Lib/Utils/DelayProvider.cs`
- 确保 CI 环境中已安装 .NET 10 SDK (`dotnet --list-sdks` 应显示 10.x)
- 避免 NU1201 错误 (无法解析 Microsoft.WindowsDesktop.App)

## 项目结构

```
UniversalDeviceToolkit/
├── UniversalDeviceToolkit.WPF/          # 主应用程序 (WPF UI)
├── UniversalDeviceToolkit.Lib/          # 核心库 (LenovoLegionToolkit.Lib.dll)
├── UniversalDeviceToolkit.Lib.Automation/ # 自动化模块
├── UniversalDeviceToolkit.Lib.Macro/     # 宏功能模块
├── UniversalDeviceToolkit.CLI/           # 命令行工具
├── UniversalDeviceToolkit.CLI.Lib/       # CLI 共享库
├── UniversalDeviceToolkit.CrossPlatform/  # 跨平台诊断工具
├── UniversalDeviceToolkit.Tests/         # 单元测试
├── UniversalDeviceToolkit.CrossPlatform.Tests/ # 跨平台测试
├── UniversalDeviceToolkit.PerformanceTest/ # 性能测试
├── UniversalDeviceToolkit.SpectrumTester/  # RGB 测试工具
├── LenovoLegionToolkit-Plugins/       # 插件仓库
│   ├── SDK/                          # 插件 SDK
│   └── Plugins/                      # 官方插件
│       ├── CustomMouse/              # 自定义鼠标插件
│       ├── ShellIntegration/         # Shell 集成插件
│       ├── NetworkAcceleration/      # 网络加速插件
│       └── ViveTool/                 # ViVeTool 兼容插件
├── Build/                        # 构建输出
├── Docs/                             # 文档目录
│   ├── ARCHITECTURE.md               # 架构文档
│   ├── DEPLOYMENT.md                 # 部署文档
│   ├── SECURITY.md                   # 安全文档
│   └── CODE_OF_CONDUCT.md            # 行为准则
├── Assets/                           # 资源文件
```

## 开发命令

### 构建命令
```bash
# 清理并构建解决方案
dotnet clean UniversalDeviceToolkit.sln
dotnet build UniversalDeviceToolkit.sln --configuration Debug

# 发布构建
dotnet build UniversalDeviceToolkit.sln --configuration Release

# 单项目构建
dotnet build UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj --configuration Release
```

### 测试命令
```bash
# 运行所有测试
dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj --framework net10.0-windows

# 收集代码覆盖率
dotnet test --collect:"XPlat Code Coverage"

# 运行单元测试
dotnet test --filter "TestCategory=Unit"
```

### 发布命令
```bash
# 发布 WPF 应用
dotnet publish UniversalDeviceToolkit.WPF/UniversalDeviceToolkit.WPF.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained false `
    --output ./Build

# 使用 Make.bat 进行发布构建 (包含完整清理)
# 或使用 Clean.bat 仅进行清理
```

### 更新 CHANGELOG.md 流程
```bash
# 按以下步骤进行更新日志维护
# 1. 编辑 CHANGELOG.md，在 [Unreleased] 下添加新条目
# 2. 提交更改
git add CHANGELOG.md
git commit -m "feat: [English] / [中文]"

# 3. 继续开发...
```

## 更新日志维护指南

### 更新日志分类原则

**应记录**: 所有**用户可见**的更改，应记录到 `CHANGELOG.md` 的 `## [Unreleased]`

#### 应记录的类型
- 新增用户可见功能
- Bug 修复 (用户会遇到的问题)
- 改进用户体验的功能
- 重要的开发者体验改进

#### 不需要记录的类型
- 开发过程中的内部调试
- 未对用户产生任何影响的代码重构
- 纯粹的技术改进（不影响功能）

### 更新日志格式

**标准格式**:
```markdown
### Added / 新增
- 功能描述 / Feature description
### Fixed / 修复
- 问题描述 / Issue description
### Improved / 改进
- 改进描述 / Improvement description
```

#### 不当示例

**问题类型 1 - 未描述问题影响**:
```markdown
# 差例
- 修复了 bug
# 优秀
- 修复了 GPU 模式切换失败的问题 / Fixed GPU mode switching failure
```

**问题类型 2 - 描述过于模糊**:
```markdown
# 差例
- 改进了性能
# 优秀
- 提高了应用启动速度 / Improved application startup performance
```

### [Unreleased] 维护原则
- **`[Unreleased]` 不是开发日志**: 不要在每次提交时添加新条目
- **简洁优先**: 每个更改只需一行总结，使用双语格式
- **保持一致**: 保持中英文描述准确对应
- **避免过度细分**: 如果多个提交修复同一个问题，只需一条

**示例**:
```markdown
## [Unreleased]

### Added / 新增
- 支持插件动态加载 / Plugin system supports dynamic loading

### Fixed / 修复
- 修复了 DLL 命名问题 / Fixed DLL naming issue
```

### 提交信息格式

**标准格式**:
```
<类型>(<范围>): <描述>

# 类型: feat, fix, improve, docs, refactor, test, chore
# 范围: plugins, ui, performance, security, etc.
# 描述: 简短描述
```

**示例**:
```
feat(plugins): 添加插件动态加载支持 / Add plugin system with dynamic loading
fix(power-mode): 修复 GPU 模式切换失败 / Fixed GPU mode switching failure
docs(readme): 更新 README 文档 / Update README documentation
```

### 发布前检查清单

#### 代码检查
- [ ] 代码符合 C# 编码规范
- [ ] 所有公开 API 都有 XML 文档注释
- [ ] 实现 IDisposable 的类正确处理资源释放
- [ ] 异步方法使用 ConfigureAwait(false)
- [ ] 错误处理已添加到所有外部调用
- [ ] 没有遗留的 Console.WriteLine 调试代码

#### 测试检查
- [ ] 运行 `dotnet test`
- [ ] Release 构建成功
- [ ] 新功能有相应的单元测试

#### 更新日志检查
- [ ] **CHANGELOG.md** 的 `[Unreleased]` 部分完整记录了所有用户可见更改
- [ ] 使用双语格式 (Added/Fixed/Improved)
- [ ] 更改描述清晰准确

---

## C# 编码规范

### 命名规范
- **类名**: PascalCase (如 `PowerModeController`)
- **方法名**: PascalCase (如 `SetPowerModeAsync`)
- **属性名**: PascalCase (如 `IsEnabled`)
- **字段**:
  - 私有字段: _camelCase (如 `_logger`)
  - 常量字段: PascalCase (如 `MaxRetryCount`)
- **局部变量**: camelCase (如 `currentMode`)
- **接口**: 以 'I' 开头 (如 `IDeviceController`)

### 命名空间

所有代码使用以下命名空间前缀:
- `UniversalDeviceToolkit.Lib`
- `UniversalDeviceToolkit.WPF`
- `UniversalDeviceToolkit.CLI`
- `UniversalDeviceToolkit.Lib.Automation`
- `UniversalDeviceToolkit.Lib.Macro`

### 代码示例

```csharp
namespace UniversalDeviceToolkit.Lib.Controllers
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
            // 实现代码
        }
    }
}
```

### 异步/等待规范
```csharp
// 推荐: 简洁的异步方法
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

// 必须使用 ConfigureAwait(false)
public async Task<Data> GetDataAsync()
{
    var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
    return await response.Content.ReadFromJsonAsync<Data>().ConfigureAwait(false);
}
```

### 资源管理
```csharp
// 推荐: 使用 using 语句
public async Task ProcessFileAsync(string filePath)
{
    await using var stream = new FileStream(filePath, FileMode.Open);
    await using var reader = new StreamReader(stream);

    var content = await reader.ReadToEndAsync();
    // 处理文件
}

// 推荐: 实现 IDisposable
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

## 依赖注入

### 服务注册
```csharp
// 在 Program.cs 或 Startup.cs 中注册
builder.RegisterType<PowerModeController>().As<IPowerModeController>().SingleInstance();
builder.RegisterType<FanController>().As<IFanController>().InstancePerLifetime();

// ViewModel 依赖注入
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

## 异常处理

### 推荐模式
```csharp
// 1. 抛出特定异常
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

// 2. 返回 Result 类型
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
```

### 重试模式
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

    // 最后一次尝试
    return await operation();
}
```

## 单元测试

### 测试框架
- xUnit (测试框架)
- Moq (模拟框架)
- FluentAssertions (断言库)

### 测试示例
```csharp
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
}
```

## 代码文档

### XML 文档注释
```csharp
/// <summary>
/// 设置电源模式
/// </summary>
/// <param name="mode">目标电源模式</param>
/// <returns>是否设置成功</returns>
/// <exception cref="PowerModeException">设置失败时抛出</exception>
/// <remarks>
/// 此方法与 Windows 电源管理集成
/// </remarks>
/// <example>
/// <code>
/// var controller = new PowerModeController(logger);
/// var success = await controller.SetPowerModeAsync(PowerMode.Performance);
/// </code>
/// </example>
public async Task<bool> SetPowerModeAsync(PowerMode mode)
{
    // 实现代码
}
```

### README 更新
- **README.md**: 英文主文档 (功能介绍、下载、FAQ)
- **CHANGELOG.md**: 版本历史记录
- **API 参考**: 公共 API 文档

## 安全检查清单

### 代码安全
- [ ] 验证所有用户输入
- [ ] 使用参数化查询防止 SQL 注入
- [ ] 清理 HTML 防止 XSS
- [ ] 敏感数据加密存储
- [ ] 不在日志中记录敏感信息

---

## 版本管理规范

### 语义化版本 (SemVer)
```
主版本.次版本.修订号 (X.Y.Z)

主版本 (X): 不兼容的 API 更改
次版本 (Y): 向后兼容的功能新增
修订号 (Z): 向后兼容的问题修复
```

### 版本示例
- `2.14.0` - 新功能发布
- `2.14.1` - Bug 修复
- `3.0.0` - 重大架构更改

### 分支策略
```
main (主分支)
├── develop (开发分支)
├── feature/xxx (功能分支)
├── hotfix/xxx (热修复分支)
└── release/x.x.x (发布分支)
```

### 提交类型
```
<类型>(<范围>): <描述>

类型:
- feat: 新功能
- fix: Bug 修复
- docs: 文档更改
- style: 代码格式
- refactor: 重构
- test: 测试更改
- chore: 维护任务

示例:
feat(plugins): 添加插件动态加载 / Add plugin system
fix(power-mode): 修复电源模式切换 / Fix power mode switch
docs(readme): 更新文档 / Update documentation
```

## 发布检查清单

### 发布前检查
- [ ] 更新 `Directory.Build.props` 中的版本号
- [ ] 更新 `CHANGELOG.md` 的 `[Unreleased]` 为发布版本
- [ ] 更新 `README.md` / `README_zh-hans.md` 文档
- [ ] 运行完整测试套件
- [ ] 执行发布构建
- [ ] 生成 SHA256 校验文件
- [ ] 验证安装包

### 构建环境
- [ ] Debug 构建成功
- [ ] Release 构建成功
- [ ] 所有测试通过
- [ ] 代码覆盖率达标

### 本地测试
- [ ] 应用启动成功
- [ ] 主要功能测试通过
- [ ] 插件安装/卸载正常
- [ ] Windows 10/11 兼容性验证

### GitHub 发布
- [ ] 创建 Git Tag
- [ ] 编写 Release 说明
- [ ] 上传构建产物
- [ ] 通知社区

---

*本文档最后更新于 2026-02-06*