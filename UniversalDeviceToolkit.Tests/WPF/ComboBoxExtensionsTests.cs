using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using FluentAssertions;
using UniversalDeviceToolkit.WPF.Extensions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class ComboBoxExtensionsTests
{
    [Fact]
    public void AppComboBoxStyle_ShouldUseOldNativeWpfUiTemplate()
    {
        var root = FindRepoRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Styles", "ControlStyles.xaml"));

        xaml.Should().Contain("x:Key=\"AppComboBoxStyle\"");
        xaml.Should().Contain("BasedOn=\"{StaticResource {x:Type ComboBox}}\"");
        xaml.Should().Contain("ButtonHeightStandard");
        xaml.Should().NotContain("AppComboBoxToggleButtonTemplate");
        xaml.Should().NotContain("SelectionBoxItemToStringConverter");
    }

    [Fact]
    public void SetItems_WhenKeyValuePairValueChanges_ShouldSelectItemByKeyAndDisplayLatestValue()
    {
        RunOnStaThread(() =>
        {
            var presetId = Guid.NewGuid();
            var comboBox = new ComboBox();
            var items = new[]
            {
                new KeyValuePair<Guid, PresetListItem>(Guid.NewGuid(), new("Quiet")),
                new KeyValuePair<Guid, PresetListItem>(presetId, new("New preset"))
            };
            var selectedItemWithStaleValue = new KeyValuePair<Guid, PresetListItem>(presetId, new("Old preset"));

            comboBox.SetItems(items, selectedItemWithStaleValue, item => item.Value.Name);

            comboBox.TryGetSelectedItem<KeyValuePair<Guid, PresetListItem>>(out var selectedItem).Should().BeTrue();
            selectedItem.Should().Be(new KeyValuePair<Guid, PresetListItem>(presetId, new("New preset")));
            comboBox.SelectedItem?.ToString().Should().Be("New preset");
        });
    }

    [Fact]
    public void SetItems_WhenSelectedValueMissingFromItems_ShouldSelectFirstItemInsteadOfBlank()
    {
        RunOnStaThread(() =>
        {
            var comboBox = new ComboBox();
            var items = new[] { "Quiet", "Balance", "Performance" };

            // Selected value is not in the list (mirrors Extreme/unlisted power modes).
            comboBox.SetItems(items, "GodMode", item => item);

            comboBox.SelectedItem.Should().NotBeNull("empty selection paints a blank dropdown");
            comboBox.TryGetSelectedItem<string>(out var selected).Should().BeTrue();
            selected.Should().Be("Quiet");
        });
    }

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception is not null)
            throw exception;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.sln")) ||
                File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.WPF", "UniversalDeviceToolkit.WPF.csproj")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate UniversalDeviceToolkit repo root.");
    }

    private sealed record PresetListItem(string Name);
}
