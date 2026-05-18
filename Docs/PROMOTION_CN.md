# 中文发布文案

以下内容用于 GitHub Release、社区帖、社交平台和下载说明同步。发布前请把版本号、下载链接和校验文件名替换为实际值。

## 短版

Lenovo Legion Toolkit vX.Y.Z 已发布。此版本重点修复了控制台传感器卡片、性能模式显示稳定性、插件扩展页布局和在线插件安装流程，同时补强了 .NET 10/WPF 运行时兼容和视觉/插件烟测链路。下载请以 GitHub Releases、winget 和社区 Scoop 包为准，并优先校验随附的 `SHA256.txt`。

## 详细版

Lenovo Legion Toolkit vX.Y.Z 已发布。

本次更新重点包括：

- 恢复并优化控制台首页的 CPU / Battery / GPU 传感器大卡片布局
- 传感器详情默认折叠，支持双击展开，并补充悬浮提示
- 性能模式卡片在 Lenovo WMI 短时失败时不再整块消失
- 修复部分 CPU / GPU 传感器读数缺失、假值和细节刷新路径问题
- 优化插件扩展页留白、默认选中行为、说明文案和使用指南
- 完整打通在线插件安装、设置、打开、卸载烟测流程
- 修复启动期未观察任务异常和部分 WMI 瞬态失败导致的稳定性问题

下载方式：

- GitHub Releases: <https://github.com/SSC-STUDIO/LenovoLegionToolkit/releases/latest>
- winget: `winget install SSC-STUDIO.LenovoLegionToolkit`
- Scoop: `scoop install extras/lenovolegiontoolkit`

校验建议：

- 优先使用 GitHub Release 中的 `LenovoLegionToolkit_vX.Y.Z_SHA256.txt`
- 转发镜像时同步附带校验文件
- 对外说明中明确 GitHub Releases 和 winget 为权威下载源
