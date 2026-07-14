# Universal Device Toolkit Plugins 代码风格规范

## 概述

本文档定义了 Universal Device Toolkit Plugins 项目的代码风格和最佳实践，确保代码质量、可维护性和团队协作一致性。

## 编辑器配置

项目根目录包含 `.editorconfig` 文件，Visual Studio 和 VS Code 会自动应用这些设置。

### 核心规则

| 规则 | 约束 | 级别 |
|------|------|------|
| 私有字段命名 | `_camelCase`（下划线前缀） | warning |
| 接口命名 | `IInterfaceName`（I前缀） | suggestion |
| 文件作用域命名空间 | `file_scoped namespace` | warning |
| 空检查 | `pattern matching` | warning |
| 大括号要求 | 所有代码块必须使用大括号 | warning |
| 修饰符要求 | 所有访问修饰符必须显式声明 | warning |
| readonly字段 | 应标记为 `readonly` | warning |
| 语句末尾 | 不使用分号 | - |

## 命名约定

### PascalCase（帕斯卡命名）

用于：
- 类名：`public class ViveToolService`
- 接口名：`public interface IViveToolService`
- 方法名：`public void StartRuntime()`
- 属性名：`public string PluginName`
- 公共字段：`public const int DefaultTimeout`
- 命名空间：`namespace UniversalDeviceToolkit.Plugins.ViveTool`

### camelCase（驼峰命名）

用于：
- 局部变量：`var pluginId = "custom-mouse"`
- 参数：`void Configure(string pluginId)`
- 私有字段：`private readonly HttpClient _httpClient`

### _underscorePrefix（下划线前缀）

用于：
- 私有实例字段：`private readonly HttpClientManager _httpClientManager`
- 静态私有字段：`private static readonly Lazy<HttpClient> _sharedClient`

## 文件组织

### 文件头部

```csharp
using System;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.Shared;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Services;

public class ViveToolFeatureService : IViveToolFeatureService
{
    // 类成员按以下顺序组织：
    // 1. 常量
    // 2. 私有字段
    // 3. 公共属性
    // 4. 构造函数
    // 5. 公共方法
    // 6. 私有方法
    // 7. 事件处理器
}
```

### Using 语句顺序

1. System 命名空间
2. Microsoft 命名空间
3. 第三方命名空间
4. 项目命名空间

```csharp
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Wpf.Ui;
using UniversalDeviceToolkit.Plugins.Shared;
```

## 异步编程

### async/await 规范

**必须遵循**：
- 所有异步方法返回 `Task` 或 `Task<T>`
- **禁止**使用 `async void`（除了事件处理器）
- 使用 `ConfigureAwait(false)` 在库代码中
- 不使用 `Task.Wait()` 或 `Task.Result`

```csharp
// ✅ 正确
public async Task InitializeAsync()
{
    await DownloadAsync().ConfigureAwait(false);
}

// ❌ 错误
public async void InitializeAsync()
{
    await DownloadAsync(); // async void，异常无法捕获
}

// ❌ 错误
public void Initialize()
{
    DownloadAsync().Wait(); // 阻塞调用
}
```

### CancellationToken 使用

所有长时间运行的操作必须支持取消：

```csharp
public async Task RunAsync(CancellationToken cancellationToken)
{
    while (!cancellationToken.IsCancellationRequested)
    {
        await ProcessAsync(cancellationToken).ConfigureAwait(false);
    }
}
```

### 事件处理器中的 async void

事件处理器是唯一允许 `async void` 的地方，但必须添加异常处理：

```csharp
private async void OnButtonClick(object sender, RoutedEventArgs e)
{
    try
    {
        await ProcessAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Button click handler failed");
    }
}
```

## 异常处理

### 捕获策略

- **禁止空 catch 块**：必须记录或处理异常
- **区分异常级别**：Error、Warning、Info
- **特定异常优先**：先捕获具体异常，再捕获通用异常

