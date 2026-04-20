using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Moq;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginManagerSecurityTests : TemporaryFileTestBase
{
    [Fact]
    public void ResolveDependencyAssembly_WhenSignatureValidationFails_ShouldNotLoadAssembly()
    {
        // Arrange
        var pluginsDirectory = CreateTempDirectory();
        var dependencyPath = Path.Combine(pluginsDirectory, "Injected.Dependency.dll");
        File.Copy(typeof(PluginManagerSecurityTests).Assembly.Location, dependencyPath);
        TempFiles.Add(dependencyPath);

        var signatureValidatorMock = new Mock<IPluginSignatureValidator>();
        signatureValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Invalid, "invalid signature"));

        var manager = CreatePluginManager(signatureValidatorMock.Object);
        var method = GetPrivateInstanceMethod(nameof(PluginManager), "ResolveDependencyAssembly");

        // Act
        var resolvedAssembly = (Assembly?)method.Invoke(manager, new object[] { pluginsDirectory, "Injected.Dependency" });

        // Assert
        resolvedAssembly.Should().BeNull();
        signatureValidatorMock.Verify(v => v.ValidateAsync(Path.GetFullPath(dependencyPath)), Times.Once);
    }

    [Fact]
    public void ResolveSatelliteAssembly_WhenSignatureValidationFails_ShouldNotLoadAssembly()
    {
        // Arrange
        var pluginsDirectory = CreateTempDirectory();
        var pluginDirectory = Path.Combine(pluginsDirectory, "plugin-a");
        var cultureDirectory = Path.Combine(pluginDirectory, "fr");
        Directory.CreateDirectory(cultureDirectory);
        TempDirectories.Add(pluginDirectory);

        var satellitePath = Path.Combine(cultureDirectory, "Injected.resources.dll");
        File.Copy(typeof(PluginManagerSecurityTests).Assembly.Location, satellitePath);
        TempFiles.Add(satellitePath);

        var signatureValidatorMock = new Mock<IPluginSignatureValidator>();
        signatureValidatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<string>()))
            .ReturnsAsync(new PluginSignatureResult(PluginSignatureStatus.Invalid, "invalid signature"));

        var manager = CreatePluginManager(signatureValidatorMock.Object);
        var method = GetPrivateInstanceMethod(nameof(PluginManager), "ResolveSatelliteAssembly");
        var requestedAssembly = new AssemblyName("Injected.resources, Culture=fr");

        // Act
        var resolvedAssembly = (Assembly?)method.Invoke(manager, new object[] { pluginsDirectory, requestedAssembly, "Injected.resources" });

        // Assert
        resolvedAssembly.Should().BeNull();
        signatureValidatorMock.Verify(v => v.ValidateAsync(Path.GetFullPath(satellitePath)), Times.Once);
    }

    private static PluginManager CreatePluginManager(IPluginSignatureValidator signatureValidator)
    {
        var loaderMock = new Mock<IPluginLoader>();
        var registry = new PluginRegistry();
        var fileSystemManagerMock = new Mock<IPluginFileSystemManager>();
        fileSystemManagerMock
            .Setup(m => m.GetCultureFolders())
            .Returns(new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ar", "bg", "bs", "ca", "cs", "de", "el", "es", "fr", "hu", "it", "ja", "ko",
                "lv", "nl-nl", "pl", "pt", "pt-br", "ro", "ru", "sk", "tr", "uk", "uz-latn-uz",
                "vi", "zh-hans", "zh-hant", "tools"
            });

        return new PluginManager(
            new LenovoLegionToolkit.Lib.Settings.ApplicationSettings(),
            signatureValidator,
            loaderMock.Object,
            registry,
            fileSystemManagerMock.Object);
    }

    private static MethodInfo GetPrivateInstanceMethod(string typeName, string methodName)
    {
        var type = typeof(PluginManager);
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull($"expected to find private method {typeName}.{methodName}");
        return method!;
    }
}
