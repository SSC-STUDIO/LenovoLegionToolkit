# 中文发布文案

以下内容用于 GitHub Release、社区帖、社交平台和下载说明同步。发布前请把版本号、下载链接和校验文件名替换为实际值。

## 短版

Universal Device Toolkit vX.Y.Z 已发布。此版本继续延续 Lenovo Legion Toolkit 的轻量、无后台服务、低资源占用和无遥测优势，并把插件扩展作为核心能力：可通过插件扩展页安装、更新、配置和移除设备专用工具。旧 Lenovo Legion Toolkit 用户可直接升级。GitHub Releases 提供 Full 与 Online 两类安装包：Full 内置语言和机型资源，Online 体积更小并通过应用内在线目录安装资源。下载请以 GitHub Releases、winget 和社区 Scoop 包为准，并优先校验随附的 `SHA256.txt`。

## 详细版

Universal Device Toolkit vX.Y.Z 已发布。

本次更新重点包括：

- 公开品牌改为 Universal Device Toolkit，旧 Lenovo Legion Toolkit 用户可直接升级
- 提供 Full 与 Online 两类 GitHub Release 资产，分别面向离线完整安装和轻量在线资源安装
- 保留轻量、无后台服务、低资源占用和无遥测定位
- 突出插件扩展页：支持在线安装、更新、配置、打开和卸载插件
- 插件可承载 CPU、GPU、网络、Shell、鼠标和其他设备专用工作流
- winget `PackageIdentifier` 暂保留 `SSC-STUDIO.LenovoLegionToolkit`
- Scoop manifest 暂保留 `lenovolegiontoolkit`

下载方式：

- GitHub Releases: <https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest>
- winget: `winget install SSC-STUDIO.LenovoLegionToolkit`
- Scoop: `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket && scoop install ssc-studio/lenovolegiontoolkit`

校验建议：

- 优先使用 GitHub Release 中的 `UniversalDeviceToolkit_vX.Y.Z_SHA256.txt`
- 转发镜像时同步附带校验文件
- 对外说明中明确 GitHub Releases 和 winget 为权威下载源
- 对外说明中使用 Universal Device Toolkit 作为产品名，并注明 winget / Scoop 命令暂留旧标识以保证升级兼容