```csharp
// ✅ 正确
try
{
    await ProcessAsync();
}
catch (IOException ex)
{
    _logger.LogWarning(ex, "IO operation failed, using fallback");
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error during processing");
    throw;
}

// ❌ 错误
try
{
    await ProcessAsync();
}
catch { } // 空catch块吞掉异常
```

### 异常抛出

- 使用 `throw` 而不是 `throw ex` 保持堆栈跟踪
- 验证参数使用 `ArgumentNullException.ThrowIfNull`

```csharp
// ✅ 正确
public void Configure(string path)
{
    ArgumentNullException.ThrowIfNull(path);

    if (!File.Exists(path))
        throw new FileNotFoundException($"Configuration file not found: {path}", path);
}

// ❌ 错误
catch (Exception ex)
{
    // Do something
    throw ex; // 丢失原始堆栈跟踪
}
```

## 资源管理

### HttpClient 单例

使用 `HttpClientManager` 而不是创建新实例：

```csharp
// ✅ 正确
private static readonly HttpClient _httpClient = HttpClientManager.GetSharedClient();

// ❌ 错误
using var client = new HttpClient(); // Socket耗尽风险
```

### IDisposable 对象

使用 `using` 或 `using var`：

```csharp
// ✅ 正确
using var stream = File.OpenRead(path);
await stream.ReadAsync(buffer);

// ✅ 正确
using (var stream = File.OpenRead(path))
{
    await stream.ReadAsync(buffer);
}
```

## 进程执行安全

### ProcessRunner 使用

禁止手动创建进程，使用 `ProcessRunner`：

```csharp
// ✅ 正确
var result = await ProcessRunner.RunAsync(
    "tool.exe",
    "--feature 123",
    timeoutSeconds: 30
);

// ❌ 错误
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "tool.exe",
        Arguments = userInput // 命令注入风险
    }
};
```

## WPF UI 模式

### InitializeComponent 回退

所有 UI 控件必须使用 `WpfFallbackHelper`：

```csharp
public class ViveToolPage : Page, IPluginPage
{
    public ViveToolPage()
    {
        WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);
    }

    private void BuildFallbackUi()
    {
        var panel = new StackPanel();
        // 构建回退UI
        this.Content = panel;
    }
}
```

## 本地化

### 资源字符串

- 所有用户可见字符串必须通过资源文件
- 英文回退值作为默认值
- 禁止硬编码中文字符串

```csharp
// ✅ 正确
public static class ViveToolText
{
    private const string DefaultPluginName = "ViveTool Plugin";

    public static string PluginName =>
        Resources.Resource.ResourceManager.GetString(nameof(PluginName))
        ?? DefaultPluginName;
}

// ❌ 错误
public static string PluginName = "ViveTool 插件"; // 硬编码中文
```

### 资源文件结构

```
Resources/
├── Resource.resx         # 英文回退值
├── Resource.zh.resx      # 中文翻译
└── Resource.zh-Hant.resx # 繁体翻译
```

## 测试规范

### 测试命名

- 方法名：`<Method>_<Scenario>_<Expected>`
- 类名：`<Class>Tests`

```csharp
public class HttpClientManagerTests
{
    [Fact]
    public void GetSharedClient_ReturnsNonNullInstance()
    {
        var client = HttpClientManager.GetSharedClient();
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    public void RunAsync_WithValidTimeout_ReturnsSuccess(int timeout)
    {
        // 测试逻辑
    }
}
```

### 测试覆盖

- 单元测试：每个公共方法至少一个测试
- 边界测试：验证边界值和异常情况
- 集成测试：关键路径覆盖

## 日志规范

### ILogger 使用

```csharp
private readonly ILogger<ViveToolService> _logger;

public async Task ProcessAsync()
{
    _logger.LogInformation("Starting process");

    try
    {
        await ExecuteAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Process failed");
        throw;
    }
}
```

### 日志级别

- **Error**: 严重错误，影响功能
- **Warning**: 潜在问题，可恢复
- **Information**: 生命周期和重要流程
- **Debug**: 详细调试信息（仅在开发环境）
- **Trace**: 最详细的跟踪信息（仅在开发环境）

