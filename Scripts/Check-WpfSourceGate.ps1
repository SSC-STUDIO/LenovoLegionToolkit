param()

$ErrorActionPreference = 'Stop'

# Parity gate: every WPF source file with an Avalonia counterpart must not be
# "much shorter" than the WPF original (length rule). Files without a
# counterpart must either be migrated (deleted from the tree after conversion)
# or have their copies removed. Run after each migration batch.
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$wpfRoot = Join-Path $root 'UniversalDeviceToolkit.WPF'
$avaloniaRoot = Join-Path $root 'UniversalDeviceToolkit.Avalonia'

if (-not (Test-Path -LiteralPath $wpfRoot)) {
    Write-Output 'Parity gate: WPF project not found.'
    exit 0
}

function Get-LineCount([string]$path) {
    try { return (Get-Content -LiteralPath $path).Count } catch { return -1 }
}

$short = [System.Collections.Generic.List[string]]::new()
$duplicateWpfCopy = [System.Collections.Generic.List[string]]::new()
$checked = 0

Get-ChildItem -LiteralPath $wpfRoot -Recurse -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Extension -in '.cs', '.xaml' } |
    ForEach-Object {
        $checked++
        $relative = $_.FullName.Substring($wpfRoot.Length + 1)
        $counterpartRelative = $relative -replace '\.xaml(\.cs)?$', '.axaml$1'
        $counterpart = Join-Path $avaloniaRoot $counterpartRelative

        # A WPF copy still present in the Avalonia tree is unconverted clutter;
        # it must be deleted as part of the migration. It is never treated as
        # the Avalonia implementation, even when it shares its file name.
        $wpfCopy = Join-Path $avaloniaRoot $relative
        $hasWpfCopy = Test-Path -LiteralPath $wpfCopy
        $hasRealCounterpart = $hasWpfCopy -and $wpfCopy -ne $counterpart -and (Test-Path -LiteralPath $counterpart)
        if (-not $hasRealCounterpart) {
            if ($hasWpfCopy) {
                $duplicateWpfCopy.Add($relative)
            }
            return
        }

        $wpfLines = Get-LineCount $_.FullName
        $avaloniaLines = Get-LineCount $counterpart
        $adequate = if ($wpfLines -lt 20) {
            $avaloniaLines -gt 0
        } else {
            $avaloniaLines -ge [math]::Max(40, $wpfLines * 0.5)
        }
        if (-not $adequate) {
            $short.Add("$relative  (WPF=$wpfLines lines, Avalonia=$avaloniaLines lines)")
        }
    }

Write-Output "Parity gate: checked=$checked short=$($short.Count) unconverted-wpf-copies=$($duplicateWpfCopy.Count)"
foreach ($item in $short) { Write-Output "SHORT    $item" }
foreach ($item in $duplicateWpfCopy) { Write-Output "WPF-COPY $item" }

if ($short.Count -gt 0 -or $duplicateWpfCopy.Count -gt 0) {
    Write-Output ''
    Write-Output 'Migrate each WPF copy (delete the .xaml/.cs file in the Avalonia tree)'
    Write-Output 'and expand any counterpart that is much shorter than the WPF original.'
    exit 1
}

exit 0
