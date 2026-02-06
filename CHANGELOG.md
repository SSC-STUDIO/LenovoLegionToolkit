# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

### Added / 新增
- Created docs/ directory with comprehensive documentation including ARCHITECTURE.md (system architecture), DEPLOYMENT.md (build and deployment guide), SECURITY.md (security policy), and CODE_OF_CONDUCT.md (community guidelines) / 创建 docs/ 目录，包含完整文档体系：ARCHITECTURE.md（系统架构）、DEPLOYMENT.md（构建部署指南）、SECURITY.md（安全政策）、CODE_OF_CONDUCT.md（社区准则）
- Improved: Inject `IDelayProvider` into `ThrottleLastDispatcher` to allow fast virtual delays in tests. / 改进：向 `ThrottleLastDispatcher` 注入 `IDelayProvider`，以便在测试中使用快速虚拟延迟。
- Migrated core projects to target `net10.0-windows`. Update other project references/tests accordingly. / 将核心项目迁移到 `net10.0-windows`。请相应更新其它项目引用/测试。
- Added quick start guide, troubleshooting section, and documentation links to README / 在 README 中添加快速入门指南、故障排查部分和文档链接
- Improved README table of contents with new sections for Quick Start and Documentation / 优化 README 目录结构，新增"快速入门"和"文档"章节
- Created dedicated plugins repository at https://github.com/Crs10259/LenovoLegionToolkit-Plugins / 在 https://github.com/Crs10259/LenovoLegionToolkit-Plugins 创建专用插件仓库
- Added GitHub Actions workflow for automated plugin building and releasing / 添加 GitHub Actions 工作流实现插件自动化编译和发布
- Updated ViveTool plugin to version 1.5.0 with improved compatibility and stability / 更新ViveTool插件至1.5.0版本，提升兼容性和稳定性
- **Internationalization / 国际化**: Added multilingual support for CustomMouse plugin with 13 languages: English, Chinese (Simplified/Traditional), Japanese, Korean, German, French, Spanish, Portuguese, Italian, Russian, Polish, Dutch, Czech, and Turkish / 为 CustomMouse 插件添加 13 种语言支持：英语、中文（简体/繁体）、日语、韩语、德语、法语、西班牙语、葡萄牙语、意大利语、俄语、波兰语、荷兰语、捷克语和土耳其语
- **Internationalization / 国际化**: Added multilingual support for ShellIntegration plugin with 13 languages: English, Chinese (Simplified/Traditional), Japanese, Korean, German, French, Spanish, Portuguese, Italian, Russian, Polish, Dutch, Czech, and Turkish / 为 ShellIntegration 插件添加 13 种语言支持：英语、中文（简体/繁体）、日语、韩语、德语、法语、西班牙语、葡萄牙语、意大利语、俄语、波兰语、荷兰语、捷克语和土耳其语
- **Internationalization / 国际化**: Added Japanese (ja) and Korean (ko) translations for ShellIntegration plugin / 为 ShellIntegration 插件添加日语 (ja) 和韩语 (ko) 翻译
- **Internationalization / 国际化**: Migrated all hardcoded Chinese text in CustomMouse plugin XAML files to resource files / 将 CustomMouse 插件 XAML 文件中所有硬编码的中文文本迁移到资源文件
- **Internationalization / 国际化**: Migrated all hardcoded Chinese text in MenuStyleSettingsWindow XAML to resource files / 将 MenuStyleSettingsWindow XAML 中所有硬编码的中文文本迁移到资源文件

### Improved / 改进
- Migrated all plugin downloads to dedicated repository releases / 将所有插件下载迁移至专用仓库 releases
- Removed plugin source code and compilation from main repository / 从主仓库移除插件源码和编译
- Implemented Central Package Management (CPM) using Directory.Packages.props for centralized NuGet package version management / 使用 Directory.Packages.props 实现中央包管理 (CPM)，集中管理所有 NuGet 包版本
- Plugin repository service now fetches plugins from LenovoLegionToolkit-Plugins / 插件仓库服务现在从 LenovoLegionToolkit-Plugins 获取插件

### Improved / 改进
- 强化了不兼容机器上的功能限制：在非支持机型上直接从导航栏移除控制台、自动化及键盘灯光入口，并严格禁用所有定制化硬件功能（电源、灯光、性能模式、GPU 监控和传感器轮询等），即使选择继续运行也无法访问这些功能 / Strengthened feature restrictions on incompatible machines: directly removed Dashboard, Automation, and Keyboard lighting entries from navigation on unsupported models, and strictly disabled all customized hardware features (power, lighting, performance modes, GPU monitoring, sensor polling, etc.), making them inaccessible even if overridden
- 优化了不兼容机器上的设置页面：在非支持机型上隐藏所有硬件相关的设置分类（性能、显示、智能按键）及相关软件禁用选项，仅保留通用的外观、应用行为和更新设置 / Optimized Settings page on incompatible machines: hidden all hardware-related categories (Power, Display, Smart Keys) and relevant software disablers on unsupported models, keeping only generic Appearance, Application behavior, and Update settings
- 引入强制进程退出机制 (ExitProcess)，彻底解决在不兼容机器或极端情况下关闭软件后的进程残留和 CPU 占用问题 / Introduced forced process termination (ExitProcess) to completely resolve process residue and CPU usage issues upon exit on incompatible machines or in extreme cases
- 优化 IoC 容器解析逻辑，防止在系统未完全初始化时因解析失败导致的退出过程挂起 / Optimized IoC container resolution to prevent shutdown hangs caused by resolution failures when the system is not fully initialized
- Updated plugin store URL to point to separate plugin repository / 更新插件商店URL指向独立插件仓库

### Improved / 改进
- 优化关闭流程性能：将窗口大小保存改为异步执行，减少界面卡顿 / Optimized shutdown flow performance: changed window size saving to asynchronous execution to reduce UI lag
- 减少插件关闭等待时间从500ms到200ms，提升关闭响应速度 / Reduced plugin shutdown wait time from 500ms to 200ms for faster shutdown response
- 修复关闭流程阻塞问题：跳过 Spectrum 键盘控制器和 Windows 消息监听器的停止操作，解决 8 秒超时问题 / Fixed shutdown hang: skipped Spectrum keyboard controller and Windows message listener stop operations to resolve 8-second timeout issue
- 优化服务停止超时：从 8 秒减少到 2 秒，提升关闭响应速度 / Optimized service stop timeout: reduced from 8 seconds to 2 seconds for faster shutdown response
- 关闭速度从 8 秒提升到 0.35 秒 (23x) / Shutdown speed improved from 8 seconds to 0.35 seconds (23x faster)
- 移除 LibreHardwareMonitorLib 依赖，简化 CPU 电压读取逻辑，直接使用 WMI / Removed LibreHardwareMonitorLib dependency, simplified CPU voltage reading to use WMI directly

