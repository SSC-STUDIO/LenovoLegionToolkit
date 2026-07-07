# UDT and OpenAI programs -- honest take (2026-07-07)

This doc keeps us honest about the "OpenAI Pro 20x" part of the goal and gives a
ready application to reuse against any real credits/maintainer program.

## TL;DR

ChatGPT Pro is a real OpenAI tier (verified 2026-07-07 on chatgpt.com/pricing,
screenshot at `Docs/_verify_chatgpt_pricing.png`: **Pro starts at US$100/mo**,
marketed as "best for research and coding" and explicitly lists
"5x or 20x usage quota" -- so "Pro 20x" is a real, on-page phrase on the Pro
tier, not a separate grant program and not folklore). No GitHub-star threshold
unlocks any OpenAI tier: "reach N stars, get free OpenAI Pro" is folklore.
Free / no-cost access to Pro-tier benefits or to Azure OpenAI comes only
through the real application programs in the next section (e.g. Microsoft for
Startups Founders Hub -> Azure OpenAI credits; OpenAI for Startups Converge
when a cohort is open). State UDT's case there factually; do not repeat an
unverified "1k stars = Pro" claim externally -- that would hurt UDT's
credibility. To *use* Pro features without paying, you still have to apply
through a real program; nothing in the verified Pro pricing auto-grants seats
for repo traction.

## What OpenAI's tiers actually are (verified 2026-07-07)

Verified 2026-07-07 on chatgpt.com/pricing (CN locale; en-US prices shown in
parallel). Tiers, in pricing-page order:

- Free -- US$0/mo. Limited GPT-5.5 Instant messages/uploads, throttled image
  gen, limited Deep Research, limited memory/context, limited Codex.
- Go -- US$8/mo. "Best for long conversations"; more Instant usage, higher
  message/file caps, longer memory. May include ads.
- Plus -- US$20/mo. "Best for advanced work"; adds GPT-5.5 Thinking reasoning,
  deeper image gen, larger Deep Research / agent quotas, more Codex.
- **Pro -- starts at US$100/mo. "Best for research and coding".** Headline
  bullet on the tier card is "5x or 20x usage quota" (vs Plus), plus Pro-only
  GPT-5.5 Pro reasoning, max Codex tasks, unlimited GPT-5.3 and file uploads,
  unlimited/fastest image gen, max Deep Research / agent / memory / context,
  and a research-preview track for new features. Subject to anti-abuse limits.
- Business -- US$20 per user/month on annual (2+ users), US$25 monthly.
- Enterprise -- contact sales.

API access is pay-as-you-go, separate from a ChatGPT subscription.

So "OpenAI Pro 20x" is, on the page, the Pro tier's "5x or 20x usage quota"
headline. It is **not** a grant program, it is **not** unlocked by GitHub stars,
and it costs at least US$100/mo out of pocket or comes bundled with an
enterprise/startup-program award.

## Realistic channels for OpenAI access or credits

Programs and figures change often. Verify each link and the current eligibility
on the official site before you apply.

1. **Microsoft for Startups Founders Hub** -- the most practical, reliably
   available route for an independent builder to get Azure credits, which can
   include Azure OpenAI Service access. Apply, build on the API, keep credits
   during development. This is the channel the submission-ready draft below
   targets.
2. **OpenAI for Startups (Converge / OpenAI startup program)** -- the most direct
   OpenAI route, when a cohort is open. OpenAI has periodically run an early-stage
   startup program offering early model access, credits, and mentorship
   (historically branded "Converge"). [VERIFY the current open cohort and exact
   name at openai.com/startups before applying; these open and close.] Worth a
   parallel application -- more competitive than Founders Hub, but OpenAI-direct.
3. **OpenAI for Nonprofits** -- discounted/free ChatGPT for registered nonprofits;
   only relevant if the project sits under a nonprofit entity.
4. **OpenAI research / dissertation / grant programs** -- OpenAI has run periodic
   programs (e.g. a one-time nonprofit grant). Check openai.com for what is
   currently open.
5. **Azure OpenAI via existing cloud credits** -- if you hold Azure sponsorship or
   Visual Studio/VS Subscription credits, Azure OpenAI may be available within them.
6. **Education access** -- institutional pathways if affiliated with a school.

None of these is "free Pro for 1k GitHub stars." Each needs an application and a
real use case.

## Ready-to-use application narrative (fill the [brackets])

