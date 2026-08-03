using Avalonia.Controls;
using Avalonia.Media;
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

    public IReadOnlyList<NavigationItemViewModel> NavigationItems => _navModel.NavigationItems;

    [ObservableProperty]
    private int _selectedNavigationIndex;

    [ObservableProperty]
    private object? _selectedContent;

    public SettingsPageViewModel(IPlatformServices platformServices)
    {
        _platformServices = platformServices;

        _navModel = new SettingsNavigationViewModel(Localization.AvaloniaLocalization.StringLocalizer);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var isSupportedLegionMachine = await _platformServices.IsSupportedLegionMachineAsync();
        _navModel.InitializeNavigationCommand.Execute(isSupportedLegionMachine);

        if (_navModel.NavigationItems.Count > 0)
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

        var item = NavigationItems[index];
        SelectedContent = item.Key switch
        {
            "Appearance" => new SettingsAppearanceView(),
            "Application" => new SettingsApplicationBehaviorView(),
            "Display" => new SettingsDisplayView(),
            _ => BuildPlaceholderView(item)
        };
    }

    private static Control BuildPlaceholderView(NavigationItemViewModel item)
    {
        return new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = item.Title,
                    FontSize = 22,
                    FontWeight = FontWeight.SemiBold,
                },
                new TextBlock
                {
                    Text = string.Format(
                        Localization.AvaloniaLocalization.GetString(
                            "Settings_Placeholder",
                            "Settings for \"{0}\" will be implemented here."),
                        item.Title),
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap,
                }
            }
        };
    }
}
