# 视觉设计建议：屏幕适配与视觉效果

**日期：** 2026-07-18
**仓库：** UniversalDeviceToolkit（宿主 WPF 应用）
**依据：** 《视觉设计审计报告：屏幕适配与视觉效果》（`VisualAudit-ScreenAdaptation-20260718.md`，下称"审计报告"，风险编号 R1–R12 与之对应）
**性质：** 建议性文档，不含代码改动；每条建议给出问题证据、具体方案、涉及文件与验收标准。

---

## 目录

- [一、设计原则](#一设计原则)
- [二、屏幕适配建议（P0–P2）](#二屏幕适配建议)
- [三、视觉效果建议（P0–P2）](#三视觉效果建议)
- [四、实施路线图](#四实施路线图)
- [五、验收清单](#五验收清单)

---

## 一、设计原则

1. **适配优先于美化。** 内容被裁切（R1/R2/R3）是功能缺陷，视觉不一致是体验缺陷；先修前者。
2. **令牌是唯一真相源。** 新增任何视觉值（尺寸、字号、圆角、颜色）前先查 `Styles/DesignTokens.xaml`；没有则加令牌，不写裸值。当前视图代码 0 硬编码颜色 / 0 硬编码圆角的成果必须守住。
3. **兜底滚动是适配的保险丝。** 任何固定尺寸 / NoResize 容器都必须假设内容会溢出。
4. **动画走 RenderTransform，不碰布局属性。**（Margin/MaxHeight/Width 动画只在极小范围内可接受。）
5. **不破坏既有契约：** MainWindow 默认 1300×850 截图契约不变、AutomationId 不改名、无 MVVM 重写、新字符串只进 resx。

---

## 二、屏幕适配建议

### A1（P0）主窗口最小宽度下调，保住小屏高缩放设备 —— 对应 R1

**问题：** `MinWidth=1200` 超过 1366×768@125%（逻辑宽 1092）与 1920×1080@150%（逻辑 1280，余量仅 80px）场景（证据：`Windows/MainWindow.xaml:14-17`）。

**建议：**
- `MinWidth` 1200 → **1024**，`MinHeight` 720 → **640**。默认尺寸 1300×850 不变，截图契约不受影响。
- 同步验收 SettingsPage 左栏 240px + 右侧内容在 1024 宽下不破版（右栏仅剩 ~760px，见 A4）。
- 若个别页面（GodMode 类宽表格）在 1024 下确实不可用，对该页面单独说明并保留横向滚动兜底，而不是抬高全局 MinWidth。

**验收：** 1366×768@125% 虚拟机/仿真下主窗完整显示、无系统裁切；1920×1080@150% 下可缩至最小宽度正常使用。

### A2（P0）全部对话框增加滚动兜底，统一最小尺寸策略 —— 对应 R2

**问题：** 38 个对话框无一使用 ScrollViewer；14+ 个 NoResize/固定尺寸窗口在文本放大或长本地化字符串下直接裁切内容（审计报告 §2.2 全表）。

**建议：**
- 制定对话框模板约定：**所有对话框根布局的第一层内容必须包在 `ScrollViewer`（`VerticalScrollBarVisibility=Auto`）内**，按钮栏留在 ScrollViewer 之外固定。先覆盖 14 个 NoResize/固定尺寸窗口（OverclockDiscreteGPUSettingsWindow、HardwareSensorSectionsWindow、UpdateWindow、DeviceSetupWindow、LanguageSelectorWindow、UnsupportedWindow、ErrorDialogWindow、InputDialogWindow、OsdSettingsWindow、BootLogoWindow、ExcludeRefreshRatesWindow、SelectSmartKeyPipelinesWindow、SelectedActionsWindow、AutomationPipelineTriggerConfigurationWindow）。
- 将模板沉淀为一个共享样式或 `BaseDialogWindow`（继承 BaseWindow），新对话框默认获得滚动兜底，避免逐窗散改。
- `SizeToContent=Height` 的窗口加 `MaxHeight` 上限（建议取 `SystemParameters.WorkArea.Height × 0.85`），防止超长内容把窗口顶出屏幕。

**验收：** 把系统文本缩放调到 150%（或德语资源）逐窗检查：内容可滚动访问、无控件被遮挡、按钮栏始终可见。

### A3（P0）修正高 DPI 字号方向，补无障碍文本缩放 —— 对应 R3

**问题：** `Utils/DpiAwareTypography.cs:31` 在高 DPI 下把字号**缩小** 8%（`Math.Clamp(1d / Math.Sqrt(dpiScale), 0.92, 1.04)`），与"用户调高缩放是为了看清"的无障碍诉求相反；系统"放大文本"设置完全无响应。

**建议：**
- 短期：将修正系数下限从 0.92 放宽到 **0.96**（仅做"防过大"的轻修正，不实质性缩小），并在注释中写明设计意图与权衡。
- 中期：在 设置 → 外观 增加**应用内文本大小选项**（小 90% / 标准 100% / 大 110% / 特大 125%），实现为对 9 个字号令牌统一乘系数并写回 `Application.Current.Resources`，复用现有 `DpiAwareTypography.Apply` 的写入路径。这是 WPF 无法响应系统 TextScaleFactor 的现实解法。
- 不要在 DpiChanged 里做超过 ±5% 的缩放——频繁跨屏拖动时字号跳变比"略大"更刺眼。

**验收：** 200% 缩放下正文有效字号不小于 100% 缩放的 95%；应用内文本大小切换即时生效、无需重启、明暗主题下均无截断。

### A4（P1）补齐 4 个页面的滚动与折叠行为 —— 对应 R4、R11

**问题：** PluginExtensionsPage、SettingsPage、PluginPageWrapper 无页面级滚动；SettingsPage 左栏 240px 固定不折叠（`Pages/SettingsPage.xaml:38`），与主导航 70/220 折叠机制（`Styles/ElevationTokens.xaml:7-8`）行为不一致。

**建议：**
- `PluginExtensionsPage.xaml`：根布局外层包 `wpfui:DynamicScrollViewer`（与 Dashboard 一致），保留 ListBox 内部滚动的区域改为内容自然增高，避免双滚动。
- `PluginPageWrapper.xaml:36-40`：`_pluginContentHost` 外包 `DynamicScrollViewer`，并在插件开发文档中声明"宿主提供纵向滚动，插件页不应再自包整页 ScrollViewer"，消除双滚动隐患。
- `SettingsPage.xaml`：左栏在窗口宽度 < 1100 时折叠为仅图标（复用 NavigationStore 的折叠令牌与动画模式，不加 VisualStateManager，用 DataTrigger 或宽度换算器实现）。左栏 `Width="240"` 改为令牌引用。

**验收：** 三页在 1024×640（新 MinWidth）下全部内容可滚动到达；Settings 页折叠/展开动画与主导航一致。

### A5（P1）键盘布局控件整体可缩放化 —— 对应 R5

**问题：** Spectrum 键盘 JIS/ISO/ANSI 三控件约 560 处绝对像素键位坐标，不可重排（`Controls/KeyboardBacklight/Spectrum/SpectrumKeyboard*Control.xaml`）。

**建议：**
- **不重排键位**（成本极高），而是在三控件外层包 `Viewbox`（`Stretch=Uniform`），让整图按比例缩放——坐标布局内部不变，外层获得任意尺寸适配能力。这是绝对坐标布局的标准低成本解法。
- 配合外层容器 `MinHeight` 与横向居中，保证 1024 宽窗口下示意图完整可见。

**验收：** 窗口从 1024 拖到最大，键盘示意图等比缩放、无裁切、无模糊（Viewbox 对矢量无损）。

### A6（P2）10 个非 BaseWindow 窗口回归统一基座 —— 对应 R6

**问题：** 7 个 FluentWindow + 1 个裸 Window + 2 个 OSD 窗口无 DPI 字号修正与统一背景策略（清单见审计报告 §2.1）。

**建议：** 将 7 个 FluentWindow 与 ErrorDialogWindow 逐一改为继承 `BaseWindow`（OSD 两窗因 `AllowsTransparency` 特殊性可豁免，但需在代码注释中标注豁免原因）。每窗改动量小（换根元素 + 移除重复的 SnapsToDevicePixels 声明），收益是 DPI 修正、Mica/Acrylic、缩放稳定辅助全覆盖。

**验收：** `grep -c "<wpfui:FluentWindow"` 在 Windows/ 下归零（OSD 除外）；跨 DPI 拖屏时这些窗口字号随动。

### A7（P2）窗口尺寸与位置持久化 —— 对应 R12

**建议：** 在 `ApplicationSettings` 增加 MainWindow 的 `RestoreBounds`（Left/Top/Width/Height）持久化，启动时校验目标区域仍在当前显示器集合内（防拔掉显示器后窗口"丢失"），越界则回落居中。仅做 MainWindow，对话框维持 CenterOwner 不变。

**验收：** 调整窗口后重启应用恢复原尺寸位置；拔掉副屏后窗口出现在主屏可视区内。

---

## 三、视觉效果建议

### V1（P1）建立统一 EmptyState 控件，收编 5 处临时实现 —— 对应 R8

**问题：** 空状态无统一控件，5 处各自实现（`AutomationPage.xaml:179,313`、`PluginExtensionsPage.xaml:799`、`PluginPageWrapper.xaml:43-51`、`WindowsOptimizationPage.xaml:1045-1072,1580`）。

**建议：**
- 新增 `Controls/Custom/EmptyState.cs` + `Styles/EmptyState.xaml`：图标（SymbolIcon，令牌字号 48）+ 标题（Subsection 17）+ 描述（Caption 14，三级文本刷）+ 可选操作按钮，垂直居中，令牌间距。
- 先替换 PluginPageWrapper（插件生态门面）与 PluginExtensionsPage，其余三处随页面维护自然迁移。
- 控件 API 只暴露 `Icon`、`Title`、`Description`、`ActionContent` 四个槽位，防止再次分化。

**验收：** 5 处空状态视觉一致（图标/字号/间距相同）；新字符串全部进 resx；暗/明主题对比度 ≥ 4.5:1。

### V2（P1）骨架屏扩展到剩余 5 页 —— 对应 R7

**问题：** 骨架仅 Dashboard / Automation / KeyboardBacklight / Packages 四页；Settings、PluginExtensions、WindowsOptimization、Macro、About 加载时白屏或无反馈。

**建议：**
- 复用现有 `SkeletonShimmer` 附加属性与 `Styles/Loading.xaml` 样式族，**不为新页发明新模式**：Settings 页骨架 = 左栏列表项骨架 ×6 + 右栏卡片骨架 ×2；WinOpt 复用 Dashboard 卡片骨架；Macro/Packages 类列表页用 ListItem 骨架。
- 加载预期 < 300ms 的页面（About）不加骨架，避免闪烁；用 `LoadStatePresenter` 的延迟阈值控制骨架最短显示时间。
- PluginExtensions 现有两处 ProgressRing（:144/:580）统一迁入 LoadStatePresenter 语义。

**验收：** 9 个主页在人为注入 1s 加载延迟后均有骨架或进度反馈；骨架→内容切换走 SkeletonCrossfade（0.2s）不闪跳。

### V3（P1）裸 FontSize 令牌化（162 处）—— 对应 R9

**问题：** 162 处裸 `FontSize="N"` 绕过令牌与 DPI 修正（分布：Pages 59 / Windows 60 / Controls 28 / Styles 10）。

**建议：**
- 建立映射表就近归并：`11/12 → FontSizeSmallBody(13)` 或新增 Caption12 令牌、`13 → SmallBody`、`14 → Caption`、`15 → Body`、`17 → Subsection`、`19/20 → Section`、`25 → DisplaySection`、`28/29 → PageTitle`。SymbolIcon 的图标字号改走 IconSize 令牌。
- 分批提交（Controls → Pages → Windows），每批保持页面视觉 diff 可人工核对；11px 等令牌外值先加令牌再替换，**禁止四舍五入改视觉**。
- 在 `Directory.Build.targets` 或 CI 脚本加一条 XAML 扫描规则：`FontSize="\d` 命中即警告（SymbolIcon 白名单），防止回潮。

**验收：** 裸 FontSize 从 162 降至 0（SymbolIcon 白名单除外）；DpiAwareTypography 对全部文本生效。

### V4（P2）Toast 动画改为 RenderTransform —— 对应 R10

**问题：** `Styles/NotificationToast.xaml:176-199` 用 ThicknessAnimationUsingKeyFrames 动画 Margin，每帧触发 layout。

**建议：** 改为 `TranslateTransform.Y` + Opacity 动画（时长/缓动复用 AnimationTokens），视觉等效；多条 toast 叠加时避免级联布局。DynamicScrollBar 宽度动画（6↔10）范围极小，可保留。

**验收：** 连续触发 5 条 toast，UI 线程无可见掉帧（可用 Docs/UI_PERFORMANCE.md 的 dotnet-counters 流程佐证）。

### V5（P2）视觉一致性收尾

- **导航间距对齐：** MainWindow 导航与 Settings 页导航的选中态、图标间距、折叠行为统一口径（前者已主题感知，见 `1b66f8ca`；后者见 A4）。
- **阴影预算：** 维持 `Effect=` 仅 3 处的现状，新增阴影必须引用 ElevationTokens 且限通知/浮层；卡片悬停阴影不扩散到列表项（列表项用背景色变化即可）。
- **OSD 独立规范：** OSD 两窗（透明、置顶、Direct DropShadow）在 `Docs/LocalizationAndUiModernization.md` 已注明圆角独立，建议把"OSD 不走通用阴影/背景策略"写成一句话规范，防止后来者"统一化"误改。

---

## 四、实施路线图

| 阶段 | 内容 | 对应条目 | 风险 |
|---|---|---|---|
| Phase 1（适配止血） | A1 最小宽度、A2 对话框滚动兜底、A3 字号方向修正 | R1/R2/R3 | 低：纯 XAML/令牌调整，逐窗可验 |
| Phase 2（适配补齐） | A4 页面滚动/折叠、A5 Viewbox、A6 BaseWindow 回归 | R4/R5/R6/R11 | 中：A6 逐窗回归测试 |
| Phase 3（效果一致） | V1 EmptyState、V2 骨架扩展、V3 FontSize 令牌化 | R7/R8/R9 | 中：V3 批次多，需视觉 diff 核对 |
| Phase 4（收尾） | V4 Toast 动画、V5 一致性、A7 位置持久化 | R10/R12 | 低 |

**验证环境说明：** 本审计在 Linux 下静态完成；所有 UI 改动合并前需在 Windows 实机按 100%/125%/150%/200% 四档缩放 + 明/暗 × 三预设主题走查，并跑 `Docs/UI_PERFORMANCE.md` 的 UiPerformance.Smoke 与 VisualRegression.Smoke。

---

## 五、验收清单

- [ ] 1366×768@125% 与 1920×1080@150% 下主窗完整可用（A1）
- [ ] 14 个 NoResize 对话框在 150% 文本缩放下内容可滚动到达（A2）
- [ ] 200% 缩放下正文有效字号 ≥ 100% 时的 95%（A3）
- [ ] PluginExtensions / Settings / PluginPageWrapper 在 1024×640 下无内容裁切（A4）
- [ ] 键盘示意图在任意窗口尺寸等比完整显示（A5）
- [ ] 视图代码硬编码 hex 颜色、数字 CornerRadius 保持为 0（回归基线）
- [ ] 裸 FontSize 计数 ≤ 白名单数量（V3）
- [ ] 9 个主页加载均有骨架/进度反馈（V2）
- [ ] 5 处空状态统一为 EmptyState 控件（V1）
- [ ] 新增用户可见字符串 100% 进 resx（贯穿）
