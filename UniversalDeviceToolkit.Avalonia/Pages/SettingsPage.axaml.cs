using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Settings;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Pages
{
public partial class SettingsPage : global::Avalonia.Controls.UserControl
{
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    private readonly FnKeysDisabler _fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();

    private SettingsAppearanceControl? _appearanceControl;
    private SettingsApplicationBehaviorControl? _applicationBehaviorControl;
    private SettingsSmartKeysControl? _smartKeysControl;
    private SettingsDisplayControl? _displayControl;
    private SettingsUpdateControl? _updateControl;
    private SettingsPowerControl? _powerControl;
    private SettingsIntegrationsControl? _integrationsControl;

    private bool _supportsLenovoHardwareControls;

    private bool _isInitialized;

    public SettingsPage()
    {
        InitializeComponent();

        PropertyChanged += SettingsPage_PropertyChanged;

        _ = InitializeNavigationItems();
    }

    private async Task InitializeNavigationItems()
    {
        try
        {
            var mi = await MachineCompatibility.GetMachineInformationAsync();
            var deviceAvailability = MachineCompatibility.GetDeviceFeatureAvailability(mi);
            _supportsLenovoHardwareControls = !deviceAvailability.HiddenFeatures.Contains("lenovo-hardware-controls");

            var navigationItems = new List<NavigationItem>
            {
                new() { Key = "Appearance", Title = T("SettingsPage_Navigation_Appearance", "Appearance"), Icon = SymbolRegular.PaintBrush24 },
                new() { Key = "Application", Title = T("SettingsPage_Navigation_Application", "Application"), Icon = SymbolRegular.Apps24 }
            };

            if (_supportsLenovoHardwareControls)
            {
                navigationItems.Add(new() { Key = "SmartKeys", Title = T("SettingsPage_Navigation_SmartKeys", "Smart Keys"), Icon = SymbolRegular.Keyboard24 });
                navigationItems.Add(new() { Key = "Display", Title = T("SettingsPage_Navigation_Display", "Display"), Icon = SymbolRegular.Desktop24 });
            }

            navigationItems.Add(new() { Key = "Update", Title = Resource.SettingsPage_Update_Title, Icon = SymbolRegular.ArrowSync24 });

            if (_supportsLenovoHardwareControls)
            {
                navigationItems.Add(new() { Key = "Power", Title = Resource.SettingsPage_Power_Title, Icon = SymbolRegular.Battery024 });
            }

            navigationItems.Add(new() { Key = "Integrations", Title = Resource.SettingsPage_Integrations_Title, Icon = SymbolRegular.PlugConnected24 });

            _navigationListBox.ItemsSource = navigationItems;
            _navigationListBox.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error initializing navigation items: {ex.Message}");
        }
    }

    private async void SettingsPage_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Visual.IsVisibleProperty)
            return;

        try
        {
            if (IsVisible && !_isInitialized)
            {
                _isInitialized = true;
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _loader.IsLoading = false;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error initializing settings page.", ex);
        }
    }

    private async Task RefreshAsync()
    {
        // Initialize all controls first
        try
        {
            _appearanceControl = new SettingsAppearanceControl();
        }
        catch (Exception ex)
        {
            Log.Instance.Error("Failed to load appearance settings control.", ex);
            _appearanceControl = null;
            RemoveNavigationItem("Appearance");
        }

        _applicationBehaviorControl = new SettingsApplicationBehaviorControl();
        _smartKeysControl = _supportsLenovoHardwareControls ? new SettingsSmartKeysControl() : null;
        _displayControl = _supportsLenovoHardwareControls ? new SettingsDisplayControl() : null;
        _updateControl = new SettingsUpdateControl();
        _powerControl = _supportsLenovoHardwareControls ? new SettingsPowerControl() : null;
        _integrationsControl = new SettingsIntegrationsControl();

        // Wire up FnKeys toggle change event
        _applicationBehaviorControl.FnKeysStatusChanged += (sender, status) =>
        {
            if (_smartKeysControl != null)
                _smartKeysControl.UpdateVisibilityBasedOnFnKeys(status);
            if (_displayControl != null)
                _displayControl.UpdateVisibilityBasedOnFnKeys(status);
        };

        // Show first available control immediately - don't wait for loading
        if (_appearanceControl is not null)
        {
            _contentControl.Content = _appearanceControl;
            PlayTransitionAnimation();
            await _appearanceControl.RefreshAsync();
        }
        else
        {
            _contentControl.Content = _applicationBehaviorControl;
            PlayTransitionAnimation();
            SelectNavigationItem("Application");
            await _applicationBehaviorControl.RefreshAsync();
        }

        // Initial settings data is ready - crossfade the skeleton out.
        _loader.IsLoading = false;

        // Load other controls in the background, but keep WPF control updates on the UI dispatcher.
        _ = RefreshRemainingControlsAsync();
    }

    private async Task RefreshRemainingControlsAsync()
    {
        try
        {
        await Task.WhenAll(
            _applicationBehaviorControl!.RefreshAsync(),
            _smartKeysControl?.RefreshAsync() ?? Task.CompletedTask,
            _displayControl?.RefreshAsync() ?? Task.CompletedTask,
            _powerControl?.RefreshAsync() ?? Task.CompletedTask,
            _integrationsControl!.RefreshAsync()
        );

            _updateControl?.Refresh();

            // Update visibility based on FnKeys status
            var fnKeysStatus = await _fnKeysDisabler.GetStatusAsync();
            _smartKeysControl?.UpdateVisibilityBasedOnFnKeys(fnKeysStatus);
            _displayControl?.UpdateVisibilityBasedOnFnKeys(fnKeysStatus);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Background settings refresh failed.", ex);
        }
    }

    private async void NavigationListBox_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
    {
        try
        {
            if (_navigationListBox.SelectedItem is not NavigationItem selectedItem)
                return;

            UserControl? controlToShow = selectedItem.Key switch
            {
                "Appearance" => _appearanceControl,
                "Application" => _applicationBehaviorControl,
                "SmartKeys" => _smartKeysControl,
                "Display" => _displayControl,
                "Update" => _updateControl,
                "Power" => _powerControl,
                "Integrations" => _integrationsControl,
                _ => null
            };

            if (controlToShow != null)
            {
                _contentControl.Content = controlToShow;
                PlayTransitionAnimation();
            }

            // Refresh the selected control immediately if it's not the first one (Appearance)
            if (selectedItem.Key != "Appearance")
            {
                switch (selectedItem.Key)
                {
                    case "Application":
                        if (_applicationBehaviorControl != null)
                            await _applicationBehaviorControl.RefreshAsync();
                        break;
                    case "SmartKeys":
                        if (_smartKeysControl != null)
                            await _smartKeysControl.RefreshAsync();
                        break;
                    case "Display":
                        if (_displayControl != null)
                            await _displayControl.RefreshAsync();
                        break;
                    case "Update":
                        if (_updateControl != null)
                            _updateControl.Refresh();
                        break;
                    case "Power":
                        if (_powerControl != null)
                            await _powerControl.RefreshAsync();
                        break;
                    case "Integrations":
                        if (_integrationsControl != null)
                            await _integrationsControl.RefreshAsync();
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error navigating settings page.", ex);
        }
    }

    private void RemoveNavigationItem(string key)
    {
        if (_navigationListBox.ItemsSource is not IList<NavigationItem> items)
            return;

        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].Key != key)
                continue;

            items.RemoveAt(i);
            if (_navigationListBox.SelectedIndex >= items.Count)
                _navigationListBox.SelectedIndex = Math.Max(0, items.Count - 1);
            return;
        }
    }

    private void SelectNavigationItem(string key)
    {
        if (_navigationListBox.ItemsSource is not IList<NavigationItem> items)
            return;

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Key == key)
            {
                _navigationListBox.SelectedIndex = i;
                return;
            }
        }
    }

    private void PlayTransitionAnimation()
    {
        if (Resources["ContentTransitionAnimation"] is Style transitionStyle)
        {
            foreach (var animation in transitionStyle.Animations)
                _ = ((global::Avalonia.Animation.Animation)animation).RunAsync(_contentControl, CancellationToken.None)
                    .ContinueWith(t2 => _ = t2.Exception, TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    private class NavigationItem
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public SymbolRegular Icon { get; set; }
    }
}
}
