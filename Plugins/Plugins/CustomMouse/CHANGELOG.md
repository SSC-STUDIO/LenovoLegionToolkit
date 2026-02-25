# CustomMouse Plugin Changelog / 自定义鼠标插件更新日志

All notable changes to this plugin will be documented in this file.
此插件的所有重要更改都将在此文件中记录。

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
格式基于 [Keep a Changelog](https://keepachangelog.com/en/1.0.0/)，
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
并遵循 [语义化版本](https://semver.org/spec/v2.0.0.html)。

## [Unreleased]

## [1.0.0] - 2026-01-27

### Added / 新增
- Initial release of Custom Mouse plugin / 自定义鼠标插件初始发布
- Intelligent color detection for automatic theme switching / 智能颜色检测，支持自动主题切换
- W11-CC-V2.2-HDPI cursor style support with high DPI optimization / W11-CC-V2.2-HDPI光标样式支持，高DPI优化
- Dual operation modes: Extension and Independent / 双操作模式：扩展和独立
- Plugin configuration management with user preferences / 插件配置管理，支持用户偏好设置
- Mouse cursor real-time preview and status display / 鼠标光标实时预览和状态显示
- Brightness threshold adjustment for better color detection / 亮度阈值调节，优化颜色检测效果
- Startup auto-apply functionality / 启动时自动应用功能
- Mouse style restoration to Windows defaults / 恢复Windows默认鼠标样式
- Multi-language support for plugin interface / 插件界面多语言支持
- Improved user experience with dedicated mouse customization interface / 专用鼠标自定义界面改善用户体验

### Fixed / 修复
- Resolved mouse style application conflicts in system optimization / 解决系统优化中鼠标样式应用冲突
- Fixed resource loading issues for cursor themes / 修复光标主题资源加载问题

### Improved / 改进
- Updated CustomMouse plugin comments to English for broader developer accessibility / 更新CustomMouse插件注释为英文，提高开发者可访问性

### Features / 功能特性
- **Color Detection Algorithm** / **颜色检测算法**:
  - Real-time background color analysis / 实时背景颜色分析
  - Automatic light/dark theme switching / 自动明暗主题切换
  - Configurable brightness threshold / 可配置亮度阈值
  - Manual override options / 手动覆盖选项

- **Cursor Customization** / **光标自定义**:
  - W11-CC-V2.2-HDPI cursor style / W11-CC-V2.2-HDPI光标样式
  - High DPI display scaling support / 高DPI显示缩放支持
  - Real-time cursor preview / 实时光标预览
  - Multiple cursor variants / 多种光标变体

- **Operation Modes** / **操作模式**:
  - Extension Mode: Integrated with system optimization / 扩展模式：与系统集成优化
  - Independent Mode: Standalone application / 独立模式：独立应用程序
  - Mode switching with settings preservation / 模式切换并保留设置

- **User Interface** / **用户界面**:
  - Intuitive settings page / 直观的设置页面
  - Real-time status indicators / 实时状态指示器
  - One-click style application / 一键样式应用
  - Configuration import/export / 配置导入/导出

### Technical Specifications / 技术规格
- **Framework**: .NET 8.0 / .NET 8.0框架
- **Platform**: Windows x64 / Windows x64平台
- **Dependencies**: LenovoLegionToolkit.SDK / LenovoLegionToolkit.SDK依赖
- **Resource Management**: Optimized cleanup / 优化的资源管理
- **Memory Usage**: <50MB typical / 典型内存使用<50MB

---

## 📝 **Development Notes / 开发说明**

### Architecture / 架构
- **Plugin-based Design**: Modular and extensible / 基于插件设计：模块化和可扩展
- **MVVM Pattern**: Separation of concerns / MVVM模式：关注点分离
- **Dependency Injection**: Testable and maintainable / 依赖注入：可测试和可维护
- **Async Operations**: Non-blocking UI / 异步操作：非阻塞UI

### Localization / 本地化
- **Languages Supported**: English, Chinese (Simplified) / 支持语言：英文、中文（简体）
- **Resource Format**: .resx files / 资源格式：.resx文件
- **Culture Support**: en, zh-hans / 文化支持：en、zh-hans
- **Dynamic Loading**: Runtime language switching / 动态加载：运行时语言切换

---

## 🔧 **Future Enhancements / 未来增强**

### Planned Features / 计划功能
- **Advanced Color Detection**: AI-powered theme detection / 高级颜色检测：AI驱动的主题检测
- **Custom Cursor Upload**: User-defined cursor packs / 自定义光标上传：用户定义的光标包
- **Animation Support**: Animated cursor effects / 动画支持：光标动画效果
- **Profile System**: Multiple configuration profiles / 配置文件系统：多配置文件
- **Cloud Sync**: Settings synchronization / 云同步：设置同步

### Technical Roadmap / 技术路线图
- **Performance Optimization**: Lower resource usage / 性能优化：降低资源使用
- **Plugin SDK Extensions**: Additional customization APIs / 插件SDK扩展：额外自定义API
- **Accessibility Features**: Enhanced screen reader support / 无障碍功能：增强屏幕阅读器支持
- **Cross-platform**: Linux and macOS support / 跨平台：Linux和macOS支持

---

## 🐛 **Known Issues / 已知问题**

### Current Limitations / 当前限制
- **High DPI Scaling**: Some cursor sizes may appear inconsistent on very high DPI displays / 高DPI缩放：在极高DPI显示器上某些光标大小可能不一致
- **Color Detection**: May not work correctly with multiple monitors of different brightness / 颜色检测：在不同亮度的多显示器环境下可能无法正常工作
- **Theme Switching**: Some applications may require restart for cursor changes to take effect / 主题切换：某些应用程序可能需要重启才能使光标更改生效

### Troubleshooting / 故障排除
- **Cursor Not Applying**: Check administrator privileges / 光标未应用：检查管理员权限
- **Performance Issues**: Disable real-time preview / 性能问题：禁用实时预览
- **Detection Failures**: Adjust brightness threshold manually / 检测失败：手动调整亮度阈值

---

## 🙏 **Acknowledgments / 致谢**

### Contributors / 贡献者
- **Lead Developer**: Custom Mouse implementation / 主要开发者：自定义鼠标实现
- **UI Design**: User interface design / UI设计：用户界面设计
- **Testing**: Quality assurance and bug reports / 测试：质量保证和错误报告
- **Localization**: Translation and cultural adaptation / 本地化：翻译和文化适配

### Third-party Resources / 第三方资源
- **W11-CC-V2.2**: Windows 11 cursor style / Windows 11光标样式
- **Color Algorithms**: Brightness detection logic / 颜色算法：亮度检测逻辑
- **HDPI Assets**: High-resolution cursor graphics / 高DPI资源：高分辨率光标图形

---

*This changelog covers all changes to the Custom Mouse plugin. For main application changes, see the main CHANGELOG.md.*

*此变更日志记录了CustomMouse插件的所有更改。主应用程序更改请参见主CHANGELOG.md。*