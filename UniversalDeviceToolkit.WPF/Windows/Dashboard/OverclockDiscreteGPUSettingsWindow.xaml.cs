using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Windows.Dashboard
{
public partial class OverclockDiscreteGPUSettingsWindow
{
    private const string MHZ = "MHz";

    private readonly GPUOverclockController _gpuOverclockController = IoCContainer.Resolve<GPUOverclockController>();
    private Guid _activeProfileId;
    private bool _isRefreshingProfiles;

    public OverclockDiscreteGPUSettingsWindow()
    {
        InitializeComponent();

        var (enabled, info) = _gpuOverclockController.GetState();

        _applyCloseGrid.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        _saveGrid.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;

        _coreSlider.Maximum = GPUOverclockController.GetMaxCoreDeltaMhz();
        _memorySlider.Maximum = GPUOverclockController.GetMaxMemoryDeltaMhz();

        RefreshProfiles();
        SetSliders(info);
    }

    private void CoreSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _coreLabel.Content = $"{(int)_coreSlider.Value:+0;-0;0} {MHZ}";

    private void MemorySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _memoryLabel.Content = $"{(int)_memorySlider.Value:+0;-0;0} {MHZ}";

    private void ProfilesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshingProfiles)
            return;

        if (!_profilesComboBox.TryGetSelectedItem<KeyValuePair<Guid, GPUOverclockSettings.GPUOverclockSettingsStore.Profile>>(out var item))
            return;

        if (item.Key == _activeProfileId)
            return;

        SaveProfile();
        _gpuOverclockController.SetActiveProfile(item.Key);
        _activeProfileId = item.Key;
        SetSliders(item.Value.Info);
        RefreshProfiles();
    }

    private async void RenameProfileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!TryGetActiveProfile(out var profile))
                return;

            var result = await MessageBoxHelper.ShowInputAsync(this, Resource.Rename, Resource.AutomationPage_RenamePipeline_Placeholder, profile.Name);
            if (string.IsNullOrEmpty(result))
                return;

            _gpuOverclockController.RenameProfile(_activeProfileId, result);
            RefreshProfiles();
        }
        catch (Exception ex) { /* Logging excluded — no Log access in this scope */ }
    }

    private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
    {
        _gpuOverclockController.DeleteProfile(_activeProfileId);
        RefreshProfiles();

        var (_, info) = _gpuOverclockController.GetState();
        SetSliders(info);
    }

    private async void AddProfileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveProfile();

            var result = await MessageBoxHelper.ShowInputAsync(this, Resource.Add, Resource.AutomationPage_AddManualPipeline_Placeholder);
            if (string.IsNullOrEmpty(result))
                return;

            var profileId = _gpuOverclockController.AddProfile(result, GetCurrentInfo());
            _activeProfileId = profileId;
            RefreshProfiles();
        }
        catch (Exception ex) { /* Logging excluded — no Log access in this scope */ }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Save();
            await ApplyAsync();
        }
        catch (Exception ex) { /* Logging excluded — no Log access in this scope */ }
    }

    private async void ApplyAndCloseButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Save();
            await ApplyAsync();
            Close();
        }
        catch (Exception ex) { /* Logging excluded — no Log access in this scope */ }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Save();
        Close();
    }

    private void Save()
    {
        var (enabled, _) = _gpuOverclockController.GetState();
        _gpuOverclockController.SaveState(enabled, _activeProfileId, GetCurrentInfo());
    }

    private async Task ApplyAsync() => await _gpuOverclockController.ApplyStateAsync();

    private void SaveProfile() => _gpuOverclockController.SaveProfile(_activeProfileId, GetCurrentInfo());

    private GPUOverclockInfo GetCurrentInfo() => new((int)_coreSlider.Value, (int)_memorySlider.Value);

    private void SetSliders(GPUOverclockInfo info)
    {
        _coreSlider.Value = info.CoreDeltaMhz;
        _memorySlider.Value = info.MemoryDeltaMhz;

        _coreLabel.Content = $"{(int)_coreSlider.Value:+0;-0;0} {MHZ}";
        _memoryLabel.Content = $"{(int)_memorySlider.Value:+0;-0;0} {MHZ}";
    }

    private void RefreshProfiles()
    {
        _isRefreshingProfiles = true;

        try
        {
            var profiles = _gpuOverclockController.GetProfiles();
            _activeProfileId = _gpuOverclockController.GetActiveProfileId();
            _profilesComboBox.SetItems(profiles.OrderBy(kv => kv.Value.Name), profiles.First(kv => kv.Key == _activeProfileId), kv => kv.Value.Name);
            _deleteProfileButton.IsEnabled = profiles.Count > 1;
        }
        finally
        {
            _isRefreshingProfiles = false;
        }
    }

    private bool TryGetActiveProfile(out GPUOverclockSettings.GPUOverclockSettingsStore.Profile profile)
    {
        if (_gpuOverclockController.GetProfiles().TryGetValue(_activeProfileId, out var result))
        {
            profile = result;
            return true;
        }

        profile = new();
        return false;
    }
}
}