### Improved / 改进
- 重构依赖注入架构：将 MainWindow 和 GPUController 改为构造函数注入，减少对 Service Locator 的依赖 / Refactored DI architecture: switched MainWindow and GPUController to constructor injection, reducing Service Locator usage
- 优化 GPUController 职责：拆分出 GPUProcessManager 和 GPUHardwareManager，提升代码内聚性 / Optimized GPUController responsibilities: split into GPUProcessManager and GPUHardwareManager for better cohesion
- 完善 PluginManager 资源管理：实现 IDisposable 接口并正确反订阅 AssemblyResolve 事件，防止内存泄漏 / Improved PluginManager resource management: implemented IDisposable and correctly unsubscribed AssemblyResolve event to prevent memory leaks
- 增强 IDisposable 模式实现：为关键控制器添加 volatile 标志和完整的释放逻辑 / Enhanced IDisposable pattern implementation: added volatile flags and complete disposal logic for key controllers
- 优化异常处理与日志记录：统一日志格式，减少空 catch 块，提升可维护性 / Optimized exception handling and logging: unified log formats, reduced empty catch blocks, and improved maintainability
- Enhanced plugin management UI: added hover effects, plugin author display, and a modern "No Results" search state / 增强插件管理界面：添加悬停效果、插件作者显示以及现代化的“无搜索结果”状态
- Added context menu for plugins: easily open plugin folders, copy plugin IDs, or uninstall plugins / 为插件添加右键菜单：可轻松打开插件文件夹、复制插件 ID 或卸载插件
- Improved search experience in plugin page with a built-in clear button / 改进插件页面的搜索体验，增加了内置的清除按钮
- Simplified plugin categorization logic: plugins directly under the root `plugins` directory are treated as remote, while those in any other subdirectory (e.g., `local`) are treated as local / 进一步简化插件分类逻辑：根 `plugins` 目录下的插件视为远程，而位于任何其他子目录（如 `local`）下的插件则视为本地
- Fully localized the plugin management interface, including update status, tooltips, and action buttons / 插件管理界面现已全面支持多语言，包括更新状态、工具提示和操作按钮
- 优化设置界面的 UI 切换动画，统一动画参数体系（时长、缓动函数等），并默认开启高性能动画效果 / Optimized UI transition animations in Settings, unified animation parameters (duration, easing, etc.), and enabled high-performance animations by default
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Enhanced plugin build system for better dependency management / 增强插件构建系统，改善依赖项管理
- Improved resource handling and localization for multi-language support / 提升资源管理和本地化，支持多语言

### Fixed / 修复
- **Security Fix**: Fixed JSON deserialization vulnerability in AbstractSettings.cs by replacing TypeNameHandling.Auto with TypeNameHandling.None to prevent potential security attacks / 修复 AbstractSettings.cs 中的 JSON 反序列化安全漏洞：将 TypeNameHandling.Auto 替换为 TypeNameHandling.None 以防止潜在的安全攻击
- **Thread Safety**: Fixed race conditions in AbstractSettings.cs Store property getter by adding proper locking to ensure thread-safe access / 修复 AbstractSettings.cs Store 属性 getter 中的竞态条件：添加适当的锁以确保线程安全访问
- **Thread Safety**: Fixed race condition in BatteryDischargeRateMonitorService.cs by adding proper synchronization for CTS and Task management / 修复 BatteryDischargeRateMonitorService.cs 中的竞态条件：为 CTS 和任务管理添加适当的同步
- **Resource Management**: Added IDisposable pattern to AutomationProcessor.cs with proper event handler unsubscription and resource cleanup / 为 AutomationProcessor.cs 添加 IDisposable 模式：正确取消事件处理程序订阅并清理资源
- **Memory Leak Fix**: Fixed memory leaks in MainWindow.xaml.cs by unsubscribing event handlers in MainWindow_Closed / 修复 MainWindow.xaml.cs 中的内存泄漏：在 MainWindow_Closed 中取消事件处理程序订阅
- **Plugin System**: Fixed plugin assembly file locking issue in PluginManager.cs by loading assemblies from bytes instead of file path, enabling plugin updates without restart / 修复 PluginManager.cs 中的插件程序集文件锁定问题：从字节加载程序集而非文件路径，支持无需重启即可更新插件
- **Plugin System**: Added version compatibility checking for plugins to ensure plugin-host version compatibility / 为插件添加版本兼容性检查以确保插件与宿主版本兼容
- **Exception Handling**: Added proper exception handling and logging throughout codebase to replace empty catch blocks / 在整个代码库中添加适当的异常处理和日志记录以替换空的 catch 块
- **Dependencies**: Updated System.Management and System.ServiceProcess.ServiceController to version 10.0.1 for consistency / 更新 System.Management 和 System.ServiceProcess.ServiceController 到版本 10.0.1 以保持一致性
- 修复插件拓展和系统优化界面的 Snackbar 冲突重叠问题，统一通过 SnackbarHelper 进行队列管理 / Fixed Snackbar overlap conflict between Plugin Extensions and Windows Optimization by unifying notification queue management through SnackbarHelper
- 引入了多级保障的进程终止机制（TerminateProcess + Environment.FailFast + 前台守护线程），彻底解决了在极端情况下（如驱动程序或 DLL 卸载挂起）程序退出后进程残留的问题 / Introduced a multi-level process termination mechanism (TerminateProcess + Environment.FailFast + Foreground Watchdog thread) to completely resolve the issue of process residue after exit under extreme conditions (such as driver or DLL unload hangs)
- 修复了不兼容机器上关闭软件后的 CPU 占用残留问题：在所有退出路径（包括不兼容系统退出和异常退出）中正确停止宏控制器，释放键盘钩子，确保进程完全终止 / Fixed CPU usage residue after closing the software on incompatible machines: correctly stopped the MacroController in all exit paths (including incompatible system exit and exception exit) to release the keyboard hook and ensure the process terminates completely
- 彻底解决了退出程序时进程残留的问题，通过强化强制退出机制（TerminateProcess + Watchdog 线程 + Foreground Thread 保障）确保进程在任何情况下都能完全终止 / Completely resolved process residue issue upon exit by strengthening the forced exit mechanism (TerminateProcess + Watchdog thread + Foreground thread protection) to ensure the process terminates under any circumstances
- 修复了因错误的按钮类型检查导致本地插件卸载按钮无反应的问题 / Resolved an issue where the uninstall button for local plugins had no reaction due to incorrect button type checking
- 修复了从商店获取插件时的 404 错误，将仓库分支从 main 更新为 master / Fixed 404 error when fetching plugins from store by updating the repository branch from main to master
- 解决了退出程序时进程残留且无法结束的问题，通过引入更彻底的强制退出机制（TerminateProcess）确保进程完全终止 / Resolved issue where the process remained in the background and could not be terminated upon exit by introducing a more thorough forced exit mechanism (TerminateProcess)
- Resolved `XamlParseException` in Plugin Extensions page caused by missing `TextToVisibilityConverter` / 修复插件扩展页面因缺失 `TextToVisibilityConverter` 导致的 `XamlParseException` 崩溃

## [3.5.1] - 2026-01-29

