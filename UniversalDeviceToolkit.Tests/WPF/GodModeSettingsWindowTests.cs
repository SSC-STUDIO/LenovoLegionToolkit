using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.WPF.Controls.Automation;
using UniversalDeviceToolkit.WPF.Controls.Automation.Steps;
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
    public void ResolveSelectedPreset_WhenRequestedPresetExists_ShouldKeepRequestedPreset()
    {
        var activePresetId = Guid.NewGuid();
        var requestedPresetId = Guid.NewGuid();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Balance" },
            [requestedPresetId] = new() { Name = "Performance" }
        });

        var selectedPreset = GodModePresetAutomationStepControl.ResolveSelectedPreset(state, requestedPresetId);

        selectedPreset.Key.Should().Be(requestedPresetId);
        selectedPreset.Value.Name.Should().Be("Performance");
    }

    [Fact]
    public void ResolveSelectedPreset_WhenRequestedPresetWasDeleted_ShouldUseActivePreset()
    {
        var activePresetId = Guid.NewGuid();
        var state = CreateState(activePresetId, new Dictionary<Guid, GodModePreset>
        {
            [activePresetId] = new() { Name = "Balance" },
            [Guid.NewGuid()] = new() { Name = "Performance" }
        });

        var selectedPreset = GodModePresetAutomationStepControl.ResolveSelectedPreset(state, Guid.NewGuid());

        selectedPreset.Key.Should().Be(activePresetId);
        selectedPreset.Value.Name.Should().Be("Balance");
    }

    [Fact]
    public void ResolveSelectedPreset_WhenActivePresetIsMissing_ShouldUseFirstPresetByName()
    {
        var quietPresetId = Guid.NewGuid();
        var balancePresetId = Guid.NewGuid();
        var state = CreateState(Guid.NewGuid(), new Dictionary<Guid, GodModePreset>
        {
            [quietPresetId] = new() { Name = "Quiet" },
            [balancePresetId] = new() { Name = "Balance" }
        });

        var selectedPreset = GodModePresetAutomationStepControl.ResolveSelectedPreset(state, Guid.NewGuid());

        selectedPreset.Key.Should().Be(balancePresetId);
        selectedPreset.Value.Name.Should().Be("Balance");
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

    [Fact]
    public void GodModePresetAutomationStepControlRefresh_ShouldUseCurrentStateAndAvoidEmptyPresetId()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Controls", "Automation", "Steps", "GodModePresetAutomationStepControl.cs");
        var refreshMethod = ExtractMethod(source, "protected override async Task RefreshAsync()");
        var createMethod = ExtractMethod(source, "public override IAutomationStep CreateAutomationStep()");

        refreshMethod.Should().Contain(".OrderBy(kv => kv.Value.Name)");
        refreshMethod.Should().Contain("ResolveSelectedPreset(state, AutomationStep.PresetId)");
        createMethod.Should().Contain("return new GodModePresetAutomationStep(AutomationStep.PresetId);");
        createMethod.Should().NotContain("Guid.Empty");
    }

    [Theory]
    [InlineData(0d, 0, 100, true)]
    [InlineData(100d, 0, 100, true)]
    [InlineData(-100d, -100, 0, true)]
    [InlineData(0d, -100, 0, true)]
    [InlineData(null, 0, 100, false)]
    [InlineData(double.NaN, 0, 100, false)]
    [InlineData(100.5, 0, 100, false)]
    [InlineData(-1d, 0, 100, false)]
    [InlineData(1d, -100, 0, false)]
    public void OffsetValueValidation_ShouldAcceptOnlyWholeNumbersWithinDisplayedRange(double? rawValue, int minimum, int maximum, bool expected)
    {
        var isValid = GodModeSettingsWindow.TryNormalizeOffsetValue(rawValue, minimum, maximum, out _);

        isValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("WARNING!\nLeave this at 0.", "Leave this at 0.")]
    [InlineData("警告！\n请谨慎使用。", "请谨慎使用。")]
    [InlineData("保守使用此设置。", "保守使用此设置。")]
    public void WarningHeading_ShouldBeSeparatedFromWarningDetails(string message, string expected)
    {
        GodModeSettingsWindow.RemoveWarningHeading(message).Should().Be(expected);
    }

    [Fact]
    public void GodModeOffsetControls_ShouldKeepTheVisibleTextInSyncWithValue()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Dashboard", "GodModeSettingsWindow.xaml.cs");
        var defaultsMethod = ExtractMethod(source, "private async Task SetDefaultsAsync(GodModeDefaults defaults)");
        var stateMethod = ExtractMethod(source, "private async Task SetStateAsync(GodModeState state)");
        var setOffsetValueMethod = ExtractMethod(source, "private static void SetOffsetValue");

        defaultsMethod.Should().Contain("SetOffsetValue(_maxValueOffsetNumberBox");
        defaultsMethod.Should().Contain("SetOffsetValue(_minValueOffsetNumberBox");
        stateMethod.Should().Contain("SetOffsetValue(_maxValueOffsetNumberBox");
        stateMethod.Should().Contain("SetOffsetValue(_minValueOffsetNumberBox");
        setOffsetValueMethod.Should().Contain("numberBox.Value = normalizedValue;");
        setOffsetValueMethod.Should().Contain("numberBox.Text = normalizedValue.ToString();");
    }

    [Fact]
    public void GodModeWarnings_ShouldUseWarningChannelInsteadOfSubtitle()
    {
        var source = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Dashboard", "GodModeSettingsWindow.xaml");
        var code = ReadRepositoryFile("UniversalDeviceToolkit.WPF", "Windows", "Dashboard", "GodModeSettingsWindow.xaml.cs");

        source.Should().Contain("x:Name=\"_fanFullSpeedHeader\"");
        source.Should().Contain("x:Name=\"_maxValueOffsetHeader\"");
        source.Should().Contain("x:Name=\"_minValueOffsetHeader\"");
        code.Should().Contain("_fanFullSpeedHeader.Warning");
        code.Should().Contain("_maxValueOffsetHeader.Warning");
        code.Should().Contain("_minValueOffsetHeader.Warning");
        code.Should().Contain("fanFullSpeedEnabled");
        code.Should().Contain("maxOffsetEnabled");
        code.Should().Contain("minOffsetEnabled");
        source.Should().NotContain("Subtitle=\"{x:Static resources:Resource.GodModeSettingsWindow_Fans_Max_Message}\"");
        source.Should().NotContain("Subtitle=\"{x:Static resources:Resource.GodModeSettingsWindow_Advanced_MaxOffset_Message}\"");
        source.Should().NotContain("Subtitle=\"{x:Static resources:Resource.GodModeSettingsWindow_Advanced_MinOffset_Message}\"");
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
