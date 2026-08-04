using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using UniversalDeviceToolkit.WPF.Controls.Loading;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Pages;

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

        // Nav crossfade may have left this Page at Opacity 0 after a prior leave — force visible.
        BeginAnimation(UIElement.OpacityProperty, null);
        Opacity = 1;

        if (_noPluginsMessage != null)
            _noPluginsMessage.Visibility = Visibility.Collapsed;
        if (_noResultsStackPanel != null)
            _noResultsStackPanel.Visibility = Visibility.Collapsed;

        // List must not cover skeleton (even empty ListBox can paint a blank surface).
        if (_pluginListPanel is FrameworkElement listPanel)
        {
            listPanel.BeginAnimation(UIElement.OpacityProperty, null);
            listPanel.Visibility = Visibility.Collapsed;
            listPanel.Opacity = 1;
            listPanel.IsHitTestVisible = false;
        }

        var skeletonAlreadyLive = _loadingIndicator is FrameworkElement existing
            && existing.Visibility == Visibility.Visible
            && existing.Opacity >= 0.95;

        // Only reset min-hold clock when skeleton is newly shown (classic soft re-entry).
        if (!skeletonAlreadyLive || _skeletonShownAtUtc == DateTime.MinValue)
            _skeletonShownAtUtc = DateTime.UtcNow;

        if (_loadingIndicator is FrameworkElement skeleton)
        {
            skeleton.BeginAnimation(UIElement.OpacityProperty, null);
            skeleton.Visibility = Visibility.Visible;
            skeleton.Opacity = 1;
            skeleton.IsHitTestVisible = true;
            Panel.SetZIndex(skeleton, 2);
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
    /// </summary>
    private void CrossfadeToContent()
    {
        var duration = TryFindResource("AnimationDurationSkeletonCrossfade") as Duration?
                       ?? new Duration(TimeSpan.FromMilliseconds(220));

        if (_loadingIndicator is FrameworkElement skeleton && skeleton.Visibility == Visibility.Visible)
        {
            SkeletonShimmer.StopSubtree(_loadingIndicator);
            skeleton.IsHitTestVisible = false;
            skeleton.BeginAnimation(UIElement.OpacityProperty, null);
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) =>
            {
                // Another ShowSkeletonImmediate may have re-shown it mid-fade.
                if (skeleton.Opacity > 0.05 && skeleton.Visibility == Visibility.Visible)
                    return;
                skeleton.Visibility = Visibility.Collapsed;
                skeleton.BeginAnimation(UIElement.OpacityProperty, null);
                skeleton.Opacity = 1;
            };
            skeleton.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
        else if (_loadingIndicator is not null)
        {
            _loadingIndicator.Visibility = Visibility.Collapsed;
        }

        if (_pluginListPanel is FrameworkElement listPanel)
        {
            listPanel.BeginAnimation(UIElement.OpacityProperty, null);
            listPanel.Visibility = Visibility.Visible;
            listPanel.IsHitTestVisible = true;
            Panel.SetZIndex(listPanel, 1);
            // Content can fade in; skeleton already covered first paint.
            if (listPanel.Opacity < 0.95)
            {
                listPanel.Opacity = 0;
                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                listPanel.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
            else
            {
                listPanel.Opacity = 1;
            }
        }
    }

    private void UpdateBulkActionButtonsVisibility()
    {
        ReconcileAvailableUpdatesWithInstalledVersions();

        if (_bulkUpdateButton != null)
            _bulkUpdateButton.Visibility = _availableUpdates.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (_bulkInstallButton != null)
        {
            var hasInstallCandidates = _onlinePlugins.Any(plugin => !IsPluginInstalledForUi(plugin.Id));
            _bulkInstallButton.Visibility = hasInstallCandidates ? Visibility.Visible : Visibility.Collapsed;
            _bulkInstallButton.ToolTip = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "PluginExtensionsPage_InstallAllTooltip", "Install all available plugins", Resource.Culture);
        }

        if (_bulkImportButton != null)
            _bulkImportButton.Visibility = Visibility.Visible;

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
