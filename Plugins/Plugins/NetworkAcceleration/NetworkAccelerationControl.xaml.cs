using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public partial class NetworkAccelerationControl : UserControl
{
    private const int MaxTelemetrySamples = 24;
    private static readonly string[] DataSizeSuffixes = ["B", "KB", "MB", "GB", "TB"];

    private readonly NetworkAccelerationPlugin _plugin;
    private readonly NetworkAccelerationTelemetryService _telemetryService = new();
    private readonly DispatcherTimer _telemetryTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };
    private readonly List<NetworkAccelerationTelemetrySnapshot> _telemetrySamples = [];

    private bool _isServiceRunning;
    private DateTimeOffset? _sessionStartedAt;
    private bool _isSelectionSyncInProgress;

    private static CultureInfo Culture => NetworkAccelerationText.Culture;

    public NetworkAccelerationControl(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
        _telemetryTimer.Tick += TelemetryTimer_Tick;

        InitializeComponent();
        LoadSavedSettings();
        LoadCurrentMode();
        SetPresetSelectionFromMode(_plugin.Settings.PreferredMode);
        UpdateModeDescription();
        UpdatePresetDetails();
        UpdateSavedModeSummary();

        var initialSnapshot = new NetworkAccelerationTelemetrySnapshot(
            DateTimeOffset.Now,
            NetworkAccelerationText.NoActiveAdapter,
            0,
            0,
            0,
            0);
        UpdateTelemetrySummary(initialSnapshot);
        UpdateAnalytics(initialSnapshot);
        SynchronizeRuntimeState();
        UpdateSessionPresentation();

        SetStatus(NetworkAccelerationText.MonitoringStatus, false);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        SynchronizeRuntimeState();
        RefreshTelemetry();
        _telemetryTimer.Start();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _telemetryTimer.Stop();
        _telemetryService.Dispose();
    }

    private void TelemetryTimer_Tick(object? sender, EventArgs e)
    {
        RefreshTelemetry();
        UpdateSessionPresentation();
    }

    private void LoadSavedSettings()
    {
        _autoOptimizeOnStartupCheckBox.IsChecked = _plugin.Settings.AutoOptimizeOnStartup;
        _resetWinsockCheckBox.IsChecked = _plugin.Settings.ResetWinsockOnOptimize;
        _resetTcpIpCheckBox.IsChecked = _plugin.Settings.ResetTcpIpOnOptimize;
    }

    private void LoadCurrentMode()
    {
        var modeValue = _plugin.Settings.PreferredMode.ToString();
        foreach (var item in _modeComboBox.Items.OfType<ComboBoxItem>())
        {
            if (item.Tag is string tag && tag.Equals(modeValue, StringComparison.OrdinalIgnoreCase))
            {
                _modeComboBox.SelectedItem = item;
                return;
            }
        }

        if (_modeComboBox.Items.Count > 0)
            _modeComboBox.SelectedIndex = 0;
    }

    private void SetPresetSelectionFromMode(NetworkAccelerationMode mode)
    {
        _isSelectionSyncInProgress = true;
        try
        {
            foreach (var item in _presetListBox.Items.OfType<ListBoxItem>())
            {
                if (item.Tag is string tag && tag.Equals(mode.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _presetListBox.SelectedItem = item;
                    return;
                }
            }
        }
        finally
        {
            _isSelectionSyncInProgress = false;
        }
    }

    private void SetModeSelection(NetworkAccelerationMode mode)
    {
        _isSelectionSyncInProgress = true;
        try
        {
            foreach (var item in _modeComboBox.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag is string tag && tag.Equals(mode.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    _modeComboBox.SelectedItem = item;
                    return;
                }
            }
        }
        finally
        {
            _isSelectionSyncInProgress = false;
        }
    }

    private NetworkAccelerationMode? ParseSelectedMode()
    {
        if (_modeComboBox.SelectedItem is ComboBoxItem comboItem &&
            comboItem.Tag is string modeTag &&
            Enum.TryParse(modeTag, true, out NetworkAccelerationMode mode))
        {
            return mode;
        }

        if (_presetListBox.SelectedItem is ListBoxItem listItem &&
            listItem.Tag is string listTag &&
            Enum.TryParse(listTag, true, out mode))
        {
            return mode;
        }

        return null;
    }

    private void PresetListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSelectionSyncInProgress)
            return;

        if (_presetListBox.SelectedItem is ListBoxItem item &&
            item.Tag is string modeTag &&
            Enum.TryParse(modeTag, true, out NetworkAccelerationMode mode))
        {
            SetModeSelection(mode);
        }

        UpdateModeDescription();
        UpdatePresetDetails();
    }

    private void ModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSelectionSyncInProgress)
            return;

        var mode = ParseSelectedMode();
        if (mode is not null)
            SetPresetSelectionFromMode(mode.Value);

        UpdateModeDescription();
        UpdatePresetDetails();
    }

    private void SettingsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        SetStatus(NetworkAccelerationText.SettingsPendingSave, false);
        UpdatePresetDetails();
    }

    private async void ServiceToggleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (IsRuntimeRunning())
            {
                _plugin.Runtime.Stop();
                _isServiceRunning = false;
                _sessionStartedAt = null;
                SetStatus(NetworkAccelerationText.StatusServiceStopped, false);
            }
            else
            {
                _plugin.Runtime.Start();
                _isServiceRunning = true;
                _sessionStartedAt = GetRuntimeSessionStartTime() ?? DateTimeOffset.Now;
                SetStatus(NetworkAccelerationText.StatusServiceStarted, false);
                await Task.Yield();
            }

            SynchronizeRuntimeState();
            UpdateSessionPresentation();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ServiceToggleButton_Click error: {ex.Message}", ex);
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshTelemetry();
        UpdatePresetDetails();
        SetStatus(NetworkAccelerationText.StatusRefreshed, false);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsExpander.IsExpanded = !_settingsExpander.IsExpanded;
    }

    private async void QuickOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.RunQuickOptimizationAsync().ConfigureAwait(true);
            SetStatus(
                success ? NetworkAccelerationText.StatusQuickOptimizationCompleted : NetworkAccelerationText.StatusQuickOptimizationFailed,
                !success);
            RefreshTelemetry();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"QuickOptimizeButton_Click error: {ex.Message}", ex);
        }
    }

    private async void ResetStackButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var success = await _plugin.ResetNetworkStackAsync().ConfigureAwait(true);
            SetStatus(
                success ? NetworkAccelerationText.StatusResetCompleted : NetworkAccelerationText.StatusResetFailed,
                !success);
            RefreshTelemetry();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"ResetStackButton_Click error: {ex.Message}", ex);
        }
    }

    private async void SaveModeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var mode = ParseSelectedMode();
            if (mode is null)
            {
                SetStatus(NetworkAccelerationText.StatusSelectValidMode, true);
                return;
            }

            _plugin.SetPreferredMode(mode.Value);
            _plugin.SetAutoOptimizeOnStartup(_autoOptimizeOnStartupCheckBox.IsChecked == true);
            _plugin.SetResetWinsockOnOptimize(_resetWinsockCheckBox.IsChecked == true);
            _plugin.SetResetTcpIpOnOptimize(_resetTcpIpCheckBox.IsChecked == true);
            await _plugin.SaveSettingsAsync().ConfigureAwait(true);

            UpdateModeDescription();
            UpdateSavedModeSummary();
            UpdatePresetDetails();
            SetStatus(NetworkAccelerationText.StatusModeSaved, false);
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", true);
            if (LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace($"SaveModeButton_Click error: {ex.Message}", ex);
        }
    }

    private void RefreshTelemetry()
    {
        SynchronizeRuntimeState();
        var snapshot = _telemetryService.Capture();
        _telemetrySamples.Add(snapshot);
        if (_telemetrySamples.Count > MaxTelemetrySamples)
            _telemetrySamples.RemoveAt(0);

        UpdateTelemetrySummary(snapshot);
        UpdateAnalytics(snapshot);
    }

    private void UpdateTelemetrySummary(NetworkAccelerationTelemetrySnapshot snapshot)
    {
        _downloadValueTextBlock.Text = FormatRate(snapshot.DownloadMbps);
        _uploadValueTextBlock.Text = FormatRate(snapshot.UploadMbps);
        _peakValueTextBlock.Text = FormatRate(_telemetrySamples.Count == 0
            ? 0
            : _telemetrySamples.Max(sample => Math.Max(sample.DownloadMbps, sample.UploadMbps)));
        _adapterValueTextBlock.Text = snapshot.InterfaceName;
        _downloadTotalTextBlock.Text = FormatDataSize(snapshot.TotalReceivedBytes);
        _uploadTotalTextBlock.Text = FormatDataSize(snapshot.TotalSentBytes);
        _updatedValueTextBlock.Text = snapshot.Timestamp.ToLocalTime().ToString("HH:mm:ss", Culture);
    }

    private void UpdateAnalytics(NetworkAccelerationTelemetrySnapshot snapshot)
    {
        var combined = Math.Max(0, snapshot.DownloadMbps) + Math.Max(0, snapshot.UploadMbps);
        var downloadShare = combined <= 0 ? 0 : (snapshot.DownloadMbps / combined) * 100d;
        var uploadShare = combined <= 0 ? 0 : (snapshot.UploadMbps / combined) * 100d;
        var rollingAverage = _telemetrySamples.Count == 0
            ? 0
            : _telemetrySamples.Average(sample => sample.DownloadMbps + sample.UploadMbps);
        var burstPeak = _telemetrySamples.Count == 0
            ? 0
            : _telemetrySamples.Max(sample => sample.DownloadMbps + sample.UploadMbps);

        _downloadShareValueTextBlock.Text = FormatPercent(downloadShare);
        _uploadShareValueTextBlock.Text = FormatPercent(uploadShare);
        _downloadShareBar.Value = downloadShare;
        _uploadShareBar.Value = uploadShare;
        _rollingAverageTextBlock.Text = FormatRate(rollingAverage);
        _burstPeakTextBlock.Text = FormatRate(burstPeak);
    }

    private void UpdateModeDescription()
    {
        var mode = ParseSelectedMode() ?? NetworkAccelerationMode.Balanced;
        _modeDescriptionTextBlock.Text = mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGamingDescription,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreamingDescription,
            _ => NetworkAccelerationText.ModeBalancedDescription
        };
    }

    private void UpdateSavedModeSummary()
    {
        _savedModeSummaryTextBlock.Text = GetModeDisplayName(_plugin.Settings.PreferredMode);
    }

    private void UpdatePresetDetails()
    {
        var mode = ParseSelectedMode() ?? NetworkAccelerationMode.Balanced;
        _presetTitleTextBlock.Text = mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGamingTargetTitle,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreamingTargetTitle,
            _ => NetworkAccelerationText.ModeBalancedTargetTitle
        };
        _presetSubtitleTextBlock.Text = mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGamingTargetDescription,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreamingTargetDescription,
            _ => NetworkAccelerationText.ModeBalancedTargetDescription
        };
        _presetRecommendationTextBlock.Text = mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGamingRecommendedFor,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreamingRecommendedFor,
            _ => NetworkAccelerationText.ModeBalancedRecommendedFor
        };
        _presetActionsTextBlock.Text = mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGamingFocus,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreamingFocus,
            _ => NetworkAccelerationText.ModeBalancedFocus
        };
        _presetStateTextBlock.Text = _isServiceRunning
            ? NetworkAccelerationText.PresetStateActive
            : NetworkAccelerationText.PresetStateReady;

        UpdateSessionPresentation();
    }

    private void UpdateSessionPresentation()
    {
        _serviceStateTextBlock.Text = _isServiceRunning
            ? NetworkAccelerationText.ServiceStateRunning
            : NetworkAccelerationText.ServiceStateStopped;

        _serviceToggleButton.Content = _isServiceRunning
            ? NetworkAccelerationText.StopServiceButton
            : NetworkAccelerationText.StartServiceButton;

        var sessionText = NetworkAccelerationText.SessionNotStarted;
        if (_isServiceRunning && _sessionStartedAt is not null)
        {
            var elapsed = DateTimeOffset.Now - _sessionStartedAt.Value;
            if (elapsed < TimeSpan.Zero)
                elapsed = TimeSpan.Zero;
            sessionText = FormatDuration(elapsed);
        }

        _sessionValueTextBlock.Text = sessionText;
        _detailSessionTextBlock.Text = sessionText;
        _presetStateTextBlock.Text = _isServiceRunning
            ? NetworkAccelerationText.PresetStateActive
            : NetworkAccelerationText.PresetStateReady;
    }

    private void SynchronizeRuntimeState()
    {
        _isServiceRunning = IsRuntimeRunning();

        if (_isServiceRunning)
        {
            _sessionStartedAt ??= GetRuntimeSessionStartTime() ?? DateTimeOffset.Now;
        }
        else
        {
            _sessionStartedAt = null;
        }
    }

    private bool IsRuntimeRunning()
    {
        return _plugin.Runtime.IsRunning;
    }

    private DateTimeOffset? GetRuntimeSessionStartTime()
    {
        var earliestSampleUtc = _plugin.Runtime.GetRecentSamples()
            .Select(sample => sample.TimestampUtc)
            .DefaultIfEmpty()
            .Min();

        if (earliestSampleUtc == default)
            return null;

        return new DateTimeOffset(DateTime.SpecifyKind(earliestSampleUtc, DateTimeKind.Utc));
    }

    private void SetStatus(string text, bool isError)
    {
        _statusTextBlock.Text = text;
        _statusTextBlock.Foreground = isError
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC42B1C"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF0F7B5A"));

        _statusIcon.Symbol = isError
            ? Wpf.Ui.Common.SymbolRegular.ErrorCircle24
            : Wpf.Ui.Common.SymbolRegular.CheckmarkCircle24;
        _statusIcon.Foreground = _statusTextBlock.Foreground;
    }

    private static string GetModeDisplayName(NetworkAccelerationMode mode)
    {
        return mode switch
        {
            NetworkAccelerationMode.Gaming => NetworkAccelerationText.ModeGaming,
            NetworkAccelerationMode.Streaming => NetworkAccelerationText.ModeStreaming,
            _ => NetworkAccelerationText.ModeBalanced
        };
    }

    private static string FormatRate(double mbps)
    {
        return string.Format(Culture, NetworkAccelerationText.MbpsValueFormat, mbps);
    }

    private static string FormatPercent(double value)
    {
        return string.Format(Culture, "{0:0}%", value);
    }

    private static string FormatDataSize(long bytes)
    {
        double value = bytes;
        var suffixIndex = 0;

        while (value >= 1024 && suffixIndex < DataSizeSuffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return string.Format(Culture, "{0:0.#} {1}", value, DataSizeSuffixes[suffixIndex]);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return duration.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

        return duration.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
    }
}
