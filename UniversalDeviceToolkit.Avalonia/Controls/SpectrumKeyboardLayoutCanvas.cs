using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace UniversalDeviceToolkit.Avalonia.Controls;

/// <summary>
/// Physical keyboard layouts rendered by <see cref="SpectrumKeyboardLayoutCanvas"/>.
/// The page keeps layout names as strings ("Ansi"/"Iso"/"Jis") so this stays
/// decoupled from the Windows-only <c>UniversalDeviceToolkit.Lib</c> enums.
/// </summary>
public enum SpectrumKeyboardLayoutKind
{
    Ansi,
    Iso,
    Jis,
}

/// <summary>
/// Key geometry in relative units inside the fixed 200 x 100 canvas space
/// (2:1 aspect). Coordinates describe the outer hit box; rendering insets each
/// key by a small gap so neighbouring keys read as separate caps.
/// </summary>
public readonly record struct SpectrumKeyGeometry(
    ushort KeyCode,
    double X,
    double Y,
    double Width,
    double Height);

/// <summary>
/// Geometry data for the ANSI/ISO/JIS Spectrum keyboards, transcribed from the
/// WPF <c>SpectrumKeyboardANSIControl/ISOControl/JisControl</c> XAML files.
/// Approximations: WPF uses a 660x244 pixel grid with 36px row pitch; this model
/// normalizes that to a 200x100 grid with a uniform 10-unit pitch and rounds
/// key widths to the nearest 0.125 key unit. Key codes and row membership match
/// the WPF layouts exactly.
/// </summary>
public static class SpectrumKeyboardLayoutData
{
    public const double CanvasWidth = 200;
    public const double CanvasHeight = 100;

    private const double KeyHeight = 9;
    private const double RowPitch = 10;
    private const double NavX = 156;
    private const double BottomRowY = 60;

    public static IReadOnlyList<SpectrumKeyGeometry> GetLayout(SpectrumKeyboardLayoutKind kind) => kind switch
    {
        SpectrumKeyboardLayoutKind.Iso => IsoLayout,
        SpectrumKeyboardLayoutKind.Jis => JisLayout,
        _ => AnsiLayout,
    };

    public static IReadOnlyList<SpectrumKeyGeometry> GetLayout(string? layoutName) =>
        TryParse(layoutName, out var kind) ? GetLayout(kind) : AnsiLayout;

    public static bool TryParse(string? layoutName, out SpectrumKeyboardLayoutKind kind)
    {
        switch (layoutName)
        {
            case null:
                break;
            case { } name when name.Equals("Ansi", System.StringComparison.OrdinalIgnoreCase):
                kind = SpectrumKeyboardLayoutKind.Ansi;
                return true;
            case { } name when name.Equals("Iso", System.StringComparison.OrdinalIgnoreCase):
                kind = SpectrumKeyboardLayoutKind.Iso;
                return true;
            case { } name when name.Equals("Jis", System.StringComparison.OrdinalIgnoreCase):
                kind = SpectrumKeyboardLayoutKind.Jis;
                return true;
        }

        kind = SpectrumKeyboardLayoutKind.Ansi;
        return false;
    }

    private static IReadOnlyList<SpectrumKeyGeometry> BuildAnsi()
    {
        var keys = new List<SpectrumKeyGeometry>();
        keys.AddRange(Flow(0, FunctionRow));
        keys.AddRange(Flow(RowPitch, MainRow1));
        keys.AddRange(Flow(2 * RowPitch, [
            (0x40, 13.75), (0x42, 10), (0x43, 10), (0x44, 10), (0x45, 10), (0x46, 10),
            (0x47, 10), (0x48, 10), (0x49, 10), (0x4A, 10), (0x4B, 10), (0x4C, 10),
            (0x4D, 10), (0x4E, 10),
        ]));
        keys.AddRange(Flow(3 * RowPitch, MainRow3));
        keys.AddRange(Flow(4 * RowPitch, MainRow4));
        keys.AddRange(Flow(5 * RowPitch, MainRow5Ansi));
        keys.AddRange(Flow(BottomRowY, BottomRow, x: 124));
        keys.AddRange(BuildNavigationCluster());
        return keys;
    }

