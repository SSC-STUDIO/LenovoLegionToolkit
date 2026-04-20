using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Moq;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginLoaderTests : IDisposable
{
    private readonly Mock<IPluginSignatureValidator> _mockSignatureValidator;
    private readonly PluginLoader _loader;
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirectories = new();

    public PluginLoaderTests()
    {
        _mockSignatureValidator = new Mock<IPluginSignatureValidator>();
        _loader = new PluginLoader();
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            try { File.Delete(file); }
            catch { }
        }
        foreach (var dir in _tempDirectories.Where(Directory.Exists))
        {
            try { Directory.Delete(dir, true); }
            catch { }
        }
    }

    private string CreateTempFile(string content = "")
    {
        var path = Path.GetTempFileName();
        _tempFiles.Add(path);
        if (!string.IsNullOrEmpty(content))
            File.WriteAllText(path, content);
        return path;
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }

    private static MethodInfo GetPrivateStaticMethod(string methodName)
    {
        var method = typeof(PluginLoader).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        return method!;
    }

    private static string InvokePrivateStringMethod(MethodInfo method, object? argument)
    {
        var result = method.Invoke(null, new object?[] { argument });
        return result.Should().BeOfType<string>().Which;
    }

    private static bool InvokePrivateBoolMethod(MethodInfo method, object? argument)
    {
        var result = method.Invoke(null, new object?[] { argument });
        return result.Should().BeOfType<bool>().Which;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        // Arrange & Act
        var loader = new PluginLoader();

        // Assert
        loader.Should().NotBeNull();
    }

    [Fact]
    public void GetCultureFolders_ShouldReturnExpectedFolders()
    {
        // Arrange & Act
        var folders = _loader.GetCultureFolders();

        // Assert
        folders.Should().NotBeNull();
        folders.Should().Contain("ar", "de", "es", "fr", "ja", "zh-hans", "zh-hant");
        folders.Should().Contain("tools");
    }

    #endregion

    #region CanLoad Tests

    [Fact]
    public void CanLoad_WithSDKDll_ShouldReturnFalse()
    {
        // Arrange
        var filePath = "LenovoLegionToolkit.Plugins.SDK.dll";

        // Act
        var result = _loader.CanLoad(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_WithResourcesDll_ShouldReturnFalse()
    {
        // Arrange
        var filePath = "SomePlugin.resources.dll";

        // Act
        var result = _loader.CanLoad(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_WithPluginPrefix_ShouldReturnTrue()
    {
        // Arrange
        var filePath = "LenovoLegionToolkit.Plugins.TestPlugin.dll";

        // Act
        var result = _loader.CanLoad(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanLoad_WithoutPluginPrefixNoParentDir_ShouldReturnFalse()
    {
        // Arrange
        var filePath = "TestPlugin.dll";

        // Act
        var result = _loader.CanLoad(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_WithMatchingParentDirectory_ShouldReturnTrue()
    {
        // Arrange
        var filePath = "TestPlugin.dll";
        var parentDir = "TestPlugin";

        // Act
        var result = _loader.CanLoad(filePath, parentDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanLoad_WithMatchingParentDirectoryPrefix_ShouldReturnTrue()
    {
        // Arrange
        var filePath = "TestPlugin.dll";
        var parentDir = "LenovoLegionToolkit.Plugins.TestPlugin";

        // Act
        var result = _loader.CanLoad(filePath, parentDir);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanLoad_WithNonMatchingParentDirectory_ShouldReturnFalse()
    {
        // Arrange
        var filePath = "TestPlugin.dll";
        var parentDir = "OtherPlugin";

        // Act
        var result = _loader.CanLoad(filePath, parentDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_WithWhitespaceFileName_ShouldReturnFalse()
    {
        // Arrange
        var filePath = "   .dll";
        var parentDir = "TestPlugin";

        // Act
        var result = _loader.CanLoad(filePath, parentDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanLoad_WithEmptyFileName_ShouldReturnFalse()
    {
        // Arrange
        var filePath = "";
        var parentDir = "TestPlugin";

        // Act
        var result = _loader.CanLoad(filePath, parentDir);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region LoadFromFileAsync Tests

    [Fact]
    public async Task LoadFromFileAsync_WithInvalidSignature_ShouldReturnNull()
    {
        // Arrange
        var dllPath = CreateTempFile("fake dll content");
        _mockSignatureValidator
            .Setup(v => v.ValidateAsync(dllPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Invalid, "Invalid signature"));

        // Act
        var result = await _loader.LoadFromFileAsync(dllPath, _mockSignatureValidator.Object);

        // Assert
        result.Should().BeNull();
        _mockSignatureValidator.Verify(v => v.ValidateAsync(dllPath), Times.Once);
    }

    [Fact]
    public async Task LoadFromFileAsync_WithMissingFile_ShouldReturnNull()
    {
        // Arrange
        var dllPath = Path.Combine(Path.GetTempPath(), "nonexistent.dll");
        _mockSignatureValidator
            .Setup(v => v.ValidateAsync(dllPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.ValidationError, "File not found"));

        // Act
        var result = await _loader.LoadFromFileAsync(dllPath, _mockSignatureValidator.Object);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadFromFileAsync_WhenValidationDisabled_ShouldStillValidate()
    {
        // Arrange - Even when validation is disabled, LoadFromFileAsync calls validator
        var dllPath = CreateTempFile("fake content");
        _mockSignatureValidator
            .Setup(v => v.ValidateAsync(dllPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid, null));

        // Act
        var result = await _loader.LoadFromFileAsync(dllPath, _mockSignatureValidator.Object);

        // Assert - Will return null because file is not a valid assembly
        result.Should().BeNull();
        _mockSignatureValidator.Verify(v => v.ValidateAsync(dllPath), Times.Once);
    }

    [Fact]
    public async Task LoadFromFileAsync_WithValidSignatureButInvalidAssembly_ShouldReturnNull()
    {
        // Arrange
        var dllPath = CreateTempFile("not a real assembly");
        _mockSignatureValidator
            .Setup(v => v.ValidateAsync(dllPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid, null));

        // Act
        var result = await _loader.LoadFromFileAsync(dllPath, _mockSignatureValidator.Object);

        // Assert - Returns null because assembly loading fails
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadFromFileAsync_WithNullPath_ShouldReturnNull()
    {
        // Arrange
        string? dllPath = null;

        // Act
        var result = await _loader.LoadFromFileAsync(dllPath!, _mockSignatureValidator.Object);

        // Assert - Returns null because null path causes validation or file read error
        result.Should().BeNull();
    }

    [Fact]
    public void GetPluginAssemblyCandidatePath_WithManagedDependency_ShouldReturnSidecarDllPath()
    {
        // Arrange
        var method = GetPrivateStaticMethod("GetPluginAssemblyCandidatePath");
        var pluginDirectory = CreateTempDirectory();
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "LenovoLegionToolkit.Plugins.TestPlugin.dll");
        var requestedAssemblyName = new AssemblyName("Helper.Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        // Act
        var result = method.Invoke(null, new object?[] { requestedAssemblyName, pluginMainAssemblyPath, pluginDirectory }) as string;

        // Assert
        result.Should().Be(Path.GetFullPath(Path.Combine(pluginDirectory, "Helper.Library.dll")));
    }

    [Fact]
    public void GetPluginAssemblyCandidatePath_WithSatelliteAssembly_ShouldReturnCultureSpecificPath()
    {
        // Arrange
        var method = GetPrivateStaticMethod("GetPluginAssemblyCandidatePath");
        var pluginDirectory = CreateTempDirectory();
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "LenovoLegionToolkit.Plugins.TestPlugin.dll");
        var requestedAssemblyName = new AssemblyName("Helper.Library.resources, Version=1.0.0.0, Culture=fr, PublicKeyToken=null");

        // Act
        var result = method.Invoke(null, new object?[] { requestedAssemblyName, pluginMainAssemblyPath, pluginDirectory }) as string;

        // Assert
        result.Should().Be(Path.GetFullPath(Path.Combine(pluginDirectory, "fr", "Helper.Library.resources.dll")));
    }

    #endregion

    #region NormalizePluginToken Tests

    [Fact]
    public void NormalizePluginToken_WithMixedCaseAndSymbols_ShouldNormalize()
    {
        // Arrange - Access private method via reflection
        var method = GetPrivateStaticMethod("NormalizePluginToken");

        // Act
        var result = InvokePrivateStringMethod(method, "Test-Plugin_Name_v1.0");

        // Assert
        result.Should().Be("testpluginnamev10");
    }

    [Fact]
    public void NormalizePluginToken_WithNull_ShouldReturnEmpty()
    {
        // Arrange
        var method = GetPrivateStaticMethod("NormalizePluginToken");

        // Act
        var result = InvokePrivateStringMethod(method, null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizePluginToken_WithWhitespace_ShouldReturnEmpty()
    {
        // Arrange
        var method = GetPrivateStaticMethod("NormalizePluginToken");

        // Act
        var result = InvokePrivateStringMethod(method, "   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void NormalizePluginToken_WithOnlyLetters_ShouldReturnLowercase()
    {
        // Arrange
        var method = GetPrivateStaticMethod("NormalizePluginToken");

        // Act
        var result = InvokePrivateStringMethod(method, "TestPlugin");

        // Assert
        result.Should().Be("testplugin");
    }

    #endregion

    #region IsVersionCompatible Tests

    [Fact]
    public void IsVersionCompatible_WithValidMinimumVersion_ShouldCheckCompatibility()
    {
        // Arrange - Access private method via reflection
        var method = GetPrivateStaticMethod("IsVersionCompatible");

        // Act - Test with a reasonable minimum version
        var result = InvokePrivateBoolMethod(method, "1.0.0");

        // Assert - Should return true (current version >= 1.0.0)
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVersionCompatible_WithInvalidVersion_ShouldReturnTrue()
    {
        // Arrange
        var method = GetPrivateStaticMethod("IsVersionCompatible");

        // Act - Invalid version format should default to allowing
        var result = InvokePrivateBoolMethod(method, "invalid-version");

        // Assert - Returns true for backward compatibility
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVersionCompatible_WithEmptyVersion_ShouldReturnTrue()
    {
        // Arrange
        var method = GetPrivateStaticMethod("IsVersionCompatible");

        // Act
        var result = InvokePrivateBoolMethod(method, string.Empty);

        // Assert - Empty version should be allowed
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVersionCompatible_WithNullVersion_ShouldReturnTrue()
    {
        // Arrange
        var method = GetPrivateStaticMethod("IsVersionCompatible");

        // Act
        var result = InvokePrivateBoolMethod(method, null);

        // Assert - Null should be allowed
        result.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void PluginLoader_ShouldImplementIPluginLoader()
    {
        // Arrange & Act
        var loader = new PluginLoader();

        // Assert
        loader.Should().BeAssignableTo<IPluginLoader>();
    }

    [Fact]
    public async Task LoadFromFileAsync_WhenCalledMultipleTimes_ShouldNotCache()
    {
        // Arrange
        var dllPath = CreateTempFile("content");
        _mockSignatureValidator
            .Setup(v => v.ValidateAsync(dllPath))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Valid, null));

        // Act - Call twice
        var result1 = await _loader.LoadFromFileAsync(dllPath, _mockSignatureValidator.Object);
        var result2 = await _loader.LoadFromFileAsync(dllPath, _mockSignatureValidator.Object);

        // Assert - Both return null (invalid assembly), but validation is called each time
        result1.Should().BeNull();
        result2.Should().BeNull();
        _mockSignatureValidator.Verify(v => v.ValidateAsync(dllPath), Times.Exactly(2));
    }

    #endregion
}
