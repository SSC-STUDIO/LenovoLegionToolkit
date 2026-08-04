# UniversalDeviceToolkit 全方位评估与升级维护计划

> 版本：2026-08-03 ｜ 适用范围：UniversalDeviceToolkit 全代码库（master @ b2434d1b + 当前工作树增量）
> 目的：为后续升级、维护、重构提供决策依据与分阶段执行计划（含验收标准）
> 方法：九维度静态调研（架构/代码质量/测试/构建/安全/功能/历史/性能UX/文档）+ 构建验证 + 定向测试

---

## 0. 代码库画像

| 指标 | 数值 |
|---|---|
| 解决方案 | UniversalDeviceToolkit.sln（35 个 csproj 中 28 个入 sln） |
| 代码规模 | 1250 个 .cs / 188K 行 C#；108 个 XAML |
| 主要项目 | Lib 45.5K 行、WPF 52.2K 行、Lib.Plugins 10.4K 行、Lib.Automation 4.0K 行、Tests 48K 行 |
| Git | 6380 提交（有效唯一内容约 3800，2022-2025 历史含约 50% 孪生重复）；6 个版本标签（v4.0.0~v5.0.1） |
| 身份 | fork 自 Lenovo Legion Toolkit；2025-11 由 crs102597 接管；2026-05 rebrand 为 UniversalDeviceToolkit；当前版本 5.0.1 |
| TFM | Windows 图统一 `net10.0-windows10.0.26100.0` + 强制 win-x64；可移植图 `net10.0` |
| 依赖管理 | Central Package Management（52 包）+ 30 个 packages.lock.json 全跟踪 + CI `--locked-mode` |
| 测试 | Tests ~4500 + CrossPlatform 137 + Fast.Tests 7 + UiAutomation.Tests 14；串行执行 |

---

## 1. 九维度调研结论摘要

### 1.1 架构与依赖（评级 ★★★★★）

- 依赖是严格 DAG：UI 壳（WPF/Avalonia/CLI）→ Lib 层 → Shared/Abstractions/ViewModels/Platform 底层，**无反向引用、无循环、无裸程序集引用**。
- 已知问题：
  - WPF 引用 `CLI.Lib`（实际承载 IPC 协议，命名与职责不符，形成"UI 依赖 CLI 组件"的语义反转）— 中
  - sln 层 WPF 声明依赖 CLI 项目，csproj 层只依赖 CLI.Lib — 不一致 — 中
  - `PresetUiValidation` 直接引用整个 WPF 程序集 — 中
  - Tests 引用 `Tools\MainAppPluginUi.Smoke`（测试依赖临时 smoke 工具）— 中
  - WPF 显式引用 Lib.Shared 但 0 个文件使用其命名空间（纯冗余）— 低
- 迁移中间态（高风险双实现）：
  - Lib 侧 `MessagingCenter`/`AbstractSettings`/`LltJson`/`ExceptionHelper` 仍是完整独立实现，Shared 侧有对应版本 — **高**
  - WPF 本地 `KeyboardBacklightViewModel`/`MacroViewModel` 与新 ViewModels 项目重复（82/95 行相同）— **高**
  - CLI（udt-cli，IPC 遥控）与 CrossPlatform（udt，独立便携 CLI）功能域重叠 — 中
  - `AssemblyName=udt` 被 CrossPlatform 与 Avalonia 同时使用，同目录发布互相覆盖 — 中
  - Platform.Linux/MacOS 零消费者（编译级死岛，0.95 置信度）— 中
- 孤立项目：PerformanceTest（不在 sln、无 CI）、HardwareValidation、SensorInventoryDump、4 个 sln 外 Tools。

### 1.2 代码质量（评级 ★★★★☆）

- 构建：Release 0 错误；生产代码 0 警告；仅 Tests 残留 1 个警告（CS8604 InstallerEngine.cs:454，非 Tests）。
- 可空性：全项目 `Nullable=enable`，生产代码 0 处 `#nullable disable`，无 CS86xx 残留 — **最强维度之一**。
- TODO/FIXME：全库仅 8 处，无 HACK/XXX — 低。
- 空 catch：4 处（WPF `NetworkAccelerationControl.xaml.cs:377,464` 静默吞保存失败 — 中；Lib Log.cs 2 处合理）。
- 硬编码异常消息：7 处（SettingsBackupService.cs:39-53 ×4、AbstractComboBox* ×2、FeatureRegistration.cs ×1）与全库 Resource 模式不一致 — 中。
- 巨型文件 22 个 >800 行：前 3 名 PluginExtensionsPage.xaml.cs 2827、PluginRepositoryService.cs 2179、SensorsControl.xaml.cs 1980 — **高**。
- async void 138 处全部为事件处理器，合法 — 低。
- File/Stream 30/30 全部 using；IDisposable 43 类；sync-over-async 24 处均在 Dispose/Stop 场景（有 ConfigureAwait(false) 防护）— 低。
- 冗余 `ConfigureAwait(true)` 37 处（WPF 迁移期残留，纯噪音）— 低。
- XML 注释覆盖 8%~42%（核心文件），无强制机制 — 中。

### 1.3 测试与 CI（评级 ★★★★☆）

- 测试命名 97.9% 遵循 `Subject_Action_Should_Expected`；[Trait] 分层（Unit/Plugin/Controller/Guard/Security...）支撑 CI fail-fast。
- 测试钩子设计优秀：`EnableUdtTestHooks` 传播 + `UDT_TEST_HOOKS` 常量 + 发布守卫（`RejectShippingAppPublishWithTestHooks`）— 最佳实践。
- 主要问题：
  - **全串行瓶颈**：xunit.runner.json `maxParallelThreads:1`，根因是 `UnitTestBase` 全局 pin culture/Resource.Culture（TestBase.cs:46-93）；已有 CultureScope 等隔离工具未采用。CI 状态套件预计 30-90 分钟 — **高**
  - **测试重构三项目未提交**：Fast.Tests / Tests.Infrastructure / UiAutomation.Tests 全部未跟踪，CI workflow 已切到新项目 → 推 CI 即挂 — **高（紧急）**
  - 跳过测试：16 静态 Skip（ExplorerRestart 9 + TokenManipulator 7，安全敏感路径零回归）+ 25 动态 Skip（含"环境决定测试有效性"反模式）
  - 命名空间/目录 4 处错位（含 PluginInfrastructureTests.cs 三重错位）
  - 根目录 77 个测试文件未归位；"Phase 系列"临时命名
  - Guard 测试 17+ 个文件用字符串断言 workflow 内容（脆弱，文案改动即崩）
  - `TestCategories.Coverage` 死代码（全库零使用）
- CI：15 个 workflow；`Ci-tests.yml` 已改三层（Security+Guard → Fast → Stateful）且新增 timeout-minutes 与 Test-WindowsTestEnvironment 预检（工作树版）；Release.yml 测试未传 coverlet.runsettings（与 CI 阈值不一致）；无 timeout 风险已修；runner 镜像不统一（windows-2022 vs latest、macos-14 vs latest）。
- 覆盖率门：50% 行覆盖（coverlet.runsettings），对 3-OS CrossPlatform 复用 Windows Include 列表（语义不可控）。

### 1.4 构建与依赖（评级 ★★★★★）

