# 中文发布文案

发布前把版本号、链接换成实际值。**语气原则**：像你在论坛回帖、给朋友安利，别像产品发布会通稿——可以讲痛点、讲取舍，少用「核心能力」「一等公民」「赋能」这类词。

---

## 随口一说（复制就能发）

**超短**

拯救者不想装 Vantage 的可以试 UDT，Fn+Q、RGB 都能管，开源不要账号。

**稍微多说两句**

我后来把 Vantage 卸了，改用一个叫 Universal Device Toolkit 的开源小工具（以前叫 Legion Toolkit）。性能模式、键盘灯、独显状态这些日常够用了，不会在后台常驻，也不用登录联想账号。GitHub 搜 UniversalDeviceToolkit 就行。

---

## Release 说明

### 短版（GitHub Release / 动态）

Universal Device Toolkit vX.Y.Z 发布了。

这版还是老样子：尽量轻、不跑后台服务、不碰遥测。插件扩展页可以继续在线装/更新/卸插件；以前装过 Lenovo Legion Toolkit 的直接升，设置会留着。

安装包还是两个：Full 自带语言和设备数据，Online 小一点、资源进应用里下。建议从 GitHub Releases 或 winget 下，顺手对一下 Release 里的 SHA256.txt。

### 详细版（论坛长帖）

Universal Device Toolkit vX.Y.Z 发布了，简单说下这版有啥：

- 产品名现在是 Universal Device Toolkit，老 Legion Toolkit 用户原地升级就行
- Full / Online 两种包还在，看你是想离线一次装完还是只要个小安装包
- 插件扩展页：CPU、GPU、网络、Shell、鼠标之类按需加，主程序不会越堆越大
- winget / Scoop 命令暂时还是旧包名 `LenovoLegionToolkit`，是为了升级不断档，不是两个软件

**怎么下**

- GitHub：https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
- winget：`winget install SSC-STUDIO.LenovoLegionToolkit`
- Scoop：`scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket && scoop install ssc-studio/lenovolegiontoolkit`

镜像转发的话把 SHA256 文件一起带上，别只丢个 exe。

---

## 各平台现成正文

### 微博 / 动态

拯救者党如果也被 Vantage 烦过：可以试试 UDT（Universal Device Toolkit），开源的，Fn+Q 和 RGB 都能用，不用注册账号。我 winget 一条命令就装好了。  
https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest

### B 站（动态 / 简介）

录/转一个我自己在用的拯救者工具——Universal Device Toolkit，前身是 Legion Toolkit，现在社区在维护。

Vantage 我卸了之后靠它管 Fn+Q 和键盘 RGB，不占后台，也不要联想账号。插件是按需装的，主程序不会越来越胖。

下载：https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest  
安装：`winget install SSC-STUDIO.LenovoLegionToolkit`

不是拯救者的话有个「基础模式」，硬件项会少一些，插件和系统工具还能用。

### 知乎 / 论坛（整段可贴）

**先说结论**：拯救者 / LOQ 用户，如果主要是想切性能模式、调 RGB、管电池，又烦 Vantage 登录和后台服务，可以试 **Universal Device Toolkit（UDT）**。

我自己的理由很土——Vantage 太重，开机一堆东西，还要账号。UDT 是 GPL-3.0 开源，前身是很多人用过的 Lenovo Legion Toolkit，现在改名叫 UDT，SSC-STUDIO 这边在持续维护。

在支持的机型上，Fn+Q 性能模式、Spectrum 键盘灯、Hybrid 独显、电池养护、自定义模式这些日常需求都能覆盖。它**故意不跑独立后台服务**，所以 Fn+Q 同步、宏之类需要让它待在托盘里，别完全退出——这是设计取舍，用之前心里有个数。

后来加了个插件扩展页，网络、Shell、鼠标之类按需装，不必全塞进主程序。老 UDT 用户直接升级，设置和插件会保留。

**下载**：GitHub Releases 或 `winget install SSC-STUDIO.LenovoLegionToolkit`。

