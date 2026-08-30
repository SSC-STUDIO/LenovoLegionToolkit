# Prune shipping debug symbols, unsupported satellite cultures, and Windows non-x64 natives.
param(
    [Parameter(Mandatory = $true)]
    [string]$PayloadPath,

    [string]$RuntimeIdentifier = 'win-x64',

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

$removedPdbs = @(
    Get-ChildItem -LiteralPath $root -Filter '*.pdb' -File -Recurse -Force -ErrorAction Stop
)
foreach ($pdb in $removedPdbs) {
    Remove-Item -LiteralPath $pdb.FullName -Force
    Write-Host "Pruned debug symbol: $($pdb.FullName)"
}

# Debugger / dump / POSIX helpers never run in the shipping Host. Documentation
# XML next to a DLL is leftover from NuGet packages and is not loaded at runtime.
$namedDebugArtifacts = [System.Collections.Generic.HashSet[string]]::new(
    [string[]]@(
        'createdump.exe',
        'createdump',
        'mscordbi.dll',
        'libmscordbi.so',
        'libmscordbi.dylib',
        'dbgshim.dll',
        'libdbgshim.so',
        'libdbgshim.dylib',
        'libmonoposixhelper.dll',
        'libmonoposixhelper.so',
        'mono.posix.netstandard.dll'
    ),
    [System.StringComparer]::OrdinalIgnoreCase
)

$removedDebugArtifacts = 0
Get-ChildItem -LiteralPath $root -File -Recurse -Force -ErrorAction Stop | ForEach-Object {
    $name = $_.Name
    $remove = $false
    if ($namedDebugArtifacts.Contains($name)) {
        $remove = $true
    }
    elseif ($name.StartsWith('mscordaccore', [System.StringComparison]::OrdinalIgnoreCase)) {
        $remove = $true
    }
    elseif ($name.StartsWith('libmscordaccore', [System.StringComparison]::OrdinalIgnoreCase)) {
        # Unix builds prefix the debugger DAC with 'lib' (libmscordaccore.so / .dylib).
        $remove = $true
    }
    elseif ($name.StartsWith('Microsoft.DiaSymReader.Native', [System.StringComparison]::OrdinalIgnoreCase)) {
        $remove = $true
    }
    elseif ($name.EndsWith('.xml', [System.StringComparison]::OrdinalIgnoreCase)) {
        $siblingDll = [System.IO.Path]::ChangeExtension($_.FullName, '.dll')
        if (Test-Path -LiteralPath $siblingDll) {
            $remove = $true
        }
    }

    if ($remove) {
        Remove-Item -LiteralPath $_.FullName -Force
        $script:removedDebugArtifacts++
        Write-Host "Pruned debug artifact: $($_.FullName)"
    }
}

if ($RuntimeIdentifier -eq 'win-x64') {
    $nativeDirs = @('x86', 'arm64')
    foreach ($name in $nativeDirs) {
        $path = Join-Path $root $name
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
            Write-Host "Pruned native folder: $name"
        }
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

Write-Host "Shipping footprint prune complete. rid=$RuntimeIdentifier pdb=$($removedPdbs.Count) debug-artifacts=$removedDebugArtifacts satellites-kept=$kept satellites-removed=$removed root=$root"
