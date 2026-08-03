using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using UniversalDeviceToolkit.Shared.Settings;
using RoutedEventArgs = global::Avalonia.Interactivity.RoutedEventArgs;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class SettingsAppearanceView : UserControl
{
    // Mirrors UniversalDeviceToolkit.Lib.AccentColorPresets.Swatches. Those presets live in the
    // Windows-only Lib assembly (net10.0-windows, UseWindowsForms) which this cross-platform
    // (net10.0) project cannot reference, so the values are duplicated here and must be kept in
    // sync with the base layer.
    private static readonly (byte R, byte G, byte B, string Key)[] AccentPresets =
    [
        (0, 120, 212, "Blue"),
        (177, 70, 194, "Purple"),
        (227, 0, 140, "Pink"),
        (232, 17, 35, "Red"),
        (247, 99, 12, "Orange"),
        (255, 185, 0, "Amber"),
        (16, 124, 16, "Green"),
        (128, 128, 128, "Gray"),
    ];

    // Accent resource keys overridden on Application.Current when applying a custom accent color.
    private static readonly string[] AccentResourceKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
    ];

    // Guards initialization so refreshing the UI does not re-enter the change handlers.
    private bool _isRefreshing;

    // Cross-platform theme/accent persistence. Uses a dedicated file (avalonia-theme.json) via the
    // Lib.Shared AbstractSettings base with a primitive-typed DTO, so this cross-platform (net10.0)
    // project can persist theme choices without referencing the Windows-only Lib enums. See Task #59.
    private static readonly AvaloniaThemePreferences _themePrefs = new();

    // Local UI state, mirrored to _themePrefs on change and restored from it on load.
    private bool _applyAccentColorToTheme = true;
    private Color? _selectedAccent;

    public SettingsAppearanceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        _isRefreshing = true;
        try
        {
            RestorePreferences();

            ApplyAccentColorToThemeCheckBox.IsChecked = _applyAccentColorToTheme;
            AccentSwatches.ItemsSource = BuildSwatches();
            UpdateThemeCardSelection(GetCurrentThemeTag());
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    // Restores persisted theme/accent preferences into the live application and local UI state.
    // Runs under the _isRefreshing guard (see OnLoaded) so it does not re-enter change handlers.
    private void RestorePreferences()
    {
        var store = _themePrefs.Store;

        var variant = store.Theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
        if (Application.Current is { } app)
            app.RequestedThemeVariant = variant;

        _applyAccentColorToTheme = store.ApplyAccentColorToTheme;

        if (!store.UseSystemAccent
            && !string.IsNullOrWhiteSpace(store.AccentColorHex)
            && Color.TryParse(store.AccentColorHex, out var accent))
        {
            _selectedAccent = accent;
            if (_applyAccentColorToTheme)
                ApplyAccentColor(accent);
        }
        else
        {
            _selectedAccent = null;
        }
    }

    private static string GetCurrentThemeTag()
    {
        var variant = Application.Current?.RequestedThemeVariant;
        if (variant == ThemeVariant.Light)
            return "Light";
        if (variant == ThemeVariant.Dark)
            return "Dark";
        return "System";
    }

    private void ThemeCard_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || sender is not Button { Tag: string tag })
            return;

        var variant = tag switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        if (Application.Current is { } app)
            app.RequestedThemeVariant = variant;

        UpdateThemeCardSelection(tag);

        _themePrefs.Store.Theme = tag;
        _themePrefs.SynchronizeStore();
    }

    private void ApplyAccentColorToThemeCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _applyAccentColorToTheme = ApplyAccentColorToThemeCheckBox.IsChecked == true;

        // When enabled, re-apply the currently selected accent; when disabled, fall back to
        // the framework default accent (do not override).
        if (_applyAccentColorToTheme && _selectedAccent is { } color)
            ApplyAccentColor(color);
        else
            ClearAccentOverride();

        _themePrefs.Store.ApplyAccentColorToTheme = _applyAccentColorToTheme;
        _themePrefs.SynchronizeStore();
    }

    private void AccentColorSwatch_Click(object? sender, RoutedEventArgs e)
    {
        if (_isRefreshing || sender is not Button { Tag: AccentSwatchItem item })
            return;

        if (item.IsSystem)
        {
            // AccentColorSource = System: fall back to the framework default accent.
            _selectedAccent = null;
            ClearAccentOverride();

            _themePrefs.Store.UseSystemAccent = true;
            _themePrefs.Store.AccentColorHex = null;
        }
        else
        {
            // AccentColorSource = Custom: remember the chosen color and apply it best-effort.
            _selectedAccent = Color.FromRgb(item.R, item.G, item.B);
            if (_applyAccentColorToTheme)
                ApplyAccentColor(_selectedAccent.Value);
            else
                ClearAccentOverride();

            _themePrefs.Store.UseSystemAccent = false;
            _themePrefs.Store.AccentColorHex = string.Format("#{0:X2}{1:X2}{2:X2}", item.R, item.G, item.B);
        }

        _themePrefs.SynchronizeStore();

        // Rebuild the swatch list so the newly selected item shows its persistent highlight.
        AccentSwatches.ItemsSource = BuildSwatches();
    }

    private AccentSwatchItem[] BuildSwatches()
    {
        var items = new List<AccentSwatchItem> { CreateSystemSwatch() };
        foreach (var (r, g, b, key) in AccentPresets)
        {
            items.Add(new AccentSwatchItem
            {
                Brush = new SolidColorBrush(Color.FromRgb(r, g, b)),
                Key = key,
                R = r,
                G = g,
                B = b,
            });
        }

        ApplySwatchSelection(items);
        return items.ToArray();
    }

    // Marks the swatch matching the current accent as selected (or the system swatch when no custom
    // accent is set) and precomputes its selection border. Selection uses a 2.5px accent border to
    // match the theme preview cards (see UpdateCardBorder); unselected swatches keep the default 1px
    // card stroke. Driven by _selectedAccent, which RestorePreferences initializes from the persisted
    // UseSystemAccent / AccentColorHex state.
    private void ApplySwatchSelection(IReadOnlyList<AccentSwatchItem> items)
    {
        var accentBrush = GetAccentBrush();
        var defaultStroke = GetDefaultStrokeBrush();

        foreach (var item in items)
        {
            var selected = item.IsSystem
                ? _selectedAccent is null
                : _selectedAccent is { } c && c.R == item.R && c.G == item.G && c.B == item.B;

            item.IsSelected = selected;
            item.SelectionBorderBrush = selected ? accentBrush : defaultStroke;
            item.SelectionBorderThickness = new Thickness(selected ? 2.5 : 1);
        }
    }

    // A "rainbow" dot representing AccentColorSource.System (framework default accent).
    private static AccentSwatchItem CreateSystemSwatch()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        };
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(0, 120, 212), 0));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(177, 70, 194), 0.5));
        brush.GradientStops.Add(new GradientStop(Color.FromRgb(232, 17, 35), 1));

        return new AccentSwatchItem { Brush = brush, Key = "System", IsSystem = true };
    }

    private void UpdateThemeCardSelection(string tag)
    {
        UpdateCardBorder(ThemeLightPreview, tag == "Light");
        UpdateCardBorder(ThemeDarkPreview, tag == "Dark");
        UpdateCardBorder(ThemeSystemPreview, tag == "System");
    }

    // Selected preview shows a 2.5px accent-colored rounded border, matching WPF UpdateCardBorder.
    private void UpdateCardBorder(Border? preview, bool selected)
    {
        if (preview is null)
            return;

        preview.BorderThickness = new Thickness(selected ? 2.5 : 1);
        preview.BorderBrush = selected ? GetAccentBrush() : GetDefaultStrokeBrush();
    }

    private IBrush GetAccentBrush()
    {
        if (_selectedAccent is { } color)
            return new SolidColorBrush(color);

        if (this.TryFindResource("SystemAccentColor", out var value))
        {
            if (value is Color resourceColor)
                return new SolidColorBrush(resourceColor);
            if (value is IBrush brush)
                return brush;
        }

        return new SolidColorBrush(Color.FromRgb(0, 120, 212));
    }

    private IBrush GetDefaultStrokeBrush()
    {
        if (this.TryFindResource("CardStrokeColorDefaultBrush", out var value) && value is IBrush brush)
            return brush;

        return new SolidColorBrush(Color.FromArgb(0x20, 0x00, 0x00, 0x00));
    }

    // Best-effort accent application: override SystemAccentColor and its light/dark derivations.
    private static void ApplyAccentColor(Color color)
    {
        if (Application.Current is not { } app)
            return;

        var resources = app.Resources;
        resources["SystemAccentColor"] = color;
        resources["SystemAccentColorLight1"] = Lighten(color, 0.15);
        resources["SystemAccentColorLight2"] = Lighten(color, 0.30);
        resources["SystemAccentColorLight3"] = Lighten(color, 0.45);
        resources["SystemAccentColorDark1"] = Darken(color, 0.15);
        resources["SystemAccentColorDark2"] = Darken(color, 0.30);
        resources["SystemAccentColorDark3"] = Darken(color, 0.45);
    }

    private static void ClearAccentOverride()
    {
        if (Application.Current is not { } app)
            return;

        foreach (var key in AccentResourceKeys)
            app.Resources.Remove(key);
    }

    private static Color Lighten(Color c, double factor) => Color.FromArgb(
        c.A,
        (byte)(c.R + (255 - c.R) * factor),
        (byte)(c.G + (255 - c.G) * factor),
        (byte)(c.B + (255 - c.B) * factor));

    private static Color Darken(Color c, double factor) => Color.FromArgb(
        c.A,
        (byte)(c.R * (1 - factor)),
        (byte)(c.G * (1 - factor)),
        (byte)(c.B * (1 - factor)));
}

/// <summary>
/// View item for an accent color swatch. <see cref="IsSystem"/> represents the "system/rainbow"
/// entry that maps to AccentColorSource.System; solid entries mirror the Lib accent presets.
/// </summary>
public sealed class AccentSwatchItem
{
    public required IBrush Brush { get; init; }
    public required string Key { get; init; }
    public bool IsSystem { get; init; }
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }

    // Selection visuals, recomputed by SettingsAppearanceView whenever the swatch list is (re)built.
    public bool IsSelected { get; set; }
    public IBrush? SelectionBorderBrush { get; set; }
    public Thickness SelectionBorderThickness { get; set; } = new(1);
}
