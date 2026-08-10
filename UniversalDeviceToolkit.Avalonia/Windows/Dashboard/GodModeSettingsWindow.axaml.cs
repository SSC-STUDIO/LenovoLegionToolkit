using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using UniversalDeviceToolkit.Lib.Controllers.GodMode;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using MenuItem = UniversalDeviceToolkit.Avalonia.Controls.MenuItem;

namespace UniversalDeviceToolkit.Avalonia.Windows.Dashboard
{
public partial class GodModeSettingsWindow : global::UniversalDeviceToolkit.Avalonia.Windows.BaseWindow
{
    private const string DEFAULT_PRESET_NAME_FALLBACK = "Preset";

    private static string T(string key, string fallback) =>
        LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private readonly PowerModeFeature _powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
    private readonly GodModeController _godModeController = IoCContainer.Resolve<GodModeController>();

    private readonly VantageDisabler _vantageDisabler = IoCContainer.Resolve<VantageDisabler>();
    private readonly LegionZoneDisabler _legionZoneDisabler = IoCContainer.Resolve<LegionZoneDisabler>();

    private GodModeState? _state;
    private Dictionary<PowerModeState, GodModeDefaults>? _defaults;
    private bool _isRefreshing;
    private bool _initialDefaultsApplied;
    private readonly Snackbar _snackBar;
    private readonly PowerModeState? _initialDefaultsSourceMode;

    public GodModeSettingsWindow(PowerModeState? initialDefaultsSourceMode = null)
    {
        InitializeComponent();
        _maxValueOffsetNumberBox.ValueChanged += (_, _) => UpdateRiskWarnings();
        _minValueOffsetNumberBox.ValueChanged += (_, _) => UpdateRiskWarnings();
        _initialDefaultsSourceMode = initialDefaultsSourceMode switch
        {
            PowerModeState.Performance => PowerModeState.Performance,
            PowerModeState.Extreme => PowerModeState.Performance,
            _ => null
        };

        _snackBar = NotificationToastFactory.Create(_snackBarPresenter, HorizontalAlignment.Center);
        _snackBar.IsCloseButtonEnabled = true;
        _snackBar.Icon = new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24 };
        _snackBar.Appearance = ControlAppearance.Danger;
        _snackBar.Timeout = TimeSpan.FromSeconds(5);

        PropertyChanged += GodModeSettingsWindow_PropertyChanged;
    }