### Added / 新增
- Safety confirmation dialog before system cleanup operations / 系统清理操作前的安全确认弹窗
- New cleanup items: App leftovers, Chrome/Edge/Firefox browser cache / 新增清理项：应用残留文件、Chrome/Edge/Firefox 浏览器缓存
- Registry redundancy cleanup for recent documents and app usage / 注册表冗余项清理（最近文档、应用使用记录等）
- Large file scanning in user profile folders with customizable size filters / 用户个人文件夹中的大文件扫描功能，支持自定义大小筛选
- One-click "Start All" interaction for driver downloads in System Optimization / 系统优化驱动下载新增“开始安装全部”一键操作

### Improved / 改进
- Redesigned icons for "Select Recommended" and "Clear Selection" in Windows Optimization / 重新设计系统优化页面“选择推荐”和“清除全部”图标
- Instant execution mechanism for optimization items upon checking / 系统优化项勾选后即时生效机制
- Batched Snackbar notifications for multiple optimization actions / 优化批量应用项时的 Snackbar 消息提示，合并显示
- Expanded system cleanup algorithm for better efficiency and coverage / 扩展系统清理算法，提升效率与覆盖范围
- Refactored Cleanup UI: Categories are now always visible, and Scan process is more transparent with a progress bar / 重构清理界面：项目列表始终可见，扫描过程配合进度条更透明
- Enhanced Driver Download UI: Dual-state toggle button (Start/Pause) for better control / 增强驱动下载 UI：采用“开始/暂停”双态切换按钮，提升控制体验
- 增强崩溃报告，增加内存状态和进程树记录 / Enhanced crash reports with memory state and process tree logging
- 引入 `ProcessWatcher` 自动管理子进程生命周期 / Introduced `ProcessWatcher` for automatic child process lifecycle management
- 优化启动时的残留进程检测与清理 / Improved detection and cleanup of residual processes during startup
- 优化了Vivetool工具 / Optimized ViveTool plugin
- Redesigned plugin item UI: replaced "Installed" button with a checkmark badge on the plugin icon / 重新设计插件列表项 UI：将“已安装”按钮替换为图标上的已安装角标
- Removed Ctrl+Shift+R reset functionality and related UI tips / 移除了 Ctrl+Shift+R 重置功能及相关的 UI 提示

### Fixed / 修复
- Scan button hover visibility issue in Cleanup page / 修复清理页面扫描按钮悬停时的显示问题
- ShellIntegration plugin compilation errors and namespace conflicts / 修复 ShellIntegration 插件编译错误及命名空间冲突
- Plugin SDK reference issues in ShellIntegration project / 修复 ShellIntegration 项目中的插件 SDK 引用问题
- 解决了 UI 崩溃后的进程残留问题，引入强制退出机制 / Resolved process residue after UI crash with a forced exit mechanism

## [3.5.0] - 2026-01-28

### Added / 新增
- Two new plugins have been added through the extension. For details, please refer to the CHANGELOG.md of each plugin. / 插件拓展新增两个插件，详情见插件每个插件的CHANGELOG.md
- Real-time power usage display for CPU and GPU in dashboard / 控制台新增 CPU 和 GPU 实时功耗显示
- Detailed model name display for CPU and GPU / CPU 和 GPU 详细型号名称显示
- Double-click interaction to toggle sensor details / 双击传感器卡片切换详情显示
- Plugin configuration management with user preferences / 插件配置管理，支持用户偏好设置
- Multi-language support for plugin interface / 插件界面多语言支持

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Unified dashboard layout merging battery, CPU, and GPU stats / 统一控制台布局，合并电池、CPU 和 GPU 状态
- Enhanced battery status display with progress bars for all metrics / 增强电池状态显示，简略视图所有指标均配有进度条
- Optimized progress bar styling and column spacing in sensors dashboard / 优化传感器控制台的进度条样式和列间距
- GPU clock display logic (Core clock in main view, Memory clock in details) / GPU 频率显示逻辑（主视图显示核心频率，详情显示显存频率）
- Plugin management interface in Windows Optimization settings / Windows优化设置中的插件管理界面
- Better error handling for plugin configuration operations / 插件配置操作的更好错误处理
- Plugin extensions list updates when switching to Extensions tab / 切换到扩展标签页时插件扩展列表更新
- Removed beautification-related code from WindowsOptimizationService and WindowsOptimizationPage / 从WindowsOptimizationService和WindowsOptimizationPage中移除美化相关代码
- Organized working directory: removed unused templates, moved shell integration files to plugin directory / 整理工作目录：删除未使用的模板，将Shell集成文件移动到插件目录
- Refactored shell integration helper usage to instance-based pattern for consistency / 重构Shell集成helper使用为基于实例的模式以确保一致性

### Fixed / 修复
- Corrected plugin metadata and version information / 修正插件元数据和版本信息
- System optimization Extensions tab for managing installed plugins / 系统优化扩展标签页，用于管理已安装的插件
- Plugin Extension ViewModel for better integration with system optimization / 插件扩展ViewModel，更好地与系统集成
- Plugin icon background color mapping for different plugin types / 不同插件类型的图标背景颜色映射
- PluginManager TryGetPlugin method for better plugin discovery / PluginManager TryGetPlugin方法，改进插件发现
- Removed donate functionality and all related UI components / 移除赞助功能及所有相关UI组件
- Added missing ExtensionsNavButton_Checked event handler for plugin tab navigation / 添加了缺失的ExtensionsNavButton_Checked事件处理器，用于插件标签页导航
- Plugin bulk import improvements for compiled plugins (DLL-only packages) / 针对编译插件（仅包含DLL的包）的批量导入改进

## [3.4.1] - 2026-01-24

### Added / 新增
- Plugin Stop interface for safe updates and uninstallation / 插件 Stop 接口，支持安全更新和卸载
- Debug logging for plugin configuration visibility diagnostics / 插件配置可见性诊断的调试日志
- Bulk plugin import functionality / 批量插件导入功能
- Comprehensive multilingual support for plugin bulk import features / 插件批量导入功能的完整多语言支持
- Plugin icon background color support from store.json / 从 store.json 读取插件图标背景颜色支持
- Improved make.bat plugin build commands with local test copy option / 改进 make.bat 插件构建命令，支持本地测试复制选项