```
I'm the maintainer of Universal Device Toolkit (UDT) -- a GPL-3.0 open-source
Windows application (C# / WPF, .NET 10) that lets Lenovo laptop owners control
their hardware (Fn+Q performance modes, RGB, fan curves, dGPU, battery threshold)
without the vendor's telemetry-bearing companion app. It is built around a
first-class plugin system so device-specific features live in plugins instead of
bloating the core, and non-Lenovo PCs run a "basic mode" with plugins + themes.

Traction so far: 18 stars, ~200 release-asset downloads across v4.2.1 (144
installer + 57 zip; Scoop pulls these same GitHub assets; winget id reserved but not yet on winget-pkgs), ~2,343 automated
tests, a v5.0.0 plugin overhaul shipped 2026-07-06, active contributor base.

What I'd use OpenAI/Azure OpenAI access for:
- [ ] A plugin-authoring assistant that generates plugin scaffolds from a natural
      language spec, lowering the barrier for new device contributors.
- [ ] Localization help: drafting and reviewing community translations across the
      25+ supported languages.
- [ ] Docs/chat: a factual, retrieval-grounded assistant over the repo for new
      contributors and users.

License/outlook: GPL-3.0, no telemetry, no account, no commercial upsell. I'm
asking for [credits / a Pro seat / API access] with a [N]-month runway to ship the
first two of the above and measure impact.
```

Pick the bracketed items you'll actually build -- reviewers reject vague asks.

## Recommendation

- Don't pay anyone who claims to sell "Pro 20x." It is not a real SKU and resale of
  a subscription account can get the account banned.
- Apply only through official channels; report DMs offering "guaranteed" OpenAI
  access -- they're usually scams.
- If you (the user) meant a specific real program under another name, tell us its
  exact name and the official page, and this doc + the application will be tailored.
- Keep the GitHub-star effort and the OpenAI-access effort separate. Stars help
  the project's organic reach; they are not a credential OpenAI asks for.

## Submission-ready draft: Microsoft for Startups Founders Hub (route to Azure OpenAI)

This is the highest-probability OpenAI-access channel for an independent
maintainer (see "Realistic channels" #1 above). A parallel application to the
OpenAI-direct startup program (#2) is worth a separate email once you confirm a
cohort is open. Fill every [VERIFY] item below before sending; the rest is ready
to paste. Confirm current eligibility and figures at
foundershub.startups.microsoft.com before submitting.

### 1. What you're building (paste into "Tell us about your product")

Universal Device Toolkit (UDT) is an open-source, GPL-3.0 Windows application
(C# / WPF, .NET) that lets Lenovo Legion / LOQ / IdeaPad Gaming owners control
their hardware (Fn+Q performance modes, per-key RGB, custom fan curves,
discrete-GPU toggle + MUX switch, battery conservation thresholds) without the
vendor's telemetry-bearing companion app. Hardware control is a plugin system:
device-specific features ship as plugins so the core stays small, and non-Lenovo
PCs run a "basic mode" with the same plugins, themes, and system tools. It ships
from GitHub Releases and Scoop (winget id SSC-STUDIO.LenovoLegionToolkit reserved, not yet on winget-pkgs) with a
CI-gated release pipeline and a GitHub Pages landing page.

### 2. Traction (measured 2026-07-07 -- verify before sending)

- GitHub stars 18, forks 1: https://github.com/SSC-STUDIO/UniversalDeviceToolkit
- Releases: v5.0.0-preview (plugin overhaul) shipped 2026-07-06; stable v4.2.1
- Distribution: Scoop bucket ssc-studio (winget id SSC-STUDIO.LenovoLegionToolkit reserved, not yet on winget-pkgs); v4.2.1 release assets
  ~200 downloads (144 installer + 57 zip; Scoop pulls these same GitHub assets)
- Localization: 25+ community-translated languages via Crowdin (78+ locales scaffolded)
- CI: GitHub Actions Ci-tests.yml, automated test suite across the solution
  (2,343 test cases -- 2,323 passing / 20 skipped per v5.0.0-preview release notes)
- Landing: https://ssc-studio.github.io/UniversalDeviceToolkit/ (live, returned 200 OK)

### 3. What the credits would fund (pick the two you will actually ship)

- Plugin authoring assistant: given a natural-language device spec, generate a
  plugin scaffold (manifest, sensors, actions, tests) so a new device contributor
  can add support in hours instead of days. Built on Azure OpenAI with retrieval
  over the repo's plugin docs and existing plugins.
- Localization assist: draft and review community translations across the 25+
  community-translated languages (78+ locales scaffolded) with a glossary and
  human-in-the-loop review, keeping community translations shippable at low cost.

### 4. Runway + measurement

I'm asking for Azure credits (which include Azure OpenAI Service) with a
[VERIFY 3]-month runway to ship item 1 and agree a usage measurement report up
front (stars, plugin PRs merged, languages updated). No commercial upsell, no
account, no telemetry -- the credit goes entirely into the assistant features
above. Answers to likely questions, up front:

- Entity: [VERIFY: personal / nonprofit / company]
- Stage: pre-revenue open-source maintainer, GPL-3.0
- Built on Microsoft stack: C# / .NET / GitHub Actions / GitHub Pages
- AI usage policy: assistant output is reviewed before merge; no user data leaves
  the maintainer's environment

### 5. If "Pro 20x" was a specific program, not this one

If you actually meant a concrete, named program under another name, give us its
exact name and the official page and this draft gets retargeted. Until then,
Microsoft for Startups Founders Hub -> Azure OpenAI is the highest-probability
real route to OpenAI access.

Last updated: 2026-07-07

