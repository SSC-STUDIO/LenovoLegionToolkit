using FluentAssertions;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Security)]
public class PathSecurityExpandedTests
{
    #region IsValidPluginId Expanded

    [Theory]
    [InlineData("com.example.my-plugin")]
    [InlineData("a")]
    [InlineData("MyPlugin123")]
    public void IsValidPluginId_WithSafeIds_ShouldReturnTrue(string id)
    {
        PathSecurity.IsValidPluginId(id).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1startsWithDigit")]
    [InlineData("has spaces")]
    [InlineData("has@at")]
    [InlineData("has..doubleDot")]
    [InlineData("has/slash")]
    public void IsValidPluginId_WithInvalidIds_ShouldReturnFalse(string? id)
    {
        PathSecurity.IsValidPluginId(id).Should().BeFalse();
    }

    #endregion

    #region IsValidRegistryPath Expanded

    [Theory]
    [InlineData("HKEY_CURRENT_USER\\Software\\Test")]
    [InlineData("HKLM\\SOFTWARE\\Test")]
    [InlineData("\\HKCR\\.txt")]
    [InlineData("HKEY_USERS\\.DEFAULT")]
    [InlineData("HKEY_CLASSES_ROOT\\CLSID")]
    public void IsValidRegistryPath_WithAllAllowedRoots_ShouldReturnTrue(string path)
    {
        PathSecurity.IsValidRegistryPath(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("HKEY_DYN_DATA\\Test")]
    [InlineData("HKEY_PERFORMANCE_DATA\\Test")]
    [InlineData("UNKNOWN_ROOT\\Test")]
    public void IsValidRegistryPath_WithDisallowedRoots_ShouldReturnFalse(string path)
    {
        PathSecurity.IsValidRegistryPath(path).Should().BeFalse();
    }

    [Fact]
    public void IsValidRegistryPath_WithTraversal_ShouldReturnFalse()
    {
        PathSecurity.IsValidRegistryPath("HKCU\\..\\..\\escape").Should().BeFalse();
    }

    [Fact]
    public void IsValidRegistryPath_WithNullByte_ShouldReturnFalse()
    {
        PathSecurity.IsValidRegistryPath("HKCU\0\\evil").Should().BeFalse();
    }

    #endregion

    #region IsValidDriverPath

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidDriverPath_WithNullOrEmpty_ShouldReturnFalse(string? path)
    {
        PathSecurity.IsValidDriverPath(path).Should().BeFalse();
    }

    [Fact]
    public void IsValidDriverPath_WithNonSystemPath_ShouldReturnFalse()
    {
        PathSecurity.IsValidDriverPath(@"C:\Users\test\evil.sys").Should().BeFalse();
    }

    [Fact]
    public void IsValidDriverPath_WithNonSysExtension_ShouldReturnFalse()
    {
        PathSecurity.IsValidDriverPath(@"C:\Windows\System32\drivers\test.dll").Should().BeFalse();
    }

    [Fact]
    public void IsValidDriverPath_WithPrefixSiblingDirectory_ShouldReturnFalse()
    {
        // Classic prefix bypass: "…\driversEvil" must not match root "…\drivers".
        var systemDir = Environment.SystemDirectory;
        PathSecurity.IsValidDriverPath(Path.Combine(systemDir, "driversEvil", "payload.sys")).Should().BeFalse();
        PathSecurity.IsValidDriverPath(Path.Combine(systemDir, "DriverStoreX", "payload.sys")).Should().BeFalse();
    }

    #endregion

    #region SanitizeFileName

    [Theory]
    [InlineData("normal-file.txt")]
    [InlineData("my_plugin.zip")]
    public void SanitizeFileName_WithCleanNames_ShouldReturnSame(string name)
    {
        PathSecurity.SanitizeFileName(name).Should().Be(name);
    }

    #endregion

    #region IsValidDirectoryPath

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValidDirectoryPath_WithNullOrEmpty_ShouldReturnFalse(string? path)
    {
        PathSecurity.IsValidDirectoryPath(path).Should().BeFalse();
    }

    [Fact]
    public void IsValidDirectoryPath_WithTilde_ShouldReturnFalse()
    {
        PathSecurity.IsValidDirectoryPath("~/secret").Should().BeFalse();
    }

    [Fact]
    public void IsValidDirectoryPath_WithWildcard_ShouldReturnFalse()
    {
        PathSecurity.IsValidDirectoryPath("C:\\test\\*.exe").Should().BeFalse();
    }

    [Fact]
    public void IsValidDirectoryPath_WithPipe_ShouldReturnFalse()
    {
        PathSecurity.IsValidDirectoryPath("C:\\test|pipe").Should().BeFalse();
    }

    #endregion

    #region CreateSafeFilePath

    [Fact]
    public void CreateSafeFilePath_WithValidInputs_ShouldReturnFullPath()
    {
        var result = PathSecurity.CreateSafeFilePath("C:\\Plugins", "test.dll");
        result.Should().NotBeNull();
        result.Should().Contain("test.dll");
    }

    [Theory]
    [InlineData(null, "file.dll")]
    [InlineData("C:\\Dir", null)]
    [InlineData("", "file.dll")]
    public void CreateSafeFilePath_WithNullInputs_ShouldReturnNull(string? dir, string? file)
    {
        PathSecurity.CreateSafeFilePath(dir!, file).Should().BeNull();
    }

    #endregion

    #region IsPathWithinAllowedDirectory

    [Fact]
    public void IsPathWithinAllowedDirectory_WithinBase_ShouldReturnTrue()
    {
        var basePath = "C:\\Plugins";
        var path = "C:\\Plugins\\test.dll";
        PathSecurity.IsPathWithinAllowedDirectory(path, basePath).Should().BeTrue();
    }

    [Theory]
    [InlineData(null, "C:\\Dir")]
    [InlineData("C:\\Dir", null)]
    [InlineData("", "C:\\Dir")]
    [InlineData("C:\\Dir", "")]
    public void IsPathWithinAllowedDirectory_WithNullInputs_ShouldReturnFalse(string? path, string? basePath)
    {
        PathSecurity.IsPathWithinAllowedDirectory(path, basePath).Should().BeFalse();
    }

    #endregion
}