### Fixed / 修复
- Plugin update process now stops plugins before updating / 插件更新流程现在会在更新前停止插件
- Configuration button responsiveness with better error handling / 配置按钮响应性及更好的错误处理
- Plugin installation/uninstallation file lock issues / 插件安装/卸载文件锁定问题
- PluginManifestAdapter missing Stop() method implementation / PluginManifestAdapter 缺失 Stop() 方法实现
- Plugin configuration button appearing for uninstalled plugins (with debug logging) / 插件配置按钮出现在未安装插件上的问题（附带调试日志）
- IsInstalled check now verifies plugin files exist on disk / IsInstalled 检查现在会验证插件文件是否存在于磁盘
- BooleanAndConverter safety improvements for null and non-boolean values / BooleanAndConverter 安全性改进，处理 null 和非布尔值
- **Plugin configuration button not responding - completely redesigned implementation** / **插件配置按钮无响应 - 完全重新设计实现**
- **Configuration button visibility logic with HasConfiguration property** / **配置按钮可见性逻辑使用 HasConfiguration 属性**
- **PluginViewModel compilation errors after configuration support changes** / **配置支持更改后的 PluginViewModel 编译错误**
- **ViveTool plugin appearing multiple times due to development folder scanning** / **ViveTool插件因开发文件夹扫描而多次显示**
- **PluginManager.PluginManifestAdapter priority issue - installed plugins showing as online adapters** / **PluginManager.PluginManifestAdapter优先级问题 - 已安装插件显示为在线适配器**
- **UI update loop caused by excessive UpdateAllPluginsUI calls** / **UI更新循环由过多的UpdateAllPluginsUI调用引起**
- **Plugin icon background colors changing on each app launch** / **插件图标背景颜色在每次应用启动时变化**
- **XAML tag mismatch error in PluginExtensionsPage** / **PluginExtensionsPage 中的 XAML 标签不匹配错误**
- **Missing translations for plugin snackbar messages** / **插件 snackbar 消息缺少翻译**
- **Hardcoded English text in plugin UI elements** / **插件 UI 元素中的硬编码英文文本**
- **Resource.Designer.cs missing new plugin resource strings** / **Resource.Designer.cs 缺少新的插件资源字符串**

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Plugin update reliability with proper resource cleanup / 插件更新可靠性及正确的资源清理
- Plugin configuration window error handling / 插件配置窗口错误处理
- Plugin state tracking and installation status validation / 插件状态跟踪和安装状态验证
- **Configuration button click handling with detailed logging** / **配置按钮点击处理及详细日志记录**
- **Plugin configuration support detection with CheckConfigurationSupport method** / **插件配置支持检测使用 CheckConfigurationSupport 方法**
- **Error handling and user feedback for configuration operations** / **配置操作的错误处理和用户反馈**
- **Plugin scanning filter to exclude development folders (obj, bin, Debug, Release)** / **插件扫描过滤器排除开发文件夹（obj、bin、Debug、Release）**
- **Plugin merging logic to prioritize installed plugins over online adapters** / **插件合并逻辑优先选择已安装插件而非在线适配器**
- **Optimized UI update flow to prevent infinite loops** / **优化UI更新流程防止无限循环**
- **Simplified plugin scanning logic - only scan root plugin directories** / **简化插件扫描逻辑 - 仅扫描根目录的插件目录**
- **Plugin icon background colors now read from store.json instead of dynamic generation** / **插件图标背景颜色现在从 store.json 读取，而非动态生成**
- **ViveTool status display moved to configuration page only** / **ViveTool 状态显示仅移至配置页面**
- **Enhanced make.bat with improved plugin build and local test copy functionality** / **增强 make.bat，改进插件构建和本地测试复制功能**

## [3.4.0] - 2026-01-22

### Added / 新增
- Bulk plugin import functionality with progress tracking / 批量插件导入功能及进度跟踪
- Plugin icon background colors in store configuration / 插件商店中图标背景颜色配置
- Comprehensive multilingual support for plugins (ja, ko, de, zh-hant) / 插件完整多语言支持（日语、韩语、德语、繁体中文）
- ViveTool status display and download functionality in plugin settings / 插件设置中的ViveTool状态显示和下载功能
- Plugin localization framework and resource standardization / 插件本地化框架及资源标准化

### Fixed / 修复
- Hard-coded strings in NetworkAcceleration plugin XAML files / NetworkAcceleration 插件XAML文件中的硬编码字符串
- Plugin configuration button click handler issues / 插件配置按钮点击处理问题
- Missing multilingual resource keys for UI elements / UI元素缺失的多语言资源键
- Plugin version synchronization with store metadata / 插件版本与商店元数据同步
- Missing Resource.Designer.cs entries for bulk import / 批量导入缺失的 Resource.Designer.cs 条目
- Duplicate resource entries in Resource.resx file / Resource.resx 文件中重复的资源条目
- JSON syntax errors in plugin store preventing online plugin loading / 插件商店JSON语法错误导致无法加载在线插件
- Removed redundant PluginImport plugin from store / 从商店中移除冗余的PluginImport插件
- Plugin uninstall button not updating UI after successful uninstall / 插件卸载成功后卸载按钮UI未更新
- Plugin descriptions not supporting Chinese localization / 插件描述不支持中文本地化
- Bulk import button icon and tooltip unclear / 批量导入按钮图标和提示不清晰
- Implemented proper ZIP file import functionality for local plugins / 实现了本地插件ZIP文件的正确导入功能
- Fixed configure button visibility to require both installed and supports configuration / 修复配置按钮可见性，需要同时安装且支持配置

### Added / 新增
- Plugin state reset functionality with Ctrl+Shift+R shortcut / 添加了插件状态重置功能，使用Ctrl+Shift+R快捷键
- Visual tip about plugin state reset shortcut in UI / 在UI中添加了插件状态重置快捷键的视觉提示

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- ViveTool plugin interface optimization by removing redundant status display / 优化ViveTool插件界面，移除冗余状态显示
- Plugin import workflow with ZIP file validation / 增强插件导入工作流及ZIP文件验证
- Plugin store UI with improved icon display and color coding / 改进插件商店界面图标显示和颜色编码
- Plugin management error handling and user feedback / 插件管理错误处理和用户反馈
- Plugin icon text color now adapts to theme (white in dark mode, black in light mode) / 插件图标文字颜色现在根据主题自动适配（深色模式白色，亮色模式黑色）

---

## [3.3.0] - 2026-01-XX

### Added / 新增
- Complete plugin system with online store and GitHub Actions publishing workflow / 完整的插件系统，包含在线商店和 GitHub Actions 发布工作流
- Network acceleration plugin with traffic statistics and UI improvements / 网络加速插件及流量统计界面改进
- ViveTool plugin integration with v1.3.0 release / ViveTool 插件集成及 v1.3.0 发布
- Plugin SDK for third-party development / 第三方插件开发 SDK

### Fixed / 修复
- Plugin installation permission errors and build issues / 插件安装权限错误和构建问题
- Compilation errors in plugin extensions and related components / 插件扩展及相关组件的编译错误
- Hard-coded strings in NetworkAcceleration plugin XAML files / NetworkAcceleration 插件 XAML 文件中的硬编码字符串

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Plugin UI with card-based layout and multilingual support / 卡片式布局和多语言支持的插件界面
- Plugin store with automatic file hash generation / 自动文件哈希生成的插件商店
- Localization consistency across all plugin components / 所有插件组件的本地化一致性

---

## [3.2.0] - 2026-01-XX

### Added / 新增
- Plugin auto-update functionality with version checking / 插件自动更新功能及版本检查
- Plugin import from compressed files / 从压缩文件导入插件
- Plugin installation with download progress bar / 带下载进度条的插件安装
- Comprehensive plugin multilingual support (ja, ko, de, zh-hant) / 插件完整多语言支持（日语、韩语、德语、繁体中文）

