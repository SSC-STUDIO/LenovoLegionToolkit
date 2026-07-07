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

- **Where:** https://www.v2ex.com/ -- section 鍒嗕韩鍒涢€?(Share and Create)
- **When:** weekday evening CST (20:00-22:00) for the tech crowd.

**Title:** `[寮€婧怾 鍗镐簡 Vantage 涔嬪悗锛屾垜鍦ㄧ敤鑷繁缁存姢鐨勮繖涓伐鍏?-- Universal Device Toolkit`

**Body:**

```
闂茶亰涓€涓嬭嚜宸卞仛鐨勪笢瑗裤€傛垜鏄淮鎶よ€咃紙鍒╃泭鐩稿叧锛岃娓呮锛夈€?
Vantage 鐨勫悗鍙版湇鍔°€佺櫥褰曡处鍙枫€佸脊绐楀疄鍦ㄧ儲锛屾墍浠ユ垜涓€鐩村湪缁存姢 Universal Device Toolkit锛圲DT锛夛紝鍓嶈韩鏄?Lenovo Legion Toolkit銆倃inget 鍖呭悕 SSC-STUDIO.LenovoLegionToolkit 鏄繚鐣欑殑锛屼絾杩樻病涓?winget-pkgs锛屾殏鏃剁敤 Scoop 鎴栧畨瑁呭櫒锛涗互鍚庝笂浜嗗氨鑳藉師鍦板崌绾с€佽缃笉涓€?
鏃ュ父澶熺敤鐨勶細
- Fn+Q 鎬ц兘 / 鍧囪　 / 瀹夐潤
- 閿洏 RGB + 鐏晥
- Hybrid / 鐙樉鍒囨崲
- 椋庢墖鏇茬嚎
- 鐢垫睜鍏绘姢闃堝€?- 鍒绘剰寰呭湪鎵樼洏锛堥潬瀹冨悓姝?Fn+Q 鍜屽畯锛岃繖鏄璁′笉鏄?bug锛屼笉鐢ㄨ繖浜涘姛鑳界殑璇濆彲浠ラ€€鍑猴紱闄ゆ涔嬪涓嶈窇鍚庡彴鏈嶅姟锛?
v5.0.0-preview 浠婂ぉ鍙戜簡锛?- 鎻掍欢绯荤粺閲嶆瀯锛堢儹閲嶈浇銆佹矙绠便€佷緷璧栬В鏋愶級
- 淇簡鍏ㄥ眬閽╁瓙娉勬紡锛堥€€鍑哄悗绯荤粺涓嶅啀鍗￠】锛?- 2500+ 鍗曞厓娴嬭瘯閫氳繃

涓嶆槸鎷晳鑰呮湁銆屽熀纭€妯″紡銆嶏紝纭欢椤瑰皯涓€浜涳紝鎻掍欢鍜岀郴缁熷伐鍏疯繕鑳界敤銆?
瀹夎锛?  scoop bucket add ssc-studio https://github.com/SSC-STUDIO/scoop-bucket
  scoop install ssc-studio/lenovolegiontoolkit
鎴栦笅杞斤細https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/latest

椤圭洰鍦板潃锛歨ttps://github.com/SSC-STUDIO/UniversalDeviceToolkit

瑙夊緱鏈夌敤鐨勮瘽缁欎釜 Star锛屽寮€婧愰」鐩府鍔╁緢澶с€傛湁 bug 鎴栬€呮兂鏀寔浣犵殑鏈哄瀷锛屽紑 issue 鍛婅瘔鎴戝叿浣撳瀷鍙枫€?```

---

## D. 鎺橀噾 (Juejin) -- long-form

- **Where:** https://juejin.cn/ -- technical article, tags 寮€婧?Windows .NET
- **Angle:** a technical teardown ranks better than a soft ad. Lead with the plugin sandbox + the global hook leak post-mortem.

