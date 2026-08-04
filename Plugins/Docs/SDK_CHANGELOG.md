# SDK 接口变更日志

记录 `Plugins/SDK/Runtime/` 与宿主兼容约定的变更，便于插件与 **Universal Device Toolkit** 主应用对齐。

## 版本控制

- SDK 能力随 **宿主** 发布基线演进；当前 vendored 宿主见 `Plugins/Dependencies/Host/host-release.json`。
- 插件必须在 `plugin.manifest.json` 中声明 `minHostVersion`。
- 运行时兼容文件 `plugin.json` 仍使用宿主 ABI 字段名 `MinLltVersion`（值 = UDT 最低宿主版本）。

## 变更记录

### v5.0.0 (2026-07-14 起 / 文档对齐 2026-07-18)

**宿主**

- Phase 3 ABI hard cutover：编译标识 `LenovoLegionToolkit.*` → `UniversalDeviceToolkit.*`
- Host assemblies：`UniversalDeviceToolkit.Lib` / `UniversalDeviceToolkit.Lib.Plugins` / `Universal Device Toolkit`
- Vendored host baseline：`5.0.0`

**插件仓库**

- 官方插件 `minHostVersion` / `MinLltVersion` / `[Plugin] MinimumHostVersion` 统一为 **5.0.0**
- 工具入口推荐 `udt-plugin.cmd`（`llt-plugin.cmd` 兼容别名）

**迁移指南**

- 旧 `LenovoLegionToolkit.Plugins.*` 包需在新宿主下重新编译发布
- `%LocalAppData%\LenovoLegionToolkit\` 仍可能作为**只读**设置迁移源；写入根为 `%LocalAppData%\UniversalDeviceToolkit\`

### v4.2.1 (2026-07-06)

**新增**

- `PluginHostContext.SetPluginResourceCultures()`
- `IPluginHostContext.IsPreviewMode`

**说明**

- 曾作为 4.x 系列宿主基线；新插件应面向 5.0.0+

### v4.0.0 (2026-06-07)

**新增**

- `IAppStartupPlugin`
- `IOptimizationCategoryProvider`
- 完整 `PluginHostContext`

**修改**

- `PluginBase`：`GetFeatureExtension()` / `GetOptimizationCategory()`

**迁移指南**

- 插件应继承 `PluginBase`，而非直接实现 `IPlugin`

### v3.6.1 (2026-03-01)

**新增**

- `IPluginPage`
- `IPluginHostContext`

## 兼容性矩阵

| 插件 / SDK 基线 | 最低主应用 | 说明 |
|-----------------|------------|------|
| 5.0.0           | 5.0.0+     | 当前官方插件与 vendored 宿主 |
| 4.2.1           | 4.2.1+     | 历史 4.x 基线 |
| 4.0.0           | 4.0.0+     | 历史 |
| 3.6.1           | 3.6.1+     | 历史 |

## 发布前检查清单

- [ ] 接口变更写入本文档
- [ ] 各插件 `plugin.manifest.json` 的 `minHostVersion` 已更新
- [ ] `[Plugin] MinimumHostVersion` 与 manifest 一致
- [ ] `plugin.json` 的 `MinLltVersion` 已同步（字段名保持 ABI）
- [ ] 弃用 API 标记 `[Obsolete]` 并附迁移说明

## 自动检查

```powershell
# 插件 minHostVersion
Get-ChildItem -Path "Plugins\*\plugin.manifest.json" | ForEach-Object {
    $manifest = Get-Content $_.FullName | ConvertFrom-Json
    Write-Host "$($_.Directory.Name): minHostVersion = $($manifest.minHostVersion)  version = $($manifest.version)"
}

# 与 host-release 对照
Get-Content Dependencies\Host\host-release.json
```

---

*维护时优先更新 manifest / host-release，再同步本文档。*
