using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Lifecycle;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.CLI;
using Xunit;
using AutomationResource = UniversalDeviceToolkit.Lib.Automation.Resources.Resource;
using WpfResource = UniversalDeviceToolkit.WPF.Resources.Resource;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaMigrationContractTests
{
    [Fact]
    public void SharedIpcServer_ImplementsSharedCliLifecycleContract()
    {
        typeof(IpcServer).GetInterfaces().Should().Contain(typeof(ICliHostLifecycle));
    }

    [Fact]
    public void SharedIpcServer_DoesNotReferenceWpfHostAssembly()
    {
        typeof(IpcServer).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Should()
            .NotContain("Universal Device Toolkit");
    }

    [Fact]
    public void SharedIpcFeatureErrors_PreserveAllWpfTranslations()
    {
        var keys = new[]
        {
            "FeatureRegistration_NotSupported",
            "FeatureRegistration_NullReturnValue",
            "FeatureRegistration_StateNotSupported",
        };

        foreach (var culture in LocalizationCatalog.SupportedCultures)
        {
            foreach (var key in keys)
            {
                var expected = WpfResource.ResourceManager.GetString(key, culture);
                expected.Should().NotBeNullOrWhiteSpace($"WPF must provide {key} for {culture.Name}");

                AutomationResource.ResourceManager.GetString(key, culture)
                    .Should().Be(expected, $"the shared IPC host must preserve {key} for {culture.Name}");
            }
        }
    }

    [Fact]
    public void AvaloniaPluginHostContext_ExposesRealRuntimeCapabilities()
    {
        var context = new AvaloniaPluginHostContext(() => null);

        context.Mode.Should().Be(UniversalDeviceToolkit.Lib.Plugins.PluginHostMode.RealRuntime);
        context.AllowSystemActions.Should().BeTrue();
        context.OwnerWindow.Should().BeNull();
        context.OpenPluginSettings(string.Empty).Should().BeFalse();
        context.ShowDialog(null!).Should().BeNull();
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

    [Theory]
    [InlineData("Balance", true, true)]
    [InlineData("Performance", true, false)]
    [InlineData("Balance", false, false)]
    public void DashboardPowerModeSettingsEntry_MatchesWpfAvailability(
        string state,
        bool settingsAvailable,
        bool expectedVisible)
    {
        var group = new DashboardGroupViewModel(
            new DashboardGroupState("Power", null, Array.Empty<string>()));
        var item = new DashboardLayoutItemViewModel(group, "PowerMode");

        item.ApplyState(new DashboardItemState(
            "PowerMode",
            true,
            state,
            ["Balance", "Performance", "GodMode"]));
        item.SetPowerModeSettingsAvailable(settingsAvailable);

        item.IsPowerModeSettingsVisible.Should().Be(expectedVisible);
    }

    [Fact]
    public async Task UnavailablePlatform_DoesNotExposeBalanceModeSettingsAction()
    {
        var services = new UnavailablePlatformServices();

        var state = await services.GetBalanceModeSettingsAsync();

        state.IsAvailable.Should().BeFalse();
        state.ErrorMessage.Should().NotBeNullOrWhiteSpace();
        (await services.SaveBalanceModeSettingsAsync(true)).Should().BeFalse();
    }

    [Fact]
    public void DashboardPowerModeSettingsEntry_UsesGodModeCapabilityForPerformanceStates()
    {
        var group = new DashboardGroupViewModel(
            new DashboardGroupState("Power", null, Array.Empty<string>()));
        var item = new DashboardLayoutItemViewModel(group, "PowerMode");

        item.ApplyState(new DashboardItemState(
            "PowerMode",
            true,
            "GodMode",
            ["Balance", "Performance", "GodMode"]));
        item.SetPowerModeSettingsAvailable(false);
        item.SetGodModeSettingsAvailable(true);

        item.IsPowerModeSettingsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task UnavailablePlatform_DoesNotExposeGodModeSettingsAction()
    {
        var services = new UnavailablePlatformServices();

        var state = await services.GetGodModeSettingsAsync();

        state.IsAvailable.Should().BeFalse();
        state.Presets.Should().BeEmpty();
        (await services.SaveGodModeSettingsAsync(new GodModeSettingsUpdate(
            Guid.Empty,
            new Dictionary<string, int>(),
            null,
            null,
            null))).Should().BeFalse();
    }

    [Fact]
    public async Task GodModeFanCurve_DefaultRestoreRemainsReachableInAvalonia()
    {
        var services = new UnavailablePlatformServices();

        (await services.GetDefaultGodModeFanCurveAsync()).Should().BeNull();

        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "GodModeSettingsWindow.cs"));

        source.Should().Contain("GodModeDefaultFanCurveButton");
        source.Should().Contain("GetDefaultGodModeFanCurveAsync");
    }

    [Fact]
    public void DashboardItemPicker_FiltersExistingItemsAcrossGroupsAndPreservesWhiteBacklightPersistence()
    {
        var services = new UnavailablePlatformServices();
        var viewModel = new DashboardPageViewModel(services);
        var existingGroup = new DashboardGroupViewModel(
            new DashboardGroupState("Power", null, ["PowerMode"]));
        var targetGroup = new DashboardGroupViewModel(
            new DashboardGroupState("Custom", "Custom", Array.Empty<string>()));
        viewModel.DashboardGroups.Add(existingGroup);
        viewModel.DashboardGroups.Add(targetGroup);

        viewModel.ToggleDashboardItemPickerCommand.Execute(targetGroup);

        viewModel.AvailableDashboardItems
            .Select(item => item.Identifier)
            .Should()
            .NotContain("PowerMode");

        var whiteBacklight = viewModel.AvailableDashboardItems
            .Single(item => item.Identifier.Equals(
                "WhiteKeyboardBacklight",
                StringComparison.OrdinalIgnoreCase));
        viewModel.AddDashboardItemCommand.Execute(whiteBacklight);

        targetGroup.Items.Select(item => item.Identifier)
            .Should()
            .Equal(
                "WhiteKeyboardBacklight",
                DashboardGroupViewModel.OneLevelWhiteKeyboardBacklightIdentifier);
        targetGroup.ToState().Items.Should().Equal("WhiteKeyboardBacklight");
        viewModel.AvailableDashboardItems
            .Select(item => item.Identifier)
            .Should()
            .NotContain("WhiteKeyboardBacklight");
    }

    [Fact]
    public void DashboardLayoutMarkup_ExposesInlineAddItemPicker()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DashboardPage.axaml"));

        markup.Should().Contain("AvaloniaDashboardAddItemButton");
        markup.Should().Contain("ToggleDashboardItemPickerCommand");
        markup.Should().Contain("AddDashboardItemCommand");
        markup.Should().Contain("AvailableDashboardItems");
    }

    [Fact]
    public void DashboardLayoutMarkup_ExposesBalanceModeSettingsEntry()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "DashboardPage.axaml"));

        markup.Should().Contain("IsPowerModeSettingsVisible");
        markup.Should().Contain("ShowPowerModeSettingsCommand");
        markup.Should().Contain("AvaloniaPowerModeSettingsButton");

        var windowSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "GodModeSettingsWindow.cs"));
        windowSource.Should().Contain("GodModePresetComboBox");
        windowSource.Should().Contain("GodModeFanFullSpeedToggle");
        windowSource.Should().Contain("GodModeSaveAndCloseButton");
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
    public void DeviceInformationWindow_AndShellEntryRemainReachable()
    {
        typeof(DeviceInformationWindow).Should().BeAssignableTo<global::Avalonia.Controls.Window>();

        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "MainWindow.axaml"));

        markup.Should().Contain("AvaloniaDeviceInfoButton");
        markup.Should().Contain("DeviceInfoButton_Click");
        markup.Should().Contain("DeviceInformationWindow_Device_Title");
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
    [InlineData("ArrowClockwise24")]
    [InlineData("ArrowExportLtr24")]
    [InlineData("ArrowImport24")]
    [InlineData("ArrowRepeatAll24")]
    [InlineData("ArrowReset24")]
    [InlineData("ChevronDown24")]
    [InlineData("ChevronUp24")]
    [InlineData("Delete24")]
    [InlineData("Edit24")]
    [InlineData("Save24")]
    [InlineData("ToggleRight24")]
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
    public void WindowsOptimizationActionDetails_PreserveWpfDoubleClickSurface()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "FeaturePageView.axaml.cs"));

        source.Should().Contain("args.ClickCount");
        source.Should().Contain("new ActionDetailsWindow(item)");

        var cleanup = AvaloniaActionDetailsCatalog.Get("cleanup.tempFiles");
        cleanup.ImplementationType.Should().NotBeNullOrWhiteSpace();
        cleanup.Details.Should().Contain(detail => detail.Contains("SystemRoot", StringComparison.Ordinal));

        var unknown = AvaloniaActionDetailsCatalog.Get("plugin.custom-action");
        unknown.ImplementationType.Should().NotBeNullOrWhiteSpace();
        unknown.Details.Should().NotBeEmpty();
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
    public void AvaloniaAppearance_FontSelectorIncludesEverySharedFontStyle()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsAppearanceView.axaml"));
        var selectorStart = markup.IndexOf(
            "<ComboBox x:Name=\"FontComboBox\"",
            StringComparison.Ordinal);
        selectorStart.Should().BeGreaterThanOrEqualTo(0);
        var selectorEnd = markup.IndexOf("</ComboBox>", selectorStart, StringComparison.Ordinal);
        selectorEnd.Should().BeGreaterThan(selectorStart);
        var selector = markup[selectorStart..selectorEnd];

        var expectedLabels = new[]
        {
            "Default",
            "Segoe UI Variable",
            "Microsoft YaHei UI",
            "DengXian",
            "Noto Sans CJK SC",
            "SimHei",
            "SimSun",
            "KaiTi",
        };

        Enum.GetValues<AppFontStyle>().Should().HaveCount(expectedLabels.Length);
        foreach (var label in expectedLabels)
            selector.Should().Contain($"Tag=\"{label}\"");
    }

    [Fact]
    public void AvaloniaOsdSettingsWindow_UsesApplicationContractAndGroupedSaveSurface()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "OsdSettingsWindow.cs"));

        source.Should().Contain("GetPageAsync(\"Application\")");
        source.Should().Contain("GeneralKeys");
        source.Should().Contain("AppearanceKeys");
        source.Should().Contain("SensorItemKeys");
        source.Should().Contain("ThresholdKeys");
        source.Should().Contain("SetToggleAsync(\"Application\"");
        source.Should().Contain("SetSelectionAsync(\"Application\"");
        source.Should().Contain("SetTextAsync(\"Application\"");
        source.Should().Contain("SetMultiSelectionAsync(");
        source.Should().Contain("InvokeActionAsync(\"Application\"");
        source.Should().Contain("AvaloniaOsdSettingsSaveButton");
        source.Should().Contain("AvaloniaOsdSettingsCloseButton");
        source.Should().Contain("Close(true)");
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
    public void AvaloniaKeyboardSpectrum_PreservesWpfAddAndDeleteEffectActions()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml"));
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml.cs"));

        markup.Should().Contain("AvaloniaSpectrumAddEffect");
        source.Should().Contain("AddSpectrumEffect_Click");
        source.Should().Contain("RemoveSpectrumEffect_Click");
        source.Should().Contain("SpectrumEffects: _spectrumEditors.Select");
    }

    [Fact]
    public void AvaloniaKeyboardSpectrum_PreservesWpfProfileImportExportAndResetActions()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml"));
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml.cs"));
        var service = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        markup.Should().Contain("AvaloniaSpectrumReset");
        markup.Should().Contain("AvaloniaSpectrumExport");
        markup.Should().Contain("AvaloniaSpectrumImport");
        source.Should().Contain("ResetSpectrum_Click");
        source.Should().Contain("ExportSpectrum_Click");
        source.Should().Contain("ImportSpectrum_Click");
        service.Should().Contain("ExportProfileDescriptionAsync");
        service.Should().Contain("ImportProfileDescription");
        service.Should().Contain("SetProfileDefaultAsync");
    }

    [Fact]
    public void AvaloniaKeyboardSpectrum_PreservesWpfKeyboardLayoutSwitch()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml"));
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml.cs"));
        var service = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        markup.Should().Contain("AvaloniaSpectrumSwitchLayout");
        source.Should().Contain("SwitchSpectrumLayout_Click");
        source.Should().Contain("KeyboardLayout: next");
        service.Should().Contain("GetKeyboardLayoutAsync");
        service.Should().Contain("_spectrumSettings.Store.KeyboardLayout");
    }

    [Fact]
    public void AvaloniaKeyboardSpectrum_PreservesWpfEffectKeySelection()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "KeyboardBacklightPage.axaml.cs"));

        source.Should().Contain("ToggleButton");
        source.Should().Contain("SetKey");
        source.Should().Contain("SetKeys(availableKeys)");
        source.Should().Contain("Keys.OrderBy(key => key).ToArray()");
        source.Should().Contain("_state.KeyboardKeys?.ToArray() ?? []");
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
