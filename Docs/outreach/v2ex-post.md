# V2EX 发帖内容（已更新 v4.2.1）

## 节点：`/go/create/share`

## 标题
拯救者卸了 Vantage 之后，我靠这个开源小工具管 Fn+Q 和 RGB

## 正文

我 R9000 用了几年，Vantage 实在用不下去——后台服务、登录、偶尔弹窗，卸载又怕 Fn+Q 废了。

后来换 **Universal Device Toolkit**（UDT，以前叫 Legion Toolkit，现在 SSC-STUDIO 在维护）。日常要用的：性能模式、键盘灯、独显状态、电池养护，都能搞定。不会在后台常驻，也不要联想账号。

v4.2.1 的变化：
- 测试覆盖率 2343/2343 全通过（CI 自动化）
- 多语言支持修复，不会再显示未翻译的资源键
- 驱动包更新检测在 WMI 不可用时会优雅降级（之前会卡住）
- 插件扩展页安装/配置/打开/卸载图标在深浅色主题下都能正确显示

插件是按需装的，CPU/GPU/网络那些不想用可以不装。以前装过 LLT 的直接升，设置还在。

项目：https://github.com/SSC-STUDIO/UniversalDeviceToolkit
偷懒安装：`winget install SSC-STUDIO.LenovoLegionToolkit`

不是拯救者的话有个「基础模式」，硬件控制会少一些，插件和系统工具还能用。

有用过的可以说下机型，方便后来的人对号入座。
