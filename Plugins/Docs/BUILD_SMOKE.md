# 插件 Smoke Build

用于快速验证插件侧能否正常编译。

## 命令

```powershell
# 自定义鼠标插件 smoke

dotnet build Plugins/CustomMouse/LenovoLegionToolkit.Plugins.CustomMouse.csproj -nologo
```

```powershell
# ShellIntegration 插件 smoke（需要主仓库编译产物）
# 先在 LenovoLegionToolkit 仓库执行 scripts\smoke-build.ps1

dotnet build Plugins/ShellIntegration/LenovoLegionToolkit.Plugins.ShellIntegration.csproj -nologo
```

## 建议

- 先执行主仓库 `scripts\smoke-build.ps1`，插件会自动回退到主仓库的 Debug 输出作为依赖（若 Dependencies\Host 为空）。
- 确认已安装 Windows Desktop 工作负载（插件包含 WPF 控件）。
- 若处于离线或内网环境，建议配置 NuGet 源镜像以减少恢复失败。

## 常见失败与处理

- **找不到 SDK / SDK 版本不匹配**
  - 报错示例：`error NETSDK1045` / `SDK not found`
  - 处理：安装/更新 .NET SDK（建议使用项目要求的主版本）。

- **缺少 Windows Desktop 工作负载**
  - 报错示例：`NETSDK1136` / `WindowsDesktop` related workload missing
  - 处理：
    ```powershell
    dotnet workload install windowsdesktop
    ```

- **无法访问 nuget.org**
  - 报错示例：`NU1301` / `NU1900` 等
  - 处理：检查网络代理或临时离线环境设置，再重试。
