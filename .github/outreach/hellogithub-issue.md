### 项目地址

https://github.com/SSC-STUDIO/UniversalDeviceToolkit

### 类别

C#

### 项目标题

拯救者用户卸 Vantage 后的开源小工具

### 项目描述

Universal Device Toolkit（前身是 Lenovo Legion Toolkit）是给联想拯救者/LOQ 用的 Windows 小工具：Fn+Q 性能模式、键盘 RGB、Hybrid 独显、电池养护这些日常需求能覆盖，但不会像 Vantage 那样常驻后台、要登录账号。GPL-3.0 开源，其他电脑也能用「基础模式」装插件。老用户原地升级，设置不丢。

### 亮点

- 我自己卸 Vantage 主要就是图这个：该用的硬件控制还在，内存占用低，不跑独立后台服务
- 插件按需装（CPU/GPU/网络/Shell/鼠标），主程序不会越改越大
- 有 `llt.exe` 命令行，脚本和自动化能玩
- 不是拯救者的机器也能用基础模式 + 插件
- winget / Scoop 一条命令安装，社区在持续维护新机型

### 示例代码

```powershell
winget install SSC-STUDIO.LenovoLegionToolkit
llt.exe status
```

### 截图或视频

![简体中文界面](https://raw.githubusercontent.com/SSC-STUDIO/UniversalDeviceToolkit/master/Assets/Screenshot_zh-hans.png)

**作者声明**：我是 SSC-STUDIO 维护团队的。项目从 Legion Toolkit 延续过来（HelloGitHub Vol.101 收录过上游），现在以 UDT 名义继续维护，按自荐流程提交。