## 魔法数字

所有数值常量应定义在 `Constants.cs` 或类内部：

```csharp
// ✅ 正确
public class Constants
{
    public const int DefaultTimeoutSeconds = 30;
    public const int DownloadTimeoutSeconds = 120;
    public const int DefaultBufferSize = 8192;
}

// ❌ 错误
await client.GetAsync(url); // 缺少timeout
var buffer = new byte[8192]; // 魔法数字
```

## 代码注释

### XML 文档注释

公共 API 必须提供 XML 文档：

```csharp
/// <summary>
/// Manages shared HttpClient instances to prevent socket exhaustion.
/// </summary>
public static class HttpClientManager
{
    /// <summary>
    /// Gets a shared HttpClient instance with default timeout settings.
    /// </summary>
    /// <returns>A singleton HttpClient instance.</returns>
    public static HttpClient GetSharedClient() => _sharedClient.Value;
}
```

### 内联注释

仅在复杂逻辑处添加注释，代码应自文档化：

```csharp
// ✅ 正确 - 解释非显而易见的逻辑
// ViVeTool requires feature ID in format: "FeatureID:State"
var featureArg = $"{featureId}:{(enable ? "Enable" : "Disable")}";

// ❌ 错误 - 显而易见的逻辑
// Get the plugin name
var name = PluginName;
```

## 重构原则

### 单一职责

每个类和方法只做一件事：

```csharp
// ✅ 正确 - 拆分服务
ViveToolFeatureService   // Feature操作
ViveToolDownloadService  // 下载管理
ViveToolProcessService   // 进程执行

// ❌ 错误 - 单一大类
ViveToolService // 1026行，包含所有功能
```

### DRY（Don't Repeat Yourself）

消除重复代码，提取共享工具：

```csharp
// ✅ 正确 - 使用共享库
WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi);

// ❌ 错误 - 6处重复代码
try { InitializeComponent(); }
catch { BuildFallbackUi(); }
```

### SOLID 原则

- **S**: 单一职责原则
- **O**: 开闭原则（扩展开放，修改关闭）
- **L**: 里氏替换原则
- **I**: 接口隔离原则
- **D**: 依赖倒置原则

## 禁止事项

### 绝对禁止

1. **反射访问私有字段**：破坏封装
2. **async void**：异常无法捕获（除事件处理器）
3. **Task.Wait()**：阻塞调用
4. **空 catch 块**：吞掉异常
5. **硬编码中文**：本地化缺失
6. **魔法数字**：可读性差
7. **手动 HttpClient 创建**：socket耗尽
8. **命令注入**：安全漏洞

### 示例反模式

```csharp
// ❌ 反模式集合
public class BadExample
{
    // 反射破坏封装
    private static readonly FieldInfo _tokenField =
        typeof(PluginBase).GetField("RuntimeCancellationTokenSource");

    // async void 异常无法捕获
    public async void StartAsync() { ... }

    // 阻塞调用
    public void Run() { Task.Delay(1000).Wait(); }

    // 空catch块
    try { ... } catch { }

    // 硬编码中文
    public string Name = "插件名称";

    // 魔法数字
    await client.GetAsync(url); // 无timeout

    // HttpClient实例泄漏
    var client = new HttpClient();

    // 命令注入
    process.StartInfo.Arguments = userInput;
}
```

## 工具验证

### 编译检查

```bash
dotnet build UniversalDeviceToolkit-Plugins.sln
```

### 测试验证

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### 完成检查

```bash
dotnet build .\UniversalDeviceToolkit-Plugins.sln -c Release
dotnet test .\UniversalDeviceToolkit-Plugins.sln -c Release
```

### 代码分析

Visual Studio 或 Rider 内置分析器会自动检测大部分规则违反。

## 参考资源

- [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [.NET EditorConfig Options](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/code-style-rule-options)
- [Framework Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [CLEAN Code Principles](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882)
