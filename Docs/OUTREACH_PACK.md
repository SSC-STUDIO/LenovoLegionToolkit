# UDT Outreach Pack -- 18 to 1,000 stars

Ready-to-post copy for every channel. Paste, lightly personalize, post.
This is a **human-only** pack: nothing here posts itself. Share from your own
account so the engagement is real. Real stars come from real humans -- never buy,
swap, or bot them (GitHub deranks repos that do, and it kills credibility).

**One source of truth for facts about the project:**

- Repo: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
- Download: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
- Install (Scoop, works now): `scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket; scoop install ssc-studio/lenovolegiontoolkit`
- Install (winget): id `SSC-STUDIO.LenovoLegionToolkit` is reserved but NOT yet published to winget-pkgs -- use Scoop or the installer until it ships (auto-upgrade from old LLT installs once it lands)
- License: GPL-3.0  Language: C# / WPF  Platform: Windows
- Latest: v5.0.0-preview.20260706001  Stable: v4.2.1
- Stars today: 18 (verified via GitHub API 2026-07-07)  Goal: 1,000

**Reusable one-liners (drop into any post first line):**

- Open-source Lenovo hardware-control + device-plugin toolkit for Windows. Fn+Q, RGB, fan curves, dGPU -- no Vantage, no telemetry, no account.
- The lightweight Windows utility for Legion laptops and beyond, with first-class plugin extensions.

---

## Order of operations (do this, in this order)

1. **One post at a time per community.** Stagger by 24h+ across channels so each
   gets full engagement and you can reply to comments as they land.
2. **Reply within the first 2 hours.** Early replies seed the discussion and the
   algorithms surface active posts. Have your coffee ready.
3. **Never multi-post the same subreddit.** Use the variants below; pick the one
   subreddit where it fits best and post once.
4. **Lead with the screenshot/GIF.** Visual posts outperform text-only by a wide
   margin. Use `Assets/Screenshot_main.png` (EN) and `Screenshot_zh-hans.png` (CN).
5. **Disclose you are the maintainer.** "I made this" is welcome; pretending to be
   a random user is not, and communities ban for it.
6. **No "star for star," no asking friends to mass-star.** Ask once, clearly, for
   people it actually helps. Quality of star > quantity.

---

## A. Hacker News -- Show HN

- **Where:** https://news.ycombinator.com/submit (use "submit" then "Show HN")
- **When:** Tue-Thu, 08:00-10:00 PT (pick a window when you can be present to reply for 2 hours).
- **Rule:** Show HN must be text + a repo link. No marketing tone. Be technical.

**Title:**

```
Show HN: Universal Device Toolkit -- open-source Lenovo hardware control + plugin toolkit (no Vantage)
```

**Body (HN self-post):**

```
Hi HN, I'm the maintainer of Universal Device Toolkit (UDT), a GPL-3.0 Windows
app for Lenovo Legion/LOQ laptops and, in a limited "basic mode", any other PC.

Lenovo's own Vantage app requires a background service, an account, and ships
telemetry. UDT does the same hardware control -- Fn+Q performance modes, keyboard
RGB, fan curves, dGPU/hybrid mode, battery conservation -- with none of that. It
stays in the tray (that is by design: it syncs Fn+Q and macros) but runs no
background service.

The thing I'd most like feedback on is the plugin model. Plugins are first-class:
install/update/configure/remove from inside the app, sandboxed, dependency-aware.
Today there are CPU/GPU/network/shell/mouse plugins; the hope is that device-
specific workflows live in plugins instead of bloating the base app. Non-Lenovo
machines run plugins + themes + optimization in basic mode while hardware toggles
stay hidden.

Technical notes:
- C# / WPF on .NET 10, ~2500 unit tests, FlaUI-based UI smoke tests.
- v5.0.0-preview shipped today with a plugin overhaul and a global-hook leak fix
  (system no longer stutters after the app exits).
- CLI (`llt.exe`) for scripting/automation.

Repo: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
Releases: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
Install (Scoop, works now): scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket; scoop install ssc-studio/lenovolegiontoolkit
(winget id SSC-STUDIO.LenovoLegionToolkit is reserved but not yet published to winget-pkgs -- use Scoop or the installer)

I'd value feedback on the plugin boundary and on whether "Universal" is honest
given that full hardware control is Lenovo-specific. Constructive roasting welcome.
```

