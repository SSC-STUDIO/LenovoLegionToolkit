#!/usr/bin/env pwsh
# governance-gate.ps1 — Zero-regression governance for plugin code-behind.
#
# Rules enforced (from PLG-001, PLG-003, PLG-013):
#   1. No ConfigureAwait(false) in *.xaml.cs (causes thread-pool starvation)
#   2. No MessageBox.Show in plugin code-behind (only WpfHostNotifications.cs is allowed)
#   3. No hardcoded hex colors in plugin *.xaml (must use DynamicResource)
#   4. async void handlers must be wrapped in try-catch (prevents unobserved exceptions)
#
# Exit 0 = all clean; exit 1 = violations found.

param(
    [string]$Root = "."
)

$ErrorActionPreference = "Stop"
$totalViolations = 0

function Write-Violation {
    param([string]$Rule, [string]$File, [string]$Line, [string]$Detail)
    Write-Host "  FAIL [$Rule] ${File}:${Line} — $Detail" -ForegroundColor Red
    $script:totalViolations++
}

# ---------------------------------------------------------------------------
# Rule 1: ConfigureAwait(false) forbidden in *.xaml.cs
# ---------------------------------------------------------------------------
Write-Host "`n=== Rule 1: ConfigureAwait(false) in *.xaml.cs ===" -ForegroundColor Cyan
$xamlCsFiles = Get-ChildItem -Path "$Root/Plugins" -Filter "*.xaml.cs" -Recurse -File
foreach ($file in $xamlCsFiles) {
    $lineNum = 0
    foreach ($line in Get-Content $file.FullName) {
        $lineNum++
        if ($line -match 'ConfigureAwait\s*\(\s*false\s*\)') {
            Write-Violation "ConfigureAwait" $file.FullName $lineNum $line.Trim()
        }
    }
}

# ---------------------------------------------------------------------------
# Rule 2: MessageBox.Show forbidden in plugin code-behind
#         Exception: Shared/WpfHostNotifications.cs (allowed host shim)
# ---------------------------------------------------------------------------
Write-Host "`n=== Rule 2: MessageBox.Show in plugin code-behind ===" -ForegroundColor Cyan
$csFiles = Get-ChildItem -Path "$Root/Plugins" -Filter "*.cs" -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '\.Tests\\' -and
        $_.FullName -notmatch 'WpfHostNotifications\.cs$' -and
        $_.FullName -notmatch 'Resources\\Resource\.Designer\.cs$'
    }
foreach ($file in $csFiles) {
    $lineNum = 0
    foreach ($line in Get-Content $file.FullName) {
        $lineNum++
        if ($line -match 'MessageBox\.Show') {
            Write-Violation "MessageBox" $file.FullName $lineNum $line.Trim()
        }
    }
}

# ---------------------------------------------------------------------------
# Rule 3: Hardcoded hex colors forbidden in plugin *.xaml
#         Allowed: x:Static resources, DynamicResource, TemplateBinding, x:Null
#         Allowed color values: Transparent, White, Black
# ---------------------------------------------------------------------------
Write-Host "`n=== Rule 3: Hardcoded hex colors in plugin *.xaml ===" -ForegroundColor Cyan
$xamlFiles = Get-ChildItem -Path "$Root/Plugins" -Filter "*.xaml" -Recurse -File
foreach ($file in $xamlFiles) {
    $lineNum = 0
    foreach ($line in Get-Content $file.FullName) {
        $lineNum++
        # Skip XML declarations, namespaces, comments, style keys, resource references
        if ($line -match 'xmlns' -or $line -match '<!--' -or $line -match 'x:Key' -or $line -match 'DynamicResource' -or $line -match 'TemplateBinding' -or $line -match 'x:Static' -or $line -match 'x:Null') {
            continue
        }
        # Match hex color pattern: #RGB, #RRGGBB, #AARRGGBB (not inside DynamicResource)
        if ($line -match '#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})\b') {
            Write-Violation "HexColor" $file.FullName $lineNum $line.Trim()
        }
    }
}

# ---------------------------------------------------------------------------
# Rule 4: async void handlers must have try-catch
# ---------------------------------------------------------------------------
Write-Host "`n=== Rule 4: async void without try-catch ===" -ForegroundColor Cyan
$xamlCsFiles2 = Get-ChildItem -Path "$Root/Plugins" -Filter "*.xaml.cs" -Recurse -File
foreach ($file in $xamlCsFiles2) {
    $content = Get-Content $file.FullName -Raw
    $pattern = 'async\s+void\s+(\w+)\s*\('
    $matches = [regex]::Matches($content, $pattern)
    foreach ($m in $matches) {
        $methodName = $m.Groups[1].Value
        $methodStart = $m.Index

        # Find the opening brace of this method body
        $braceSearch = $content.Substring($methodStart, [Math]::Min($content.Length - $methodStart, 500))
        $bracePos = $braceSearch.IndexOf('{')
        if ($bracePos -eq -1) { continue }

        # Extract method body (approximate — up to 500 chars from brace)
        $bodyStart = $methodStart + $bracePos
        $bodySnippet = $content.Substring($bodyStart, [Math]::Min($content.Length - $bodyStart, 1500))

        if ($bodySnippet -notmatch '\btry\b') {
            $lineNum = $content.Substring(0, $methodStart).Split("`n").Count
            Write-Violation "AsyncVoid" $file.FullName $lineNum "$methodName() — async void without try-catch"
        }
    }
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host ""
if ($totalViolations -gt 0) {
    Write-Host "FAILED: $totalViolations governance violation(s) found." -ForegroundColor Red
    exit 1
} else {
    Write-Host "PASSED: All governance rules satisfied." -ForegroundColor Green
    exit 0
}
