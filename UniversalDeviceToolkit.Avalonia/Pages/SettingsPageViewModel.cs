using CommunityToolkit.Mvvm.ComponentModel;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.ViewModels;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Avalonia-specific ViewModel wrapping the shared SettingsNavigationViewModel.
/// Manages the left/right split layout: navigation list + content area.
/// </summary>
public partial class SettingsPageViewModel : ObservableObject
{
    private readonly SettingsNavigationViewModel _navModel;
    private readonly IPlatformServices _platformServices;
    private readonly Func<string, object?> _contentFactory;

    [ObservableProperty]
    private IReadOnlyList<NavigationItemViewModel> _navigationItems = Array.Empty<NavigationItemViewModel>();

    [ObservableProperty]
    private int _selectedNavigationIndex;

    [ObservableProperty]
    private object? _selectedContent;

    /// <summary>
    /// Completes after capability detection has populated the navigation list.
    /// Exposed so the host and tests can await the initial route state instead
    /// of depending on an unobserved constructor task.
    /// </summary>
    public Task Initialization { get; }

    public SettingsPageViewModel(
        IPlatformServices platformServices,
        Func<string, object?>? contentFactory = null)
    {
        _platformServices = platformServices ?? throw new ArgumentNullException(nameof(platformServices));
        _contentFactory = contentFactory ?? CreateContent;

        _navModel = new SettingsNavigationViewModel(Localization.AvaloniaLocalization.StringLocalizer);

        Initialization = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var isSupportedLegionMachine = false;
        try
        {
            isSupportedLegionMachine = await _platformServices.IsSupportedLegionMachineAsync();
        }
        catch
        {
            // Capability detection is optional. Keep the portable settings
            // pages reachable instead of leaving Settings without navigation.
        }

        _navModel.InitializeNavigationCommand.Execute(isSupportedLegionMachine);
        NavigationItems = _navModel.NavigationItems;

        if (NavigationItems.Count > 0)
        {
            SelectedNavigationIndex = 0;
            UpdateContent(0);
        }
    }

    partial void OnSelectedNavigationIndexChanged(int value)
    {
        UpdateContent(value);
    }

    private void UpdateContent(int index)
    {
        if (index < 0 || index >= NavigationItems.Count)
        {
            SelectedContent = null;
            return;
        }

        SelectedContent = _contentFactory(NavigationItems[index].Key);
    }

    private static object? CreateContent(string key) => key switch
        {
            "Appearance" => new SettingsAppearanceView(),
            "Application" => new SettingsApplicationBehaviorView(),
            "SmartKeys" => new SettingsSmartKeysView(),
            "Display" => new SettingsDisplayView(),
            "Update" => new SettingsUpdateView(),
            "Power" => new SettingsPowerView(),
            "Integrations" => new SettingsIntegrationsView(),
            _ => null
        };
}
