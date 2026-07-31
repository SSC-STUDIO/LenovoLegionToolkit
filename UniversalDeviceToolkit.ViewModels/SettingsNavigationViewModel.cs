using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.ViewModels;

/// <summary>
/// Platform-agnostic navigation data model for the Settings page.
/// Extracted from the WPF SettingsViewModel to remove WPF-UI icon dependencies.
/// </summary>
public partial class SettingsNavigationViewModel : ObservableObject
{
    private readonly IStringLocalizer _localizer;

    [ObservableProperty]
    private List<NavigationItemViewModel> _navigationItems = new();

    [ObservableProperty]
    private int _selectedNavigationIndex;

    [ObservableProperty]
    private bool _isSupportedLegionMachine;

    [ObservableProperty]
    private bool _isInitialized;

    public SettingsNavigationViewModel(IStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    /// <summary>
    /// Initializes navigation items based on machine capabilities.
    /// </summary>
    /// <param name="isSupportedLegionMachine">Whether the current machine is a supported Legion device.</param>
    [RelayCommand]
    private void InitializeNavigation(bool isSupportedLegionMachine)
    {
        IsSupportedLegionMachine = isSupportedLegionMachine;

        var items = new List<NavigationItemViewModel>
        {
            new("Appearance", _localizer.GetString("SettingsPage_Navigation_Appearance", "Appearance"), "PaintBrush24"),
            new("Application", _localizer.GetString("SettingsPage_Navigation_Application", "Application"), "Apps24"),
        };

        if (IsSupportedLegionMachine)
        {
            items.Add(new("SmartKeys", _localizer.GetString("SettingsPage_Navigation_SmartKeys", "Smart Keys"), "Keyboard24"));
            items.Add(new("Display", _localizer.GetString("SettingsPage_Navigation_Display", "Display"), "Desktop24"));
        }

        items.Add(new("Update", _localizer.GetString("SettingsPage_Update_Title", "Update"), "ArrowSync24"));

        if (IsSupportedLegionMachine)
            items.Add(new("Power", _localizer.GetString("SettingsPage_Power_Title", "Power"), "Battery024"));

        items.Add(new("Integrations", _localizer.GetString("SettingsPage_Integrations_Title", "Integrations"), "PlugConnected24"));

        NavigationItems = items;
        SelectedNavigationIndex = 0;
    }

    [RelayCommand]
    private Task RefreshAsync()
    {
        if (IsInitialized)
            return Task.CompletedTask;

        IsInitialized = true;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents a single navigation item in the Settings page.
/// Uses a string icon identifier instead of WPF-UI SymbolRegular enum.
/// </summary>
public record NavigationItemViewModel(string Key, string Title, string IconIdentifier);
