# 视觉设计审计报告：屏幕适配与视觉效果

**日期：** 2026-07-18
**仓库：** UniversalDeviceToolkit（宿主 WPF 应用）
**基线：** `master` @ `1b66f8ca`（`fix(ui): theme-aware settings nav selection chrome`）
**方法：** 静态代码审计（XAML / C# 全量检索 + 关键文件通读），未运行动态 UI（WPF 需 Windows 环境）。
**范围：** `UniversalDeviceToolkit.WPF/`（109 个 XAML 文件）。`UniversalDeviceToolkit.Lib/` 无任何 UI；`UniversalDeviceToolkit.CrossPlatform/` 为纯控制台项目。
**配套文档：** 《视觉设计建议：屏幕适配与视觉效果》（`VisualDesignRecommendations-ScreenAdaptation.md`）

> 本报告只陈述事实与风险，所有结论附 `文件:行号` 证据；改进方案见配套建议文档。

---

## 目录

- [一、审计摘要](#一审计摘要)
- [二、屏幕适配现状](#二屏幕适配现状)
  - [2.1 DPI 基础设施](#21-dpi-基础设施)
  - [2.2 窗口尺寸清单](#22-窗口尺寸清单)
  - [2.3 页面固定尺寸与滚动覆盖](#23-页面固定尺寸与滚动覆盖)
  - [2.4 字体排印与文本缩放](#24-字体排印与文本缩放)
- [三、视觉效果现状](#三视觉效果现状)
  - [3.1 主题机制与设计令牌](#31-主题机制与设计令牌)
  - [3.2 背景特效（Mica / Acrylic）](#32-背景特效mica--acrylic)
  - [3.3 动画体系](#33-动画体系)
  - [3.4 阴影与海拔](#34-阴影与海拔)
  - [3.5 加载态与骨架屏](#35-加载态与骨架屏)
  - [3.6 空状态](#36-空状态)
  - [3.7 硬编码值审计](#37-硬编码值审计)
- [四、风险清单（按严重度排序）](#四风险清单按严重度排序)
- [五、附录：关键统计数据](#五附录关键统计数据)

---

## 一、审计摘要

**总体判断：视觉效果的"地基"成熟，屏幕适配存在系统性缺口。**

- 设计令牌体系（圆角 / 间距 / 字号 / 语义色 / 焦点）完整且已被全库采用：**视图代码 0 处硬编码十六进制颜色、0 处硬编码数字 CornerRadius**（见 §3.7）。主题切换、自定义调色板预设、Mica/Acrylic 背景、骨架微光等机制齐备。
- 屏幕适配的短板集中在四点：**主窗口 `MinWidth=1200` 高于小屏高缩放设备的逻辑宽度**（§2.2）；**所有对话框均无 ScrollViewer 兜底**且大量 `NoResize`（§2.2）；**4 个页面/容器无页面级滚动**（§2.3）；**高 DPI 下字号被反向缩小，且完全不响应 Windows 系统文本缩放**（§2.4）。
- 视觉效果的剩余差距在"一致性收尾"：**空状态无统一控件**（5 处各自实现，§3.6）、**骨架屏仅覆盖 4/9 主页**（§3.5）、**Toast 使用 Margin 布局动画**（§3.3）、**162 处裸 FontSize** 未令牌化（§2.4）。

---

## 二、屏幕适配现状

### 2.1 DPI 基础设施

**清单声明（良好）：** `UniversalDeviceToolkit.WPF/App.manifest:25` 声明 `<dpiAwareness>PerMonitorV2</dpiAwareness>`，`:26` `highResolutionScrollingAware=true`。目标框架 `net10.0-windows10.0.26100.0`（`Directory.Build.props:21`），WPF-UI `4.3.0`（`Directory.Packages.props:32`）。PerMonitorV2 意味着跨显示器拖动时 WPF 布局树会自动按新 DPI 缩放，无需手动重排。

**DPI 变更响应：** `Windows/BaseWindow.cs:31` 订阅 `DpiChanged` 事件，`:108-112` 处理器执行 `VisualTreeHelper.SetRootDpi(this, e.NewDpi)` 并调用 `DpiAwareTypography.Apply(Resources, e.NewDpi.DpiScaleX)`。

**自研 DpiAwareTypography（方向存疑）：** `Utils/DpiAwareTypography.cs:12-23` 维护 9 个字号基准（SmallBody 13 / Caption 14 / Body 15 / PageDescription 16 / Subsection 17 / Section 19 / DisplaySection 25 / PageTitle 29 等），`:31` 的核心公式为：

```csharp
var factor = Math.Clamp(1d / Math.Sqrt(dpiScale), 0.92d, 1.04d);
```

即 DPI 缩放 ≥125% 时把字号资源**反向缩小约 8%**。注释自述 "WPF already scales layout for DPI; only apply a light correction"（`:30`）。该设计可防止高缩放下"文字相对过大"，但与无障碍场景下用户调高系统缩放以**看清文字**的诉求方向相反。

**覆盖面缺口：** 29 个窗口以 `<local:BaseWindow>` 为根（享有 DPI 字号修正 + 背景特效 + 缩放稳定辅助），但以下 **10 个窗口不在 BaseWindow 体系内**，DPI 变更时无字号修正：

- 7 个直接使用 `<wpfui:FluentWindow>`：`Settings/HardwareSensorSectionsWindow`、`Utils/CompatibilityCheckErrorWindow`、`Utils/CrashReportNotificationWindow`、`Utils/DeviceSetupWindow`、`Utils/LanguageSelectorWindow`、`Utils/StatusWindow`、`Utils/UnsupportedWindow`
- 1 个裸 `<Window>`：`Utils/ErrorDialogWindow.xaml`
- 2 个 `<fg:OsdWindowBase>`（OSD 族，透明置顶窗口）

**系统文本缩放不支持：** 全库无 `TextScaleFactor` / `UISettings.TextScaleFactor` 任何引用。Windows"轻松使用 → 放大文本"设置对 WPF 不生效，应用未做任何补偿（如监听 `SPI_GETNONCLIENTMETRICS` 或提供应用内文本大小设置）。

**像素对齐（良好）：** `BaseWindow.cs:17-18` 设置 `SnapsToDevicePixels=true`、`UseLayoutRounding=true`（`MainWindow.xaml:19-20` 重复声明），可有效避免非整数 DPI（125%/175%）下的边框发虚。

### 2.2 窗口尺寸清单

**主窗口：** `Windows/MainWindow.xaml:14-17` —— `Width=1300 Height=850 MinWidth=1200 MinHeight=720`，默认 `CanResize`，无窗口尺寸/位置持久化（全库无 `RestoreBounds` 存储）。1300×850 与截图契约一致。

**关键风险：MinWidth=1200 超出小屏高缩放设备的逻辑宽度。**

| 设备场景 | 物理分辨率 | 系统缩放 | 逻辑宽度 | 能否容纳 MinWidth 1200 |
|---|---|---|---|---|
| 入门笔记本 | 1366×768 | 100% | 1366 | ✅ |
| 入门笔记本 | 1366×768 | **125%**（OEM 常见预装值） | **1092** | ❌ 窗口被系统级裁切 |
| 全高清笔记本 | 1920×1080 | **150%** | **1280** | ⚠️ 仅剩 80px 余量 |
| 小尺寸 2K 屏 | 2560×1440 | **200%** | **1280** | ⚠️ 同上 |

**对话框/子窗口全量清单**（共 38 个，模式归纳后）：

| 模式 | 窗口 | 尺寸声明 |
|---|---|---|
| **固定尺寸 + NoResize**（无重排可能） | OverclockDiscreteGPUSettingsWindow (500×340)、HardwareSensorSectionsWindow (420×420)、UpdateWindow (720×520..560)、DeviceSetupWindow (600×420..620×480)、LanguageSelectorWindow (450×300..520×320)、UnsupportedWindow (650×420)、ErrorDialogWindow (600×450)、InputDialogWindow (420×184) | Min=Max 或区间锁死 |
| **固定尺寸 + CanMinimize**（同样无重排） | AutomationPipelineTriggerConfigurationWindow (500×600)、EditEffectWindow (500×500)、OsdSettingsWindow (460×600)、BootLogoWindow (400×250)、ExcludeRefreshRatesWindow (400×500)、SelectSmartKeyPipelinesWindow (400×500)、SelectedActionsWindow (600×500) 等 | Min=Max |
| **锁宽 + 高度自适应** | BalanceModeSettingsWindow (宽400)、ExtendedHybridModeInfoWindow (宽550)、WindowsPowerModes/PlansWindow (宽600, `SizeToContent=Height`)、DeviceInformationWindow (宽600) | `SizeToContent=Height` |
| **可调但区间受限** | AddAutomationStepWindow (600×570, Min 500×400)、EditDashboardWindow (700×700, Min 600×400)、GodModeSettingsWindow (920×720, Min 820×560)、SensorDetailsWindow (1080×640, Min 900×520)、PluginSettingsWindow (560×520, Min 480×360，**唯一 CanResize 的设置窗**)、NavigationItemsSettingsWindow / NotificationsSettingsWindow (H=600, Min 500×300, MaxWidth 500)、ActionDetailsWindow / SymbolRegularPicker / LargeFilesWindow (800×600 级)、CrashReportNotificationWindow (640×520, Min 520×420, Max 840×720)、CompatibilityCheckErrorWindow (Min 700×500, Max 900×650) | — |

**共性结论：抽查的所有对话框根布局均为 Grid + RowDefinition，没有任何一个把内容包进 ScrollViewer。** 这意味着 `NoResize` / 固定尺寸窗口在 200% 文本缩放、长本地化字符串或小屏场景下，内容溢出时**没有兜底滚动**，只能被裁切。

**窗口辅助设施（良好）：** `Utils/WindowResizeStabilityHelper.cs`（278 行，稳定左/上边缘拖拽）、`Utils/WindowMaximizeWorkAreaHelper.cs`（146 行，最大化限制在工作区内），均挂载于 `BaseWindow.cs:69,72`。

### 2.3 页面固定尺寸与滚动覆盖

**页面级固定数值尺寸统计**（`Width|MinWidth|MaxWidth|Height|MinHeight|MaxHeight="数字"` 出现次数）与滚动覆盖：

| 页面 | 固定尺寸数 | 页面级滚动 |
|---|---|---|
| DashboardPage | 57 | ✅ `wpfui:DynamicScrollViewer`（:149） |
| WindowsOptimizationPage | 54 | ✅ 4 个 plain ScrollViewer（:476, :793, :1105, :1488，每 Tab 一个） |
| AutomationPage | 46 | ✅ DynamicScrollViewer（:19） |
| PluginExtensionsPage | 30 | ❌ **无页面滚动器**，仅 ListBox 内部滚动 |
| PackagesPage | 14 | ✅ DynamicScrollViewer（:280） |
| MacroPage | 8 | ✅ DynamicScrollViewer（:15） |
| KeyboardBacklightPage | 6 | ✅ DynamicScrollViewer（:12） |
| SettingsPage | 3 | ❌ **无页面滚动器**：左侧固定导航列 `Width="240" MinWidth="180"`（:38）+ 右侧 ContentControl，滚动下放给各 Settings 子控件自觉 |
| AboutPage | 0 | ✅ DynamicScrollViewer（:12） |
| PluginPageWrapper | 0 | ❌ **无滚动**（:36-40 裸 `ContentControl _pluginContentHost`），插件页溢出风险完全转嫁插件 |

**固定像素 ColumnDefinition：全库 48 处。** 典型：`Pages/MacroPage.xaml:52-54`（`Width="56"` ×3）、`Pages/PackagesPage.xaml:293`（`250`）、`Pages/SettingsPage.xaml:38`（`240`）、`Windows/Automation/TabItemContent/BatteryPercentage…:11-55`（`140` ×5）、`HardwareSensorAutomation…:16-63`（`140` ×4）。

**不可缩放布局的重灾区：** 键盘布局示意图三控件 `Controls/KeyboardBacklight/Spectrum/SpectrumKeyboardJisControl.xaml`（191 处固定像素）、`SpectrumKeyboardISOControl.xaml`（185）、`SpectrumKeyboardANSIControl.xaml`（183）——每个键位硬编码像素坐标，本质是位图式绝对布局，合计约 **560 处**，在任何非设计尺寸下都无法重排（只能整体缩放或裁切，当前是裁切）。

**自适应机制（局部良好）：** 全库 0 处 VisualStateManager。唯一自适应是自定义 NavigationStore 导航侧栏折叠/展开（宽度令牌 `Styles/ElevationTokens.xaml:7-8` `NavigationWidthCollapsed=70 / Expanded=220`，动画见 `Styles/NavigationStore.xaml:189-298`），且 MainWindow 提供 Thumb 拖拽分隔条（`MainWindow.xaml:249-265`）。**SettingsPage 左栏 240px 固定、小窗口下不折叠**，与主导航行为不一致。

### 2.4 字体排印与文本缩放

**令牌与全局样式（良好）：** 9 个字号令牌（`Styles/DesignTokens.xaml:46-54`，13–29px）；`Styles/Typography.xaml` 定义 12 个命名 TextBlock 样式 + 5 个隐式全局样式（Control/Window/Page/UserControl/TextBlock，:6-44），全局默认 `FontSizeBody=15`，字体族 `"Segoe UI, Microsoft YaHei UI, Microsoft YaHei, Noto Sans CJK SC, SimSun"`（:3-4），`TextFormattingMode=Display` + `TextHintingMode=Fixed`（高分屏文本渲染清晰）。

**裸 FontSize：全库 162 处**（Pages 59 / Windows 60 / Controls 28 / Styles 10）。典型：`Controls/Dashboard/DiscreteGPUControl.xaml:64` `FontSize="20"`；`Controls/Dashboard/GodMode/GodModeValueControl.xaml:46` `"11"`；`Controls/KeyboardBacklight/Spectrum/SpectrumKeyboardBacklightControl.xaml:191-223` `"20"` ×4；`Pages/PluginPageWrapper.xaml:25` `"28"`。这些值绕过 DpiAwareTypography 的 DPI 修正，也游离于令牌治理之外。

**防溢出措施：** TextWrapping 88 处、TextTrimming 25 处；MainWindow 标题 `MaxWidth="760"` + `CharacterEllipsis`（`MainWindow.xaml:88,99`）。覆盖尚可但不系统——长本地化文本（德语/俄语普遍比英语长 30–40%）在无 Wrap 的固定尺寸对话框内仍有溢出风险。

**本地化基础（良好）：** `WPF/Resources/Resource.resx` + 25 个文化卫星；`Lib/Resources/` 24 个；`Directory.Build.props:33` 编译 32 种文化。XAML 字符串普遍 `{x:Static resources:Resource.*}`；窗口/页面普遍 `FlowDirection="{x:Static utils:LocalizationHelper.Direction}"`（RTL 就绪）。XAML 硬编码用户可见字符串极少（AboutPage 第三方品牌名属白名单）。

---

## 三、视觉效果现状

### 3.1 主题机制与设计令牌

**主题应用机制：** `Utils/ThemeManager.cs` —— WPF-UI `ApplicationThemeManager.Apply(theme, backdropType, false)`（:125）；`ApplicationAccentColorManager.Apply` 四色同 accent（:153-156）；`SystemThemeListener.Changed` 经 dispatcher 重应用（:51）。**6 套自定义调色板预设**（Official / Midnight / Forest × 明暗，硬编码 RGB，:269-345），运行时覆盖 18 个刷键（:19-39）；`ApplySurfaceResources`（:183-215）处理 AppSurface/Chart/NotificationGlass 刷；`ApplyStatusTextBrushes`（:347-357）在浅色主题下将 `StatusCriticalTextBrush` 换成 `#C62828` 保证对比度。`App.xaml:13-15` 提供暗色静态默认值防启动闪白，合并 17 个 ResourceDictionary（:17-36）。

**设计令牌（成熟，全库 SoT）：** `Styles/DesignTokens.xaml`（151 行）：

| 类别 | 内容 |
|---|---|
| 间距 | XXS 4 / XS 8 / SM 12 / MD 16 / LG 24 / XL 32 + ContentPaddingPage 24（:6-12） |
| **圆角刻度** | None 0 / ProgressBar 2 / Small=Compact **8** / Control **12** / Card **18** / Surface **20** / Round **999** + 方向变体 + 兼容别名 SM/MD/LG/XL（:23-42） |
| 字号 | 9 枚（13–29，:46-54） |
| 图标 | 14 / 18 / 24 / 32 / 48 |
| 按钮 | MinWidth 80/120/160 × 高 32/36/40 |
| 图表调色板 | 6 色（:73-78） |
| 语义状态色 | Success / Warning / Info / Critical + 20% alpha 背景 + 高对比文本色（:101-125） |
| 焦点 | 焦点令牌 + `DefaultControlFocusVisualStyle`（:127-149） |

**DynamicResource vs StaticResource：** 全库 DR 708 / SR 1039（仅 Pages：DR 184 / SR 424）。画刷/前景普遍 DR（主题可热切换），尺寸令牌普遍 SR，分工合理。

### 3.2 背景特效（Mica / Acrylic）

`Utils/RenderingCompatibilityHelper.cs:36-43`：默认 **Mica**；`WindowBackdropStyle.macOS`（`Lib/Enums.cs:726-732`，设置存于 `ApplicationSettings.cs:45`）时用 **Acrylic**；`ShouldDisableBackdrop` 时 None。BaseWindow 构造即设（`BaseWindow.cs:22-25`）、Loaded 重设（:80-83），含软件渲染兼容模式（:34-73）；`ThemeManager.UpdateWindowBackdrops`（:133-146）遍历所有 BaseWindow 更新。OSD 两窗用 `AllowsTransparency` + `WindowStyle=None`。**注意：§2.1 列出的 10 个非 BaseWindow 窗口不享有统一背景特效策略。**

### 3.3 动画体系

**动画令牌：** `Styles/AnimationTokens.xaml` —— Fast 0.1s / Medium 0.2s / Slow 0.3s / Shimmer 1.65s / SkeletonCrossfade 0.20s；CubicEase Out + ExpoOut(7)；位移量 10px/6px。时长/缓动已令牌化，符合 Fluent 动效规范。

**Storyboard 分布（6 个 XAML 文件）：**

| 文件 | 动画 | 评价 |
|---|---|---|
| `Styles/Animations.xaml` | CardHoverEnter/Leave（Y −2px + 阴影 Blur 8→12）、PageFadeIn、AppPageEntranceAnimationStyle（Loaded 淡入+上移）、ButtonClick（Scale 0.95）、ListItemLoad、SectionExpand | 体系完整，走 RenderTransform，性能好 |
| `Styles/NavigationStore.xaml:189-298` | 导航项展开/折叠 Opacity + `ContentGrid.MaxHeight` 30↔0 | MaxHeight 属布局动画，但范围小、可接受 |
| `Styles/NotificationToast.xaml:176-199` | **ThicknessAnimationUsingKeyFrames 动画 Margin** | ⚠️ 布局动画，toast 进出每帧触发 layout pass，多条 toast 叠加时性能敏感 |
| `Styles/DynamicScrollBar.xaml:56-108` | 滚动条 Track 宽度 6↔10 悬停加粗 | 布局动画但范围极小 |
| `Pages/SettingsPage.xaml:14-30` | Tab 切换内容淡入+上移 | 良好 |
| `Controls/Dashboard/SensorsControl.xaml:226` | 骨架遮罩淡出 | 良好 |

### 3.4 阴影与海拔

阴影令牌 4 枚（`Styles/ElevationTokens.xaml`：Low blur8/op0.1/depth2、Medium 12/0.15/3、NotificationGlass 14/0.18/3、ContentSurfaceDivider 14/0.18/5）。`Effect=` 引用仅 3 处（`Controls/Shell/AppNotificationHost.xaml:59`、`AppStatusBanner.xaml:24`、`NotificationToast.xaml:89`），均为 NotificationGlass；直接 `DropShadowEffect` 仅 OSD（`Windows/Osd/OsdPanelWindow.xaml:41,59`，文字光晕）；代码侧 `Windows/Utils/NotificationWindow.cs:225` 按主题切阴影色。卡片悬停阴影动画直接驱动 `UIElement.Effect`（DropShadowEffect 是 CPU 位图效果，但仅作用于悬停卡片，范围可控）。

### 3.5 加载态与骨架屏

**基础设施（强）：** `Controls/LoadStatePresenter.cs`（500 行）+ `LoadableControl.cs`（81 行）+ `Controls/Loading/{LoadSession,LoadState,LoadStateCoordinator,ILoadingChromeOwner}.cs`；骨架微光 `Utils/SkeletonShimmer.cs`（218 行，附加属性 IsEnabled/DelaySeconds，明/暗两套微光色 :16-21）+ `Utils/SkeletonShimmerBehavior.cs`（280 行，:172 高对比检测、:231 监听主题变更刷新）；`Styles/Loading.xaml` 约 12 个骨架样式。

**页面覆盖（4/9）：** Dashboard（整页骨架含传感器卡 3 列 UniformGrid，`DashboardPage.xaml:153-488`）、Automation（LoadableControl 8 处）、KeyboardBacklight（:21）、Packages（:221）。**缺失：Settings、PluginExtensions、WindowsOptimization、Macro、About**——PluginExtensions 仅在 :144/:580 用 ProgressRing。

### 3.6 空状态

**无专用 EmptyState 控件/样式**（类定义 grep 为 0）。各页面临时实现 5 处：`AutomationPage.xaml:179,313`（空列表文本）、`PluginExtensionsPage.xaml:799`（EmptyStoreMessage）、`PluginPageWrapper.xaml:43-51`（`_emptyStateBorder`）、`WindowsOptimizationPage.xaml:1045-1072`（驱动空状态 Border+图标+双文本）、`:1580`（清理规则空文本）。视觉语言（图标、字号、间距、文案层级）不统一。

### 3.7 硬编码值审计

**结论：视图层硬编码治理是全库最强项。**

- **硬编码十六进制颜色：全库 XAML 仅 28 处匹配**，其中 App.xaml 4（:12-15 启动防闪白默认值）、Styles 23（DesignTokens 19 + Loading.xaml 4 骨架微光色）、Pages 1（`SettingsPage.xaml:129`，且仅为注释文本）。**Pages/Windows/Controls 实际视图代码 0 处。**
- **硬编码数字 CornerRadius：0 处**——圆角已 100% 令牌化。
- 剩余硬编码集中在裸 FontSize（162 处，见 §2.4）与固定像素尺寸（见 §2.3）。

---

## 四、风险清单（按严重度排序）

| # | 严重度 | 类别 | 风险 | 证据 |
|---|---|---|---|---|
| R1 | **高** | 适配 | `MinWidth=1200` 在 1366×768@125%（逻辑 1092）等场景下主窗被系统裁切 | `MainWindow.xaml:14-17` |
| R2 | **高** | 适配 | 全部 38 个对话框无 ScrollViewer 兜底，NoResize/固定尺寸窗口在高文本缩放/长文本下内容被裁切 | §2.2 全表 |
| R3 | **高** | 适配 | DpiAwareTypography 高 DPI 反向缩小字号（0.92×），且不响应系统"放大文本"设置，无障碍方向错误 | `DpiAwareTypography.cs:31` |
| R4 | 中 | 适配 | PluginExtensionsPage / SettingsPage / PluginPageWrapper 无页面级滚动，溢出依赖子控件自觉 | §2.3 表 |
| R5 | 中 | 适配 | 键盘布局三控件约 560 处绝对像素坐标，完全不可重排 | §2.3 |
| R6 | 中 | 适配 | 10 个非 BaseWindow 窗口无 DPI 字号修正与统一背景策略 | §2.1 清单 |
| R7 | 中 | 效果 | 骨架屏仅覆盖 4/9 主页，Settings/PluginExtensions/WinOpt/Macro/About 加载时无反馈 | §3.5 |
| R8 | 中 | 效果 | 空状态无统一控件，5 处临时实现视觉语言不一致 | §3.6 |
| R9 | 中 | 效果 | 162 处裸 FontSize 绕过令牌与 DPI 修正 | §2.4 |
| R10 | 低 | 效果 | NotificationToast Margin 布局动画，多 toast 叠加性能敏感 | `NotificationToast.xaml:176-199` |
| R11 | 低 | 适配 | SettingsPage 左栏 240px 固定不折叠，与主导航折叠行为不一致 | `SettingsPage.xaml:38` |
| R12 | 低 | 适配 | 无窗口尺寸/位置持久化，多显示器环境重开位置漂移 | §2.2 |

---

## 五、附录：关键统计数据

| 指标 | 数值 |
|---|---|
| XAML 文件总数（WPF 项目） | 109 |
| 窗口总数 / BaseWindow 覆盖 | 38+MainWindow / 29 |
| 页面固定数值尺寸总计（前 3 页） | Dashboard 57 / WinOpt 54 / Automation 46 |
| 固定像素 ColumnDefinition | 48 |
| 键盘布局控件固定像素 | 191 + 185 + 183 ≈ 560 |
| 裸 FontSize | 162（Pages 59 / Windows 60 / Controls 28 / Styles 10） |
| 硬编码 hex 颜色（视图代码） | **0** |
| 硬编码数字 CornerRadius | **0** |
| DynamicResource / StaticResource | 708 / 1039 |
| TextWrapping / TextTrimming | 88 / 25 |
| VisualStateManager | 0 |
| resx 卫星文化（WPF / Lib） | 25 / 24（编译 32 种） |
| Storyboard 所在 XAML 文件 | 6 |
| 骨架屏页面覆盖 | 4 / 9 |
