# Changelog / 更新日志

All notable changes to this project will be documented in this file.
此项目的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

### Added / 新增
- Shell Integration plugin for enhanced Windows context menu functionality / Shell集成插件，增强Windows右键菜单功能
- System optimization Extensions tab for managing installed plugins / 系统优化扩展标签页，用于管理已安装的插件
- Context menu item management with customizable commands / 右键菜单项管理，支持自定义命令
- Shell extension management with enable/disable controls / Shell扩展管理，支持启用/禁用控制
- Multi-language support for Shell Integration plugin (English and Chinese) / Shell集成插件的多语言支持（英文和中文）
 - Plugin Extension ViewModel for better integration with system optimization / 插件扩展ViewModel，更好地与系统集成
- Plugin icon background color mapping for different plugin types / 不同插件类型的图标背景颜色映射
- Shell Integration functionality migrated to plugin architecture / Shell集成功能迁移到插件架构
- PluginManager TryGetPlugin method for better plugin discovery / PluginManager TryGetPlugin方法，改进插件发现


### Fixed / 修复
- Added missing ExtensionsNavButton_Checked event handler for plugin tab navigation / 添加了缺失的ExtensionsNavButton_Checked事件处理器，用于插件标签页导航
- Plugin bulk import improvements for compiled plugins (DLL-only packages) / 针对编译插件（仅包含DLL的包）的批量导入改进
- Fixed ViveTool plugin compilation error / 修复ViveTool插件编译错误

### Improved / 改进
- System optimization page now integrates with plugin system / 系统优化页面现在与插件系统集成
- Plugin management interface in Windows Optimization settings / Windows优化设置中的插件管理界面
- Enhanced plugin discovery for shell integration and system tools / Shell集成和系统工具的增强插件发现
- Better error handling for plugin configuration operations / 插件配置操作的更好错误处理
- Extensions tab now shows actual plugins instead of "Coming Soon" placeholder / 扩展标签页现在显示实际插件而非"即将推出"占位符
- Plugin extensions list updates when switching to Extensions tab / 切换到扩展标签页时插件扩展列表更新
- Refactored Shell Integration from core system to plugin-based architecture / 将Shell集成从核心系统重构为基于插件的架构
- Removed beautification-related code from WindowsOptimizationService and WindowsOptimizationPage / 从WindowsOptimizationService和WindowsOptimizationPage中移除美化相关代码
- Moved NilesoftShellHelper to ShellIntegration plugin / 将NilesoftShellHelper移动到ShellIntegration插件
- Organized working directory: removed unused templates, moved shell integration files to plugin directory / 整理工作目录：删除未使用的模板，将Shell集成文件移动到插件目录
- Refactored shell integration helper usage to instance-based pattern for consistency / 重构Shell集成helper使用为基于实例的模式以确保一致性
- Removed Extensions tab and integrated Shell functionality into Beautification section / 移除扩展标签页并将Shell功能集成到美化部分
- Automatically copy plugins to publish directory during build / 构建期间自动将插件复制到发布目录
- Shell Integration plugin optimization categories now correctly appear in Beautification section / Shell集成插件优化类别现在正确显示在美化部分
- Added automatic Shell Extension unregistration during plugin updates and uninstallation / 插件更新和卸载期间自动取消Shell扩展注册


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
- Plugin details panel with automatic icon generation / 自动图标生成的插件详情面板
- Performance optimizations for plugin loading and management / 插件加载和管理的性能优化
- Plugin resource file organization and maintainability / 插件资源文件组织性和可维护性

---

## [3.1.3] - 2025-12-XX

### Added / 新增
- Battery min/max discharge rate and wear level information / 电池最小/最大放电率和损耗等级信息
- Session lock/unlock automation pipeline / 会话锁定/解锁自动化流水线
- Notifications Always on Top (AoT) feature / 通知置顶功能
- JIS layout keyboard support / JIS 布局键盘支持

### Fixed / 修复
- Actions freeze when SmartKey is used / 使用 SmartKey 时操作冻结的问题
- Display brightness not remembered in actions / 操作中亮度不被记忆的问题
- Various UI and stability issues / 各种界面和稳定性问题

### Improved / 改进
- Use display path APIs for better 60Hz battery life / 使用显示路径 API 提升电池续航
- Updated .NET runtime version and dependencies / 更新 .NET 运行时版本和依赖项

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
- UI responsiveness and performance / 界面响应性和性能
- Error messages and user feedback / 错误消息和用户反馈
- Hardware detection and compatibility / 硬件检测和兼容性

---

## [3.0.5] - 2025-10-XX

### Fixed / 修复
- Installer build in make.bat / make.bat 中的安装程序构建
- GitHub Actions workflows and submodules / GitHub Actions 工作流和子模块

### Improved / 改进
- Update checker functionality / 更新检查器功能
- Shell integration stability / Shell 集成稳定性
- Build process and CI/CD pipeline / 构建过程和 CI/CD 流水线

---

## [3.0.1] - 2025-09-XX

### Added / 新增
- .NET 8.0 migration / .NET 8.0 迁移
- Improved error handling and logging / 改进的错误处理和日志记录
- Shell integration enhancements / Shell 集成增强

### Fixed / 修复
- ShellIntegration submodule paths and build artifacts / ShellIntegration 子模块路径和构建产物
- Installation and distribution issues / 安装和分发问题

### Improved / 改进
- Performance optimizations / 性能优化
- Code cleanup and refactoring / 代码清理和重构

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
- Performance optimization for sensors / 传感器性能优化
- Notification system and positioning / 通知系统和定位
- Compatibility with newer Windows versions / 与较新 Windows 版本的兼容性

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
- Automation pipeline processing / 自动化流水线处理
- Hardware monitoring and sensors / 硬件监控和传感器
- User interface responsiveness / 用户界面响应性

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
- User activity detection / 用户活动检测
- Battery information accuracy / 电池信息准确性
- Overall system performance / 整体系统性能

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
- Dashboard customization and layout / 仪表板自定义和布局
- RGB lighting consistency / RGB 灯光一致性
- Overall application performance / 整体应用程序性能

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
- Fan control algorithms / 风扇控制算法
- Temperature monitoring accuracy / 温度监控准确性
- System stability under load / 负载下系统稳定性

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
- GPU management and control / GPU 管理和控制
- Power efficiency optimization / 功效优化
- Hardware compatibility detection / 硬件兼容性检测

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
- Automation performance and reliability / 自动化性能和可靠性
- User interface for automation setup / 自动化设置用户界面
- Error handling in automation pipelines / 自动化流水线中的错误处理

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
- Download management and reliability / 下载管理和可靠性
- User interface for system info / 系统信息用户界面
- Overall application stability / 整体应用程序稳定性

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
- Power management algorithms / 电源管理算法
- Hardware control precision / 硬件控制精度
- User interface responsiveness / 用户界面响应性

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
- Keyboard control reliability / 键盘控制可靠性
- Display management / 显示管理
- Power management integration / 电源管理集成

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
- RGB lighting performance / RGB 灯光性能
- User interface for RGB controls / RGB 控制用户界面
- Hardware compatibility / 硬件兼容性

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