- 最佳实践齐全：CPM + lock 文件 + `--locked-mode` CI + 版本门禁（tag 必须匹配 props 版本）+ dependabot + 集中 props/targets。
- 待改进：
  - `Nullable`/`ImplicitUsings` 默认值已集中到 `Directory.Build.props`；CLI.Lib、Lib、Lib.Macro 和 WPF 保留有意的 `ImplicitUsings=disable` 例外 — 已完成
  - 5 个可移植项目手写 `Platforms`/`PlatformTarget`/`Prefer32Bit` 三件套与集中逻辑重复 — 低
  - 包版本积压：8 个小版本（Autofac 9.3.2、Serilog 4.4.0、Test.Sdk 18.8.1、coverlet 10.0.1、System.CommandLine 2.0.10、System.Management/ServiceController 10.0.10、Xunit.SkippableFact 1.5.61、ZenStates 1.0.3）；2 个大版本需评估（NAudio.Wasapi 2.3→22、Avalonia 11.3→12.1）；SDK BuildTools 不要升 — 中
  - `InstallOpt-out`：Installer.csproj `RestorePackagesWithLockFile=false`（有注释说明）— 低
- 输出嵌套 `bin\x64\Release\<tfm>\win-x64` 是标准 SDK 路径模板产物（Platforms=x64 + RID 叠加），非问题。
- 安装器：自研 WPF 安装器（Inno 已退役），Full/Online 双版本 + 内嵌 payload zip + SHA256 校验 + winget/scoop 清单校验 — 成熟。

### 1.5 安全与平台集成（评级 ★★★☆☆ — 三个结构性弱点）

| 级别 | 问题 | 位置 |
|---|---|---|
| **P0** | FanCurveManager 遗留 `Assembly.LoadFrom` 无校验加载（旧目录 *.dll，提权上下文任意代码执行） | `Lib\Utils\FanCurveManager.cs:129-188` |
| **P0** | 发布物零 Authenticode 签名（WDAC VerifiedAndReputable 0x800711C7 全盘拦截 + SmartScreen）；README 自述无力承担 EV 证书 | 全仓无签名配置；`README.md:631` |
| **P0** | `requireAdministrator` 全功能提权 + 安装目录用户可写 + 登录任务 RunLevel=Highest | `App.manifest:9`、`Autorun.cs:91` |
| 中 | TrustedPluginPackageStore 命中时免签放行旁路（同用户可"自签"） | `PluginSignatureValidator.cs:118-159` |
| 中 | IPC 挑战应答只证"同一用户"，不区分提权态，同用户非提升进程可驱动特权操作 | `WPF\CLI\IpcServer.cs:159-241` |
| 中 | InstallerEngine 下载 .NET 运行时安装器无哈希校验后静默执行 | `InstallerEngine.cs:95-105` |
| 中低 | `UDT_RESOURCE_CATALOG_URL` 覆盖仅验 scheme 无 host 白名单；语言包/设备包下载无哈希（目录有 SHA256 兜底） | `OnlineResourceCatalogClient.cs:24-39` |
| 低 | HttpClientFactory `allowAllCerts` 参数被静默忽略（语义困惑，实际安全） | `HttpClientFactory.cs:51` |

防护良好的部分：插件主链路（WinVerifyTrust + ALC 隔离 + 依赖链签名）、ZIP 解压 zip-slip 防护（3 处）、`CMD.cs` 命令注入防护、`PathSecurity`（符号链接解析）、凭据零持久化、日志无敏感数据。

### 1.6 功能模块全景

- **硬件控制器**（Lib\Controllers）：God Mode（Lenovo WMI）、GPU 超频/重启（nvapi+SetupAPI）、RGB/Spectrum 键盘（WMI/HID/Aurora）、传感器双轨（新 LHM `SensorsGroupController` + 旧 WMI `SensorsController`）、8 个品牌厂商控制器、LampArray 氛围灯（HID）、AI 模式、AMD 超频。
- **Features 抽象层**：60+ 能力功能（AbstractCapabilityFeature 等 6 基类 + 电池/混合显卡/电源模式/白键盘灯等）。
- **设备支持**：`LenovoDeviceSupportProvider` 内置 128 设备包、19+ 品牌；`DevicePackManager` 在线安装（SHA256 校验）。
- **自动化**：33 触发器 + 46 步骤 + 多态 JSON 序列化（automation.json）。
- **宏**：低层键盘钩子录制/回放（小键盘 0-9）。
- **插件系统**：签名验证 + ALC + 依赖拓扑排序 + 热重载 + 在线商店（6 候选 URL）+ 商店缓存 HMAC。
- **包下载**：PCSupport/Vantage 下载器 + 12 条更新检测规则。
- **网络加速**：PAC 代理 + 隔离 worker 进程（CONNECT 隧道无 MITM）+ 快照恢复 + 域名白名单。
- **系统**：WMI 26 partial / EC（PawnIO）/ 驱动封装 / NVAPI / HID / 注册表。
- **跨进程**：CLI↔WPF 命名管道（22 操作 + DPAPI 挑战应答）；NetworkProxy worker IPC（环境变量令牌）；单实例互斥。

### 1.7 Git 历史与 WIP

- 演进：2021-10 上游 LLT 创建 → 2022-09 LaptopPerformanceToolkit fork 合入 → 2022-10 中文翻译分支 → 2025-11 crs102597 接管 → 2026-05 rebrand（95d99651 批量改名）→ 2026-07-14 Phase 3 ABI 硬切换（v5.0.0）→ 2026-07-23 v5.0.1。
- 版本节奏：2026 年 1273 提交（接管后爆发）；3.4.1→5.0.1 密集迭代。
- **工作树 WIP（未提交，309 文件 ±1.2 万行）可拆 6 个逻辑变更集**：
  1. **测试体系重构**（最完整，接近可提交）：Tests.Infrastructure + Fast.Tests + UiAutomation.Tests 三项目 + FlaUI 迁出 + CI 全链路切换
  2. **传感器子系统解耦**：SensorsGroupController 2036→158 行 + 5 个新类（HardwareDiscoveryService/SensorPreferenceCatalog/SensorSelector/SensorSnapshotStore）
  3. **Avalonia 跨平台扩展**：多 TFM + IPlatformServices + 主题偏好持久化
  4. **语言包 BCP47 规范化**：22+26 resx 重写 + props/catalog/crowdin 同步 + 3 个 Assert 脚本护航
  5. **网络代理重构 + 安全加固**：LocalHttpProxyHost 遥测 + PluginDiscovery 目录校验
  6. **生命周期与 ABI 清理**：IoCContainer.Dispose + Lib.Plugins 11 契约文件删除（双 ABI 承接）+ WPF 死代码删除
- 残留：`body.json`/`resp.json`（LLM API 调试垃圾）、`.opencode/dead-code-scan/` 99 文件已误提交入库、3 个 `LenovoLegionToolkit.*.DotSettings` 已跟踪、`feature/ui-optimization` 陈旧分支（0 独有提交）、master 领先 origin 6 个未推送提交。

### 1.8 性能 / UX / 可访问性 / 本地化

