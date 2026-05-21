using FluentAssertions;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

public class InstallerLanguageBridgeTests
{
    [Theory]
    [InlineData("ptbr", "pt-br")]
    [InlineData("nlnl", "nl-nl")]
    [InlineData("ukr", "uk")]
    [InlineData("zhhans", "zh-hans")]
    [InlineData("zhhant", "zh-hant")]
    public void MakeInstaller_ShouldMapInnoLanguageNamesToAppCultures(string innoLanguage, string appCulture)
    {
        var script = ReadInstallerScript();

        script.Should().Contain($"if LanguageName = '{innoLanguage}'");
        script.Should().Contain($"Result := '{appCulture}'");
    }

    [Fact]
    public void MakeInstaller_ShouldPersistSelectedSetupLanguageForFirstAppLaunch()
    {
        var script = ReadInstallerScript();

        script.Should().Contain("LangPath := AppDataDir + '\\lang'");
        script.Should().Contain("if FileExists(LangPath) then");
        script.Should().Contain("AppCulture := SetupLanguageToAppCulture(ActiveLanguage)");
        script.Should().Contain("SaveStringToFile(LangPath, AppCulture, False)");
        script.Should().Contain("if CurStep = ssPostInstall then");
    }

    private static string ReadInstallerScript()
    {
        var repositoryRoot = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(repositoryRoot, "MakeInstaller.iss"));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MakeInstaller.iss")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
