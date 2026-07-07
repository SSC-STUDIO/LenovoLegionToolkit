# 告别 Vantage 臃肿：用 .NET 10 + WPF 构建开源 Legion 笔记本硬件控制工具

> Universal Device Toolkit (UDT) — 一个 GPL-3.0 开源项目，替代 Lenovo Vantage，支持 Legion/LOQ/IdeaPad Gaming 全系列及更多 PC 的基础模式。

---

## 起因：Vantage 太重了

每个 Legion 用户都经历过这个循环：

1. 装机，Vantage 自动启动
2. 发现它需要后台服务、联想账号、还有遥测
3. 想关掉它，但 Fn+Q、风扇曲线、独显切换都依赖它
4. 进退两难

Lenovo Legion Toolkit 曾经是解决方案，但它只支持 Legion 系列。当 LOQ、IdeaPad Gaming 甚至非联想 PC 用户想用插件和系统优化时，没有选择。

**Universal Device Toolkit** 把这个缺口填上了。它是一个 C# / WPF 桌面应用，运行在 .NET 10 上，零后台服务、零遥测、零账号。

![UDT 主界面](https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/Assets/Screenshot_main.png)

## 核心数据

| 指标 | UDT | Lenovo Vantage |
|---|---|---|
| 后台服务 | **无** | 必需 |
| 遥测/账号 | **无** | 必需 |
| 内存占用 | **~50-100 MB** | 200-400 MB |
| 启动时间 | **<2 秒** | 5-10 秒 |
| 开源 | **GPL-3.0** | 否 |
| 插件系统 | **支持** | 有限 |
| 非联想 PC | **基础模式** | 不支持 |

## 架构设计

```
UniversalDeviceToolkit.WPF       ← MVVM 展示层
UniversalDeviceToolkit.Lib       ← 34 个硬件控制器 + 服务
UniversalDeviceToolkit.CLI       ← 命令行/自动化 (llt.exe)
UniversalDeviceToolkit.Lib.Plugins  ← 插件 SDK
UniversalDeviceToolkit.Lib.Automation ← 自动化规则引擎
UniversalDeviceToolkit.Lib.Macro     ← 宏录制/回放
```

### 为什么选 WPF？

WPF 在2026年可能不是最时髦的选择，但它是唯一能同时做到：
- 原生 Windows 支持（Win10 1809+ / Win11）
- XAML 主题绑定（深色/浅色模式无切换延迟）
- GPU 加速渲染（仪表盘实时数据刷新）
- 成熟的 MVVM 生态（Autofac DI）

的技术栈。对于一个需要直接访问 WMI、ACPI、HID 硬件接口的工具来说，WPF 是正确答案。

### 插件系统

插件不是附加功能，而是一等公民：

- **Feature 插件**：添加新的自动化功能
- **Integration 插件**：对接第三方服务
- **Tool 插件**：独立工具（CPU 调优、GPU 信息、网络工具）

插件支持热重载、沙箱隔离、依赖管理，可以从应用内的「插件扩展」页面直接安装/更新/配置/卸载。

非联想 PC 在基础模式下运行插件、主题和系统优化，硬件控制开关自动隐藏。

## 三个核心工程挑战

### 1. WMI 远程桌面死锁

**问题**：同步 `ManagementObjectSearcher.Get()` 在 RDP 会话下会进入 ACPI 自旋等待，UI 冻结30秒。

**解决**：所有 WMI 查询强制异步 + 2500ms 超时：

```csharp
// 之前（危险）
var results = searcher.Get(); // UI 线程阻塞，RDP 下可能死锁

// 之后（安全）
var results = await searcher.GetAsync(timeoutMs: 2500);
```

项目规则：**UI 线程可达路径零同步 WMI 调用**。

### 2. WPF 线程安全

**问题**：ViewModel 中的 `.ConfigureAwait(false)` 会丢失 `SynchronizationContext`，后续 UI 属性访问抛出 `InvalidOperationException`。

**解决**：零 `.ConfigureAwait(false)`（WPF UI/ViewModel 范围内）。后台回调必须用 `Dispatcher.CheckAccess()` 守卫：

```csharp
if (_dispatcher.CheckAccess())
    RefreshUI();
else
    _dispatcher.InvokeAsync(RefreshUI);
```

### 3.78 种语言本地化

**问题**：XAML 里一个 `Text="OK"` 硬编码就破坏78种语言的体验。

**解决**：所有用户界面字符串必须引用 `Resource.resx`：

```xml
<!-- 之前 -->
<Button Content="OK" />

<!-- 之后 -->
<Button Content="{x:Static resources:Resource.OK}" />
```

目前152个 `.resx` 文件，覆盖78+语言。CI 验证所有语言文件的键完整性——缺少键是构建失败，不是运行时回退。

## 测试覆盖

```
UniversalDeviceToolkit.Tests:     2,327 通过 / 0 失败 / 30 跳过
UniversalDeviceToolkit.Plugin.Tests:    186 通过 / 0 失败
UniversalDeviceToolkit.CrossPlatform.Tests: 119 通过 / 0 失败
```

三层测试架构：
1. **单元测试**：控制器逻辑、服务行为、工具函数
2. **插件测试**：插件 SDK 契约验证、依赖解析
3. **跨平台测试**：平台无关代码路径

FlaUI + WinRT OCR 自动化 UI 验证：启动应用、验证窗口渲染、OCR 提取文本确认本地化正确。

## 安装方式

**Scoop（推荐）：**
```powershell
scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
scoop install ssc-studio/lenovolegiontoolkit
```

**GitHub Releases 下载安装包：**
https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest

## 如何贡献

1. 安装 [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. 克隆：`git clone https://github.com/SSC-STUDIO/UniversalDeviceToolkit.git`
3. 构建：`dotnet build UniversalDeviceToolkit.sln`
4. 测试：`dotnet test UniversalDeviceToolkit.Tests/UniversalDeviceToolkit.Tests.csproj`
5. 详见 [CONTRIBUTING.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/CONTRIBUTING.md)

插件开发文档：[PLUGIN_DEVELOPMENT.md](https://github.com/SSC-STUDIO/UniversalDeviceToolkit/blob/master/Docs/PLUGIN_DEVELOPMENT.md)

## 相关链接

- **GitHub**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
- **最新版本**: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
- **协议**: GPL-3.0
- **技术栈**: C# / WPF / .NET 10 / Autofac / WMI / ACPI / HID

---

*如果 UDT 让你的 Legion 跑得更轻，一个 Star 能帮更多人找到它——也说明插件生态值得继续投入。*

**#开源 #Lenovo #Vantage替代 #WPF #.NET10**