    private static IReadOnlyList<SpectrumKeyGeometry> BuildIso()
    {
        var keys = new List<SpectrumKeyGeometry>();
        keys.AddRange(Flow(0, FunctionRow));
        keys.AddRange(Flow(RowPitch, MainRow1));
        keys.AddRange(Flow(2 * RowPitch, [
            (0x40, 13.75), (0x42, 10), (0x43, 10), (0x44, 10), (0x45, 10), (0x46, 10),
            (0x47, 10), (0x48, 10), (0x49, 10), (0x4A, 10), (0x4B, 10), (0x4C, 10),
            (0x4D, 11.25),
        ]));
        keys.AddRange(Flow(3 * RowPitch, MainRow3Iso));
        keys.Add(new SpectrumKeyGeometry(0x77, 135, 2 * RowPitch, 8.75, 19));
        keys.AddRange(Flow(4 * RowPitch, MainRow4Iso));
        keys.AddRange(Flow(5 * RowPitch, MainRow5Ansi));
        keys.AddRange(Flow(BottomRowY, BottomRow, x: 124));
        keys.AddRange(BuildNavigationCluster());
        return keys;
    }

    private static IReadOnlyList<SpectrumKeyGeometry> BuildJis()
    {
        var keys = new List<SpectrumKeyGeometry>();
        keys.AddRange(Flow(0, FunctionRow));
        keys.AddRange(Flow(RowPitch, [
            (0x16, 7.5), (0x17, 10), (0x18, 10), (0x19, 10), (0x1A, 10), (0x1B, 10),
            (0x1C, 10), (0x1D, 10), (0x1E, 10), (0x1F, 10), (0x20, 10), (0x21, 10),
            (0x22, 10), (0xA8, 7.5), (0x38, 7.5),
        ]));
        keys.AddRange(Flow(2 * RowPitch, [
            (0x40, 13.75), (0x42, 10), (0x43, 10), (0x44, 10), (0x45, 10), (0x46, 10),
            (0x47, 10), (0x48, 10), (0x49, 10), (0x4A, 10), (0x4B, 10), (0x60, 10),
            (0x4C, 11.25),
        ]));
        keys.AddRange(Flow(3 * RowPitch, MainRow3Jis));
        keys.Add(new SpectrumKeyGeometry(0x77, 135, 2 * RowPitch, 8.75, 19));
        keys.AddRange(Flow(4 * RowPitch, MainRow4Jis));
        keys.AddRange(Flow(5 * RowPitch, MainRow5Jis));
        keys.AddRange(Flow(BottomRowY, BottomRow, x: 124));
        keys.AddRange(BuildNavigationCluster());
        return keys;
    }

