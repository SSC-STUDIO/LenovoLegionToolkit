# 视觉设计审计报告：屏幕适配与视觉效果

**日期：** 2026-07-18
**仓库：** UniversalDeviceToolkit-Plugins（官方插件生态）
**基线：** `master` @ `4d07092`（`refactor(ui): align plugin chrome with shared design tokens`），宿主基线 UDT v5.0.0
**方法：** 静态代码审计（仓库内全部 10 个 XAML 文件通读 + C# fallback UI 全量检索），未运行动态 UI。
**范围：** `Plugins/`（CustomMouse、ShellIntegration、ViveTool、Shared）、`Tools/`（PluginWorkbench、PluginCompletionUiTool）、`Templates/`、`SDK/`。
**配套文档：** 《视觉设计建议：屏幕适配与视觉效果》（`VISUAL_DESIGN_RECOMMENDATIONS.md`）

> 本报告只陈述事实与风险，所有结论附 `文件:行号` 证据；改进方案见配套建议文档。

---

## 目录

- [一、审计摘要](#一审计摘要)
- [二、共享设计令牌体系现状](#二共享设计令牌体系现状)
- [三、屏幕适配现状](#三屏幕适配现状)
- [四、视觉效果现状](#四视觉效果现状)
- [五、双 UI（XAML + C# fallback）漂移问题](#五双-uixaml--c-fallback漂移问题)
- [六、规范真空](#六规范真空)
- [七、风险清单（按严重度排序）](#七风险清单按严重度排序)
- [八、附录：关键统计数据](#八附录关键统计数据)

---

## 一、审计摘要

**总体判断：主题一致性实际不错（插件 XAML 零 hex 颜色、零数字 CornerRadius、零硬编码字符串），但令牌体系"半死不活"、fallback UI 双份漂移、动画/阴影/DPI 全面缺席。**

- 共享令牌字典（`Plugins/Shared/DesignTokens.xaml` + `PluginUiStyles.xaml`）存在且与宿主圆角刻度对齐（8/12/18/20），但**只有 1/3 插件真正合并使用**：ShellIntegration 全量、ViveTool 一半（主页面复制粘贴令牌而非合并）、CustomMouse 完全不用（§2）。
- `PluginUiStyles.xaml` 9 个样式中 **5 个全仓库零引用**（死代码），被引用的 4 个也只在 ShellIntegration 内（§2.3）。
- 硬编码集中在**裸 FontSize（XAML 40 处 + C# 17 处）**与 **ViveTool DataGrid 固定像素**（§4.4）。
- **全仓库无 Storyboard、无阴影、无 DPI 清单/代码**；加载态与空态仅 ViveTool 具备（且其 C# fallback 版空态缺图标，与 XAML 版视觉不一致）（§4.2/§4.3）。
- 每个控件强制维护的 `BuildFallbackUi`（C# 重建 UI）与 XAML 是两套实现，值已明显漂移（§5）。
- 圆角/间距/字号/阴影/动画/DPI/空态/加载态**无任何成文规范**，唯一写下的词汇表规则在 `DesignTokens.xaml:4-11` 的注释里（§6）。

---

## 二、共享设计令牌体系现状

### 2.1 `Plugins/Shared/DesignTokens.xaml`（53 行，已通读）

头部注释声明与宿主对齐："values must match UDT host … Compact 8 / Control 12 / Card 18 / Surface 20 / Round 999"（:4-11）。实际定义：

| 类别 | 键 | 值 |
|---|---|---|
| CornerRadius ×12 | None / ProgressBar / Small=Compact / Control / Card / Surface / Round + SM/MD/LG/XL 别名 | 0 / 3 / 8 / 12 / 18 / 20 / 999（:14-25） |
| Plugin 别名 ×4 | PluginCornerRadiusCompact/Control/Card/Surface | 8/12/18/20（:28-31；注释提及 NetworkAcceleration/BatteryHealth——两插件已迁出本仓库，注释已过时） |
| 间距 ×6 | XXS/XS/SM/MD/LG + PluginCardPadding / PluginSectionMargin | 4/8/12/16/24 + `16,14` / `0,0,0,16`（:34-40） |
| 按钮 ×2 | ButtonHeightCompact / Standard | 32 / 36（:43-44） |
| 字号 ×4 | PluginFontSizeCaption / Body / Section / Metric | 12 / 14 / 15 / 28（:47-50） |
| 图标 ×2 | PluginIconSizeMD / LG | 18 / 24（:51-52） |

### 2.2 `Plugins/Shared/PluginUiStyles.xaml`（74 行，已通读）

9 个 Style，全部基于 DynamicResource 主题画刷 + StaticResource 令牌：`PluginCardBorderStyle`（Card 18，:10）、`PluginSurfaceBorderStyle`（:20）、`PluginMetricCardStyle`（Control 12，:25）、`PluginSectionTitleTextStyle`（:34）、`PluginSectionDescriptionTextStyle`（:41）、`PluginMetricLabelTextStyle`（**自身硬编码 FontSize=11**，:49）、`PluginMetricValueTextStyle`（:54）、`PluginStatusTextStyle`（:60）、`PluginEmptyStatePanelStyle`（:66）。

### 2.3 合并情况（核心问题）

| 插件 | 合并 DesignTokens | 合并 PluginUiStyles | 实际使用 |
|---|---|---|---|
| ShellIntegration | ✅（`ShellIntegrationSettingsControl.xaml:16-17`） | ✅ | 引用 4 个样式（PluginCardBorderStyle ×4、SectionTitle ×4、SectionDescription ×3、StatusText ×1）——**唯一真正使用者** |
| ViveTool | 仅 SettingsPage（`ViveToolSettingsPage.xaml:16`） | ❌ | **ViveToolPage.xaml 完全不合并共享字典**，而在自己的 Resources 里**重复内联定义**同样的 4 个 PluginCornerRadius 键（:21-24）——SoT 被复制 |
| CustomMouse | ❌ | ❌ | 全部裸数字 |

**死代码：** `PluginSurfaceBorderStyle`、`PluginMetricCardStyle`、`PluginMetricLabelTextStyle`、`PluginMetricValueTextStyle`、`PluginEmptyStatePanelStyle` 全仓库零引用（5/9）。

**第三套来源：** `Tools/PluginWorkbench` 通过代码加载宿主的 `Styles/DesignTokens.xaml`（`PluginWorkbenchThemeService.cs:23`），与插件 Shared 字典是两套来源，值若漂移互不可见。

---

## 三、屏幕适配现状

### 3.1 DPI 处理：全仓库为零

- 无 app.manifest、无 dpiAware/PerMonitor 声明、无 DpiHelper/DpiScale 代码（grep 仅命中 ViveTool 捆绑的 `FeatureDictionary.pfs` 无关数据行）。
- 唯一的像素对齐措施：`ViveToolPage.xaml:14-18` 设置 `SnapsToDevicePixels` / `UseLayoutRounding` / `TextFormattingMode=Display`。
- **影响面评估：** 插件主 UI 是 UserControl，由宿主（PerMonitorV2）承载，DPI 缩放布局由 WPF/宿主兜底，缺口相对可控；但 **Tools 下两个独立 exe（PluginWorkbench、PluginCompletionUiTool）无 manifest 时按系统 DPI 感知默认值运行**（.NET 自带 app.manifest 模板未启用 PerMonitorV2 时为 system-DPI aware），在 PerMonitor 场景会被位图拉伸发虚。

### 3.2 窗口清单（插件 1 个 + Tools 3 个）

| 窗口 | 尺寸 | ResizeMode / SizeToContent | 滚动 |
|---|---|---|---|
| `Plugins/ShellIntegration/ShellIntegrationStyleSettingsWindow.cs:21-24` | 880×720，Min 640×520 | 默认 CanResize | ✅ 代码创建 ScrollViewer（:77-81） |
| `Tools/PluginWorkbench/MainWindow.xaml:7-10` | 1540×940，Min 1180×760 | 默认 | 局部 |
| `Tools/PluginWorkbench/HostedPluginContentWindow.xaml:5-8` | 1160×760，Min 640×420 | 默认 | ❌ **内容区无 ScrollViewer**，裸 ContentControl（:38）——托管插件内容溢出即裁切 |
| `Tools/PluginCompletionUiTool/MainWindow.xaml:9-12` | 1280×860，Min 1000×700 | 默认 | ❌ 无整体滚动；日志 TextBox 自带滚动（:208-209），日志区固定行高 220（:19） |

**风险：** PluginWorkbench MinWidth 1180、PluginCompletionUiTool MinWidth 1000，在 1366×768@125%（逻辑 1092）设备上均被系统裁切——与宿主 R1 同构。

### 3.3 页面（UserControl）布局健壮性

仓库内插件 XAML 共 5 个（10 个 XAML 文件含 Tools/Templates 中实际为 5 插件 + 4 Tools + Shared 2）：

| 文件 | 根元素 | ScrollViewer | 固定尺寸要点 |
|---|---|---|---|
| `CustomMouse/CustomMouseSettingsControl.xaml`（187 行） | UserControl 无尺寸 | ✅ :14 | ComboBox MinWidth 220 / MinHeight 32（:132-134）、数值标签 MinWidth 40（:99） |
| `ShellIntegration/ShellIntegrationSettingsControl.xaml`（242 行） | UserControl 无尺寸 | ✅ :22 | 路径文本 MaxWidth 280 + CharacterEllipsis（:75-76，少数主动防溢出措施） |
| `ViveTool/ViveToolPage.xaml`（412 行） | UserControl + 像素对齐三件套 | ✅ :49 | 见下 |
| `ViveTool/ViveToolSettingsPage.xaml`（143 行） | UserControl 无尺寸 | ✅ :22 | ProgressBar Height 6（:55） |

**ViveToolPage 固定像素集中区：** 40×40 图标块（:69）、搜索/筛选框 Height 34（:205/:215）、ProgressRing 20×20（:276-279）、**DataGrid MaxHeight=700 / MinHeight=360 / RowHeight=46 / ColumnHeaderHeight=36（:296-300）、列宽固定 110（:320）、110（:344）、230（:363）**、MinWidth 220（:93/:222）/ MaxWidth 360（:223）。

**具体风险点：**
- **双滚动：** DataGrid `MaxHeight=700` 与外层 ScrollViewer（:49）并存——内容高时页面滚动条与 DataGrid 内部滚动条同时出现，滚动体验混乱。
- **工具栏挤压：** 三列布局（`*` / `*` MinWidth=170 / Auto，:196-200）+ 状态区 MinWidth 220（:93），在宿主窄宽度下互相挤压无降级。
- **固定列宽不可读：** 110px 列在德语等长本地化文本下必截断（无 Wrap 说明）。
- **PluginCompletionUiTool 脆弱布局：** 用 Margin 偏移定位 CheckBox（`MainWindow.xaml:113 Margin="180,0,0,0"`、:120 `"300,0,0,0"`），文本一变长即错位——典型脆弱布局。
- **TextWrapping/Trimming：** 插件 XAML 共 24 处，但大量标签仍无 Wrap；`ViveToolPage.xaml:105-106` 同时设 `Wrap` 和 `CharacterEllipsis`（后者对 Wrap 文本无效，属冗余）。

### 3.4 本地化（良好）

三个插件全部 resx 化，每插件 **33 个 resx**（Resource.resx + 32 个卫星语言），XAML 通过 `{x:Static text:CustomMouseText.*}` / `{x:Static pluginRes:Resource.*}` 绑定，**插件 XAML 中 0 处硬编码用户可见字符串**。反例：`CustomMouseSettingsControl.xaml.cs:575/:597` 硬编码英文 `"Apply failed: "` / `"Apply cursor theme failed: "` 拼接（违反 CODING_STANDARDS 本地化条款）；Tools（PluginWorkbench/PluginCompletionUiTool）全部硬编码英文（如 `MainWindow.xaml:46 "Repository root:"`）——Tools 为内部工具，风险低但应记录。

---

## 四、视觉效果现状

### 4.1 动画与阴影：全面缺席

- **动画：0。** 全仓库（含 Tools）无 Storyboard / DoubleAnimation / BeginAnimation，无 VisualStateManager。加载/空态切换均为硬 `Visibility` 切换，无任何过渡。
- **阴影：0 处 DropShadowEffect / Effect。** 唯一"阴影"是两个 Tools App.xaml 定义的 `SnackbarShadowColor #40000000`（`PluginWorkbench/App.xaml:7`、`PluginCompletionUiTool/App.xaml:9`），且在本仓库内未见被引用。

**评价：** 与宿主的卡片悬停、页面入场、骨架微光相比，插件在宿主窗口内呈现"静态贴片"感；但考虑到插件运行于宿主页面内，**克制是合理默认**，缺的是最小必要反馈（加载、空态、状态切换过渡），不是装饰性动效。

### 4.2 加载指示

| 插件 | 现状 |
|---|---|
| ViveTool | ✅ 参考实现：`ViveToolPage.xaml:269-283 _loadingPanel` = ProgressRing 20×20 IsIndeterminate + 文字，代码切换（`.cs:1256-1257 UpdateLoadingVisibility`）；SettingsPage 下载用 ProgressBar Height 6 + 百分比（`.cs:314/:323/:334`），下载中禁用按钮（`.cs:308-310`） |
| CustomMouse | ❌ **无任何进度指示**。长操作（应用指针/光标主题，`.cs:519-651`）只有完成后 SetStatus 文字，**按钮不 disable、无 busy 态**，用户可重复点击 |
| ShellIntegration | ❌ **无进度指示**，仅按能力置灰按钮（`.cs:154-184`）+ 状态文字 |

### 4.3 空状态

- ViveTool 是参考实现：`ViveToolPage.xaml:392-407 _emptyStatePanel` = SymbolIcon Search24 FontSize 26 三级文字色 + 居中文案，显示逻辑 `.cs:1301-1309`（`Features.Count==0 && !IsLoading`）。
- **缺陷：C# fallback 版空态（`ViveToolPage.xaml.cs:322-335`）没有图标，只有 TextBlock——fallback 与 XAML 版视觉不一致。**
- `PluginUiStyles.xaml:66` 的 `PluginEmptyStatePanelStyle` 无人使用（死代码）。
- CustomMouse / ShellIntegration 无空态概念（设置型页面，合理）。

### 4.4 硬编码值审计

| 类别 | 插件 XAML | C# fallback / Tools |
|---|---|---|
| hex 颜色 | **0 处** ✅ | C# `Brushes.*` 命名色 fallback 8 处（`CustomMouseSettingsControl.xaml.cs:121 Green`、`:339 White`、`WpfFallbackHelper.cs:54/63/75`、`ShellIntegrationStyleSettingsWindow.cs:90/118/119/133`）；Tools App.xaml 各 1 处 SnackbarShadowColor。`ShellIntegrationProfile.cs:47-53` 等 hex 是 Nilesoft Shell 配置数据，非 WPF chrome，**不算违规** |
| 数字 CornerRadius | **0 处** ✅ | C# fallback 6 处：`ShellIntegrationStyleSettingsWindow.cs:92 (12)`、`CustomMouseSettingsControl.xaml.cs:109/:269/:347 (8)`、**`ViveToolSettingsPage.xaml.cs:107/:127 (10)`——词汇外值**；Tools：`PluginWorkbench MainWindow.xaml` 4/6/8/9 多处、`HostedPluginContentWindow.xaml:20/:34 (24，词汇外)`、`PluginCompletionUiTool MainWindow.xaml:27 (8) /:138/:195 (6)` |
| 裸 FontSize | **40 处**：CustomMouse 16（11/12/13/15）、ViveToolPage 16（11/12/13/14/16/20/26）、ViveToolSettingsPage 7、ShellIntegration 1（:29 隐藏图标 FontSize=1） | C# fallback **17 处**：CustomMouse 10（含 :336 `21`、:95 `16`）、ViveToolPage 5、ViveToolSettingsPage 2（:116/:135 `18`）。`PluginUiStyles.xaml:49` 自身也硬编码 `11` |
| 固定像素 Width/Height | 集中 ViveToolPage DataGrid 区（§3.3）、CustomMouse MinWidth 220 | Tools 侧栏固定 280/300（`PluginWorkbench MainWindow.xaml:279/:283`） |

**词汇表执行结论：** 声明刻度 0/3/8/12/18/20/999，实际散落 **4/6/9/10/24**（均在 C# fallback 与 Tools）——插件 XAML 本体干净，漂移全部发生在"第二套 UI"里。

### 4.5 其他视觉机制

- **通知：** `Shared/WpfHostNotifications.cs` 反射调用宿主 SnackbarHelper（:33,:78-93），ViveTool 10+ 处 ShowSnackbar——架构合理（复用宿主通知），无本地 toast 体系。
- **InfoBar：** wpfui:InfoBar 全仓库零引用；ViveTool 用 Border + `SystemFillColorCautionBackgroundBrush` 自制警示条（`ViveToolPage.xaml:115-138,141-181`；`ViveToolSettingsPage.xaml:131-140`）——与宿主 InfoBar 风格不统一。
- **状态着色：** `ViveToolPage.xaml:36-41` DataTrigger 给 DataGridRow 上 Success/Critical 背景，行 MinHeight 46（:34）——语义色用法正确。
- **主题画刷引用：** DynamicResource 用量 CustomMouse 14 / ShellIntegration 8+样式 / ViveToolPage 36 / ViveToolSettingsPage 11——主题热切换基本安全。

---

## 五、双 UI（XAML + C# fallback）漂移问题

**背景：** `Docs/CODING_STANDARDS.md:260-281` 规定所有控件必须经 `WpfFallbackHelper.TryInitializeComponent(this, BuildFallbackUi)` 做 XAML 加载失败回退——即**每个控件维护两套 UI**：XAML 一套、C# `BuildFallbackUi` 一套。

**漂移证据（同一属性的两套值）：**

| 控件 | XAML 版 | C# fallback 版 |
|---|---|---|
| ViveToolSettingsPage 圆角 | 令牌（8/12） | `new CornerRadius(10)`（.cs:107/:127，词汇外） |
| ViveToolPage 空态 | SymbolIcon + 居中文案（:392-407） | 仅 TextBlock 无图标（.cs:322-335） |
| CustomMouse 字号 | 11/12/13/15 | 16/21 等 10 处 |
| CustomMouse 状态色 | DynamicResource 主题刷 | `Brushes.Green`（.cs:121）硬编码语义 |

**结论：** fallback 是"保底可用"路径，但实际触发时用户看到的是**视觉降级且词汇违规的 UI**；且双份维护使每次视觉升级成本翻倍——这是插件仓库视觉债务的结构性根源。

---

## 六、规范真空

`Docs/CODING_STANDARDS.md`（中文）UI 相关仅两节：WPF UI 模式（fallback 要求）与本地化（resx 强制）。**没有关于圆角词汇表、间距刻度、字号刻度、阴影、动画、DPI/缩放、空态/加载态的任何成文规范**（Docs/ 全目录 grep radius/圆角/spacing/shadow/动画/DPI/缩放 零命中）。全仓库唯一写下词汇表规则的位置是 `Plugins/Shared/DesignTokens.xaml:4-11` 的头部注释——规范存在于注释而非文档，是 4/6/9/10/24 等词汇外值得以进入代码库的直接原因。

---

## 七、风险清单（按严重度排序）

| # | 严重度 | 类别 | 风险 | 证据 |
|---|---|---|---|---|
| P1 | **高** | 效果/治理 | 共享令牌体系半死不活：1/3 插件使用，ViveToolPage 复制令牌，CustomMouse 完全不用，5/9 样式死代码 | §2.3 |
| P2 | **高** | 结构 | 双 UI 强制维护导致值漂移，fallback 路径视觉降级且词汇违规 | §5 |
| P3 | 中 | 适配 | Tools 两 exe 无 DPI manifest，PerMonitor 场景位图拉伸发虚；MinWidth 1180/1000 小屏高缩放被裁切 | §3.1/§3.2 |
| P4 | 中 | 适配 | HostedPluginContentWindow 内容区无 ScrollViewer，托管插件溢出即裁切 | §3.2 |
| P5 | 中 | 适配 | ViveToolPage DataGrid 双滚动 + 固定列宽 110/230 长文本截断 + 工具栏无窄宽降级 | §3.3 |
| P6 | 中 | 效果 | CustomMouse/ShellIntegration 长操作无进度、无 busy 态、按钮可重复点击 | §4.2 |
| P7 | 中 | 效果 | 40+17 处裸 FontSize 绕过令牌；PluginUiStyles 自身硬编码 11 | §4.4 |
| P8 | 低 | 效果 | fallback 空态缺图标，与 XAML 版不一致 | §4.3 |
| P9 | 低 | 效果 | 自制警示条替代 InfoBar，与宿主风格不统一 | §4.5 |
| P10 | 低 | 适配 | PluginCompletionUiTool Margin 偏移定位，长文本即错位 | §3.3 |
| P11 | 低 | 治理 | 视觉规范全部缺失，词汇表只存在于注释 | §6 |
| P12 | 低 | 本地化 | CustomMouse .cs 两处硬编码英文错误前缀 | §3.4 |

---

## 八、附录：关键统计数据

| 指标 | 数值 |
|---|---|
| 插件 XAML 文件 | 5（+ Shared 2 + Tools 4） |
| 合并共享字典的插件 | 1.5 / 3 |
| PluginUiStyles 死样式 | 5 / 9 |
| 插件 XAML 硬编码 hex / CornerRadius / 字符串 | **0 / 0 / 0** |
| 裸 FontSize（XAML / C# fallback） | 40 / 17 |
| Storyboard / DropShadowEffect | **0 / 0** |
| DPI manifest / DPI 代码 | **0 / 0** |
| 带加载指示的插件 | 1 / 3 |
| 带空态的插件 | 1 / 3（fallback 版缺图标） |
| 每插件 resx 卫星语言 | 32 |
| 词汇外圆角值出现位置 | C# fallback（10）+ Tools（4/6/9/24） |
