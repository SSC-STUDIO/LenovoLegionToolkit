# Universal Device Toolkit Plugins 架构文档

## 概述

Universal Device Toolkit Plugins 是通用设备工具包的官方插件系统，采用独立构建模式，编译产物以 ZIP 包形式发布。

当前工具链围绕两条路径设计：

1. 贡献者路径：`doctor -> init -> dev -> test -> validate -> package`
2. 官方收录路径：在贡献者路径基础上补 `promote`、`plugin.manifest.json` 的 `store` 元数据和官方发布流程

## 项目结构

```
UniversalDeviceToolkit-Plugins/
├── SDK/                          # 插件SDK接口层
│   ├── PluginBase.cs             # 插件基类（提供生命周期方法）
│   ├── PluginAttribute.cs         # 插件元数据特性
│   └── IPluginPage.cs            # 插件页面接口
├── Plugins/                      # 插件目录
│   ├── Shared/                   # 共享工具库 (Priority 0)
│   │   ├── HttpClientManager.cs   # 单例HttpClient管理
│   │   ├── WpfFallbackHelper.cs  # WPF回退UI辅助类
│   │   ├── ProcessRunner.cs       # 安全进程执行器
│   │   ├── SettingsManager.cs    # 统一设置持久化管理
│   │   └── Constants.cs           # 魔法数字集中定义
│   ├── Shared.Tests/             # 共享库测试 (140 tests)
│   ├── CustomMouse/              # 鼠标自定义插件
│   ├── CustomMouse.Tests/        # 鼠标插件测试 (40 tests)
│   ├── ShellIntegration/         # Shell集成插件
│   ├── ShellIntegration.Tests/    # Shell插件测试 (93 tests)
│   ├── NetworkAcceleration/      # 网络加速插件
│   ├── NetworkAcceleration.Tests/ # 网络插件测试 (33 tests)
│   ├── ViveTool/                 # ViVeTool功能控制插件
│   │   └── Services/            # 拆分后的服务层
│   │       ├── IViveToolService.cs
│   │       ├── ViveToolService.cs        # 聚合服务 (198行)
│   │       ├── ViveToolFeatureService.cs  # Feature操作 (456行)
│   │       ├── ViveToolDownloadService.cs # 下载服务 (313行)
│   │       ├── ViveToolPathService.cs      # 路径服务 (239行)
│   │       └── ViveToolProcessService.cs   # 进程服务 (48行)
│   └── ViveTool.Tests/          # ViveTool测试 (217 tests)
├── Dependencies/                # 外部依赖
│   └── Host/                    # 宿主应用引用与基线清单
│       └── host-release.json    # 独立开发使用的主程序 release 基线
├── Build/                       # 构建输出
└── Tools/                       # 工具项目
    ├── PluginTooling.Core/      # 共享作者工具核心
    ├── PluginTooling.Cli/       # 标准作者命令入口
    ├── PluginCompletionUiTool/  # 维护者校验 UI
    ├── PluginWorkbench/         # 宿主级预览工作台
    └── PluginWorkbench.Smoke/   # 工作台 UI 冒烟验证
```

## 架构原则

### 1. 独立构建模式
- 插件项目不引用主应用源码
- 使用 `Dependencies/Host` 中的预编译引用
- 通过 `Scripts/ensure-host-dependencies.ps1` 刷新宿主引用
- 若本机没有 sibling `UniversalDeviceToolkit` 构建输出，则回退到 `host-release.json` 声明的主程序 release ZIP

### 2. SDK抽象层
- 提供 `PluginBase` 作为所有插件的基类
- 定义 `IPluginPage` 接口规范UI页面实现
- `PluginAttribute` 标记插件元数据
- `PluginHostContext` 为插件提供宿主无关的设置页打开、对话框承载与运行模式能力

### 2.1 元数据分层

插件元数据现在拆成三层：

- `plugin.manifest.json`: 作者编辑的统一清单，包含运行时身份、贡献点、打包规则和官方商店元数据
- `plugin.json`: 为当前宿主加载保留的运行时兼容输出
- `store-entry.json`: 为旧发布脚本保留的官方元数据兼容输出
- 根 `store.json`: 发布输出，不再作为新插件作者的日常编辑入口

### 2.1 独立宿主模式
- `PluginWorkbench` 直接加载 `Build/plugins/...` 输出或本地 ZIP 包
- 默认 `Preview` 模式只承载 UI，不执行系统变更
- 切换到 `Real Runtime` 后才运行插件启动钩子和优化动作
- 支持 `System / Light / Dark`，并桥接主程序的宿主样式资源
- 插件配置隔离在 `Build/PluginWorkbenchState/<Mode>/...`
- `PluginWorkbench.Smoke` 使用 Windows UI Automation 验证主题切换、宿主壳和 `Preview -> Real Runtime` 交互

### 3. 共享工具库 (Shared)
消除跨插件代码重复，提供：
- **HttpClientManager**: 单例模式，避免socket耗尽
- **WpfFallbackHelper**: 统一的XAML回退UI模式
- **ProcessRunner**: 命令注入防护的进程执行
- **SettingsManager**: 统一设置持久化策略
- **Constants**: 魔法数字集中管理

### 4. 作者工具链

标准作者入口是 `PluginTooling.Cli`：

- `doctor`
- `init`
- `dev`
- `build`
- `test`
- `preview`
- `validate`
- `package`
- `migrate`
- `promote`

`PluginCompletionUiTool` 继续保留，但定位为维护者/仓库侧校验 UI，而不是第一次贡献者的标准入口。

