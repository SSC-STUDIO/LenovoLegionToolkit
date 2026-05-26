$path = Join-Path $PSScriptRoot '..\UniversalDeviceToolkit.WPF\App.xaml.cs'
$content = Get-Content -Raw -LiteralPath $path
if ($content -match 'AgentDebugLog\.Write') {
    Write-Output 'ALREADY_PATCHED'
    exit 0
}

$needle = @"
        EnsureSingleInstance();

        await LocalizationHelper.SetLanguageAsync(true, CreateStartupLanguagePackManager(flags));
"@

$insert = @"
        EnsureSingleInstance();

        // #region agent log
        var deviceSetupPath = Path.Combine(Folders.AppData, "device-setup");
        AgentDebugLog.Write(
            "App.Application_Startup:beforeLanguage",
            "Application startup before language init",
            "D",
            new
            {
                skipCompatCheck = flags.SkipCompatibilityCheck,
                deviceSetupExists = File.Exists(deviceSetupPath),
                deviceSetupPath,
                langFileExists = File.Exists(Path.Combine(Folders.AppData, "lang")),
                legacyAppDataExists = Directory.Exists(Folders.LegacyAppData)
            });
        // #endregion

        await LocalizationHelper.SetLanguageAsync(true, CreateStartupLanguagePackManager(flags));
"@

if (-not $content.Contains($needle)) {
    Write-Error 'Needle not found'
    exit 1
}

$content = $content.Replace($needle, $insert)
Set-Content -LiteralPath $path -Value $content -NoNewline
Write-Output 'OK'