**Title:** `鎴戜笉鎯冲湪鎷晳鑰呬笂寮€ Vantage锛屾墍浠ユ垜鑷繁缁存姢浜嗕竴涓紑婧愬伐鍏凤紙闄勬彃浠舵矙绠变笌鍏ㄥ眬閽╁瓙娉勬紡淇锛塦

**Skeleton (expand to 2000+ words):**

```
寮€澶达細Vantage 鐑﹀湪鍝紙鍚庡彴鏈嶅姟 + 鐧诲綍 + 閬ユ祴 + 寮圭獥锛夛紝鍗镐簡涔嬪悗 Fn+Q/RGB/鐙樉娌′簡鎬庝箞鍔炪€?涓 1锛歎DT 鏄粈涔堬紝鍜?Lenovo Legion Toolkit 鐨勫叧绯伙紝涓轰粈涔堥噸鍛藉悕銆亀inget 鍖呭悕淇濈暀浣嗚繕娌′笂 winget-pkgs锛堟殏鏃剁敤 Scoop锛夈€?涓 2锛堟妧鏈噸鐐癸級锛氭彃浠剁郴缁熸€庝箞鍋氭矙绠变笌渚濊禆瑙ｆ瀽锛岀儹閲嶈浇閬囧埌鐨勫潙銆傝创 1-2 娈靛叧閿唬鐮併€?涓 3锛堟妧鏈噸鐐癸級锛氬叏灞€閿洏閽╁瓙涓轰粈涔堜細鍦ㄩ€€鍑哄悗璁╃郴缁熷崱椤匡紝鎬庝箞瀹氫綅锛圗TW/Performance
            Analyzer锛夛紝鏈€缁堟€庝箞淇殑銆傝繖鏄渶瀹规槗琚妧鏈鑰呰鍙殑閮ㄥ垎銆?缁撳熬锛氭€庝箞瑁咃紙Scoop / 涓嬭浇閾炬帴锛夛紝鎷涘嫙鎻掍欢浣滆€呭拰璇戣€咃紝Star 瀵瑰紑婧愮殑鎰忎箟銆?閰嶅浘锛欰ssets/Screenshot_main.png銆丼creenshot_zh-hans.png銆丼tar History 鍥俱€?```

**娉ㄦ剰锛?* 鎺橀噾鏈夈€屾哺鐐广€嶅彲鍚屾鐭甫閲忥紱闀挎枃棣栨棩涓嶈鍐嶅彂鍒?V2EX 鍚屼竴鍐呭鍒峰睆锛岄棿闅?24h銆?
---

## E. 灏戞暟娲?(sspai)

- **Where:** https://sspai.com/ -- submit an article
- **Angle:** tool-oriented / "鎷晳鑰呬笉璇ヨ Vantage 缁戞灦". sspai readers respond to "one fewer background process".
- 闀垮害 1500-2500 瀛楋紝閰嶆埅鍥撅紝缁撳熬缁?GitHub + Star銆?
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

## H. B绔?(Bilibili) -- 90s screencast

- **Where:** 鐭ヨ瘑鍖?/ 绉戞妧 -> 鏁扮爜
- **Length:** 60-90s. Switch Fn+Q, change RGB, install a plugin, quit and show no hitch.

**鍙ｆ挱鑴氭湰锛堜腑鏂囷級锛?*

```
(0-5s) 鎷晳鑰呬笂涓嶆兂鍐嶈 Vantage锛熻繖涓槸鎴戠淮鎶ょ殑寮€婧愬伐鍏?UDT銆?(5-20s) 瀹冩病鏈夊悗鍙版湇鍔°€佷笉鐢ㄧ櫥褰曘€佷篃娌℃湁閬ユ祴銆傛棩甯哥殑 Fn+Q銆侀敭鐩?RGB銆侀鎵囨洸绾裤€?        鐙樉鍒囨崲銆佺數姹犲吇鎶ら兘鍦ㄣ€?(20-45s) 閲嶇偣鐪嬫彃浠讹細鍍忚鎵嬫満鎻掍欢涓€鏍疯 CPU/GPU/缃戠粶宸ュ叿锛屼富绋嬪簭涓嶄細瓒婃潵瓒婅噧鑲匡紝
        鑰屼笖鑳界儹閲嶈浇銆佹矙绠遍殧绂汇€?(45-70s) v5.0.0 鍒氫慨浜嗕釜鐑︿汉鐨勯棶棰橈細浠ュ墠閫€鍑哄簲鐢ㄥ悗绯荤粺浼氬崱涓€涓嬶紝鐜板湪涓嶄細浜嗐€?(70-90s) Scoop 涓€鏉″懡浠よ锛坵inget 鍖呭悕鐣欑潃浣嗚繕娌′笂绾匡級锛岄摼鎺ュ湪绠€浠嬨€傝寰楁湁鐢ㄧ粰涓?Star锛屽寮€婧愰」鐩剰涔夐噸澶с€?```

**绠€浠嬪尯鏀撅細** GitHub 閾炬帴銆丼coop 鍛戒护锛岀疆椤惰嚜宸辩殑璇勮鎸傞摼鎺ャ€傚皝闈㈢敤
`Assets/Screenshot_main.png` 閰?Logo 鍚堟垚銆?
---

## I. Linux.do / Chiphell / NGA

- **Linux.do:** 绉戞妧 / 寮€婧?鍖猴紝鍙戜富棰樺笘锛屽悓 V2EX 姝ｆ枃锛岃姘旀洿鎶€鏈€?- **Chiphell / NGA 绗旇鏈尯:** 鍋忕帺瀹跺悜锛屽己璋?RGB / 椋庢墖鏇茬嚎 / 鐙樉锛屼竴鍙ヨ娓?鏃犻仴娴?銆?- 鍧囬伒瀹堬細涓€娆℃€с€佽嚜鎶ュ闂ㄧ淮鎶よ€呫€佽鐪熷洖绛旈棶棰樸€?
---

## J. 寰崥 / 灏忕孩涔?
- **寰崥:** 9 鍥惧€掑簭閰嶆枃锛屾爣绛?#鎷晳鑰? #寮€婧? #Vantage骞虫浛#锛屾寕 repo 閾炬帴銆?- **灏忕孩涔?** 灏侀潰澶у瓧"鎷晳鑰呭埆鍐嶈 Vantage"锛屽唴椤垫埅鍥撅紝鏂囨湯 Scoop 鍛戒护 + Star銆?
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

- Week 1: HN + 1 subreddit + V2EX + X thread + B绔?
- Week 2: Dev.to + 鎺橀噾 + 1 different subreddit + 灏戞暟娲?
- Week 3: Linux.do + Chiphell/NGA + 寰崥 + 寰俊缇?鎶€鏈兢.
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
| Sspai | `Docs/ARTICLES/sspai_article.md` | Draft ready (~800 words CN) |

All articles reference `Assets/Screenshot_main.png` as cover/attachment.

---

Last updated: 2026-07-07  Stars today: 18 (verified via GitHub API)  Goal: 1,000