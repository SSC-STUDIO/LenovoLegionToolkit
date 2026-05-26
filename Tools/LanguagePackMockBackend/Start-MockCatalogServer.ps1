#Requires -Version 5.1
<#
.SYNOPSIS
  Serves a local language-pack catalog (catalog.json + culture zip) for offline / no-WAN testing.

.DESCRIPTION
  Separate from LanguagePackUi.Smoke — start this in one terminal, then run the app or smoke test
  with UDT_RESOURCE_CATALOG_URL=http://127.0.0.1:18765/catalog.json (or use port-forward scripts).

.PARAMETER Port
  TCP port to listen on (default 18765).

.PARAMETER Culture
  Culture folder inside the zip (default de).

.PARAMETER ServeRoot
  Folder containing catalog.json and {culture}.zip. Created on first run if missing.
#>
param(
    [int] $Port = 18765,
    [string] $Culture = "de",
    [string] $ServeRoot = ""
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $dir = $PSScriptRoot
    while ($dir) {
        if (Test-Path (Join-Path $dir "UniversalDeviceToolkit.sln")) { return $dir }
        $dir = Split-Path $dir -Parent
    }
    throw "Repository root not found."
}

function Get-RuntimeDirectory([string] $repoRoot) {
    foreach ($cfg in @("Debug", "Release")) {
        $root = Join-Path $repoRoot "UniversalDeviceToolkit.WPF\bin\$cfg"
        if (-not (Test-Path $root)) { continue }
        $exe = Get-ChildItem -Path $root -Filter "Universal Device Toolkit.exe" -Recurse -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending |
            Select-Object -First 1
        if ($exe) { return $exe.DirectoryName }
    }
    throw "Build WPF first: dotnet build UniversalDeviceToolkit.WPF -c Debug"
}

function Ensure-WpfBuilt([string] $repoRoot, [string] $runtimeDir, [string] $culture) {
    $satellite = Join-Path $runtimeDir "$culture\Universal Device Toolkit.resources.dll"
    if (Test-Path $satellite) { return $satellite }
    Write-Host "[mock-catalog] Building WPF (satellite missing)..."
    $proj = Join-Path $repoRoot "UniversalDeviceToolkit.WPF\UniversalDeviceToolkit.WPF.csproj"
    dotnet build $proj -c Debug | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "WPF build failed." }
    $runtimeDir = Get-RuntimeDirectory $repoRoot
    $satellite = Join-Path $runtimeDir "$culture\Universal Device Toolkit.resources.dll"
    if (-not (Test-Path $satellite)) { throw "Satellite not found: $satellite" }
    return $satellite
}

function Get-AppVersion([string] $runtimeDir) {
    $exe = Join-Path $runtimeDir "Universal Device Toolkit.exe"
    if (Test-Path $exe) {
        $v = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exe)
        return "$($v.FileMajorPart).$($v.FileMinorPart).$($v.FileBuildPart)"
    }
    return "3.8.1"
}

