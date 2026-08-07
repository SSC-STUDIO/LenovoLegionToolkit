using Avalonia.Media;
using Avalonia.Styling;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaThemeManagerTests
{
    [Theory]
    [InlineData("Light", "Light")]
    [InlineData("light", "Light")]
    [InlineData("Dark", "Dark")]
    [InlineData("dark", "Dark")]
    [InlineData("System", "Default")]
    [InlineData("", "Default")]
    [InlineData(null, "Default")]
    [InlineData("Unknown", "Default")]
    public void MapThemeVariant_ShouldMapPersistedThemeNames(string? theme, string expectedName)
    {
        var expected = expectedName switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
        AvaloniaThemeManager.MapThemeVariant(theme).Should().Be(expected);
    }

    [Theory]
    [InlineData("Compact", 0.90)]
    [InlineData("Standard", 1.00)]
    [InlineData("Large", 1.10)]
    [InlineData("ExtraLarge", 1.25)]
    [InlineData("standard", 1.00)]
    [InlineData("", 1.00)]
    [InlineData(null, 1.00)]
    [InlineData("Unknown", 1.00)]
    public void ResolveUiScaleFactor_ShouldMapUiScaleNames(string? uiScale, double expected)
    {
        AvaloniaThemeManager.ResolveUiScaleFactor(uiScale).Should().Be(expected);
    }

    [Theory]
    [InlineData("Compact", 0.80)]
    [InlineData("Small", 0.90)]
    [InlineData("Standard", 1.00)]
    [InlineData("Large", 1.10)]
    [InlineData("ExtraLarge", 1.25)]
    [InlineData("Unknown", 1.00)]
    public void ResolveAppScaleFactor_ShouldMapAppScaleEnumNames(string? appScale, double expected)
    {
        AvaloniaThemeManager.ResolveAppScaleFactor(appScale).Should().Be(expected);
    }

    [Theory]
    [InlineData("#FF0078D4", 0xFF, 0x00, 0x78, 0xD4)]
    [InlineData("#0078D4", 0xFF, 0x00, 0x78, 0xD4)]
    [InlineData("0078D4", 0xFF, 0x00, 0x78, 0xD4)]
    [InlineData("#000000", 0xFF, 0x00, 0x00, 0x00)]
    [InlineData("#ffffff", 0xFF, 0xFF, 0xFF, 0xFF)]
    public void ParseAccentColor_ShouldAcceptRgbHexFormats(string hex, byte a, byte r, byte g, byte b)
    {
        var color = AvaloniaThemeManager.ParseAccentColor(hex);
        color.Should().NotBeNull();
        color!.Value.A.Should().Be(a);
        color.Value.R.Should().Be(r);
        color.Value.G.Should().Be(g);
        color.Value.B.Should().Be(b);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    [InlineData("red")]
    [InlineData("#FF0078D")]
    public void ParseAccentColor_ShouldRejectInvalidHex(string? hex)
    {
        AvaloniaThemeManager.ParseAccentColor(hex).Should().BeNull();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("Default", null)]
    [InlineData("default", null)]
    [InlineData("Segoe UI Variable", "Segoe UI Variable")]
    [InlineData("Microsoft YaHei UI", "Microsoft YaHei UI")]
    public void ResolveFontFamily_ShouldResolvePersistedFontNames(string? fontFamily, string? expected)
    {
        AvaloniaThemeManager.ResolveFontFamily(fontFamily).Should().Be(expected);
    }

    [Theory]
    [InlineData("FluentVariable", "Segoe UI Variable")]
    [InlineData("YaHeiUI", "Microsoft YaHei UI")]
    [InlineData("DengXian", "DengXian")]
    [InlineData("NotoSans", "Noto Sans CJK SC")]
    [InlineData("SimHei", "SimHei")]
    [InlineData("SimSun", "SimSun")]
    [InlineData("KaiTi", "KaiTi")]
    [InlineData("Default", null)]
    [InlineData(null, null)]
    [InlineData("Unknown", null)]
    public void ResolveWindowsFontStyleName_ShouldMapLibFontStyleEnumNames(string? appFontStyle, string? expected)
    {
        AvaloniaThemeManager.ResolveWindowsFontStyleName(appFontStyle).Should().Be(expected);
    }

    [Fact]
    public void Startup_ShouldDelegateThemeApplicationToThemeManager()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));

        source.Should().Contain("private void ApplyPersistedTheme() => AvaloniaThemeManager.Instance.Apply();");
    }

    [Fact]
    public void MainWindow_ShouldSubscribeToThemeManagerScaleAndBackdropHooks()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml.cs"));
        var markup = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml"));

        source.Should().Contain("AvaloniaThemeManager.Instance.ThemeApplied += OnThemeApplied;");
        source.Should().Contain("AvaloniaThemeManager.Instance.UiScaleChanged += OnUiScaleChanged;");
        source.Should().Contain("private void OnThemeApplied(object? sender, EventArgs e) => ApplyWindowBackdrop();");
        source.Should().Contain("ContentScaleTransform.LayoutTransform = new ScaleTransform(scale, scale);");
        source.Should().Contain("AvaloniaThemeManager.Instance.Reapply();");
        markup.Should().Contain("x:Name=\"ContentScaleTransform\"");
        markup.Should().Contain("LayoutTransformControl");
    }
}
