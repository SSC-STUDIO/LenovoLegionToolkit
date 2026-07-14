using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginSandboxTests : TemporaryFileTestBase
{
    private static PluginSandbox CreateSandbox() => new();

    [Fact]
    public void CreateSandbox_WithEmptyPluginId_ShouldThrow()
    {
        using var sandbox = CreateSandbox();
        Action act = () => sandbox.CreateSandbox(" ", CreateTempFile(), new SandboxConfiguration());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateSandbox_WhenAssemblyMissing_ShouldReturnFalse()
    {
        using var sandbox = CreateSandbox();
        var result = sandbox.CreateSandbox("test-plugin", Path.Combine(CreateTempDirectory(), "missing.dll"), new SandboxConfiguration());

        result.Should().BeFalse();
    }

    [Fact]
    public void CreateSandbox_WhenDuplicate_ShouldReturnFalse()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        var config = new SandboxConfiguration { Permissions = SandboxPermission.FileSystemRead };

        sandbox.CreateSandbox("dup-plugin", assemblyPath, config).Should().BeTrue();
        sandbox.CreateSandbox("dup-plugin", assemblyPath, config).Should().BeFalse();
    }

    [Fact]
    public void LoadPlugin_WhenSandboxMissing_ShouldReturnNull()
    {
        using var sandbox = CreateSandbox();
        sandbox.LoadPlugin("missing").Should().BeNull();
    }

    [Fact]
    public void LoadPlugin_WithTestAssembly_ShouldLoadPlugin()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        var config = new SandboxConfiguration { Permissions = SandboxPermission.None, MaxMemoryMB = 512 };

        sandbox.CreateSandbox(SandboxTestPlugin.PluginId, assemblyPath, config).Should().BeTrue();

        var plugin = sandbox.LoadPlugin(SandboxTestPlugin.PluginId);

        plugin.Should().NotBeNull();
        plugin!.Id.Should().Be(SandboxTestPlugin.PluginId);
        sandbox.LoadPlugin(SandboxTestPlugin.PluginId).Should().BeSameAs(plugin);
    }

    [Fact]
    public void ExecuteInSandbox_WhenSandboxMissing_ShouldReturnFailure()
    {
        using var sandbox = CreateSandbox();
        var result = sandbox.ExecuteInSandbox("missing", () => 42);

        result.Success.Should().BeFalse();
        result.WasBlocked.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Sandbox not found");
    }

    [Fact]
    public void ExecuteInSandbox_WhenOperationSucceeds_ShouldReturnData()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        sandbox.CreateSandbox(SandboxTestPlugin.PluginId, assemblyPath, new SandboxConfiguration { MaxMemoryMB = 512 });

        var result = sandbox.ExecuteInSandbox(SandboxTestPlugin.PluginId, () => "ok");

        result.Success.Should().BeTrue();
        result.Data.Should().Be("ok");
        result.WasBlocked.Should().BeFalse();
    }

    [Fact]
    public void ExecuteInSandbox_WhenSecurityExceptionThrown_ShouldMarkBlocked()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        sandbox.CreateSandbox(SandboxTestPlugin.PluginId, assemblyPath, new SandboxConfiguration { MaxMemoryMB = 512 });
        SandboxViolationEventArgs? violation = null;
        sandbox.SandboxViolation += (_, args) => violation = args;

        var result = sandbox.ExecuteInSandbox(SandboxTestPlugin.PluginId, () => throw new SecurityException("blocked"));

        result.Success.Should().BeFalse();
        result.WasBlocked.Should().BeTrue();
        violation.Should().NotBeNull();
        violation!.PluginId.Should().Be(SandboxTestPlugin.PluginId);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_WhenOperationTimesOut_ShouldReturnBlocked()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        sandbox.CreateSandbox(SandboxTestPlugin.PluginId, assemblyPath, new SandboxConfiguration
        {
            MaxMemoryMB = 512,
            OperationTimeoutSeconds = 1
        });

        var result = await sandbox.ExecuteInSandboxAsync(
            SandboxTestPlugin.PluginId,
            async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return (object?)null;
            });

        result.Success.Should().BeFalse();
        result.WasBlocked.Should().BeTrue();
        result.ErrorMessage.Should().Contain("timed out");
    }

    [Fact]
    public void HasPermission_WhenGranted_ShouldReturnTrue()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        sandbox.CreateSandbox(
            SandboxTestPlugin.PluginId,
            assemblyPath,
            new SandboxConfiguration { Permissions = SandboxPermission.FileSystemRead | SandboxPermission.NetworkAccess, MaxMemoryMB = 512 });

        sandbox.HasPermission(SandboxTestPlugin.PluginId, SandboxPermission.FileSystemRead).Should().BeTrue();
        sandbox.HasPermission(SandboxTestPlugin.PluginId, SandboxPermission.HardwareAccess).Should().BeFalse();
    }

    [Fact]
    public void UnloadAndDestroySandbox_ShouldRemoveSandbox()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        sandbox.CreateSandbox(SandboxTestPlugin.PluginId, assemblyPath, new SandboxConfiguration { MaxMemoryMB = 512 });
        sandbox.LoadPlugin(SandboxTestPlugin.PluginId).Should().NotBeNull();

        sandbox.UnloadPlugin(SandboxTestPlugin.PluginId).Should().BeTrue();
        sandbox.DestroySandbox(SandboxTestPlugin.PluginId).Should().BeTrue();
        sandbox.GetPluginInfo(SandboxTestPlugin.PluginId).Should().BeNull();
    }

    [Fact]
    public void UpdateConfiguration_WhenSandboxExists_ShouldPersistConfiguration()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        sandbox.CreateSandbox(SandboxTestPlugin.PluginId, assemblyPath, new SandboxConfiguration { Permissions = SandboxPermission.None, MaxMemoryMB = 512 });

        var updated = new SandboxConfiguration { Permissions = SandboxPermission.UICustomization, MaxMemoryMB = 256 };
        sandbox.UpdateConfiguration(SandboxTestPlugin.PluginId, updated).Should().BeTrue();

        sandbox.HasPermission(SandboxTestPlugin.PluginId, SandboxPermission.UICustomization).Should().BeTrue();
    }

    [Fact]
    public void GetAllSandboxedPlugins_ShouldReturnActiveEntries()
    {
        using var sandbox = CreateSandbox();
        var assemblyPath = typeof(SandboxTestPlugin).Assembly.Location;
        sandbox.CreateSandbox(SandboxTestPlugin.PluginId, assemblyPath, new SandboxConfiguration { MaxMemoryMB = 512 });
        sandbox.LoadPlugin(SandboxTestPlugin.PluginId);

        var plugins = sandbox.GetAllSandboxedPlugins();

        plugins.Should().ContainSingle(info => info.PluginId == SandboxTestPlugin.PluginId && info.IsActive);
    }
}

public sealed class SandboxTestPlugin : IPlugin
{
    public const string PluginId = "sandbox-test-plugin";

    public string Id => PluginId;
    public string Name => "Sandbox Test Plugin";
    public string Description => "Test plugin for sandbox coverage";
    public string Icon => "PlugConnected24";
    public bool IsSystemPlugin => false;
    public string[]? Dependencies => null;

    public void OnInstalled() { }
    public void OnUninstalled() { }
    public void OnShutdown() { }
    public void Stop() { }
}
