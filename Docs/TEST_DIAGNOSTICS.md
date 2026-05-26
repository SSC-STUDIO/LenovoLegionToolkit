# 测试环境诊断与修复指�?
## 问题描述

运行 `dotnet test` 后，testhost.exe 进程会持续锁定测试项目的 DLL 文件，导致后续测试运行失败�?
**错误信息**:
```
error MSB3021: 无法将文�?...LenovoLegionToolkit.Lib.dll"复制�?..."
The process cannot access the file '...' because it is being used by another process.
文件�?testhost.exe (PID)"锁定
```

## 根本原因

1. **Visual Studio Test Explorer**: 如果 Visual Studio 的测试资源管理器正在运行，它会保�?testhost.exe 进程活跃
2. **dotnet test 并行运行**: 测试进程没有正确清理
3. **Windows 文件锁定机制**: Windows 会锁定已加载�?DLL 文件

## 解决方案

### 方案 1: 使用 --no-build 标志（推荐用�?CI/CD�?
```bash
# 先构建一�?dotnet build LenovoLegionToolkit.Tests/UniversalDeviceToolkit.Tests.csproj -c Release

# 运行测试时不重新构建
dotnet test LenovoLegionToolkit.Tests/UniversalDeviceToolkit.Tests.csproj -c Release --no-build --verbosity normal
```

### 方案 2: 清理并重�?
```bash
# 1. 关闭 Visual Studio
# 2. 清理项目
dotnet clean LenovoLegionToolkit.Tests/UniversalDeviceToolkit.Tests.csproj

# 3. 手动删除 bin/obj 目录（如�?clean 不成功）
rm -rf LenovoLegionToolkit.Tests/bin LenovoLegionToolkit.Tests/obj

# 4. 重新运行测试
dotnet test LenovoLegionToolkit.Tests/UniversalDeviceToolkit.Tests.csproj
```

### 方案 3: 终止 testhost 进程（管理员权限�?
```powershell
# 以管理员身份运行 PowerShell
Get-Process testhost | Stop-Process -Force

# 或者使�?taskkill（管理员命令提示符）
taskkill /F /IM testhost.exe
```

### 方案 4: 重启计算�?
如果以上方法都不奏效，重启计算机会释放所有文件锁定�?
## CI/CD 配置建议

�?GitHub Actions �?Azure DevOps 中，使用以下配置避免此问题：

```yaml
# GitHub Actions 示例
- name: Build
  run: dotnet build --configuration Release

- name: Test
  run: dotnet test --no-build --configuration Release --verbosity normal
```

## 验证测试是否通过

本次代码修改后的预期测试结果�?
| 测试类别 | 预期结果 | 备注 |
|---------|---------|------|
| AbstractSettings 测试 | �?通过 | 线程安全修复验证 |
| BatteryDischargeRateMonitorService 测试 | �?通过 | CTS 竞态条件修复验�?|
| AutomationProcessor 测试 | �?通过 | IDisposable 实现验证 |
| PluginManager 测试 | �?通过 | 插件加载和版本检查验�?|
| 集成测试 | ⚠️ 需监控 | 关注内存泄漏和资源清�?|

## 本地开发工作流程建�?
### 推荐的开发循�?
```bash
# 1. 修改代码
# ...

# 2. 构建（不运行测试�?dotnet build UniversalDeviceToolkit.sln

# 3. 运行特定测试（避免锁定所�?DLL�?dotnet test LenovoLegionToolkit.Tests --filter "FullyQualifiedName~AbstractSettings"

# 4. 完成所有修改后，运行完整测试套�?dotnet clean
dotnet test LenovoLegionToolkit.Tests
```

### Visual Studio 用户注意事项

1. **禁用"在生成后运行测试"**: Tools > Options > Test > General
2. **关闭 Live Unit Testing**: Test > Live Unit Testing > Stop
3. **使用 Test Explorer �?Run All"而不是单个测�?*: 这样可以复用 testhost 进程

## 相关文件

