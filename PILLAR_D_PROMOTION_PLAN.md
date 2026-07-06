# Pillar D: 开源推广计划 (Target: 1,000 stars)

## 现状 (2026-07-06)

- 当前 stars: 18
- 目标: 1,000 stars（还差 982）
- GitHub Release: v5.0.0-preview.20260706001 已发布；稳定版 v4.2.1

## 诚实说明（先读这条）

- Star 来自真实用户，不能买、不能刷、不能 star-for-star。GitHub 反作弊会把项目降权，信誉也回不来。
- 推广是长期工程，不是一次性发帖。最有效的不是首发，是发帖后 48h 内认真回复 + 持续小版本。
- 配套文档（都已就绪）：
  - 现成可发文案（每渠道一版）：`Docs/OUTREACH_PACK.md`
  - 目录 / awesome-list 提交跟踪：`Docs/SUBMISSIONS.md`
  - OpenAI 程序诚实分析 + 申请文案：`Docs/OPENAI_PROGRAMS.md`

## 阶段里程碑

| 阶段 | 目标 | 关键动作 |
|------|------|----------|
| Phase 0 | 0 -> 100 | HN（Show HN）+ 1 个 subreddit + V2EX + X 7 连推 + B站；提交 awesome-dotnet / AlternativeTo |
| Phase 1 | 100 -> 300 | Dev.to + 掘金 + 少数派 + 第 2 个 subreddit；每 2 周一个小版本保持 trending |
| Phase 2 | 300 -> 700 | Linux.do / Chiphell / NGA + 微博 / 小红书；联系 2-3 个科技博主 / UP 主 |
| Phase 3 | 700 -> 1000 | 持续 2 周一版；帮真实用户解决机型适配；把 issue 回复做到最快 |

## 每周增长跟踪

| 周截止 | Stars | 周增量 | 触达渠道 | 备注 |
|--------|-------|--------|----------|------|
| 2026-07-06 | 18 | - | v5.0.0-preview 发布 | baseline |

每周日在 `Docs/SUBMISSIONS.md` 的周表里同步增一行；任何时候查 star 计数：

```
gh api repos/SSC-STUDIO/UniversalDeviceToolkit --jq .stargazers_count
```

## 推广动作清单（实战）

### Phase 0（本周）

- [x] 创建 GitHub Release v5.0.0-preview
- [x] README 优化（hero 一键安装 + Goal: 1,000 stars + Star History CTA）
- [x] 仓库可发现性（topics / description / Discussions 开启）
- [x] 准备 OUTREACH_PACK.md / SUBMISSIONS.md / OPENAI_PROGRAMS.md
- [ ] 在 Hacker News 发布 Show HN（见 OUTREACH_PACK.md A）
- [ ] 在 1 个 subreddit 发布（见 OUTREACH_PACK.md B，按优先级选）
- [ ] 在 V2EX 分享创造 发布（见 OUTREACH_PACK.md C）
- [ ] X 7 连推（见 OUTREACH_PACK.md G）
- [ ] B站 90s 录屏（见 OUTREACH_PACK.md H）
- [ ] 提交 awesome-dotnet PR（见 SUBMISSIONS.md）
- [ ] AlternativeTo 建条目（见 SUBMISSIONS.md）

### Phase 1（接下来 2-3 周）

- [ ] Dev.to 长文 + 掘金长文（技术拆解，见 OUTREACH_PACK.md D/F）
- [ ] 少数派投稿（见 OUTREACH_PACK.md E）
- [ ] 第 2 个 subreddit（与第一个不同）
- [ ] 每两周一个小版本，发版即转一条短推 + 仓库 Discussion 公告
- [ ] 跟踪 star 增长，周日更新本表

### Phase 2 / 3

- [ ] Linux.do / Chiphell / NGA
- [ ] 微博 / 小红书
- [ ] 联系科技博主 / UP 主（提供现成素材：截图 + 脚本 + 一句话简介）
- [ ] 真实机型适配 issue 响应（把"作者秒回 + 修了"做成营销亮点）

## 增长黑客（合规前提下）

1. 朋友网络: 让真用得上的朋友/同事 star（一次，真诚地，不是群发）
2. 技术会议: .NET Conf / 本地开源聚会分享插件沙箱 + 钩子泄漏排查故事
3. 大学合作: 计算机社团、开源社团体验插件开发
4. 企业友好: 无遥测 -> 推到 IT 管理员 / 内网系统社区
5. 内容 SEO: README 关键词 "lenovo legion toolkit" "open source Vantage alternative" 已就位
6. 译者优先权: 招募 25+ 语言译者，译者即推广者（自己的语言圈最容易带量）

## 真实节奏 + 反指标

- 不要每天发同一篇到多个社区 -- 平台限流 + 显得 spam。
- 不要在每个回复下求 star -- 求 1 次，温暖地，说清"为什么对开源项目有意义"。
- 不要为追 1000 去 star farms 或买量 -- 19 个真 star 的项目，远胜 1000 个水军。
- 真实 1000 star 的项目共同点：发版稳定 + 作者响应快 + 真解决一个痛点 + 有人自发转发。盯住这四点。

## 下一步行动（今天）

1. 按 OUTREACH_PACK.md A 发 Hacker News，发完盯 2 小时回复
2. 按 OUTREACH_PACK.md C 发 V2EX
3. 按 OUTREACH_PACK.md G 发 X 7 连推
4. 提交 awesome-dotnet PR（见 SUBMISSIONS.md）
5. 周日回来更新本表 + SUBMISSIONS.md 周表

最后更新: 2026-07-06
