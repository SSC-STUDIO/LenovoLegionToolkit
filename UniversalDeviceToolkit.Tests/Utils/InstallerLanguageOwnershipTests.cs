using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

public class InstallerLanguageOwnershipTests
{
    [Fact]
    public void MakeInstaller_ShouldNotOfferInnoLanguageSelection()
    {
        var script = ReadInstallerScript();

        script.Should().NotContain("[Languages]");
        script.Should().NotContain("MessagesFile:");
        script.Should().NotContain("compiler:Languages");
        script.Should().NotContain("InnoDependencies\\Chinese");
    }

    [Fact]
    public void MakeInstaller_ShouldNotPersistAppLanguageFromSetup()
    {
        var script = ReadInstallerScript();

        script.Should().NotContain("SetupLanguageToAppCulture");
        script.Should().NotContain("ActiveLanguage");
        script.Should().NotContain("LangPath");
        script.Should().NotContain("SaveStringToFile");
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
    public void MakeInstaller_ShouldCleanRuntimeDownloadedLanguagePacks()
    {
        var script = ReadInstallerScript();

        script.Should().Contain(@"Name: ""{app}\zh""");
        script.Should().Contain(@"Name: ""{app}\zh-hans""");
        script.Should().Contain(@"Name: ""{app}\pt-br""");
        script.Should().Contain(@"Name: ""{app}\uz-latn-uz""");
    }

    private static string ReadInstallerScript()
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine(repositoryRoot, "MakeInstaller.iss"));
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine([repositoryRoot, .. pathParts]));
    }

}
