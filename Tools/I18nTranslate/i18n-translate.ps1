# i18n-translate.ps1 - UDT resx batch translation pipeline
#
# Reads the source .resx files declared in crowdin.yml, translates every key
# to the target locales listed in locales.txt through the local llama-server
# (OpenAI-compatible API), validates placeholders, and writes back
# Resource.<locale>.resx files (UTF-8, no BOM).
#
# Prompts and keep-as-is terms come from prompts.json + glossary.json
# (built by build-prompt-pack.py from the language-family templates).
#
# Usage:
#   .\i18n-translate.ps1                     # all locales
#   .\i18n-translate.ps1 -Locales hi,sw      # pilot run for specific locales
#   .\i18n-translate.ps1 -DryRun             # list what would be done
#   .\i18n-translate.ps1 -ParallelJobs 4     # parallel locale workers
#
# Requires: llama-server running (see ..\..\LocalAI-API\start-ai.ps1)
param(
    [string[]]$Locales,
    [switch]$DryRun,
    [int]$ParallelJobs = 4,
    [string]$RepoRoot = '',
    [string]$LocaleFile = '',
    [string]$PromptPack = '',
    [string]$GlossaryPath = ''
)

$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot
if (-not $RepoRoot) { $RepoRoot = Split-Path (Split-Path $scriptDir -Parent) -Parent }
if (-not $LocaleFile) { $LocaleFile = Join-Path $scriptDir 'locales.txt' }
if (-not $PromptPack) { $PromptPack = Join-Path $scriptDir 'prompts.json' }
if (-not $GlossaryPath) { $GlossaryPath = Join-Path $scriptDir 'glossary.json' }
$reportDir = Join-Path $scriptDir 'reports'
New-Item -ItemType Directory -Path $reportDir -Force | Out-Null

$enginePorts = @{ tg = 11434; g4 = 11435 }
$batchSize = 10
$defaultPlaceholderRegex = '\{\{-[A-Za-z0-9_]+\}\}|\{\{[A-Za-z0-9_]+,\s*[^}]+\}\}|\{\{[A-Za-z0-9_]+\}\}|\{[0-9]+:[^}]+\}|\{[0-9]+\}|\{[A-Za-z_][A-Za-z0-9_]*\}|%[sdifxun%]|\\[rn]|<[^>]+>|&(?:amp|lt|gt|quot|apos);|&#(?:[0-9]+|x[0-9A-Fa-f]+);'

if (-not (Test-Path -LiteralPath $PromptPack)) { throw "prompts.json not found at $PromptPack" }
if (-not (Test-Path -LiteralPath $GlossaryPath)) { throw "glossary.json not found at $GlossaryPath" }
$promptPackObject = Get-Content -LiteralPath $PromptPack -Raw -Encoding UTF8 | ConvertFrom-Json
$glossaryObject = Get-Content -LiteralPath $GlossaryPath -Raw -Encoding UTF8 | ConvertFrom-Json
$placeholderRegex = [string]$promptPackObject.placeholderRegex
if (-not $placeholderRegex) { $placeholderRegex = $defaultPlaceholderRegex }

# ---------------------------------------------------------------- helpers ---

function Get-PlaceholderSet {
    param([string]$S, [string]$Pattern)
    if (-not $Pattern) { $Pattern = $script:placeholderRegex }
    $set = New-Object System.Collections.Generic.List[string]
    foreach ($m in [regex]::Matches($S, $Pattern)) { $set.Add($m.Value) }
    return ($set | Sort-Object) -join '|'
}

function Assert-Placeholders {
    param([string]$Src, [string]$Dst, [string]$Pattern)
    return (Get-PlaceholderSet $Src $Pattern) -eq (Get-PlaceholderSet $Dst $Pattern)
}

function Read-TextKeys {
    param([string]$Path)
    $xml = New-Object System.Xml.XmlDocument
    $xml.Load($Path)
    $rows = New-Object System.Collections.Generic.List[object]
    foreach ($node in $xml.SelectNodes('//data')) {
        $name = $node.GetAttribute('name')
        if ($node.HasAttribute('type') -or $node.HasAttribute('mimetype')) { continue }
        $value = $node.SelectSingleNode('value').InnerText
        if ([string]::IsNullOrWhiteSpace($value)) { continue }
        $rows.Add(@{ Name = $name; Value = $value.Trim() })
    }
    return $rows
}

