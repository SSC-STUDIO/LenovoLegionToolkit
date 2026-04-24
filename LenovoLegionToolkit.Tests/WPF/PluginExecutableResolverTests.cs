using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace LenovoLegionToolkit.Tests.WPF;

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
        const string pluginId = "network-acceleration";
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
        var resolverType = typeof(LenovoLegionToolkit.WPF.Pages.PluginExtensionsPage).Assembly
            .GetType("LenovoLegionToolkit.WPF.Utils.PluginExecutableResolver");
        var method = resolverType?.GetMethod("TryResolve", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();

        object?[] parameters = [pluginId, metadataFilePath, pluginsDirectory, null, null];
        var result = method!.Invoke(null, parameters);

        result.Should().BeOfType<bool>();
        return ((bool)result!, parameters[3] as string, parameters[4] as string);
    }
}
