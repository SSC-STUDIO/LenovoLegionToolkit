using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Lifecycle;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.WPF.CLI;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaMigrationContractTests
{
    [Fact]
    public void WpfIpcServer_ImplementsSharedCliLifecycleContract()
    {
        typeof(IpcServer).GetInterfaces().Should().Contain(typeof(ICliHostLifecycle));
    }

    [Theory]
    [InlineData("CPU Usage", "CPU", "cpu")]
    [InlineData("GPU Temperature", "Temperature", "gpu")]
    [InlineData("Battery Charge", "Battery", "battery")]
    [InlineData("Memory Total", "Memory", "system")]
    public void DashboardTelemetryGroups_ClassifiesAdapterReadings(
        string name,
        string category,
        string expectedGroup)
    {
        var reading = new SensorReadingItem(name, "1", category, 1, "%");

        DashboardTelemetryGroups.Classify(reading).Should().Be(expectedGroup);
    }

    [Fact]
    public void DashboardSensorLayout_FiltersHiddenSectionsAndPreservesConfiguredOrder()
    {
        var readings = new SensorReadingItem[]
        {
            new("GPU Temperature", "70 C", "GPU", 70, "C"),
            new("System Memory", "8 GB", "System", 8, "GB"),
            new("CPU Usage", "40 %", "CPU", 40, "%"),
            new("Battery Charge", "80 %", "Battery", 80, "%"),
        };

        var filtered = DashboardSensorLayout.FilterAndOrder(
            readings,
            ["GPU", "CPU"],
            ["GPU", "CPU", "Battery"]);

        filtered.Select(reading => reading.Name)
            .Should().Equal("GPU Temperature", "CPU Usage", "System Memory");
        DashboardSensorLayout.GetCardOrder(["GPU", "CPU", "Battery"])
            .Should().Equal("gpu", "cpu", "battery", "system");
        DashboardSensorLayout.IsCardVisible("battery", ["GPU", "CPU"])
            .Should().BeFalse();
        DashboardSensorLayout.IsCardVisible("system", ["GPU", "CPU"])
            .Should().BeTrue();
    }

    [Fact]
    public void DashboardItemDescriptors_PreserveWpfControlSemantics()
    {
        DashboardItemDescriptors.Get("PowerMode").PresentationMode
            .Should().Be(DashboardItemPresentationMode.Combo);
        DashboardItemDescriptors.Get("HDR").PresentationMode
            .Should().Be(DashboardItemPresentationMode.Toggle);
        DashboardItemDescriptors.Get("DiscreteGpu").PresentationMode
            .Should().Be(DashboardItemPresentationMode.Custom);
        DashboardItemDescriptors.Get(DashboardGroupViewModel.OneLevelWhiteKeyboardBacklightIdentifier)
            .PresentationMode.Should().Be(DashboardItemPresentationMode.Toggle);
    }

    [Fact]
    public void WhiteKeyboardBacklightLayoutRendersBothWpfControlsAndPersistsOneItem()
    {
        var group = new DashboardGroupViewModel(new DashboardGroupState(
            "Other",
            null,
            ["WhiteKeyboardBacklight"]));

        group.Items.Select(item => item.Identifier)
            .Should().Equal(
                "WhiteKeyboardBacklight",
                DashboardGroupViewModel.OneLevelWhiteKeyboardBacklightIdentifier);
        group.ToState().Items.Should().Equal("WhiteKeyboardBacklight");
    }

    [Fact]
    public void HybridModeUsesToggleSemanticsWhenOnlyOnAndOffAreSupported()
    {
        var group = new DashboardGroupViewModel(
            new DashboardGroupState("Graphics", null, Array.Empty<string>()));
        var item = new DashboardLayoutItemViewModel(group, "HybridMode");

        item.ApplyState(new DashboardItemState("HybridMode", true, "On", ["On", "Off"]));

        item.IsToggleControl.Should().BeTrue();
        item.IsComboControl.Should().BeFalse();
        item.IsComboAvailable.Should().BeFalse();
        item.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HybridModeKeepsComboSemanticsWhenIGpuModesAreSupported()
    {
        var group = new DashboardGroupViewModel(
            new DashboardGroupState("Graphics", null, Array.Empty<string>()));
        var item = new DashboardLayoutItemViewModel(group, "HybridMode");

        item.ApplyState(new DashboardItemState(
            "HybridMode",
            true,
            "On",
            ["On", "OnIGPUOnly", "OnAuto", "Off"]));

        item.IsToggleControl.Should().BeFalse();
        item.IsComboControl.Should().BeTrue();
        item.IsComboAvailable.Should().BeTrue();
        item.DisplayName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void HybridModeInfoEntryOnlyAppearsForIGpuModes()
    {
        var group = new DashboardGroupViewModel(
            new DashboardGroupState("Graphics", null, Array.Empty<string>()));
        var item = new DashboardLayoutItemViewModel(group, "HybridMode");

        item.ApplyState(new DashboardItemState("HybridMode", true, "On", ["On", "Off"]));
        item.IsHybridModeInfoVisible.Should().BeFalse();

        item.ApplyState(new DashboardItemState(
            "HybridMode", true, "On", ["On", "OnIGPUOnly", "OnAuto", "Off"]));
        item.IsHybridModeInfoVisible.Should().BeTrue();
    }

    [Fact]
    public void DashboardMarkup_ExposesHybridModeInformationEntry()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DashboardPage.axaml"));

        markup.Should().Contain("AvaloniaDashboardHybridModeInfoButton");
        markup.Should().Contain("ShowHybridModeInfoCommand");
    }

    [Fact]
    public void DashboardTelemetryDetails_ExposeOnlyAvailableMetricsAndPreserveExpansionState()
    {
        var card = new DashboardTelemetryCardViewModel(
            "gpu",
            "GPU",
            "Graphics",
            "Gauge24");
        card.Update([new SensorReadingItem("GPU Usage", "50 %", "GPU", 50, "%")]);

        card.UpdateDetails(new SensorDetailsSnapshot
        {
            IsAvailable = true,
            GpuPowerWatts = 42.5,
            GpuVramUsedGb = 2,
            GpuVramTotalGb = 8,
            GpuVramUsagePercent = 25,
        });

        card.CanShowDetails.Should().BeTrue();
        card.HasDetails.Should().BeTrue();
        card.Details.Select(detail => detail.Value)
            .Should().Contain(value => value.Contains("42.5", StringComparison.Ordinal));
        card.Details.Select(detail => detail.Name)
            .Should().NotContain(string.Empty);

        card.IsDetailsExpanded = true;
        card.IsDetailsExpanded.Should().BeTrue();
    }

    [Fact]
    public void DashboardTelemetryDetails_KeepOtherCardsExpandedWhenARefreshHasNoDetails()
    {
        var cpu = new DashboardTelemetryCardViewModel(
            "cpu",
            "CPU",
            "Processor",
            "Cpu24");
        var gpu = new DashboardTelemetryCardViewModel(
            "gpu",
            "GPU",
            "Graphics",
            "Gpu24");

        cpu.Update([new SensorReadingItem("CPU Usage", "50 %", "CPU", 50, "%")]);
        gpu.Update([new SensorReadingItem("GPU Usage", "50 %", "GPU", 50, "%")]);
        cpu.IsDetailsExpanded = true;
        gpu.IsDetailsExpanded = true;

        // DashboardPageViewModel applies one service response to every card.
        // An unavailable detail response must not collapse cards as a side effect.
        cpu.UpdateDetails(SensorDetailsSnapshot.Empty);
        gpu.UpdateDetails(SensorDetailsSnapshot.Empty);

        cpu.IsDetailsExpanded.Should().BeTrue();
        gpu.IsDetailsExpanded.Should().BeTrue();
        cpu.HasDetailsStatus.Should().BeTrue();
        gpu.HasDetailsStatus.Should().BeTrue();
    }

    [Fact]
    public void DashboardTelemetryCard_ClearsStaleDetailsWhenSummaryBecomesUnavailable()
    {
        var card = new DashboardTelemetryCardViewModel(
            "cpu",
            "CPU",
            "Processor",
            "Cpu24");

        card.Update([new SensorReadingItem("CPU Usage", "50 %", "CPU", 50, "%")]);
        card.UpdateDetails(new SensorDetailsSnapshot
        {
            IsAvailable = true,
            CpuPowerWatts = 12,
        });
        card.IsDetailsExpanded = true;

        card.Update([]);

        card.IsDetailsExpanded.Should().BeFalse();
        card.Details.Should().BeEmpty();
        card.HasDetailsStatus.Should().BeFalse();
    }

    [Fact]
    public void DashboardBatteryState_PreservesWarningsStatusAndExpandedDetails()
    {
        var card = new DashboardTelemetryCardViewModel(
            "battery",
            "Battery",
            "Charge",
            "Battery024");

        card.UpdateBatteryState(new DashboardBatteryState
        {
            IsAvailable = true,
            IsCharging = true,
            IsLowBattery = true,
            PowerAdapterStatus = "ConnectedLowWattage",
            Percentage = 18,
            DischargeRateWatts = 12,
            MinDischargeRateWatts = 4,
            MaxDischargeRateWatts = 18,
            DesignCapacityWh = 80,
            ModelName = "Test battery",
        });

        card.IsAvailable.Should().BeTrue();
        card.HasPrimaryProgress.Should().BeTrue();
        card.PrimaryProgressPercent.Should().Be(18);
        card.StatusText.Should().NotBeNullOrWhiteSpace();
        card.HasWarning.Should().BeTrue();
        card.WarningText.Should().NotBeNullOrWhiteSpace();
        card.WarningText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Should().HaveCount(2);
        card.IconIdentifier.Should().Be("BatteryCharge24");

        card.UpdateDetails(new SensorDetailsSnapshot
        {
            IsAvailable = true,
            BatteryIsCharging = true,
            BatteryIsLowBattery = true,
            BatteryPowerAdapterStatus = "ConnectedLowWattage",
            BatteryPercentage = 18,
            BatteryRateWatts = 12,
            BatteryMinRateWatts = 4,
            BatteryMaxRateWatts = 18,
            BatteryDesignCapacityWh = 80,
            BatteryModelName = "Test battery",
        });

        card.Details.Select(detail => detail.Name)
            .Should().Contain(name => name.Contains("Battery", StringComparison.OrdinalIgnoreCase));
        card.Details.Select(detail => detail.Value)
            .Should().Contain(value => value.Contains("80", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnavailablePlatformServices_ShouldReturnEmptySensorDetails()
    {
        var details = await new UnavailablePlatformServices().GetSensorDetailsAsync();

        details.Should().BeSameAs(SensorDetailsSnapshot.Empty);
        details.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task UnavailablePlatformServices_ShouldReturnEmptyBatteryState()
    {
        var battery = await new UnavailablePlatformServices().GetDashboardBatteryStateAsync();

        battery.Should().BeSameAs(DashboardBatteryState.Empty);
        battery.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task UnavailablePlatformServices_ShouldRejectMacroRecordingModes()
    {
        var services = new UnavailablePlatformServices();

        (await services.StartMacroRecordingAsync(0x60, MacroRecordingMode.Keyboard)).Should().BeFalse();
        (await services.StartMacroRecordingAsync(0x60, MacroRecordingMode.KeyboardMouse)).Should().BeFalse();
        (await services.StartMacroRecordingAsync(0x60, MacroRecordingMode.KeyboardMouseMovement)).Should().BeFalse();
    }

    [Fact]
    public void MacroPage_ExposesTheWpfRecordingSourceOptions()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "MacroPage.cs"));

        source.Should().Contain("MacroSequenceControl_Keyboard");
        source.Should().Contain("MacroSequenceControl_KeyboardMouse");
        source.Should().Contain("MacroSequenceControl_KeyboardMouseMovement");
        source.Should().Contain("StartMacroRecordingAsync");
    }

    [Theory]
    [InlineData("Add24")]
    [InlineData("ArrowReset24")]
    [InlineData("ChevronDown24")]
    [InlineData("ChevronUp24")]
    [InlineData("Delete24")]
    [InlineData("Edit24")]
    [InlineData("Save24")]
    public void NavigationIcon_MapsEveryDashboardLayoutCommandIcon(string identifier)
    {
        NavigationIcon.HasGlyph(identifier).Should().BeTrue();
    }

    [Fact]
    public void DashboardTelemetryMarkup_UsesParentCommandWithoutTwoWayCommandDoubleToggle()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DashboardPage.axaml"));

        markup.Should().Contain("ToggleTelemetryDetailsCommand");
        markup.Should().Contain("IsChecked=\"{Binding IsDetailsExpanded, Mode=OneWay}\"");
        markup.Should().NotContain("ToggleDetailsCommand");
    }

    [Fact]
    public void DashboardTelemetryMarkup_UsesWpfCompatibleMultiSeriesTrendAndLegend()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DashboardPage.axaml"));

        markup.Should().Contain("SeriesSource=\"{Binding TrendSeries}\"");
        markup.Should().Contain("ItemsSource=\"{Binding TrendSeries}\"");
        markup.Should().Contain("Capacity=\"60\"");
        markup.Should().Contain("Background=\"{Binding Stroke}\"");
    }

    [Fact]
    public void SettingsCapabilityView_RefreshesNotificationEditorsAndUsesStableMultiSelectionIds()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsCapabilityView.axaml.cs"));

        source.Should().Contain("option.Key == \"DontShowNotifications\"");
        source.Should().Contain("await RefreshPageAsync();");
        source.Should().Contain("AvaloniaSettings_{_pageKey}_{option.Key}_{valueIndex++}");
    }

    [Fact]
    public void PluginHostedPage_UsesMappedIconAndLocalizedOverflowControls()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "PluginHostedPage.cs"));

        source.Should().Contain("NavigationIcon _icon");
        source.Should().Contain("NavigationIcon.HasGlyph(state.IconIdentifier)");
        source.Should().Contain("LocalizedTextBlock _title");
        source.Should().Contain("_status.MaxLines = 4");
    }

    [Fact]
    public void FeaturePageView_RollsBackRejectedToggleActions()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "FeaturePageView.axaml.cs"));

        source.Should().Contain("accepted = await _platformServices.SetFeatureActionAsync");
        source.Should().Contain("toggle.IsChecked = item.IsSelected;");
        source.Should().Contain("ToolTip.SetTip(toggle, item.Description + \" \" + item.Status)");
    }

    [Fact]
    public void SettingsPageViewModel_MapsEveryWpfSettingsCapabilityWithoutPlaceholderContent()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsPageViewModel.cs"));

        source.Should().NotContain("BuildPlaceholderView");
        source.Should().Contain("SettingsAppearanceView");
        source.Should().Contain("SettingsApplicationBehaviorView");
        source.Should().Contain("SettingsSmartKeysView");
        source.Should().Contain("SettingsDisplayView");
        source.Should().Contain("SettingsUpdateView");
        source.Should().Contain("SettingsPowerView");
        source.Should().Contain("SettingsIntegrationsView");
    }

    [Fact]
    public void AvaloniaAppearance_ProvidesWpfEquivalentCustomAccentPicker()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsAppearanceView.axaml"));
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsAppearanceView.axaml.cs"));

        markup.Should().Contain("CustomAccentColorButton");
        markup.Should().Contain("ColorView");
        markup.Should().Contain("CustomAccentColorView_ColorChanged");
        source.Should().Contain("_themePrefs.Store.UseSystemAccent = false");
        source.Should().Contain("PersistSharedAccentColorAsync");
    }

    [Fact]
    public void AvaloniaDriverDownload_PreservesWpfFilterAndSortSurface()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DriverDownloadPage.axaml"));
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DriverDownloadPage.axaml.cs"));

        markup.Should().Contain("AvaloniaDriverFilter");
        markup.Should().Contain("AvaloniaDriverSort");
        source.Should().Contain("FilterTextBox_TextChanged");
        source.Should().Contain("OrderByDescending(package => package.ReleaseDate)");
        source.Should().Contain("WindowsOptimizationPage_DriverEmpty_NoFilterResults_Message");
    }

    [Fact]
    public void AvaloniaKeyboardRgb_PreservesWpfZoneSynchronizationAction()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml.cs"));

        source.Should().Contain("SynchronizeRequested");
        source.Should().Contain("RgbZoneEditor_SynchronizeRequested");
        source.Should().Contain("zone.SetColor(color)");
        source.Should().Contain("RGBKeyboardBacklightControl_SynchroniseZones");
    }

    [Fact]
    public void WindowsPlatformServices_MapsTheWpfSensorDetailsSurface()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsPlatformServices.cs"));

        foreach (var property in new[]
                 {
                     "CpuPowerWatts",
                     "CpuCoresPowerWatts",
                     "CpuMemoryPowerWatts",
                     "CpuPlatformPowerWatts",
                     "CpuPCoreClockMHz",
                     "CpuECoreClockMHz",
                     "CpuMemoryUsagePercent",
                     "CpuMemoryTemperatureCelsius",
                     "CpuSsdTemperature1Celsius",
                     "CpuSsdTemperature2Celsius",
                     "GpuMemoryClockMHz",
                     "GpuPowerWatts",
                     "GpuVramUsedGb",
                     "GpuVramTotalGb",
                     "GpuVramUsagePercent",
                     "GpuVramTemperatureCelsius",
                     "GpuHotSpotTemperatureCelsius",
                     "GpuPcieRxBytesPerSecond",
                     "GpuPcieTxBytesPerSecond",
                     "BatteryIsCharging",
                     "BatteryIsLowBattery",
                     "BatteryMinRateWatts",
                     "BatteryMaxRateWatts",
                     "BatteryDesignCapacityWh",
                     "BatteryChargeCapacityWh",
                     "BatteryFullCapacityWh",
                     "BatteryManufactureDate",
                     "BatteryFirstUseDate",
                     "BatteryOnBatterySince",
                 })
        {
            source.Should().Contain($"{property} =");
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(101, false)]
    public void SensorReadingItem_OnlyExposesPercentValuesAsProgress(double value, bool expected)
    {
        var reading = new SensorReadingItem("CPU Usage", "value", "CPU", value, "%");

        reading.HasProgress.Should().Be(expected);
    }

    [Fact]
    public void DashboardItemStateSummary_UsesSelectedOptionForNormalCard()
    {
        var group = new DashboardGroupViewModel(
            new DashboardGroupState("Power", null, Array.Empty<string>()));
        var item = new DashboardLayoutItemViewModel(group, "PowerMode");

        item.ApplyState(new DashboardItemState(
            "PowerMode",
            true,
            "Performance",
            ["Quiet", "Performance"]));

        item.StateDisplayText.Should().Be("Performance");
    }

    [Fact]
    public void DashboardItemStateSummary_PrioritizesServiceError()
    {
        var group = new DashboardGroupViewModel(
            new DashboardGroupState("Graphics", null, Array.Empty<string>()));
        var item = new DashboardLayoutItemViewModel(group, "DiscreteGpu");

        item.ApplyState(new DashboardItemState(
            "DiscreteGpu",
            false,
            null,
            Array.Empty<string>(),
            "GPU service unavailable"));

        item.StateDisplayText.Should().Be("GPU service unavailable");
    }

    [Fact]
    public void LanguagePackService_ReportsBuiltInEnglishWithoutNetworkAccess()
    {
        var service = AvaloniaLanguagePackServiceFactory.Create();
        var english = System.Globalization.CultureInfo.GetCultureInfo("en");

        service.IsAvailable.Should().BeTrue();
        service.IsEnglish(english).Should().BeTrue();
        service.IsInstalled(english).Should().BeTrue();
    }

    [Theory]
    [InlineData("macro-record:60", 0x60UL)]
    [InlineData("MACRO-RECORD:69", 0x69UL)]
    public void MacroRecordActionKeys_TargetOnlySupportedKeyboardSlots(string actionKey, ulong expectedKey)
    {
        FeatureActionContract.TryParseMacroRecordKey(actionKey, out var key).Should().BeTrue();
        key.Should().Be(expectedKey);
    }

    [Theory]
    [InlineData("macro-record:00")]
    [InlineData("macro-record:6A")]
    [InlineData("macro-record:not-a-key")]
    [InlineData("macro-key:60")]
    public void MacroRecordActionKeys_RejectInvalidOrWrongActionPrefixes(string actionKey)
    {
        FeatureActionContract.TryParseMacroRecordKey(actionKey, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void FeatureActions_OnlyUseTogglesForReversibleOperations(bool hasRollback, bool expectedToggle)
    {
        FeatureActionContract.IsToggleAction(hasRollback).Should().Be(expectedToggle);
    }

    [Theory]
    [InlineData("Recommended", FeatureActionStatusKind.Warning)]
    [InlineData("Applied", FeatureActionStatusKind.Success)]
    [InlineData("Selected", FeatureActionStatusKind.Success)]
    [InlineData("Not supported", FeatureActionStatusKind.Critical)]
    [InlineData("Recording", FeatureActionStatusKind.Info)]
    [InlineData("Available", FeatureActionStatusKind.Neutral)]
    public void FeatureActions_ResolveStatusKindWithoutDependingOnLocalizedText(
        string status,
        FeatureActionStatusKind expected)
    {
        var action = new FeatureActionItem(
            "test",
            "Test action",
            "Test description",
            status,
            true,
            false,
            false);

        FeatureActionContract.ResolveStatusKind(action).Should().Be(expected);
    }

    [Fact]
    public void FeatureActions_ExplicitStatusKindOverridesFallbackText()
    {
        var action = new FeatureActionItem(
            "test",
            "Test action",
            "Test description",
            "Applied",
            true,
            false,
            false,
            StatusKind: FeatureActionStatusKind.Critical);

        FeatureActionContract.ResolveStatusKind(action).Should().Be(FeatureActionStatusKind.Critical);
    }

    [Theory]
    [InlineData("cleanup.browserCache", true)]
    [InlineData("cleanup.custom", true)]
    [InlineData("performance.telemetry", false)]
    [InlineData("cleanup-scan", false)]
    public void OptimizationActionKeys_ClassifyCleanupSelections(string actionKey, bool expectedCleanup)
    {
        FeatureActionContract.IsCleanupAction(actionKey).Should().Be(expectedCleanup);
    }

    [Fact]
    public void OptimizationBatchActions_UseStableKeys()
    {
        FeatureActionContract.OptimizationApplyRecommendedActionKey.Should().Be("optimization-apply-recommended");
        FeatureActionContract.CleanupScanActionKey.Should().Be("cleanup-scan");
        FeatureActionContract.CleanupRunActionKey.Should().Be("cleanup-run");
        FeatureActionContract.CleanupClearActionKey.Should().Be("cleanup-clear");
    }

    [Fact]
    public async Task PortablePlatformServices_ShouldExposeExplicitNetworkUnavailableState()
    {
        var state = await new UnavailablePlatformServices().GetNetworkAccelerationStateAsync();

        state.IsAvailable.Should().BeFalse();
        state.IsRunning.Should().BeFalse();
        state.Groups.Should().BeEmpty();
        state.Status.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PortablePlatformServices_ShouldExposeExplicitDriverUnavailableState()
    {
        var state = await new UnavailablePlatformServices().GetDriverDownloadStateAsync();

        state.IsAvailable.Should().BeFalse();
        state.Packages.Should().BeEmpty();
        state.Error.Should().NotBeNullOrWhiteSpace();
    }
}
