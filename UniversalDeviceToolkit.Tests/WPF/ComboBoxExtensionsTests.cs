using System;
using System.Collections.Generic;
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

    private sealed record PresetListItem(string Name);
}
