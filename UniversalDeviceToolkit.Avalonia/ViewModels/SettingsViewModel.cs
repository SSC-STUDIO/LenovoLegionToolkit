using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly FnKeysDisabler _fnKeysDisabler;
    private static CultureInfo ActiveCulture => Resource.Culture ?? CultureInfo.CurrentUICulture;
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, ActiveCulture);

    [ObservableProperty]
    private List<NavigationItemViewModel> _navigationItems = new();

    [ObservableProperty]
    private int _selectedNavigationIndex;

    [ObservableProperty]
    private bool _isSupportedLegionMachine;

    [ObservableProperty]
    private bool _isInitialized;

    public SettingsViewModel(ApplicationSettings settings, FnKeysDisabler fnKeysDisabler)
    {
        // ApplicationSettings retained for Autofac resolution shape; not stored until bindings need it.
        _ = settings;
        _fnKeysDisabler = fnKeysDisabler;
    }

    [RelayCommand]
    private async Task InitializeNavigationAsync()
    {
        var mi = await MachineCompatibility.GetMachineInformationAsync();
        IsSupportedLegionMachine = MachineCompatibility.IsSupportedLegionMachine(mi);

        var items = new List<NavigationItemViewModel>
        {
            new("Appearance", T("SettingsPage_Navigation_Appearance", "Appearance"), SymbolRegular.PaintBrush24),
            new("Application", T("SettingsPage_Navigation_Application", "Application"), SymbolRegular.Apps24),
        };

        if (IsSupportedLegionMachine)
        {
            items.Add(new("SmartKeys", T("SettingsPage_Navigation_SmartKeys", "Smart Keys"), SymbolRegular.Keyboard24));
            items.Add(new("Display", T("SettingsPage_Navigation_Display", "Display"), SymbolRegular.Desktop24));
        }

        items.Add(new("Update", Resource.SettingsPage_Update_Title, SymbolRegular.ArrowSync24));

        if (IsSupportedLegionMachine)
            items.Add(new("Power", Resource.SettingsPage_Power_Title, SymbolRegular.Battery024));

        items.Add(new("Integrations", Resource.SettingsPage_Integrations_Title, SymbolRegular.PlugConnected24));

        NavigationItems = items;
        SelectedNavigationIndex = 0;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsInitialized)
            return;

        IsInitialized = true;
        await Task.CompletedTask;
    }

    public FnKeysDisabler FnKeysDisabler => _fnKeysDisabler;
}

public record NavigationItemViewModel(string Key, string Title, SymbolRegular Icon);


