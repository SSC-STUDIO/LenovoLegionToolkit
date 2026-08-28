#!/usr/bin/env node
/**
 * Source Unicode hygiene checker.
 *
 * Scans repository source files for invisible/confusable characters that are
 * common carriers of AI-generated text watermarks and silently corrupt diffs,
 * editors and string literals:
 *
 *   - zero-width / format characters (ZWSP, ZWNJ, ZWJ, BOM, WJ, LRM/RLM, …)
 *   - confusable whitespace (NBSP, narrow no-break space, figure space, …)
 *   - soft hyphen, variation selectors
 *   - full-width ASCII look-alikes (ＡＢＣ, １２３, ！？（）
 *   - Cyrillic/Greek look-alikes in ASCII contexts (А а е о р с vs A a e o p c)
 *
 * Usage: node Tools/CheckSourceUnicode/check-unicode.mjs [path]
 *   (defaults to the repository root; skips build artifacts and .git)
 * Exit code: 0 = clean, 1 = violations found.
 *
 * Pure Node, no dependencies.
 */

import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join, resolve, relative } from 'node:path'

const SKIP_DIRS = new Set([
  '.git',
  '.vs',
  '.opencode',
  'node_modules',
  'obj',
  'bin',
  'dist',
  'out',
  '.build',
  '.next',
  '.turbo',
  '.vite',
  '.cache',
  'coverage',
  'Build',
  'Build-English',
  'BuildInstaller',
  'BuildInstallerPayload',
  'release-assets',
  'I18nTranslate',
  '_agent_out'
])

/** Source/text extensions that get scanned (whitelist keeps the walk fast). */
const SOURCE_EXT = new Set([
  '.cs', '.ts', '.tsx', '.js', '.jsx', '.mjs', '.cjs', '.css', '.scss', '.less',
  '.json', '.jsonc', '.xaml', '.resx', '.props', '.targets', '.csproj', '.sln',
  '.slnx', '.editorconfig', '.yaml', '.yml', '.toml', '.ini', '.md', '.txt',
  '.html', '.htm', '.vue', '.axaml', '.xml', '.bat', '.ps1', '.sh', '.cmd'
])

/** Source/text extensions that get scanned (whitelist keeps the walk fast). */
const SKIP_EXT = new Set([])

/** Name → code points for format / invisible characters. */
const BAD_FORMAT = {
  'ZERO WIDTH SPACE': [0x200b],
  'ZERO WIDTH NON-JOINER': [0x200c],
  'ZERO WIDTH JOINER': [0x200d],
  'WORD JOINER': [0x2060],
  'ZERO WIDTH NO-BREAK SPACE/BOM': [0xfeff],
  'LINE SEPARATOR': [0x2028],
  'PARAGRAPH SEPARATOR': [0x2029],
  'LEFT-TO-RIGHT EMBEDDING': [0x202a],
  'RIGHT-TO-LEFT EMBEDDING': [0x202b],
  'POP DIRECTIONAL FORMATTING': [0x202c],
  'LEFT-TO-RIGHT OVERRIDE': [0x202d],
  'RIGHT-TO-LEFT OVERRIDE': [0x202e],
  'LEFT-TO-RIGHT MARK': [0x200e],
  'RIGHT-TO-LEFT MARK': [0x200f],
  'SOFT HYPHEN': [0x00ad],
  'NO-BREAK SPACE': [0x00a0],
  'FIGURE SPACE': [0x2007],
  'NARROW NO-BREAK SPACE': [0x202f],
  'EN/EM/THICK/HAIR SPACE': [0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2008, 0x2009, 0x200a],
  'VARIATION SELECTOR-1..16': [0xfe00, 0xfe01, 0xfe02, 0xfe03, 0xfe04, 0xfe05, 0xfe06, 0xfe07, 0xfe08, 0xfe09, 0xfe0a, 0xfe0b, 0xfe0c, 0xfe0d, 0xfe0e, 0xfe0f]
}

/** Full-width ASCII look-alikes (alphanumerics + punctuation). */
const FULLWIDTH_RANGE = [[0xff01, 0xff5e]]

/** Cyrillic look-alikes that visually collide with ASCII. */
const CYRILLIC_LOOKALIKES = new Set([
  0x0410, 0x0430, // А а → A a
  0x0415, 0x0435, // Е е → E e
  0x041e, 0x043e, // О о → O o
  0x0420, 0x0440, // Р р → P p
  0x0421, 0x0441, // С с → C c
  0x0422, 0x0442, // Т т → T t
  0x041d, 0x043d, // Н н → H h
  0x041a, 0x043a, // К к → K k
  0x041c, 0x043c, // М м → M m
  0x0412, 0x0432, // В в → B b
  0x0417, 0x0437, // З з → 3
  0x0425, 0x0445, // Х х → X x
  0x0423, 0x0443, // У у → Y y
  0x0418, 0x044f, // И я
  0x0436, 0x0434, // ж д
  0x0448, 0x0449  // ш щ
])

/** Greek look-alikes for common letters. */
const GREEK_LOOKALIKES = new Set([
  0x0391, 0x03b1, // Α α → A a
  0x0392, 0x03b2, // Β β → B b
  0x0395, 0x03b5, // Ε ε → E e
  0x039f, 0x03bf, // Ο ο → O o
  0x03a1, 0x03c1, // Ρ ρ → P p
  0x03a4, 0x03c4, // Τ τ → T t
  0x039d, 0x03bd, // Ν ν → N v
  0x039c, 0x03bc, // Μ μ → M μ
  0x039a, 0x03ba, // Κ κ → K k
  0x03a3, 0x03c3, // Σ σ → S s (partial)
  0x0397, 0x03b7, // Η η → H n
  0x03a7, 0x03c7  // Χ χ → X x
])

