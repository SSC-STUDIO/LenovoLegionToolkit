using Avalonia;
using Avalonia.Media;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Controls;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class SpectrumKeyboardLayoutCanvasTests
{
    private static readonly IReadOnlyList<SpectrumKeyGeometry>[] AllLayouts =
    [
        SpectrumKeyboardLayoutData.Ansi,
        SpectrumKeyboardLayoutData.Iso,
        SpectrumKeyboardLayoutData.Jis,
    ];

    [Theory]
    [InlineData(SpectrumKeyboardLayoutKind.Ansi, 101)]
    [InlineData(SpectrumKeyboardLayoutKind.Iso, 102)]
    [InlineData(SpectrumKeyboardLayoutKind.Jis, 105)]
    public void Layout_ShouldMatchWpfKeyCount(SpectrumKeyboardLayoutKind kind, int expected)
    {
        SpectrumKeyboardLayoutData.GetLayout(kind).Should().HaveCount(expected);
    }

    [Fact]
    public void Geometry_ShouldStayInsideCanvasBounds()
    {
        foreach (var layout in AllLayouts)
        {
            layout.Should().NotBeEmpty();
            foreach (var key in layout)
            {
                key.X.Should().BeGreaterThanOrEqualTo(0);
                key.Y.Should().BeGreaterThanOrEqualTo(0);
                (key.X + key.Width).Should().BeLessThanOrEqualTo(SpectrumKeyboardLayoutData.CanvasWidth);
                (key.Y + key.Height).Should().BeLessThanOrEqualTo(SpectrumKeyboardLayoutData.CanvasHeight);
                key.Width.Should().BeGreaterThan(0);
                key.Height.Should().BeGreaterThan(0);
            }
        }
    }

    [Fact]
    public void Geometry_ShouldContainUniqueKeyCodesPerLayout()
    {
        foreach (var layout in AllLayouts)
            layout.Select(key => key.KeyCode).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Geometry_ShouldNotHaveOverlappingKeys()
    {
        foreach (var (layout, layoutName) in new[]
                 {
                     (SpectrumKeyboardLayoutData.Ansi, "ANSI"),
                     (SpectrumKeyboardLayoutData.Iso, "ISO"),
                     (SpectrumKeyboardLayoutData.Jis, "JIS"),
                 })
        {
            for (var first = 0; first < layout.Count; first++)
            {
                for (var second = first + 1; second < layout.Count; second++)
                {
                    var a = layout[first];
                    var b = layout[second];
                    var overlaps = a.X < b.X + b.Width && a.X + a.Width > b.X
                        && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;
                    overlaps.Should().BeFalse(
                        $"keys 0x{a.KeyCode:X4} and 0x{b.KeyCode:X4} overlap in {layoutName}");
                }
            }
        }
    }

    [Fact]
    public void LayoutKeySets_ShouldMatchDetectedKeyboardLayoutRules()
    {
        var ansi = SpectrumKeyboardLayoutData.Ansi.Select(key => key.KeyCode).ToHashSet();
        var iso = SpectrumKeyboardLayoutData.Iso.Select(key => key.KeyCode).ToHashSet();
        var jis = SpectrumKeyboardLayoutData.Jis.Select(key => key.KeyCode).ToHashSet();

        ansi.Should().Contain(0x01).And.Contain(0x14)
            .And.Contain(0x98).And.Contain(0xA7)
            .And.NotContain(0xA8).And.NotContain(0xA9);
        iso.Should().Contain(0xA8).And.Contain(0x77).And.NotContain(0xA9);
        jis.Should().Contain(0xA9).And.Contain(0xAA).And.Contain(0xAB)
            .And.Contain(0x60).And.Contain(0xA8).And.Contain(0x4D).And.Contain(0x38);
    }

    [Theory]
    [InlineData("Ansi", SpectrumKeyboardLayoutKind.Ansi)]
    [InlineData("iso", SpectrumKeyboardLayoutKind.Iso)]
    [InlineData("JIS", SpectrumKeyboardLayoutKind.Jis)]
    public void TryParse_ShouldMatchLayoutNamesCaseInsensitively(string name, SpectrumKeyboardLayoutKind expected)
    {
        SpectrumKeyboardLayoutData.TryParse(name, out var kind).Should().BeTrue();
        kind.Should().Be(expected);
    }

    [Theory]
    [InlineData("Keyboard24Zone")]
    [InlineData("Ansi64")]
    [InlineData("")]
    public void TryParse_ShouldRejectUnknownLayoutNames(string name)
    {
        SpectrumKeyboardLayoutData.TryParse(name, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParse_ShouldRejectNullLayoutName()
    {
        SpectrumKeyboardLayoutData.TryParse(null, out _).Should().BeFalse();
    }

    [Fact]
    public void GetLayout_ShouldAcceptThePageLayoutStrings()
    {
        SpectrumKeyboardLayoutData.GetLayout("Ansi").Should().BeSameAs(SpectrumKeyboardLayoutData.Ansi);
        SpectrumKeyboardLayoutData.GetLayout("Iso").Should().BeSameAs(SpectrumKeyboardLayoutData.Iso);
        SpectrumKeyboardLayoutData.GetLayout("jis").Should().BeSameAs(SpectrumKeyboardLayoutData.Jis);
        SpectrumKeyboardLayoutData.GetLayout("unknown").Should().BeSameAs(SpectrumKeyboardLayoutData.Ansi);
    }

    [Fact]
    public void HitTestKey_ShouldMapCanvasPointsToKeyCodes()
    {
        var geometry = SpectrumKeyboardLayoutData.Ansi;

        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(10, 10), new Size(400, 200)).Should().Be(0x01);
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(270, 10), new Size(400, 200)).Should().Be(0x10);
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(25, 30), new Size(400, 200)).Should().Be(0x17);
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(380, 30), new Size(400, 200)).Should().Be(0x29);
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(380, 50), new Size(400, 200)).Should().Be(0x68);
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(380, 80), new Size(400, 200)).Should().Be(0xA7);
    }

    [Fact]
    public void HitTestKey_ShouldReturnNullForEmptySpace()
    {
        var geometry = SpectrumKeyboardLayoutData.Ansi;

        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(10, 150), new Size(400, 200)).Should().BeNull();
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(-5, 5), new Size(400, 200)).Should().BeNull();
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(5, 5), new Size(0, 0)).Should().BeNull();
        SpectrumKeyboardLayoutCanvas.HitTestKey(geometry, new Point(5, 5), new Size(200, 0)).Should().BeNull();
    }

    [Fact]
    public void GetVisibleKeys_ShouldFilterGeometryByAvailableKeys()
    {
        IReadOnlyList<SpectrumKeyGeometry> geometry =
        [
            new SpectrumKeyGeometry(0x01, 0, 0, 1, 1),
            new SpectrumKeyGeometry(0x02, 1, 0, 1, 1),
        ];

        SpectrumKeyboardLayoutCanvas.GetVisibleKeys(geometry, [0x02])
            .Should().ContainSingle(key => key.KeyCode == 0x02);
        SpectrumKeyboardLayoutCanvas.GetVisibleKeys(geometry, null).Should().HaveCount(2);
        SpectrumKeyboardLayoutCanvas.GetVisibleKeys(geometry, []).Should().HaveCount(2);
    }

    [Fact]
    public void CombineKeyColors_ShouldApplyLaterEffectOverSharedKeys()
    {
        var red = Color.FromRgb(255, 0, 0);
        var blue = Color.FromRgb(0, 0, 255);

        var map = SpectrumKeyboardLayoutCanvas.CombineKeyColors(
        [
            (Keys: new ushort[] { 0x01, 0x02 }, Colors: new[] { red }),
            (Keys: new ushort[] { 0x02, 0x03 }, Colors: new[] { blue }),
        ]);

        map.Should().HaveCount(3);
        map[0x01].Should().Be(red);
        map[0x02].Should().Be(blue);
        map[0x03].Should().Be(blue);
    }

    [Fact]
    public void CombineKeyColors_ShouldCycleThroughEffectColors()
    {
        var red = Color.FromRgb(255, 0, 0);
        var green = Color.FromRgb(0, 255, 0);

        var map = SpectrumKeyboardLayoutCanvas.CombineKeyColors(
        [
            (Keys: new ushort[] { 0x01, 0x02, 0x03 }, Colors: new[] { red, green }),
        ]);

        map[0x01].Should().Be(red);
        map[0x02].Should().Be(green);
        map[0x03].Should().Be(red);
    }

    [Fact]
    public void CombineKeyColors_ShouldSkipColorlessContributions()
    {
        var map = SpectrumKeyboardLayoutCanvas.CombineKeyColors(
        [
            (Keys: new ushort[] { 0x01 }, Colors: Array.Empty<Color>()),
        ]);

        map.Should().BeEmpty();
    }

    [Fact]
    public void ToggleKey_ShouldAddAndRemoveSelection()
    {
        var canvas = new SpectrumKeyboardLayoutCanvas();

        canvas.ToggleKey(0x01);
        canvas.Selection.Should().Contain(0x01);

        canvas.ToggleKey(0x01);
        canvas.Selection.Should().NotContain(0x01);
    }

    [Fact]
    public void SelectionMutations_ShouldRaiseSelectionChangedOnlyOnChange()
    {
        var canvas = new SpectrumKeyboardLayoutCanvas();
        var raised = 0;
        canvas.SelectionChanged += (_, _) => raised++;

        canvas.ToggleKey(0x01);
        raised.Should().Be(1);
        canvas.SetSelection([0x02]);
        raised.Should().Be(2);
        canvas.SetSelection([0x02]);
        raised.Should().Be(2);
        canvas.SetKey(0x02, false);
        raised.Should().Be(3);
        canvas.SetKey(0x02, false);
        raised.Should().Be(3);
        canvas.SetKey(0x03, true);
        raised.Should().Be(4);
        canvas.ClearSelection();
        raised.Should().Be(5);
        canvas.ClearSelection();
        raised.Should().Be(5);
    }

    [Fact]
    public void Selection_ShouldRespectAvailableKeys()
    {
        var canvas = new SpectrumKeyboardLayoutCanvas { AvailableKeys = [0x01, 0x02] };

        canvas.ToggleKey(0x05);
        canvas.Selection.Should().BeEmpty();
        canvas.SetKey(0x09, true);
        canvas.Selection.Should().BeEmpty();

        canvas.ToggleKey(0x01);
        canvas.Selection.Should().Contain(0x01);
    }
}
