using System.Collections.Generic;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.WPF.Windows;

namespace LenovoLegionToolkit.WPF.Windows.Settings;

public partial class NavigationItemsSettingsWindow : BaseWindow
{
    private readonly ApplicationSettings _applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
    private bool _isInitializing;

    public NavigationItemsSettingsWindow()
    {
        InitializeComponent();
        Loaded += NavigationItemsSettingsWindow_Loaded;
    }

    private void NavigationItemsSettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;
        
        // Initialize the toggle states of all navigation items
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;
        
        _keyboardToggle.IsChecked = GetNavigationItemVisibility("keyboard", visibilitySettings);
        _automationToggle.IsChecked = GetNavigationItemVisibility("automation", visibilitySettings);
        _macroToggle.IsChecked = GetNavigationItemVisibility("macro", visibilitySettings);
        _windowsOptimizationToggle.IsChecked = GetNavigationItemVisibility("windowsOptimization", visibilitySettings);

        _pluginExtensionsToggle.IsChecked = GetNavigationItemVisibility("pluginExtensions", visibilitySettings);
        _donateToggle.IsChecked = GetNavigationItemVisibility("donate", visibilitySettings);
        _aboutToggle.IsChecked = GetNavigationItemVisibility("about", visibilitySettings);
        
        _isInitializing = false;
    }

    private bool GetNavigationItemVisibility(string pageTag, Dictionary<string, bool> visibilitySettings)
    {
        // Dashboard and settings must always be visible but are not configured in this window
        if (pageTag == "dashboard" || pageTag == "settings")
            return true;
            
        if (visibilitySettings.TryGetValue(pageTag, out var visibility))
            return visibility;
            
        // Visible by default
        return true;
    }

    private void NavigationItemToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
            return;

        if (sender is not Wpf.Ui.Controls.ToggleSwitch toggleSwitch || toggleSwitch.Tag is not string pageTag)
            return;

        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;
        visibilitySettings[pageTag] = toggleSwitch.IsChecked == true;
        _applicationSettings.SynchronizeStore();

        // Update navigation items visibility in main window
        if (Application.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateNavigationVisibility();
        }
    }
}