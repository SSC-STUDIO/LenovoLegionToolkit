using System;
using System.IO;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginPathsTests : TemporaryFileTestBase
{
    private readonly string? _previousAppDataOverride;
    private readonly string? _previousPluginsOverride;

    public PluginPathsTests()
    {
        _previousAppDataOverride = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        _previousPluginsOverride = Environment.GetEnvironmentVariable(PluginPaths.PluginsDirectoryOverrideEnvironmentVariable);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, CreateTempDirectory());
    }

    public override void Dispose()
    {
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, _previousAppDataOverride);
        Environment.SetEnvironmentVariable(PluginPaths.PluginsDirectoryOverrideEnvironmentVariable, _previousPluginsOverride);
        Environment.SetEnvironmentVariable(PluginPaths.LegacyPluginsDirectoryOverrideEnvironmentVariable, null);
        base.Dispose();
    }

    [Fact]
    public void GetPluginsDirectory_WithOverride_ShouldReturnOverridePath()
    {
        var overrideDir = CreateTempDirectory();
        Environment.SetEnvironmentVariable(PluginPaths.PluginsDirectoryOverrideEnvironmentVariable, overrideDir);

        var result = PluginPaths.GetPluginsDirectory();

        result.Should().Be(Path.GetFullPath(overrideDir));
        Directory.Exists(result).Should().BeTrue();
    }

    [Fact]
    public void GetPluginsDirectoryOverride_WithLegacyVariable_ShouldResolveOverride()
    {
        var overrideDir = CreateTempDirectory();
        Environment.SetEnvironmentVariable(PluginPaths.LegacyPluginsDirectoryOverrideEnvironmentVariable, overrideDir);

        var result = PluginPaths.GetPluginsDirectoryOverride();

        result.Should().Be(Path.GetFullPath(overrideDir));
    }

    [Fact]
    public void GetPluginDirectory_ShouldCombineRootAndId()
    {
        var overrideDir = CreateTempDirectory();
        Environment.SetEnvironmentVariable(PluginPaths.PluginsDirectoryOverrideEnvironmentVariable, overrideDir);

        var pluginDir = PluginPaths.GetPluginDirectory("custom-mouse");

        pluginDir.Should().Be(Path.Combine(Path.GetFullPath(overrideDir), "custom-mouse"));
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
        var overrideDir = CreateTempDirectory();
        Environment.SetEnvironmentVariable(PluginPaths.PluginsDirectoryOverrideEnvironmentVariable, overrideDir);

        var directories = PluginPaths.GetAllPossiblePluginsDirectories();

        directories.Should().Contain(Path.GetFullPath(overrideDir));
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
        var overrideDir = CreateTempDirectory();
        Environment.SetEnvironmentVariable(PluginPaths.PluginsDirectoryOverrideEnvironmentVariable, overrideDir);
        const string pluginId = "shell-integration";

        PluginPaths.GetPluginResourcesDirectory(pluginId)
            .Should().Be(Path.Combine(Path.GetFullPath(overrideDir), pluginId, "Resources"));
        PluginPaths.GetPluginConfigFilePath(pluginId)
            .Should().Be(Path.Combine(Path.GetFullPath(overrideDir), pluginId, "config.json"));
    }
}