**First self-comment (post immediately after publishing):**

```
A note on the name: it was "Lenovo Legion Toolkit" upstream; the winget
package ID SSC-STUDIO.LenovoLegionToolkit is reserved for upgrade continuity but
is not yet published to winget-pkgs, so install via Scoop or the Releases
installer for now. Renamed to UDT once basic-mode + plugins made it more than a
Lenovo-only tool. If that framing reads as misleading I'd genuinely like to hear it.
```

**HN etiquette reminders:** no "upvote please", no asking friends to upvote (they
shadow-ban coordinated voting), do not repost if it goes to the new page and dies
-- that is normal.

---

## B. Reddit -- pick ONE subreddit that fits best, post once

Pick in this priority order based on which community will most care: r/LenovoLegion
> r/Lenovo > r/SysAdmin (no-telemetry angle) > r/dotnet (C# angle) > r/opensource.
Only post to 2-3 total across a week, never the same one twice.

### B1. r/LenovoLegion / r/Lenovo

**Title:** `[Free/OSS] Dropped Vantage? Here's what I've been using -- Universal Device Toolkit (open source, no account/telemetry)`

**Body:**

```
Hey -- maintainer here (full disclosure, it's my project, GPL-3.0).

If you uninstalled Vantage because of the background service / account / popups,
UDT is the open-source replacement I keep maintaining. It's the continuation of
Lenovo Legion Toolkit -- we kept the winget package id (SSC-STUDIO.LenovoLegionToolkit)
for continuity, but it's not on winget-pkgs yet; install via Scoop or the Releases
installer (your existing Lenovo Legion Toolkit settings carry over).

What works day to day:
- Fn+Q performance/quiet/balanced modes
- Keyboard RGB + lighting presets
- Hybrid/dGPU toggles
- Fan curves
- Battery conservation threshold
- It deliberately sits in the tray (that's how it syncs Fn+Q + macros) -- quit only
  if you want those silent. No background service otherwise.

New in v5.0.0-preview (out today):
- Plugin system overhaul -- hot-reload, sandboxing, dependency resolution
- Fixed the global hook leak that made the system stutter after quitting
- ~2500 unit tests passing

Not a Legion? There's a "basic mode" -- fewer hardware toggles, but plugins
(CPU/GPU/network/shell/mouse), themes, optimization still work.

Install:
  scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
  scoop install ssc-studio/lenovolegiontoolkit
or grab the installer/portable zip from
https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest

If UDT helps you, a star on GitHub genuinely helps more owners find it:
https://github.com/SSC-STUDIO/UniversalDeviceToolkit

Happy to answer questions -- also tell me your exact model so I can map it.
```

### B2. r/SysAdmin (no-telemetry / enterprise-friendly angle)

**Title:** `Open-source Windows utility that controls Lenovo laptop hardware with no telemetry, no account, no background service`

**Body:**

```
Posting for visibility for anyone who bans Vantage on managed fleets and wants a
no-telemetry alternative for the hardware controls users actually expect (Fn+Q,
fan curves, RGB, dGPU, battery threshold). Universal Device Toolkit, GPL-3.0,
C#/WPF, runs no background service. There's a CLI (llt.exe) so you can fold it
into scripts. Scoop: scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket; scoop install ssc-studio/lenovolegiontoolkit (winget id reserved, not yet on winget-pkgs).

Repo: https://github.com/SSC-STUDIO/UniversalDeviceToolkit

(Full disclosure: I help maintain it. Not commercial, no telemetry, no upsell.)
```

### B3. r/dotnet (engineering angle)

**Title:** `Show and tell: a WPF/.NET 10 hardware-control app with a plugin sandbox + FlaUI UI smoke tests`

**Body:**

```
Maintainer here. Sharing the architecture side more than the marketing side.

UDT is a C#/WPF app on .NET 10 that controls Lenovo laptop hardware (Fn+Q, RGB,
fan curves, dGPU) without the vendor's telemetry-bearing companion app. The part
I'd like .NET folks to poke at is the plugin system: install/update/configure/
remove from inside the app, sandboxed, dependency-aware, hot-reload. There's a
FlaUI-based visual regression harness that runs ~2500 checks after each change.

Repo: https://github.com/SSC-STUDIO/UniversalDeviceToolkit

Happy to talk about the WPF/MVVM plugins, the WinRT/Intel ACPI paths, or the
global-hook leak we finally fixed in v5.0.0-preview (system no longer hitches
after exit).
```

**Reddit rules:** read each subreddit's rules tab first; r/SysAdmin and r/dotnet
may require account age/karma. Never post links-only as a brand-new
account -- earn a little karma commenting first.

---

## C. V2EX

- **Where:** https://www.v2ex.com/ -- section 分享创�?(Share and Create)
- **When:** weekday evening CST (20:00-22:00) for the tech crowd.

**Title:** `[开源] 卸了 Vantage 之后，我在用自己维护的这个工�?-- Universal Device Toolkit`

**Body:**

```
闲聊一下自己做的东西。我是维护者（利益相关，说清楚）�?
Vantage 的后台服务、登录账号、弹窗实在烦，所以我一直在维护 Universal Device Toolkit（UDT），前身�?Lenovo Legion Toolkit。winget 包名 SSC-STUDIO.LenovoLegionToolkit 是保留的，但还没�?winget-pkgs，暂时用 Scoop 或安装器；以后上了就能原地升级、设置不丢�?
日常够用的：
- Fn+Q 性能 / 均衡 / 安静
- 键盘 RGB + 灯效
- Hybrid / 独显切换
- 风扇曲线
- 电池养护阈�?- 刻意待在托盘（靠它同�?Fn+Q 和宏，这是设计不�?bug，不用这些功能的话可以退出；除此之外不跑后台服务�?
v5.0.0-preview 今天发了�?- 插件系统重构（热重载、沙箱、依赖解析）
- 修了全局钩子泄漏（退出后系统不再卡顿�?- 2500+ 单元测试通过

不是拯救者有「基础模式」，硬件项少一些，插件和系统工具还能用�?
安装�?  scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
  scoop install ssc-studio/lenovolegiontoolkit
或下载：https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest

项目地址：https://github.com/SSC-STUDIO/UniversalDeviceToolkit

觉得有用的话给个 Star，对开源项目帮助很大。有 bug 或者想支持你的机型，开 issue 告诉我具体型号�?```

---

## D. 掘金 (Juejin) -- long-form

- **Where:** https://juejin.cn/ -- technical article, tags 开�?Windows .NET
- **Angle:** a technical teardown ranks better than a soft ad. Lead with the plugin sandbox + the global hook leak post-mortem.

**Title:** `我不想在拯救者上开 Vantage，所以我自己维护了一个开源工具（附插件沙箱与全局钩子泄漏修复）`

**Skeleton (expand to 2000+ words):**

```
开头：Vantage 烦在哪（后台服务 + 登录 + 遥测 + 弹窗），卸了之后 Fn+Q/RGB/独显没了怎么办�?中段 1：UDT 是什么，�?Lenovo Legion Toolkit 的关系，为什么重命名、winget 包名保留但还没上 winget-pkgs（暂时用 Scoop）�?中段 2（技术重点）：插件系统怎么做沙箱与依赖解析，热重载遇到的坑。贴 1-2 段关键代码�?中段 3（技术重点）：全局键盘钩子为什么会在退出后让系统卡顿，怎么定位（ETW/Performance
            Analyzer），最终怎么修的。这是最容易被技术读者认可的部分�?结尾：怎么装（Scoop / 下载链接），招募插件作者和译者，Star 对开源的意义�?配图：Assets/Screenshot_main.png、Screenshot_zh-hans.png、Star History 图�?```

**注意�?* 掘金有「沸点」可同步短带量；长文首日不要再发�?V2EX 同一内容刷屏，间�?24h�?
---

## E. 少数�?(sspai)

- **Where:** https://sspai.com/ -- submit an article
- **Angle:** tool-oriented / "拯救者不该被 Vantage 绑架". sspai readers respond to "one fewer background process".
- 长度 1500-2500 字，配截图，结尾�?GitHub + Star�?
---

## F. Dev.to -- long-form (EN)

- **Where:** https://dev.to/ -- write an article, tags opensource, dotnet, windows, csharp

**Title:** `I built an open-source Lenovo Vantage replacement with a plugin sandbox. Here's how it works.`

**Body skeleton (expand to 1500+ words):**

```
Intro: why Vantage is a problem (background service, account, telemetry, popups).
  Why "another Legion toolkit" -- what changed upstream, why a rename.
Section 1: what UDT controls (Fn+Q, RGB, fan curves, dGPU, battery threshold) and
  what "no background service" actually means (tray-resident by design for sync).
Section 2: the plugin model -- sandbox, dependency resolution, hot reload, the
  repo/collection model. Code snippet of a minimal plugin.
Section 3: the global hook leak detective story (system hitched after exit, how we
  traced it, the fix). This post-mortem is what technical readers star for.
Outro: install (Scoop / releases; winget id is reserved but not yet on winget-pkgs), call for plugin + translation contributors,
  honest ask for a star.
```

---

## G. X / Twitter -- 7-post thread

Post in order, ~10 min apart so each reply chains. Pair post 1 with a GIF/screenshot.

```
1/7
Tired of Lenovo Vantage's background service, account, and telemetry just to use
Fn+Q and RGB?

I help maintain Universal Device Toolkit -- open source, GPL-3.0, no account, no
telemetry, no background service.

Repo: https://github.com/SSC-STUDIO/UniversalDeviceToolkit

2/7
What it does on supported Lenovo Legion/LOQ:
- Fn+Q performance modes
- Keyboard RGB + presets
- Fan curves
- Hybrid/dGPU toggles
- Battery conservation threshold
- Deliberately tray-resident (that's how it syncs Fn+Q + macros)

3/7
Plugin-first: install/update/remove plugins from inside the app -- sandboxed,
dependency-aware, hot-reload. CPU/GPU/network/shell/mouse today, more coming.
Main app stays lean; device-specific features live in plugins, not the core.

4/7
Not a Lenovo? "Basic mode": fewer hardware toggles, but plugins, themes, and
system optimization still work -- so it's useful on other Windows PCs too.

5/7
What shipped in v5.0.0-preview today:
- Plugin system overhaul (hot-reload + sandbox)
- Fixed the global hook leak -- system no longer stutters after app exit
- ~2500 unit tests passing, FlaUI UI smoke tests in CI

6/7
Install (Scoop, works now):
  scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
  scoop install ssc-studio/lenovolegiontoolkit
Releases: https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest
Stable: v4.2.1  Preview: v5.0.0-preview

7/7
If UDT makes your Legion (or any Windows PC) run leaner, a star genuinely helps
more people find it -- and tells us the plugin model is worth building out.

https://github.com/SSC-STUDIO/UniversalDeviceToolkit/stargazers
```

**X tip:** tag @dotnet / @windows sparingly, attach the screenshot to post 1,
quote-tweet your own thread the next day with the Star History image as a
milestone post.

---

## H. B�?(Bilibili) -- 90s screencast

- **Where:** 知识�?/ 科技 -> 数码
- **Length:** 60-90s. Switch Fn+Q, change RGB, install a plugin, quit and show no hitch.

**口播脚本（中文）�?*

```
(0-5s) 拯救者上不想再装 Vantage？这个是我维护的开源工�?UDT�?(5-20s) 它没有后台服务、不用登录、也没有遥测。日常的 Fn+Q、键�?RGB、风扇曲线�?        独显切换、电池养护都在�?(20-45s) 重点看插件：像装手机插件一样装 CPU/GPU/网络工具，主程序不会越来越臃肿，
        而且能热重载、沙箱隔离�?(45-70s) v5.0.0 刚修了个烦人的问题：以前退出应用后系统会卡一下，现在不会了�?(70-90s) Scoop 一条命令装（winget 包名留着但还没上线），链接在简介。觉得有用给�?Star，对开源项目意义重大�?```

**简介区放：** GitHub 链接、Scoop 命令，置顶自己的评论挂链接。封面用
`Assets/Screenshot_main.png` �?Logo 合成�?
---

## I. Linux.do / Chiphell / NGA

- **Linux.do:** 科技 / 开�?区，发主题帖，同 V2EX 正文，语气更技术�?- **Chiphell / NGA 笔记本区:** 偏玩家向，强�?RGB / 风扇曲线 / 独显，一句讲�?无遥�?�?- 均遵守：一次性、自报家门维护者、认真回答问题�?
---

## J. 微博 / 小红�?
- **微博:** 9 图倒序配文，标�?#拯救�? #开�? #Vantage平替#，挂 repo 链接�?- **小红�?** 封面大字"拯救者别再装 Vantage"，内页截图，文末 Scoop 命令 + Star�?
---

## Reply playbook (do this for the first 48h after each post)

- **Answer every comment** within ~2h for the first day; it doubles engagement.
- Common questions to pre-empt in a reply:
  - "Will it work on my non-Lenovo laptop?" -> Basic mode + plugins, full hardware
    control needs supported Lenovo models.
  - "Is it safe / any telemetry?" -> GPL-3.0 open source, no account, no telemetry,
    the code is auditable; stays in tray only to sync Fn+Q + macros.
  - "Vantage vs UDT battery/thermal difference?" -> Uses the same firmware APIs,
    behavior should match; report model-specific issues as Issues.
- **When someone finds a bug:** thank them publicly, ask them to open an Issue,
  and link the Issue in the discussion. That visible responsiveness earns stars
  more than the launch post.
- **Do not** ask for upvotes/stars under every reply -- ask once, warmly, where
  it fits.

---

## What NOT to do

- No bot/star-swap/buying. GitHub's spam detection deranks the repo and it never
  recovers organic ranking.
- No pretending to be an unrelated happy user. Disclose maintainer status.
- No mass-cross-posting identical text the same hour across all channels -- it
  reads as spam and platforms throttle it.
- No asking friends to star in a coordinated burst. A real developer who stars
  because it genuinely removes a pain is worth dozens of pity stars.

## Weekly cadence (keep momentum, don't burn out)

- Week 1: HN + 1 subreddit + V2EX + X thread + B�?
- Week 2: Dev.to + 掘金 + 1 different subreddit + 少数�?
- Week 3: Linux.do + Chiphell/NGA + 微博 + 微信�?技术群.
- Then: 1 release every 2 weeks (even small) to stay in the star-history
  "trending" feed, and cross-post the release note as a short thread.
- Each Sunday: log the star delta in `Docs/SUBMISSIONS.md` and
  `PILLAR_D_PROMOTION_PLAN.md`.

---


## Article drafts (ready to publish)

| Platform | File | Status |
|----------|------|--------|
| Dev.to | `Docs/ARTICLES/devto_article.md` | Draft ready (~1800 words EN) |
| Juejin | `Docs/ARTICLES/juejin_article.md` | Draft ready (~1200 words CN) |
| Reddit r/dotnet | `Docs/ARTICLES/reddit_rdotnet_post.md` | Draft ready |
| V2EX | `Docs/ARTICLES/v2ex_post.md` | Draft ready |

All articles reference `Assets/Screenshot_main.png` as cover/attachment.

---

Last updated: 2026-07-07  Stars today: 18 (verified via GitHub API)  Goal: 1,000