- 测试项目: `LenovoLegionToolkit.Tests/UniversalDeviceToolkit.Tests.csproj`
- 主要测试文件:
  - `ThrottleFirstDispatcherTests.cs`
  - `CMDTests.cs`
  - (其他测试文件)

## MainAppPluginUi.Smoke 执行与诊�?
## VisualRegression.Smoke 页面视觉回归检�?
WPF-UI 迁移或主�?布局相关改动完成后，先构建主程序和视觉烟测工具：

```bash
dotnet build LenovoLegionToolkit.WPF/UniversalDeviceToolkit.WPF.csproj --configuration Release --no-restore
dotnet build Tools/VisualRegression.Smoke/VisualRegression.Smoke.csproj --configuration Release --no-restore
```

再运行逐页截图烟测�?
```bash
dotnet Tools/VisualRegression.Smoke/bin/Release/net10.0-windows/VisualRegression.Smoke.dll \
  --repo-root . \
  --configuration Release \
  --output-dir Build/visual-regression-local \
  --theme Dark
```

检�?`Build/visual-regression-local/result.json` �?`error` 是否�?`null` �?`errorLogs` 是否为空，并�?`current/storyboard.html` 或单�?PNG 逐页复核。WPF-UI 4 迁移后必须重点看这些位置�?
- 主窗口标题栏左侧�?`Log` 和设备信息按钮应�?hover/click 行为，且不应贴近右侧最小化/最大化/关闭按钮�?- About 页在最小窗口尺寸下应显�?`AboutPageScrollViewer` �?`PART_VerticalScrollBar`，内容不应被无滚动裁剪�?- Plugin Extensions 顶部应保持卡片式 `Total Plugins`、`Installed`、`Updates Ready`、`Store Pulse` 汇总，不应退化成整条灰色横幅�?- 深色主题�?About、Plugin Extensions、Settings、System Optimization 的正文、详情值和禁用/次级文字应保持可读对比度�?
### 不打扰本机桌面的推荐方案

如果不希�?smoke 抢占你当前的鼠标、键盘和前台窗口，不要在你正在使用的桌面会话里直接运�?`MainAppPluginUi.Smoke`�?
推荐改为使用仓库内置的独�?UI runner 工作流：

- Workflow: `.github/workflows/MainAppPluginUi.Smoke.yml`
- 触发方式: `workflow_dispatch`
- 目标 runner: `self-hosted`, `Windows`, `LLT-UI-SMOKE`

这个 runner 应满足以下条件：

1. 是独立的 Windows 主机、虚拟机或专用测试会话，而不是你当前正在操作的桌面�?2. Runner 以交互式用户会话运行，不能只作为后台 service �?UIA�?3. 允许主程序真正弹出窗口并执行前台自动化�?
工作流会自动�?
- 构建主程序和 `MainAppPluginUi.Smoke`
- 在专�?runner 上执行真�?UI smoke
- 把日志、截图目录和截图索引上传�?artifact
- 可选开�?watch 模式，让 runner 桌面上的 smoke 过程按真实窗口效果放慢显�?- 在截图目录里额外生成 `storyboard.html`，可直接打开按步骤回放整�?smoke 过程

推荐的默认输入是�?
- `scenario`: `combo-local`
- `plugin_ids`: `shell-integration,custom-mouse`
- `plugin_sources`: `shell-integration=online,custom-mouse=local`
- `theme`: `dark`
- `screenshot_mode`: `always`
- `watch`: `true`
- `step_delay_ms`: `1200`

工作流内部通过 `Tools/MainAppPluginUi.Smoke/Run-MainAppPluginUi.Smoke.ps1` 调用 smoke，并把产物落�?runner 临时目录后统一上传�?
### 推荐执行顺序

```bash
# 1. 先确�?smoke 工具本身可构�?dotnet build Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false

# 2. 先跑单插件样�?LLT_SMOKE_PLUGIN_IDS=shell-integration dotnet run --project Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj -c Release --no-build -- <repo-root>

# 3. 再跑默认集合
dotnet run --project Tools/MainAppPluginUi.Smoke/MainAppPluginUi.Smoke.csproj -c Release --no-build -- <repo-root>
```

