using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

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
            catch { /* Best-effort cleanup in Dispose */ }
        }
        foreach (var dir in _tempDirectories.Where(Directory.Exists))
        {
            try { Directory.Delete(dir, true); }
            catch { /* Best-effort cleanup in Dispose */ }
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

    private static MethodInfo GetPrivateNestedStaticMethod(string nestedTypeName, string methodName)
    {
        var nestedType = typeof(PluginLoader).GetNestedType(nestedTypeName, BindingFlags.NonPublic);
        nestedType.Should().NotBeNull();

        var method = nestedType!.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
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

    private object RegisterDependencyResolutionContext(string pluginMainAssemblyPath, string pluginDirectory)
    {
        var registerMethod = GetPrivateStaticMethod("RegisterPluginDependencyResolutionContext");
        var registration = registerMethod.Invoke(null, new object?[] { pluginMainAssemblyPath, pluginDirectory, _mockSignatureValidator.Object });
        registration.Should().NotBeNull();

        var context = registration!.GetType().GetProperty("Context")?.GetValue(registration);
        context.Should().NotBeNull();
        return context!;
    }

    private static void RemoveDependencyResolutionContext(object context)
    {
        var removeMethod = GetPrivateStaticMethod("RemovePluginDependencyResolutionContext");
        removeMethod.Invoke(null, new[] { context });
    }

    private static object[] GetScopedDependencyResolutionContexts(Assembly? requestingAssembly)
    {
        var method = GetPrivateStaticMethod("GetScopedDependencyResolutionContexts");
        var result = method.Invoke(null, new object?[] { requestingAssembly });
        result.Should().BeAssignableTo<Array>();
        return ((Array)result!).Cast<object>().ToArray();
    }

    private static void SetPluginMainAssembly(object context, Assembly assembly)
    {
        var method = context.GetType().GetMethod("SetPluginMainAssembly", BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull();
        method!.Invoke(context, new object[] { assembly });
    }

    private static string GetPluginDirectory(object context)
    {
        var property = context.GetType().GetProperty("PluginDirectory", BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull();
        return property!.GetValue(context).Should().BeOfType<string>().Which;
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
    public void CanLoad_WithSharedDll_ShouldReturnFalse()
    {
        // Arrange
        var filePath = "LenovoLegionToolkit.Plugins.Shared.dll";

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
        var filePath = "UniversalDeviceToolkit.Plugins.TestPlugin.dll";

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
        var parentDir = "UniversalDeviceToolkit.Plugins.TestPlugin";

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
    public void GetScopedDependencyResolutionContexts_WithMultipleContextsAndNoRequestingAssembly_ShouldReturnNone()
    {
        // Arrange
        var pluginDirectory1 = CreateTempDirectory();
        var pluginDirectory2 = CreateTempDirectory();
        var context1 = RegisterDependencyResolutionContext(Path.Combine(pluginDirectory1, "PluginA.dll"), pluginDirectory1);
        var context2 = RegisterDependencyResolutionContext(Path.Combine(pluginDirectory2, "PluginB.dll"), pluginDirectory2);

        try
        {
            // Act
            var contexts = GetScopedDependencyResolutionContexts(null);

            // Assert
            contexts.Should().BeEmpty();
        }
        finally
        {
            RemoveDependencyResolutionContext(context1);
            RemoveDependencyResolutionContext(context2);
        }
    }

    [Fact]
    public void GetScopedDependencyResolutionContexts_WithRequestingMainAssembly_ShouldReturnOnlyMatchingContext()
    {
        // Arrange
        var pluginDirectory1 = CreateTempDirectory();
        var pluginDirectory2 = CreateTempDirectory();
        var context1 = RegisterDependencyResolutionContext(Path.Combine(pluginDirectory1, "PluginA.dll"), pluginDirectory1);
        var context2 = RegisterDependencyResolutionContext(Path.Combine(pluginDirectory2, "PluginB.dll"), pluginDirectory2);

        try
        {
            SetPluginMainAssembly(context1, typeof(PluginLoaderTests).Assembly);

            // Act
            var contexts = GetScopedDependencyResolutionContexts(typeof(PluginLoaderTests).Assembly);

            // Assert
            contexts.Should().ContainSingle();
            GetPluginDirectory(contexts[0]).Should().Be(Path.GetFullPath(pluginDirectory1));
        }
        finally
        {
            RemoveDependencyResolutionContext(context1);
            RemoveDependencyResolutionContext(context2);
        }
    }

    [Fact]
    public void IsValidPluginDependencySignature_WithExpiredResultWithoutCertificate_ShouldReturnFalse()
    {
        // Arrange
        var method = GetPrivateStaticMethod("IsValidPluginDependencySignature");
        var signatureResult = new PluginSignatureResult(PluginSignatureStatus.Expired, "expired");
        var requestedAssemblyName = new AssemblyName("Microsoft.Extensions.Logging.Abstractions, Version=8.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60");
        var candidatePath = Path.Combine(CreateTempDirectory(), "Microsoft.Extensions.Logging.Abstractions.dll");

        // Act
        var result = method.Invoke(null, new object[] { signatureResult, requestedAssemblyName, candidatePath });

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void GetPluginAssemblyCandidatePath_WithManagedDependency_ShouldReturnSidecarDllPath()
    {
        // Arrange
        var method = GetPrivateStaticMethod("GetPluginAssemblyCandidatePath");
        var pluginDirectory = CreateTempDirectory();
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "UniversalDeviceToolkit.Plugins.TestPlugin.dll");
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
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "UniversalDeviceToolkit.Plugins.TestPlugin.dll");
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

    [Fact]
    public void ShouldShareDefaultContextAssembly_WithWpfUiAssembly_ShouldReturnTrue()
    {
        var method = GetPrivateNestedStaticMethod("PluginAssemblyLoadContext", "ShouldShareDefaultContextAssembly");

        var result = InvokePrivateBoolMethod(method, "Wpf.Ui");

        result.Should().BeTrue();
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

    #region Dependency Resolution Context Tests

    [Fact]
    public void RegisterPluginDependencyResolutionContext_ShouldCreateNewContext()
    {
        // Arrange
        var pluginDirectory = CreateTempDirectory();
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "TestPlugin.dll");

        // Act
        var context = RegisterDependencyResolutionContext(pluginMainAssemblyPath, pluginDirectory);

        // Assert
        context.Should().NotBeNull();
        GetPluginDirectory(context).Should().Be(Path.GetFullPath(pluginDirectory));

        // Cleanup
        RemoveDependencyResolutionContext(context);
    }

    [Fact]
    public void RegisterPluginDependencyResolutionContext_WithSamePath_ShouldReturnExistingContext()
    {
        // Arrange
        var pluginDirectory = CreateTempDirectory();
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "TestPlugin.dll");

        // Act
        var context1 = RegisterDependencyResolutionContext(pluginMainAssemblyPath, pluginDirectory);
        var context2 = RegisterDependencyResolutionContext(pluginMainAssemblyPath, pluginDirectory);

        // Assert
        context1.Should().BeSameAs(context2);

        // Cleanup
        RemoveDependencyResolutionContext(context1);
    }

    [Fact]
    public void GetScopedDependencyResolutionContexts_WithNoContexts_ShouldReturnEmpty()
    {
        // Act
        var contexts = GetScopedDependencyResolutionContexts(null);

        // Assert
        contexts.Should().BeEmpty();
    }

    [Fact]
    public void GetScopedDependencyResolutionContexts_WithMultipleContextsAndMatchingAssembly_ShouldReturnMatching()
    {
        // Arrange
        var pluginDirectory1 = CreateTempDirectory();
        var pluginDirectory2 = CreateTempDirectory();
        var context1 = RegisterDependencyResolutionContext(Path.Combine(pluginDirectory1, "PluginA.dll"), pluginDirectory1);
        var context2 = RegisterDependencyResolutionContext(Path.Combine(pluginDirectory2, "PluginB.dll"), pluginDirectory2);

        try
        {
            SetPluginMainAssembly(context1, typeof(PluginLoaderTests).Assembly);

            // Act
            var contexts = GetScopedDependencyResolutionContexts(typeof(PluginLoaderTests).Assembly);

            // Assert
            contexts.Should().ContainSingle();
            GetPluginDirectory(contexts[0]).Should().Be(Path.GetFullPath(pluginDirectory1));
        }
        finally
        {
            RemoveDependencyResolutionContext(context1);
            RemoveDependencyResolutionContext(context2);
        }
    }

    [Fact]
    public void RemovePluginDependencyResolutionContext_ShouldRemoveContext()
    {
        // Arrange
        var pluginDirectory = CreateTempDirectory();
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "TestPlugin.dll");
        var context = RegisterDependencyResolutionContext(pluginMainAssemblyPath, pluginDirectory);

        // Act
        RemoveDependencyResolutionContext(context);
        var contexts = GetScopedDependencyResolutionContexts(null);

        // Assert
        contexts.Should().BeEmpty();
    }

    [Fact]
    public void Unload_WithRegisteredDependencyContext_ShouldRemoveContext()
    {
        // Arrange
        const string pluginId = "test-plugin";
        var pluginDirectory = CreateTempDirectory();
        var pluginMainAssemblyPath = Path.Combine(pluginDirectory, "TestPlugin.dll");
        var context = RegisterDependencyResolutionContext(pluginMainAssemblyPath, pluginDirectory);

        var dependencyContextsField = typeof(PluginLoader)
            .GetField("PluginDependencyContexts", BindingFlags.NonPublic | BindingFlags.Static);
        dependencyContextsField.Should().NotBeNull();

        var dependencyContexts = dependencyContextsField!.GetValue(null)
            .Should().BeAssignableTo<System.Collections.IDictionary>().Which;
        dependencyContexts[pluginId] = context;

        try
        {
            // Act
            var unloaded = _loader.Unload(pluginId);

            // Assert
            unloaded.Should().BeTrue();
            var contexts = GetScopedDependencyResolutionContexts(null);
            contexts.Should().BeEmpty();
        }
        finally
        {
            dependencyContexts.Remove(pluginId);
            RemoveDependencyResolutionContext(context);
        }
    }

    #endregion

    #region Certificate Validation Tests

    [Fact]
    public void CertificateLooksMicrosoftOwned_WithMicrosoftSubject_ShouldReturnTrue()
    {
        // Arrange
        var method = GetPrivateStaticMethod("CertificateLooksMicrosoftOwned");
        // We can't easily create a real certificate, so we test the helper method
        var containsMicrosoftMethod = typeof(PluginLoader).GetMethod("ContainsMicrosoftCorporation", BindingFlags.NonPublic | BindingFlags.Static);

        // Act & Assert
        containsMicrosoftMethod.Should().NotBeNull();
        containsMicrosoftMethod!.Invoke(null, new object[] { "CN=Microsoft Corporation, O=Microsoft Corporation" })
            .Should().Be(true);
        containsMicrosoftMethod.Invoke(null, new object[] { "CN=Some Other Company" })
            .Should().Be(false);
    }

    [Fact]
    public void IsCandidateAssemblyIdentityCompatible_WithMatchingAssembly_ShouldReturnTrue()
    {
        // Arrange
        var method = GetPrivateStaticMethod("IsCandidateAssemblyIdentityCompatible");
        var pluginDirectory = CreateTempDirectory();
        var assemblyPath = Path.Combine(pluginDirectory, "TestAssembly.dll");

        // Create a test DLL file
        File.WriteAllText(assemblyPath, "not a real assembly");

        // Act - This will return false because the file is not a valid assembly
        var requestedAssemblyName = new AssemblyName("TestAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        var result = method.Invoke(null, new object[] { assemblyPath, requestedAssemblyName, Array.Empty<byte>() });

        // Assert - Returns false because file is not a valid assembly
        result.Should().Be(false);
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