**也要说实话**：不是所有电脑都能完整控硬件。对不上的机型会进「基础模式」，通用工具和插件还在，别指望它在戴尔/华硕上当完整版 Vantage 用。RGB 如果和 Riot Vanguard 冲突，README FAQ 里有说明。

### 小红书（口语，少 emoji）

拯救者卸载 Vantage 之后 Fn+Q 和 RGB 我是靠 UDT 解决的，全名 Universal Device Toolkit，开源不要钱。

PowerShell 里：`winget install SSC-STUDIO.LenovoLegionToolkit`

不是拯救者可以试基础模式，插件还挺好玩的。有问题 GitHub 提 Issue 就行。

### V2EX（分享创造）

**标题建议**：拯救者卸了 Vantage 之后，我靠这个开源小工具管 Fn+Q 和 RGB

**正文**：

我 R9000 用了几年，Vantage 实在用不下去——后台服务、登录、偶尔弹窗，卸载又怕 Fn+Q 废了。

后来换 Universal Device Toolkit（UDT，以前叫 Legion Toolkit，现在 SSC-STUDIO 在维护）。日常要用的：性能模式、键盘灯、独显状态、电池养护，都能搞定。不会在后台常驻，也不要联想账号。

插件是按需装的，CPU/GPU/网络那些不想用可以不装。以前装过 UDT 的直接升，设置还在。

- 项目：https://github.com/SSC-STUDIO/UniversalDeviceToolkit
- 偷懒安装：`winget install SSC-STUDIO.LenovoLegionToolkit`

不是拯救者的话有个「基础模式」，硬件控制会少一些，插件和系统工具还能用。

有用过的可以说下机型，方便后来的人对号入座。

### Linux.do

**标题**：【软件】拯救者用户分享：一个轻量的 Vantage 替代（开源）

**正文**：

拯救者 / LOQ 用户，如果烦 Vantage 登录和后台，可以看看 Universal Device Toolkit。前身 Legion Toolkit，GPL-3.0 开源，Fn+Q、RGB、独显、电池这些在支持的机型上都能用。

不跑独立后台服务，所以要靠 Fn+Q 同步的话让它挂托盘，别完全关。插件按需装，主程序保持精简。

https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest  
`winget install SSC-STUDIO.LenovoLegionToolkit`

### Chiphell / NGA（硬件区）

**标题**：【软件】拯救者开源工具 UDT，我用来替代 Vantage 的

**正文**：

硬件区的朋友可能熟悉 Legion Toolkit，现在改名叫 Universal Device Toolkit，社区还在更新。

我主要用它管 Fn+Q 性能模式、Custom Mode 风扇曲线、Hybrid / iGPU-only、Spectrum RGB（用 RGB 的话记得把 Vantage 禁掉，不然可能冲突）。国行 R7000/Y7000 系列 README 里有写支持情况，装之前可以先对一下机型列表。

不占后台服务，托盘挂着就行。GitHub 开源，winget 能装。

---

## 别人问「和 Vantage 比呢」

别背表格，口语说就行：

> Vantage 功能多但重，要账号还有后台。UDT 就图轻便、开源、能自己看代码。拯救者该用的硬件控制基本都有，插件还能自己加。代价是不跑后台服务，有些功能得让它在托盘里；不是所有品牌笔记本都能完整控硬件。

需要对照时再贴表：

| | UDT | Vantage |
|---|:---:|:---:|
| 后台 | 无独立服务，托盘挂着 | 常驻 |
| 账号/遥测 | 不要 | 要 |
| 开源 | 是 | 否 |
| 非拯救者 | 基础模式，插件还能用 | 基本不行 |

---

## 标签（别堆太多）

`#联想拯救者` `#LOQ` `#开源` `#Windows` `#Vantage` `#LenovoLegionToolkit`

---

## 发之前看一眼

- [ ] 链接是 latest release
- [ ] 配图用 `Assets/Screenshot_zh-hans.png`
- [ ] 镜像转发带上 SHA256