- **性能**：三种刷新模式（SensorsControl 1s 轮询 + SensorsGroupController 生产者/订阅者 + OSD TheRing 精确节流）；180ms 快照缓存合并并发调用；3s 硬超时；FPS 1s 采样；GPU 状态自适应刷新（2s/10s）；启动无 Splash，语言门先于首窗；safe-start 健康守卫（连续 3 次失败）；日志滚动 10×50MB；`UiPerformance.Smoke`（评级 ≤400/≤900/≤1800ms）+ PerformanceTest 微基准。
- **UX**：WPF-UI 4.3 基础 + 自定义 ThemeManager（4 色板预设 + 10 token 推导 + 20 画笔动态覆盖）+ Mica/Acrylic 材质 + 动画 token 体系 + 109 XAML 屏幕适配审计（R1-R12）+ AdaptiveTextBlock 防长翻译溢出。
- **可访问性**：AutomationId/AutomationProperties 100+ 处、AccessKey 体系（为自动化测试服务）；高对比度仅 2 处（shimmer 停用）；无屏读测试 — 薄弱区。
- **本地化**：25 语言 × 6 程序集（WPF 1370 键）；LocalizationHelper（culture pin en + 父文化回退链 + 非中文永不落中文 + RTL/LRM 处理）；语言包在线安装（SHA256 + 原子替换 + 回退便携包）；7 个守卫测试强制。
- **文档**：22 篇 Docs + 8 篇根 md；3 处过时（LanguagePacks.md 声称 RepairAsync/UpdateAsync 不存在、LocalizationAndUiModernization.md 状态滞后、VisualAudit 称 CrossPlatform 纯控制台但已有 Avalonia）；README 日志路径单复数不一致。

---

## 2. 风险清单（按优先级）

### P0（立即，本周）
| # | 风险 | 位置 | 修复要点 |
|---|---|---|---|
| R1 | FanCurveManager 无校验 Assembly.LoadFrom | Lib\Utils\FanCurveManager.cs:165 | 删除遗留路径或接入 IPluginSignatureValidator |
| R2 | 发布物零代码签名（WDAC/SmartScreen 双拦截） | 全仓 | 接入 Azure Trusted Signing（开源免费额度） |
| R3 | 测试重构 WIP 未提交（CI 必挂） | Fast.Tests/Infrastructure/UiAutomation + workflows | 6 变更集拆分提交（见 §4 M1） |
| R4 | requireAdministrator 全功能提权 + 目录用户可写 | App.manifest:9 / Autorun.cs:91 | asInvoker + 按需提权（中期） |

### P1（近期，一个月）
| # | 风险 | 位置 | 修复要点 |
|---|---|---|---|
| R5 | Lib/Shared 四组双实现漂移 | 已改为 Shared 实现 + Lib 兼容 facade | 保持 wrapper，不再新增独立实现 |
| R6 | WPF 本地 VM 与新 ViewModels 项目重复 | 本地副本已删除，页面统一解析 `UniversalDeviceToolkit.ViewModels` | 增加迁移 Guard，防止副本回流 |
| R7 | 巨型文件 3 个 >1900 行 | `PluginRepositoryService`、`SensorsControl`、`PluginExtensionsPage` 已部分 partial 化；仓库下载和包校验职责已进一步隔离 | 继续拆分安装生命周期和 UI 状态职责 |
| R8 | 测试全串行（CI 30-90 分钟） | 已启用程序集/集合并行，状态集合隔离 | 保持并行基线，关注新增全局状态测试 |
| R9 | 插件信任链旁路 | PluginSignatureValidator.cs:118-159 | trusted store 完整性校验或移除免签通道 |
| R10 | IPC 认证不区分提权态 | WPF\CLI\IpcServer.cs | 校验对端提权令牌 |
| R11 | Installer 运行时下载无哈希 | InstallerEngine.cs:95-105 | 固化 SHA256 校验 |
| R12 | `udt` AssemblyName 冲突 | CrossPlatform/Avalonia csproj | 唯一化（udt / udt-gui） |

### P2（季度内）
| # | 风险 | 位置 |
|---|---|---|
| R13 | 集中重复属性 | Nullable/ImplicitUsings 默认值已集中到 Directory.Build.props；4 个 framework-import 项目保留显式 disable |
| R14 | 包版本积压 | Directory.Packages.props 一批升级 |
| R15 | Guard 测试字符串断言脆弱 | 已完成：workflow Guard 统一使用 `GitHubWorkflowContract` 的 job/step/with/参数结构解析，Guard 全集 140/140 |
| R16 | 死岛治理 | Linux/MacOS 已接入 Avalonia/CrossPlatform；PerformanceTest 已接 CI；Coverage 分类已无生产引用 | 收尾孤立工具项目清单 |
| R17 | 供应链补洞 | 资源目录 host 白名单、语言包/设备包下载哈希 |
| R18 | 残留文件入库 | `.opencode`、DotSettings、body/resp.json 已清理并加忽略规则 | 后续只防止重新入库 |
| R19 | 命名空间/目录错位 4 处 + 根目录 77 文件 | 已完成：`14f05d2a` 归位 58 个测试文件，布局 Guard 2/2 |

### P3（随手清理）
| # | 项 |
|---|---|
| R20 | 37 处 ConfigureAwait(true) 机械清理 | 已从源码移除 |
| R21 | 7 处硬编码异常消息归入 Resource（已完成，`ExceptionResourceGuardTests` 2/2） |
| R22 | 文档纠偏 3 处 + README 路径不一致（已完成：`21a5d2f6`） |
| R23 | sln 外 4 工具项目收纳、InnoDependencies 空目录、陈旧分支清理（已完成：`559ccf78`） |

---

## 3. 最佳实践对照（已达标 vs 建议引入）

### 已达标（保持）
- CPM + lock 文件 + --locked-mode；版本单一来源 + tag 门禁；发布禁止测试钩子。
- 可空性全面开启且无逃逸区；生产零警告。
- 集中化本地化 + 守卫测试体系 + Crowdin 流程。
- 插件加载纵深防御（签名→ALC→依赖链→沙箱）。
- 原子设置写入（temp+move）、zip-slip 防护、命令注入防护。
- 崩溃三连处理 + 崩溃报告 + 30 天清理。
- 测试钩子环境变量驱动 + 发布安全阀。

### 建议引入
1. `TreatWarningsAsErrors` 试点（CI `-warnaserror`，从 1 个警告清零起步）。
2. 架构守卫测试（禁止底层引用 UI 壳；禁止无校验 Assembly.LoadFrom；目录↔命名空间↔文件名三一致）。
3. 集合级测试并行 + culture 按集合隔离（预计 CI 30-90 分钟 → 5-15 分钟）。
4. `GenerateDocumentationFile` 试点核心 Lib 项目（CS1591 显形）。
5. 代码签名（Azure Trusted Signing）作为发布硬门禁。
6. 提交规范：大 WIP 按逻辑变更集拆分提交（当前 6 变更集待拆）。

---

## 4. 分阶段执行计划（含验收标准）

### M1 — 收尾与止血（本周）
1. **提交 6 个 WIP 变更集**（每集一个提交，与 WIP 隔离策略一致）：
   - 测试体系重构（Tests.Infrastructure + Fast.Tests + UiAutomation.Tests + sln + 5 workflows + Guard 测试适配）
   - 传感器解耦（SensorsGroupController 拆分 + 5 新类 + 厂商控制器联动 + 6 新测试）
   - Avalonia 扩展（多 TFM + IPlatformServices + 主题偏好）
   - 语言包 BCP47 规范化（resx/props/catalog/crowdin/脚本）
   - 网络代理重构 + PluginDiscovery 安全加固
   - 生命周期/ABI 清理（IoCContainer.Dispose + 契约删除 + 死代码）
   - 顺带删除 body.json/resp.json；git rm .opencode 与 3 个 DotSettings
   - **验收**：每个变更集构建 0 错误；测试重构集后全量 4538 测试通过；push 后 CI 绿
