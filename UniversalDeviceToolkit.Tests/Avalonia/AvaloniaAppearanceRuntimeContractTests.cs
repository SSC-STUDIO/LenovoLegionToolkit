using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaAppearanceRuntimeContractTests
{
    [Fact]
    public void AppearanceRuntime_UsesSharedWindowsSettingsAndPortableFallbackOnly()
    {
        var root = RepositoryPaths.FindRoot();
        var manager = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "AvaloniaAppearanceManager.cs"));
        var app = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "App.axaml.cs"));
        var page = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsAppearanceView.axaml.cs"));

        manager.Should().Contain("FromApplicationSettings(WindowsAvaloniaSettingsService.SharedApplicationSettings)");
        manager.Should().Contain("FromPortablePreferences(portableFallback)");
        manager.Should().Contain("store.AppFontStyle");
        manager.Should().Contain("store.AppTextSize");
        manager.Should().Contain("store.AppScale");
        app.Should().Contain("AvaloniaAppearanceManager.Apply(");
        app.Should().Contain("WindowsAvaloniaSettingsService.SharedApplicationSettings");
        page.Should().Contain("AvaloniaAppearanceManager.GetCurrentState(_themePrefs.Store)");
        page.Should().Contain("#if !WINDOWS");
        page.Should().Contain("_themePrefs.SynchronizeStore()");
    }

    [Fact]
    public void AppearanceRuntime_AppliesFontAndScaleToExistingMainWindowImmediately()
    {
        var root = RepositoryPaths.FindRoot();
        var manager = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "AvaloniaAppearanceManager.cs"));
        var window = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "MainWindow.axaml.cs"));
        var page = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsAppearanceView.axaml.cs"));

        manager.Should().Contain("window.FontFamily = new FontFamily(GetFontFamilyChain(_current.FontFamily))");
        manager.Should().Contain("window.FontSize = 15d * GetTextScale(_current.UiScale)");
        manager.Should().Contain("scaleHost.Scale = GetLayoutScale(_current.UiScale)");
        manager.Should().Contain("internal sealed class AvaloniaAppScaleHost : Decorator");
        manager.Should().Contain("Child.Measure(Divide(availableSize))");
        manager.Should().Contain("Child?.Arrange(new Rect(Divide(finalSize)))");
        window.Should().Contain("AvaloniaAppearanceManager.Attach(this)");
        page.Should().Contain("AvaloniaAppearanceManager.Apply(_themePrefs.Store)");
    }
}
