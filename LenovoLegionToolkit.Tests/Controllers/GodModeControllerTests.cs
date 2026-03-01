using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LenovoLegionToolkit.Tests.Controllers;

[TestClass]
[TestCategory(TestCategories.Controller)]
public class GodModeControllerTests : UnitTestBase
{
    private TestGodModeControllerV1 _controllerV1 = null!;
    private TestGodModeControllerV2 _controllerV2 = null!;
    private GodModeController _controller = null!;
    private object? _originalMachineInformation;
    private object? _originalIsCompatible;

    protected override void Setup()
    {
        BackupCompatibilityState();

        var v1Settings = new GodModeSettings();
        var v2Settings = new GodModeSettings();

        _controllerV1 = new TestGodModeControllerV1(v1Settings);
        _controllerV2 = new TestGodModeControllerV2(v2Settings);
        _controller = new GodModeController(_controllerV1, _controllerV2);

        SetCompatibility(supportsGodModeV1: true, supportsGodModeV2: false);
    }

    protected override void Cleanup()
    {
        RestoreCompatibilityState();
    }

    [TestMethod]
    public void Constructor_ShouldInitializeCorrectly()
    {
        _controller.Should().NotBeNull();
    }

    [TestMethod]
    public void PresetChanged_Event_ShouldBeSubscribedToBothControllers()
    {
        var eventCount = 0;
        EventHandler<Guid> handler = (_, _) => eventCount++;

        _controller.PresetChanged += handler;

        _controllerV1.TriggerPresetChanged(Guid.NewGuid());
        _controllerV2.TriggerPresetChanged(Guid.NewGuid());

        eventCount.Should().Be(2);

        _controller.PresetChanged -= handler;
    }

    [TestMethod]
    public async Task NeedsVantageDisabledAsync_ShouldCallCorrectController()
    {
        var result = await _controller.NeedsVantageDisabledAsync();

        result.Should().BeFalse();
    }

    [TestMethod]
    public async Task NeedsLegionZoneDisabledAsync_ShouldCallCorrectController()
    {
        var result = await _controller.NeedsLegionZoneDisabledAsync();

        result.Should().BeTrue();
    }

    [TestMethod]
    public async Task GetActivePresetIdAsync_ShouldReturnCorrectId()
    {
        var expectedId = Guid.NewGuid();
        _controllerV1.SetStore(expectedId, new Dictionary<Guid, GodModeSettings.GodModeSettingsStore.Preset>
        {
            [expectedId] = new() { Name = "Preset A" }
        });

        var result = await _controller.GetActivePresetIdAsync();

        result.Should().Be(expectedId);
    }

    [TestMethod]
    public async Task GetActivePresetNameAsync_ShouldReturnCorrectName()
    {
        const string expectedName = "Test Preset";
        var presetId = Guid.NewGuid();
        _controllerV1.SetStore(presetId, new Dictionary<Guid, GodModeSettings.GodModeSettingsStore.Preset>
        {
            [presetId] = new() { Name = expectedName }
        });

        var result = await _controller.GetActivePresetNameAsync();

        result.Should().Be(expectedName);
    }

    [TestMethod]
    public async Task GetActivePresetNameAsync_WhenNoPreset_ShouldReturnNull()
    {
        var activeId = Guid.NewGuid();
        _controllerV1.SetStore(activeId, new Dictionary<Guid, GodModeSettings.GodModeSettingsStore.Preset>
        {
            [Guid.NewGuid()] = new() { Name = "Different Preset" }
        });

        var result = await _controller.GetActivePresetNameAsync();

        result.Should().BeNull();
    }

    [TestMethod]
    public async Task SetStateAsync_ShouldCallCorrectController()
    {
        var presetId = Guid.NewGuid();
        var state = new GodModeState
        {
            ActivePresetId = presetId,
            Presets = new ReadOnlyDictionary<Guid, GodModePreset>(new Dictionary<Guid, GodModePreset>
            {
                [presetId] = new() { Name = "Saved Preset" }
            })
        };

        await _controller.SetStateAsync(state);

        var result = await _controller.GetActivePresetIdAsync();
        result.Should().Be(presetId);
    }

    [TestMethod]
    public async Task ApplyStateAsync_ShouldCallCorrectController()
    {
        await _controller.ApplyStateAsync();

        _controllerV1.ApplyCalls.Should().Be(1);
    }

    [TestMethod]
    public async Task GetDefaultFanTableAsync_ShouldReturnFanTable()
    {
        var result = await _controller.GetDefaultFanTableAsync();

        result.GetTable().Should().Equal((ushort)1, (ushort)2, (ushort)3, (ushort)4, (ushort)5, (ushort)6, (ushort)7, (ushort)8, (ushort)9, (ushort)10);
    }

    [TestMethod]
    public async Task GetMinimumFanTableAsync_ShouldReturnFanTable()
    {
        var result = await _controller.GetMinimumFanTableAsync();

        result.GetTable().Should().Equal((ushort)1, (ushort)1, (ushort)1, (ushort)1, (ushort)1, (ushort)1, (ushort)1, (ushort)1, (ushort)3, (ushort)5);
    }

    [TestMethod]
    public async Task GetDefaultsInOtherPowerModesAsync_ShouldReturnDictionary()
    {
        var result = await _controller.GetDefaultsInOtherPowerModesAsync();

        result.Should().ContainKey(PowerModeState.Quiet);
    }