2. **R1 封堵 FanCurveManager**：删除遗留 LoadFrom 路径（功能已由现代插件系统覆盖）或接入签名验证；CI 加"禁止未校验 Assembly.LoadFrom"守卫测试。
3. **R2 签名调研**：验证 Azure Trusted Signing 免费额度资格；产出 Release.yml 签名步骤设计（含 signtool 命令与证书获取）。
4. **验收**：构建 0 错误；全量测试绿；`git grep "Assembly.LoadFrom"` 仅剩带校验路径。

### M2 — 质量与安全（下月）
1. **R4 权限模型**：App.manifest 降为 asInvoker + 按需提权（复用 runas 短进程模式）；Autorun 任务降权；安装目录改受保护位置或加 ACL。
2. **R9/R11 安全补洞**：trusted store 完整性、Installer 运行时 SHA256；IPC 提权校验已在本轮完成。
3. **R5/R6 双实现收尾**：Lib 四类 wrapper 化 + WPF 双 VM 删除（连带 R7 的 ViewModels 迁移）。
4. **R7 巨型文件**：先拆 PluginRepositoryService（纯逻辑，风险最低）与 SensorsControl（已有 VM 拆分先例）。
5. **R8 测试并行化**：xunit.runner.json 开集合并行 + UnitTestBase culture pin 改按集合隔离 + 逐个修复破坏并行的测试 + Ci-tests.yml 主 job timeout-minutes。
6. **验收**：全量测试并行下绿且 <20 分钟；警告归零；CI 无 timeout。

### M3 — 治理与现代化（下季度）
1. **R14**：包版本一批升级 + 两个大版本（NAudio/Avalonia）评估矩阵。
2. **R15**：已由 `GitHubWorkflowContract` 统一提供 YAML job/step/with/参数结构断言；脚本行为仍保留必要的语义契约。
3. **R16**：Platform.Linux/MacOS 接入 Avalonia 路线或归档；PerformanceTest 接 CI 基准或删除；TestCategories.Coverage 删除。
4. **R19**：Tests 根目录 77 文件归位 + 4 处命名空间错位修复 + "Phase 系列"改名 + 三一致守卫测试。
5. **R17/R12/R18/R22**：供应链补洞、udt 改名、残留清理、文档纠偏。
6. **验收**：`dotnet build -warnaserror` 全绿；测试目录三一致校验通过；无孤立项目。

### M4 — 长期演进（半年）
1. **WPF 巨型 code-behind 系统性拆解**（PluginExtensionsPage 2827 行等）→ CommunityToolkit + ViewModels 项目统一。
2. **CLI 双线合并**：IPC 遥控（udt-cli）与独立便携（udt）公共命令逻辑合并；Avalonia 里程碑明确（与 WPF 退出计划）。
3. **可访问性**：高对比度主题资源、焦点环、屏读测试（可接入 FlaUI 管线）。
4. **文档体系**：dead-code 报告归档、Docs 状态标注刷新、README 品牌一致性。

---

## 5. 执行核对记录（第一轮 Check）

> 本节保留第一轮复核背景；后续执行记录以 §7 及其后的日期分节为准。

| 项 | 状态 | 证据 |
|---|---|---|
| xUnit1026 假矩阵测试 | ✅ 已修（真修复） | 9 警告归零；Theory 参数经反射进入被测路径 |
| R1/R2/R9/R10/R11/R12/R13/R14/R17 | ✅ 已完成 | §7 执行记录、对应 Guard、locked restore 与 Release 构建验证 |
| R3/R8 | ✅ 已完成 | 测试迁移验收与集合级并行记录；主测试 4568 总数、4538 通过、17 个明确环境 Skip |
| R5/R6 | ✅ 已完成 | Lib 侧为 Shared 兼容 facade；WPF 重复 ViewModel 已移除 |
| R7 | 🟡 进行中 | `PluginRepositoryService`、`SensorsControl`、`PluginExtensionsPage` 已 partial 化，仍有安装生命周期和主 UI 状态块待拆 |
| R4 | 🟡 进行中 | 权限模型收尾仍需完整审计 |
| R19/R21/R22/R23 | ✅ 已完成 | 测试归位、异常资源化、文档纠偏和 4 个工具项目收纳均有独立提交与定向验证 |
| WSL Linux / macOS / FlaUI nightly | ⚠️ 环境待验证 | Windows 无 WSL distribution；macOS 和桌面 UI 必须由对应 CI runner 执行 |

### 5.1 后续功能修复：系统优化界面事务交互

该项是本计划之外、但与测试体系和 WPF 运行时覆盖直接相关的功能缺陷修复，不代表 R1-R23 中任何安全项已经完成。

| 项 | 状态 | 证据 |
|---|---|---|
| 推荐项不再勾选即执行 | ✅ 已实现 | `WindowsOptimizationViewModel.Action_PropertyChanged` 仅更新待应用选择；系统变更集中在 `ApplyOptimizationChangesAsync` |
| 应用/取消分离 | ✅ 已实现 | `WindowsOptimizationPage.xaml` 暴露 `WindowsOptimizationApplyButton` / `WindowsOptimizationCancelButton` |
| 启动时读取机器真实状态 | ✅ 已实现 | 扫描结果写入 `OptimizationActionViewModel.IsApplied`，同时初始化 `IsSelected` |
| 已应用项取消勾选后执行反向动作 | ✅ 已实现 | `OptimizationToggleActionHelper` 根据 enable/disable 成对动作解析目标；取消操作恢复 `IsApplied` 基线 |
| 回归验证 | ✅ 已通过 | WPF 构建 0 警告/0 错误；优化相关定向测试 109/109 通过、0 Skip |

本项的测试范围是定向行为测试和 Guard 测试，不能替代完整 WPF、跨平台和 UI 自动化矩阵。后续应在测试基础设施迁移完成后，将该交互纳入独立 UI smoke 场景，验证 AutomationId、初始状态、推荐选择、应用和取消的完整流程。

---

## 6. 关键证据路径速查

| 主题 | 位置 |
|---|---|
| 分层依赖 | 各 csproj ProjectReference；WPF csproj:54,56 |
| DI 装配 | Lib\IoCModule.cs、WPF\IoCModule.cs、Startup\StartupOrchestrator.cs |
| 插件加载 | Lib.Plugins\PluginManager.cs、PluginLoader.cs、PluginSignatureValidator.cs |
| 传感器 | Lib\Controllers\Sensors\AbstractSensorsController.cs、SensorsGroupController.cs |
| 自动化 | Lib.Automation\AutomationProcessor.cs、Pipeline\ |
| 宏 | Lib.Macro\MacroController.cs、Utils\MacroRecorder.cs |
| 网络加速 | Lib\Network\NetworkAccelerationService.cs、NetworkProxy\ |
| 主题 | WPF\Utils\ThemeManager.cs、RenderingCompatibilityHelper.cs |
| 本地化 | WPF\Utils\LocalizationHelper.cs、LanguagePackManager.cs |
| 构建集中 | Directory.Build.props/targets、Directory.Packages.props |
| CI | .github\workflows\Ci-tests.yml、Release.yml、flaui-tests.yml |
| 崩溃处理 | WPF\App.xaml.cs:832-997、Utils\CrashReportHelper.cs |
| 测试基建 | UniversalDeviceToolkit.Tests.Infrastructure\TestBase.cs |

