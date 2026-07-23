using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

public class InstallerLanguageOwnershipTests
{
    [Fact]
    public void Installer_ShouldNotOfferLanguageSelection()
    {
        foreach (var source in ReadInstallerSources())
        {
            source.Should().NotContain("[Languages]");
            source.Should().NotContain("MessagesFile:");
            source.Should().NotContain("compiler:Languages");
            source.Should().NotContain("InnoDependencies\\Chinese");
            source.Should().NotContain("LanguageSelectorWindow");
        }

        // The wizard must not contain a language picker of any kind.
        var mainWindow = ReadRepositoryFile("Tools", "Installer", "MainWindow.xaml");
        mainWindow.Should().NotContain("ComboBox");
    }

    [Fact]
    public void Installer_ShouldNotPersistAppLanguageFromSetup()
    {
        foreach (var source in ReadInstallerSources())
        {
            source.Should().NotContain("SetupLanguageToAppCulture");
            source.Should().NotContain("ActiveLanguage");
            source.Should().NotContain("LangPath");
            source.Should().NotContain("SaveStringToFile");
        }
    }

    [Fact]
    public void AppStartup_ShouldOwnInteractiveLanguageInitialization()
    {
        var appStartup = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Startup", "StartupOrchestrator.cs");
        var localizationHelper = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Utils", "LocalizationHelper.cs");
        var languageWindow = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "LanguageSelectorWindow.xaml.cs");

        appStartup.Should().Contain("SetLanguageAsync(true");
        localizationHelper.Should().Contain("showLanguageSelector = interactive && (savedCultureInfo is null || !deviceSetupExists)");
        localizationHelper.Should().Contain("new LanguageSelectorWindow(Languages");
        localizationHelper.Should().Contain("LanguagePackManager? languagePackManager");
        languageWindow.Should().Contain("EnsureLanguageInstalledAsync");
        languageWindow.Should().Contain("_languagePackManager.InstallAsync");
        languageWindow.Should().Contain("_fallbackLanguage");
        languageWindow.Should().Contain("Complete(LanguageGateOutcome.ContinueEnglish, _fallbackLanguage)");
    }

    [Fact]
    public void Installer_ShouldRemoveWholeInstallTreeIncludingLanguagePacks()
    {
        // The payload ships satellite folders (en/zh-hans/zh-hant) and the app can
        // download more packs at runtime; recursive deletion removes them all by
        // construction, replacing Inno's per-folder [UninstallDelete] list.
        var engine = ReadRepositoryFile("Tools", "Installer", "InstallerEngine.cs");

        engine.Should().Contain("Directory.Delete(path, recursive: true)");
        engine.Should().Contain("TryDeleteDirectory(installDir)");
        engine.Should().Contain("DeleteDirectoryContentsExcept(installDir");
    }

    private static string[] ReadInstallerSources()
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        var installerDir = Path.Combine(repositoryRoot, "Tools", "Installer");
        return Directory
            .EnumerateFiles(installerDir, "*.cs")
            .Select(File.ReadAllText)
            .ToArray();
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

}
