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
/// Device zone layouts rendered alongside the keyboard by
/// <see cref="SpectrumKeyboardLayoutCanvas"/>. Mirrors the four
/// <c>SpectrumDevice*Control</c> variants in the WPF host; the page keeps the
/// layout name as a string so this stays decoupled from the Windows-only
/// <c>UniversalDeviceToolkit.Lib</c> <c>SpectrumLayout</c> enum.
/// </summary>
public enum SpectrumDeviceLayoutKind
{
    /// <summary>Keyboard + logo + rear vents + side panels + front panel.</summary>
    Full,

    /// <summary>Keyboard + logo + 6 rear vents + 3 side zones per side + front panel.</summary>
    FullAlternative,

    /// <summary>Keyboard + front panel only.</summary>
    KeyboardAndFront,

    /// <summary>Keyboard only, no device zones (matches the plain canvas).</summary>
    KeyboardOnly,
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
/// Non-keyboard device zone geometry (logo, vents, side/front panels) in the
/// same relative units as <see cref="SpectrumKeyGeometry"/>, but expressed in
/// the larger device canvas space (220 x 112). Key codes match the WPF
/// <c>SpectrumDevice*Control</c> XAML files exactly.
/// </summary>
public readonly record struct SpectrumDeviceZoneGeometry(
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

    /// <summary>
    /// Device layouts render the keyboard shifted down by
    /// <see cref="DeviceKeyboardOffsetY"/> (and right by
    /// <see cref="DeviceKeyboardOffsetX"/>) inside a wider 220 x 112 canvas so
    /// the logo, vents, side panels and front panel fit around it. The offset
    /// and size are chosen to keep the keyboard geometry identical in all
    /// layouts; only the enclosing canvas and the keyboard placement change.
    /// </summary>
    public const double DeviceCanvasWidth = 220;
    public const double DeviceCanvasHeight = 112;
    public const double DeviceKeyboardOffsetX = 12;
    public const double DeviceKeyboardOffsetY = 26;

    /// <summary>
    /// Zone rectangles for a device layout, transcribed from the WPF
    /// <c>SpectrumDevice*Control</c> XAML grids: keyboard occupies grid
    /// columns 1-6 and rows 2-6, with the logo above, vents between logo and
    /// keyboard, side panels at columns 0/7 and the front panel below.
    /// </summary>
    public static IReadOnlyList<SpectrumDeviceZoneGeometry> GetDeviceZones(SpectrumDeviceLayoutKind kind) => kind switch
    {
        SpectrumDeviceLayoutKind.FullAlternative => FullAlternativeZones,
        SpectrumDeviceLayoutKind.KeyboardAndFront => KeyboardAndFrontZones,
        SpectrumDeviceLayoutKind.KeyboardOnly => KeyboardOnlyZones,
        _ => FullZones,
    };

    public static IReadOnlyList<SpectrumDeviceZoneGeometry> GetDeviceZones(string? layoutName) =>
        TryParseDeviceLayout(layoutName, out var kind) ? GetDeviceZones(kind) : FullZones;

    public static bool TryParseDeviceLayout(string? layoutName, out SpectrumDeviceLayoutKind kind)
    {
        switch (layoutName)
        {
            case null:
                break;
            case { } name when name.Equals("Full", System.StringComparison.OrdinalIgnoreCase):
                kind = SpectrumDeviceLayoutKind.Full;
                return true;
            case { } name when name.Equals("FullAlternative", System.StringComparison.OrdinalIgnoreCase):
                kind = SpectrumDeviceLayoutKind.FullAlternative;
                return true;
            case { } name when name.Equals("KeyboardAndFront", System.StringComparison.OrdinalIgnoreCase):
                kind = SpectrumDeviceLayoutKind.KeyboardAndFront;
                return true;
            case { } name when name.Equals("KeyboardOnly", System.StringComparison.OrdinalIgnoreCase):
                kind = SpectrumDeviceLayoutKind.KeyboardOnly;
                return true;
        }

        kind = SpectrumDeviceLayoutKind.Full;
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

    private const double VentY = 13;
    private const double VentHeight = 10;
    private const double SideX = 0;
    private const double SideRightX = DeviceCanvasWidth - 10;
    private const double SideWidth = 10;
    private const double FrontY = 97;
    private const double FrontHeight = 12;

    // Rear vents: WPF grid columns 1-6 map to six 34-unit columns starting at 14.
    private static readonly IReadOnlyList<(ushort Code, double X)> VentColumns =
    [
        (0x03EA, 14), (0x03EB, 48), (0x03EC, 82), (0x03ED, 116), (0x03EE, 150), (0x03EF, 184),
    ];

    // Front panel: same column mapping, key codes follow the WPF XAML order.
    private static readonly IReadOnlyList<(ushort Code, double X)> FrontColumns =
    [
        (0x01F5, 14), (0x01F6, 48), (0x01F7, 82), (0x01F8, 116), (0x01F9, 150), (0x01FA, 184),
    ];

    // Side panels: keyboard rows 2/3 (upper half) and 5/6 (lower half).
    private const double SideTopY = 44;
    private const double SideUpperY = 58;
    private const double SideLowerY = 74;
    private const double SideBottomY = 88;
    private const double SideHeight = 13;
    private const double SideTallHeight = 19;

    private static SpectrumDeviceZoneGeometry Zone(ushort code, double x, double y, double width, double height) =>
        new(code, x, y, width, height);

    private static IReadOnlyList<SpectrumDeviceZoneGeometry> BuildFullZones() =>
    [
        // Panel logo (WPF: row 0, column 6, centered above the vents).
        Zone(0x05DD, 92, 2, 36, 9),
        // Rear vents (WPF: columns 1, 2, 5, 6 of the vent row).
        Zone(0x03EB, VentColumns[1].X, VentY, 32, VentHeight),
        Zone(0x03EC, VentColumns[2].X, VentY, 32, VentHeight),
        Zone(0x03ED, VentColumns[4].X, VentY, 32, VentHeight),
        Zone(0x03EE, VentColumns[5].X, VentY, 32, VentHeight),
        // Left side (WPF: rows 2, 3, 5, 6 of column 0).
        Zone(0x03EA, SideX, SideTopY, SideWidth, SideHeight),
        Zone(0x03E9, SideX, SideUpperY, SideWidth, SideHeight),
        Zone(0x01F5, SideX, SideLowerY, SideWidth, SideHeight),
        Zone(0x01F6, SideX, SideBottomY, SideWidth, SideHeight),
        // Right side (WPF: rows 2, 3, 5, 6 of column 7).
        Zone(0x03EF, SideRightX, SideTopY, SideWidth, SideHeight),
        Zone(0x03F0, SideRightX, SideUpperY, SideWidth, SideHeight),
        Zone(0x01FE, SideRightX, SideLowerY, SideWidth, SideHeight),
        Zone(0x01FD, SideRightX, SideBottomY, SideWidth, SideHeight),
        // Front panel (WPF: columns 1-6 of the bottom row).
        Zone(0x01F7, FrontColumns[2].X, FrontY, 32, FrontHeight),
        Zone(0x01F8, FrontColumns[3].X, FrontY, 32, FrontHeight),
        Zone(0x01F9, FrontColumns[4].X, FrontY, 32, FrontHeight),
        Zone(0x01FA, FrontColumns[5].X, FrontY, 32, FrontHeight),
        Zone(0x01FB, 150, FrontY, 32, FrontHeight),
        Zone(0x01FC, 184, FrontY, 32, FrontHeight),
    ];

    private static IReadOnlyList<SpectrumDeviceZoneGeometry> BuildFullAlternativeZones() =>
    [
        // Panel logo (same placement as Full).
        Zone(0x05DD, 92, 2, 36, 9),
        // Rear vents (WPF alternative: all six columns 1-6, key codes shift).
        Zone(0x03EA, VentColumns[0].X, VentY, 32, VentHeight),
        Zone(0x03EB, VentColumns[1].X, VentY, 32, VentHeight),
        Zone(0x03EC, VentColumns[2].X, VentY, 32, VentHeight),
        Zone(0x03ED, VentColumns[3].X, VentY, 32, VentHeight),
        Zone(0x03EE, VentColumns[4].X, VentY, 32, VentHeight),
        Zone(0x03EF, VentColumns[5].X, VentY, 32, VentHeight),
        // Left side (WPF alternative: row 2 only at the top, then rows 5 and 6).
        Zone(0x03E9, SideX, SideTopY, SideWidth, SideTallHeight),
        Zone(0x01F5, SideX, SideLowerY, SideWidth, SideHeight),
        Zone(0x01F6, SideX, SideBottomY, SideWidth, SideHeight),
        // Right side (WPF alternative: row 2 only at the top, then rows 5 and 6).
        Zone(0x03F0, SideRightX, SideTopY, SideWidth, SideTallHeight),
        Zone(0x01FE, SideRightX, SideLowerY, SideWidth, SideHeight),
        Zone(0x01FD, SideRightX, SideBottomY, SideWidth, SideHeight),
        // Front panel (same six zones as Full).
        Zone(0x01F7, FrontColumns[2].X, FrontY, 32, FrontHeight),
        Zone(0x01F8, FrontColumns[3].X, FrontY, 32, FrontHeight),
        Zone(0x01F9, FrontColumns[4].X, FrontY, 32, FrontHeight),
        Zone(0x01FA, FrontColumns[5].X, FrontY, 32, FrontHeight),
        Zone(0x01FB, 150, FrontY, 32, FrontHeight),
        Zone(0x01FC, 184, FrontY, 32, FrontHeight),
    ];

    private static IReadOnlyList<SpectrumDeviceZoneGeometry> BuildKeyboardAndFrontZones()
    {
        var zones = new List<SpectrumDeviceZoneGeometry>(6);
        // Front panel (WPF KeyboardAndFront: columns 0-5 use codes 0x01F5-0x01FA).
        foreach (var (code, x) in FrontColumns)
            zones.Add(Zone(code, x, FrontY, 32, FrontHeight));
        return zones;
    }

    private static readonly IReadOnlyList<SpectrumDeviceZoneGeometry> FullZones = BuildFullZones();
    private static readonly IReadOnlyList<SpectrumDeviceZoneGeometry> FullAlternativeZones = BuildFullAlternativeZones();
    private static readonly IReadOnlyList<SpectrumDeviceZoneGeometry> KeyboardAndFrontZones = BuildKeyboardAndFrontZones();
    private static readonly IReadOnlyList<SpectrumDeviceZoneGeometry> KeyboardOnlyZones = [];

    public static IReadOnlyList<SpectrumDeviceZoneGeometry> Full => FullZones;
    public static IReadOnlyList<SpectrumDeviceZoneGeometry> FullAlternative => FullAlternativeZones;
    public static IReadOnlyList<SpectrumDeviceZoneGeometry> KeyboardAndFront => KeyboardAndFrontZones;
    public static IReadOnlyList<SpectrumDeviceZoneGeometry> KeyboardOnly => KeyboardOnlyZones;
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

    public static readonly StyledProperty<SpectrumDeviceLayoutKind> DeviceLayoutProperty =
        AvaloniaProperty.Register<SpectrumKeyboardLayoutCanvas, SpectrumDeviceLayoutKind>(
            nameof(DeviceLayout),
            SpectrumDeviceLayoutKind.KeyboardOnly);

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
    /// Device zone layout rendered around the keyboard. KeyboardOnly matches
    /// the original bare-keyboard canvas; the other values add logo, vent,
    /// side-panel and front-panel zones transcribed from the WPF
    /// <c>SpectrumDevice*Control</c> variants.
    /// </summary>
    public SpectrumDeviceLayoutKind DeviceLayout
    {
        get => GetValue(DeviceLayoutProperty);
        set => SetValue(DeviceLayoutProperty, value);
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

    /// <summary>
    /// Device zones for the current <see cref="DeviceLayout"/>. Zone key codes
    /// participate in selection, coloring and the <see cref="AvailableKeys"/>
    /// filter exactly like keyboard keys.
    /// </summary>
    public IReadOnlyList<SpectrumDeviceZoneGeometry> Zones =>
        SpectrumKeyboardLayoutData.GetDeviceZones(DeviceLayout);

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
            || change.Property == DeviceLayoutProperty
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

        var key = HitTest(
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
        var key = HitTest(
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

        var (scale, offsetX, offsetY) = ComputeDeviceTransform(Bounds.Size);
        var isDeviceLayout = DeviceLayout != SpectrumDeviceLayoutKind.KeyboardOnly;
        var canvasWidth = isDeviceLayout
            ? SpectrumKeyboardLayoutData.DeviceCanvasWidth
            : SpectrumKeyboardLayoutData.CanvasWidth;
        var canvasHeight = isDeviceLayout
            ? SpectrumKeyboardLayoutData.DeviceCanvasHeight
            : SpectrumKeyboardLayoutData.CanvasHeight;
        var outline = new Rect(
            offsetX,
            offsetY,
            canvasWidth * scale,
            canvasHeight * scale);
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

        // Device zones sit directly in the device canvas space.
        foreach (var zone in GetVisibleZones())
        {
            var rect = new Rect(
                offsetX + zone.X * scale + gap,
                offsetY + zone.Y * scale + gap,
                zone.Width * scale - gap * 2,
                zone.Height * scale - gap * 2);
            DrawCap(context, rect, zone.KeyCode, selectionFill, selectionPen, dimBrush, keyFill, corner);
        }

        // Keyboard keys shift down/right inside the device canvas so the zones
        // above, below and beside the keyboard line up with the WPF grid.
        var keyOffsetX = isDeviceLayout ? SpectrumKeyboardLayoutData.DeviceKeyboardOffsetX * scale : 0;
        var keyOffsetY = isDeviceLayout ? SpectrumKeyboardLayoutData.DeviceKeyboardOffsetY * scale : 0;
        foreach (var key in GetVisibleKeys(Geometry, AvailableKeys))
        {
            var rect = new Rect(
                offsetX + keyOffsetX + key.X * scale + gap,
                offsetY + keyOffsetY + key.Y * scale + gap,
                key.Width * scale - gap * 2,
                key.Height * scale - gap * 2);
            DrawCap(context, rect, key.KeyCode, selectionFill, selectionPen, dimBrush, keyFill, corner);
        }
    }

    private void DrawCap(
        DrawingContext context,
        Rect rect,
        ushort keyCode,
        IBrush selectionFill,
        Pen selectionPen,
        IBrush dimBrush,
        IBrush keyFill,
        double corner)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        var rounded = new RoundedRect(rect, corner);
        var selected = _selectedKeys.Contains(keyCode);
        var hasColor = false;
        var keyColor = default(Color);
        if (KeyColors is { } keyColors)
            hasColor = keyColors.TryGetValue(keyCode, out keyColor);
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
    /// Filters device zones down to the key codes reported by the controller.
    /// </summary>
    public static IReadOnlyList<SpectrumDeviceZoneGeometry> GetVisibleZones(
        IReadOnlyList<SpectrumDeviceZoneGeometry> zones,
        IReadOnlyCollection<ushort>? availableKeys)
    {
        if (availableKeys is null || availableKeys.Count == 0)
            return zones;

        return zones.Where(zone => availableKeys.Contains(zone.KeyCode)).ToArray();
    }

    /// <summary>
    /// Zones of the current <see cref="DeviceLayout"/> filtered by
    /// <see cref="AvailableKeys"/>, mirroring <see cref="GetVisibleKeys"/>.
    /// </summary>
    public IReadOnlyList<SpectrumDeviceZoneGeometry> GetVisibleZones() =>
        GetVisibleZones(Zones, AvailableKeys);

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

    internal static (double Scale, double OffsetX, double OffsetY) ComputeTransform(Size size) =>
        ComputeTransform(size, SpectrumKeyboardLayoutData.CanvasWidth, SpectrumKeyboardLayoutData.CanvasHeight);

    internal static (double Scale, double OffsetX, double OffsetY) ComputeTransform(
        Size size,
        double canvasWidth,
        double canvasHeight)
    {
        var scale = Math.Min(
            size.Width / canvasWidth,
            size.Height / canvasHeight);
        var offsetX = (size.Width - canvasWidth * scale) / 2;
        var offsetY = (size.Height - canvasHeight * scale) / 2;
        return (scale, offsetX, offsetY);
    }

    /// <summary>
    /// Transform that maps the model canvas (200 x 100 for KeyboardOnly,
    /// 220 x 112 for device layouts) onto <paramref name="size"/>. Keyboard
    /// geometry additionally shifts by the device keyboard offset so the keys
    /// line up with the zones; see <see cref="SpectrumKeyboardLayoutData"/>.
    /// </summary>
    private (double Scale, double OffsetX, double OffsetY) ComputeDeviceTransform(Size size)
    {
        var isDeviceLayout = DeviceLayout != SpectrumDeviceLayoutKind.KeyboardOnly;
        return ComputeTransform(
            size,
            isDeviceLayout ? SpectrumKeyboardLayoutData.DeviceCanvasWidth : SpectrumKeyboardLayoutData.CanvasWidth,
            isDeviceLayout ? SpectrumKeyboardLayoutData.DeviceCanvasHeight : SpectrumKeyboardLayoutData.CanvasHeight);
    }

    /// <summary>
    /// Hit-tests zones and keyboard keys (with the device-layout keyboard
    /// offset applied) against a pointer position. Zones are tested first so
    /// a click on a zone overlapping the keyboard outline selects the zone.
    /// </summary>
    private ushort? HitTest(Point point, Size size)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return null;

        var visibleZones = GetVisibleZones();
        var visibleKeys = GetVisibleKeys(Geometry, AvailableKeys);
        if (visibleZones.Count == 0 && visibleKeys.Count == 0)
            return null;

        var (scale, offsetX, offsetY) = ComputeDeviceTransform(size);
        var isDeviceLayout = DeviceLayout != SpectrumDeviceLayoutKind.KeyboardOnly;
        var keyOffsetX = isDeviceLayout ? SpectrumKeyboardLayoutData.DeviceKeyboardOffsetX * scale : 0;
        var keyOffsetY = isDeviceLayout ? SpectrumKeyboardLayoutData.DeviceKeyboardOffsetY * scale : 0;

        foreach (var zone in visibleZones)
        {
            var rect = new Rect(
                offsetX + zone.X * scale,
                offsetY + zone.Y * scale,
                zone.Width * scale,
                zone.Height * scale);
            if (rect.Contains(point))
                return zone.KeyCode;
        }

        foreach (var key in visibleKeys)
        {
            var rect = new Rect(
                offsetX + keyOffsetX + key.X * scale,
                offsetY + keyOffsetY + key.Y * scale,
                key.Width * scale,
                key.Height * scale);
            if (rect.Contains(point))
                return key.KeyCode;
        }

        return null;
    }
}