---

## 7. 2026-08-03 执行记录

本轮已落地并验证以下事项：

| 项目 | 状态 | 验证结果 |
|---|---|---|
| R1 FanCurve DLL 加载 | 已完成 | `WinVerifyTrust` 校验在 `Assembly.LoadFrom` 前执行，并有安全 Guard |
| R2 发布签名 | 已接入门禁 | Release workflow 对 payload/installer 调用 Azure Trusted Signing，并用 `Get-AuthenticodeSignature` 验签；仍需配置仓库 secrets 后才能执行真实签名 |
| R9 插件签名旁路 | 已完成 | trusted package 命中不再绕过生产 Authenticode 校验 |
| R11 Installer runtime 下载 | 已完成 | .NET Desktop Runtime 固定官方 SHA-512，校验失败不执行并清理临时文件 |
| R12 AssemblyName 冲突 | 已完成 | CrossPlatform 保持 `udt`，Avalonia 改为 `udt-gui`，XAML URI 已同步 |
| R17 catalog host 校验 | 已完成 | `UDT_RESOURCE_CATALOG_URL` 限制 HTTPS 官方域名/镜像；mock host 仅在 `UDT_TEST_HOOKS` 下可用 |
| 测试迁移验收 | 已完成 | locked restore 通过；solution 串行构建 35 项目 0 警告/0 错误；主测试并行 4551/4568 通过、17 个明确环境 Skip；Fast 13/13、CrossPlatform 137/137 |
| 系统优化交互回归 | 已完成 | 定向回归 156/156；推荐只改待应用状态，应用/取消分离，启动状态扫描和反向动作均有覆盖 |
| R10 IPC 对端提权态 | 已完成 | 提升服务端通过 `RunAsClient` 校验对端管理员令牌；非提升同用户进程被拒绝 |
| 资源/Guard 漂移 | 已完成 | Sensors skeleton、Pages screenshot 资产引用、God Mode 数字默认值 Guard 已修复 |
| R13 项目默认值集中 | 已完成 | `Directory.Build.props` 提供 Nullable/ImplicitUsings 默认值；CLI.Lib、Lib、Lib.Macro、WPF 的显式 `ImplicitUsings=disable` 经过 Guard 固化；方案 Release 构建 0 警告/0 错误 |
| 测试日志资源释放 | 已完成 | `Log.ResetForTests()` 在 AppData 临时目录清理前释放异步 sink；插件仓库优化筛选 156/156 通过 |

当前仍未完成的工作包括 R4 权限模型收尾、R7 深层职责拆分，以及真实桌面 FlaUI nightly。R5/R6/R8/R14/R15/R16/R17/R18/R19/R20/R21/R22/R23 已在后续记录中完成；R2 的签名动作不会在缺少凭据时伪造成功，当前本地仍只验证 workflow 契约和验签脚本。
## 8. 2026-08-03 系统优化反向动作加固

- 注册表优化动作保留跨 `GetCategories()` 调用的原始值快照；历史上没有快照的已应用动作回退为删除优化值，恢复 Windows 默认/继承行为。
- 服务优化动作恢复应用前的 `Start` 值；缺少快照时恢复为非禁用的手动启动状态，避免取消勾选只改变界面而不改变系统。
- 电源计划新增从高性能计划回退到平衡计划并恢复休眠的动作；开始菜单策略恢复时删除策略值、刷新设置并重启 Explorer。
- 修复 toggle 成对动作在没有匹配 pair 时被误判，以及当前显示 disable 行时“推荐”状态反向的问题。
- 验证：主测试 TRX `SystemOptimization.Final.trx` 为 4555 总数、4538 执行、4538 通过、0 失败；系统优化定向测试 109/109 通过；WPF Release 构建 0 警告、0 错误。

## 9. 2026-08-03 系统优化事务状态一致性

- 分类汇总现在同时监听动作的可用性和可见性变化，启动扫描后项目数量与勾选状态同步刷新。
- 新增 `CanEdit` 状态：状态未知、扫描中和应用中禁止修改复选框；推荐按钮在未完成扫描或应用中不可操作。
- 首次进入页面由页面协调器只发起一次状态扫描；后续导航和插件刷新使用后台扫描，避免重复扫描造成状态闪烁。
- 应用取消或取消令牌触发后重新读取机器状态，避免部分动作已经执行时界面保留过期的待应用状态。
- 验证：系统优化筛选测试 156/156 通过；WPF Release 构建 0 警告、0 错误；新增分类状态通知和编辑锁 Guard 已纳入测试。

## 10. 2026-08-03 IPC 对端提权态门禁

- 保留现有 DPAPI 当前用户挑战和受保护管道 ACL；认证成功后、业务分发前增加对端令牌检查。
- 当服务端处于提升态时，通过 `NamedPipeServerStream.RunAsClient` 检查客户端管理员令牌；同用户但未提升的进程返回 Unauthorized，不再直接驱动提升进程执行特权操作。
- 服务端未提升时不改变现有同用户 CLI IPC 行为，避免无必要地扩大协议变更范围。
- 新增四态契约测试覆盖服务端/对端提升组合；IPC 测试 12/12 通过，WPF Release 构建 0 警告、0 错误。

## 11. 2026-08-03 测试集合级并行化

- 主测试程序集启用程序集和测试集合并行；`UnitTestBase` 与 `CultureScope` 不再修改进程级默认 Culture。
- 进程环境、AppData、插件缓存和单实例对象测试归入主测试程序集可发现的 `Process State Tests` 集合；Localization、Settings 和 FlaUI 保持独立串行集合。
- `SettingsCleanupHelper` 的所有使用者（包括 GodMode、Sunrise/Sunset 和 Warranty 测试）归入 Settings 集合，避免共享 AppData 和硬件兼容性静态状态互相污染。
- 修复迁移后集合定义仍位于 Infrastructure 程序集、xUnit 实际无法发现的问题；定义现位于主测试程序集，Infrastructure 只提供共享常量。
- 验证：Fast 13/13；系统优化筛选 94/94；主测试并行 4551 通过、17 个明确环境 Skip、0 失败，总计 4568，耗时约 71 秒。单实例 hook 测试 25/25 通过。
- 构建测试时必须让生产引用带 `EnableUdtTestHooks=true`；本地验证使用独立 `bin\hooked` 输出，避免正在运行的桌面程序锁定默认 WPF 输出。

## 12. 2026-08-03 残留产物清理

- 移除 `.opencode/dead-code-scan` 下 99 个已入库的死代码扫描产物、3 个历史 LenovoLegionToolkit DotSettings 和 WPF 临时 csproj。
- 移除根目录 API 调试残留 `body.json` / `resp.json`，并加入忽略规则，避免后续重新入库。

## 13. 2026-08-03 SensorsControl partial 拆分

