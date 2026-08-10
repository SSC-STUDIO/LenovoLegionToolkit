using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Loading;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class PluginExtensionsPage
{
    private void SetLoadingState(bool isLoading)
    {
        if (isLoading)
        {
            ShowSkeletonImmediate();
            return;
        }

        // Honor minimum skeleton visibility so first-open shimmer is actually seen.
        var version = ++_loadingStateVersion;
        var elapsed = _skeletonShownAtUtc == DateTime.MinValue
            ? MinSkeletonVisible
            : DateTime.UtcNow - _skeletonShownAtUtc;
        var remaining = MinSkeletonVisible - elapsed;
        if (remaining > TimeSpan.Zero && IsLoaded)
        {
            _ = HideLoadingStateAfterAsync(remaining, version);
            return;
        }

        ApplyLoadingStateHidden();
    }

    /// <summary>
    /// First-step chrome: skeleton fully opaque and list collapsed. Never fade-from-0
    /// (that left a blank white region until the fade finished, or forever if interrupted).
    /// Always restarts the hold clock so re-entry cannot skip shimmer because of a stale timestamp.
    /// </summary>
    private void ShowSkeletonImmediate()
    {
        _loadingStateVersion++;

        // Nav crossfade may have left this page at Opacity 0 after a prior leave — force visible.
        // AVALONIA: WPF BeginAnimation(OpacityProperty, null) replaced by clearing Transitions.
        Transitions = null;
        Opacity = 1;

        if (_noPluginsMessage != null)
            _noPluginsMessage.IsVisible = false;
        if (_noResultsStackPanel != null)
            _noResultsStackPanel.IsVisible = false;

        // List must not cover skeleton (even empty ListBox can paint a blank surface).
        if (_pluginListPanel is Control listPanel)
        {
            listPanel.Transitions = null;
            listPanel.IsVisible = false;
            listPanel.Opacity = 1;
            listPanel.IsHitTestVisible = false;
        }

        var skeletonAlreadyLive = _loadingIndicator is Control existing
            && existing.IsVisible
            && existing.Opacity >= 0.95;

        // Only reset min-hold clock when skeleton is newly shown (classic soft re-entry).
        if (!skeletonAlreadyLive || _skeletonShownAtUtc == DateTime.MinValue)
            _skeletonShownAtUtc = DateTime.UtcNow;

        if (_loadingIndicator is Control skeleton)
        {
            skeleton.Transitions = null;
            skeleton.IsVisible = true;
            skeleton.Opacity = 1;
            skeleton.IsHitTestVisible = true;
            skeleton.ZIndex = 2;
            // One-time layout prime: XAML defaults Visible so skeletonAlreadyLive is true on
            // first paint — still need a single measure pass before walking shimmer borders.
            if (!_skeletonSubtreeLayoutPrimed)
            {
                skeleton.UpdateLayout();
                _skeletonSubtreeLayoutPrimed = true;
            }
        }

        // Soft restart: keep phase of already-running sweeps (4.x-style smoothness).
        SkeletonShimmer.RestartSubtree(_loadingIndicator, force: !skeletonAlreadyLive);
    }

    private async Task HideLoadingStateAfterAsync(TimeSpan delay, int version)
    {
        try
        {
            await Task.Delay(delay);
        }
        catch
        {
            return;
        }

        if (version != _loadingStateVersion || !IsLoaded)
            return;

        ApplyLoadingStateHidden();
    }

    private void ApplyLoadingStateHidden()
    {
        _skeletonShownAtUtc = DateTime.MinValue;
        CrossfadeToContent();
    }

    /// <summary>
    /// Soft handoff skeleton → real list only. Skeleton show path always snaps in
    /// via <see cref="ShowSkeletonImmediate"/> (never opacity 0).
    /// AVALONIA: WPF DoubleAnimation/BeginAnimation + Completed replaced by DoubleTransition
    /// (skeleton fade-out) with the collapse deferred via a delayed task.
    /// </summary>
    private void CrossfadeToContent()
    {
        var duration = this.TryFindResource("AnimationDurationSkeletonCrossfade") as TimeSpan?
                       ?? TimeSpan.FromMilliseconds(220);

        if (_loadingIndicator is Control skeleton && skeleton.IsVisible)
        {
            SkeletonShimmer.StopSubtree(_loadingIndicator);
            skeleton.IsHitTestVisible = false;
            skeleton.Transitions = null;
            skeleton.Transitions = new Transitions
            {
                new DoubleTransition
                {
                    Property = OpacityProperty,
                    Duration = duration,
                    Easing = new QuadraticEaseIn()
                }
            };
            skeleton.Opacity = 0;

            _ = HideLoadingIndicatorAfterFadeAsync(skeleton, duration);
        }
        else if (_loadingIndicator is not null)
        {
            _loadingIndicator.IsVisible = false;
        }

        if (_pluginListPanel is Control listPanel)
        {
            listPanel.Transitions = null;
            listPanel.IsVisible = true;
            listPanel.IsHitTestVisible = true;
            listPanel.ZIndex = 1;
            // Content can fade in; skeleton already covered first paint.
            if (listPanel.Opacity < 0.95)
            {
                listPanel.Opacity = 0;
                listPanel.Transitions = new Transitions
                {
                    new DoubleTransition
                    {
                        Property = OpacityProperty,
                        Duration = duration,
                        Easing = new QuadraticEaseOut()
                    }
                };
                listPanel.Opacity = 1;
            }
            else
            {
                listPanel.Opacity = 1;
            }
        }
    }

    private async Task HideLoadingIndicatorAfterFadeAsync(Control skeleton, TimeSpan delay)
    {
        try
        {
            await Task.Delay(delay);
        }
        catch
        {
            return;
        }

        // Another ShowSkeletonImmediate may have re-shown it mid-fade.
        if (skeleton.Opacity > 0.05 && skeleton.IsVisible)
            return;
        skeleton.IsVisible = false;
        skeleton.Transitions = null;
        skeleton.Opacity = 1;
    }

    private void UpdateBulkActionButtonsVisibility()
    {
        ReconcileAvailableUpdatesWithInstalledVersions();

        if (_bulkUpdateButton != null)
            _bulkUpdateButton.IsVisible = _availableUpdates.Count > 0 ? true : false;

        if (_bulkInstallButton != null)
        {
            var hasInstallCandidates = _onlinePlugins.Any(plugin => !IsPluginInstalledForUi(plugin.Id));
            _bulkInstallButton.IsVisible = hasInstallCandidates ? true : false;
            ToolTip.SetTip(_bulkInstallButton, LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAllTooltip", "Install all available plugins", Resource.Culture));
        }

        if (_bulkImportButton != null)
            _bulkImportButton.IsVisible = true;

        UpdateSummaryMetrics();
    }

    private void UpdateSummaryMetrics()
    {
        if (_summaryTotalTextBlock == null ||
            _summaryInstalledTextBlock == null ||
            _summaryUpdatesTextBlock == null ||
            _summaryStorePulseValueTextBlock == null ||
            _summaryHintTextBlock == null)
        {
            return;
        }

        var totalPlugins = _allPlugins.Count;
        var installedPlugins = _allPlugins.Count(plugin => IsPluginInstalledForUi(plugin.Id));
        var updatesReady = _availableUpdates.Count;
        var discoverablePlugins = Math.Max(0, totalPlugins - installedPlugins);
        var isWaitingForMetadata = totalPlugins == 0 && !_onlineMetadataLoadCompleted;

        _summaryTotalTextBlock.Text = totalPlugins.ToString(CultureInfo.InvariantCulture);
        _summaryInstalledTextBlock.Text = installedPlugins.ToString(CultureInfo.InvariantCulture);
        _summaryUpdatesTextBlock.Text = updatesReady.ToString(CultureInfo.InvariantCulture);
        _summaryStorePulseValueTextBlock.Text = updatesReady > 0
            ? updatesReady.ToString(CultureInfo.InvariantCulture)
            : discoverablePlugins > 0
                ? discoverablePlugins.ToString(CultureInfo.InvariantCulture)
                : isWaitingForMetadata
                    ? "..."
                    : "0";

        _summaryHintTextBlock.Text = updatesReady > 0
            ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryUpdatesAvailableLabel", "Updates available", Resource.Culture)
            : discoverablePlugins > 0
                ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryDiscoverableLabel", "Available to install", Resource.Culture)
                : isWaitingForMetadata
                    ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryWaitingMetadataShort", "Loading metadata", Resource.Culture)
                    : _onlineMetadataLoadFailed && totalPlugins == 0
                        ? LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_FetchFailed", "Failed to fetch plugins", Resource.Culture)
                        : totalPlugins == 0
                            ? Resource.PluginExtensionsPage_NoPluginsAvailable
                            : LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_SummaryUpToDateShort", "Up to date", Resource.Culture);
    }

    private static string FormatReleaseDate(string releaseDateRaw)
    {
        if (string.IsNullOrWhiteSpace(releaseDateRaw))
            return string.Empty;

        if (!DateTimeOffset.TryParse(releaseDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var releaseDate))
            return releaseDateRaw;

        return releaseDate.ToLocalTime().ToString(LocalizationHelper.ShortDateFormat);
    }

    private static string T(string key, string fallback)
    {
        return LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);
    }
}
