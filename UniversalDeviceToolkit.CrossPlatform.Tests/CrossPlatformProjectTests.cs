using FluentAssertions;
using System.Xml.Linq;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class CrossPlatformProjectTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void CrossPlatformCli_ShouldTargetPlainNet10()
    {
        var project = XDocument.Load(Path.Combine(RepositoryRoot, "UniversalDeviceToolkit.CrossPlatform", "UniversalDeviceToolkit.CrossPlatform.csproj"));

        project.Descendants("TargetFramework").Single().Value.Should().Be("net10.0");
        var projectText = project.ToString();
        projectText.Contains("windows", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        projectText.Contains("UseWPF", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        projectText.Contains("RuntimeIdentifier", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }

    [Fact]
    public void CrossPlatformCli_ShouldAvoidWindowsOnlyApis()
    {
        var source = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "UniversalDeviceToolkit.CrossPlatform"), "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        source.Should().NotContain("System.Management");
        source.Should().NotContain("Microsoft.Win32");
        source.Should().NotContain("Windows.Win32");
        source.Should().NotContain("System.Windows");
        source.Should().NotContain("NamedPipe");
    }

    [Fact]
    public void CrossPlatformCliAssetScript_ShouldPackageLaunchers()
    {
        var scriptText = File.ReadAllText(Path.Combine(RepositoryRoot, "Scripts", "Build-CrossPlatformCliAsset.ps1"));

        scriptText.Should().Contain("Write-CrossPlatformLaunchers");
        scriptText.Should().Contain("'udt.cmd'");
        scriptText.Should().Contain("'README.txt'");
        scriptText.Should().Contain("dotnet \"$SCRIPT_DIR/udt.dll\" \"$@\"");
        scriptText.Should().Contain("dotnet \"%~dp0udt.dll\" %*");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