- 将传感器区段可见性、排序、骨架布局和设置变更响应移至 `SensorsControl.Layout.cs`，原有字段、事件和运行时行为保持不变。
- 将 `PluginRepositoryService` 的插件更新查询移至 `PluginRepositoryService.Updates.cs`，保留原有缓存、下载和安装流程。
- 更新源码 Guard，使其读取主文件与 partial 文件的组合，避免结构化拆分被脆弱的单文件文本断言误报。
- 验证：WPF hook 独立输出构建 0 警告/0 错误；插件仓库、SensorsControl、系统优化和单实例相关定向测试 219/219 通过。
- `PluginExtensionsPage` 已新增批量导入、交互、能力解析三个 partial；`PluginRepositoryService` 和 `SensorsControl` 也已 partial 化，但安装生命周期、主列表状态和传感器刷新主流程仍待进一步拆分，不能视为 R7 全部完成。

## 14. 2026-08-03 跨平台验证入口

- 新增 `IPlatformProbe` 和 Windows 可注入 fake，实现 Linux/macOS 平台能力解析逻辑在 Windows 上的确定性契约测试。
- Avalonia 现在引用 Linux/macOS 平台能力程序集，并将 GUI 输出名固定为 `udt-gui`，避免与 CLI/diagnostics 输出覆盖。
- 新增 `Scripts/Test-CrossPlatformInWsl.ps1`：从 Windows 工作区映射到 WSL，执行 Linux TFM restore、build、test 以及 CLI smoke；脚本不会自动安装发行版。
- Linux/macOS 真实运行时仍由对应 CI 矩阵负责；WSL 只能验证 Linux，不能替代 macOS runner。
- Windows 验证：跨平台测试 `141/141`，Avalonia Release 构建 `0` 警告、`0` 错误。当前机器未安装 WSL distribution，因此真实 WSL 执行被明确前置检查阻止，未伪造通过。

## 15. 2026-08-03 维护项与格式化职责收尾

- CPM 小版本已更新并通过 `dotnet restore UniversalDeviceToolkit.sln --locked-mode`：Autofac、Serilog、Test SDK、coverlet、Xunit.SkippableFact、System.Management/ServiceController、ProtectedData/Logging.Abstractions 和 System.CommandLine。
- PerformanceTest 不在 solution restore 图中，已单独更新其 lock 文件并通过独立 `--locked-mode` restore；Release 构建和 `--output` benchmark smoke 均成功，报告可生成到指定路径。
- `ConfigureAwait(true)` 已从源码移除；它不会改变 UI 同步上下文，只增加噪声和维护成本。
- `SensorsControl` 的展示格式化、温度/吞吐量/功耗转换和硬件名称归一化移至 `SensorsControl.Formatting.cs`；CPU 功耗格式化改用调用内局部列表，避免共享静态临时集合造成并行串扰。
- 验证：WPF hook 独立输出构建 `0` 警告、`0` 错误；`SensorsControlTests` `64/64` 通过；跨平台测试 `141/141` 通过。
- 尚未完成：WSL 真实 Linux 执行、macOS runner 实测、`PluginExtensionsPage`/`PluginRepositoryService` 的完整职责拆分、提权模型重构和真实 FlaUI nightly。

## 16. 2026-08-03 后续执行：插件页拆分与提权边界

- `PluginExtensionsPage` 的批量导入/本地 ZIP 安装、列表交互/上下文菜单、插件 UI 能力解析分别移至 `PluginExtensionsPage.BulkImport.cs`、`PluginExtensionsPage.Interactions.cs` 和 `PluginExtensionsPage.Capabilities.cs`；主文件由约 2251 行降至约 1984 行，行为通过插件页面相关测试验证。
- `WindowsOptimizationElevationClient` 将 `runas` worker 的创建纳入统一异常与清理范围；UAC 错误码 1223 现在转换为明确的取消语义，worker 进程在成功、失败、取消三种路径均执行清理。
- 清理页也通过同一 worker 执行选中的内置 `cleanup.*` action；worker 拒绝插件清理 action 和混合/空操作组，避免 `asInvoker` 下权限不足被误报为完整成功。
- Windows 验证：WPF Release 构建 0 警告/0 错误；插件仓库、插件 ViewModel、页面 Guard、可执行文件解析、图标解析定向测试 91/91；提权协议测试 8/8。
- WSL 验证前置：`wsl.exe` 存在，但当前系统没有已安装 distribution，`Scripts/Test-CrossPlatformInWsl.ps1` 只能按设计报告前置失败；不执行自动安装。Linux 真实运行需用户安装 distribution 后重跑，macOS 仍由 CI runner 验证。
- 仍需：插件安装生命周期和页面主状态块的更深拆分；安装目录 ACL/按需提权完整审计；CI 上 macOS、WSL Linux 和 FlaUI nightly 的真实结果。

## 17. 2026-08-03 文档与迁移防回流收尾

- `Docs/LanguagePacks.md` 已按 `LanguagePackManager` 实际 API 纠正为 `QueryCatalogAsync`、`InstallAsync`、`Uninstall`，并说明重新安装是修复/更新路径。
- `Docs/LocalizationAndUiModernization.md`、视觉审计报告和 README 已更新当前状态、Avalonia/CrossPlatform 拓扑、双 CLI 职责以及实际 `logs` 目录。
- 新增 `ViewModelMigrationGuardTests`，锁定 WPF 页面使用共享 `UniversalDeviceToolkit.ViewModels`，防止 `KeyboardBacklightViewModel`/`MacroViewModel` 双实现回流。
- `GitHubWorkflowContract` 新增命令选项解析，CI Guard 对 `--filter`/`--logger` 断言改为参数级契约；相关 Guard 测试 18/18 通过，降低 YAML 换行和引号变化造成的误报。
- 已验证 `feature/ui-optimization` 无相对 `master` 的独有提交并删除本地陈旧分支；未触碰其他工作树改动。

## 18. 2026-08-03 安装器安全与 workflow Guard 收尾

- `73b93c87` 收紧安装器信任边界：安装器要求管理员权限，默认安装到受保护的 Program Files，拒绝目录外路径和 reparse-point 路径；在线语言包迁移到用户 AppData，避免运行期写入受保护安装目录。
- `5a01b8ab` 将 Release/Shipping workflow Guard 改为按 job/step/with 结构解析；解析器同时支持 GitHub Actions 的标准缩进序列，避免 YAML 换行、引号或 step 缩进变化造成误报。
- 验证：Installer Release 构建 0 警告/0 错误；安装器安全 Guard 2/2；语言包测试 6/6；Release/Shipping/CI Guard 28/28。
- 当前仍未完成：`PluginExtensionsPage` 安装生命周期的进一步拆分、真实 WSL Linux/macOS runner、FlaUI nightly，以及按需提权模型重构。R15 Guard 去脆弱化、R19 测试归位、R21 异常资源化、R22 文档纠偏和 R23 工具收纳已完成；并发工作区修改未混入上述提交。

## 19. 2026-08-03 插件安装生命周期与 WSL 前置

- `4ada5e2e` 将 `PluginExtensionsPage` 的批量更新、批量安装、在线/本地安装、安装后反馈和卸载流程移至 `PluginExtensionsPage.Installation.cs`；页面主文件减少约 425 行，页面 Guard 已支持组合读取 partial 文件。
- 验证：WPF Release 构建 0 警告/0 错误；插件仓库、插件 ViewModel、页面 Guard 65/65；系统优化及 toggle 相关测试 117/117。
- 已尝试准备 WSL：Windows Subsystem for Linux 与 Virtual Machine Platform 均已启用，DISM 返回 `3010`，需要重启后才能安装 Ubuntu/执行 `Scripts/Test-CrossPlatformInWsl.ps1`。本轮没有把 Linux 结果记为通过；macOS 仍必须由 macOS CI runner 验证。

