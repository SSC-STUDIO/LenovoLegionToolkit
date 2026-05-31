[CmdletBinding()]
param(
    [string]$BuildDir,

    [Alias('EnglishBuildDir')]
    [string]$OnlineBuildDir,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseOutput,

    [Parameter(Mandatory = $true)]
    [string]$PagesOutput,

    [Parameter(Mandatory = $true)]
    [string]$Version,

    [switch]$FinalizeOnly,

    [string]$FullInstallerPath,

    [string]$OnlineInstallerPath,

    [string]$PublicAssetPrefix = 'UniversalDeviceToolkit',

    [string]$LegacyAssetPrefix = 'LenovoLegionToolkit',

    [string]$ProductName = 'Universal Device Toolkit',

    [string]$Repository = 'SSC-STUDIO/UniversalDeviceToolkit',

    [string]$ResourcesBaseUrl = 'https://ssc-studio.github.io/UniversalDeviceToolkit/resources'
)

$ErrorActionPreference = 'Stop'

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $PWD $Path))
}

function ConvertTo-UrlPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    return $Path.Replace('\', '/')
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

function Get-FullSetupAssetName { param([string]$AssetVersion) "${PublicAssetPrefix}_v${AssetVersion}_Full_Setup.exe" }
function Get-OnlineSetupAssetName { param([string]$AssetVersion) "${PublicAssetPrefix}_v${AssetVersion}_Online_Setup.exe" }
function Get-FullZipAssetName { param([string]$AssetVersion) "${PublicAssetPrefix}_v${AssetVersion}_Full_win-x64.zip" }
function Get-OnlineZipAssetName { param([string]$AssetVersion) "${PublicAssetPrefix}_v${AssetVersion}_Online_win-x64.zip" }
function Get-CrossPlatformCliAssetName { param([string]$AssetVersion) "${PublicAssetPrefix}_v${AssetVersion}_CLI_cross-platform.zip" }
function Get-HashAssetName { param([string]$AssetVersion) "${PublicAssetPrefix}_v${AssetVersion}_SHA256.txt" }
function Get-LegacySetupAssetName { param([string]$AssetVersion) "${LegacyAssetPrefix}_v${AssetVersion}_Setup.exe" }
function Get-LanguageAssetName { param([string]$AssetVersion, [string]$Culture) "$Culture.zip" }
function Get-DeviceAssetName { param([string]$PackId) "$PackId.zip" }

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

function Get-LanguageDirectories {
    param([Parameter(Mandatory = $true)][string]$BuildPath)

    Get-ChildItem -LiteralPath $BuildPath -Directory |
        Where-Object {
            Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.resources.dll' -ErrorAction SilentlyContinue |
                Select-Object -First 1
        } |
        Sort-Object Name
}

function New-FileMetadata {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$CatalogPath,
        [string]$Url
    )

    if (-not (Test-Path -LiteralPath $FilePath)) {
        throw "Expected asset not found at '$FilePath'."
    }

    $file = Get-Item -LiteralPath $FilePath
    $metadata = [ordered]@{
        name = $Name
        path = ConvertTo-UrlPath $CatalogPath
        size = $file.Length
        sha256 = Get-Sha256Hash -Path $file.FullName
    }

    if (-not [string]::IsNullOrWhiteSpace($Url)) {
        $metadata['url'] = $Url
    }

    return $metadata
}

function Get-LanguageDisplayName {
    param([Parameter(Mandatory = $true)][string]$Culture)

    try {
        return ([System.Globalization.CultureInfo]::GetCultureInfo($Culture)).EnglishName
    }
    catch {
        return $Culture
    }
}

