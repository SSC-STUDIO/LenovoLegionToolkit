# SDK 接口变更日志

本文档记录 `SDK/` 目录中接口的变更历史，确保插件与主应用保持兼容。

## 版本控制

SDK 版本遵循 **主应用版本号**：
- 主应用 v4.2.1 → SDK 版本 4.2.1
- 插件必须声明 `MinimumHostVersion` 匹配 SDK 版本

## 变更记录

### v4.2.1 (2026-07-06)
**新增**:
- `PluginHostContext.cs`: 添加 `SetPluginResourceCultures()` 方法
- `IPluginHostContext.cs`: 添加 `IsPreviewMode` 属性

**修改**: 无

**弃用**: 无

**移除**: 无

**迁移指南**: 无

### v4.0.0 (2026-06-07)
**新增**:
- `IAppStartupPlugin.cs`: 应用启动插件接口
- `IOptimizationCategoryProvider.cs`: 优化类别提供器接口
- `PluginHostContext.cs`: 完整的宿主上下文实现

**修改**:
- `PluginBase.cs`: 添加 `GetFeatureExtension()` 和 `GetOptimizationCategory()` 虚拟方法

**弃用**: 无

**移除**: 无

**迁移指南**: 插件应继承 `PluginBase` 而非直接实现 `IPlugin`。

### v3.6.1 (2026-03-01)
**新增**:
- `IPluginPage.cs`: 插件页面接口
- `IPluginHostContext.cs`: 插件宿主上下文接口

**修改**: 无

**弃用**: 无

**移除**: 无

**迁移指南**: 无

## 兼容性矩阵

| 插件 SDK 版本 | 最低主应用版本 | 兼容主应用版本 |
|---------------|----------------|----------------|
| 4.2.1         | 4.2.1          | 4.2.1+        |
| 4.0.0         | 4.0.0          | 4.0.0+        |
| 3.6.1         | 3.6.1          | 3.6.1+        |

## 检查清单

在发布新版本前，确认：
- [ ] 所有接口变更记录在本文档
- [ ] 插件 `plugin.manifest.json` 中的 `minHostVersion` 已更新
- [ ] 主应用版本号与 SDK 版本号一致
- [ ] 弃用接口已标记 `[Obsolete]`
- [ ] 迁移指南已提供

## 自动检查

运行以下命令验证 SDK 版本一致性：

```powershell
# 检查 SDK 版本
Select-Xml -Path "SDK\*.csproj" -XPath "//Version" | ForEach-Object { $_.Node.InnerText }

# 检查插件 minHostVersion
Get-ChildItem -Path "Plugins\*\plugin.manifest.json" | ForEach-Object {
    $manifest = Get-Content $_.FullName | ConvertFrom-Json
    Write-Host "$($_.Directory.Name): minHostVersion = $($manifest.minHostVersion)"
}
```

---
*本文档由自主维护工作流自动更新。*
