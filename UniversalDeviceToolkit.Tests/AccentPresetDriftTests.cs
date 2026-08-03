using System.Collections;
using System.Reflection;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

/// <summary>
/// Drift guard for the accent color presets that are intentionally duplicated across the two UI
/// layers: WPF consumes <see cref="AccentColorPresets.Swatches"/> from the Windows-only Lib, while
/// the cross-platform Avalonia project hard-codes an equivalent array in
/// <c>SettingsAppearanceView.AccentPresets</c> (it cannot reference the net10.0-windows Lib).
/// The values are equal today but easily drift; this test fails if the (Key, R, G, B) sets ever
/// diverge, forcing whoever changes one side to update the other.
/// </summary>
public sealed class AccentPresetDriftTests
{
    [Fact]
    public void AvaloniaAccentPresets_MatchLibSwatches()
    {
        var libSet = AccentColorPresets.Swatches
            .Select(s => (s.Key, s.Color.R, s.Color.G, s.Color.B))
            .ToHashSet();

        var avaloniaSet = ReadAvaloniaAccentPresets();

        avaloniaSet.Should().BeEquivalentTo(libSet,
            "the Avalonia hard-coded accent presets must mirror Lib.AccentColorPresets.Swatches (keep both in sync)");
    }

    // Reflectively reads the private static (byte R, byte G, byte B, string Key)[] AccentPresets
    // field from the Avalonia SettingsAppearanceView, so this guard does not require changing that
    // type's visibility (minimal intrusion into the Avalonia layer).
    private static HashSet<(string Key, byte R, byte G, byte B)> ReadAvaloniaAccentPresets()
    {
        var viewType = typeof(UniversalDeviceToolkit.Avalonia.Pages.SettingsAppearanceView);
        var field = viewType.GetField("AccentPresets", BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull("SettingsAppearanceView must expose a private static AccentPresets array");

        var value = field!.GetValue(null);
        value.Should().NotBeNull("the AccentPresets field must be initialized");

        var result = new HashSet<(string, byte, byte, byte)>();
        foreach (var entry in (IEnumerable)value!)
        {
            var (r, g, b, key) = ((byte R, byte G, byte B, string Key))entry;
            result.Add((key, r, g, b));
        }

        return result;
    }
}
