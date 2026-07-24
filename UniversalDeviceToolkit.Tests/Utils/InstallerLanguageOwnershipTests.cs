using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

public class InstallerLanguageOwnershipTests
{
    [Fact]
    public void Installer_ShouldSeedFirstRunStateInAppFormat()
    {
        // The installer owns the first-run answers now: it writes the very same
        // files the app would, so the app must not ask again on first launch.
        var firstRunState = ReadRepositoryFile("Tools", "Installer", "FirstRunState.cs");

        firstRunState.Should().Contain("\"lang\"");
        firstRunState.Should().Contain("\"device-setup\"");
        firstRunState.Should().Contain("devicePackId=");
        firstRunState.Should().Contain("basicMode=");
        firstRunState.Should().Contain("confirmedAtUtc=");
    }

    [Fact]
    public void Installer_ShouldKeepUninstallCoverageForSeededState()
    {
        // The seeded files live in %LocalAppData%\UniversalDeviceToolkit; the
        // uninstaller's app-data purge must cover them.
        var engine = ReadRepositoryFile("Tools", "Installer", "InstallerEngine.cs");

        engine.Should().Contain("TryDeleteDirectory(InstallerConstants.AppDataDir)");
        engine.Should().Contain("DeleteAppData");
    }

    [Fact]
    public void App_ShouldSkipSelectorsWhenFirstRunStateIsSeeded()
    {
        var localizationHelper = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Utils", "LocalizationHelper.cs");
        var setupCoordinator = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Utils", "StartupDeviceSetupCoordinator.cs");

        localizationHelper.Should().Contain("showLanguageSelector = interactive && (savedCultureInfo is null || !deviceSetupExists)");
        setupCoordinator.Should().Contain("if (_isSetupComplete())");
    }

    [Fact]
    public void AppStartup_ShouldKeepSelectorFallbackForUnseededInstalls()
    {
        var appStartup = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Startup", "StartupOrchestrator.cs");
        var localizationHelper = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Utils", "LocalizationHelper.cs");
        var languageWindow = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Utils", "LanguageSelectorWindow.xaml.cs");

        appStartup.Should().Contain("SetLanguageAsync(true");
        localizationHelper.Should().Contain("new LanguageSelectorWindow(Languages");
        localizationHelper.Should().Contain("LanguagePackManager? languagePackManager");
        languageWindow.Should().Contain("EnsureLanguageInstalledAsync");
        languageWindow.Should().Contain("_languagePackManager.InstallAsync");
        languageWindow.Should().Contain("_fallbackLanguage");
        languageWindow.Should().Contain("Complete(LanguageGateOutcome.ContinueEnglish, _fallbackLanguage)");
    }

    [Fact]
    public void Installer_ShouldNotContainInnoLeftovers()
    {
        foreach (var source in ReadInstallerSources())
        {
            source.Should().NotContain("[Languages]");
            source.Should().NotContain("MessagesFile:");
            source.Should().NotContain("compiler:Languages");
            source.Should().NotContain("InnoDependencies\\Chinese");
            source.Should().NotContain("SetupLanguageToAppCulture");
            source.Should().NotContain("SaveStringToFile");
        }
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