function Write-JsonFile {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Path
    )

    $directory = Split-Path -Parent $Path
    if (-not [string]::IsNullOrWhiteSpace($directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Write-HashFile {
    param(
        [Parameter(Mandatory = $true)][string[]]$AssetNames,
        [Parameter(Mandatory = $true)][string]$ReleaseOutputPath,
        [Parameter(Mandatory = $true)][string]$HashFileName
    )

    $lines = foreach ($assetName in $AssetNames) {
        $assetPath = Join-Path $ReleaseOutputPath $assetName
        if (-not (Test-Path -LiteralPath $assetPath)) {
            throw "Cannot write '$HashFileName' because '$assetName' is missing."
        }

        "{0}  {1}" -f (Get-Sha256Hash -Path $assetPath), $assetName
    }

    Set-Content -LiteralPath (Join-Path $ReleaseOutputPath $HashFileName) -Value $lines -Encoding ASCII
}

function Get-LanguagePackDefinitions {
    @(
        @{ Culture = 'ar'; Directories = @('ar') },
        @{ Culture = 'bg'; Directories = @('bg') },
        @{ Culture = 'bs'; Directories = @('bs') },
        @{ Culture = 'ca'; Directories = @('ca') },
        @{ Culture = 'cs'; Directories = @('cs') },
        @{ Culture = 'de'; Directories = @('de') },
        @{ Culture = 'el'; Directories = @('el') },
        @{ Culture = 'es'; Directories = @('es') },
        @{ Culture = 'fr'; Directories = @('fr') },
        @{ Culture = 'hu'; Directories = @('hu') },
        @{ Culture = 'it'; Directories = @('it') },
        @{ Culture = 'ja'; Directories = @('ja') },
        @{ Culture = 'ko'; Directories = @('ko') },
        @{ Culture = 'lv'; Directories = @('lv') },
        @{ Culture = 'nl-nl'; Directories = @('nl', 'nl-nl', 'nl-NL') },
        @{ Culture = 'no'; Directories = @('no') },
        @{ Culture = 'pl'; Directories = @('pl') },
        @{ Culture = 'pt'; Directories = @('pt') },
        @{ Culture = 'pt-br'; Directories = @('pt-br', 'pt-BR') },
        @{ Culture = 'ro'; Directories = @('ro') },
        @{ Culture = 'ru'; Directories = @('ru') },
        @{ Culture = 'sk'; Directories = @('sk') },
        @{ Culture = 'tr'; Directories = @('tr') },
        @{ Culture = 'uk'; Directories = @('uk') },
        @{ Culture = 'vi'; Directories = @('vi') },
        @{ Culture = 'zh-hans'; Directories = @('zh', 'zh-hans', 'zh-Hans') },
        @{ Culture = 'zh-hant'; Directories = @('zh-hant', 'zh-Hant') },
        @{ Culture = 'uz-latn-uz'; Directories = @('uz', 'uz-latn-uz', 'uz-Latn-UZ') }
    )
}

function Get-ListValue {
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    return @($Value)
}

function Get-DevicePackDefinitions {
    $lenovoHardwareEnabledFeatures = @(
        'lenovo-hardware-controls',
        'sensors',
        'power-modes',
        'battery',
        'plugins',
        'system-optimization'
    )

    $universalBasicEnabledFeatures = @(
        'plugins',
        'system-optimization',
        'language',
        'theme',
        'updates',
        'logs'
    )

    $universalBasicHiddenFeatures = @(
        'lenovo-hardware-controls',
        'power-modes',
        'keyboard-backlight',
        'god-mode',
        'gpu-overclock',
        'fan-curve'
    )

    function New-DevicePackDefinition {
        param(
            [Parameter(Mandatory = $true)][string]$Id,
            [Parameter(Mandatory = $true)][string]$DisplayName,
            [Parameter(Mandatory = $true)][string]$Vendor,
            [string[]]$VendorAliases = @(),
            [string[]]$Families = @(),
            [string[]]$ModelPrefixes = @(),
            [string[]]$MachineTypes = @(),
            [string[]]$ModelKeywords = @(),
            [string[]]$EnabledFeatures = @(),
            [string[]]$HiddenFeatures = @()
        )

        [ordered]@{
            Id = $Id
            DisplayName = $DisplayName
            Vendor = $Vendor
            VendorAliases = $VendorAliases
            Families = $Families
            ModelPrefixes = $ModelPrefixes
            MachineTypes = $MachineTypes
            ModelKeywords = $ModelKeywords
            EnabledFeatures = $EnabledFeatures
            HiddenFeatures = $HiddenFeatures
        }
    }

    function New-LenovoHardwarePackDefinition {
        param(
            [Parameter(Mandatory = $true)][string]$Id,
            [Parameter(Mandatory = $true)][string]$DisplayName,
            [string[]]$Families = @(),
            [string[]]$ModelPrefixes = @(),
            [string[]]$MachineTypes = @(),
            [string[]]$ModelKeywords = @()
        )

        New-DevicePackDefinition `
            -Id $Id `
            -DisplayName $DisplayName `
            -Vendor 'LENOVO' `
            -Families $Families `
            -ModelPrefixes $ModelPrefixes `
            -MachineTypes $MachineTypes `
            -ModelKeywords $ModelKeywords `
            -EnabledFeatures $lenovoHardwareEnabledFeatures `
            -HiddenFeatures @()
    }

    function New-BasicPackDefinition {
        param(
            [Parameter(Mandatory = $true)][string]$Id,
            [Parameter(Mandatory = $true)][string]$DisplayName,
            [Parameter(Mandatory = $true)][string]$Vendor,
            [string[]]$VendorAliases = @(),
            [string[]]$Families = @(),
            [string[]]$ModelPrefixes = @(),
            [string[]]$MachineTypes = @(),
            [string[]]$ModelKeywords = @()
        )

        New-DevicePackDefinition `
            -Id $Id `
            -DisplayName $DisplayName `
            -Vendor $Vendor `
            -VendorAliases $VendorAliases `
            -Families $Families `
            -ModelPrefixes $ModelPrefixes `
            -MachineTypes $MachineTypes `
            -ModelKeywords $ModelKeywords `
            -EnabledFeatures $universalBasicEnabledFeatures `
            -HiddenFeatures $universalBasicHiddenFeatures
    }

    function New-LenovoBasicPackDefinition {
        param(
            [Parameter(Mandatory = $true)][string]$Id,
            [Parameter(Mandatory = $true)][string]$DisplayName,
            [string[]]$Families = @(),
            [string[]]$ModelPrefixes = @(),
            [string[]]$MachineTypes = @(),
            [string[]]$ModelKeywords = @()
        )

        New-BasicPackDefinition `
            -Id $Id `
            -DisplayName $DisplayName `
            -Vendor 'LENOVO' `
            -Families $Families `
            -ModelPrefixes $ModelPrefixes `
            -MachineTypes $MachineTypes `
            -ModelKeywords $ModelKeywords
    }

    @(
        New-LenovoHardwarePackDefinition -Id 'lenovo-legion-5' -DisplayName 'Lenovo Legion 5' -Families @('Legion') -ModelPrefixes @('15ACH', '15AKP', '15AHP', '15APH', '15ARH', '15ARP', '15IAH', '15IAX', '15IHU', '15IMH', '15IRH', '15IRX', '15ITH', '16ACH', '16ADR', '16AFR', '16AHP', '16APH', '16ARH', '16ARP', '16ARX', '16IAH', '16IAX', '16IRH', '16IRX', '16ITH', '17ACH', '17ARH', '17IRX', '17ITH', '17IMH') -MachineTypes @('83F0', '83F1', '83M0', '83NX', '83N2', '83LY', '83DG', '83EW', '83EG', '83JJ', '82RC', '82RB', '82TB', '83EF', '82RE', '82RD', '82AX', '82B0', '82GR') -ModelKeywords @('Legion 5', 'Y7000', 'Y7000P', 'Y7000P 2020H', 'Y7000P2020H', 'R7000', 'R7000P')
        New-LenovoHardwarePackDefinition -Id 'lenovo-legion-slim-5' -DisplayName 'Lenovo Legion Slim 5' -Families @('Legion', 'Lenovo Slim') -ModelPrefixes @('14AHP', '14APH', '14AKP', '14IRP') -MachineTypes @('83DH', '83EX', '82Y5', '82Y9', '82YA', '83D6') -ModelKeywords @('Legion Slim 5')
        New-LenovoHardwarePackDefinition -Id 'lenovo-legion-pro-5' -DisplayName 'Lenovo Legion Pro 5' -Families @('Legion') -ModelPrefixes @('16IAX', '16IRX', '16ARX') -MachineTypes @('83LT', '83F3', '83DF', '83F2', '83LU', '82WM', '83NN', '82WK', '82JQ') -ModelKeywords @('Legion Pro 5', 'Y9000P', 'R9000P')
        New-LenovoHardwarePackDefinition -Id 'lenovo-legion-7' -DisplayName 'Lenovo Legion 7' -Families @('Legion') -ModelPrefixes @('16ACH', '16ARH', '16IAH', '16IAX', '16IRH') -MachineTypes @('83KY', '83FD', '82UH', '82TD', '82N6') -ModelKeywords @('Legion 7')
        New-LenovoHardwarePackDefinition -Id 'lenovo-legion-pro-7' -DisplayName 'Lenovo Legion Pro 7' -Families @('Legion') -ModelPrefixes @('16IAX', '16IRX', '16ARX') -MachineTypes @('83RU', '83F5', '83DE', '82WR', '82WQ', '82WS') -ModelKeywords @('Legion Pro 7', 'Y9000P', 'R9000P')
        New-LenovoHardwarePackDefinition -Id 'lenovo-legion-9' -DisplayName 'Lenovo Legion 9' -Families @('Legion') -ModelPrefixes @('16IRX', '16IAX', '18IAX') -MachineTypes @('83G0', '83EY') -ModelKeywords @('Legion 9')
        New-LenovoHardwarePackDefinition -Id 'lenovo-legion-go' -DisplayName 'Lenovo Legion Go' -Families @('Legion') -ModelPrefixes @('NX') -MachineTypes @('83E1') -ModelKeywords @('Legion Go')
        New-LenovoHardwarePackDefinition -Id 'lenovo-loq' -DisplayName 'Lenovo LOQ' -Families @('LOQ') -ModelPrefixes @('15IAX', '15IRH', '15IRX', '15ARP', '15APH', '16IRH', '16IAX', '16APH', '17IRX', '17IAX') -ModelKeywords @('LOQ', 'G5000')
        New-LenovoHardwarePackDefinition -Id 'lenovo-ideapad' -DisplayName 'Lenovo IdeaPad' -Families @('IdeaPad', 'IdeaPad Gaming') -ModelKeywords @('IdeaPad Gaming', 'IdeaPad')
        New-LenovoHardwarePackDefinition -Id 'lenovo-thinkbook' -DisplayName 'Lenovo ThinkBook' -Families @('ThinkBook') -ModelPrefixes @('ThinkBook') -ModelKeywords @('ThinkBook')
        New-LenovoHardwarePackDefinition -Id 'lenovo-yoga' -DisplayName 'Lenovo YOGA' -Families @('YOGA') -ModelKeywords @('YOGA', 'Yoga')
        New-LenovoBasicPackDefinition -Id 'lenovo-legion-desktop-basic' -DisplayName 'Lenovo Legion Desktop Basic' -Families @('Legion') -ModelKeywords @('Legion T', 'Legion Tower', 'Legion C', 'Legion Desktop')
        New-LenovoHardwarePackDefinition -Id 'lenovo-legacy-limited' -DisplayName 'Lenovo Legacy Limited' -Families @('Legion') -ModelPrefixes @('18IAX', '17IR', '15IR', '15IC', '15IK', 'G5000', 'R9000', 'R7000', 'Y9000', 'Y7000') -ModelKeywords @('Legion')
        New-LenovoBasicPackDefinition -Id 'lenovo-thinkpad-basic' -DisplayName 'Lenovo ThinkPad Basic' -Families @('ThinkPad') -ModelKeywords @('ThinkPad', 'ThinkPad P', 'ThinkPad T', 'ThinkPad X', 'ThinkPad E', 'ThinkPad L', 'ThinkPad Z')
        New-LenovoBasicPackDefinition -Id 'lenovo-thinkcentre-basic' -DisplayName 'Lenovo ThinkCentre Basic' -Families @('ThinkCentre') -ModelKeywords @('ThinkCentre', 'ThinkCentre M', 'ThinkCentre Neo')
        New-LenovoBasicPackDefinition -Id 'lenovo-thinkstation-basic' -DisplayName 'Lenovo ThinkStation Basic' -Families @('ThinkStation') -ModelKeywords @('ThinkStation', 'ThinkStation P')
        New-LenovoBasicPackDefinition -Id 'lenovo-ideacentre-basic' -DisplayName 'Lenovo IdeaCentre Basic' -Families @('IdeaCentre') -ModelKeywords @('IdeaCentre', 'Yoga AIO')
        New-LenovoBasicPackDefinition -Id 'lenovo-xiaoxin-basic' -DisplayName 'Lenovo XiaoXin Basic' -Families @('XiaoXin') -ModelKeywords @('XiaoXin', 'Xiaoxin', '小新')
        New-LenovoBasicPackDefinition -Id 'lenovo-y-series-legacy' -DisplayName 'Lenovo Y Series Legacy' -Families @('IdeaPad', 'Legion') -ModelPrefixes @('Y520', 'Y530', 'Y540', 'Y545', 'Y700', 'Y7000', 'Y9000') -ModelKeywords @('Lenovo Y', 'Y Series')
        New-LenovoBasicPackDefinition -Id 'lenovo-v-series-basic' -DisplayName 'Lenovo V Series Basic' -Families @('Lenovo V') -ModelPrefixes @('V14', 'V15', 'V17') -ModelKeywords @('Lenovo V', 'V14', 'V15', 'V17')
        New-LenovoBasicPackDefinition -Id 'lenovo-slim-basic' -DisplayName 'Lenovo Slim Basic' -Families @('Lenovo Slim', 'IdeaPad Slim') -ModelKeywords @('Lenovo Slim', 'IdeaPad Slim', 'Slim 7', 'Slim 5')
        New-BasicPackDefinition -Id 'motorola-lenovo-basic' -DisplayName 'Motorola Lenovo Basic' -Vendor 'MOTOROLA' -Families @('Motorola') -ModelKeywords @('Legion', 'ThinkPhone', 'Moto')
        New-BasicPackDefinition -Id 'asus-basic' -DisplayName 'ASUS Basic' -Vendor 'ASUSTeK COMPUTER INC.' -VendorAliases @('ASUS', 'ASUSTeK COMPUTER INC') -Families @('ASUS', 'ROG', 'TUF', 'ProArt') -ModelKeywords @('ROG', 'TUF', 'Zephyrus', 'Strix', 'VivoBook', 'Zenbook', 'ProArt', 'ExpertBook')
        New-BasicPackDefinition -Id 'dell-basic' -DisplayName 'Dell Basic' -Vendor 'Dell Inc.' -VendorAliases @('Dell', 'Dell Computer Corporation') -Families @('Dell', 'Alienware', 'Inspiron', 'XPS', 'Precision', 'G Series') -ModelKeywords @('Alienware', 'XPS', 'Inspiron', 'Precision', 'Latitude', 'Dell G', 'OptiPlex', 'Vostro')
        New-BasicPackDefinition -Id 'hp-basic' -DisplayName 'HP Basic' -Vendor 'HP' -VendorAliases @('HP Inc.', 'Hewlett-Packard', 'Hewlett-Packard Company') -Families @('HP', 'OMEN', 'Victus', 'Pavilion', 'Envy', 'EliteBook', 'ProBook') -ModelKeywords @('OMEN', 'Victus', 'Pavilion', 'Envy', 'EliteBook', 'ProBook', 'ZBook', 'Spectre')
        New-BasicPackDefinition -Id 'acer-basic' -DisplayName 'Acer Basic' -Vendor 'Acer' -VendorAliases @('Acer Incorporated', 'Acer Inc.') -Families @('Acer', 'Predator', 'Nitro', 'Swift', 'Aspire') -ModelKeywords @('Predator', 'Nitro', 'Swift', 'Aspire', 'TravelMate', 'ConceptD')
        New-BasicPackDefinition -Id 'msi-basic' -DisplayName 'MSI Basic' -Vendor 'Micro-Star International Co., Ltd.' -VendorAliases @('MSI', 'Micro-Star International', 'MICRO-STAR INTERNATIONAL CO., LTD') -Families @('MSI') -ModelKeywords @('MSI', 'Raider', 'Stealth', 'Vector', 'Katana', 'Cyborg', 'Creator', 'Prestige', 'Modern', 'Summit')
        New-BasicPackDefinition -Id 'microsoft-surface-basic' -DisplayName 'Microsoft Surface Basic' -Vendor 'Microsoft Corporation' -VendorAliases @('Microsoft') -Families @('Surface') -ModelKeywords @('Surface Laptop', 'Surface Pro', 'Surface Book', 'Surface Studio', 'Surface Go')
        New-BasicPackDefinition -Id 'gigabyte-basic' -DisplayName 'GIGABYTE Basic' -Vendor 'GIGABYTE' -VendorAliases @('Gigabyte Technology Co., Ltd.', 'Gigabyte Technology Co., Ltd') -Families @('GIGABYTE', 'AORUS', 'AERO') -ModelKeywords @('AORUS', 'AERO', 'GIGABYTE G')
        New-BasicPackDefinition -Id 'razer-basic' -DisplayName 'Razer Basic' -Vendor 'Razer' -VendorAliases @('Razer Inc.', 'Razer Inc') -Families @('Razer Blade') -ModelKeywords @('Blade', 'Razer Book')
        New-BasicPackDefinition -Id 'samsung-basic' -DisplayName 'Samsung Basic' -Vendor 'SAMSUNG ELECTRONICS CO., LTD.' -VendorAliases @('Samsung', 'Samsung Electronics', 'SAMSUNG ELECTRONICS CO., LTD') -Families @('Samsung', 'Galaxy Book') -ModelKeywords @('Galaxy Book', 'Notebook 9')
        New-BasicPackDefinition -Id 'huawei-basic' -DisplayName 'HUAWEI Basic' -Vendor 'HUAWEI' -VendorAliases @('Huawei Technologies Co., Ltd.', 'Huawei Technologies Co., Ltd') -Families @('HUAWEI', 'MateBook') -ModelKeywords @('MateBook')
        New-BasicPackDefinition -Id 'xiaomi-basic' -DisplayName 'Xiaomi Basic' -Vendor 'Xiaomi' -VendorAliases @('Xiaomi Inc.', 'Xiaomi Corporation', 'Redmi') -Families @('Xiaomi', 'RedmiBook') -ModelKeywords @('Mi Notebook', 'RedmiBook', 'Xiaomi Book')
        New-BasicPackDefinition -Id 'honor-basic' -DisplayName 'HONOR Basic' -Vendor 'HONOR' -VendorAliases @('Honor Device Co., Ltd.', 'Honor Device Co., Ltd') -Families @('HONOR', 'MagicBook') -ModelKeywords @('MagicBook')
        New-BasicPackDefinition -Id 'lg-basic' -DisplayName 'LG Basic' -Vendor 'LG Electronics' -VendorAliases @('LG Electronics Inc.', 'LG Electronics Inc', 'LG') -Families @('LG gram', 'LG') -ModelKeywords @('gram', 'UltraPC')
        New-BasicPackDefinition -Id 'framework-basic' -DisplayName 'Framework Basic' -Vendor 'Framework' -VendorAliases @('Framework Computer Inc.', 'Framework Computer') -Families @('Framework Laptop') -ModelKeywords @('Framework Laptop')
        New-BasicPackDefinition -Id 'panasonic-basic' -DisplayName 'Panasonic Basic' -Vendor 'Panasonic' -VendorAliases @('Panasonic Corporation') -Families @('Panasonic', 'TOUGHBOOK', 'Lets note') -ModelKeywords @('TOUGHBOOK', 'Let''s note', 'Lets note')
        New-BasicPackDefinition -Id 'dynabook-basic' -DisplayName 'Dynabook Basic' -Vendor 'Dynabook Inc.' -VendorAliases @('Dynabook', 'TOSHIBA', 'TOSHIBA CORPORATION') -Families @('Dynabook', 'Toshiba') -ModelKeywords @('Portégé', 'Portege', 'Tecra', 'Satellite')
        New-BasicPackDefinition -Id 'fujitsu-basic' -DisplayName 'Fujitsu Basic' -Vendor 'FUJITSU' -VendorAliases @('FUJITSU CLIENT COMPUTING LIMITED', 'Fujitsu Client Computing Limited') -Families @('Fujitsu', 'LIFEBOOK') -ModelKeywords @('LIFEBOOK', 'CELSIUS')
        New-BasicPackDefinition -Id 'vaio-basic' -DisplayName 'VAIO Basic' -Vendor 'VAIO Corporation' -VendorAliases @('VAIO') -Families @('VAIO') -ModelKeywords @('VAIO')
        New-BasicPackDefinition -Id 'medion-basic' -DisplayName 'MEDION Basic' -Vendor 'MEDION' -VendorAliases @('MEDION AG') -Families @('MEDION', 'ERAZER') -ModelKeywords @('ERAZER', 'AKOYA')
        New-BasicPackDefinition -Id 'xmg-schenker-basic' -DisplayName 'XMG/SCHENKER Basic' -Vendor 'SCHENKER' -VendorAliases @('Schenker Technologies GmbH', 'XMG', 'TUXEDO') -Families @('XMG', 'SCHENKER', 'TUXEDO') -ModelKeywords @('XMG', 'SCHENKER', 'TUXEDO')
        New-BasicPackDefinition -Id 'clevo-tongfang-basic' -DisplayName 'Clevo/Tongfang Basic' -Vendor 'CLEVO' -VendorAliases @('Notebook', 'Tongfang', 'Eluktronics', 'MECHREVO', 'THUNDEROBOT', 'Hasee', 'SAGER') -Families @('Clevo', 'Tongfang', 'Barebone') -ModelKeywords @('MECHREVO', 'THUNDEROBOT', 'Hasee', 'SAGER', 'Eluktronics', 'Maingear')
        New-BasicPackDefinition -Id 'generic-pc-basic' -DisplayName 'Generic PC Basic' -Vendor '*' -Families @('Generic PC')
    )
}

function Write-StableCatalog {
    param(
        [Parameter(Mandatory = $true)][string]$ReleaseOutputPath,
        [Parameter(Mandatory = $true)][string]$PagesOutputPath
    )

    $resourcesRoot = Join-Path $PagesOutputPath "resources\$Version"
    $languagesCatalogPath = Join-Path $resourcesRoot 'languages\catalog.json'
    $devicesCatalogPath = Join-Path $resourcesRoot 'devices\catalog.json'
    $stableCatalogPath = Join-Path $PagesOutputPath 'stable\catalog.json'
    $resourcesStableCatalogPath = Join-Path $PagesOutputPath 'resources\stable\catalog.json'
    $releaseBaseUrl = "https://github.com/$Repository/releases/download/v$Version"

    $fullSetupName = Get-FullSetupAssetName $Version
    $onlineSetupName = Get-OnlineSetupAssetName $Version
    $fullZipName = Get-FullZipAssetName $Version
    $onlineZipName = Get-OnlineZipAssetName $Version
    $crossPlatformCliName = Get-CrossPlatformCliAssetName $Version
    $hashName = Get-HashAssetName $Version
    $legacySetupName = Get-LegacySetupAssetName $Version

    $downloads = [ordered]@{}

    if (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $fullZipName)) {
        $downloads['full'] = [ordered]@{
            portable = New-FileMetadata `
                -FilePath (Join-Path $ReleaseOutputPath $fullZipName) `
                -Name $fullZipName `
                -CatalogPath "releases/v$Version/$fullZipName" `
                -Url "$releaseBaseUrl/$fullZipName"
        }
    }

    if (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $onlineZipName)) {
        $downloads['online'] = [ordered]@{
            portable = New-FileMetadata `
                -FilePath (Join-Path $ReleaseOutputPath $onlineZipName) `
                -Name $onlineZipName `
                -CatalogPath "releases/v$Version/$onlineZipName" `
                -Url "$releaseBaseUrl/$onlineZipName"
        }
    }

    if (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $fullSetupName)) {
        if (-not $downloads.Contains('full')) {
            $downloads['full'] = [ordered]@{}
        }

        $downloads['full']['installer'] = New-FileMetadata `
            -FilePath (Join-Path $ReleaseOutputPath $fullSetupName) `
            -Name $fullSetupName `
            -CatalogPath "releases/v$Version/$fullSetupName" `
            -Url "$releaseBaseUrl/$fullSetupName"
    }

    if (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $onlineSetupName)) {
        if (-not $downloads.Contains('online')) {
            $downloads['online'] = [ordered]@{}
        }

        $downloads['online']['installer'] = New-FileMetadata `
            -FilePath (Join-Path $ReleaseOutputPath $onlineSetupName) `
            -Name $onlineSetupName `
            -CatalogPath "releases/v$Version/$onlineSetupName" `
            -Url "$releaseBaseUrl/$onlineSetupName"
    }

    if (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $crossPlatformCliName)) {
        $downloads['cli'] = [ordered]@{
            crossPlatform = New-FileMetadata `
                -FilePath (Join-Path $ReleaseOutputPath $crossPlatformCliName) `
                -Name $crossPlatformCliName `
                -CatalogPath "releases/v$Version/$crossPlatformCliName" `
                -Url "$releaseBaseUrl/$crossPlatformCliName"
        }
    }

    $legacyAliases = @()
    if (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $legacySetupName)) {
        $legacyAlias = New-FileMetadata `
            -FilePath (Join-Path $ReleaseOutputPath $legacySetupName) `
            -Name $legacySetupName `
            -CatalogPath "releases/v$Version/$legacySetupName" `
            -Url "$releaseBaseUrl/$legacySetupName"
        $legacyAlias['target'] = $fullSetupName
        $legacyAliases += $legacyAlias
    }

    $languages = @()
    if (Test-Path -LiteralPath $languagesCatalogPath) {
        $languageCatalog = Get-Content -LiteralPath $languagesCatalogPath -Raw | ConvertFrom-Json
        $languages = @($languageCatalog.languages | ForEach-Object {
            [ordered]@{
                culture = $_.culture
                displayName = $_.displayName
                url = $_.url
                sha256 = $_.sha256
                size = $_.size
            }
        })
    }

    $devicePacks = @()
    if (Test-Path -LiteralPath $devicesCatalogPath) {
        $devicesCatalog = Get-Content -LiteralPath $devicesCatalogPath -Raw | ConvertFrom-Json
        $devicePacks = @($devicesCatalog.devicePacks | ForEach-Object {
            [ordered]@{
                id = $_.id
                displayName = $_.displayName
                vendor = $_.vendor
                families = @($_.families)
                modelPrefixes = @($_.modelPrefixes)
                machineTypes = @($_.machineTypes)
                modelKeywords = @($_.modelKeywords)
                url = $_.url
                sha256 = $_.sha256
                size = $_.size
            }
        })
    }

    $sha256 = $null
    if (Test-Path -LiteralPath (Join-Path $ReleaseOutputPath $hashName)) {
        $sha256 = New-FileMetadata `
            -FilePath (Join-Path $ReleaseOutputPath $hashName) `
            -Name $hashName `
            -CatalogPath "releases/v$Version/$hashName" `
            -Url "$releaseBaseUrl/$hashName"
    }

    $catalog = [ordered]@{
        schemaVersion = 1
        appVersion = $Version
        generatedAt = [DateTime]::UtcNow.ToString('o')
        productName = $ProductName
        downloads = $downloads
        sha256 = $sha256
        legacyAliases = $legacyAliases
        languages = $languages
        devicePacks = $devicePacks
    }

    Write-JsonFile -Value $catalog -Path $stableCatalogPath
    Write-JsonFile -Value $catalog -Path $resourcesStableCatalogPath
}

