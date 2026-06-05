using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class PluginExtensionsPageGuardTests
{
    [Fact]
    public void InstalledUiCheck_ShouldTreatAnyManifestUiCapabilityAsInstalled()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml.cs");
        var methodStart = source.IndexOf("private bool IsPluginInstalledForUi", System.StringComparison.Ordinal);
        var nextMethodStart = source.IndexOf("private void ReconcileAvailableUpdatesWithInstalledVersions", System.StringComparison.Ordinal);
        methodStart.Should().BeGreaterThanOrEqualTo(0);
        nextMethodStart.Should().BeGreaterThan(methodStart);

        var method = source[methodStart..nextMethodStart];
        method.Should().Contain("ResolveFromInstalledManifest(pluginId)");
        method.Should().Contain(".HasAny;");
        method.Should().NotContain(".SupportsOptimizationCategory;");
    }

    [Fact]
    public void LoadedHandler_ShouldResubscribeInstallCoordinatorAfterUnload()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml.cs");

        source.Should().Contain("private bool _isPluginInstallCoordinatorSubscribed;");
        source.Should().Contain("AttachPluginInstallCoordinator();");
        source.Should().Contain("DetachPluginInstallCoordinator();");
        source.Should().Contain("if (_isPluginInstallCoordinatorSubscribed)");
        source.Should().Contain("if (!_isPluginInstallCoordinatorSubscribed)");

        var unloadStart = source.IndexOf("private void PluginExtensionsPage_Unloaded", System.StringComparison.Ordinal);
        var changedStart = source.IndexOf("private void PluginInstallCoordinator_Changed", System.StringComparison.Ordinal);
        unloadStart.Should().BeGreaterThanOrEqualTo(0);
        changedStart.Should().BeGreaterThan(unloadStart);

        var lifecycleBlock = source[unloadStart..changedStart];
        lifecycleBlock.Should().Contain("DetachPluginInstallCoordinator();");
        lifecycleBlock.Should().Contain("_pluginInstallCoordinator.Changed +=");
        lifecycleBlock.Should().Contain("_pluginInstallCoordinator.Changed -=");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine([root, .. pathParts]));
    }
}
