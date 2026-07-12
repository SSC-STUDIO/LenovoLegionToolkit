# Prune non-x64 natives and unsupported satellite culture folders from a shipping payload.
param(
    [Parameter(Mandatory = $true)]
    [string]$PayloadPath,

    [string]$AllowedCultures = ''
)

$ErrorActionPreference = 'Stop'

# PublishDir often ends with '\', which breaks PowerShell "...\" quoting and
# merges the next argument into -PayloadPath.
$PayloadPath = [string]$PayloadPath
if ($PayloadPath.StartsWith('"') -and $PayloadPath.EndsWith('"')) {
    $PayloadPath = $PayloadPath.Substring(1, $PayloadPath.Length - 2)
}
$PayloadPath = $PayloadPath.Trim().TrimEnd([char[]]@('\', '/'))

if ([string]::IsNullOrWhiteSpace($PayloadPath) -or -not (Test-Path -LiteralPath $PayloadPath)) {
    throw "Shipping payload directory not found: $PayloadPath"
}

$root = (Resolve-Path -LiteralPath $PayloadPath).Path

$allowed = @(
    $AllowedCultures -split ';' |
        ForEach-Object { $_.Trim().ToLowerInvariant() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)

$nativeDirs = @('x86', 'arm64')
foreach ($name in $nativeDirs) {
    $path = Join-Path $root $name
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
        Write-Host "Pruned native folder: $name"
    }
}

$kept = 0
$removed = 0
Get-ChildItem -LiteralPath $root -Directory -ErrorAction Stop | ForEach-Object {
    $hasSatellite = $null -ne (
        Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.resources.dll' -ErrorAction SilentlyContinue |
            Select-Object -First 1
    )

    if (-not $hasSatellite) {
        return
    }

    $culture = $_.Name.ToLowerInvariant()
    if ($allowed.Count -gt 0 -and -not ($allowed -contains $culture)) {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
        $removed++
        Write-Host "Pruned satellite culture: $($_.Name)"
    }
    else {
        $kept++
    }
}

Write-Host "Satellite prune complete. kept=$kept removed=$removed root=$root"
