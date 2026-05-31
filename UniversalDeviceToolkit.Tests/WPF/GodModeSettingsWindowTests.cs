using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Windows.Dashboard;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class GodModeSettingsWindowTests
{
    [Fact]
    public void GetUniquePresetName_WhenNameAlreadyExists_ShouldAppendIncrementingSuffix()
    {
        var presets = new ReadOnlyDictionary<Guid, GodModePreset>(
            new Dictionary<Guid, GodModePreset>
            {
                [Guid.NewGuid()] = new() { Name = "Performance" },
                [Guid.NewGuid()] = new() { Name = "Performance (2)" },
                [Guid.NewGuid()] = new() { Name = "Quiet" }
            });

        var uniqueName = GodModeSettingsWindow.GetUniquePresetName("Performance", presets);

        uniqueName.Should().Be("Performance (3)");
    }

    [Fact]
    public void GetUniquePresetName_WhenRenamingCurrentPresetToSameName_ShouldKeepOriginalName()
    {
        var activePresetId = Guid.NewGuid();
        var presets = new ReadOnlyDictionary<Guid, GodModePreset>(
            new Dictionary<Guid, GodModePreset>
            {
                [activePresetId] = new() { Name = "Performance" },
                [Guid.NewGuid()] = new() { Name = "Quiet" }
            });

        var uniqueName = GodModeSettingsWindow.GetUniquePresetName("Performance", presets, activePresetId);

        uniqueName.Should().Be("Performance");
    }
}
