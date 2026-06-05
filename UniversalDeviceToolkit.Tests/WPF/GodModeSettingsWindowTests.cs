using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Controls.Automation;
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
    public void GetUniquePresetName_WhenNameIsBlank_ShouldUseDefaultPresetName()
    {
        var defaultName = GodModeSettingsWindow.GetDefaultPresetName();
        var presets = new ReadOnlyDictionary<Guid, GodModePreset>(
            new Dictionary<Guid, GodModePreset>
            {
                [Guid.NewGuid()] = new() { Name = defaultName }
            });

        var uniqueName = GodModeSettingsWindow.GetUniquePresetName("   ", presets);

        uniqueName.Should().Be($"{defaultName} (2)");
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
    public void AddPreset_WhenNameIsBlank_ShouldUseVisibleDefaultName()
    {
        var activePresetId = Guid.NewGuid();
        var newPresetId = Guid.NewGuid();
        var defaultName = GodModeSettingsWindow.GetDefaultPresetName();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Performance", SourcePowerMode = PowerModeState.Performance },
            [Guid.NewGuid()] = new() { Name = defaultName }
        });

        var updatedState = GodModeSettingsWindow.AddPreset(state, "   ", newPresetId);

        updatedState.ActivePresetId.Should().Be(newPresetId);
        updatedState.Presets[newPresetId].Name.Should().Be($"{defaultName} (2)");
        updatedState.Presets[newPresetId].SourcePowerMode.Should().BeNull();
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

    [Fact]
    public void GetPresetName_WhenPresetWasRenamed_ShouldReturnLatestNameFromCurrentState()
    {
        var presetId = Guid.NewGuid();
        var presets = new ReadOnlyDictionary<Guid, GodModePreset>(
            new Dictionary<Guid, GodModePreset>
            {
                [presetId] = new() { Name = "Renamed performance" },
                [Guid.NewGuid()] = new() { Name = "Quiet" }
            });

        var name = AutomationPipelineControl.GetPresetName(presetId, presets);

        name.Should().Be("Renamed performance");
    }

    [Fact]
    public void GetPresetName_WhenPresetIsMissing_ShouldReturnPlaceholder()
    {
        var presets = new ReadOnlyDictionary<Guid, GodModePreset>(
            new Dictionary<Guid, GodModePreset>
            {
                [Guid.NewGuid()] = new() { Name = "Quiet" }
            });

        var name = AutomationPipelineControl.GetPresetName(Guid.NewGuid(), presets);

        name.Should().Be("-");
    }

    [Fact]
    public void PresetCrudHandlers_ShouldRefreshComboBoxFromPersistedState()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Dashboard", "GodModeSettingsWindow.xaml.cs");
        var refreshMethod = ExtractMethod(source, "private async Task PersistAndRefreshPresetListAsync()");
        var addHandler = ExtractMethod(source, "private async void AddPresetsButton_Click");
        var renameHandler = ExtractMethod(source, "private async void EditPresetsButton_Click");
        var deleteHandler = ExtractMethod(source, "private async void DeletePresetsButton_Click");

        refreshMethod.Should().Contain("await PersistStateAsync();");
        refreshMethod.Should().Contain("await SetStateAsync(_state.Value);");
        addHandler.Should().Contain("await PersistAndRefreshPresetListAsync();");
        renameHandler.Should().Contain("await PersistAndRefreshPresetListAsync();");
        deleteHandler.Should().Contain("await PersistAndRefreshPresetListAsync();");
    }

    private static GodModeState CreateState(Guid activePresetId, Dictionary<Guid, GodModePreset> presets) => new()
    {
        ActivePresetId = activePresetId,
        Presets = new ReadOnlyDictionary<Guid, GodModePreset>(presets)
    };

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        var braceStart = source.IndexOf('{', start);
        braceStart.Should().BeGreaterThanOrEqualTo(0);

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return source[start..(i + 1)];
            }
        }

        throw new InvalidOperationException($"Could not extract method '{signature}'.");
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var expectedRelativePath = Path.Combine(pathParts);
        foreach (var candidateRoot in GetRepositoryRootCandidates())
        {
            var path = Path.Combine(candidateRoot, expectedRelativePath);
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        throw new DirectoryNotFoundException($"Could not locate repository file '{expectedRelativePath}'.");
    }

    private static IEnumerable<string> GetRepositoryRootCandidates()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Environment.CurrentDirectory,
            AppContext.BaseDirectory,
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."))
        };

        foreach (var root in roots.Where(static root => !string.IsNullOrWhiteSpace(root)))
        {
            var directory = new DirectoryInfo(root!);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "UniversalDeviceToolkit.sln")))
                    yield return directory.FullName;

                directory = directory.Parent;
            }
        }
    }
}
