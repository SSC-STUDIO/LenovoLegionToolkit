[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BuildDir,

    [Parameter(Mandatory = $true)]
    [string]$EnglishBuildDir,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseOutput,

    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $PWD $Path))
}

function Compress-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)][string]$SourceDir,
        [Parameter(Mandatory = $true)][string]$DestinationPath
    )

    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Force
    }

    $items = Get-ChildItem -LiteralPath $SourceDir -Force
    if ($items.Count -eq 0) {
        throw "Cannot create '$DestinationPath' because '$SourceDir' is empty."
    }

    Compress-Archive -Path (Join-Path $SourceDir '*') -DestinationPath $DestinationPath -CompressionLevel Optimal
}

function Get-LanguageDirectories {
    param([Parameter(Mandatory = $true)][string]$BuildPath)

    Get-ChildItem -LiteralPath $BuildPath -Directory |
        Where-Object {
            Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.resources.dll' -ErrorAction SilentlyContinue |
                Select-Object -First 1
        } |
        Sort-Object Name
}

$buildPath = Resolve-RepoPath $BuildDir
$englishBuildPath = Resolve-RepoPath $EnglishBuildDir
$releaseOutputPath = Resolve-RepoPath $ReleaseOutput

if (-not (Test-Path -LiteralPath $buildPath)) {
    throw "Build output not found at '$buildPath'."
}

Remove-Item -LiteralPath $englishBuildPath, $releaseOutputPath -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $englishBuildPath, $releaseOutputPath -Force | Out-Null

Copy-Item -Path (Join-Path $buildPath '*') -Destination $englishBuildPath -Recurse -Force

$languageDirectories = @(Get-LanguageDirectories $buildPath)
foreach ($directory in $languageDirectories) {
    $target = Join-Path $englishBuildPath $directory.Name
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
}

$fullZip = Join-Path $releaseOutputPath "LenovoLegionToolkit_v${Version}_win-x64.zip"
$englishZip = Join-Path $releaseOutputPath "LenovoLegionToolkit_v${Version}_English_win-x64.zip"
Compress-DirectoryContents -SourceDir $buildPath -DestinationPath $fullZip
Compress-DirectoryContents -SourceDir $englishBuildPath -DestinationPath $englishZip

$languagePacks = @(
    @{ Asset = 'ar'; Directories = @('ar') },
    @{ Asset = 'bg'; Directories = @('bg') },
    @{ Asset = 'cs'; Directories = @('cs') },
    @{ Asset = 'de'; Directories = @('de') },
    @{ Asset = 'el'; Directories = @('el') },
    @{ Asset = 'es'; Directories = @('es') },
    @{ Asset = 'fr'; Directories = @('fr') },
    @{ Asset = 'hu'; Directories = @('hu') },
    @{ Asset = 'it'; Directories = @('it') },
    @{ Asset = 'ja'; Directories = @('ja') },
    @{ Asset = 'lv'; Directories = @('lv') },
    @{ Asset = 'nl-nl'; Directories = @('nl') },
    @{ Asset = 'pl'; Directories = @('pl') },
    @{ Asset = 'pt'; Directories = @('pt') },
    @{ Asset = 'pt-br'; Directories = @('pt-br') },
    @{ Asset = 'ro'; Directories = @('ro') },
    @{ Asset = 'ru'; Directories = @('ru') },
    @{ Asset = 'sk'; Directories = @('sk') },
    @{ Asset = 'tr'; Directories = @('tr') },
    @{ Asset = 'uk'; Directories = @('uk') },
    @{ Asset = 'vi'; Directories = @('vi') },
    @{ Asset = 'zh-hans'; Directories = @('zh', 'zh-Hans') },
    @{ Asset = 'zh-hant'; Directories = @('zh-hant') },
    @{ Asset = 'uz-latn-uz'; Directories = @('uz') }
)

$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "LenovoLegionToolkit-lang-assets-$([Guid]::NewGuid().ToString('N'))"
try {
    New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

    foreach ($pack in $languagePacks) {
        $assetName = [string]$pack.Asset
        $sourceDirectories = @($pack.Directories |
            ForEach-Object {
                $candidate = Join-Path $buildPath $_
                if (Test-Path -LiteralPath $candidate) {
                    $candidate
                }
            })

        if ($sourceDirectories.Count -eq 0) {
            Write-Warning "Skipping language pack '$assetName' because no matching resource directory was found."
            continue
        }

        $packStage = Join-Path $stagingRoot $assetName
        New-Item -ItemType Directory -Path $packStage -Force | Out-Null

        foreach ($sourceDirectory in $sourceDirectories) {
            Copy-Item -LiteralPath $sourceDirectory -Destination $packStage -Recurse -Force
        }

        $packZip = Join-Path $releaseOutputPath "LenovoLegionToolkit_v${Version}_lang_${assetName}.zip"
        Compress-DirectoryContents -SourceDir $packStage -DestinationPath $packZip
    }
}
finally {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Prepared full portable zip, English-only portable zip, and language pack assets in '$releaseOutputPath'."
Write-Host "Prepared English-only build output in '$englishBuildPath'."