如果你必须本机直跑，建议至少加上�?
```bash
dotnet Tools/MainAppPluginUi.Smoke/bin/Release/net10.0-windows/MainAppPluginUi.Smoke.dll \
  --repo-root <repo-root> \
  --scenario combo-local \
  --theme dark \
  --watch \
  --step-delay-ms 1200 \
  --success-hold-ms 5000 \
  --failure-hold-ms 15000 \
  --screenshots always \
  --screenshot-dir Build/main-app-plugin-ui-smoke-local
```

但这仍然会打扰当前桌面，只是会把证据归档得更完整�?
`--watch` 模式会：

- 每个关键页面切换后停留一小段时间，便于肉眼观�?- 成功结束前保留主窗口一段时�?- 失败时保留失败状态更久，便于直接看异常页面或错误提示

截图产物会同时包含：

- `index.md`: 文本索引
- `storyboard.html`: 可直接打开的图片回放页
- `*.png`: 每个关键步骤的真实窗口截�?
如果你需要逐页�?UI 审查，建议优先使用这两组本地预设�?
- `--scenario shell-local --theme dark|light`
- `--scenario combo-local --theme dark|light`

这样会在干净沙箱里走真实本地导入路径，并�?`Scenario` �?`Theme` 一起写进截图索引，便于对照同一批页面的深浅色版本�?
这条模式的目标是“和真实桌面效果一致且可观看”，而不是为了最快速度执行�?
### 2026-03-24 已验证现�?
| 场景 | 结果 | 诊断结论 |
|------|------|----------|
| `shell-integration` 单插�?smoke | �?PASS | 已走�?marketplace �?optimization route；验证了设置按钮、启�?禁用动作，并生成截图证据�?|
| `custom-mouse` 单插�?smoke | �?FAIL | 已进�?Windows Optimization 页面，但等待 `WindowsOptimizationCategory_custom.mouse` 超时，说明失败点在优化分类定位，不是主程序未启动�?|
| `network-acceleration` 单插�?smoke | �?FAIL | 主程序尚未启动；`PrepareRuntimePluginFixtures(...)` 删除运行时插件目录时�?`LenovoLegionToolkit.Plugins.ViveTool.resources.dll` 触发 `UnauthorizedAccessException`�?|
| 默认插件集合 smoke | �?FAIL | 与上面相同，启动前就�?runtime fixture 清理/文件锁定问题阻断�?|

### 如何判读这类失败

- 若日志已出现 `Main window ready`、`Navigated to Plugin Extensions page`，说明主程序启动链路基本正常，失败更可能在具体插件入口或 UIA 定位�?- 若失败栈停在 `PrepareRuntimePluginFixtures(...)`，优先按“运行时插件目录被占�?/ 文件锁定”排查，而不是先�?marketplace 或页面逻辑�?- 若优化路由插件失败且日志显示已进�?`Windows Optimization page`，优先检查目标分类的 AutomationId、分类加载时序、以及插件是否真的暴露了对应 optimization category�?
### MainAppPluginUi.Smoke 当前已知限制

1. 运行�?fixture 准备阶段会对目标插件目录做备�?替换；如果运行时目录里仍�?DLL 被占用，smoke 可能在主程序启动前失败�?2. 当前环境�?`shell-integration` 已有完整 PASS 证据，但默认插件全集尚无一次干净通过记录�?3. �?`custom-mouse`，当前证据表明问题集中在 optimization category 可见性，而不�?marketplace 可用性�?
## 已知限制

1. �?Windows 上，dotnet test �?testhost.exe 进程有时会保持活跃状�?2. 这是 .NET SDK 的已知行为，与项目代码无�?3. 使用 `--no-build` 是最佳实�?
## 参考链�?
- [.NET Test Documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-test)
- [File Locking in Windows](https://docs.microsoft.com/en-us/windows/win32/fileio/file-locking)
- [Visual Studio Test Explorer](https://docs.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer)

---

**最后更�?*: 2026-02-26
**适用版本**: .NET 10.0, xUnit 2.x