    private static readonly IReadOnlyList<(ushort Code, double Width)> FunctionRow =
    [
        (0x01, 8.75), (0x02, 8.75), (0x03, 8.75), (0x04, 8.75), (0x05, 8.75),
        (0x06, 8.75), (0x07, 8.75), (0x08, 8.75), (0x09, 8.75), (0x0A, 8.75),
        (0x0B, 8.75), (0x0C, 8.75), (0x0D, 8.75), (0x0E, 8.75), (0x0F, 8.75),
        (0x10, 10),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow1 =
    [
        (0x16, 7.5), (0x17, 10), (0x18, 10), (0x19, 10), (0x1A, 10), (0x1B, 10),
        (0x1C, 10), (0x1D, 10), (0x1E, 10), (0x1F, 10), (0x20, 10), (0x21, 10),
        (0x22, 10), (0x38, 16.25),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow3 =
    [
        (0x55, 17.5), (0x6D, 10), (0x6E, 10), (0x58, 10), (0x59, 10), (0x5A, 10),
        (0x71, 10), (0x72, 10), (0x5B, 10), (0x5C, 10), (0x5D, 10), (0x5F, 10),
        (0x77, 17.5),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow3Iso =
    [
        (0x55, 17.5), (0x6D, 10), (0x6E, 10), (0x58, 10), (0x59, 10), (0x5A, 10),
        (0x71, 10), (0x72, 10), (0x5B, 10), (0x5C, 10), (0x5D, 10), (0x5F, 10),
        (0xA8, 7.5),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow3Jis =
    [
        (0x55, 17.5), (0x6D, 10), (0x6E, 10), (0x58, 10), (0x59, 10), (0x5A, 10),
        (0x71, 10), (0x72, 10), (0x5B, 10), (0x5C, 10), (0x5D, 10), (0x5F, 10),
        (0x4D, 7.5),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow4 =
    [
        (0x6A, 23.125), (0x82, 10), (0x83, 10), (0x6F, 10), (0x70, 10), (0x87, 10),
        (0x88, 10), (0x73, 10), (0x74, 10), (0x75, 10), (0x76, 10), (0x8D, 23.125),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow4Iso =
    [
        (0x6A, 23.125), (0x4E, 10), (0x82, 10), (0x83, 10), (0x6F, 10), (0x70, 10), (0x87, 10),
        (0x88, 10), (0x73, 10), (0x74, 10), (0x75, 10), (0x76, 10), (0x8D, 10),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow4Jis = MainRow4Iso;

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow5Ansi =
    [
        (0x7F, 11.875), (0x80, 10), (0x96, 10), (0x97, 10), (0x98, 55),
        (0x9A, 10), (0x9B, 10), (0x9D, 10),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> MainRow5Jis =
    [
        (0x7F, 11.875), (0x80, 10), (0x96, 10), (0x97, 10), (0xA9, 10), (0x98, 32.5),
        (0xAA, 10), (0xAB, 10), (0x9B, 10), (0x9D, 10),
    ];

    private static readonly IReadOnlyList<(ushort Code, double Width)> BottomRow =
    [(0x9C, 10), (0x9F, 10), (0xA1, 10)];

    private static readonly IReadOnlyList<SpectrumKeyGeometry> AnsiLayout = BuildAnsi();
    private static readonly IReadOnlyList<SpectrumKeyGeometry> IsoLayout = BuildIso();
    private static readonly IReadOnlyList<SpectrumKeyGeometry> JisLayout = BuildJis();

    public static IReadOnlyList<SpectrumKeyGeometry> Ansi => AnsiLayout;
    public static IReadOnlyList<SpectrumKeyGeometry> Iso => IsoLayout;
    public static IReadOnlyList<SpectrumKeyGeometry> Jis => JisLayout;

    private static IReadOnlyList<SpectrumKeyGeometry> BuildNavigationCluster()
    {
        var keys = new List<SpectrumKeyGeometry>();
        keys.AddRange(Flow(0, [(0x11, 10), (0x12, 10), (0x13, 10), (0x14, 10)], x: NavX));
        keys.AddRange(Flow(RowPitch, [(0x26, 10), (0x27, 10), (0x28, 10), (0x29, 10)], x: NavX));
        keys.AddRange(Flow(2 * RowPitch, [(0x4F, 10), (0x50, 10), (0x51, 10)], x: NavX));
        keys.Add(new SpectrumKeyGeometry(0x68, NavX + 30, 2 * RowPitch, 10, 19));
        keys.AddRange(Flow(3 * RowPitch, [(0x79, 10), (0x7B, 10), (0x7C, 10)], x: NavX));
        keys.AddRange(Flow(4 * RowPitch, [(0x8E, 10), (0x90, 10), (0x92, 10)], x: NavX));
        keys.Add(new SpectrumKeyGeometry(0xA7, NavX + 30, 4 * RowPitch, 10, 19));
        keys.Add(new SpectrumKeyGeometry(0xA3, NavX, 5 * RowPitch, 20, KeyHeight));
        keys.Add(new SpectrumKeyGeometry(0xA5, NavX + 20, 5 * RowPitch, 10, KeyHeight));
        return keys;
    }

    private static IReadOnlyList<SpectrumKeyGeometry> Flow(
        double y,
        IReadOnlyList<(ushort Code, double Width)> row,
        double x = 0,
        double height = KeyHeight)
    {
        var keys = new List<SpectrumKeyGeometry>(row.Count);
        foreach (var (code, width) in row)
        {
            keys.Add(new SpectrumKeyGeometry(code, x, y, width, height));
            x += width;
        }

        return keys;
    }
}

/// <summary>
/// Data-driven Spectrum keyboard renderer. Draws a keyboard outline plus one
/// rounded cap per key, colors keys from a key-code map, and supports click
/// selection for effect key editing. Pure helpers (<see cref="HitTestKey"/>,
/// <see cref="GetVisibleKeys"/>, <see cref="CombineKeyColors"/>) keep the
/// mapping and geometry logic unit-testable without a UI thread.
/// </summary>
public sealed class SpectrumKeyboardLayoutCanvas : Control
{
    public static readonly StyledProperty<SpectrumKeyboardLayoutKind> LayoutProperty =
        AvaloniaProperty.Register<SpectrumKeyboardLayoutCanvas, SpectrumKeyboardLayoutKind>(
            nameof(Layout),
            SpectrumKeyboardLayoutKind.Ansi);

    public static readonly StyledProperty<IReadOnlyCollection<ushort>?> AvailableKeysProperty =
        AvaloniaProperty.Register<SpectrumKeyboardLayoutCanvas, IReadOnlyCollection<ushort>?>(
            nameof(AvailableKeys));

    public static readonly StyledProperty<IReadOnlyDictionary<ushort, Color>?> KeyColorsProperty =
        AvaloniaProperty.Register<SpectrumKeyboardLayoutCanvas, IReadOnlyDictionary<ushort, Color>?>(
            nameof(KeyColors));

    public static readonly StyledProperty<bool> SelectionEnabledProperty =
        AvaloniaProperty.Register<SpectrumKeyboardLayoutCanvas, bool>(
            nameof(SelectionEnabled),
            true);

    private readonly HashSet<ushort> _selectedKeys = [];

    public event EventHandler? SelectionChanged;

    public SpectrumKeyboardLayoutCanvas()
    {
        AutomationProperties.SetName(this, "Keyboard keys");
    }

    public SpectrumKeyboardLayoutKind Layout
    {
        get => GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    /// <summary>
    /// Key codes reported by the keyboard controller. Keys without a matching
    /// geometry entry remain visible only in the hex fallback row. When null or
    /// empty every geometry key is shown.
    /// </summary>
    public IReadOnlyCollection<ushort>? AvailableKeys
    {
        get => GetValue(AvailableKeysProperty);
        set => SetValue(AvailableKeysProperty, value);
    }

    /// <summary>Key-code to color mapping used to paint the keyboard surface.</summary>
    public IReadOnlyDictionary<ushort, Color>? KeyColors
    {
        get => GetValue(KeyColorsProperty);
        set => SetValue(KeyColorsProperty, value);
    }

    public bool SelectionEnabled
    {
        get => GetValue(SelectionEnabledProperty);
        set => SetValue(SelectionEnabledProperty, value);
    }

    public IReadOnlyCollection<ushort> Selection => _selectedKeys;

    public IReadOnlyList<SpectrumKeyGeometry> Geometry => SpectrumKeyboardLayoutData.GetLayout(Layout);

    public void SetKeyColors(IReadOnlyDictionary<ushort, Color> colors) => KeyColors = colors;

    public void SetSelection(IEnumerable<ushort> keys)
    {
        var next = keys.ToHashSet();
        if (next.SetEquals(_selectedKeys))
            return;

        _selectedKeys.Clear();
        _selectedKeys.UnionWith(next);
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSelection()
    {
        if (_selectedKeys.Count == 0)
            return;

        _selectedKeys.Clear();
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetKey(ushort key, bool selected)
    {
        if (!IsKeyAvailable(key))
            return;

        var changed = selected ? _selectedKeys.Add(key) : _selectedKeys.Remove(key);
        if (!changed)
            return;

        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleKey(ushort key)
    {
        if (!IsKeyAvailable(key))
            return;

        if (!_selectedKeys.Remove(key))
            _selectedKeys.Add(key);

        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool IsKeyAvailable(ushort key) =>
        AvailableKeys is null || AvailableKeys.Count == 0 || AvailableKeys.Contains(key);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LayoutProperty
            || change.Property == AvailableKeysProperty
            || change.Property == KeyColorsProperty)
        {
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!SelectionEnabled)
            return;

        var key = HitTestKey(
            GetVisibleKeys(Geometry, AvailableKeys),
            e.GetCurrentPoint(this).Position,
            Bounds.Size);
        if (key is { } keyCode)
        {
            ToggleKey(keyCode);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var key = HitTestKey(
            GetVisibleKeys(Geometry, AvailableKeys),
            e.GetCurrentPoint(this).Position,
            Bounds.Size);
        ToolTip.SetTip(this, key is { } keyCode ? $"0x{keyCode:X4}" : null);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 1 || height <= 1)
            return;

        var (scale, offsetX, offsetY) = ComputeTransform(Bounds.Size);
        var outline = new Rect(
            offsetX,
            offsetY,
            SpectrumKeyboardLayoutData.CanvasWidth * scale,
            SpectrumKeyboardLayoutData.CanvasHeight * scale);
        var outlinePen = new Pen(ResolveBrush("CardBorderBrush", Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)), 1.5);
        context.DrawRectangle(
            ResolveBrush("CardBackgroundBrush", Color.FromRgb(0x2B, 0x2B, 0x2B)),
            outlinePen,
            new RoundedRect(outline, 12 * scale));

        var accent = ResolveBrush("AccentBackgroundBrush", Color.FromRgb(0x00, 0x78, 0xD4));
        var accentColor = accent is SolidColorBrush solid ? solid.Color : Color.FromRgb(0x00, 0x78, 0xD4);
        var selectionFill = new SolidColorBrush(Color.FromArgb(0x59, accentColor.R, accentColor.G, accentColor.B));
        var selectionPen = new Pen(accent, Math.Max(1, 1.5 * scale));
        var dimBrush = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
        var keyFill = ResolveBrush("ButtonBackgroundBrush", Color.FromRgb(0x30, 0x30, 0x30));
        var gap = 0.5 * scale;
        var corner = 3 * scale;

        foreach (var key in GetVisibleKeys(Geometry, AvailableKeys))
        {
            var rect = new Rect(
                offsetX + key.X * scale + gap,
                offsetY + key.Y * scale + gap,
                key.Width * scale - gap * 2,
                key.Height * scale - gap * 2);
            if (rect.Width <= 0 || rect.Height <= 0)
                continue;

            var rounded = new RoundedRect(rect, corner);
            var selected = _selectedKeys.Contains(key.KeyCode);
            var hasColor = false;
            var keyColor = default(Color);
            if (KeyColors is { } keyColors)
                hasColor = keyColors.TryGetValue(key.KeyCode, out keyColor);
            if (hasColor)
            {
                context.DrawRectangle(new SolidColorBrush(keyColor), null, rounded);
                if (!selected)
                    context.DrawRectangle(dimBrush, null, rounded);
            }
            else if (selected)
            {
                context.DrawRectangle(selectionFill, null, rounded);
            }
            else
            {
                context.DrawRectangle(keyFill, null, rounded);
            }

            if (selected)
                context.DrawRectangle(null, selectionPen, rounded);
        }
    }

    private IBrush ResolveBrush(string key, Color fallbackColor)
    {
        if (this.TryFindResource(key, out var value) && value is IBrush brush)
            return brush;

        return new SolidColorBrush(fallbackColor);
    }

    /// <summary>
    /// Maps a pointer position inside <paramref name="size"/> onto the geometry
    /// grid and returns the key code under the point, or null for empty space.
    /// </summary>
    public static ushort? HitTestKey(
        IReadOnlyList<SpectrumKeyGeometry> geometry,
        Point point,
        Size size)
    {
        if (geometry is null || geometry.Count == 0 || size.Width <= 0 || size.Height <= 0)
            return null;

        var (scale, offsetX, offsetY) = ComputeTransform(size);
        foreach (var key in geometry)
        {
            var rect = new Rect(
                offsetX + key.X * scale,
                offsetY + key.Y * scale,
                key.Width * scale,
                key.Height * scale);
            if (rect.Contains(point))
                return key.KeyCode;
        }

        return null;
    }

    /// <summary>
    /// Filters geometry down to the key codes reported by the controller.
    /// </summary>
    public static IReadOnlyList<SpectrumKeyGeometry> GetVisibleKeys(
        IReadOnlyList<SpectrumKeyGeometry> geometry,
        IReadOnlyCollection<ushort>? availableKeys)
    {
        if (availableKeys is null || availableKeys.Count == 0)
            return geometry;

        return geometry.Where(key => availableKeys.Contains(key.KeyCode)).ToArray();
    }

    /// <summary>
    /// Merges per-effect key colors into one key-code map. Effects are applied in
    /// list order, so a later effect wins for a key shared by several effects
    /// (matching the device profile priority in the WPF host).
    /// </summary>
    public static IReadOnlyDictionary<ushort, Color> CombineKeyColors(
        IReadOnlyList<(IReadOnlyCollection<ushort> Keys, IReadOnlyList<Color> Colors)> contributions)
    {
        var map = new Dictionary<ushort, Color>();
        foreach (var (keys, colors) in contributions)
        {
            if (colors is null || colors.Count == 0)
                continue;

            var index = 0;
            foreach (var key in keys)
            {
                map[key] = colors[index % colors.Count];
                index++;
            }
        }

        return map;
    }

    internal static (double Scale, double OffsetX, double OffsetY) ComputeTransform(Size size)
    {
        var scale = Math.Min(
            size.Width / SpectrumKeyboardLayoutData.CanvasWidth,
            size.Height / SpectrumKeyboardLayoutData.CanvasHeight);
        var offsetX = (size.Width - SpectrumKeyboardLayoutData.CanvasWidth * scale) / 2;
        var offsetY = (size.Height - SpectrumKeyboardLayoutData.CanvasHeight * scale) / 2;
        return (scale, offsetX, offsetY);
    }
}