function Write-CatalogArtifacts {
    param(
        [string] $ServeRoot,
        [string] $BaseUrl,
        [string] $Culture,
        [byte[]] $ResourceDllBytes,
        [string] $AppVersion
    )
    $zipPath = Join-Path $ServeRoot "$Culture.zip"
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $tempZip = [System.IO.Path]::GetTempFileName()
    try {
        if (Test-Path $tempZip) { Remove-Item $tempZip -Force }
        $fs = [System.IO.File]::Open($tempZip, [System.IO.FileMode]::Create)
        try {
            $zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create)
            $entry = $zip.CreateEntry("$Culture/Universal Device Toolkit.resources.dll")
            $es = $entry.Open()
            try { $es.Write($ResourceDllBytes, 0, $ResourceDllBytes.Length) }
            finally { $es.Dispose() }
            $zip.Dispose()
        }
        finally { $fs.Dispose() }
        Move-Item -Force $tempZip $zipPath
    }
    catch {
        if (Test-Path $tempZip) { Remove-Item $tempZip -Force -ErrorAction SilentlyContinue }
        throw
    }

    $zipBytes = [System.IO.File]::ReadAllBytes($zipPath)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $sha256 = [BitConverter]::ToString($sha.ComputeHash($zipBytes)).Replace("-", "").ToLowerInvariant()
    }
    finally {
        $sha.Dispose()
    }
    $languageUrl = "$BaseUrl/$Culture.zip"

    $catalog = @{
        schemaVersion = 1
        appVersion    = $AppVersion
        generatedAt   = (Get-Date).ToUniversalTime().ToString("o")
        productName   = "Universal Device Toolkit"
        downloads     = @{
            online = @{
                portable = @{
                    name   = "UniversalDeviceToolkit_v${AppVersion}_Online_win-x64.zip"
                    url    = $languageUrl
                    size   = $zipBytes.Length
                    sha256 = $sha256
                }
            }
        }
        languages     = @(
            @{
                culture     = if ($Culture -eq "de") { "de" } else { $Culture }
                displayName = if ($Culture -eq "de") { "German" } else { $Culture }
                url         = $languageUrl
                sha256      = $sha256
                size        = $zipBytes.Length
            }
        )
    }

    $catalogPath = Join-Path $ServeRoot "catalog.json"
    $catalog | ConvertTo-Json -Depth 6 | Set-Content -Path $catalogPath -Encoding UTF8
    Write-Host "[mock-catalog] Wrote $catalogPath and $zipPath ($($zipBytes.Length) bytes zip, $($ResourceDllBytes.Length) bytes dll)"
}

$repoRoot = Get-RepoRoot
$runtimeDir = Get-RuntimeDirectory $repoRoot
$satellitePath = Ensure-WpfBuilt $repoRoot $runtimeDir $Culture
$resourceBytes = [System.IO.File]::ReadAllBytes($satellitePath)
$appVersion = Get-AppVersion $runtimeDir

if ([string]::IsNullOrWhiteSpace($ServeRoot)) {
    $ServeRoot = Join-Path $PSScriptRoot "_serve"
}
New-Item -ItemType Directory -Force -Path $ServeRoot | Out-Null

$baseUrl = "http://127.0.0.1:$Port"
Write-CatalogArtifacts -ServeRoot $ServeRoot -BaseUrl $baseUrl -Culture $Culture -ResourceDllBytes $resourceBytes -AppVersion $appVersion

$prefix = "http://127.0.0.1:$Port/"
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add($prefix)
$listener.Start()
Write-Host "[mock-catalog] Listening on $prefix"
Write-Host "[mock-catalog] Catalog URL: ${baseUrl}/catalog.json"
Write-Host "[mock-catalog] Set for app/smoke: `$env:UDT_RESOURCE_CATALOG_URL='${baseUrl}/catalog.json'"
Write-Host "[mock-catalog] Press Ctrl+C to stop."

while ($listener.IsListening) {
    $context = $listener.GetContext()
    try {
        $rel = $context.Request.Url.LocalPath.TrimStart("/").Replace("/", [IO.Path]::DirectorySeparatorChar)
        if ([string]::IsNullOrWhiteSpace($rel)) { $rel = "catalog.json" }
        $filePath = Join-Path $ServeRoot $rel
        if (-not (Test-Path $filePath)) {
            $context.Response.StatusCode = 404
            $context.Response.Close()
            continue
        }
        $bytes = [System.IO.File]::ReadAllBytes($filePath)
        if ($filePath.EndsWith(".json")) { $context.Response.ContentType = "application/json" }
        else { $context.Response.ContentType = "application/zip" }
        $context.Response.ContentLength64 = $bytes.Length
        $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        $context.Response.Close()
    }
    catch {
        try { $context.Response.Abort() } catch { }
        Write-Warning $_
    }
}