    private async void GodModeSettingsWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible)
                await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(GodModeSettingsWindow_PropertyChanged)}: {ex.Message}", ex);
        }
    }

    private async Task RefreshAsync()
    {
        _isRefreshing = true;

        try
        {
            _loader.IsLoading = true;
            _buttonsStackPanel.IsVisible = false;

            var loadingTask = Task.Delay(TimeSpan.FromMilliseconds(500));

            _vantageRunningWarningInfoBar.IsOpen = await _godModeController.NeedsVantageDisabledAsync() && await _vantageDisabler.GetStatusAsync() == SoftwareStatus.Enabled;
            _legionZoneRunningWarningInfoBar.IsOpen = await _godModeController.NeedsLegionZoneDisabledAsync() && await _legionZoneDisabler.GetStatusAsync() == SoftwareStatus.Enabled;

            _state = await _godModeController.GetStateAsync();
            _defaults = await _godModeController.GetDefaultsInOtherPowerModesAsync();

            if (_state is null)
                throw new InvalidOperationException($"{nameof(_state)} is null");

            if (_defaults is null)
                throw new InvalidOperationException($"{nameof(_defaults)} are null");

            await SetStateAsync(_state.Value);

            if (!_initialDefaultsApplied
                && _initialDefaultsSourceMode is { } initialDefaultsSourceMode
                && _defaults.TryGetValue(initialDefaultsSourceMode, out var defaults))
            {
                await SetDefaultsAsync(defaults);
                _initialDefaultsApplied = true;
            }

            await loadingTask;

            _loadButton.IsVisible = _defaults.Count != 0 ? true : false;
            _buttonsStackPanel.IsVisible = true;
            _loader.IsLoading = false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't load settings.", ex);

            await ShowSnackBarAsync(Resource.GodModeSettingsWindow_Error_Load_Title, ex.Message);

            Close();
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private GodModePreset BuildActivePresetFromControls(GodModePreset preset) => new()
    {
        Name = preset.Name,
        PowerPlanGuid = preset.PowerPlanGuid,
        PowerMode = preset.PowerMode,
        SourcePowerMode = preset.SourcePowerMode,
        CPULongTermPowerLimit = preset.CPULongTermPowerLimit?.WithValue(_cpuLongTermPowerLimitControl.Value),
        CPUShortTermPowerLimit = preset.CPUShortTermPowerLimit?.WithValue(_cpuShortTermPowerLimitControl.Value),
        CPUPeakPowerLimit = preset.CPUPeakPowerLimit?.WithValue(_cpuPeakPowerLimitControl.Value),
        CPUCrossLoadingPowerLimit = preset.CPUCrossLoadingPowerLimit?.WithValue(_cpuCrossLoadingLimitControl.Value),
        CPUPL1Tau = preset.CPUPL1Tau?.WithValue(_cpuPL1TauControl.Value),
        APUsPPTPowerLimit = preset.APUsPPTPowerLimit?.WithValue(_apuSPPTPowerLimitControl.Value),
        CPUTemperatureLimit = preset.CPUTemperatureLimit?.WithValue(_cpuTemperatureLimitControl.Value),
        GPUPowerBoost = preset.GPUPowerBoost?.WithValue(_gpuPowerBoostControl.Value),
        GPUConfigurableTGP = preset.GPUConfigurableTGP?.WithValue(_gpuConfigurableTGPControl.Value),
        GPUTemperatureLimit = preset.GPUTemperatureLimit?.WithValue(_gpuTemperatureLimitControl.Value),
        GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline = preset.GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline?.WithValue(_gpuTotalProcessingPowerTargetOnAcOffsetFromBaselineControl.Value),
        GPUToCPUDynamicBoost = preset.GPUToCPUDynamicBoost?.WithValue(_gpuToCpuDynamicBoostControl.Value),
        FanTableInfo = preset.FanTableInfo is not null ? _fanCurveControl.GetFanTableInfo() : null,
        FanFullSpeed = preset.FanFullSpeed is not null ? _fanFullSpeedToggle.IsChecked : null,
        MaxValueOffset = preset.MaxValueOffset is not null ? GetRequiredOffsetValue(_maxValueOffsetNumberBox, 0, 100) : null,
        MinValueOffset = preset.MinValueOffset is not null ? GetRequiredOffsetValue(_minValueOffsetNumberBox, -100, 0) : null
    };

    private void FlushActivePresetToState()
    {
        if (!_state.HasValue)
            return;

        var activePresetId = _state.Value.ActivePresetId;
        var presets = _state.Value.Presets;
        var preset = presets[activePresetId];
        var newPresets = new Dictionary<Guid, GodModePreset>(presets)
        {
            [activePresetId] = BuildActivePresetFromControls(preset)
        };

        _state = _state.Value with { Presets = newPresets.AsReadOnlyDictionary() };
    }

    private async Task PersistStateAsync()
    {
        if (!_state.HasValue)
            return;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Persisting God Mode state... [activePresetId={_state.Value.ActivePresetId}, presetCount={_state.Value.Presets.Count}]");

        await _godModeController.SetStateAsync(_state.Value);
        _state = await _godModeController.GetStateAsync();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"God Mode state reloaded after persistence. [activePresetId={_state.Value.ActivePresetId}, presetCount={_state.Value.Presets.Count}]");
    }

    private async Task PersistAndRefreshPresetListAsync()
    {
        await PersistStateAsync();

        if (!_state.HasValue)
            return;

        await SetStateAsync(_state.Value);
    }

    internal static string GetUniquePresetName(
        string requestedName,
        IReadOnlyDictionary<Guid, GodModePreset> presets,
        Guid? excludePresetId = null)
    {
        var normalizedRequestedName = NormalizePresetName(requestedName);

        var existingNames = presets
            .Where(kv => !excludePresetId.HasValue || kv.Key != excludePresetId.Value)
            .Select(kv => kv.Value.Name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingNames.Contains(normalizedRequestedName))
            return normalizedRequestedName;

        var suffix = 2;
        while (true)
        {
            var candidate = $"{normalizedRequestedName} ({suffix})";
            if (!existingNames.Contains(candidate))
                return candidate;

            suffix++;
        }
    }

    internal static string NormalizePresetName(string? requestedName)
    {
        var normalizedRequestedName = requestedName?.Trim();
        return string.IsNullOrWhiteSpace(normalizedRequestedName)
            ? GetDefaultPresetName()
            : normalizedRequestedName;
    }

    internal static string GetDefaultPresetName() =>
        T("GodModeSettingsWindow_DefaultPresetName", DEFAULT_PRESET_NAME_FALLBACK);

    internal static GodModeState AddPreset(GodModeState state, string requestedName, Guid? newPresetId = null)
    {
        var activePreset = GetActivePreset(state);
        var presetId = newPresetId ?? Guid.NewGuid();
        if (state.Presets.ContainsKey(presetId))
            throw new InvalidOperationException($"Preset with ID {presetId} already exists.");

        var uniqueName = GetUniquePresetName(requestedName, state.Presets);
        var presets = new Dictionary<Guid, GodModePreset>(state.Presets)
        {
            [presetId] = activePreset with { Name = uniqueName, SourcePowerMode = null }
        };

        return new()
        {
            ActivePresetId = presetId,
            Presets = presets.AsReadOnlyDictionary()
        };
    }

    internal static GodModeState RenameActivePreset(GodModeState state, string requestedName)
    {
        var activePresetId = state.ActivePresetId;
        var activePreset = GetActivePreset(state);
        var uniqueName = GetUniquePresetName(requestedName, state.Presets, activePresetId);
        var presets = new Dictionary<Guid, GodModePreset>(state.Presets)
        {
            [activePresetId] = activePreset with { Name = uniqueName, SourcePowerMode = null }
        };

        return state with { Presets = presets.AsReadOnlyDictionary() };
    }

    internal static GodModeState DeleteActivePreset(GodModeState state)
    {
        _ = GetActivePreset(state);
        if (state.Presets.Count <= 1)
            return state;

        var presets = new Dictionary<Guid, GodModePreset>(state.Presets);
        presets.Remove(state.ActivePresetId);
        var activePresetId = presets.OrderBy(kv => kv.Value.Name)
            .Select(kv => kv.Key)
            .First();

        return new()
        {
            ActivePresetId = activePresetId,
            Presets = presets.AsReadOnlyDictionary()
        };
    }

    private static GodModePreset GetActivePreset(GodModeState state)
    {
        if (state.Presets is null || !state.Presets.TryGetValue(state.ActivePresetId, out var preset))
            throw new InvalidOperationException($"Preset with ID {state.ActivePresetId} not found.");

        return preset;
    }

    private async Task<bool> ApplyAsync()
    {
        try
        {
            if (!_state.HasValue)
                throw new InvalidOperationException("State is null");

            if (!TryValidateOffsetInputs(out var invalidOffsetMessage))
            {
                await ShowSnackBarAsync(Resource.GodModeSettingsWindow_Error_Apply_Title, invalidOffsetMessage);
                return false;
            }

            FlushActivePresetToState();

            if (await _powerModeFeature.GetStateAsync() != PowerModeState.GodMode)
                await _powerModeFeature.SetStateAsync(PowerModeState.GodMode);

            await PersistStateAsync();
            await _godModeController.ApplyStateAsync();

            // Auto-closing success toast (not an error-style snackbar).
            await ShowSuccessSnackBarAsync(
                Resource.GodModeSettingsWindow_Title,
                LocalizationHelper.GetStringOrEnglish(
                    Resource.ResourceManager,
                    "GodModeSettingsWindow_ApplySuccess_Message",
                    "Custom mode settings applied successfully.",
                    Resource.Culture));

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't apply settings", ex);

            await ShowSnackBarAsync(Resource.GodModeSettingsWindow_Error_Apply_Title, ex.Message);

            return false;
        }
    }

    private async Task SetStateAsync(GodModeState state)
    {
        _cpuLongTermPowerLimitControl.ValueChanged -= CpuLongTermPowerLimitSlider_ValueChanged;
        _cpuShortTermPowerLimitControl.ValueChanged -= CpuShortTermPowerLimitSlider_ValueChanged;

        var activePresetId = state.ActivePresetId;
        var preset = state.Presets[activePresetId];

        _presetsComboBox.SelectionChanged -= PresetsComboBox_SelectionChanged;
        try
        {
            _presetsComboBox.SetItems(
                state.Presets.OrderBy(kv => kv.Value.Name),
                new KeyValuePair<Guid, GodModePreset>(activePresetId, preset),
                kv => kv.Value.Name);
        }
        finally
        {
            _presetsComboBox.SelectionChanged += PresetsComboBox_SelectionChanged;
        }

        _deletePresetsButton.IsEnabled = state.Presets.Count > 1;

        _cpuLongTermPowerLimitControl.Set(preset.CPULongTermPowerLimit);
        _cpuShortTermPowerLimitControl.Set(preset.CPUShortTermPowerLimit);
        _cpuPeakPowerLimitControl.Set(preset.CPUPeakPowerLimit);
        _cpuCrossLoadingLimitControl.Set(preset.CPUCrossLoadingPowerLimit);
        _cpuPL1TauControl.Set(preset.CPUPL1Tau);
        _apuSPPTPowerLimitControl.Set(preset.APUsPPTPowerLimit);
        _cpuTemperatureLimitControl.Set(preset.CPUTemperatureLimit);
        _gpuPowerBoostControl.Set(preset.GPUPowerBoost);
        _gpuConfigurableTGPControl.Set(preset.GPUConfigurableTGP);
        _gpuTemperatureLimitControl.Set(preset.GPUTemperatureLimit);
        _gpuTotalProcessingPowerTargetOnAcOffsetFromBaselineControl.Set(preset.GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline);
        _gpuToCpuDynamicBoostControl.Set(preset.GPUToCPUDynamicBoost);

        var fanTableInfo = preset.FanTableInfo;
        _fanCurveCardControl.IsVisible = fanTableInfo.HasValue ? true : false;
        if (fanTableInfo.HasValue)
        {
            var minimum = await _godModeController.GetMinimumFanTableAsync();
            _fanCurveControl.SetFanTableInfo(fanTableInfo.Value, minimum);
        }
        else
        {
            _fanCurveCardControl.IsVisible = false;
        }

        var fanFullSpeed = preset.FanFullSpeed;
        _fanFullSpeedCardControl.IsVisible = fanFullSpeed.HasValue ? true : false;
        if (fanFullSpeed.HasValue)
        {
            _fanCurveCardControl.IsEnabled = !fanFullSpeed.Value;
            _fanFullSpeedToggle.IsChecked = fanFullSpeed.Value;
        }
        else
        {
            _fanCurveCardControl.IsEnabled = true;
            _fanFullSpeedCardControl.IsVisible = false;
        }

        var maxValueOffset = preset.MaxValueOffset;
        if (maxValueOffset.HasValue)
        {
            _maxValueOffsetCardControl.IsVisible = true;
            SetOffsetValue(_maxValueOffsetNumberBox, maxValueOffset.Value, 0, 100);
        }
        else
            _maxValueOffsetCardControl.IsVisible = false;

        var minValueOffset = preset.MinValueOffset;
        if (minValueOffset.HasValue)
        {
            _minValueOffsetCardControl.IsVisible = true;
            SetOffsetValue(_minValueOffsetNumberBox, minValueOffset.Value, -100, 0);
        }
        else
            _minValueOffsetCardControl.IsVisible = false;

        UpdateRiskWarnings();

        var cpuSectionVisible = new[]
        {
            _cpuLongTermPowerLimitControl,
            _cpuShortTermPowerLimitControl,
            _cpuPeakPowerLimitControl,
            _cpuCrossLoadingLimitControl,
            _cpuPL1TauControl,
            _apuSPPTPowerLimitControl,
            _cpuTemperatureLimitControl
        }.Any(v => v.IsVisible);

        var gpuSectionVisible = new[]
        {
            _gpuPowerBoostControl,
            _gpuConfigurableTGPControl,
            _gpuTemperatureLimitControl,
            _gpuTotalProcessingPowerTargetOnAcOffsetFromBaselineControl,
            _gpuToCpuDynamicBoostControl
        }.Any(v => v.IsVisible);

        var fanSectionVisible = new[]
        {
            _fanCurveCardControl,
            _fanFullSpeedCardControl
        }.Any(v => v.IsVisible);

        var advancedSectionVisible = new[]
        {
            _maxValueOffsetCardControl,
            _minValueOffsetCardControl
        }.Any(v => v.IsVisible);

        _cpuSectionTitle.IsVisible = cpuSectionVisible ? true : false;
        _gpuSectionTitle.IsVisible = gpuSectionVisible ? true : false;
        _fanSectionTitle.IsVisible = fanSectionVisible ? true : false;
        _advancedSectionTitle.IsVisible = advancedSectionVisible ? true : false;

        _cpuLongTermPowerLimitControl.ValueChanged += CpuLongTermPowerLimitSlider_ValueChanged;
        _cpuShortTermPowerLimitControl.ValueChanged += CpuShortTermPowerLimitSlider_ValueChanged;
    }

    private async Task SetDefaultsAsync(GodModeDefaults defaults)
    {
        try
        {
            if (_cpuLongTermPowerLimitControl.IsVisible && defaults.CPULongTermPowerLimit is { } cpuLongTermPowerLimit)
                _cpuLongTermPowerLimitControl.Value = cpuLongTermPowerLimit;

            if (_cpuShortTermPowerLimitControl.IsVisible && defaults.CPUShortTermPowerLimit is { } cpuShortTermPowerLimit)
                _cpuShortTermPowerLimitControl.Value = cpuShortTermPowerLimit;

            if (_cpuPeakPowerLimitControl.IsVisible && defaults.CPUPeakPowerLimit is { } cpuPeakPowerLimit)
                _cpuPeakPowerLimitControl.Value = cpuPeakPowerLimit;

            if (_cpuCrossLoadingLimitControl.IsVisible && defaults.CPUCrossLoadingPowerLimit is { } cpuCrossLoadingPowerLimit)
                _cpuCrossLoadingLimitControl.Value = cpuCrossLoadingPowerLimit;

            if (_cpuPL1TauControl.IsVisible && defaults.CPUPL1Tau is { } cpuPL1Tau)
                _cpuPL1TauControl.Value = cpuPL1Tau;

            if (_apuSPPTPowerLimitControl.IsVisible && defaults.APUsPPTPowerLimit is { } apuSPPTPowerLimit)
                _apuSPPTPowerLimitControl.Value = apuSPPTPowerLimit;

            if (_cpuTemperatureLimitControl.IsVisible && defaults.CPUTemperatureLimit is { } cpuTemperatureLimit)
                _cpuTemperatureLimitControl.Value = cpuTemperatureLimit;

            if (_gpuPowerBoostControl.IsVisible && defaults.GPUPowerBoost is { } gpuPowerBoost)
                _gpuPowerBoostControl.Value = gpuPowerBoost;

            if (_gpuConfigurableTGPControl.IsVisible && defaults.GPUConfigurableTGP is { } gpuConfigurableTgp)
                _gpuConfigurableTGPControl.Value = gpuConfigurableTgp;

            if (_gpuTemperatureLimitControl.IsVisible && defaults.GPUTemperatureLimit is { } gpuTemperatureLimit)
                _gpuTemperatureLimitControl.Value = gpuTemperatureLimit;

            if (_gpuTotalProcessingPowerTargetOnAcOffsetFromBaselineControl.IsVisible && defaults.GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline is { } gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline)
                _gpuTotalProcessingPowerTargetOnAcOffsetFromBaselineControl.Value = gpuTotalProcessingPowerTargetOnAcOffsetFromBaseline;

            if (_gpuToCpuDynamicBoostControl.IsVisible && defaults.GPUToCPUDynamicBoost is { } gpuToCPUDynamicBoost)
                _gpuToCpuDynamicBoostControl.Value = gpuToCPUDynamicBoost;

            if (_fanCurveCardControl.IsVisible && defaults.FanTable is { } fanTable)
            {
                var state = await _godModeController.GetStateAsync();
                var preset = state.Presets[state.ActivePresetId];
                var data = preset.FanTableInfo?.Data;

                if (data is not null)
                {
                    var defaultFanTableInfo = new FanTableInfo(data, fanTable);
                    var minimum = await _godModeController.GetMinimumFanTableAsync();
                    _fanCurveControl.SetFanTableInfo(defaultFanTableInfo, minimum);
                }
            }

            if (_fanFullSpeedCardControl.IsVisible && defaults.FanFullSpeed is { } fanFullSpeed)
                _fanFullSpeedToggle.IsChecked = fanFullSpeed;

            if (_maxValueOffsetCardControl.IsVisible)
                SetOffsetValue(_maxValueOffsetNumberBox, 0, 0, 100);

            if (_minValueOffsetCardControl.IsVisible)
                SetOffsetValue(_minValueOffsetNumberBox, 0, -100, 0);

            UpdateRiskWarnings();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to set God Mode defaults: {ex.Message}", ex);
        }
    }

    private async void PresetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (_isRefreshing || !_state.HasValue)
                return;

            if (!_presetsComboBox.TryGetSelectedItem<KeyValuePair<Guid, GodModePreset>>(out var item))
                return;

            if (_state.Value.ActivePresetId == item.Key)
                return;

            FlushActivePresetToState();
            _state = _state.Value with { ActivePresetId = item.Key };

            try
            {
                await PersistStateAsync();

                if (await _powerModeFeature.GetStateAsync() == PowerModeState.GodMode)
                    await _godModeController.ApplyStateAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't switch preset.", ex);

                await ShowSnackBarAsync(Resource.GodModeSettingsWindow_Error_Apply_Title, ex.Message);
                return;
            }

            await SetStateAsync(_state.Value);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(PresetsComboBox_SelectionChanged)}: {ex.Message}", ex);
        }
    }

    private async void EditPresetsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_state.HasValue)
                return;

            FlushActivePresetToState();

            var activePresetId = _state.Value.ActivePresetId;
            var presets = _state.Value.Presets;
            var preset = presets[activePresetId];

            var result = await MessageBoxHelper.ShowInputAsync(this, Resource.GodModeSettingsWindow_EditPreset_Title, Resource.GodModeSettingsWindow_EditPreset_Message, preset.Name);
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Edit preset dialog completed. [result={(result is null ? "<null>" : result)}, activePresetId={activePresetId}]");
            if (string.IsNullOrWhiteSpace(result))
                return;

            _state = RenameActivePreset(_state.Value, result);

            try
            {
                await PersistAndRefreshPresetListAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't rename preset.", ex);

                await ShowSnackBarAsync(Resource.GodModeSettingsWindow_Error_Apply_Title, ex.Message);
                return;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(EditPresetsButton_Click)}: {ex.Message}", ex);
        }
    }

    private async void DeletePresetsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_state.HasValue)
                return;

            if (_state.Value.Presets.Count <= 1)
                return;

            var activePresetId = _state.Value.ActivePresetId;
            _state = DeleteActivePreset(_state.Value);

            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Deleting God Mode preset. [deletedPresetId={activePresetId}, remainingPresetCount={_state.Value.Presets.Count}, newActivePresetId={_state.Value.ActivePresetId}]");

                await PersistAndRefreshPresetListAsync();

                if (await _powerModeFeature.GetStateAsync() == PowerModeState.GodMode)
                    await _godModeController.ApplyStateAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't delete preset.", ex);

                await ShowSnackBarAsync(Resource.GodModeSettingsWindow_Error_Apply_Title, ex.Message);
                return;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(DeletePresetsButton_Click)}: {ex.Message}", ex);
        }
    }

    private async void AddPresetsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!_state.HasValue)
                return;

            var defaultName = GetUniquePresetName(GetDefaultPresetName(), _state.Value.Presets);
            var result = await MessageBoxHelper.ShowInputAsync(this, Resource.GodModeSettingsWindow_EditPreset_Title, Resource.GodModeSettingsWindow_EditPreset_Message, defaultName);
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Add preset dialog completed. [result={(result is null ? "<null>" : result)}, activePresetId={_state.Value.ActivePresetId}, presetCount={_state.Value.Presets.Count}]");
            if (string.IsNullOrWhiteSpace(result))
                return;

            FlushActivePresetToState();

            _state = AddPreset(_state.Value, result);

            try
            {
                var newActivePresetId = _state.Value.ActivePresetId;
                var newPreset = _state.Value.Presets[newActivePresetId];
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Adding God Mode preset. [newPresetId={newActivePresetId}, newPresetName={newPreset.Name}, presetCount={_state.Value.Presets.Count}]");

                await PersistAndRefreshPresetListAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't add preset.", ex);

                await ShowSnackBarAsync(Resource.GodModeSettingsWindow_Error_Apply_Title, ex.Message);
                return;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(AddPresetsButton_Click)}: {ex.Message}", ex);
        }
    }

    private async void DefaultFanCurve_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var state = await _godModeController.GetStateAsync();
            var preset = state.Presets[state.ActivePresetId];
            var data = preset.FanTableInfo?.Data;

            if (data is null)
                return;

            var defaultFanTable = await _godModeController.GetDefaultFanTableAsync();
            var defaultFanTableInfo = new FanTableInfo(data, defaultFanTable);
            var minimum = await _godModeController.GetMinimumFanTableAsync();
            _fanCurveControl.SetFanTableInfo(defaultFanTableInfo, minimum);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(DefaultFanCurve_Click)}: {ex.Message}", ex);
        }
    }

    private async Task ShowSnackBarAsync(string title, string? message)
    {
        _snackBar.Title = title;
        _snackBar.Content = message;
        _snackBar.IsCloseButtonEnabled = true;
        _snackBar.Icon = new SymbolIcon { Symbol = SymbolRegular.ErrorCircle24 };
        _snackBar.Appearance = ControlAppearance.Danger;
        _snackBar.Timeout = TimeSpan.FromSeconds(5);
        await _snackBar.ShowAsync();
    }

    private async Task ShowSuccessSnackBarAsync(string title, string? message)
    {
        _snackBar.Title = title;
        _snackBar.Content = message;
        // Success toasts auto-dismiss and do not need a close button.
        _snackBar.IsCloseButtonEnabled = false;
        _snackBar.Icon = new SymbolIcon { Symbol = SymbolRegular.Checkmark24 };
        _snackBar.Appearance = ControlAppearance.Success;
        _snackBar.Timeout = TimeSpan.FromMilliseconds(2800);
        await _snackBar.ShowAsync();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_defaults is null || _defaults.IsEmpty())
        {
            _loadButton.IsVisible = false;
            return;
        }

        var menuItems = _defaults
            .OrderBy(d => d.Key)
            .Select(d =>
            {
                var menuItem = new MenuItem { Header = d.Key.GetDisplayName() };
                menuItem.Click += async (_, _) => await SetDefaultsAsync(d.Value);
                return menuItem;
            });

        // AVALONIA: ContextMenu has no PlacementTarget — it opens at the pointer position.
        var contextMenu = new ContextMenu
        {
            Placement = PlacementMode.Bottom
        };

        foreach (var menuItem in menuItems)
            contextMenu.Items.Add(menuItem);

        _loadButton.ContextMenu = contextMenu;
        _loadButton.ContextMenu.Open();
    }

    private async void SaveAndCloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (await ApplyAsync())
                Close();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(SaveAndCloseButton_Click)}: {ex.Message}", ex);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ApplyAsync();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in {nameof(SaveButton_Click)}: {ex.Message}", ex);
        }
    }

    private void CpuLongTermPowerLimitSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (_cpuLongTermPowerLimitControl.Value > _cpuShortTermPowerLimitControl.Value)
            _cpuShortTermPowerLimitControl.Value = _cpuLongTermPowerLimitControl.Value;
    }

    private void CpuShortTermPowerLimitSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        if (_cpuLongTermPowerLimitControl.Value > _cpuShortTermPowerLimitControl.Value)
            _cpuLongTermPowerLimitControl.Value = _cpuShortTermPowerLimitControl.Value;
    }

    private void FanFullSpeedToggle_Click(object sender, RoutedEventArgs e)
    {
        _fanCurveCardControl.IsEnabled = !(_fanFullSpeedToggle.IsChecked ?? false);
        UpdateRiskWarnings();
    }

    private static void SetOffsetValue(NumberBox numberBox, int value, int minimum, int maximum)
    {
        var normalizedValue = Math.Clamp(value, minimum, maximum);
        numberBox.Value = normalizedValue;
        numberBox.Text = normalizedValue.ToString();
    }

    private static bool TryReadOffsetValue(NumberBox numberBox, int minimum, int maximum, out int value)
    {
        return TryNormalizeOffsetValue(numberBox.Value, minimum, maximum, out value);
    }

    internal static bool TryNormalizeOffsetValue(double? rawValue, int minimum, int maximum, out int value)
    {
        value = 0;
        if (rawValue is not { } numericValue
            || !double.IsFinite(numericValue)
            || numericValue < minimum
            || numericValue > maximum
            || numericValue != Math.Truncate(numericValue))
            return false;

        value = (int)numericValue;
        return true;
    }

    private static int GetRequiredOffsetValue(NumberBox numberBox, int minimum, int maximum) =>
        TryReadOffsetValue(numberBox, minimum, maximum, out var value)
            ? value
            : throw new InvalidOperationException("The offset value is invalid.");

    private bool TryValidateOffsetInputs(out string message)
    {
        if (_maxValueOffsetCardControl.IsVisible
            && !TryReadOffsetValue(_maxValueOffsetNumberBox, 0, 100, out _))
        {
            message = T(
                "GodModeSettingsWindow_Advanced_InvalidOffset_Message",
                "Enter a whole number from 0 to 100 before saving.");
            return false;
        }

        if (_minValueOffsetCardControl.IsVisible
            && !TryReadOffsetValue(_minValueOffsetNumberBox, -100, 0, out _))
        {
            message = T(
                "GodModeSettingsWindow_Advanced_InvalidOffset_Message",
                "Enter a whole number from -100 to 0 before saving.");
            return false;
        }

        message = string.Empty;
        return true;
    }

    private void UpdateRiskWarnings()
    {
        var fanFullSpeedEnabled = _fanFullSpeedToggle.IsChecked == true;
        _fanFullSpeedHeader.Warning = fanFullSpeedEnabled
            ? RemoveWarningHeading(Resource.GodModeSettingsWindow_Fans_Max_Message)
            : string.Empty;
        _fanFullSpeedHeader.WarningSeverity = CardHeaderWarningSeverity.Warning;

        var maxOffsetEnabled = TryReadOffsetValue(_maxValueOffsetNumberBox, 0, 100, out var maxValue) && maxValue != 0;
        _maxValueOffsetHeader.Warning = maxOffsetEnabled
            ? RemoveWarningHeading(Resource.GodModeSettingsWindow_Advanced_MaxOffset_Message)
            : string.Empty;
        _maxValueOffsetHeader.WarningSeverity = CardHeaderWarningSeverity.Critical;

        var minOffsetEnabled = TryReadOffsetValue(_minValueOffsetNumberBox, -100, 0, out var minValue) && minValue != 0;
        _minValueOffsetHeader.Warning = minOffsetEnabled
            ? RemoveWarningHeading(Resource.GodModeSettingsWindow_Advanced_MinOffset_Message)
            : string.Empty;
        _minValueOffsetHeader.WarningSeverity = CardHeaderWarningSeverity.Critical;
    }

    internal static string RemoveWarningHeading(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var lines = message.Replace("\r\n", "\n").Split('\n');
        if (lines.Length > 1)
        {
            var heading = lines[0].Trim();
            if (heading.Length <= 32 && (heading.Contains('!') || heading.Contains('！') || heading.EndsWith(':') || heading.EndsWith('：')))
                return string.Join(Environment.NewLine, lines.Skip(1)).Trim();
        }

        return message.Trim();
    }
}
}
