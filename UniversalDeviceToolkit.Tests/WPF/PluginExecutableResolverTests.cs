using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class PluginExecutableResolverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"llt-plugin-executable-resolver-{Guid.NewGuid():N}");

    public PluginExecutableResolverTests()
    {
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void TryResolve_ShouldReturnExplicitPluginExecutable()
    {
        const string pluginId = "custom-mouse";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        var expectedExe = Path.Combine(pluginDirectory, $"{pluginId}.exe");
        File.WriteAllText(expectedExe, string.Empty);

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, null, pluginsDirectory);

        resolved.Should().BeTrue();
        exeFile.Should().Be(expectedExe);
        workingDirectory.Should().Be(pluginDirectory);
    }

    [Fact]
    public void TryResolve_ShouldIgnoreArbitraryBundledExecutable()
    {
        const string pluginId = "vive-tool";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        var bundledExe = Path.Combine(pluginDirectory, "ViVeTool.exe");
        File.WriteAllText(bundledExe, string.Empty);

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, null, pluginsDirectory);

        resolved.Should().BeFalse();
        exeFile.Should().BeNull();
        workingDirectory.Should().BeNull();
    }

    [Fact]
    public void TryResolve_ShouldFindExecutableInLocalSubdirectory()
    {
        const string pluginId = "test-plugin";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        var localPluginDirectory = Path.Combine(pluginsDirectory, "local", pluginId);
        Directory.CreateDirectory(localPluginDirectory);

        var expectedExe = Path.Combine(localPluginDirectory, $"{pluginId}.exe");
        File.WriteAllText(expectedExe, string.Empty);

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, null, pluginsDirectory);

        resolved.Should().BeTrue();
        exeFile.Should().Be(expectedExe);
        workingDirectory.Should().Be(localPluginDirectory);
    }

    [Fact]
    public void TryResolve_ShouldFindExecutableWithPluginPrefix()
    {
        const string pluginId = "test-plugin";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        var expectedExe = Path.Combine(pluginDirectory, $"UniversalDeviceToolkit.Plugins.{pluginId}.exe");
        File.WriteAllText(expectedExe, string.Empty);

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, null, pluginsDirectory);

        resolved.Should().BeTrue();
        exeFile.Should().Be(expectedExe);
        workingDirectory.Should().Be(pluginDirectory);
    }

    [Fact]
    public void TryResolve_ShouldFindExecutableWithNormalizedPluginId()
    {
        const string pluginId = "test-plugin";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        var expectedExe = Path.Combine(pluginDirectory, "UniversalDeviceToolkit.Plugins.testplugin.exe");
        File.WriteAllText(expectedExe, string.Empty);

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, null, pluginsDirectory);

        resolved.Should().BeTrue();
        exeFile.Should().Be(expectedExe);
        workingDirectory.Should().Be(pluginDirectory);
    }

    [Fact]
    public void TryResolve_ShouldPreferMetadataDirectoryOverPluginsDirectory()
    {
        const string pluginId = "test-plugin";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        var metadataDirectory = Path.Combine(_root, "metadata", pluginId);
        Directory.CreateDirectory(metadataDirectory);

        var pluginsExe = Path.Combine(pluginDirectory, $"{pluginId}.exe");
        File.WriteAllText(pluginsExe, "plugins");

        var metadataExe = Path.Combine(metadataDirectory, $"{pluginId}.exe");
        File.WriteAllText(metadataExe, "metadata");

        var metadataFilePath = Path.Combine(metadataDirectory, "plugin.json");
        File.WriteAllText(metadataFilePath, "{}");

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, metadataFilePath, pluginsDirectory);

        resolved.Should().BeTrue();
        exeFile.Should().Be(metadataExe);
        workingDirectory.Should().Be(metadataDirectory);
    }

    [Fact]
    public void TryResolve_ShouldReturnFalseWhenNoExecutableFound()
    {
        const string pluginId = "nonexistent-plugin";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        Directory.CreateDirectory(pluginsDirectory);

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, null, pluginsDirectory);

        resolved.Should().BeFalse();
        exeFile.Should().BeNull();
        workingDirectory.Should().BeNull();
    }

    [Fact]
    public void TryResolve_ShouldHandlePluginIdWithHyphens()
    {
        const string pluginId = "my-awesome-plugin";
        var pluginsDirectory = Path.Combine(_root, "plugins");
        var pluginDirectory = Path.Combine(pluginsDirectory, pluginId);
        Directory.CreateDirectory(pluginDirectory);

        var expectedExe = Path.Combine(pluginDirectory, $"{pluginId}.exe");
        File.WriteAllText(expectedExe, string.Empty);

        var (resolved, exeFile, workingDirectory) = InvokeTryResolve(pluginId, null, pluginsDirectory);

        resolved.Should().BeTrue();
        exeFile.Should().Be(expectedExe);
        workingDirectory.Should().Be(pluginDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static (bool resolved, string? exeFile, string? workingDirectory) InvokeTryResolve(
        string pluginId,
        string? metadataFilePath,
        string pluginsDirectory)
    {
        var resolverType = typeof(UniversalDeviceToolkit.WPF.Pages.PluginExtensionsPage).Assembly
            .GetType("UniversalDeviceToolkit.WPF.Utils.PluginExecutableResolver");
        var method = resolverType?.GetMethod("TryResolve", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        object?[] parameters = [pluginId, metadataFilePath, pluginsDirectory, null, null, true, true];
        var result = method!.Invoke(null, parameters);

        result.Should().BeOfType<bool>();
        return ((bool)result!, parameters[3] as string, parameters[4] as string);
    }
}
