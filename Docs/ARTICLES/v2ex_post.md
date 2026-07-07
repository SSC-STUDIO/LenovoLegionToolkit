# V2EX Post Draft

**Node:** create/share (or software)

**Title:**
> [开源] Universal Device Toolkit — 告别 Vantage，Legion 笔记本硬件控制的开源替代方案

**Body:**

大家好，我是 Universal Device Toolkit (UDT) 的维护者。

UDT 是一个开源的 Windows 桌面工具，用于替代 Lenovo Vantage 对 Legion/LOQ/IdeaPad Gaming 笔记本的硬件控制。

**为什么做这个？**
- Vantage 需要后台服务、联想账号、还有遥测
- UDT 零后台服务、零遥测、零账号
- 同样支持 Fn+Q、风扇曲线、键盘 RGB、独显切换、电池养护

**技术亮点：**
- C# / WPF / .NET 10，MVVM 架构
- 2,500+ 单元测试，FlaUI + WinRT OCR UI 验证
- 78+ 语言本地化（Crowdin 管理）
- 插件系统：热重载、沙箱隔离、依赖管理
- WMI 异步超时保护（防止 RDP 死锁）
- 内存占用 ~50-100MB，启动 <2秒

**基本模式：** 非联想 PC 也能用插件、主题和系统优化功能。

**安装：**
- Scoop: `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket; scoop install ssc-studio/lenovolegiontoolkit`
- GitHub Releases: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest

GitHub: https://github.com/SSC-STUDIO/UniversalDeviceToolkit

GPL-3.0 开源，欢迎 Star 和贡献！
