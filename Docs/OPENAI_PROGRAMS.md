# UDT and OpenAI programs -- honest take (2026-07-06)

This doc keeps us honest about the "OpenAI Pro 20x" part of the goal and gives a
ready application to reuse against any real credits/maintainer program.

## TL;DR

There is no OpenAI program named "Pro 20x", and no GitHub-star threshold unlocks
any OpenAI tier. "Reach N stars, get free OpenAI Pro" is internet folklore; we
could not verify a real program matching that description. Be honest about this
externally -- overstating a rumor would hurt UDT's credibility.

## What OpenAI's tiers actually are (verify on openai.com/pricing before quoting)

- ChatGPT tiers: Free / Plus / Pro / Team / Business / Enterprise.
- "Pro" is the roughly $200/mo plan. **No tier named "20x" exists in OpenAI's
  public pricing.**
- API access is pay-as-you-go, separate from a ChatGPT subscription.

So the phrase "OpenAI Pro 20x" most likely means one of:

- a misremembering of ChatGPT Pro ($200 is roughly a multiple of the $20 Plus plan),
- a rumored future tier -- treat as rumor until openai.com announces it,
- a hoped-for "20x rate-limit" product -- not a named SKU today.

## Realistic channels for OpenAI access or credits

Programs and figures change often. Verify each link and the current eligibility
on the official site before you apply.

1. **Microsoft for Startups Founders Hub** -- the most practical route for an
   independent builder to get Azure credits, which can include Azure OpenAI
   Service access. Apply, build on the API, keep credits during development.
2. **OpenAI for Nonprofits** -- discounted/free ChatGPT for registered nonprofits;
   only relevant if the project sits under a nonprofit entity.
3. **OpenAI research / dissertation / grant programs** -- OpenAI has run periodic
   programs (e.g. a one-time nonprofit grant). Check openai.com for what is
   currently open.
4. **Azure OpenAI via existing cloud credits** -- if you hold Azure sponsorship or
   Visual Studio/VS Subscription credits, Azure OpenAI may be available within them.
5. **Education access** -- institutional pathways if affiliated with a school.

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

Traction so far: [N] stars, [N] winget installs, ~2500 unit tests, a v5.0.0 plugin
overhaul shipped [date], active contributor base.

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

Last updated: 2026-07-06
