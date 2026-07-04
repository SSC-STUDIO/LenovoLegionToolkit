# Universal Device Toolkit 深度性能优化与 AI Agent 批量重构指南

本指南基于通用设备工具箱 (`UniversalDeviceToolkit` / `LenovoLegionToolkit`) 在 Windows 10/11 (.NET 10 + WPF) 环境下长期的排查与深度优化实践总结而成。旨在为团队开发人员及后续接入的 **AI Agents** 提供一套标准化的性能排查、并发安全改造及代码重构规范。

---

## 目录
1. [第一部分：WPF 线程模型与异步并发规范（防闪退核心）](#一wpf-线程模型与异步并发规范防闪退核心)
2. [第二部分：WMI 与硬件底层通信优化（防卡死与 RDP 优化）](#二wmi-与硬件底层通信优化防卡死与-rdp-优化)
3. [第三部分：后台轮询、传感器监控与磁盘 I/O 治理](#三后台轮询传感器监控与磁盘-io-治理)
4. [第四部分：WPF UI 渲染与高频响应优化](#四wpf-ui-渲染与高频响应优化)
5. [第五部分：面向 AI Agent 的批量排查与改造标准流程 (SOP)](#五面向-ai-agent-的批量排查与改造标准流程-sop)
6. [第六部分：可以直接发给其他 Agent 的 Prompt 模板](#六可以直接发给其他-agent-的-prompt-模板)
7. [第七部分：对项目未来发展的建设性意见与架构规划](#七对项目未来发展的建设性意见与架构规划)

---

## 一、WPF 线程模型与异步并发规范（防闪退核心）

### 1. 严格划分 `.ConfigureAwait(false)` 的使用边界
在 .NET 异步编程中，`.ConfigureAwait(false)` 用于指示不需要在调用方的 `SynchronizationContext`（同步上下文）上继续执行，从而减少上下文切换开销并防止死锁。然而在 WPF 应用程序中，滥用该方法是导致程序神秘闪退、无提示崩溃的**头号元凶**。

> [!CAUTION]
> **绝对禁止在 UI 视图层使用 `.ConfigureAwait(false)`！**
> 一旦后台线程池试图读取或写入 `DependencyProperty`、更新 `Visibility`、操作控件文本或调用 UI 服务（如 `SnackbarHelper`），WPF 将立即抛出 `InvalidOperationException: The calling thread cannot access this object because a different thread owns it`。在 `async void` 事件中，此异常将直接摧毁主进程。

#### 分层原则：
* **🔴 UI 层（绝对禁止）**：
  * 适用范围：`UniversalDeviceToolkit.WPF` 目录下的所有 `Pages/`、`Windows/`、`Controls/`、`ViewModels/` 以及任何包含 UI 绑定、事件处理（如 `Button_Click`、`Loaded`、`OnRefreshed`）的类。
  * 规则：所有的 `await` 调用**必须保持默认**（即保留同步上下文），不得额外添加 `.ConfigureAwait(false)`。
* **🟢 底层类库层（强烈推荐）**：
  * 适用范围：`UniversalDeviceToolkit.Lib/`、`UniversalDeviceToolkit.CLI/`、`UniversalDeviceToolkit.Lib.Automation/` 等无 WPF 依赖、无 UI 绑定的底层核心服务库。
  * 规则：所有的异步 I/O、硬件查询、文件读写 `await` 调用，**必须**追加 `.ConfigureAwait(false)`，以确保最大程度的并发性能与避免上层死锁。

---

### 2. 后台任务安全更新 UI 的标准范式
当底层的 `Task.Run`、定时器或硬件回调事件需要在后台线程向前台反馈进度或状态时，必须使用 UI 线程的分发器 (`Dispatcher`) 进行安全编组。

```csharp
// 推荐范式：使用 Dispatcher.BeginInvoke / Dispatcher.InvokeAsync
Task.Run(async () =>
{
    try
    {
        // 1. 在后台线程执行耗时操作/底层查询 (加上 .ConfigureAwait(false))
        var mi = await MachineCompatibility.GetMachineInformationAsync().ConfigureAwait(false);
        
        // 2. 切回 UI 线程更新界面
        await Dispatcher.InvokeAsync(() =>
        {
            _deviceInfoIndicatorText.Text = mi.Model;
            _deviceInfoIndicator.Visibility = Visibility.Visible;
        });
    }
    catch (Exception ex)
    {
        Log.Instance.Trace("Failed to update device info.", ex);
    }
});
```

---

### 3. `async void` 事件处理器的防御性编程
除了 WPF/WinForms 的顶级事件处理器（如 `Button_Click`、`Loaded`、`Unloaded`、`Closing`）外，绝对不要使用 `async void`。在事件处理器内部，必须用 `try-catch` 全面包裹保护。

> [!IMPORTANT]
> `async void` 方法内的未捕获异常无法被外层的调用者或 `Task` 捕获，它们会直接被抛到 `SynchronizationContext` 的顶层，导致整个程序崩溃。

```csharp
// 正确的事件处理器写法
private async void ScanButton_Click(object sender, RoutedEventArgs e)
{
    try
    {
        // 必须不加 ConfigureAwait(false)
        await ViewModel.ScanAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("Scan failed during button click.", ex);
        
        await SnackbarHelper.ShowAsync("Error", ex.Message, SnackbarType.Error);
    }
}
```

---

## 二、WMI 与硬件底层通信优化（防卡死与 RDP 优化）

### 1. 致命的 WMI 同步阻塞与远程桌面 (RDP) 锁死
软件在启动时会通过 WMI（Windows Management Instrumentation）查询 ACPI、系统 BIOS、硬件配置以及监听系统注册表主题变化。在以下场景中，同步 WMI 调用的底层内核等待会陷入**内核态死循环或超时长等待**：
* 通过远程桌面 (RDP) 连接计算机时；
* 安装了虚拟显卡驱动（如 GameViewer Virtual Display / Parsec / 剪映虚拟串流驱动）时；
* 联想笔记本处于特定的睡眠唤醒 / 节能状态时。

现象为：`ManagementObjectSearcher.Get()` 或 `ManagementEventWatcher.Start()` 同步卡死数十秒，CPU 内核态占用飙升，主窗口迟迟不弹窗甚至导致远程桌面画面假死。

---

### 2. WMI 改造黄金法则

#### 规则 A：完全弃用同步 WMI 查询，必须附加超时保护
禁止在代码中使用直接同步的 `mos.Get()`。必须通过异步 Task 包装，并使用 `CancellationTokenSource` 施加严格的超时限制（推荐 2500ms ~ 5000ms）。

```csharp
// 推荐范式：带超时的异步 WMI 查询封装
public static async Task<ManagementObjectCollection> GetAsyncWithTimeout(
    this ManagementObjectSearcher searcher, 
    int timeoutMs = 3000)
{
    using var cts = new CancellationTokenSource(timeoutMs);
    try
    {
        return await Task.Run(() => searcher.Get(), cts.Token).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
        Log.Instance.Warn($"WMI query timed out after {timeoutMs}ms: {searcher.Query.QueryString}");
        throw new TimeoutException($"WMI query timed out after {timeoutMs}ms.");
    }
}
```

#### 规则 B：禁用 WMI 监听系统事件，改用 Win32 API
对于系统主题（深色/浅色模式）切换、注册表值变化等长期监听任务，**禁止使用 `ManagementEventWatcher`**。改用 Win32 系统的原生的 `PInvoke.RegNotifyChangeKeyValue` 或窗口消息 `WndProc` (WM_SETTINGCHANGE)，内存和 CPU 开销降低 95% 以上，且完全不会阻塞启动。

---

## 三、后台轮询、传感器监控与磁盘 I/O 治理

### 1. 杜绝高频后台轮询中的日志轰炸 (Log Spamming)
硬件工具箱需高频轮询 CPU/GPU 功耗、风扇转速和温度传感器（通常每秒 1~2 次）。在高频执行的循环代码路径（如 `GetDataAsync`、`RefreshLoopAsync`）中：

> [!WARNING]
> **绝对禁止在高频轮询循环中输出常规 Trace / Debug 日志或格式化 JSON 字符串！**
> 之前我们在传感器获取循环中每次输出 `SensorsData` JSON 序列化字符串，导致在几个小时的运行内存里产生数 GB 的磁盘写入日志，引发严重的磁盘 I/O 放大、内存 GC 频繁回收以及系统卡顿。

* **整改标准**：只有在**状态发生重大改变**（如风扇模式切换、温度触发警告上限、硬件离线）或**捕捉到异常**时，才允许写入磁盘日志。正常轮询只需在内存中静默更新属性。

---

### 2. 性能计数器 (Performance Counters) 的冷却熔断机制
获取网络流量、磁盘读写等监控数据依赖 `System.Diagnostics.PerformanceCounter`。如果部分 Windows 系统（如精简版 Win10/11）损坏或缺失某些计数器，高频轮询会每秒触发并捕获多次底层异常，消耗大量 CPU。

* **规范**：必须对底层的监控源加入**冷却熔断时间（Cooldown Timer）**。一旦读取失败或抛出异常，立即捕获并将该监控项标记为禁用，挂起 30~60 秒后再尝试重建；如果在冷却期内，直接返回默认值（0），不再频繁发起底层调用。

---

### 3. CLI 进程与异步流重定向的资源回收
在通过 `Process` 调用命令行（如 `powercfg`、`nvidiacli` 等）并异步读取 `StandardOutput` / `StandardError` 时：
* 在处理任务取消 (`CancellationToken`) 或超时终止时，必须显式调用 `process.Kill(true)` 杀掉整个进程树。
* 必须正确等待异步读取流完成，避免在流未关闭前强行释放进程对象而抛出 `NullReferenceException`。

---

## 四、WPF UI 渲染与高频响应优化

### 1. 高频事件的防抖 (Debounce) 与节流 (Throttle)
对于用户调整滑动条（Slider）、输入搜索过滤文本、实时刷新折线图等高频触发 UI 事件：
* **禁止直接进行实时重绘或底层I/O调用**。
* **搜索/过滤文本框**：使用防抖（Debounce），等待用户停止按键 300ms ~ 500ms 后，再触发列表重载（参考 `WindowsOptimizationPage.Drivers.cs` 中的 `CancellationTokenSource` 防抖实现）。
* **高频数值监控界面**：使用节流分发器（`ThrottleFirstDispatcher` / `ThrottleLastDispatcher`），将 UI 刷新频率控制在最高每秒 3~5 帧以内，确保主界面丝滑不掉帧。

### 2. 消除视觉阴影与加速渲染瑕疵
在 WPF 自定义圆角卡片边框（如 `AppStatusBanner`、警告框）中，如果外围 `Border` 具有圆角且内部嵌套复杂布局，往往会看到周遭出现黑色矩形阴影或裁剪边缘。
* **规范**：在最外层的 `<UserControl>` 必须设置 `Background="Transparent"`；包裹圆角背景的容器必须设置 `BorderBrush="Transparent"` 与 `BorderThickness="0"`，防止硬件加速渲染时背景刷与裁剪区产生混色冲突。

---

## 五、面向 AI Agent 的批量排查与改造标准流程 (SOP)

为了让其他 AI Agent 能够自动、安全、批量地优化整个代码库，请指示它们严格遵循以下 **SOP 六步工作法**：

```mermaid
graph TD
    A[步骤 1: Grep 定位高危代码模式] --> B[步骤 2: 确定所属的分层边界]
    B --> C{是否在 WPF UI 层?}
    C -- 是 --> D[步骤 3: 严格移除 .ConfigureAwait(false)]
    C -- 否 --> E[步骤 3: 确保使用 .ConfigureAwait(false)]
    D --> F[步骤 4: 检查并修复 async void 异常保护]
    E --> F
    F --> G[步骤 5: 审计高频轮询与日志 I/O]
    G --> H[步骤 6: 自动化编译与单测验证]
```

### 1. 定位高危模式 (Grep Search)
Agent 必须优先使用高精度工具 `grep_search`（绝对不要在终端用 bash `grep` / `cat`）全库搜索以下特征：
* `ConfigureAwait\(false\)` （检查是否误用于 UI 层）
* `async void` （检查是否缺少 `try-catch` 或被用于非事件处理器）
* `ManagementObjectSearcher` / `ManagementEventWatcher` / `.Get\(\)` （检查是否有同步 WMI 调用）
* `Log.Instance.(Trace|Debug|Info)` （检查是否在高频轮询循环中输出）

### 2. 判定所属分层边界
* 读取文件绝对路径与顶层命名空间。
* 判定为 **WPF UI 层** (`*.WPF/Pages`, `*.WPF/Windows`, `*.WPF/Controls`, `*.WPF/ViewModels`) 还是 **底层类库** (`*.Lib`, `*.CLI`, `*.Macro`, `*.Automation`)。

### 3. 施加异步改造规则
* 如果在 UI 层且调用方直接操作 UI 元素、依赖属性或触发数据绑定：使用 `replace_file_content` 或 `multi_replace_file_content` 批量移除 `.ConfigureAwait(false)`。
* 如果在纯底层类库层：确保所有 `await` 均带上 `.ConfigureAwait(false)`。

### 4. 补全安全防护
* 为所有 `async void` 事件加上完善的 `try-catch` 结构，并在 `catch` 中通过 `SnackbarHelper` 提示用户，杜绝程序崩溃。
* 将所有的同步 WMI 查询替换为带超时包装的异步 `GetAsyncWithTimeout()`。

### 5. 高频 I/O 审计
* 检查 `Timer`、`While(true)` 循环、`Task.Delay` 轮询内部的日志输出代码。将每次循环必打的常规日志移除，改为状态变更触发。

### 6. 编译与单元测试验证
* 执行命令 `dotnet build <SLN_PATH> -c Debug`，确保 **0 Error** 且没有引入新的严重警告。
* 执行命令 `dotnet test`，运行自动化测试套件，保证核心逻辑无破损。

---

## 六、可以直接发给其他 Agent 的 Prompt 模板

你可以直接复制以下 Prompt 模板，分发给其他 AI Agent 执行批量重构与排查任务：

```markdown
<TASK_PROMPT>
你是一个资深的 .NET 10 + WPF 桌面应用程序架构师与性能优化专家。现在请你按照《Universal Device Toolkit 深度性能优化与 AI Agent 批量重构指南》，对指定的模块/项目进行批量审查和性能优化。

### 核心任务与约束：
1. **WPF 异步线程规则（最优先）**：
   - 严禁使用 bash `grep`，请使用高精度搜索工具（如 `grep_search`）扫描指定目录下所有文件中的 `ConfigureAwait(false)`。
   - 如果文件属于 WPF UI 层（包含 `UserControl`、`Window`、`Page`、`ViewModel`、事件处理器、操作 UI 控件/依赖属性/Snackbar），你**必须**批量移除其中的 `.ConfigureAwait(false)`，让代码返回 UI 同步上下文，防止抛出跨线程异常导致程序闪退！
   - 如果文件属于底层核心类库（没有 UI 依赖的纯服务/工具类），请确保其异步方法使用 `.ConfigureAwait(false)`。

2. **WMI 与同步阻塞改造**：
   - 检查代码中是否存在同步的 WMI 查询（如 `ManagementObjectSearcher.Get()`）或同步监听（`ManagementEventWatcher`）。
   - 将同步 WMI 查询改为异步并附加超时限制（建议 3000ms），防止在远程桌面 (RDP) 或虚拟显卡驱动下卡死主程序。
   - 对注册表和系统设置监听，优化为 Win32 `RegNotifyChangeKeyValue` 或异步防抖处理。

3. **异常捕获与后台 I/O 治理**：
   - 检查所有的 `async void` 事件处理器，确保内部有完整的 `try-catch` 结构保护，防止未捕获异常导致应用程序崩溃闪退。
   - 检查高频轮询代码（如传感器读取、风扇/功耗监控循环），彻底移除循环内的常规数据打印日志（`Log.Instance.Trace/Debug`），杜绝磁盘 I/O 飙升。

4. **安全编程与验证**：
   - 不要破坏代码中无关的既有注释、业务逻辑及语言本地化资源。
   - 每次重构完成后，务必使用 `run_command` 运行 `dotnet build` 检查编译错误，并确保编译通过（0 错误）。
</TASK_PROMPT>
```

---

## 七、对项目未来发展的建设性意见与架构规划

在深入排查与重构本项目的过程中，为确保项目的长远可维护性、高可用性及规范化，提出以下建设性意见与规划：

### 1. 架构改造：更严格的 MVVM 隔离与 UI 线程调度解耦
* **现状**：目前部分 UI 控件及 Page 页面中（如 `WindowsOptimizationPage`、各个 `Control` 内部）混合了非常多的业务逻辑、底层下载调用和直接的 `Dispatcher.Invoke` / `Dispatcher.BeginInvoke` 操作。这导致代码逻辑在 UI 线程与后台线程之间交织，极易引来线程安全隐患。
* **建议**：
  * 引入抽象层 **`IUiDispatcher` / `IUiScheduler`** 接口，在底层服务和 ViewModel 中只通过接口请求回到主线程，将 `Wpf.Dispatcher` 作为实现类在 IoC 容器中注入。
  * 这样不仅可以把 ViewModel 彻底同 WPF 视图解耦，大幅提升单元测试的覆盖率和自动化测试可行性，还能从根本上避免因为手动写错 `Dispatcher` 或 `ConfigureAwait` 导致的死锁与跨线程异常。

### 2. 自动化代码审查 (CI/CD) 与静态语法分析器 (Roslyn Analyzers)
* **现状**：开发人员或 Agent 在写异步代码时，容易凭习惯顺手加上 `.ConfigureAwait(false)`，或者使用同步的 WMI API，这些问题往往在特定硬件或 RDP 远程桌面下才会爆雷。
* **建议**：
  * 引入 **Roslyn 静态语法分析器（如 Meziantou.Analyzer / Microsoft.VisualStudio.Threading.Analyzers）**，并在项目的 `.editorconfig` 中配置自定义规则。
  * 在 CI 编译流水线（如 GitHub Actions）中强制启用规则：**当检测到 WPF 视图层调用了 `ConfigureAwait(false)` 或调用了同步阻塞 API 时，直接触发编译 Error**。从源头拦截高危代码的提交。

### 3. 增强并发与硬件模拟专项测试
* **现状**：目前项目拥有很好的单元测试基础（2340+ 单测），但大部分集中在纯逻辑、字符串序列化和工具类，对于并发竞态、多线程懒加载（如 `Lazy<T>` 缓存缓存淘汰）、硬件查询响应超时等场景覆盖较少。
* **建议**：
  * 编写针对硬件接口和底层服务的 **Mock 模拟层**，在测试中人为注入延迟（如模拟 WMI 阻塞 10 秒）与并发高频调用的测试用例，验证系统在高负载和极端网络/硬件环境下的容错熔断能力。
  * 针对单例缓存和硬件状态管理器，增加多线程并发读写的压力测试，确保竞态安全。

### 4. 硬件适配层的插件化与解耦 (Hardware Adapter Pattern)
* **现状**：作为通用设备工具箱，底层承载了大量针对 Lenovo Legion、ThinkPad 等不同机型、EC 寄存器、功耗墙、GPU 控制的特化逻辑。
* **建议**：
  * 进一步将硬件控制抽象为标准化的 **硬件适配器驱动接口 (Hardware Adapter / Provider Pattern)**。
  * 将联想特有的 WMI 接口、嵌入式控制器 (EC) 读写、光效控制明确封装在独立的 Provider 插件模块中。这样未来如果要扩展支持其他品牌（如华硕 ROG、机械革命、惠普等）的硬件控制，只需开发实现新的 Provider，而不必在核心主业务流程中不断增加 `if-else` 或设备兼容性判断，极大提升软硬件生态的扩展能力。
