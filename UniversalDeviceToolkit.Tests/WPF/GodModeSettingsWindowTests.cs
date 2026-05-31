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

    [Fact]
    public void AddPreset_ShouldAddNewPresetAndSelectIt()
    {
        var activePresetId = Guid.NewGuid();
        var newPresetId = Guid.NewGuid();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Performance", SourcePowerMode = PowerModeState.Performance },
            [Guid.NewGuid()] = new() { Name = "Quiet", SourcePowerMode = PowerModeState.Quiet }
        });

        var updatedState = GodModeSettingsWindow.AddPreset(state, "My mode", newPresetId);

        updatedState.ActivePresetId.Should().Be(newPresetId);
        updatedState.Presets.Should().ContainKey(newPresetId);
        updatedState.Presets[newPresetId].Name.Should().Be("My mode");
        updatedState.Presets[newPresetId].SourcePowerMode.Should().BeNull();
        updatedState.Presets.Should().HaveCount(3);
    }

    [Fact]
    public void AddPreset_WhenNameAlreadyExists_ShouldUseUniqueName()
    {
        var activePresetId = Guid.NewGuid();
        var newPresetId = Guid.NewGuid();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Performance", SourcePowerMode = PowerModeState.Performance },
            [Guid.NewGuid()] = new() { Name = "Performance (2)" }
        });

        var updatedState = GodModeSettingsWindow.AddPreset(state, "Performance", newPresetId);

        updatedState.Presets[newPresetId].Name.Should().Be("Performance (3)");
    }

    [Fact]
    public void RenameActivePreset_ShouldKeepActivePresetAndRefreshName()
    {
        var activePresetId = Guid.NewGuid();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Performance", SourcePowerMode = PowerModeState.Performance },
            [Guid.NewGuid()] = new() { Name = "Quiet" }
        });

        var updatedState = GodModeSettingsWindow.RenameActivePreset(state, "Custom performance");

        updatedState.ActivePresetId.Should().Be(activePresetId);
        updatedState.Presets[activePresetId].Name.Should().Be("Custom performance");
        updatedState.Presets[activePresetId].SourcePowerMode.Should().BeNull();
        updatedState.Presets.Should().HaveCount(2);
    }

    [Fact]
    public void DeleteActivePreset_ShouldRemoveActivePresetAndSelectFirstRemainingByName()
    {
        var activePresetId = Guid.NewGuid();
        var quietPresetId = Guid.NewGuid();
        var balancePresetId = Guid.NewGuid();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Performance" },
            [quietPresetId] = new() { Name = "Quiet" },
            [balancePresetId] = new() { Name = "Balance" }
        });

        var updatedState = GodModeSettingsWindow.DeleteActivePreset(state);

        updatedState.Presets.Should().NotContainKey(activePresetId);
        updatedState.ActivePresetId.Should().Be(balancePresetId);
        updatedState.Presets.Should().HaveCount(2);
    }

    [Fact]
    public void DeleteActivePreset_WhenOnlyOnePresetExists_ShouldKeepState()
    {
        var activePresetId = Guid.NewGuid();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Only preset" }
        });

        var updatedState = GodModeSettingsWindow.DeleteActivePreset(state);

        updatedState.ActivePresetId.Should().Be(activePresetId);
        updatedState.Presets.Should().ContainSingle();
    }

    private static GodModeState CreateState(Guid activePresetId, Dictionary<Guid, GodModePreset> presets) => new()
    {
        ActivePresetId = activePresetId,
        Presets = new ReadOnlyDictionary<Guid, GodModePreset>(presets)
    };
}