## 20. 2026-08-04 测试归位与异常资源化收尾

- `14f05d2a` 将主测试项目根目录中的领域测试归位到 `Automation`、`CLI`、`DeviceSupport`、`Hardware`、`Models`、`Network`、`Plugins`、`Settings`、`UI`、`Optimization` 等目录，删除与现有插件测试重复的旧根目录副本，并同步 namespace。
- `TestLayoutGuardTests` 的聚合类型契约已改为使用归位后的相对路径；布局和临时 Phase 命名 Guard `2/2` 通过。
- R21 的 `SettingsBackupService`、两个 AbstractComboBox 基类和 `FeatureRegistration` 已使用 Resource-backed 异常消息；`ExceptionResourceGuardTests` `2/2` 通过，防止硬编码消息回流。

## 21. 2026-08-04 文档与工具项目收纳收尾

- `21a5d2f6` 已纠正语言包 API 文档、现代化文档状态、视觉审计历史基线、README 双 CLI 职责和 `logs` 路径。
- `559ccf78` 将 `PerformanceTest`、`LanguagePackInstallProgressSmoke`、`LanguagePackUi.Smoke`、`UiPerformance.Smoke` 加入方案；`Plugins.Abstractions` 保留为由 `crossplatform-libs.yml` 直接构建的显式 SDK 例外。
- `SolutionProjectInventoryGuardTests` `2/2` 通过；方案 locked restore 覆盖 32 个项目，4 个新收纳工具串行 Release 构建均为 `0` 警告/`0` 错误。`InnoDependencies` 已确认为空且无 Git 条目，陈旧 `feature/*` 分支不存在。

## 22. 2026-08-04 Guard 结构化断言收尾

- `CiWorkflowGuardTests`、`ReleaseSigningGuardTests`、`ShippingPayloadGuardTests` 和其他 workflow 检查均通过 `GitHubWorkflowContract` 解析 workflow，再按 job、step、`with` 和命令参数断言；没有直接依赖 YAML 原文缩进或换行。
- 运行时发现并修复 `UpstreamCapabilityMatrixGuardTests` 对已删除可选 `site/index.html` 的过时硬编码依赖；Guard 全集最终 `140/140` 通过，修复提交为 `6650fdba`。

## 23. 2026-08-04 系统优化权限与桌面测试验收

- `4e78c143` 新增权限模型 Guard，校验 WPF `asInvoker`、Autorun `TaskRunLevel.LUA`、`runas` worker、随机 token、管道 ACL 和内置动作组隔离；测试 `3/3` 通过。安装目录 ACL 与完整权限模型审计仍待后续完成。
- 系统优化定向测试当前 `123/123` 通过，覆盖推荐仅修改待应用状态、应用/取消分离、机器状态扫描、反向取消和批量执行。
- `6bfe8d12` 修复 FlaUI 基类的显式路径语义：默认构造器可搜索程序，显式路径不再回退搜索；`FlaUISetupTests` `4/4` 通过。完整 FlaUI nightly 本地运行 180 秒超时，仅记录为环境/清理问题，未标记为通过。
- WSL 相关可选功能已启用，但本机尚无 Linux distribution，`Scripts/Test-CrossPlatformInWsl.ps1` 尚未获得真实执行结果；macOS 仍需 macOS CI runner 验证。

## 24. 2026-08-04 WSL 实测与插件仓库职责拆分

- `ae9936cf` 将 `PluginRepositoryService` 的下载候选、Native curl、托管 HTTP 重试和下载进度处理移至 `PluginRepositoryService.Download.cs`；`708e63b0` 将本地包回退、ZIP/DLL 完整性、主 DLL 解析和运行时依赖落盘移至 `PluginRepositoryService.Package.cs`；`194d2e81` 将下载后安装编排和安装后可用性回滚移至 `PluginRepositoryService.Installation.cs`。主文件由 1121 行降至 700 行，未修改生产公共 API。
- Windows 验证：插件库和主测试项目 Release 构建均为 `0` 警告、`0` 错误；`Plugins` 领域测试 `714/714` 通过、`0` Skip，仓库服务定向测试 `36/36` 通过。
- WSL Linux 已完成真实执行：Ubuntu `26.04`、.NET `10.0.110`；Linux TFM 构建和跨平台测试构建均为 `0` 警告、`0` 错误，跨平台测试 `153/153` 通过、`0` Skip；`status`、`hardware`、`json` 三个 CLI smoke 均通过。
- Android 支持面已在 `d4025280` 中移除，并由 `AndroidSupportGuardTests` 固化；移动/Android companion app 已明确为不支持范围。
- 仍需：`PluginRepositoryService` 安装生命周期和 `PluginExtensionsPage` 主状态块的更深拆分；macOS runner、完整 FlaUI nightly、安装目录权限完整审计和按需提权模型重构仍未完成。

## 25. 2026-08-04 locked restore 与方案构建验收

- 初次 Windows `dotnet restore UniversalDeviceToolkit.sln --locked-mode` 暴露 `Tests`、`WPF` 和 `PresetUiValidation` 的 `Platform.Windows.Core` 项目引用未进入锁文件；`1e10e7d6` 更新三份锁文件并补入方案内缺失的 `LanguagePackInstallProgressSmoke/packages.lock.json`。
- 修复后方案 `--locked-mode` restore 通过全部 34 个项目；随后使用 `-m:1 -nr:false -p:UseSharedCompilation=false` 串行 Release 构建通过 35 个项目，`0` 警告、`0` 错误。
- 首次并行方案构建的两个错误均为遗留 MSBuild node reuse 对 `obj` 文件的占用，清理明确的 `dotnet ... MSBuild.dll /nodeReuse:true` 节点后串行构建通过；不属于源码或锁文件回归。

## 26. 2026-08-04 PluginExtensionsPage 生命周期职责拆分

- `22b075c2` 将 `PluginExtensionsPage` 的 Loaded/Unloaded、可见性切换、插件安装协调器订阅和安装进度 UI 同步移至 `PluginExtensionsPage.Lifecycle.cs`；页面主文件仅保留列表、数据和业务操作状态。
- 生命周期专用字段随 partial 归位，保持页面重新进入时的状态扫描、在线刷新取消、骨架动画停止和事件解绑行为不变；未修改生产公共 API。
- 更新 `PluginExtensionsPageGuardTests` 的组合源码读取，避免 partial 化后结构契约失效。
- 验证：WPF Release 构建 `0` 警告、`0` 错误；插件页面 Guard 与 ViewModel 定向测试 `29/29` 通过、`0` Skip。
- R7 仍未完全结束：`PluginRepositoryService` 的安装生命周期和页面其他主状态职责还需继续评估，完整 FlaUI nightly、macOS runner、安装目录权限审计和按需提权模型重构仍未完成。

## 27. 2026-08-04 PluginExtensionsPage 状态协调职责拆分

