using System;
using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.WPF.Settings;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Settings;

[Trait("Category", TestCategories.Unit)]
public class SettingsNormalizationTests
{
    [Fact]
    public void ApplicationSettings_Normalize_ShouldRepairNullCollections()
    {
        var store = new ApplicationSettings.ApplicationSettingsStore
        {
            PowerPlans = null!,
            PowerModes = null!,
            Notifications = null!,
            ExcludedRefreshRates = null!,
            SmartKeySinglePressActionList = null!,
            SmartKeyDoublePressActionList = null!,
            ExcludedProcesses = null!,
            CustomCleanupRules =
            [
                null!,
                new CustomCleanupRule
                {
                    DirectoryPath = null!,
                    Extensions = null!
                }
            ],
            InstalledExtensions = null!,
            PendingDeletionExtensions = null!,
            NavigationItemsVisibility = null!
        };

        var normalized = Normalize<ApplicationSettings.ApplicationSettingsStore>(typeof(ApplicationSettings), store);

        normalized.PowerPlans.Should().NotBeNull().And.BeEmpty();
        normalized.PowerModes.Should().NotBeNull().And.BeEmpty();
        normalized.Notifications.Should().NotBeNull();
        normalized.ExcludedRefreshRates.Should().NotBeNull().And.BeEmpty();
        normalized.SmartKeySinglePressActionList.Should().NotBeNull().And.BeEmpty();
        normalized.SmartKeyDoublePressActionList.Should().NotBeNull().And.BeEmpty();
        normalized.ExcludedProcesses.Should().NotBeNull().And.BeEmpty();
        normalized.CustomCleanupRules.Should().ContainSingle();
        normalized.CustomCleanupRules[0].DirectoryPath.Should().BeEmpty();
        normalized.CustomCleanupRules[0].Extensions.Should().NotBeNull().And.BeEmpty();
        normalized.InstalledExtensions.Should().NotBeNull().And.BeEmpty();
        normalized.PendingDeletionExtensions.Should().NotBeNull().And.BeEmpty();
        normalized.NavigationItemsVisibility.Should().NotBeNull();
    }