    [TestMethod]
    public async Task RestoreDefaultsInOtherPowerModeAsync_ShouldCallCorrectController()
    {
        var powerMode = PowerModeState.Quiet;
        await _controller.RestoreDefaultsInOtherPowerModeAsync(powerMode);

        _controllerV1.RestoreCalls.Should().Be(1);
        _controllerV1.LastRestoreState.Should().Be(powerMode);
    }

    private void BackupCompatibilityState()
    {
        _originalMachineInformation = GetCompatibilityField("_machineInformation").GetValue(null);
        _originalIsCompatible = GetCompatibilityField("_isCompatible").GetValue(null);
    }

    private void RestoreCompatibilityState()
    {
        GetCompatibilityField("_machineInformation").SetValue(null, _originalMachineInformation);
        GetCompatibilityField("_isCompatible").SetValue(null, _originalIsCompatible);
    }

    private static void SetCompatibility(bool supportsGodModeV1, bool supportsGodModeV2)
    {
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "82XX",
            Model = "16IRX",
            SupportedPowerModes = new[] { PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance, PowerModeState.GodMode },
            Properties = new MachineInformation.PropertyData
            {
                SupportsGodModeV1 = supportsGodModeV1,
                SupportsGodModeV2 = supportsGodModeV2
            }
        };

        GetCompatibilityField("_machineInformation").SetValue(null, machineInformation);
        GetCompatibilityField("_isCompatible").SetValue(null, true);
    }

    private static FieldInfo GetCompatibilityField(string name)
    {
        var field = typeof(Compatibility).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();
        return field!;
    }

    private sealed class TestGodModeControllerV1 : GodModeControllerV1
    {
        private readonly GodModeSettings _settings;

        public int ApplyCalls { get; private set; }
        public int RestoreCalls { get; private set; }
        public PowerModeState? LastRestoreState { get; private set; }

        public TestGodModeControllerV1(GodModeSettings settings)
            : base(settings, new LegionZoneDisabler())
        {
            _settings = settings;
        }

        public override Task<bool> NeedsVantageDisabledAsync() => Task.FromResult(false);

        public override Task<bool> NeedsLegionZoneDisabledAsync() => Task.FromResult(true);

        public override Task ApplyStateAsync()
        {
            ApplyCalls++;
            return Task.CompletedTask;
        }

        public override Task<FanTable> GetMinimumFanTableAsync() => Task.FromResult(new FanTable([1, 1, 1, 1, 1, 1, 1, 1, 3, 5]));

        public override Task<Dictionary<PowerModeState, GodModeDefaults>> GetDefaultsInOtherPowerModesAsync() =>
            Task.FromResult(new Dictionary<PowerModeState, GodModeDefaults>
            {
                [PowerModeState.Quiet] = new() { CPULongTermPowerLimit = 1 }
            });

        public override Task RestoreDefaultsInOtherPowerModeAsync(PowerModeState state)
        {
            RestoreCalls++;
            LastRestoreState = state;
            return Task.CompletedTask;
        }

        protected override Task<GodModePreset> GetDefaultStateAsync() => Task.FromResult(new GodModePreset { Name = "Default V1" });

        public void TriggerPresetChanged(Guid presetId) => RaisePresetChanged(presetId);

        public void SetStore(Guid activePresetId, Dictionary<Guid, GodModeSettings.GodModeSettingsStore.Preset> presets)
        {
            _settings.Store.ActivePresetId = activePresetId;
            _settings.Store.Presets = presets;
        }
    }

    private sealed class TestGodModeControllerV2 : GodModeControllerV2
    {
        public TestGodModeControllerV2(GodModeSettings settings)
            : base(settings, new VantageDisabler(), new LegionZoneDisabler())
        {
        }

        public override Task<bool> NeedsVantageDisabledAsync() => Task.FromResult(true);

        public override Task<bool> NeedsLegionZoneDisabledAsync() => Task.FromResult(true);

        public override Task ApplyStateAsync() => Task.CompletedTask;

        public override Task<FanTable> GetMinimumFanTableAsync() => Task.FromResult(new FanTable([1, 1, 1, 1, 1, 1, 1, 1, 3, 5]));

        public override Task<Dictionary<PowerModeState, GodModeDefaults>> GetDefaultsInOtherPowerModesAsync() =>
            Task.FromResult(new Dictionary<PowerModeState, GodModeDefaults>());

        public override Task RestoreDefaultsInOtherPowerModeAsync(PowerModeState state) => Task.CompletedTask;

        protected override Task<GodModePreset> GetDefaultStateAsync() => Task.FromResult(new GodModePreset { Name = "Default V2" });

        public void TriggerPresetChanged(Guid presetId) => RaisePresetChanged(presetId);
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class FanTableTests : UnitTestBase
{
    [TestMethod]
    public void FanTable_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        var fanTable = new FanTable();

        fanTable.Should().NotBeNull();
    }

    [TestMethod]
    public void FanTable_WithParameters_ShouldSetPropertiesCorrectly()
    {
        var fanTable = new FanTable
        {
            FSTM = 1,
            FSID = 0
        };

        fanTable.FSTM.Should().Be(1);
        fanTable.FSID.Should().Be(0);
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class GodModeStateTests : UnitTestBase
{
    [TestMethod]
    public void GodModeState_DefaultConstructor_ShouldInitialize()
    {
        var state = new GodModeState();

        state.Should().NotBeNull();
    }

    [TestMethod]
    public void GodModeState_WithPresetId_ShouldSetCorrectly()
    {
        var presetId = Guid.NewGuid();
        var state = new GodModeState
        {
            ActivePresetId = presetId
        };

        state.ActivePresetId.Should().Be(presetId);
    }
}