### Fixed / 修复
- Plugin icon loading logic for installed vs uninstalled plugins / 已安装和未安装插件的图标加载逻辑
- Plugin UI layout and interaction issues / 插件界面布局和交互问题

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Plugin details panel with automatic icon generation / 自动图标生成的插件详情面板
- Performance optimizations for plugin loading and management / 插件加载和管理的性能优化
- Plugin resource file organization and maintainability / 插件资源文件组织性和可维护性

---

## [3.2.1] - 2026-01-15

### Fixed / 修复
- Fixed bugs in settings interface / 修复设置界面中的错误

---

## [3.2.0] - 2026-01-14

### Added / 新增
- Updated 'Tools' UI with improved layout / 更新"工具"界面，改进布局
- Added collection utilities for better data handling / 添加集合工具以改进数据处理

---

## [3.1.5] - 2026-01-14

### Added / 新增
- Optimized ViVeTool plugin and updated Settings page navigation style / 优化ViVeTool插件并更新设置页面导航样式

---

## [3.1.3] - 2026-01-13

### Fixed / 修复
- Version bump only - no actual code changes / 仅版本号升级 - 无实际代码变更

---

## [3.1.2] - 2026-01-12

### Fixed / 修复
- Updated tools configuration / 更新工具配置

---

## [3.1.1] - 2026-01-12

### Fixed / 修复
- Version bump only - minor update preparation / 仅版本号升级 - 为小更新准备

---

## [3.1.0] - 2025-11-XX

### Added / 新增
- Categorized settings page navigation / 分类设置页面导航
- Advanced CLI with enhanced functionality / 增强功能的高级命令行工具
- Multiple SSIDs support for WiFi automation triggers / WiFi 自动化触发器支持多个 SSID
- Periodic action automation / 周期性操作自动化

### Fixed / 修复
- Power plan selector in settings / 设置中的电源计划选择器
- User inactivity timer bug / 用户非活动计时器错误
- CLI validator logic and duplicate WTS entries / CLI 验证器逻辑和重复 WTS 条目

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- UI responsiveness and performance / 界面响应性和性能
- Error messages and user feedback / 错误消息和用户反馈
- Hardware detection and compatibility / 硬件检测和兼容性

---

## [3.0.5] - 2026-01-11

### Fixed / 修复
- Installer build configuration in make.bat / make.bat 中的安装程序构建配置
- GitHub Actions workflow permissions / GitHub Actions 工作流权限

---

## [3.0.4] - 2026-01-11

### Fixed / 修复
- GitHub Actions release permissions and updates / GitHub Actions 发布权限和更新

---

## [3.0.3] - 2026-01-11

### Fixed / 修复
- Minor compatibility fixes / 小的兼容性修复

---

## [3.0.2] - 2026-01-11

### Fixed / 修复
- Version bump to 3.0 series / 版本升级到3.0系列

---

## [3.0.1] - 2025-09-XX

### Added / 新增
- .NET 8.0 migration / .NET 8.0 迁移
- Improved error handling and logging / 改进错误处理和日志记录
- Shell integration enhancements / Shell 集成增强

### Fixed / 修复
- ShellIntegration submodule paths and build artifacts / ShellIntegration 子模块路径和构建产物
- Installation and distribution issues / 安装和分发问题

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Performance optimizations / 性能优化
- Code cleanup and refactoring / 代码清理和重构

---

## [2.26.1] - 2023-08-XX

### Fixed / 修复
- Security vulnerability fixes / 安全漏洞修复
- Minor bug fixes and stability improvements / 小错误修复和稳定性改进

---

## [2.26.0] - 2023-08-XX

### Added / 新增
- Final stability improvements before 3.0 migration / 3.0迁移前的最终稳定性改进
- Enhanced hardware support for new Legion models / 对新Legion型号的增强硬件支持

---

## [2.25.3] - 2023-08-XX

### Fixed / 修复
- Critical bug fixes for production stability / 生产环境稳定性的关键错误修复

---

## [2.25.2] - 2023-08-XX

### Fixed / 修复
- Minor bug fixes and performance optimizations / 小错误修复和性能优化

---

## [2.25.1] - 2023-08-XX

### Fixed / 修复
- Stability improvements and crash fixes / 稳定性改进和崩溃修复

---

## [2.25.0] - 2023-08-XX

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Code quality and maintainability improvements / 代码质量和可维护性改进
- Performance optimizations and resource management / 性能优化和资源管理

---

## [2.24.2] - 2023-07-XX

### Fixed / 修复
- Minor bug fixes and stability improvements / 小错误修复和稳定性改进

---

## [2.24.1] - 2023-07-XX

### Fixed / 修复
- Bug fixes for reported issues / 已报告问题的错误修复

---

## [2.24.0] - 2023-07-XX

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Overall system stability and performance / 整体系统稳定性和性能
- User experience enhancements / 用户体验增强

---

## [2.23.1] - 2023-07-XX

### Fixed / 修复
- Critical bug fixes and stability improvements / 关键错误修复和稳定性改进

---

## [2.23.0] - 2023-07-XX

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Performance optimizations and memory management / 性能优化和内存管理
- Enhanced error handling and logging / 增强的错误处理和日志记录

---

## [2.22.2] - 2023-06-XX

### Fixed / 修复
- Security patches and minor bug fixes / 安全补丁和小错误修复

---

## [2.22.1] - 2023-06-XX

### Fixed / 修复
- Bug fixes for stability and compatibility / 稳定性和兼容性的错误修复

---

## [2.22.0] - 2023-06-XX

### Added / 新增
- Performance monitoring improvements / 性能监控改进
- Enhanced hardware detection capabilities / 增强的硬件检测能力

---

## [2.21.3] - 2023-05-XX

### Fixed / 修复
- Critical stability fixes and crash prevention / 关键稳定性修复和崩溃防护

---

## [2.21.2] - 2023-05-XX

### Fixed / 修复
- Minor bug fixes and user experience improvements / 小错误修复和用户体验改进

---

## [2.21.1] - 2023-05-XX

### Fixed / 修复
- Bug fixes for reported issues / 已报告问题的错误修复

---

## [2.21.0] - 2023-05-XX

### Added / 新增
- Advanced fan control improvements / 高级风扇控制改进
- Enhanced system integration features / 增强的系统集成功能

---

## [2.20.2] - 2023-04-XX

### Fixed / 修复
- Minor stability fixes and performance improvements / 小稳定性修复和性能改进

---

## [2.20.1] - 2023-04-XX

### Fixed / 修复
- Bug fixes for user-reported issues / 用户报告问题的错误修复

---

## [2.20.0] - 2023-04-XX

### Added / 新增
- New automation triggers and actions / 新的自动化触发器和操作
- Enhanced RGB lighting effects / 增强的RGB灯光效果

---

## [2.19.0] - 2023-03-XX

### Added / 新增
- Improved system monitoring capabilities / 改进的系统监控功能
- Enhanced user interface responsiveness / 增强的用户界面响应性

