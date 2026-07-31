using System.Globalization;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.ViewModels;

namespace UniversalDeviceToolkit.Avalonia.Pages;

/// <summary>
/// Avalonia-specific ViewModel wrapping the shared SettingsNavigationViewModel.
/// Manages the left/right split layout: navigation list + content area.
/// </summary>
public partial class SettingsPageViewModel : ObservableObject
{
    private readonly SettingsNavigationViewModel _navModel;

    public IReadOnlyList<NavigationItemViewModel> NavigationItems => _navModel.NavigationItems;

    [ObservableProperty]
    private int _selectedNavigationIndex;

    [ObservableProperty]
    private object? _selectedContent;

    public SettingsPageViewModel()
    {
        // Use a pass-through localizer that returns fallback strings (no .resx dependency in Avalonia prototype).
        var localizer = new FallbackStringLocalizer();
        _navModel = new SettingsNavigationViewModel(localizer);
        _navModel.InitializeNavigationCommand.Execute(false);

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
                    Text = $"Settings for \"{item.Title}\" will be implemented here.",
                    Opacity = 0.7,
                    TextWrapping = TextWrapping.Wrap,
                }
            }
        };
    }
}

/// <summary>
/// Minimal IStringLocalizer that always returns the fallback value.
/// Suitable for the Avalonia prototype before full resource integration.
/// </summary>
internal sealed class FallbackStringLocalizer : IStringLocalizer
{
    public string GetString(string key, string fallback = "") => fallback;
    public CultureInfo CurrentCulture { get; set; } = CultureInfo.CurrentUICulture;
}
