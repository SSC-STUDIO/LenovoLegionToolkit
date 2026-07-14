using System;
using System.IO;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginPathsTests : TemporaryFileTestBase
{
    private readonly string? _previousAppDataOverride;

    public PluginPathsTests()
    {
        _previousAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
    }

    public override void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _previousAppDataOverride);
        base.Dispose();
    }

    [Fact]
    public void GetPluginsDirectory_ShouldReturnAppDataPluginsPath()
    {
        var expectedRoot = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        expectedRoot.Should().NotBeNullOrWhiteSpace();

        var result = PluginPaths.GetPluginsDirectory();

        result.Should().Be(Path.Combine(Path.GetFullPath(expectedRoot!), PluginPaths.PluginsDirectoryName));
        Directory.Exists(result).Should().BeTrue();
    }

    [Fact]
    public void GetPluginDirectory_ShouldCombineRootAndId()
    {
        var pluginsRoot = PluginPaths.GetPluginsDirectory();

        var pluginDir = PluginPaths.GetPluginDirectory("custom-mouse");

        pluginDir.Should().Be(Path.Combine(pluginsRoot, "custom-mouse"));
    }

    [Fact]
    public void GetPluginDirectory_WithEmptyId_ShouldThrow()
    {
        Action act = () => PluginPaths.GetPluginDirectory(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetAllPossiblePluginsDirectories_ShouldIncludeAppDataDirectory()
    {
        var directories = PluginPaths.GetAllPossiblePluginsDirectories();

        directories.Should().Contain(PluginPaths.GetPluginsDirectory());
        directories.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void GetPluginAssemblyFiles_WhenDirectoryMissing_ShouldReturnEmpty()
    {
        var missingDir = Path.Combine(CreateTempDirectory(), "missing-plugin");

        PluginPaths.GetPluginAssemblyFiles(missingDir).Should().BeEmpty();
    }

    [Fact]
    public void GetPluginAssemblyFiles_WhenDllPresent_ShouldReturnDll()
    {
        var pluginDir = CreateTempDirectory();
        var dllPath = Path.Combine(pluginDir, "Example.Plugin.dll");
        File.WriteAllText(dllPath, "stub");

        PluginPaths.GetPluginAssemblyFiles(pluginDir).Should().ContainSingle(path => path == dllPath);
    }

    [Fact]
    public void GetPluginMetadataFilePath_WhenMissing_ShouldReturnNull()
    {
        var pluginDir = CreateTempDirectory();

        PluginPaths.GetPluginMetadataFilePath(pluginDir).Should().BeNull();
    }

    [Fact]
    public void GetPluginMetadataFilePath_WhenPresent_ShouldReturnPath()
    {
        var pluginDir = CreateTempDirectory();
        var metadataPath = Path.Combine(pluginDir, PluginPaths.PluginMetadataFileName);
        File.WriteAllText(metadataPath, "{}");

        PluginPaths.GetPluginMetadataFilePath(pluginDir).Should().Be(metadataPath);
    }

    [Fact]
    public void ContainsPlugin_WhenNoDlls_ShouldReturnFalse()
    {
        var pluginDir = CreateTempDirectory();

        PluginPaths.ContainsPlugin(pluginDir).Should().BeFalse();
    }

    [Fact]
    public void ContainsPlugin_WhenDllPresent_ShouldReturnTrue()
    {
        var pluginDir = CreateTempDirectory();
        File.WriteAllText(Path.Combine(pluginDir, "Example.Plugin.dll"), "stub");

        PluginPaths.ContainsPlugin(pluginDir).Should().BeTrue();
    }

    [Fact]
    public void GetPluginResourcesAndConfigPaths_ShouldUsePluginDirectory()
    {
        const string pluginId = "shell-integration";
        var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);

        PluginPaths.GetPluginResourcesDirectory(pluginId)
            .Should().Be(Path.Combine(pluginDirectory, "Resources"));
        PluginPaths.GetPluginConfigFilePath(pluginId)
            .Should().Be(Path.Combine(pluginDirectory, "config.json"));
    }
}
