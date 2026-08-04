using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
[Trait("Category", TestCategories.Unit)]
public sealed class InstallerSecurityGuardTests
{
    [Fact]
    public void Installer_ShouldRequireElevationAndUseProtectedInstallRoot()
    {
        var manifest = ReadRepositoryFile("Tools", "Installer", "app.manifest");
        var constants = ReadRepositoryFile("Tools", "Installer", "InstallerConstants.cs");
        var engine = ReadRepositoryFile("Tools", "Installer", "InstallerEngine.cs");
        var policy = ReadRepositoryFile("Tools", "Installer", "InstallerInstallPathPolicy.cs");

        manifest.Should().Contain("requestedExecutionLevel level=\"requireAdministrator\"");
        constants.Should().Contain("Environment.SpecialFolder.ProgramFiles");
        engine.Should().Contain("InstallerInstallPathPolicy.PrepareForInstall(installDir)");
        engine.Should().Contain("InstallerInstallPathPolicy.ValidateForUninstall(installDir)");
        policy.Should().Contain("FileAttributes.ReparsePoint");
        policy.Should().Contain("DirectorySecurity");
        policy.Should().Contain("FileSecurity");
        policy.Should().Contain("SetAccessRuleProtection(isProtected: true, preserveInheritance: false)");
        policy.Should().Contain("WellKnownSidType.BuiltinAdministratorsSid");
        policy.Should().Contain("WellKnownSidType.BuiltinUsersSid");
        policy.Should().Contain("must be installed below the protected Program Files directory");
    }

    [Fact]
    public void RuntimeLanguagePacks_ShouldStayOutsideProtectedInstallRoot()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Utils", "LanguagePackManager.cs");

        source.Should().Contain("language-packs");
        source.Should().Contain("AssemblyLoadContext.Default.Resolving");
        source.Should().Contain("ResolveUserLanguageSatelliteAssembly");
        source.Should().Contain("ApplicationDirectory => UserLanguagePackDirectory");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var root = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine([root, .. pathParts]));
    }
}
