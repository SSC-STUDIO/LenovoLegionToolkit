[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$ProjectPath = 'UniversalDeviceToolkit.CrossPlatform/UniversalDeviceToolkit.CrossPlatform.csproj',

    [string]$PublishOutput = 'Build-CrossPlatformCli',

    [Parameter(Mandatory = $true)]
    [string]$ReleaseOutput,

    [string]$AssetPrefix = 'UniversalDeviceToolkit',

    [string]$HashFileName,

    [switch]$SkipHashUpdate
)

$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $PWD $Path))
}

function Get-Sha256Hash {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Cannot hash missing file '$Path'."
    }

    $resolvedPath = (Resolve-Path -LiteralPath $Path).ProviderPath
    $stream = [System.IO.File]::OpenRead($resolvedPath)
    try {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha256.ComputeHash($stream)
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    return -join ($bytes | ForEach-Object { $_.ToString('x2') })
}

function Compress-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Force
    }

    $items = @(Get-ChildItem -LiteralPath $SourceDir -Force)
    if ($items.Count -eq 0) {
        throw "Cannot create '$DestinationPath' because '$SourceDir' is empty."
    }

    Compress-Archive -Path (Join-Path $SourceDir '*') -DestinationPath $DestinationPath -CompressionLevel Optimal
}

function Add-HashLine {
    param(
        [Parameter(Mandatory = $true)][string]$HashPath,
        [Parameter(Mandatory = $true)][string]$AssetPath,
        [Parameter(Mandatory = $true)][string]$AssetName
    )

    $hashLine = "{0}  {1}" -f (Get-Sha256Hash -Path $AssetPath), $AssetName

    if (Test-Path -LiteralPath $HashPath) {
        $existingLines = [System.IO.File]::ReadAllLines($HashPath)
        $filteredLines = @($existingLines | Where-Object { $_ -notmatch "\s+$([regex]::Escape($AssetName))$" })
        $lines = @($filteredLines + $hashLine)
    }
    else {
        $lines = @($hashLine)
    }

    Set-Content -LiteralPath $HashPath -Value $lines -Encoding ASCII
}

$project = Resolve-RepoPath $ProjectPath
$publishOutputPath = Resolve-RepoPath $PublishOutput
$releaseOutputPath = Resolve-RepoPath $ReleaseOutput
$assetName = "${AssetPrefix}_v${Version}_CLI_cross-platform.zip"
$assetPath = Join-Path $releaseOutputPath $assetName
$resolvedHashFileName = if ([string]::IsNullOrWhiteSpace($HashFileName)) { "${AssetPrefix}_v${Version}_SHA256.txt" } else { $HashFileName }
$hashPath = Join-Path $releaseOutputPath $resolvedHashFileName

if (-not (Test-Path -LiteralPath $project)) {
    throw "Cross-platform CLI project not found at '$project'."
}

Remove-Item -LiteralPath $publishOutputPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishOutputPath, $releaseOutputPath -Force | Out-Null

dotnet publish $project --configuration Release --output $publishOutputPath /p:DebugType=None /p:FileVersion=$Version /p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    throw 'Cross-platform CLI publish failed.'
}

$requiredFiles = @('udt.dll', 'udt.deps.json', 'udt.runtimeconfig.json')
foreach ($fileName in $requiredFiles) {
    $filePath = Join-Path $publishOutputPath $fileName
    if (-not (Test-Path -LiteralPath $filePath)) {
        throw "Published cross-platform CLI is missing '$fileName'."
    }
}

Compress-DirectoryContents -SourceDir $publishOutputPath -DestinationPath $assetPath
if (-not $SkipHashUpdate) {
    Add-HashLine -HashPath $hashPath -AssetPath $assetPath -AssetName $assetName
}

Write-Host "Prepared cross-platform CLI asset '$assetPath'."
if ($SkipHashUpdate) {
    Write-Host 'Skipped SHA256 manifest update.'
}
else {
    Write-Host "Updated SHA256 manifest '$hashPath'."
}
