using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class DisplayNavigationSettingsContractTests
{
    [Fact]
    public void DisplaySettings_OmitKeyboardVisibilityWhenKeyboardBacklightIsUnsupported()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Windows",
            "Settings",
            "NavigationItemsSettingsWindow.xaml.cs"));
        var avaloniaSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsAvaloniaSettingsService.cs"));

        wpfSource.Should().Contain("KeyboardBacklightViewModel");
        wpfSource.Should().Contain("await keyboardViewModel.IsSupportedAsync()");
        wpfSource.Should().Contain("_keyboardCard.Visibility = keyboardSupported ? Visibility.Visible : Visibility.Collapsed");

        avaloniaSource.Should().Contain("GetKeyboardBacklightSupportedAsync");
        avaloniaSource.Should().Contain("KeyboardBacklightViewModel");
        avaloniaSource.Should().Contain("await keyboardViewModel.IsSupportedAsync().ConfigureAwait(false)");
        avaloniaSource.Should().Contain(".Where(entry => entry.Route != MainNavigation.Keyboard || keyboardSupported)");
    }
}