    [Fact]
    public void PackageDownloaderSettings_Normalize_ShouldRepairNullHiddenPackages()
    {
        var store = new PackageDownloaderSettings.PackageDownloaderSettingsStore
        {
            HiddenPackages = null!
        };

        var normalized = Normalize<PackageDownloaderSettings.PackageDownloaderSettingsStore>(typeof(PackageDownloaderSettings), store);

        normalized.HiddenPackages.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void PluginSettings_Normalize_ShouldRepairNullPluginLanguages()
    {
        var store = new PluginSettings.PluginSettingsStore
        {
            PluginLanguages = null!
        };

        var normalized = Normalize<PluginSettings.PluginSettingsStore>(typeof(PluginSettings), store);

        normalized.PluginLanguages.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void FanCurveSettings_Normalize_ShouldRepairNullEntries()
    {
        var store = new FanCurveSettings.FanCurveSettingsStore
        {
            Entries = null!
        };

        var normalized = Normalize<FanCurveSettings.FanCurveSettingsStore>(typeof(FanCurveSettings), store);

        normalized.Entries.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void GodModeSettings_Normalize_ShouldRepairNullPresets()
    {
        var store = new GodModeSettings.GodModeSettingsStore
        {
            Presets = null!
        };

        var normalized = Normalize<GodModeSettings.GodModeSettingsStore>(typeof(GodModeSettings), store);

        normalized.Presets.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void GodModeSettings_Normalize_ShouldFilterNullPresetsAndRepairNames()
    {
        var validPresetId = Guid.NewGuid();
        var nullPresetId = Guid.NewGuid();
        var store = new GodModeSettings.GodModeSettingsStore
        {
            Presets = new Dictionary<Guid, GodModeSettings.GodModeSettingsStore.Preset>
            {
                [validPresetId] = new() { Name = null! },
                [nullPresetId] = null!
            }
        };

        var normalized = Normalize<GodModeSettings.GodModeSettingsStore>(typeof(GodModeSettings), store);

        normalized.Presets.Should().ContainKey(validPresetId);
        normalized.Presets.Should().NotContainKey(nullPresetId);
        normalized.Presets[validPresetId].Name.Should().BeEmpty();
    }

    [Fact]
    public void GPUOverclockSettings_Normalize_ShouldRepairProfiles()
    {
        var validProfileId = Guid.NewGuid();
        var nullProfileId = Guid.NewGuid();
        var store = new GPUOverclockSettings.GPUOverclockSettingsStore
        {
            Profiles = new Dictionary<Guid, GPUOverclockSettings.GPUOverclockSettingsStore.Profile>
            {
                [validProfileId] = new() { Name = null! },
                [nullProfileId] = null!
            }
        };

        var normalized = Normalize<GPUOverclockSettings.GPUOverclockSettingsStore>(typeof(GPUOverclockSettings), store);

        normalized.Profiles.Should().ContainKey(validProfileId);
        normalized.Profiles.Should().NotContainKey(nullProfileId);
        normalized.Profiles[validProfileId].Name.Should().Be(GPUOverclockSettings.DefaultProfileName);
    }

    [Fact]
    public void RGBKeyboardSettings_Normalize_ShouldRepairNullPresets()
    {
        var store = new RGBKeyboardSettings.RGBKeyboardSettingsStore
        {
            State = new RGBKeyboardBacklightState(RGBKeyboardBacklightPreset.One, null!)
        };

        var normalized = Normalize<RGBKeyboardSettings.RGBKeyboardSettingsStore>(typeof(RGBKeyboardSettings), store);

        normalized.State.SelectedPreset.Should().Be(RGBKeyboardBacklightPreset.One);
        normalized.State.Presets.Should().NotBeNull();
        normalized.State.Presets.Should().ContainKeys(
            RGBKeyboardBacklightPreset.One,
            RGBKeyboardBacklightPreset.Two,
            RGBKeyboardBacklightPreset.Three,
            RGBKeyboardBacklightPreset.Four);
    }

    [Fact]
    public void RGBKeyboardSettings_Normalize_ShouldFilterInvalidPresetKeys()
    {
        var store = new RGBKeyboardSettings.RGBKeyboardSettingsStore
        {
            State = new RGBKeyboardBacklightState(
                (RGBKeyboardBacklightPreset)999,
                new Dictionary<RGBKeyboardBacklightPreset, RGBKeyboardBacklightBacklightPresetDescription>
                {
                    [(RGBKeyboardBacklightPreset)999] = RGBKeyboardBacklightBacklightPresetDescription.Default,
                    [RGBKeyboardBacklightPreset.One] = RGBKeyboardBacklightBacklightPresetDescription.Default
                })
        };

        var normalized = Normalize<RGBKeyboardSettings.RGBKeyboardSettingsStore>(typeof(RGBKeyboardSettings), store);

        normalized.State.SelectedPreset.Should().Be(RGBKeyboardBacklightPreset.Off);
        normalized.State.Presets.Should().ContainKey(RGBKeyboardBacklightPreset.One);
        normalized.State.Presets.Should().NotContainKey((RGBKeyboardBacklightPreset)999);
    }

    [Fact]
    public void OsdSettings_Normalize_ShouldRepairNullItems()
    {
        var store = new OsdSettings.OsdSettingsStore
        {
            Items = null!
        };

        var normalized = Normalize<OsdSettings.OsdSettingsStore>(typeof(OsdSettings), store);

        normalized.Items.Should().NotBeNull();
        normalized.Items.Should().Contain(Enum.GetValues<OsdItem>());
    }

    [Fact]
    public void OsdSettings_Normalize_ShouldFilterInvalidAndDuplicateItems()
    {
        var store = new OsdSettings.OsdSettingsStore
        {
            Items = [(OsdItem)999, OsdItem.Fps, OsdItem.Fps]
        };

        var normalized = Normalize<OsdSettings.OsdSettingsStore>(typeof(OsdSettings), store);

        normalized.Items.Should().Equal(OsdItem.Fps);
    }

    [Fact]
    public void LampArraySettings_Normalize_ShouldRepairNestedNullCollections()
    {
        var store = new LampArraySettings.LampArraySettingsStore
        {
            DefaultEffect = new LampArraySettings.LampEffectConfig
            {
                Parameters = null!
            },
            PerLampEffects = new Dictionary<int, LampArraySettings.LampEffectConfig>
            {
                [1] = new() { Parameters = null! },
                [2] = null!
            }
        };

        var normalized = Normalize<LampArraySettings.LampArraySettingsStore>(typeof(LampArraySettings), store);

        normalized.DefaultEffect.Should().NotBeNull();
        normalized.DefaultEffect!.Parameters.Should().NotBeNull().And.BeEmpty();
        normalized.PerLampEffects.Should().ContainKey(1);
        normalized.PerLampEffects.Should().NotContainKey(2);
        normalized.PerLampEffects[1].Parameters.Should().NotBeNull().And.BeEmpty();
    }

    private static TStore Normalize<TStore>(Type settingsType, TStore store)
    {
        var method = settingsType.GetMethod("Normalize", BindingFlags.NonPublic | BindingFlags.Static);

        method.Should().NotBeNull();
        var normalized = method!.Invoke(null, [store]);

        normalized.Should().BeOfType<TStore>();
        return (TStore)normalized!;
    }
}