## 本地化架构

### 资源文件结构
```
Resources/
├── Resource.resx           # 英文回退值（默认）
├── Resource.zh.resx       # 中文翻译
└── Resource.zh-Hant.resx  # 繁体翻译
```

### 文本类模式
```csharp
public static class SomeText {
    // 英文回退值
    private const string DefaultPluginName = "Plugin Name";

    public static string PluginName =>
        Resources.Resource.ResourceManager.GetString(nameof(PluginName))
        ?? DefaultPluginName;
}
```

### 本地化测试
每个插件提供 `TextTests` 验证：
- `TextClass_HasNoHardcodedChinese`: 验证回退值不含硬编码中文
- `TextClass_FallbackValues_AreEnglish`: 验证回退机制
- `AllResourceKeys_AreAccessible`: 验证资源键可访问
- `CommonKeys_ReturnNonEmptyString`: 验证常用键返回非空值

## 生命周期

### 插件生命周期
```
Load → Start → [Runtime Loop] → Stop → Unload
```

- **Load**: 初始化UI控件和设置
- **Start**: 启动后台运行时
- **Runtime Loop**: 执行后台任务
- **Stop**: 停止运行时
- **Unload**: 清理资源

### CancellationToken传播
运行时使用 `CancellationToken` 控制生命周期：
- `PluginBase.RuntimeCancellationTokenSource` 提供运行时取消令牌
- 所有异步操作应支持取消
- 避免使用 `async void`，统一使用 `async Task`

## 测试覆盖

| 项目 | 测试数 | 覆盖率 |
|------|--------|--------|
| Shared | 140 | ~80% |
| ViveTool | 217 | ~70% |
| ShellIntegration | 93 | ~60% |
| CustomMouse | 40 | ~60% |
| NetworkAcceleration | 33 | ~60% |
| **总计** | **523** | - |

## 安全实践

### 1. 路径遍历防护
- 所有文件操作使用 `Path.GetFullPath()` 规范化路径
- 验证路径在预期目录内
- 禁止用户控制的路径成分

### 2. 命令注入防护
- `ProcessRunner` 验证所有参数
- 禁止特殊字符（`;`, `|`, `&`, `$`等）
- 路径白名单检查

### 3. HttpClient单例
- 使用 `Lazy<HttpClient>` 避免socket耗尽
- 不在每次请求时创建新实例

## 性能优化

### 构建性能
- `Directory.Build.props` 统一构建配置
- 插件清理Target集中管理
- 并行构建支持

### 运行时性能
- 避免 `Task.Wait()` 阻塞调用
- 使用 `ConfigureAwait(false)` 优化异步性能
- HttpClient单例减少连接开销

## 版本管理

### 版本号同步
- `Directory.Build.props`: 主版本
- `*.csproj`: 文件版本
- `plugin.manifest.json`: 插件版本、贡献点、包名与商店元数据
- `plugin.json`: 兼容运行时清单
- `store.json`: 发布元数据

### 发布流程
1. 更新所有版本文件
2. 更新 `CHANGELOG.md`
3. 运行 `dotnet build` / `dotnet test`
4. 触发 `workflow_dispatch` 构建
5. GitHub Actions 自动创建发布和ZIP

## 依赖关系图

```
┌─────────────────────────────────────────────────────────────┐
│                   Universal Device Toolkit                   │
│                       (Host App)                             │
└─────────────────────────┬───────────────────────────────────┘
                          │ PluginBase, IPluginPage (SDK)
┌─────────────────────────┴───────────────────────────────────┐
│                      SDK Layer                               │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ PluginBase | PluginAttribute | IPluginPage | Interfaces │ │
│  └─────────────────────────────────────────────────────────┘ │
└─────────────────────────┬───────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┬───────────────┐
          │               │               │               │
          ▼               ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   ViveTool      │ │ CustomMouse     │ │ShellIntegration │ │NetworkAcceler.  │
│   Services/     │ │                 │ │                 │ │                 │
│                 │ │                 │ │                 │ │                 │
│ ─────────────── │ │ ─────────────── │ │ ─────────────── │ │ ─────────────── │
│ FeatureService  │ │ CustomMouseText │ │ThemeWatcher     │ │NetworkAccel.    │
│ DownloadService │ │ SettingsControl │ │SettingsControl │ │ Runtime         │
│ PathService     │ │                 │ │                 │ │ SettingsControl │
│ ProcessService  │ │                 │ │                 │ │                 │
└────────┬────────┘ └────────┬────────┘ └────────┬────────┘ └────────┬────────┘
         │                    │                    │                    │
         └────────────────────┴────────┬───────────┴────────────────────┘
                                      │
                          ┌───────────┴───────────┐
                          │   Shared Library     │
                          │                      │
                          │ HttpClientManager    │
                          │ WpfFallbackHelper    │
                          │ ProcessRunner        │
                          │ SettingsManager      │
                          │ Constants            │
                          └──────────────────────┘
```

## 未来改进方向

### Phase 3 (测试)
- 提升各插件测试覆盖率至80%
- 添加集成测试覆盖关键路径
- 使用Mock框架隔离依赖

### Phase 4 (文档)
- [x] 架构文档 (本文档)
- [ ] XML API文档
- [ ] .editorconfig代码风格规范
- [ ] coding-standards.md

### Phase 5 (性能)
- [ ] 构建性能优化
- [ ] 运行时内存优化
- [ ] 异步性能分析