function Prepare-ReleaseAssets {
    if ([string]::IsNullOrWhiteSpace($BuildDir)) {
        throw 'BuildDir is required unless -FinalizeOnly is specified.'
    }
    if ([string]::IsNullOrWhiteSpace($OnlineBuildDir)) {
        throw 'OnlineBuildDir is required unless -FinalizeOnly is specified.'
    }

    $buildPath = Resolve-RepoPath $BuildDir
    $onlineBuildPath = Resolve-RepoPath $OnlineBuildDir
    $releaseOutputPath = Resolve-RepoPath $ReleaseOutput
    $pagesOutputPath = Resolve-RepoPath $PagesOutput

    if (-not (Test-Path -LiteralPath $buildPath)) {
        throw "Build output not found at '$buildPath'."
    }

    Remove-Item -LiteralPath $onlineBuildPath, $releaseOutputPath, $pagesOutputPath -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $onlineBuildPath, $releaseOutputPath, $pagesOutputPath -Force | Out-Null

    Copy-Item -Path (Join-Path $buildPath '*') -Destination $onlineBuildPath -Recurse -Force

    $languageDirectories = @(Get-LanguageDirectories $buildPath)
    foreach ($directory in $languageDirectories) {
        $target = Join-Path $onlineBuildPath $directory.Name
        if (Test-Path -LiteralPath $target) {
            Remove-Item -LiteralPath $target -Recurse -Force
        }
    }

    $fullZip = Join-Path $releaseOutputPath (Get-FullZipAssetName $Version)
    $onlineZip = Join-Path $releaseOutputPath (Get-OnlineZipAssetName $Version)
    Compress-DirectoryContents -SourceDir $buildPath -DestinationPath $fullZip
    Compress-DirectoryContents -SourceDir $onlineBuildPath -DestinationPath $onlineZip

    $resourcesRoot = Join-Path $pagesOutputPath "resources\$Version"
    $languageOutputPath = Join-Path $resourcesRoot 'languages'
    $deviceOutputPath = Join-Path $resourcesRoot 'devices'
    New-Item -ItemType Directory -Path $languageOutputPath, $deviceOutputPath -Force | Out-Null

    $languageEntries = @()
    $stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "${LegacyAssetPrefix}-lang-assets-$([Guid]::NewGuid().ToString('N'))"
    try {
        New-Item -ItemType Directory -Path $stagingRoot -Force | Out-Null

        foreach ($pack in (Get-LanguagePackDefinitions)) {
            $culture = [string]$pack.Culture
            $sourceDirectories = @($pack.Directories |
                ForEach-Object {
                    $candidate = Join-Path $buildPath $_
                    if (Test-Path -LiteralPath $candidate) {
                        $candidate
                    }
                })

            if ($sourceDirectories.Count -eq 0) {
                Write-Warning "Skipping language pack '$culture' because no matching resource directory was found."
                continue
            }

            $packStage = Join-Path $stagingRoot $culture
            New-Item -ItemType Directory -Path $packStage -Force | Out-Null

            foreach ($sourceDirectory in $sourceDirectories) {
                Copy-Item -LiteralPath $sourceDirectory -Destination $packStage -Recurse -Force
            }

            $packName = Get-LanguageAssetName $Version $culture
            $packZip = Join-Path $languageOutputPath $packName
            Compress-DirectoryContents -SourceDir $packStage -DestinationPath $packZip

            $languageEntries += [ordered]@{
                culture = $culture
                displayName = Get-LanguageDisplayName $culture
                url = "$ResourcesBaseUrl/$Version/languages/$packName"
                sha256 = Get-Sha256Hash -Path $packZip
                size = (Get-Item -LiteralPath $packZip).Length
                asset = New-FileMetadata `
                    -FilePath $packZip `
                    -Name $packName `
                    -CatalogPath "resources/$Version/languages/$packName" `
                    -Url "$ResourcesBaseUrl/$Version/languages/$packName"
                directories = @($sourceDirectories | ForEach-Object { Split-Path -Leaf $_ })
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    $languageCatalog = [ordered]@{
        schemaVersion = 1
        appVersion = $Version
        generatedAt = [DateTime]::UtcNow.ToString('o')
        productName = $ProductName
        languages = $languageEntries
    }
    Write-JsonFile -Value $languageCatalog -Path (Join-Path $languageOutputPath 'catalog.json')

    $deviceEntries = @()
    $deviceStagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "${PublicAssetPrefix}-device-assets-$([Guid]::NewGuid().ToString('N'))"
    try {
        New-Item -ItemType Directory -Path $deviceStagingRoot -Force | Out-Null

        foreach ($pack in (Get-DevicePackDefinitions)) {
            $packId = [string]$pack.Id
            $packStage = Join-Path $deviceStagingRoot $packId
            New-Item -ItemType Directory -Path $packStage -Force | Out-Null

            $manifest = [ordered]@{
                id = $packId
                displayName = [string]$pack.DisplayName
                vendor = [string]$pack.Vendor
                vendorAliases = @(Get-ListValue $pack.VendorAliases)
                families = @(Get-ListValue $pack.Families)
                modelPrefixes = @(Get-ListValue $pack.ModelPrefixes)
                modelKeywords = @(Get-ListValue $pack.ModelKeywords)
                machineTypes = @(Get-ListValue $pack.MachineTypes)
                enabledFeatures = @(Get-ListValue $pack.EnabledFeatures)
                hiddenFeatures = @(Get-ListValue $pack.HiddenFeatures)
            }

            Write-JsonFile -Value $manifest -Path (Join-Path $packStage 'device-pack.json')

            $packName = Get-DeviceAssetName $packId
            $packZip = Join-Path $deviceOutputPath $packName
            Compress-DirectoryContents -SourceDir $packStage -DestinationPath $packZip

            $deviceEntries += [ordered]@{
                id = $packId
                displayName = [string]$pack.DisplayName
                vendor = [string]$pack.Vendor
                vendorAliases = @(Get-ListValue $pack.VendorAliases)
                families = @(Get-ListValue $pack.Families)
                modelPrefixes = @(Get-ListValue $pack.ModelPrefixes)
                modelKeywords = @(Get-ListValue $pack.ModelKeywords)
                machineTypes = @(Get-ListValue $pack.MachineTypes)
                url = "$ResourcesBaseUrl/$Version/devices/$packName"
                sha256 = Get-Sha256Hash -Path $packZip
                size = (Get-Item -LiteralPath $packZip).Length
                asset = New-FileMetadata `
                    -FilePath $packZip `
                    -Name $packName `
                    -CatalogPath "resources/$Version/devices/$packName" `
                    -Url "$ResourcesBaseUrl/$Version/devices/$packName"
            }
        }
    }
    finally {
        Remove-Item -LiteralPath $deviceStagingRoot -Recurse -Force -ErrorAction SilentlyContinue
    }

    $deviceCatalog = [ordered]@{
        schemaVersion = 1
        appVersion = $Version
        generatedAt = [DateTime]::UtcNow.ToString('o')
        productName = $ProductName
        devicePacks = $deviceEntries
    }
    Write-JsonFile -Value $deviceCatalog -Path (Join-Path $deviceOutputPath 'catalog.json')

    Write-StableCatalog -ReleaseOutputPath $releaseOutputPath -PagesOutputPath $pagesOutputPath

    Write-Host "Prepared Full and Online portable zips in '$releaseOutputPath'."
    Write-Host "Prepared Online build output in '$onlineBuildPath'."
    Write-Host "Prepared GitHub Pages resources in '$pagesOutputPath'."
}

function Finalize-ReleaseAssets {
    if ([string]::IsNullOrWhiteSpace($FullInstallerPath)) {
        throw 'FullInstallerPath is required with -FinalizeOnly.'
    }
    if ([string]::IsNullOrWhiteSpace($OnlineInstallerPath)) {
        throw 'OnlineInstallerPath is required with -FinalizeOnly.'
    }

    $releaseOutputPath = Resolve-RepoPath $ReleaseOutput
    $pagesOutputPath = Resolve-RepoPath $PagesOutput
    $fullInstallerSource = Resolve-RepoPath $FullInstallerPath
    $onlineInstallerSource = Resolve-RepoPath $OnlineInstallerPath

    if (-not (Test-Path -LiteralPath $fullInstallerSource)) {
        throw "Full installer not found at '$fullInstallerSource'."
    }
    if (-not (Test-Path -LiteralPath $onlineInstallerSource)) {
        throw "Online installer not found at '$onlineInstallerSource'."
    }

    New-Item -ItemType Directory -Path $releaseOutputPath, $pagesOutputPath -Force | Out-Null

    $fullSetupName = Get-FullSetupAssetName $Version
    $onlineSetupName = Get-OnlineSetupAssetName $Version
    $fullZipName = Get-FullZipAssetName $Version
    $onlineZipName = Get-OnlineZipAssetName $Version
    $crossPlatformCliName = Get-CrossPlatformCliAssetName $Version
    $hashName = Get-HashAssetName $Version
    $legacySetupName = Get-LegacySetupAssetName $Version

    Copy-Item -LiteralPath $fullInstallerSource -Destination (Join-Path $releaseOutputPath $fullSetupName) -Force
    Copy-Item -LiteralPath $fullInstallerSource -Destination (Join-Path $releaseOutputPath $legacySetupName) -Force
    Copy-Item -LiteralPath $onlineInstallerSource -Destination (Join-Path $releaseOutputPath $onlineSetupName) -Force

    $hashAssetNames = @($fullSetupName, $onlineSetupName, $fullZipName, $onlineZipName, $legacySetupName)
    if (Test-Path -LiteralPath (Join-Path $releaseOutputPath $crossPlatformCliName)) {
        $hashAssetNames += $crossPlatformCliName
    }

    Write-HashFile -AssetNames $hashAssetNames -ReleaseOutputPath $releaseOutputPath -HashFileName $hashName
    Write-StableCatalog -ReleaseOutputPath $releaseOutputPath -PagesOutputPath $pagesOutputPath

    Write-Host "Finalized installer aliases and SHA256 file in '$releaseOutputPath'."
    Write-Host "Finalized stable catalogs in '$pagesOutputPath\stable\catalog.json' and '$pagesOutputPath\resources\stable\catalog.json'."
}

if ($FinalizeOnly) {
    Finalize-ReleaseAssets
} else {
    Prepare-ReleaseAssets
}