function Get-EnglishName {
    param([string]$Code)
    $map = @{
        'af'='Afrikaans'; 'am'='Amharic'; 'ar'='Arabic'; 'az'='Azerbaijani'; 'be'='Belarusian'
        'bg'='Bulgarian'; 'bn'='Bengali'; 'bs'='Bosnian'; 'ca'='Catalan'; 'cs'='Czech'
        'cy'='Welsh'; 'da'='Danish'; 'de'='German'; 'el'='Greek'; 'en-GB'='British English'
        'es'='Spanish'; 'es-MX'='Mexican Spanish'; 'et'='Estonian'; 'eu'='Basque'; 'fa'='Persian'
        'fi'='Finnish'; 'fr'='French'; 'fr-CA'='Canadian French'; 'ga'='Irish'; 'gl'='Galician'
        'gu'='Gujarati'; 'he'='Hebrew'; 'hi'='Hindi'; 'hr'='Croatian'; 'hu'='Hungarian'
        'hy'='Armenian'; 'id'='Indonesian'; 'is'='Icelandic'; 'it'='Italian'; 'ja'='Japanese'
        'ka'='Georgian'; 'kk'='Kazakh'; 'km'='Khmer'; 'kn'='Kannada'; 'ko'='Korean'
        'ky'='Kyrgyz'; 'lo'='Lao'; 'lt'='Lithuanian'; 'lv'='Latvian'; 'mk'='Macedonian'
        'ml'='Malayalam'; 'mn'='Mongolian'; 'mr'='Marathi'; 'ms'='Malay'; 'my'='Burmese'
        'ne'='Nepali'; 'nl'='Dutch'; 'nl-BE'='Flemish'; 'no'='Norwegian'; 'pa'='Punjabi'
        'pl'='Polish'; 'ps'='Pashto'; 'pt'='Portuguese'; 'pt-BR'='Brazilian Portuguese'; 'ro'='Romanian'
        'ru'='Russian'; 'si'='Sinhala'; 'sk'='Slovak'; 'sl'='Slovenian'; 'sq'='Albanian'
        'sr'='Serbian'; 'sr-Latn'='Serbian Latin'; 'sv'='Swedish'; 'sw'='Swahili'; 'ta'='Tamil'
        'te'='Telugu'; 'th'='Thai'; 'tl'='Tagalog'; 'tr'='Turkish'; 'uk'='Ukrainian'
        'ur'='Urdu'; 'uz'='Uzbek'; 'uz-Latn-UZ'='Uzbek Latin'; 'vi'='Vietnamese'; 'yo'='Yoruba'
        'zh-Hans'='Simplified Chinese'; 'zh-Hant'='Traditional Chinese'; 'zu'='Zulu'
        'eo'='Esperanto'; 'ceb'='Cebuano'; 'co'='Corsican'; 'fy'='Frisian'; 'gd'='Scottish Gaelic'
        'ha'='Hausa'; 'haw'='Hawaiian'; 'ht'='Haitian Creole'; 'ig'='Igbo'; 'jv'='Javanese'
        'lb'='Luxembourgish'; 'mg'='Malagasy'; 'mi'='Maori'; 'mt'='Maltese'; 'ny'='Chichewa'
        'oc'='Occitan'; 'om'='Oromo'; 'qu'='Quechua'; 'rw'='Kinyarwanda'; 'sm'='Samoan'
        'sn'='Shona'; 'so'='Somali'; 'st'='Sesotho'; 'su'='Sundanese'; 'tg'='Tajik'
        'tk'='Turkmen'; 'tt'='Tatar'; 'ug'='Uyghur'; 'wa'='Walloon'; 'xh'='Xhosa'
        'yi'='Yiddish'; 'ckb'='Central Kurdish'; 'dv'='Divehi'; 'dz'='Dzongkha'; 'ti'='Tigrinya'
        'ak'='Akan'; 'ln'='Lingala'; 'ff'='Fulah'; 'ee'='Ewe'; 'kr'='Kanuri'
        'ks'='Kashmiri'; 'sd'='Sindhi'; 'or'='Odia'; 'as'='Assamese'
        'mn-Mong'='Mongolian Traditional'; 'nb'='Norwegian Bokmal'; 'nn'='Norwegian Nynorsk'
    }
    if ($map.ContainsKey($Code)) { return $map[$Code] }
    return $Code
}

