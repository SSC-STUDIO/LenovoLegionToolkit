# I18nTranslate — UDT resx 批量翻译管线

用本地 llama-server（OpenAI 兼容 API）批量翻译 crowdin.yml 中声明的 .resx 源文件。

提示词与术语表由 `prompts.json` + `glossary.json` 驱动（9 个语族风格、双引擎模板、占位符与质量门禁）。草稿在 `_agent_out/`，用 `build-prompt-pack.py` 合并。

## 前置条件

1. 启动推理引擎（见 `..\..\LocalAI-API\`）：
   ```powershell
   ..\..\LocalAI-API\start-ai.ps1 -Model translategemma   # 55 语种主力
   ..\..\LocalAI-API\start-ai.ps1 -Model gemma4-e4b       # 140+ 语种兜底
   ```
   两个模型端口不同（11434/11435），可同时运行。

2. 翻译引擎健康检查：
   ```powershell
   ..\..\LocalAI-API\bench.ps1
   ```

## 用法

```powershell
.\i18n-translate.ps1                      # 翻译全部语言（增量：只补缺失 key）
.\i18n-translate.ps1 -Locales hi,sw       # 只翻译指定语言（试点）
.\i18n-translate.ps1 -DryRun              # 预览将翻译多少条
.\i18n-translate.ps1 -ParallelJobs 4      # 4 路并发（默认）
```

## 语言路由

`locales.txt` 每行 `<locale> <engine>`：
- `tg` → translategemma（55 种主流语言，质量/速度最优，端口 11434）
- `g4` → gemma4-e4b（其余语言兜底，端口 11435）

## 翻译模板

| 文件 | 作用 |
|---|---|
| `glossary.json` | 品牌/硬件缩写 keep-as-is。按当前 batch 命中的词条注入，避免把无关术语塞进提示词 |
| `prompts.json` | 引擎模板、语族 `stylePrompt`/`localeNotes`、占位符正则、质量规则、Electron 预留模板 |
| `build-prompt-pack.py` | 把 `_agent_out/*.json` 合并成上面两个文件 |

`prompts.json` 语族：`cjk`、`rtl`、`romance`、`germanic`、`slavic`、`indic`、`sea`、`turkic-uralic-baltic`、`other`。每个 locale 走对应 `localeNotes`（TranslateGemma 截断到约 400 字符；Gemma4 约 1600 字符）。zh-Hans 额外注入简中 few-shot；其它语言不注入中文示例，避免串味。

TranslateGemma 用户消息必须以 `<translate en to {LanguageName}>` 开头。Gemma4 使用完整 system 提示（短标签不扩写、长错误不删减、禁止零宽/bidi/NBSP）。

## 行为与保障

- **增量模式**：已存在翻译文件只补缺失 key，不覆盖已有译文
- **占位符保护**：`{0}`、`{0:0.##}`、`{{name}}`、`%s`、`\n`、HTML 标签、`&amp;` 等按扩展正则做多重集合比对；不匹配则按 retry 模板重译一次，仍失败则保留原文并记入失败列表
- **质量清洗**：去掉 markdown 围栏、包裹引号、末尾英文括注、零宽/NBSP、全角拉丁字母
- **术语**：源串里出现的 keep 词条（UDT、ViVeTool、dGPU 等）必须原样出现在译文中，否则重试
- **格式**：生成标准 resx（含 resheader），UTF-8 无 BOM
- **失败处理**：单条失败自动降级为逐条重译；最终保留原英文字符串保证文件可编译
- 翻译完成后运行字符检测（AGENTS.md 要求）：
  ```powershell
  node Tools/CheckSourceUnicode/check-unicode.mjs Tools/I18nTranslate
  ```

## 重新生成模板

修改 `_agent_out/` 草稿后：

```powershell
python Tools\I18nTranslate\build-prompt-pack.py
```

Electron 嵌套 locale（`en-US.ts` / i18next `{{version}}`）的提示词在 `prompts.json` 的 `electron` 段，当前管线仍只处理 crowdin.yml 里的 resx。

## 验证

试点建议先跑 `-Locales hi,sw`，人工抽查 `Resource.hi.resx` 质量后再全量。
