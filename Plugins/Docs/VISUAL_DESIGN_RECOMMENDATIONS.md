# 视觉设计建议：屏幕适配与视觉效果

**日期：** 2026-07-18
**仓库：** UniversalDeviceToolkit（官方插件生态）
**依据：** 《视觉设计审计报告：屏幕适配与视觉效果》（`VISUAL_AUDIT_20260718.md`，下称"审计报告"，风险编号 P1–P12 与之对应）
**性质：** 建议性文档，不含代码改动；每条建议给出问题证据、具体方案、涉及文件与验收标准。

---

## 目录

- [一、设计原则](#一设计原则)
- [二、治理类建议（先立规矩）](#二治理类建议先立规矩)
- [三、屏幕适配建议](#三屏幕适配建议)
- [四、视觉效果建议](#四视觉效果建议)
- [五、实施路线图](#五实施路线图)
- [六、验收清单](#六验收清单)

---

## 一、设计原则

1. **插件是宿主页面的"内容"，不是独立应用。** 窗口、DPI、背景特效由宿主（PerMonitorV2 + Mica）负责；插件的视觉职责是：主题画刷一致、令牌一致、加载/空态/错误反馈到位。
2. **`Plugins/Shared` 是插件视觉的唯一真相源。** 能合并字典就不复制值，能引用样式就不写裸值。插件 XAML 当前 0 hex / 0 数字圆角 / 0 硬编码字符串的成果必须守住。
3. **克制是插件动效的默认。** 插件运行在宿主页面内，不加装饰性动画；只补"状态切换反馈"这一最小必要集。
4. **fallback UI 不是二等公民。** `BuildFallbackUi` 触发时用户看到的也必须是同一套视觉词汇——宁可让 fallback 简化布局，不可让它漂移值。
5. **规范先于重构。** 词汇表先写进 `CODING_STANDARDS.md`，再动手改代码，否则改动无判据。

---

## 二、治理类建议（先立规矩）

### G1（P0）把视觉词汇表写进 CODING_STANDARDS.md —— 对应 P11

**问题：** 圆角/间距/字号/阴影/动画/DPI/空态/加载态全无成文规范，唯一规则在 `Plugins/Shared/DesignTokens.xaml:4-11` 注释里。

**建议：** 在 `Docs/CODING_STANDARDS.md` 的"WPF UI 模式"节后新增"视觉词汇（Visual vocabulary）"一节，内容一次性写清：

- **圆角刻度：** 0 / ProgressBar 3 / Compact 8 / Control 12 / Card 18 / Surface 20 / Round 999——只允许这 7 个值，XAML 与 C# fallback 同等约束；卡片容器用 Card(18)，按钮/输入用 Control(12)，小贴片用 Compact(8)。
- **间距刻度：** 4/8/12/16/24，卡片内边距统一 `PluginCardPadding`（16,14）。
- **字号刻度：** Caption 12 / Body 14 / Section 15 / Metric 28——插件内不得出现其他值（SymbolIcon 图标字号除外，走 PluginIconSizeMD/LG 18/24）。
- **颜色：** 禁止 hex；状态用语义刷（Success/Warning/Info/Critical），文本用三级 TextFill 刷。
- **阴影/动画：** 默认不加；需要时引用宿主令牌，不自造。
- **加载/空态：** 长操作必须有进度或 busy 态 + 按钮禁用；空列表必须有图标+标题+描述三段式空态。
- **fallback 一致性：** `BuildFallbackUi` 的视觉值必须从与 XAML 相同的令牌/常量来源读取（见 G3）。

**验收：** 规范章节合入后，后续 UI PR 评审以该节为准据。

### G2（P0）让共享字典真正生效：三插件统一合并 —— 对应 P1

**问题：** ShellIntegration 全量合并、ViveTool 半合并且主页面复制令牌、CustomMouse 完全不合并（审计报告 §2.3）。

**建议：**
- `ViveTool/ViveToolPage.xaml`：删除 Resources 里内联复制的 4 个 PluginCornerRadius 键（:21-24），改为 `MergedDictionaries` 合并 `../Shared/DesignTokens.xaml`——消除 SoT 复制。
- `CustomMouse/CustomMouseSettingsControl.xaml`：合并 DesignTokens + PluginUiStyles，16 处裸 FontSize 与间距值替换为令牌（与 V2 同步做）。
- `ViveTool/ViveToolSettingsPage.xaml`：补合并 PluginUiStyles（当前只合并了 DesignTokens）。
- 在 `Templates/PluginArchetypes` 的模板说明里加一句"新插件默认合并 Shared 两个字典"，让新插件第一天就走对路。

**验收：** 三个插件根元素均可见两个字典的 MergedDictionaries；`grep "PluginCornerRadius" ViveToolPage.xaml` 无内联定义。

### G3（P0）双 UI 漂移止血：fallback 与 XAML 同源取值 —— 对应 P2

**问题：** `BuildFallbackUi` 与 XAML 两套实现值已漂移（圆角 10、字号 16/21、Brushes.Green、空态缺图标——审计报告 §5）。

**建议：**
- **短期（止血）：** 把漂移点逐一改回词汇表内——`ViveToolSettingsPage.xaml.cs:107/:127` 的 `CornerRadius(10)` → 12（Control）；`CustomMouseSettingsControl.xaml.cs` 的 `FontSize=21/16` → 令牌对应值；`Brushes.Green` → `TryFindResource("SystemFillColorSuccessBrush")` 带主题刷回退链（`WpfFallbackHelper` 已有此模式，:54-75，可直接复用）。
- **中期（治本）：** 在 `Plugins/Shared` 新增 `PluginVisualValues.cs`（C# 常量类，值与 DesignTokens.xaml 一一对应，注释注明"改此处必须同步改 XAML 字典"），fallback 代码一律引用常量类，不再散落魔法数。XAML↔C# 双写的漂移用一条单元测试守住：反射读常量类值与 DesignTokens.xaml 解析值比对（`Shared.Tests` 已存在，可加）。
- **简化 fallback 结构的权利：** 规范中明确"fallback 可简化布局（单列、无图标除外），但视觉值不得简化"——降低双份维护成本。

**验收：** 全仓库 grep 不到词汇外圆角值（4/6/9/10/24 清零，Tools 除外见 A3）；常量类与字典值比对测试通过。

---

## 三、屏幕适配建议

### A1（P1）Tools 两个 exe 补 DPI manifest —— 对应 P3

**问题：** PluginWorkbench / PluginCompletionUiTool 无 manifest，PerMonitor 场景位图拉伸发虚；MinWidth 1180/1000 在小屏高缩放下被裁切。

**建议：**
- 两项目各加 `app.manifest`，声明 `<dpiAwareness>PerMonitorV2</dpiAwareness>`（与宿主 `App.manifest:25` 对齐），csproj `<ApplicationManifest>` 挂接。
- `PluginWorkbench/MainWindow.xaml:7-10` MinWidth 1180 → **1024**；`PluginCompletionUiTool/MainWindow.xaml:9-12` MinWidth 1000 → **960**。默认尺寸不变。

**验收：** 150% 缩放副屏上 Tools 窗口文本锐利；1366×768@125% 下两窗可完整显示。

### A2（P1）HostedPluginContentWindow 与 CompletionUiTool 布局修复 —— 对应 P4、P10

**建议：**
- `Plugins/Tooling/PluginWorkbench/HostedPluginContentWindow.xaml:38`：裸 ContentControl 外包 `ScrollViewer`（Vertical Auto）——托管任意插件页时溢出可滚动，一行改动。
- `Plugins/Tooling/PluginCompletionUiTool/MainWindow.xaml:113/:120`：两处 `Margin="180,0,0,0"` / `"300,0,0,0"` 偏移定位的 CheckBox 改为 Grid 列布局（Auto/* 列），消除长文本错位。

**验收：** 托管 ViveTool 页缩小窗口至 640×420 内容可滚动到达；CompletionUiTool 在德语长度文本模拟下不错位。

### A3（P2）Tools 窗口词汇表对齐

Tools 属内部工具，不强制插件规范，但 `HostedPluginContentWindow.xaml:20/:34` 的 `CornerRadius=24`、`PluginWorkbench` 的 4/6/9 建议就近替换为 20/8/12，避免 Tools 截图与产品截图并排放时观感割裂。优先级最低，可随 Tools 下次维护顺手做。

### A4（P1）ViveToolPage 布局弹性改造 —— 对应 P5

**问题：** DataGrid `MaxHeight=700` 与外层 ScrollViewer 双滚动；列宽固定 110/230 长文本截断；工具栏三列无窄宽降级（审计报告 §3.3）。

**建议：**
- **去双滚动：** DataGrid 的 `MaxHeight=700/MinHeight=360` 去掉，高度交给外层 ScrollViewer（页面滚）或改为 DataGrid 内部滚（页面不滚）——二选一。推荐后者：工具栏固定 + 列表区滚，符合宿主 WinOpt 页模式。
- **列宽弹性化：** `Width=110`（:320/:344）→ `MinWidth=90, Width=*`；`Width=230`（:363）→ `MinWidth=180, Width=2*`；长文本列加 `TextTrimming=CharacterEllipsis`。
- **工具栏降级：** 状态区 `MinWidth=220`（:93）降为 160，搜索框 `MinWidth=170`（:198）保留，窄宽时筛选框（:215）允许换行到第二行（WrapPanel 或 Grid 行定义）。
- 删除 `:105-106` 上 Wrap 与 CharacterEllipsis 并存的冗余设置（Wrap 优先）。

**验收：** 宿主窗口拖到最小宽度（宿主 A1 后 1024）时 ViveTool 页无横向滚动条、无内容遮挡；德语资源下列内容可辨认。

### A5（P2）CustomMouse 窄宽余量

`CustomMouseSettingsControl.xaml:132-134` ComboBox `MinWidth=220` 在宿主窄宽度 + 德语长文本下偏挤，建议降到 180 并允许 `*` 列伸展；数值标签 `MinWidth=40`（:99）对 3 位数 + 单位足够，不动。

---

## 四、视觉效果建议

### V1（P0）加载反馈补齐：CustomMouse 与 ShellIntegration —— 对应 P6

**问题：** 两插件长操作无进度、按钮不禁用（审计报告 §4.2），用户可重复点击触发并发。

**建议（照抄 ViveTool 参考模式，不发明新模式）：**
- CustomMouse 应用指针/光标主题（`.cs:519-651`）：操作期间主按钮 `IsEnabled=false` + 按钮旁 ProgressRing（IsIndeterminate，尺寸走 PluginIconSizeMD 18）+ 状态区文字"正在应用…"；完成后恢复。XAML 与 fallback 同步加。
- ShellIntegration 注册/注销操作：同模式（按钮已有能力置灰逻辑 `.cs:154-184`，在其上叠加 busy 态即可）。
- 操作耗时可预期的（ViveTool 下载已是 ProgressBar 模式）用确定性进度；不可预期的用 IsIndeterminate——两模式分工写入规范（G1）。

**验收：** 人为注入 2s 延迟后，两插件操作期间按钮禁用且有可见进度反馈；操作完成状态文字与主题色正确。

### V2（P1）裸 FontSize 令牌化（40 处 XAML + 17 处 C#）—— 对应 P7

**映射表（就近归并，不四舍五入改视觉）：**

| 裸值 | 去向 |
|---|---|
| 11 | 新增 `PluginFontSizeSmall=11` 令牌（真实需求存在：ViveTool 11 处密集表格文本）或归入 Caption 12——**先由各插件维护者二选一，全库统一执行** |
| 12 | PluginFontSizeCaption |
| 13/14 | PluginFontSizeBody |
| 15 | PluginFontSizeSection |
| 16/18 | 新增 `PluginFontSizeEmphasis=16` 或归 Section 15（同上，先定后换） |
| 20/26 | 图标语境 → PluginIconSizeLG(24)/新增 32；标题语境 → 新增 PageTitle 档 |
| 28 | PluginFontSizeMetric |

- `PluginUiStyles.xaml:49` 自身硬编码 `FontSize=11` 一并修正——样式库不能带头违规。
- 分批：ViveTool → CustomMouse → Shared；ShellIntegration 仅 1 处（:29 的 FontSize=1 隐藏图标，属 hack，建议改 `Visibility` 方案）。
- CI 防回潮：与宿主同款的 XAML 扫描规则（`FontSize="\d` 命中即警告，SymbolIcon 白名单）。

**验收：** 插件 XAML 裸 FontSize 清零（白名单除外）；C# fallback 17 处同步替换。

### V3（P1）空态统一：启用并修复 PluginEmptyStatePanelStyle —— 对应 P8

**建议：**
- 修好死代码 `PluginUiStyles.xaml:66 PluginEmptyStatePanelStyle`（补图标槽位规范：SymbolIcon + PluginIconSizeLG + 三级文字色），让 ViveTool XAML 空态（:392-407）改为引用该样式——从"参考实现"升级为"共享实现"。
- **修复 fallback 空态**（`ViveToolPage.xaml.cs:322-335`）：补上与 XAML 版同款 SymbolIcon（Search24）与层级文案，保证两条路径视觉一致。
- 空态三段式（图标+标题+描述）写入规范，供未来列表型插件复用。

**验收：** 强制走 fallback 路径（模拟 XAML 加载失败）时，空态与正常路径视觉一致；样式零本地覆写。

### V4（P2）警示条统一到 InfoBar —— 对应 P9

**建议：** ViveTool 三处自制警示条（`ViveToolPage.xaml:115-138,141-181`；`ViveToolSettingsPage.xaml:131-140`）迁移到 `wpfui:InfoBar`（宿主已有成熟样式体系）， Severity 映射 Warning/Caution；若顾虑依赖，至少在 `PluginUiStyles.xaml` 沉淀一个 `PluginWarningBannerStyle`，让三处共享一份视觉定义而非各自堆 Border。

**验收：** 三处警示条视觉一致；明/暗主题下对比度达标。

### V5（P2）状态切换微过渡（唯一建议引入的"动画"）

**建议：** 插件不引入 Storyboard 体系，但允许一处低成本过渡：加载面板/空态/内容的切换用 `Opacity` 淡入（150ms，CubicEase Out）——可用 WPF-UI 自带过渡或代码触发，与宿主 SkeletonCrossfade 观感对齐。规范中写明"除此之外不加动画"，防止动效发散。

**验收：** 加载→内容切换无硬跳变；无新增 Storyboard 文件。

### V6（P2）本地化尾巴

`CustomMouseSettingsControl.xaml.cs:575/:597` 两处硬编码英文错误前缀（`"Apply failed: "`）迁入 resx（三插件均 32 卫星语言，新增键先英文占位，走正常翻译流程）。

---

## 五、实施路线图

| 阶段 | 内容 | 对应条目 | 风险 |
|---|---|---|---|
| Phase 1（立规矩 + 止血） | G1 规范章节、G2 字典合并、G3 短期漂移修复 | P1/P2/P11 | 低：文档 + 点状替换 |
| Phase 2（适配） | A1 Tools manifest、A2 窗口滚动/布局、A4 ViveTool 弹性化 | P3/P4/P5/P10 | 中：A4 需在宿主内多宽度走查 |
| Phase 3（反馈补齐） | V1 加载态、V2 FontSize 令牌化、V3 空态统一 | P6/P7/P8 | 中：fallback 双份同步修改 |
| Phase 4（收尾） | V4 InfoBar、V5 微过渡、A3/A5、V6 | P9/P12 等 | 低 |

**验证环境说明：** 本审计在 Linux 下静态完成；UI 改动合并前需：① 宿主 PluginWorkbench.Smoke 跑通；② 受影响插件 `dotnet test`（各插件均有 .Tests 项目）；③ Windows 实机在宿主内按 100%/150% 缩放 + 明/暗主题走查；④ 强制 fallback 路径人工走查一次（双 UI 一致性）。

---

## 六、验收清单

- [ ] `CODING_STANDARDS.md` 新增视觉词汇章节（G1）
- [ ] 三插件均合并 Shared 两个字典，ViveToolPage 无内联令牌复制（G2）
- [ ] 词汇外圆角值（4/6/9/10/24）全仓库清零（G3/A3）
- [ ] Tools 两 exe PerMonitorV2 manifest 就位，150% 副屏文本锐利（A1）
- [ ] HostedPluginContentWindow 内容可滚动（A2）
- [ ] ViveTool 页宿主最小宽度下无横向滚动、无双滚动条（A4）
- [ ] CustomMouse / ShellIntegration 长操作有 busy 态 + 按钮禁用（V1）
- [ ] 插件 XAML 裸 FontSize 清零（V2）
- [ ] fallback 路径空态与 XAML 版一致（V3）
- [ ] 插件 XAML 硬编码 hex / 数字圆角 / 硬编码字符串保持为 0（回归基线）
