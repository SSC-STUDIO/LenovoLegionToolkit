using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [Fact]
    public void LoadingChrome_ShouldShowSkeletonImmediateOnFirstStep()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml.cs");
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml");

        // First paint path: snap-in skeleton, never fade-from-0 blank.
        source.Should().Contain("ShowSkeletonImmediate");
        source.Should().Contain("SkeletonShimmer.RestartSubtree");
        source.Should().Contain("SkeletonShimmer.StopSubtree");
        source.Should().Contain("MinSkeletonVisible");
        source.Should().Contain("TimeSpan.FromMilliseconds(520)");
        source.Should().Contain("ShowSkeletonImmediate();");
        source.Should().Contain("OnlineFetchTimeout");
        source.Should().Contain("PluginPageUiState");
        source.Should().Contain("_pageLoadVersion");

        var loadedStart = source.IndexOf("private async void PluginExtensionsPage_Loaded", StringComparison.Ordinal);
        loadedStart.Should().BeGreaterThanOrEqualTo(0);
        var loadedEnd = source.IndexOf("private void PluginExtensionsPage_IsVisibleChanged", loadedStart, StringComparison.Ordinal);
        loadedEnd.Should().BeGreaterThan(loadedStart);
        var loaded = source[loadedStart..loadedEnd];
        // Every load (cold + hot re-entry) shows skeleton before rebuild — not first-open-only.
        loaded.IndexOf("ShowSkeletonImmediate()", StringComparison.Ordinal)
            .Should().BeLessThan(loaded.IndexOf("RebuildPluginListWithoutBlockingAsync()", StringComparison.Ordinal));
        loaded.Should().Contain("_hasStartedInitialFetch");
        loaded.Should().NotContain("ShowCachedContentImmediate();");
        // Culture scan must not block the first paint path.
        loaded.Should().Contain("DispatcherPriority.Background");
        loaded.Should().Contain("SetPluginResourceCultures");

        xaml.Should().Contain("PluginLoadingIndicator");
        xaml.Should().Contain("x:Name=\"_loadingIndicator\"");
        xaml.Should().Contain("Visibility=\"Visible\"");
        xaml.Should().Contain("PluginStoreOfflineBanner");
        xaml.Should().Contain("PluginStoreRetryButton");
        xaml.Should().Contain("PluginExtensionsPage_StoreUnavailableTitle");
        xaml.Should().Contain("PluginExtensionsPage_StoreUnavailableMessage");
        xaml.Should().Contain("PluginExtensionsPage_StoreRetry");
        xaml.Should().Contain("PluginExtensionsPage_InstallAll");
        xaml.Should().Contain("PluginExtensionsPage_InstallAllTooltip");
        xaml.Should().NotContain("Text=\"Plugin store unavailable\"");
        xaml.Should().NotContain("Content=\"Retry\"");
        source.Should().Contain("UpdateStoreOfflineBanner");
        source.Should().Contain("StoreRetryButton_Click");
    }

    [Fact]
    public void PluginViewModel_DefaultInstallButtonText_ShouldUseResourceKey()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginViewModel.cs");
        source.Should().Contain("_installButtonText = Resource.PluginExtensionsPage_InstallPlugin");
        source.Should().NotContain("_installButtonText = \"Install\"");
    }

    [Fact]
    public void LoadingChrome_ShouldUseSingleSkeletonHost()
    {
        var xaml = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml");
        xaml.Split("PluginLoadingIndicator").Length.Should().Be(2);
        xaml.Split("x:Name=\"_loadingIndicator\"").Length.Should().Be(2);
        xaml.Should().Contain("x:Name=\"_pluginListPanel\" Visibility=\"Collapsed\"");
        xaml.Should().Contain("Panel.ZIndex=\"2\"");
    }

    [Fact]
    public void LocalInstall_ShouldRefreshRuntimeUiAndShowCapabilityAwareFeedback()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml.cs");
        var installHandler = ExtractMethod(source, "private async void PluginInstallButton_Click");

        installHandler.Should().Contain("_pluginManager.InstallPlugin(pluginId);");
        installHandler.Should().Contain("await RefreshInstalledPluginUiAfterInstallAsync(pluginId, forceRefreshRuntime: true);");
        installHandler.Should().Contain("await ShowInstalledPluginFeedbackAsync(pluginId);");
        installHandler.Should().NotContain("PluginExtensionsPage_InstallSuccessMessage");
    }

    [Fact]
    public void SharedInstallRefresh_ShouldReloadRuntimeResourcesUiAndMainNavigation()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml.cs");
        var refreshMethod = ExtractMethod(source, "private async Task RefreshInstalledPluginUiAfterInstallAsync");
        var onlineInstallMethod = ExtractMethod(source, "private async Task InstallOnlinePluginAsync");

        refreshMethod.Should().Contain("_pluginIdsReloadedForUi.Remove(pluginId);");
        refreshMethod.Should().Contain("await _pluginManager.ScanAndLoadPluginsAsync(forceRefreshRuntime)");
        refreshMethod.Should().Contain("LocalizationHelper.SetPluginResourceCultures();");
        refreshMethod.Should().Contain("UpdateAllPluginsUI();");
        refreshMethod.Should().Contain("mainWindow.UpdateInstalledPluginsNavigationItems();");

        onlineInstallMethod.Should().Contain("await RefreshInstalledPluginUiAfterInstallAsync(manifest.Id, forceRefreshRuntime: true);");
        onlineInstallMethod.Should().Contain("await ShowInstalledPluginFeedbackAsync(manifest.Id, manifest);");
    }

    [Fact]
    public void ManifestOnlyInstalledPlugins_ShouldHaveMetadataFallbackForOptimizationNavigation()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml.cs");
        var resolveMethod = ExtractMethod(source, "private PluginManifest? ResolvePluginManifestMetadata");

        resolveMethod.Should().Contain("TryReadInstalledPluginManifest(pluginId, metadata?.FilePath) ??");
        resolveMethod.Should().Contain("PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);");
    }

    [Fact]
    public void InstalledFeedback_ShouldExplainPluginsWithoutUsableEntry()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Pages", "PluginExtensionsPage.xaml.cs");
        var feedbackMethod = ExtractMethod(source, "private async Task ShowInstalledPluginFeedbackAsync");
        var resolveMethod = ExtractMethod(source, "internal static InstalledPluginFeedback ResolveInstalledPluginFeedback");
        var resources = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Resources", "Resource.resx");
        var zhResources = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Resources", "Resource.zh.resx");

        feedbackMethod.Should().Contain("ResolveInstalledPluginFeedback(runtimeCapabilities, manifestCapabilities, hasExecutable, plugin is null)");
        feedbackMethod.Should().Contain("PluginExtensionsPage_InstalledButRuntimeUnavailableMessage");
        feedbackMethod.Should().Contain("PluginExtensionsPage_InstalledButNoEntryMessage");
        feedbackMethod.Should().Contain("PluginExtensionsPage_InstallSuccessOptimizationMessage");
        resolveMethod.Should().Contain("runtimeCapabilities.HasAny || hasExecutable");
        resolveMethod.Should().Contain("manifestCapabilities.SupportsOptimizationCategory");
        resolveMethod.Should().Contain("!manifestCapabilities.SupportsFeaturePage");
        resolveMethod.Should().Contain("!manifestCapabilities.SupportsSettingsPage");
        resolveMethod.Should().Contain("runtimeMissing && manifestCapabilities.HasAny");
        resolveMethod.Should().Contain("InstalledPluginFeedback.RuntimeNotLoaded");
        resolveMethod.Should().Contain("InstalledPluginFeedback.NoUserFacingEntry");
        resolveMethod.IndexOf("manifestCapabilities.SupportsOptimizationCategory", StringComparison.Ordinal)
            .Should()
            .BeLessThan(resolveMethod.IndexOf("runtimeMissing && manifestCapabilities.HasAny", StringComparison.Ordinal));

        resources.Should().Contain("<data name=\"PluginExtensionsPage_InstalledButNoEntryMessage\"");
        zhResources.Should().Contain("<data name=\"PluginExtensionsPage_InstalledButNoEntryMessage\"");
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var braceStart = source.IndexOf('{', start);
        braceStart.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[start..(i + 1)];
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var expectedRelativePath = Path.Combine(pathParts);
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, expectedRelativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException($"Could not locate repository file '{expectedRelativePath}'.");
    }

    private static IEnumerable<string> GetRepositoryRootCandidates()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }
}