function describeChar(code) {
  for (const [name, points] of Object.entries(BAD_FORMAT)) {
    if (points.includes(code)) return name
  }
  if (code >= 0xff01 && code <= 0xff5e) {
    return `FULLWIDTH ${String.fromCodePoint(code)} (looks like ${String.fromCharCode(code - 0xfee0)})`
  }
  if (CYRILLIC_LOOKALIKES.has(code)) return `CYRILLIC ${String.fromCodePoint(code)}`
  if (GREEK_LOOKALIKES.has(code)) return `GREEK ${String.fromCodePoint(code)}`
  return `U+${code.toString(16).toUpperCase().padStart(4, '0')}`
}

function scanFile(filePath) {
  if (filePath.endsWith('AGENTS.md') || /[\\/]CheckSourceUnicode[\\/]/i.test(filePath) || filePath.endsWith('check-unicode.mjs')) return []
  let bytes
  try {
    bytes = readFileSync(filePath)
  } catch {
    return [] // locked / unreadable — skip
  }
  let text
  try {
    text = new TextDecoder('utf-8', { fatal: false }).decode(bytes)
  } catch {
    return [] // binary / unreadable
  }
  const isCyrillicLocale =
    /[\\/](?:locales|Languages|i18n|Resources)[\\/].*\b(?:ru|bg|uk|uz|ug|sr|be|kk|mk)\b/i.test(filePath) ||
    filePath.endsWith('.resx') ||
    filePath.endsWith('ru.ts') ||
    filePath.endsWith('bg.ts') ||
    filePath.endsWith('uk.ts') ||
    filePath.endsWith('CommandInjectionTests.cs') ||
    filePath.endsWith('renderer.mjs') ||
    filePath.endsWith('i18n.mjs') ||
    filePath.endsWith('antdLocale.ts') ||
    filePath.endsWith('i18n\\index.ts') ||
    filePath.endsWith('i18n/index.ts') ||
    filePath.endsWith('catalog.json')
  const isGreekLocale =
    /[\\/](?:locales|Languages|i18n|Resources)[\\/].*\b(?:el)\b/i.test(filePath) ||
    filePath.endsWith('el.ts') ||
    filePath.endsWith('renderer.mjs') ||
    filePath.endsWith('i18n.mjs') ||
    filePath.endsWith('antdLocale.ts') ||
    filePath.endsWith('i18n\\index.ts') ||
    filePath.endsWith('i18n/index.ts') ||
    filePath.endsWith('catalog.json')
  if (filePath.endsWith('AGENTS.md') || filePath.includes('CheckSourceUnicode')) return []
  const isResx = filePath.endsWith('.resx')
  if (isResx) return [] // Resource satellite files contain legitimate multilingual scripts and ligatures
  const isMarkdown = filePath.endsWith('.md')
  const hits = []
  for (let i = 0; i < text.length; i++) {
    const code = text.codePointAt(i)
    if (code === undefined) break
    if (code === 0xfeff && i === 0) continue // leading BOM is benign-ish but flag? keep flagged via loop below
    if (isMarkdown && (code === 0xfe0f || code === 0x2002)) continue // emoji variation selector and markdown spacing
    const isBad =
      Object.values(BAD_FORMAT).some((points) => points.includes(code)) ||
      (code >= 0xff21 && code <= 0xff3a) || // fullwidth uppercase Latin
      (code >= 0xff41 && code <= 0xff5a) || // fullwidth lowercase Latin
      (code >= 0xff10 && code <= 0xff19) || // fullwidth digits
      (!isCyrillicLocale && CYRILLIC_LOOKALIKES.has(code)) ||
      (!isGreekLocale && GREEK_LOOKALIKES.has(code))
    if (!isBad) continue
    const line = text.slice(0, i).split('\n').length
    const col = i - text.lastIndexOf('\n', i - 1)
    const preview = text.slice(Math.max(0, i - 12), i + 12).replace(/[\n\r]/g, ' ')
    hits.push({ line, col, code, name: describeChar(code), preview })
    if (code >= 0x10000) i++ // skip low surrogate
  }
  return hits
}

function walk(dir, out) {
  let entries
  try {
    entries = readdirSync(dir)
  } catch {
    return out
  }
  for (const entry of entries) {
    const full = join(dir, entry)
    let stat
    try {
      stat = statSync(full)
    } catch {
      continue
    }
    if (stat.isDirectory()) {
      if (!SKIP_DIRS.has(entry)) walk(full, out)
    } else {
      const dot = entry.lastIndexOf('.')
      const ext = dot >= 0 ? entry.slice(dot).toLowerCase() : ''
      if (SOURCE_EXT.has(ext)) out.push(full)
    }
  }
  return out
}

const root = resolve(process.argv[2] ?? process.cwd())
const files = walk(root, [])
let violations = 0

for (const file of files) {
  const hits = scanFile(file)
  if (hits.length === 0) continue
  violations += hits.length
  const rel = relative(root, file) || file
  for (const hit of hits) {
    console.log(`${rel}:${hit.line}:${hit.col}  [${hit.name}]  …${hit.preview}…`)
  }
}

if (violations > 0) {
  console.error(`\n${violations} Unicode violation(s) found. Remove them before committing.`)
  process.exit(1)
}
console.log(`OK — no Unicode hygiene violations in ${files.length} files.`)
