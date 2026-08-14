# Universal Device Toolkit Plugins 架构文档

## 概述

Universal Device Toolkit Plugins 是通用设备工具包的官方插件系统，采用独立构建模式，编译产物以 ZIP 包形式发布。

当前工具链围绕两条路径设计：

1. 贡献者路径：`doctor -> init -> dev -> test -> validate -> package`
2. 官方收录路径：在贡献者路径基础上补 `promote`、`plugin.manifest.json` 的 `store` 元数据和官方发布流程

## 项目结构

```
UniversalDeviceToolkit/
+- Plugins/Official/         # Official plugin projects and tests
|  +- CustomMouse/
|  +- ShellIntegration/
|  `- ViveTool/
+- Plugins/SDK/Runtime/       # Plugin SDK runtime surface
+- Plugins/Shared/            # Shared plugin helpers
+- Plugins/Shared.Tests/       # Shared helper tests
+- Plugins/Testing/            # Tooling and performance tests
+- Plugins/Tooling/            # CLI and remaining author tools
+- Plugins/HostBaseline/        # Tracked host release manifest; downloaded cache is .host/
+- Plugins/Templates/          # Authoring archetypes
+- Plugins/.build/             # Ignored build, package, and catalog output
+- Docs/Plugins/                # Plugin documentation
`- .github/workflows/plugins-* # Monorepo plugin CI and release workflows
```

## 架构原则

### 1. 独立构建模式
- 插件项目不引用主应用源码
- 使用 `Plugins/.host/<host-version>` 中的预编译引用；仓库不跟踪宿主 DLL
- 通过 `Scripts/ensure-host-dependencies.ps1` 刷新宿主引用
- 若本机没有主仓 `Build/` 构建输出，则回退到 `host-release.json` 声明的主程序 release ZIP

### 2. SDK抽象层
- 提供 `PluginBase` 作为所有插件的基类
- 定义 `IPluginPage` 接口（冻结 ABI；现行 UI 是 `contributes.webPage`）
- `PluginAttribute` 标记插件元数据
- `PluginHostContext` 为插件提供宿主无关的设置页打开、对话框承载与运行模式能力

### 2.1 元数据分层

插件元数据现在拆成三层：

- `plugin.manifest.json`: 作者编辑的统一清单，包含运行时身份、贡献点、打包规则和官方商店元数据
- `plugin.json`: 为当前宿主加载保留的运行时兼容输出
- `store-entry.json`: 为旧发布脚本保留的官方元数据兼容输出
- 生成的 `Plugins/.build/catalog/store.json`: 发布输出，不再作为新插件作者的日常编辑入口

### 2.1 Electron 宿主模式
- 插件 UI 是 `web/index.html`，由 Electron `<webview>` 加载
- `window.pluginHost.invoke` 走与主窗口相同的 JSON-RPC（含 `dialog:*`）
- Host 通过 `IPluginManager.TryGetPlugin` 调用已加载的 C# 插件实例
- `plugins.list` 提供 `directory` + `webPage`；不要用 `plugins.getConfig` 驱动官方三插件

### 3. 共享工具库 (Shared)
消除跨插件代码重复，提供：
- **HttpClientManager**: 单例模式，避免socket耗尽
- **ProcessRunner**: 命令注入防护的进程执行
- **SettingsManager**: 统一设置持久化策略
- **Constants**: 魔法数字集中管理
- 插件页样式：各插件 `web/plugin-ui.css`（`up-*` 类）

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

| 项目 | 测试数（约） | 备注 |
|------|--------------|------|
| Shared | ~167 | 共享库 |
| ViveTool | ~170 | 功能标志插件 |
| ShellIntegration | ~130 | Nilesoft Shell |
| CustomMouse | ~37 | 光标与指针 |
| **合计** | **~500+** | 以 `dotnet test` 为准 |

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
- HttpClient单例减少连接开→

## 版本管理

### 版本号同步
- `Directory.Build.props`: 主版本
- `*.csproj`: 文件版本
- `plugin.manifest.json`: 插件版本、贡献点、包名与商店元数据
- `plugin.json`: 兼容运行时清单
- `Plugins/.build/catalog/store.json`: 发布元数据

### 发布流程
1. 更新所有版本文件
2. 更新 `CHANGELOG.md`
3. 运行 `dotnet build` / `dotnet test`
4. 触发 `workflow_dispatch` 构建
5. GitHub Actions 自动创建发布和ZIP

## 依赖关系图

```
┌─────────────────────────────────────────────────────────────┐
│  Electron shell  +  UniversalDeviceToolkit.Host (JSON-RPC)  │
└─────────────────────────┬───────────────────────────────────┘
                          │ PluginBase, contributes.webPage
┌─────────────────────────┴───────────────────────────────────┐
│                      SDK Layer                               │
│  PluginBase | PluginAttribute | IPluginPage (frozen ABI)     │
└─────────────────────────┬───────────────────────────────────┘
                          │
          ┌───────────────┼───────────────┐
          ▼               ▼               ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│   ViveTool      │ │ CustomMouse     │ │ShellIntegration │
│   web/index.html│ │ web/index.html  │ │ web/index.html  │
│   ViveToolService│ │ SPI / cursors  │ │ ConfigService   │
└────────┬────────┘ └────────┬────────┘ └────────┬────────┘
         └────────────────────┴────────┬───────────┘
                          ┌───────────┴───────────┐
                          │   Shared Library      │
                          │ HttpClientManager     │
                          │ ProcessRunner         │
                          │ SettingsManager       │
                          └───────────────────────┘
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
