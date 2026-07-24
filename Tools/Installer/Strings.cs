using System.Globalization;

namespace UniversalDeviceToolkit.Installer;

/// <summary>
/// Built-in EN/zh UI strings for the installer itself (follows the OS display
/// language). The app language is chosen on the wizard's language page and
/// seeded into the app's first-run state.
/// </summary>
internal static class Strings
{
    private static readonly Dictionary<string, (string En, string Zh)> Table = new()
    {
        ["WindowTitle"] = ("Universal Device Toolkit Setup", "Universal Device Toolkit 安装程序"),
        ["WelcomeTitle"] = ("Welcome to Universal Device Toolkit Setup", "欢迎使用 Universal Device Toolkit 安装程序"),
        ["WelcomeText"] = ("This will install Universal Device Toolkit {0} on your computer.\r\n\r\nClick Next to continue.", "将在你的电脑上安装 Universal Device Toolkit {0}。\r\n\r\n点击“下一步”继续。"),
        ["UpgradeDetected"] = ("An existing installation was detected and will be upgraded.", "检测到已安装的版本，将为你升级。"),
        ["RuntimeMissing"] = ("Microsoft .NET Desktop Runtime {0} or later is required but was not found.", "需要 Microsoft .NET Desktop Runtime {0} 或更高版本，但未检测到。"),
        ["InstallRuntime"] = ("Install runtime automatically", "自动安装运行时"),
        ["RuntimeInstalling"] = ("Downloading and installing .NET Desktop Runtime…", "正在下载并安装 .NET Desktop Runtime…"),
        ["RuntimeFailed"] = ("Runtime installation failed. Please install it manually and click Retry.", "运行时安装失败。请手动安装后点击“重试”。"),
        ["LocationTitle"] = ("Choose install location", "选择安装位置"),
        ["LocationText"] = ("Setup will install Universal Device Toolkit into the following folder.", "安装程序将把 Universal Device Toolkit 安装到以下文件夹。"),
        ["LanguageTitle"] = ("Choose your language", "选择语言"),
        ["LanguageText"] = ("The app will use this language. You can change it later in Settings.", "应用将使用此语言，之后可在设置中更改。"),
        ["DeviceTitle"] = ("Choose your device", "选择你的设备"),
        ["DeviceDetected"] = ("Detected: {0}", "检测到：{0}"),
        ["DeviceText"] = ("This decides which features the app enables. You can change it later in Settings.", "这将决定应用启用哪些功能，之后可在设置中更改。"),
        ["DeviceHardwareNote"] = ("Hardware controls will be enabled for this device.", "将为该机型启用硬件控制功能。"),
        ["DeviceBasicNote"] = ("Basic mode: plugins and system optimization only (no hardware controls).", "基础模式：仅插件与系统优化（不含硬件控制）。"),
        ["DeviceAskLater"] = ("Ask me on first run", "首次启动时再询问我"),
        ["StatusLanguagePack"] = ("Downloading language pack…", "正在下载语言包…"),
        ["Browse"] = ("Browse…", "浏览…"),
        ["DesktopShortcut"] = ("Create a desktop shortcut", "创建桌面快捷方式"),
        ["ProgressTitle"] = ("Installing", "正在安装"),
        ["UninstallProgressTitle"] = ("Uninstalling", "正在卸载"),
        ["StatusCheckingRuntime"] = ("Checking .NET Desktop Runtime…", "正在检查 .NET Desktop Runtime…"),
        ["StatusRemovingOld"] = ("Removing previous installation…", "正在移除旧版本…"),
        ["StatusDownloading"] = ("Downloading payload… {0:0}%", "正在下载组件… {0:0}%"),
        ["StatusDownloadMirror"] = ("Trying mirror {0}/{1}…", "正在尝试镜像 {0}/{1}…"),
        ["StatusVerifying"] = ("Verifying download…", "正在校验下载内容…"),
        ["StatusExtracting"] = ("Extracting files…", "正在解压文件…"),
        ["StatusShortcuts"] = ("Creating shortcuts…", "正在创建快捷方式…"),
        ["StatusFinishing"] = ("Finishing up…", "正在完成安装…"),
        ["StatusUnregisterShell"] = ("Releasing shell integration…", "正在释放资源管理器集成…"),
        ["StatusRemovingTasks"] = ("Removing scheduled tasks…", "正在删除计划任务…"),
        ["StatusRemovingFiles"] = ("Removing files…", "正在删除文件…"),
        ["StatusRemovingData"] = ("Removing application data…", "正在删除应用数据…"),
        ["DoneTitle"] = ("Installation complete", "安装完成"),
        ["DoneText"] = ("Universal Device Toolkit {0} has been installed.", "Universal Device Toolkit {0} 已安装完成。"),
        ["LaunchApp"] = ("Launch Universal Device Toolkit", "启动 Universal Device Toolkit"),
        ["ErrorTitle"] = ("Installation failed", "安装失败"),
        ["UninstallConfirmTitle"] = ("Uninstall Universal Device Toolkit", "卸载 Universal Device Toolkit"),
        ["UninstallConfirmText"] = ("This will remove Universal Device Toolkit from:\r\n{0}", "将从以下位置移除 Universal Device Toolkit：\r\n{0}"),
        ["DeleteData"] = ("Also delete settings and application data", "同时删除设置和应用数据"),
        ["UninstallDoneTitle"] = ("Uninstall complete", "卸载完成"),
        ["UninstallDoneText"] = ("Universal Device Toolkit has been removed.", "Universal Device Toolkit 已被移除。"),
        ["Next"] = ("Next >", "下一步 >"),
        ["Back"] = ("< Back", "< 上一步"),
        ["Cancel"] = ("Cancel", "取消"),
        ["Install"] = ("Install", "安装"),
        ["Uninstall"] = ("Uninstall", "卸载"),
        ["Finish"] = ("Finish", "完成"),
        ["Retry"] = ("Retry", "重试"),
        ["Exit"] = ("Exit", "退出"),
        ["CancelConfirm"] = ("Cancel the installation?", "确定要取消安装吗？"),
    };

    private static readonly bool IsChinese =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase);

    public static string Get(string key) =>
        Table.TryGetValue(key, out var pair) ? (IsChinese ? pair.Zh : pair.En) : key;

    public static string Format(string key, params object[] args) => string.Format(Get(key), args);
}