---

## [2.18.0] - 2023-02-XX

### Added / 新增
- Additional hardware support for new Legion models / 对新Legion型号的额外硬件支持
- Performance optimizations and bug fixes / 性能优化和错误修复

---

## [2.17.0] - 2023-02-XX

### Added / 新增
- Enhanced automation system features / 增强的自动化系统功能
- Improved system stability and performance / 改进的系统稳定性和性能

---

## [2.16.1] - 2023-08-25

### Fixed / 修复
- Fix resharper warnings / 修复 ReSharper 警告
- Fix #935 / 修复问题 #935
- Fix crash caused by inputting non-digit into color picker input (#934) / 修复颜色选择器输入非数字导致的崩溃 (#934)
- New Crowdin updates (#930) / 新的 Crowdin 更新 (#930)
- New Crowdin updates (#929) / 新的 Crowdin 更新 (#929)

---

## [2.16.0] - 2023-08-24

### Added / 新增
- Final 2.x release with stability improvements / 带​​稳定性改进的最终2.x版本
- Enhanced compatibility and performance / 增强的兼容性和性能

---

## [2.15.4] - 2023-07-18

### Fixed / 修复
- Critical stability fixes for production use / 生产使用的关键稳定性修复
- Performance improvements and bug fixes / 性能改进和错误修复

---

## [2.15.3] - 2023-07-18

### Fixed / 修复
- Minor bug fixes and improvements / 小错误修复和改进

---

## [2.15.2] - 2023-07-18

### Fixed / 修复
- Additional bug fixes and stability improvements / 额外的错误修复和稳定性改进

---

## [2.15.1] - 2023-07-12

### Fixed / 修复
- Bug fixes for user-reported issues / 用户报告问题的错误修复
- Stability and performance improvements / 稳定性和性能改进

---

## [2.15.0] - 2023-08-XX

### Added / 新增
- Experimental GPU Working Mode switch / 实验性 GPU 工作模式切换
- Spectrum RGB keyboard backlight control / Spectrum RGB 键盘背光控制
- Panel logo and ports backlight options / 面板标志和端口背光选项
- Boot logo customization / 启动标志自定义
- Advanced fan curve controls / 高级风扇曲线控制

### Fixed / 修复
- Compatibility with various Legion models / 与各种 Legion 型号的兼容性
- Keyboard backlight control issues / 键盘背光控制问题
- Power mode switching stability / 电源模式切换稳定性

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- RGB lighting effects and customization / RGB 灯光效果和自定义
- UI for keyboard and lighting controls / 键盘和灯光控制界面
- Hardware detection and device support / 硬件检测和设备支持

---

## [2.14.0] - 2023-07-XX

### Added / 新增
- GPU overclocking support / GPU 超频支持
- Advanced automation with time-based triggers / 基于时间的触发器高级自动化
- Custom tray icon tooltips / 自定义托盘图标工具提示
- Monitor (dis)connected automation triggers / 显示器连接/断开自动化触发器

### Fixed / 修复
- Runtime exceptions and crashes / 运行时异常和崩溃
- Process listener restart issues / 进程监听器重启问题
- Various UI bugs and inconsistencies / 各种界面错误和不一致

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Performance optimization for sensors / 传感器性能优化
- Notification system and positioning / 通知系统和定位
- Compatibility with newer Windows versions / 与较新 Windows 版本的兼容性

---

## [2.14.3] - 2023-06-26

### Fixed / 修复
- Critical bug fixes and stability improvements / 关键错误修复和稳定性改进

---

## [2.14.2] - 2023-06-24

### Fixed / 修复
- Additional bug fixes and performance improvements / 额外的错误修复和性能改进

---

## [2.14.1] - 2023-06-21

### Fixed / 修复
- Minor bug fixes and user experience improvements / 小错误修复和用户体验改进

---

## [2.13.2] - 2023-05-25

### Fixed / 修复
- Minor stability fixes and performance improvements / 小稳定性修复和性能改进

---

## [2.13.1] - 2023-05-25

### Fixed / 修复
- Bug fixes for user-reported issues / 用户报告问题的错误修复

---

## [2.13.2] - 2023-05-25

### Fixed / 修复
- Additional WiFi automation stability fixes / 额外的WiFi自动化稳定性修复

---

## [2.13.1] - 2023-05-25

### Fixed / 修复
- WiFi automation bug fixes and improvements / WiFi自动化错误修复和改进

---

## [2.13.0] - 2023-06-XX

### Added / 新增
- WiFi connect/disconnect automation actions / WiFi 连接/断开自动化操作
- Resume trigger for automation pipelines / 自动化流水线的恢复触发器
- Battery temperature monitoring and wear level / 电池温度监控和损耗等级
- HWiNFO64 integration for advanced monitoring / HWiNFO64 集成用于高级监控

### Fixed / 修复
- Gaming detection and automation / 游戏检测和自动化
- Power mode synchronization / 电源模式同步
- Various stability and compatibility issues / 各种稳定性和兼容性问题

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Automation pipeline processing / 自动化流水线处理
- Hardware monitoring and sensors / 硬件监控和传感器
- User interface responsiveness / 用户界面响应性

---

## [2.23.1] - 2023-XX-XX

### Fixed / 修复
- Critical stability fixes and performance optimizations / 关键稳定性修复和性能优化

---

## [2.23.0] - 2023-XX-XX

### Added / 新增
- Performance monitoring improvements and system integration / 性能监控改进和系统集成

---

## [2.12.0] - 2023-05-XX

### Added / 新增
- HDR state automation and triggers / HDR 状态自动化和触发器
- Device connected/disconnected automation / 设备连接/断开自动化
- Advanced power plan management / 高级电源计划管理
- Custom boot logo feature / 自定义启动标志功能

### Fixed / 修复
- Display brightness control issues / 显示亮度控制问题
- Power mode indicator errors / 电源模式指示器错误
- Automation pipeline failures / 自动化流水线故障

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- User activity detection / 用户活动检测
- Battery information accuracy / 电池信息准确性
- Overall system performance / 整体系统性能

---

## [2.11.2] - 2023-03-18

### Fixed / 修复
- Critical stability fixes and bug resolutions / 关键稳定性修复和错误解决

---

## [2.11.1] - 2023-03-18

### Fixed / 修复
- Minor bug fixes and performance improvements / 小错误修复和性能改进

---

## [2.11.0] - 2023-04-XX

### Added / 新增
- Multiple SSIDs for WiFi triggers / WiFi 触发器支持多个 SSID
- DPI scale automation / DPI 缩放自动化
- Screen resolution switching automation / 屏幕分辨率切换自动化
- Custom notification positioning / 自定义通知定位

### Fixed / 修复
- Touchpad scrolling performance / 触摸板滚动性能
- Process listener functionality / 进程监听器功能
- Notification display and positioning / 通知显示和定位

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- UI scaling and high DPI support / 界面缩放和高 DPI 支持
- Automation step execution / 自动化步骤执行
- Error handling and user feedback / 错误处理和用户反馈

---

## [2.10.0] - 2023-03-XX

### Added / 新增
- RGB keyboard automation steps / RGB 键盘自动化步骤
- Custom dashboard widgets and groups / 自定义仪表板小部件和分组
- Update available notifications / 更新可用通知
- Battery usage time estimation / 电池使用时间估算

### Fixed / 修复
- Power mode state restoration / 电源模式状态恢复
- GPU controller initialization / GPU 控制器初始化
- Settings import/export functionality / 设置导入/导出功能

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Dashboard customization and layout / 仪表板自定义和布局
- RGB lighting consistency / RGB 灯光一致性
- Overall application performance / 整体应用程序性能

---

## [2.9.1] - 2023-02-08

### Fixed / 修复
- Stability improvements and bug fixes / 稳定性改进和错误修复
- Performance optimizations / 性能优化

---

## [2.9.0] - 2023-02-XX

### Added / 新增
- AI mode with intelligent performance adjustment / AI 模式及智能性能调整
- Advanced fan control with custom curves / 高级风扇控制及自定义曲线
- GPU temperature and utilization monitoring / GPU 温度和利用率监控
- Custom power mode settings / 自定义电源模式设置

### Fixed / 修复
- Hybrid mode switching reliability / 混合模式切换可靠性
- Fan curve application / 风扇曲线应用
- Thermal sensor readings / 温度传感器读数

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Fan control algorithms / 风扇控制算法
- Temperature monitoring accuracy / 温度监控准确性
- System stability under load / 负载下系统稳定性

---

## [2.8.1] - 2023-01-18

### Fixed / 修复
- Minor bug fixes and stability improvements / 小错误修复和稳定性改进
- GPU mode switching reliability / GPU模式切换可靠性

---

## [2.8.0] - 2023-01-XX

### Added / 新增
- Hybrid GPU mode support / 混合 GPU 模式支持
- Advanced power limit controls / 高级功耗限制控制
- Battery health monitoring / 电池健康监控
- Custom automation triggers / 自定义自动化触发器

### Fixed / 修复
- GPU mode switching / GPU 模式切换
- Power limit application / 功耗限制应用
- Battery status reporting / 电池状态报告

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- GPU management and control / GPU 管理和控制
- Power efficiency optimization / 功效优化
- Hardware compatibility detection / 硬件兼容性检测

---

## [2.7.1] - 2022-12-15

### Fixed / 修复
- Automation pipeline reliability improvements / 自动化流水线可靠性改进
- Minor bug fixes and performance tweaks / 小错误修复和性能调整

---

## [2.7.0] - 2022-12-XX

### Added / 新增
- Automation system with pipelines and triggers / 自动化系统及流水线和触发器
- Process start/stop automation / 进程启动/停止自动化
- Time-based automation triggers / 基于时间的自动化触发器
- WiFi network automation triggers / WiFi 网络自动化触发器

### Fixed / 修复
- Application startup and initialization / 应用程序启动和初始化
- Settings persistence and loading / 设置持久化和加载
- UI responsiveness during automation / 自动化期间界面响应性

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Automation performance and reliability / 自动化性能和可靠性
- User interface for automation setup / 自动化设置用户界面
- Error handling in automation pipelines / 自动化流水线中的错误处理

---

## [2.6.5] - 2022-11-06

### Fixed / 修复
- Keyboard backlight control improvements / 键盘背光控制改进
- Stability fixes and performance optimizations / 稳定性修复和性能优化

---

## [2.6.4] - 2022-10-19

### Fixed / 修复
- RGB lighting consistency fixes / RGB灯光一致性修复
- Minor bug fixes and improvements / 小错误修复和改进

---

## [2.6.3] - 2022-10-16

### Fixed / 修复
- Color application and persistence fixes / 颜色应用和持久性修复
- Performance optimizations / 性能优化

---

## [2.6.2] - 2022-09-30

### Fixed / 修复
- Keyboard detection improvements / 键盘检测改进
- Minor stability fixes / 小稳定性修复

---

## [2.6.1] - 2022-09-29

### Fixed / 修复
- RGB control conflicts and errors / RGB控制冲突和错误
- Initial RGB system stability / 初始RGB系统稳定性

---

## [2.6.0] - 2022-11-XX

### Added / 新增
- RGB keyboard backlight control / RGB 键盘背光控制
- Multiple color zones and effects / 多色彩区域和效果
- Keyboard lighting presets / 键盘灯光预设
- Real-time color picker / 实时颜色选择器

### Fixed / 修复
- RGB control conflicts with Vantage / 与 Vantage 的 RGB 控制冲突
- Keyboard detection and initialization / 键盘检测和初始化
- Color application and persistence / 颜色应用和持久化

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- RGB lighting performance / RGB 灯光性能
- User interface for RGB controls / RGB 控制用户界面
- Hardware compatibility for RGB / RGB 硬件兼容性

---

## [2.5.0] - 2022-10-XX

### Added / 新增
- Package downloader for drivers and utilities / 驱动程序和实用程序包下载器
- System information and warranty display / 系统信息和保修显示
- Advanced compatibility checking / 高级兼容性检查
- Custom notification system / 自定义通知系统

### Fixed / 修复
- Update checking and notifications / 更新检查和通知
- Package download and installation / 包下载和安装
- System information accuracy / 系统信息准确性

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Download management and reliability / 下载管理和可靠性
- User interface for system info / 系统信息用户界面
- Overall application stability / 整体应用程序稳定性

---

## [2.4.1] - 2022-08-16

### Fixed / 修复
- Display configuration issues / 显示配置问题
- Stability improvements and bug fixes / 稳定性改进和错误修复

---

## [2.4.0] - 2022-09-XX

### Added / 新增
- Custom power mode with full control / 完全控制的自定义电源模式
- Advanced CPU and GPU power limits / 高级 CPU 和 GPU 功耗限制
- Temperature-based performance scaling / 基于温度的性能缩放
- Real-time performance monitoring / 实时性能监控

### Fixed / 修复
- Power mode switching reliability / 电源模式切换可靠性
- Performance limit application / 性能限制应用
- Temperature sensor readings / 温度传感器读数

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Power management algorithms / 电源管理算法
- Hardware control precision / 硬件控制精度
- User interface responsiveness / 用户界面响应性

---

## [2.3.1] - 2022-08-04

### Fixed / 修复
- RGB keyboard control improvements and logging / RGB键盘控制改进和日志记录
- Display and power management fixes / 显示和电源管理修复

---

## [2.3.0] - 2022-08-XX

### Added / 新增
- White keyboard backlight control / 白色键盘背光控制
- Microphone mute/unmute automation / 麦克风静音/取消静音自动化
- Display refresh rate control / 显示刷新率控制
- Advanced power plan management / 高级电源计划管理

### Fixed / 修复
- Keyboard backlight detection / 键盘背光检测
- Display configuration issues / 显示配置问题
- Power plan synchronization / 电源计划同步

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Keyboard control reliability / 键盘控制可靠性
- Display management / 显示管理
- Power management integration / 电源管理集成

---

## [2.2.1] - 2022-06-30

### Fixed / 修复
- Color application consistency / 颜色应用一致性
- RGB control reliability / RGB控制可靠性

---

## [2.2.0] - 2022-07-XX

### Added / 新增
- RGB keyboard preset system / RGB 键盘预设系统
- Custom color schemes and effects / 自定义颜色方案和效果
- Keyboard automation integration / 键盘自动化集成
- Enhanced RGB control algorithms / 增强的 RGB 控制算法

### Fixed / 修复
- RGB control conflicts and errors / RGB 控制冲突和错误
- Color application consistency / 颜色应用一致性
- Keyboard detection issues / 键盘检测问题

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- RGB lighting performance / RGB 灯光性能
- User interface for RGB controls / RGB 控制用户界面
- Hardware compatibility / 硬件兼容性

---

## [2.1.1] - 2022-06-25

### Fixed / 修复
- Fix restart after hybrid mode change / 修复混合模式更改后重启问题
- Fix for crash on AMD systems / 修复AMD系统崩溃问题
- Added accent color picker / 添加强调色选择器
- Apply current preset on startup / 启动时应用当前预设

---

## [2.1.0] - 2022-06-XX

### Added / 新增
- System accent color matching / 系统主题色匹配
- Custom themes and appearance settings / 自定义主题和外观设置
- Enhanced UI with WPFUI framework / 使用 WPFUI 框架的增强界面
- Tray icon improvements and actions / 托盘图标改进和操作

### Fixed / 修复
- Theme application and persistence / 主题应用和持久化
- UI rendering and scaling issues / UI 渲染和缩放问题
- Tray icon functionality / 托盘图标功能

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- User interface design and usability / 用户界面设计和可用性
- System integration and consistency / 系统集成和一致性
- Overall visual experience / 整体视觉体验

---

## [2.0.0] - 2022-05-XX

### Added / 新增
- Complete rewrite with WPFUI framework / 使用 WPFUI 框架完全重写
- Modern user interface design / 现代用户界面设计
- Enhanced hardware compatibility / 增强的硬件兼容性
- Advanced power management features / 高级电源管理功能

### Fixed / 修复
- Legacy UI framework limitations / 传统 UI 框架限制
- Hardware control reliability / 硬件控制可靠性
- System integration issues / 系统集成问题

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Application performance and responsiveness / 应用程序性能和响应性
- User experience and workflow / 用户体验和工作流
- Code architecture and maintainability / 代码架构和可维护性

---

## [1.6.0] - 2022-04-XX

### Added / 新增
- Initial RGB keyboard support / 初始 RGB 键盘支持
- Basic color control and presets / 基本颜色控制和预设
- Keyboard detection and initialization / 键盘检测和初始化

### Fixed / 修复
- Keyboard compatibility issues / 键盘兼容性问题
- Color application errors / 颜色应用错误

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Hardware detection accuracy / 硬件检测准确性
- User interface for keyboard controls / 键盘控制用户界面

---

## [1.5.0] - 2022-03-XX

### Added / 新增
- GPU monitoring and control / GPU 监控和控制
- dGPU deactivation support / dGPU 停用支持
- Power mode synchronization / 电源模式同步

### Fixed / 修复
- GPU detection issues / GPU 检测问题
- Power mode switching / 电源模式切换

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- GPU management reliability / GPU 管理可靠性
- Performance optimization / 性能优化

---

## [1.4.0] - 2022-02-XX

### Added / 新增
- Power plan management / 电源计划管理
- Enhanced power mode controls / 增强的电源模式控制
- Windows integration features / Windows 集成功能

### Fixed / 修复
- Power plan synchronization / 电源计划同步
- Mode switching reliability / 模式切换可靠性

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- User interface for power management / 电源管理用户界面
- System integration depth / 系统集成深度

---

## [1.3.0] - 2022-01-XX

### Added / 新增
- GPU activity monitoring / GPU 活动监控
- Enhanced compatibility detection / 增强的兼容性检测
- Additional device support / 额外设备支持

### Fixed / 修复
- GPU monitoring accuracy / GPU 监控准确性
- Compatibility detection / 兼容性检测

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- Hardware support breadth / 硬件支持广度
- Monitoring reliability / 监控可靠性

---

## [1.2.0] - 2021-12-XX

### Added / 新增
- Basic automation features / 基本自动化功能
- Process monitoring / 进程监控
- Settings persistence / 设置持久化

### Fixed / 修复
- Application stability / 应用程序稳定性
- Settings loading / 设置加载

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- User experience / 用户体验
- System integration / 系统集成

---

## [1.1.0] - 2021-11-XX

### Added / 新增
- Power mode controls / 电源模式控制
- Basic hardware monitoring / 基本硬件监控
- System tray integration / 系统托盘集成

### Fixed / 修复
- Initial stability issues / 初始稳定性问题
- Hardware detection / 硬件检测

### Improved / 改进
- 修复系统优化界面的布局列索引错误并限制其最大宽度，防止内容溢出遮挡左侧区域 / Fixed layout column index error and restricted maximum width of the details panel in Windows Optimization to prevent content from obscuring the left-side area
- 增加系统优化界面的选择状态和模式记忆功能 / Added persistence for selection state and page mode in Windows Optimization
- User interface / 用户界面
- System compatibility / 系统兼容性

---

## [1.0.0] - 2021-10-XX

### Added / 新增
- Initial release of Lenovo Legion Toolkit / Lenovo Legion Toolkit 初始版本
- Basic power mode switching / 基本电源模式切换
- Hardware compatibility detection / 硬件兼容性检测
- User interface for Legion devices / Legion 设备用户界面

---

## Migration Guide / 迁移指南

### From 2.x to 3.x / 从 2.x 到 3.x
- Backup your settings before upgrading / 升级前备份您的设置
- Some automation features have been redesigned / 某些自动化功能已重新设计
- Plugin system replaces old tools functionality / 插件系统替换旧工具功能

### From 1.x to 2.x / 从 1.x 到 2.x
- Complete UI overhaul / 完整的 UI 改造
- Settings migration required / 需要设置迁移
- Enhanced hardware support / 增强的硬件支持

---

## Support / 支持

- **GitHub Issues**: [Report bugs and request features](https://github.com/BartoszCichecki/LenovoLegionToolkit/issues)
- **Discord**: [Community support and discussions](https://discord.gg/)
- **QQ Channel**: [中文用户支持群](https://jq.qq.com/)

---

## Contributors / 贡献者

Thanks to everyone who has contributed to this project!
感谢所有为这个项目做出贡献的人！

- Main developer: BartoszCichecki / 主要开发者：BartoszCichecki
- Community contributors and translators / 社区贡献者和翻译者
- Beta testers and feedback providers / Beta 测试者和反馈提供者

---

*This changelog follows the format established by [Keep a Changelog](https://keepachangelog.com/).*
*此更新日志遵循 [Keep a Changelog](https://keepachangelog.com/) 建立的格式。*