- `712798bc` 将 `PluginExtensionsPage` 的插件列表汇总、已安装状态快照、更新判定和单项 UI 状态刷新移至 `PluginExtensionsPage.State.cs`；筛选和 ViewModel 列表构造仍留在主文件，保持显示流程边界清晰。
- 状态 partial 保持现有在线清单合并、已安装清单回退、更新版本判定、能力刷新和失败回退行为；未修改生产公共 API。
- `PluginExtensionsPageGuardTests` 的组合源码读取已纳入 `Lifecycle.cs` 和 `State.cs`，避免 partial 化后 Guard 只读主文件造成漏检。
- 验证：WPF Release 当前项目构建 `0` 警告、`0` 错误；插件页面 Guard 与 ViewModel 定向测试 `29/29` 通过、`0` Skip。
- R7 仍需评估 `PluginRepositoryService` 安装生命周期和页面筛选/列表构造是否值得继续拆分；完整 FlaUI nightly、macOS runner、安装目录权限审计和按需提权模型重构仍未完成。

## 28. 2026-08-04 依赖小版本升级与锁文件验收

- `b3d098c7` 将 `Microsoft.Windows.CsWin32` 从 `0.3.275` 升至 `0.3.298`，将 `RAMSPDToolkit-NDD` 从 `1.4.2` 升至 `1.6.0`，并同步受影响项目的 lock 文件。
- 按计划未升级 `Microsoft.Windows.SDK.BuildTools`；Avalonia `11.3.6 -> 12.1.1` 保留为独立大版本评估，不混入本次小版本变更。
- 验证：solution `--locked-mode` restore 通过；Lib 与 WPF Release 构建均为 `0` 警告、`0` 错误；Fast 测试 `13/13`、安装器/权限/FanCurve/插件安全定向测试 `9/9` 通过，均 `0` Skip。

## 29. 2026-08-04 Installer 路径策略运行时审计

- `344301de` 为主测试项目加入 Installer 项目引用，并通过反射加载实际构建的 `UniversalDeviceToolkit.Installer.dll`，新增 `InstallerInstallPathPolicyRuntimeTests`。
- 运行时测试验证 Program Files 根目录不可直接安装、其子目录可接受、相邻目录和临时目录均拒绝；`Validate` 对受保护目录之外的路径抛出 `UnauthorizedAccessException`，不创建文件或修改 ACL。
- 验证：Installer Release 构建 `0` 警告、`0` 错误；Installer 运行时边界与既有安全 Guard `4/4` 通过、`0` Skip。
- R4 仍未完全结束：真实 ACL 写入/继承审计需要管理员专用安装环境，按需提权模型的完整端到端验证也仍需专用桌面环境；本地路径策略测试不替代这两项验收。

## 30. 2026-08-04 FlaUI nightly Skip 门禁

- 新增 `Scripts/Assert-FlaUiTestResults.ps1`，要求 FlaUI TRX 至少包含一个 `UnitTestResult`，并将 `Skipped`/`NotExecuted` 视为失败。
- `flaui-tests.yml` 将 TRX 固定写入 `TestResults/FlaUI`，在上传 artifact 前执行结果门禁；桌面前置检查失败仍直接失败，不通过 Skip 伪造绿色。
- `CiWorkflowGuardTests` 已锁定结果目录、门禁脚本、执行顺序和两类非执行结果。
- Windows 验证：FlaUI workflow Guard `3/3` 通过；TRX 断言脚本已验证正常、缺失、空结果、`Skipped` 和 `NotExecuted` 五类结果；完整 nightly 仍需 self-hosted 交互桌面 runner。

## 31. 2026-08-04 PluginExtensionsPage 筛选职责拆分

- `c3d9eb28` 将 `PluginExtensionsPage` 的 `ApplyFilters`、`UpdatePluginsList` 和 `SelectPreferredPlugin` 移至 `PluginExtensionsPage.Filtering.cs`，保持页面行为和生产公共 API 不变。
- `PluginExtensionsPageGuardTests` 已纳入筛选 partial 的组合源码读取，并新增职责归位断言，避免后续拆分后 Guard 漏检。
- 验证：WPF `x64 Release` 构建 `0` 警告、`0` 错误；页面 Guard `10/10` 通过、`0` Skip。
- 插件全量验证本轮未完成：此前使用未启用 `EnableUdtTestHooks` 的复用输出时得到 `537/540`，剩余失败涉及测试 Hook/进程环境，不能将该结果记为全通过；需要隔离且正确启用 Hook 的构建输出后再复测。

## 32. 2026-08-04 PluginRepositoryService 安装生命周期拆分

- `eae95b92` 将下载与安装编排、运行时可用性确认和临时产物清理移至 `PluginRepositoryService.Installation.Lifecycle.cs`；安装事务使用实例级互斥，避免并发替换插件目录或并发刷新运行时；未修改公共 API。
- 安装入口在构造临时路径前校验插件 ID；所有下载/校验/解压早退路径都通过 `finally` 清理 ZIP 和解压目录，失败清理仍保留可追踪日志。
- 验证：`UniversalDeviceToolkit.Lib.Plugins` Release 构建 `0` 警告、`0` 错误；测试项目在 `BuildProjectReferences=false` 的隔离模式下构建 `0` 警告、`0` 错误；`PluginRepositoryServiceTests` `38/38` 通过、`0` Skip。
- 普通测试项目构建仍受外部 `.NET Host (2444)` 锁定 WPF 输出影响，曾产生 `69` 个锁重试警告和 `12` 个复制错误；这不是本次插件代码编译错误，后续需要在无 VS 构建锁的环境完成全图验证。
- R7 仍未完成：`PluginRepositoryService` 的安装事务内部（解压/备份/回滚）和 `PluginExtensionsPage` 主状态块仍可继续按职责拆分；完整 FlaUI nightly、macOS runner、安装目录 ACL 审计和按需提权端到端验证仍待专用环境。

## 33. 2026-08-04 安装目录 ACL 真实写入审计

- `20a19205` 为 `InstallerInstallPathPolicyRuntimeTests` 增加专用管理员令牌测试：在 Program Files 下创建临时安装树，调用实际构建的 `PrepareForInstall`，检查根目录、嵌套目录和文件均关闭 ACL 继承，`BUILTIN\\Users` 仅具备读执行权限且不具备写入/删除/改权限能力。
- Windows 本机管理员环境验证：ACL 审计 `3/3` 通过、`0` Skip；测试在 `finally` 删除唯一创建的 `UniversalDeviceToolkit-AclAudit-*` 临时目录，当前 Program Files 未残留审计目录。
- 非管理员 runner 会因明确的“需要提升管理员令牌”前置条件跳过该专用测试；普通路径策略测试仍保持无环境依赖。R4 的完整安装、卸载、UAC worker 和权限模型端到端验证仍未完成。

## 34. 2026-08-04 PluginRepositoryService 清单持久化职责拆分

- `faa2be10` 将 `EnsureInstalledManifest`、`TryReadInstalledManifest` 和清单序列化依赖移至 `PluginRepositoryService.Installation.Manifest.cs`；`Installation.cs` 现在只保留解压、校验、备份、复制、信任记录和回滚事务。
- 验证：插件项目 Release 构建 `0` 警告、`0` 错误；所有 `DownloadAndInstallPluginAsync` 定向用例 `11/11` 通过、`0` Skip。
- 完整 `PluginRepositoryServiceTests` 在正确 Hook 输出下已恢复为 `38/38`，`0` Skip；此前 `37/38` 的唯一失败来自复用未启用 `EnableUdtTestHooks` 的生产插件程序集。可复现方式是先以 `/p:EnableUdtTestHooks=true` 串行构建 `Lib.Plugins` 及其 Lib/Shared 依赖，再以 `BuildProjectReferences=false` 构建并运行测试程序集；该方式不复用 shipping 输出。
