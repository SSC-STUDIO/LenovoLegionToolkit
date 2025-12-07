# 代码命名标准

## 1. 概述
本文档定义了 LenovoLegionToolkit 代码库的命名约定，旨在确保代码的一致性、可读性和可维护性。所有开发人员在编写或修改代码时应遵循这些标准。

## 2. 大小写规则

### 2.1 PascalCase
- **类名**：`GPUController`, `PowerModeFeature`
- **接口名**：`IGPUController`, `IFeature`
- **方法名**：`GetStateAsync`, `SetPerformanceMode`
- **属性名**：`IsSupported`, `CurrentState`
- **枚举名**：`GPUState`, `PowerModeState`
- **枚举值**：`GPUState.Active`, `PowerModeState.Performance`
- **事件名**：`StateChanged`, `PerformanceModeUpdated`

### 2.2 camelCase
- **参数名**：`deviceId`, `cancellationToken`
- **局部变量**：`currentState`, `processList`
- **私有字段**：`_lock`, `_currentState`, `_refreshTask`
- **受保护字段**：`_initialized`, `_disposed`

### 2.3 UPPER_SNAKE_CASE
- **常量**：`MAX_RETRY_COUNT`, `DEFAULT_TIMEOUT_MS`
- **只读静态字段**：`DEFAULT_CONFIG_PATH`, `SUPPORTED_DEVICES`

## 3. 命名原则

### 3.1 描述性命名
- 变量名应清晰描述其用途，避免使用模糊或过于简短的名称
- **推荐**：`currentPowerMode`
- **不推荐**：`pm`, `cur`

### 3.2 长度限制
- 变量名应足够长以描述其用途，但不应过长导致可读性下降
- 理想长度：3-20个字符
- 对于特别复杂的概念，可以使用更长的名称

### 3.3 避免缩写
- 除非是广为人知的技术缩写，否则应使用完整单词
- **允许的缩写**：`CPU`, `GPU`, `USB`, `RAM`, `URI`, `ID`, `GUID`
- **不推荐**：`btn`（使用 `button`）, `txt`（使用 `text`）, `arg`（使用 `argument`）

### 3.4 缩写处理
- 对于广为人知的缩写，保持大写：`CPU`, `GPU`, `HTTP`
- 对于由多个单词组成的缩写，使用 PascalCase：`XmlParser`, `JsonSerializer`

### 3.5 布尔变量
- 使用 `is`, `has`, `can`, `should` 等前缀
- **推荐**：`isConnected`, `hasPermission`, `canExecute`
- **不推荐**：`connected`, `permission`, `execute`

## 4. 特定类型的命名规则

### 4.1 类和接口
- 类名应是名词或名词短语：`GPUController`, `PowerModeFeature`
- 接口名应描述行为，并以 `I` 为前缀：`IFeature`, `IStateProvider`
- 抽象类名可以以 `Abstract` 为前缀：`AbstractController`, `AbstractFeature`

### 4.2 方法
- 方法名应是动词或动词短语：`GetStateAsync`, `SetPerformanceMode`
- 异步方法应后缀 `Async`：`GetStateAsync`, `SaveSettingsAsync`
- 事件处理方法应使用 `On` 前缀：`OnStateChanged`, `OnPerformanceModeUpdated`

### 4.3 变量
- 集合变量应使用复数形式：`processes`, `devices`, `settings`
- 临时变量应清晰描述其用途：`tempFilePath`, `backupData`

### 4.4 常量
- 常量名应使用完整单词，避免缩写
- **推荐**：`MAX_RETRY_COUNT`, `DEFAULT_TIMEOUT_MS`
- **不推荐**：`MAX_RETRIES`, `DEF_TIMEOUT`

### 4.5 枚举
- 枚举名应是名词或名词短语：`GPUState`, `PowerModeState`
- 枚举值应是形容词或名词：`GPUState.Active`, `PowerModeState.Performance`
- 避免使用 `None` 作为枚举值，使用更描述性的名称：`Unknown`, `Disabled`

## 5. 特殊情况

### 5.1 私有字段
- 私有字段应使用下划线前缀：`_currentState`, `_refreshTask`
- 静态私有字段也应使用下划线前缀：`_instance`, `_initialized`

### 5.2 事件
- 事件名应使用过去分词形式描述发生的事情：`StateChanged`, `PerformanceModeUpdated`
- 事件处理程序应使用 `EventHandler` 或 `AsyncEventHandler` 委托

### 5.3 扩展方法
- 扩展方法名应描述其功能，避免使用与现有方法冲突的名称
- 扩展方法应放在适当命名的静态类中，类名以 `Extensions` 后缀：`StringExtensions`, `TaskExtensions`

## 6. 命名示例

### 6.1 类和接口
```csharp
// 类
public class SpectrumKeyboardBacklightController
{
    // 接口
    public interface ISpectrumScreenCapture
    {
        // 方法
        void CaptureScreen(ref RGBColor[,] buffer, int width, int height, CancellationToken token);
    }
}
```

### 6.2 方法和属性
```csharp
public class PowerModeFeature
{
    // 属性
    public bool AllowAllPowerModesOnBattery { get; set; }
    
    // 方法
    public async Task<PowerModeState> GetStateAsync() { /* ... */ }
    public async Task SetStateAsync(PowerModeState state) { /* ... */ }
}
```

### 6.3 常量和私有字段
```csharp
public class App
{
    // 常量
    private const string MUTEX_NAME = "LenovoLegionToolkit_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const int BACKGROUND_INITIALIZATION_WAIT_TIMEOUT_MS = 3000;
    
    // 私有字段
    private Mutex? _singleInstanceMutex;
    private Task? _backgroundInitializationTask;
    private readonly object _shutdownLock = new();
}
```

### 6.4 枚举
```csharp
public enum GPUState
{
    Unknown,
    Active,
    Inactive,
    PoweredOff,
    NvidiaGpuNotFound
}
```

## 7. 例外情况

- 当与外部库或 API 交互时，可能需要遵循其命名约定
- 对于历史代码，优先保持一致性，然后逐步迁移到新标准
- 在特殊情况下，团队可以共同决定偏离这些标准，但应记录原因

## 8. 执行和验证

- 所有代码提交前应通过代码审查，确保符合命名标准
- 项目将配置静态分析工具以自动检查命名一致性
- 定期进行代码库范围的命名一致性审查

## 9. 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| 1.0  | 2025-12-06 | 初始版本 |