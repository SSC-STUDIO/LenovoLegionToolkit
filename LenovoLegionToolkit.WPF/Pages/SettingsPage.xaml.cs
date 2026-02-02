using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Controls.Settings;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Pages;

public partial class SettingsPage
{
    private readonly ApplicationSettings _settings = IoCContainer.Resolve<ApplicationSettings>();
    private readonly FnKeysDisabler _fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();

    private SettingsAppearanceControl? _appearanceControl;
    private SettingsApplicationBehaviorControl? _applicationBehaviorControl;
    private SettingsSmartKeysControl? _smartKeysControl;
    private SettingsDisplayControl? _displayControl;
    private SettingsUpdateControl? _updateControl;
    private SettingsPowerControl? _powerControl;
    private SettingsIntegrationsControl? _integrationsControl;

    private bool _isInitialized;

    public SettingsPage()
    {
        InitializeComponent();

        IsVisibleChanged += SettingsPage_IsVisibleChanged;

        InitializeNavigationItems();
    }

    private async void InitializeNavigationItems()
    {
        var mi = await Compatibility.GetMachineInformationAsync();
        var isSupportedLegionMachine = Compatibility.IsSupportedLegionMachine(mi);

        var navigationItems = new List<NavigationItem>
        {
            new() { Key = "Appearance", Title = "外观", Icon = SymbolRegular.PaintBrush24 },
            new() { Key = "Application", Title = "应用行为", Icon = SymbolRegular.Apps24 }
        };

        if (isSupportedLegionMachine)
        {
            navigationItems.Add(new() { Key = "SmartKeys", Title = "智能按键", Icon = SymbolRegular.Keyboard24 });
            navigationItems.Add(new() { Key = "Display", Title = "显示", Icon = SymbolRegular.Desktop24 });
        }

        navigationItems.Add(new() { Key = "Update", Title = Resource.SettingsPage_Update_Title, Icon = SymbolRegular.ArrowSync24 });

        if (isSupportedLegionMachine)
        {
            navigationItems.Add(new() { Key = "Power", Title = Resource.SettingsPage_Power_Title, Icon = SymbolRegular.Battery024 });
        }

        navigationItems.Add(new() { Key = "Integrations", Title = Resource.SettingsPage_Integrations_Title, Icon = SymbolRegular.PlugConnected24 });

        _navigationListBox.ItemsSource = navigationItems;
        _navigationListBox.SelectedIndex = 0;
    }

    private async void SettingsPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible && !_isInitialized)
        {
            _isInitialized = true;
            await RefreshAsync();
        }
    }

    private async Task RefreshAsync()
    {
        // Initialize all controls first
        _appearanceControl = new SettingsAppearanceControl();
        _applicationBehaviorControl = new SettingsApplicationBehaviorControl();
        _smartKeysControl = new SettingsSmartKeysControl();
        _displayControl = new SettingsDisplayControl();
        _updateControl = new SettingsUpdateControl();
        _powerControl = new SettingsPowerControl();
        _integrationsControl = new SettingsIntegrationsControl();

        // Wire up FnKeys toggle change event
        _applicationBehaviorControl.FnKeysStatusChanged += (sender, status) =>
        {
            if (_smartKeysControl != null)
                _smartKeysControl.UpdateVisibilityBasedOnFnKeys(status);
            if (_displayControl != null)
                _displayControl.UpdateVisibilityBasedOnFnKeys(status);
        };

        // Show first item immediately (Appearance control) - don't wait for loading
        _contentControl.Content = _appearanceControl;
        PlayTransitionAnimation();

        // Priority load: refresh the first visible control (Appearance) immediately
        await _appearanceControl.RefreshAsync();

        // Load other controls in parallel in the background (fire and forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.WhenAll(
                    _applicationBehaviorControl.RefreshAsync(),
                    _smartKeysControl.RefreshAsync(),
                    _displayControl.RefreshAsync(),
                    _powerControl.RefreshAsync(),
                    _integrationsControl.RefreshAsync()
                ).ConfigureAwait(false);

                await Dispatcher.InvokeAsync(() =>
                {
                    _updateControl.Refresh();
                });

                // Update visibility based on FnKeys status
                var fnKeysStatus = await _fnKeysDisabler.GetStatusAsync().ConfigureAwait(false);
                await Dispatcher.InvokeAsync(() =>
                {
                    _smartKeysControl?.UpdateVisibilityBasedOnFnKeys(fnKeysStatus);
                    _displayControl?.UpdateVisibilityBasedOnFnKeys(fnKeysStatus);
                });
            }
            catch
            {
                // Ignore errors in background loading
            }
        });
    }

    private async void NavigationListBox_SelectionChanged(object sender, SelectionChangedEventArgs? e)
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

    private void PlayTransitionAnimation()
    {
        if (Resources["ContentTransitionAnimation"] is Storyboard storyboard)
        {
            storyboard.Begin();
        }
    }

    private class NavigationItem
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public SymbolRegular Icon { get; set; }
    }
}