# ------------------------------------------------------------- parsing ----

$crowdin = Join-Path $RepoRoot 'crowdin.yml'
if (-not (Test-Path -LiteralPath $crowdin)) { throw "crowdin.yml not found at $crowdin" }
$crowdinText = Get-Content -LiteralPath $crowdin -Raw -Encoding UTF8
$pairs = New-Object System.Collections.Generic.List[object]
$srcPattern = [regex]'- source:\s*/?([^\r\n]+)'
$trnPattern = [regex]'translation:\s*/?([^\r\n]+)'
$srcMatches = $srcPattern.Matches($crowdinText)
$trnMatches = $trnPattern.Matches($crowdinText)
if ($srcMatches.Count -ne $trnMatches.Count) { throw 'crowdin.yml parse error: source/translation count mismatch' }
for ($i = 0; $i -lt $srcMatches.Count; $i++) {
    $src = $srcMatches[$i].Groups[1].Value.Trim().Trim('/').Replace('/', '\')
    $trn = $trnMatches[$i].Groups[1].Value.Trim().Trim('/').Replace('/', '\')
    if (-not $trn.Contains('%locale%')) { continue }
    $pairs.Add(@{ Source = Join-Path $RepoRoot $src; TranslationPattern = Join-Path $RepoRoot $trn })
}
if ($pairs.Count -eq 0) { throw 'no translatable source/translation pairs found in crowdin.yml' }

$localeLines = Get-Content -LiteralPath $LocaleFile -Encoding UTF8 | Where-Object { $_ -and -not $_.TrimStart().StartsWith('#') }
$allLocales = New-Object System.Collections.Generic.List[object]
foreach ($line in $localeLines) {
    $parts = $line -split '\s+'
    if ($parts.Count -lt 2) { continue }
    $allLocales.Add(@{ Code = $parts[0]; Engine = $parts[1]; Name = Get-EnglishName $parts[0] })
}
if ($Locales -and $Locales.Count -gt 0) {
    # -File invocation passes the whole list as one comma-joined string: split it
    $wanted = @($Locales -join ',' -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $filtered = $allLocales | Where-Object { $wanted -contains $_.Code }
    $allLocales = New-Object System.Collections.Generic.List[object]
    foreach ($f in $filtered) { $allLocales.Add($f) }
}
Write-Host "Modules: $($pairs.Count), Locales: $($allLocales.Count)"
foreach ($p in $pairs) { Write-Host "  source: $($p.Source)" }

if ($DryRun) {
    $planned = 0
    foreach ($p in $pairs) {
        if (-not (Test-Path -LiteralPath $p.Source)) { Write-Host "  [MISSING] $($p.Source)"; continue }
        $keys = Read-TextKeys $p.Source
        foreach ($loc in $allLocales) {
            $target = $p.TranslationPattern.Replace('%locale%', $loc.Code)
            $pending = $keys
            if (Test-Path -LiteralPath $target) {
                $existing = Read-TextKeys $target
                $have = @{}; foreach ($e in $existing) { $have[$e.Name] = $true }
                $pending = $keys | Where-Object { -not $have.ContainsKey($_.Name) }
            }
            if ($pending.Count -gt 0) { $planned += $pending.Count }
        }
    }
    Write-Host "DRY RUN: $planned strings would be translated"
    exit 0
}

# --------------------------------------------------------- worker job ----

$worker = {
    param($Args2)
    $pair = $Args2.pair
    $loc = $Args2.loc
    $batchSize = $Args2.batchSize
    $enginePorts = $Args2.enginePorts
    $prompts = $Args2.prompts
    $glossary = @($Args2.glossary)
    $placeholderRegex = [string]$Args2.placeholderRegex
    $ErrorActionPreference = 'Stop'

    function Invoke-Chat2 {
        param([int]$Port, [string]$System, [string]$User)
        $payload = @{ model = 'local'; messages = @(); temperature = 0; max_tokens = 2048; repeat_penalty = 1.15; top_p = 0.9 }
        if ($System) { $payload.messages += @{ role = 'system'; content = $System } }
        $payload.messages += @{ role = 'user'; content = $User }
        $body = $payload | ConvertTo-Json -Depth 6
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($body)
        $r = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/v1/chat/completions" -Method Post `
            -ContentType 'application/json' -Body $bytes -TimeoutSec 300
        return [string]$r.choices[0].message.content
    }
    function Invoke-Chat3 {
        param([int]$Port, [string]$System, [string]$User)
        for ($a = 1; $a -le 3; $a++) {
            try { return (Invoke-Chat2 $Port $System $User) }
            catch {
                if ($a -eq 3) { throw }
                Start-Sleep -Seconds 2
            }
        }
    }
    function Get-PlaceholderSet2 {
        param([string]$S)
        $set = New-Object System.Collections.Generic.List[string]
        foreach ($m in [regex]::Matches($S, $placeholderRegex)) { $set.Add($m.Value) }
        return ($set | Sort-Object) -join '|'
    }
    function Assert-Placeholders2 {
        param([string]$Src, [string]$Dst)
        return (Get-PlaceholderSet2 $Src) -eq (Get-PlaceholderSet2 $Dst)
    }
    function Get-JsonProp {
        param($Obj, [string]$Name)
        if (-not $Obj -or [string]::IsNullOrEmpty($Name)) { return $null }
        $p = $Obj.PSObject.Properties[$Name]
        if ($p) { return $p.Value }
        return $null
    }
    function Limit-Text {
        param([string]$Text, [int]$Max)
        if ([string]::IsNullOrEmpty($Text) -or $Text.Length -le $Max) { return [string]$Text }
        $cut = $Text.Substring(0, $Max)
        $nl = $cut.LastIndexOf([char]10)
        if ($nl -ge 80) { return $cut.Substring(0, $nl).TrimEnd() }
        return $cut.TrimEnd()
    }
    function Test-TermInText {
        param([string]$Text, [string]$Term)
        if ([string]::IsNullOrEmpty($Text) -or [string]::IsNullOrEmpty($Term)) { return $false }
        if ($Term.Length -le 4) {
            $pattern = '(?<![A-Za-z0-9])' + [regex]::Escape($Term) + '(?![A-Za-z0-9])'
            return [regex]::IsMatch($Text, $pattern)
        }
        return $Text.IndexOf($Term) -ge 0
    }
    function Get-GlossaryHits {
        param($Rows, [string]$LocCode)
        $hits = New-Object System.Collections.Generic.List[object]
        $seen = @{}
        foreach ($g in $glossary) {
            $srcTerm = [string]$g.source
            if ($seen.ContainsKey($srcTerm)) { continue }
            # locale-scoped entries: only apply when the current locale is listed
            if ($g.PSObject.Properties.Name -contains 'locales') {
                $locales = @($g.locales)
                if ($LocCode -and $locales -notcontains $LocCode) { continue }
                if (-not $LocCode -and $locales) { continue }
            }
            foreach ($row in $Rows) {
                if (Test-TermInText ([string]$row.Value) $srcTerm) {
                    $hits.Add($g)
                    $seen[$srcTerm] = $true
                    break
                }
            }
        }
        return $hits
    }
    function Build-GlossaryBlock {
        param($Hits)
        if (-not $Hits -or $Hits.Count -eq 0) { return '' }
        $lines = foreach ($g in $Hits) {
            if ([string]$g.mode -eq 'translate') {
                '- "' + $g.source + '" must be translated exactly as "' + $g.target + '"'
            } else {
                '- "' + $g.source + '" must stay "' + $g.target + '"'
            }
        }
        return "Glossary (must follow):`n" + ($lines -join "`n")
    }
    function Get-FamilyStyle {
        param($LocCode, $Engine)
        $famName = [string](Get-JsonProp $prompts.localeFamily $LocCode)
        if (-not $famName) { $famName = 'other' }
        $family = Get-JsonProp $prompts.families $famName
        if (-not $family) { return '' }
        $note = [string](Get-JsonProp $family.localeNotes $LocCode)
        $style = [string]$family.stylePrompt
        $max = 400
        if ($Engine -eq 'g4') { $max = 1600 }
        if ($prompts.tgMaxFamilyChars -and $Engine -eq 'tg') { $max = [int]$prompts.tgMaxFamilyChars }
        if ($prompts.g4MaxFamilyChars -and $Engine -eq 'g4') { $max = [int]$prompts.g4MaxFamilyChars }
        if ($Engine -eq 'tg') {
            if ($note) { return (Limit-Text $note $max) }
            return (Limit-Text $style $max)
        }
        $combined = $note
        if ($style) {
            if ($combined) { $combined = $combined + "`n" + $style } else { $combined = $style }
        }
        if ($LocCode -eq 'zh-Hans' -and $prompts.fewShotZhHans) {
            $combined = $combined + "`n" + [string]$prompts.fewShotZhHans
        }
        return (Limit-Text $combined $max)
    }
    function Expand-PromptTemplate {
        param([string]$Template, [hashtable]$Vars)
        $s = [string]$Template
        foreach ($key in @('LanguageName', 'FamilyStyle', 'GlossaryBlock', 'RetryInstruction')) {
            if ($Vars.ContainsKey($key)) { $s = $s.Replace('{' + $key + '}', [string]$Vars[$key]) }
        }
        foreach ($key in @('Lines', 'Line')) {
            if ($Vars.ContainsKey($key)) { $s = $s.Replace('{' + $key + '}', [string]$Vars[$key]) }
        }
        return $s
    }
    function Repair-Translation {
        param([string]$Src, [string]$Dst)
        if ([string]::IsNullOrWhiteSpace($Dst)) { return $Dst }
        $t = $Dst.Trim()
        $t = [regex]::Replace($t, '^```(?:\w+)?\s*', '')
        $t = [regex]::Replace($t, '\s*```$', '')
        $t = $t.Trim()
        if ($t -match '^\[\d+\]\s*(.+)$') { $t = $Matches[1].Trim() }
        if ($t.Length -ge 2) {
            $pairs = @(
                @('"', '"'),
                @("'", "'"),
                @([string][char]0x201C, [string][char]0x201D),
                @([string][char]0x300C, [string][char]0x300D)
            )
            foreach ($p in $pairs) {
                if ($t.StartsWith($p[0]) -and $t.EndsWith($p[1])) {
                    $inner = $t.Substring($p[0].Length, $t.Length - $p[0].Length - $p[1].Length).Trim()
                    if ($inner) { $t = $inner }
                }
            }
        }
        $escaped = [regex]::Escape($Src.Trim())
        $t = [regex]::Replace($t, '\s*[\(\uFF08]\s*' + $escaped + '\s*[\)\uFF09]\s*$', '')
        $t = $t.Replace([char]0x00A0, ' ')
        $t = [regex]::Replace($t, '[\u200B-\u200F\u2028-\u202F\u2060\uFEFF\u00AD]', '')
        $sb = New-Object System.Text.StringBuilder
        foreach ($ch in $t.ToCharArray()) {
            $c = [int]$ch
            if (($c -ge 0xFF10 -and $c -le 0xFF19) -or ($c -ge 0xFF21 -and $c -le 0xFF3A) -or ($c -ge 0xFF41 -and $c -le 0xFF5A)) {
                [void]$sb.Append([char]($c - 0xFEE0))
            } else {
                [void]$sb.Append($ch)
            }
        }
        return $sb.ToString().Trim()
    }
    function Strip-ModelEnvelope {
        param([string]$Content)
        if ([string]::IsNullOrWhiteSpace($Content)) { return $Content }
        $c = $Content.Trim()
        $c = [regex]::Replace($c, '^```(?:\w+)?\s*', '')
        $c = [regex]::Replace($c, '\s*```\s*$', '')
        $lines = $c -split "`r?`n"
        $kept = New-Object System.Collections.Generic.List[string]
        $started = $false
        foreach ($line in $lines) {
            if ($line -match '^\[\d+\]\s*') { $started = $true; $kept.Add($line) }
            elseif ($started) { $kept.Add($line) }
        }
        if ($kept.Count -gt 0) { return ($kept -join "`n") }
        return $c.Trim()
    }
    function Assert-KeepTerms {
        param([string]$Src, [string]$Dst, $Hits)
        foreach ($g in $Hits) {
            if ([string]$g.mode -eq 'translate') { continue }  # translate-mode entries are directives, not keep-checks
            $term = [string]$g.source
            if ((Test-TermInText $Src $term) -and -not (Test-TermInText $Dst $term)) { return $false }
        }
        return $true
    }
    function Test-TranslationOk {
        param([string]$Src, [string]$Dst, $Hits)
        if ([string]::IsNullOrWhiteSpace($Dst)) { return $false }
        if (-not (Assert-Placeholders2 $Src $Dst)) { return $false }
        if (-not (Assert-KeepTerms $Src $Dst $Hits)) { return $false }
        return $true
    }
    function Get-EngineTemplates {
        param([string]$Engine)
        $eng = Get-JsonProp $prompts.engines $Engine
        if (-not $eng) { $eng = Get-JsonProp $prompts.engines 'g4' }
        return $eng
    }

    $port = $enginePorts[$loc.Engine]
    $engineTemplates = Get-EngineTemplates $loc.Engine
    $familyStyle = Get-FamilyStyle $loc.Code $loc.Engine
    $langName = [string]$loc.Name
    $retryInstruction = [string]$prompts.retryInstruction

    $stats = @{ translated = 0; failed = 0; failedKeys = @() }
    $target = $pair.TranslationPattern.Replace('%locale%', $loc.Code)
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null

    $sourceRows = @()
    $srcXml = New-Object System.Xml.XmlDocument
    $srcXml.Load($pair.Source)
    foreach ($node in $srcXml.SelectNodes('//data')) {
        if ($node.HasAttribute('type') -or $node.HasAttribute('mimetype')) { continue }
        $v = $node.SelectSingleNode('value').InnerText
        if ([string]::IsNullOrWhiteSpace($v)) { continue }
        $sourceRows += @{ Name = $node.GetAttribute('name'); Value = $v.Trim(); XmlSpace = $node.SelectSingleNode('value').HasAttribute('xml:space') }
    }

    $existing = @{}
    $srcByName = @{}
    foreach ($row in $sourceRows) { $srcByName[$row.Name] = $row.Value }
    $targetExists = Test-Path -LiteralPath $target
    if ($targetExists) {
        $tXml = New-Object System.Xml.XmlDocument
        $tXml.Load($target)
        foreach ($node in $tXml.SelectNodes('//data')) {
            if ($node.HasAttribute('type') -or $node.HasAttribute('mimetype')) { continue }
            $v = $node.SelectSingleNode('value').InnerText
            if ([string]::IsNullOrWhiteSpace($v)) { continue }
            $name = $node.GetAttribute('name')
            # a value identical to the English source is an untranslated fallback: re-translate
            if ($srcByName.ContainsKey($name) -and $v.Trim() -eq $srcByName[$name]) { continue }
            $existing[$name] = $true
        }
    }
    $pending = @($sourceRows | Where-Object { -not $existing.ContainsKey($_.Name) })

    if ($pending.Count -eq 0) {
        Write-Host ("    [{0}] {1}: up to date ({2} keys)" -f $loc.Code, $pair.Source, $sourceRows.Count)
        return $null
    }
    Write-Host ("    [{0}] {1}: {2} new keys" -f $loc.Code, (Split-Path $pair.Source -Parent | Split-Path -Leaf), $pending.Count)

    $results = @{}
    for ($i = 0; $i -lt $pending.Count; $i += $batchSize) {
      try {
        $batch = @($pending[$i..([math]::Min($i + $batchSize - 1, $pending.Count - 1))])
        $lines = @()
        $j = 0
        foreach ($item in $batch) {
            $j++
            $lines += ("[{0}] {1}" -f $j, $item.Value)
        }
        $lineBlock = $lines -join "`n"
        $hits = @(Get-GlossaryHits $batch $loc.Code)
        $glossaryBlock = Build-GlossaryBlock $hits
        $vars = @{
            LanguageName  = $langName
            FamilyStyle   = $familyStyle
            GlossaryBlock = $glossaryBlock
            Lines         = $lineBlock
            Line          = ''
            RetryInstruction = $retryInstruction
        }

        if ($loc.Engine -eq 'tg') {
            $user = Expand-PromptTemplate ([string]$engineTemplates.batchUser) $vars
            $content = Invoke-Chat3 $port '' $user
        } else {
            $system = Expand-PromptTemplate ([string]$engineTemplates.system) $vars
            $user = Expand-PromptTemplate ([string]$engineTemplates.batchUser) $vars
            $content = Invoke-Chat3 $port $system $user
        }
        $content = Strip-ModelEnvelope $content

        $parsed = @{}
        try {
        foreach ($m in [regex]::Matches($content, '\[(\d+)\]\s*([^\r\n]*)')) {
            $idx = [int]$m.Groups[1].Value
            if ($idx -lt 1 -or $idx -gt $batch.Count) { continue }
            $text = Repair-Translation $batch[$idx - 1].Value $m.Groups[2].Value
            if ($text) { $parsed[$idx] = $text }
        }
        for ($bi = 0; $bi -lt $batch.Count; $bi++) {
            $item = $batch[$bi]
            $idx = $bi + 1
            $needRetry = $false
            if (-not $parsed.ContainsKey($idx)) { $needRetry = $true }
            elseif (-not (Test-TranslationOk $item.Value $parsed[$idx] $hits)) { $needRetry = $true }
            if (-not $needRetry) { continue }

            $ok = $false
            try {
                $vars.Line = $item.Value
                $vars.Lines = $item.Value
                if ($loc.Engine -eq 'tg') {
                    $u2 = Expand-PromptTemplate ([string]$engineTemplates.retryUser) $vars
                    $c2 = Invoke-Chat3 $port '' $u2
                } else {
                    $system2 = Expand-PromptTemplate ([string]$engineTemplates.system) $vars
                    $u2 = Expand-PromptTemplate ([string]$engineTemplates.retryUser) $vars
                    $c2 = Invoke-Chat3 $port $system2 $u2
                }
                $c2 = Repair-Translation $item.Value (Strip-ModelEnvelope $c2)
                if ($c2 -and (Test-TranslationOk $item.Value $c2 $hits)) { $parsed[$idx] = $c2; $ok = $true }
            } catch { }
            if (-not $ok) {
                $stats.failed++
                $stats.failedKeys += $item.Name
                if ($parsed.ContainsKey($idx)) { $parsed.Remove($idx) }
            }
        }
        } catch {
            foreach ($item in $batch) {
                if (-not $results.ContainsKey($item.Name)) {
                    $stats.failed++
                    $stats.failedKeys += $item.Name
                }
            }
            $parsed = @{}
        }
        foreach ($kv in $parsed.GetEnumerator()) {
            $bi = $kv.Key - 1
            if ($bi -ge 0 -and $bi -lt $batch.Count) { $results[$batch[$bi].Name] = $kv.Value }
        }
        Write-Host ("      batch {0}/{1} done, {2} ok" -f ([math]::Floor($i / $batchSize) + 1), [math]::Ceiling($pending.Count / $batchSize), $parsed.Count)
      } catch {
        # whole-batch failure (e.g. server down): mark batch keys failed, keep going
        foreach ($item in $batch) {
            if (-not $results.ContainsKey($item.Name)) {
                $stats.failed++
                $stats.failedKeys += $item.Name
            }
        }
      }
    }

    $merged = New-Object System.Collections.Generic.List[object]
    $known = @{}
    if ($targetExists) {
        $tXml2 = New-Object System.Xml.XmlDocument
        $tXml2.Load($target)
        foreach ($node in $tXml2.SelectNodes('//data')) {
            if ($node.HasAttribute('type') -or $node.HasAttribute('mimetype')) { continue }
            $name = $node.GetAttribute('name')
            $v = $node.SelectSingleNode('value').InnerText
            if ([string]::IsNullOrWhiteSpace($v)) { continue }
            if ($results.ContainsKey($name)) { $v = $results[$name] }
            $merged.Add(@{ Name = $name; Value = $v; XmlSpace = $node.SelectSingleNode('value').HasAttribute('xml:space') })
            $known[$name] = $true
        }
    }
    foreach ($row in $sourceRows) {
        if ($known.ContainsKey($row.Name)) { continue }
        if ($results.ContainsKey($row.Name)) {
            $merged.Add(@{ Name = $row.Name; Value = $results[$row.Name]; XmlSpace = $row.XmlSpace })
        } else {
            $merged.Add(@{ Name = $row.Name; Value = $row.Value; XmlSpace = $row.XmlSpace })
        }
    }

    $schema = ''
    if ($targetExists) {
        $tXml3 = New-Object System.Xml.XmlDocument
        $tXml3.PreserveWhitespace = $false
        $tXml3.Load($target)
        $sb = New-Object System.Text.StringBuilder
        foreach ($node in $tXml3.SelectNodes('/root/resheader')) { $null = $sb.Append($node.OuterXml) }
        $schema = $sb.ToString()
    } else {
        $sXml = New-Object System.Xml.XmlDocument
        $sXml.PreserveWhitespace = $false
        $sXml.Load($pair.Source)
        $sb = New-Object System.Text.StringBuilder
        foreach ($node in $sXml.SelectNodes('/root/resheader')) { $null = $sb.Append($node.OuterXml) }
        $schema = $sb.ToString()
    }

    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $settings.Indent = $true
    $writer = [System.Xml.XmlWriter]::Create($target, $settings)
    $writer.WriteStartDocument()
    $writer.WriteStartElement('root')
    $writer.WriteRaw($schema)
    foreach ($row in $merged) {
        $writer.WriteStartElement('data')
        $writer.WriteAttributeString('name', $row.Name)
        $writer.WriteStartElement('value')
        $writer.WriteString($row.Value)
        $writer.WriteEndElement()
        $writer.WriteEndElement()
    }
    $writer.WriteEndElement()
    $writer.WriteEndDocument()
    $writer.Close()

    $stats.translated = $results.Count
    return @{ Locale = $loc.Code; Module = $pair.Source; Translated = $stats.translated; Failed = $stats.failed; FailedKeys = $stats.failedKeys }
}

# -------------------------------------------------------------- dispatch ----

$tasks = New-Object System.Collections.Generic.List[object]
foreach ($pair in $pairs) {
    if (-not (Test-Path -LiteralPath $pair.Source)) {
        Write-Host "SKIP missing source: $($pair.Source)"
        continue
    }
    foreach ($loc in $allLocales) {
        $tasks.Add(@{ pair = $pair; loc = $loc })
    }
}

Write-Host "Starting $($tasks.Count) translation tasks (parallel=$ParallelJobs)..."
$active = New-Object System.Collections.Generic.List[object]
$i = 0
while ($i -lt $tasks.Count -or $active.Count -gt 0) {
    while ($active.Count -lt $ParallelJobs -and $i -lt $tasks.Count) {
        $t = $tasks[$i]
        $jobArgs = @{
            pair = $t.pair
            loc = $t.loc
            batchSize = $batchSize
            enginePorts = $enginePorts
            prompts = $promptPackObject
            glossary = $glossaryObject
            placeholderRegex = $placeholderRegex
        }
        $active.Add(@{ Job = Start-Job -ScriptBlock $worker -ArgumentList $jobArgs; Locale = $t.loc.Code })
        $i++
    }
    $finished = @($active | Where-Object { $_.Job.State -ne 'Running' })
    foreach ($f in $finished) {
        $result = $null
        try {
            $result = Receive-Job -Job $f.Job -Wait -AutoRemoveJob
        } catch {
            Write-Host ("    WARN job failed: " + $_.Exception.Message)
            $null = Remove-Job -Job $f.Job -Force -ErrorAction SilentlyContinue
        }
        if ($result) {
            Write-Host ("    DONE [{0}] translated={1} failed={2}" -f $result.Locale, $result.Translated, $result.Failed)
            if ($result.Failed -gt 0) {
                Write-Host ("         failed keys: {0}" -f ($result.FailedKeys -join ', '))
            }
        }
        $active.Remove($f) | Out-Null
    }
    if ($active.Count -eq $ParallelJobs) { Start-Sleep -Seconds 2 }
}

Write-Host "All tasks finished. Remember to run:"
Write-Host "  node Tools/CheckSourceUnicode/check-unicode.mjs"
