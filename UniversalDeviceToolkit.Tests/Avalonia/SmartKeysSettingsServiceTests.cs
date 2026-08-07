#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class SmartKeysSettingsServiceTests
{
    [Fact]
    public async Task SmartKeysPage_ExposesFnLockAndBothPipelineEditors()
    {
        var service = AvaloniaSettingsServiceFactory.Create();

        var page = await service.GetPageAsync("SmartKeys");
        var localizedCombination = (ModifierKey.Alt | ModifierKey.Ctrl | ModifierKey.Shift)
            .GetFlagsDisplayName(ModifierKey.None);

        page.IsAvailable.Should().BeTrue();
        page.Options.Should().ContainSingle(option =>
            option.Key == "SmartFnLockFlags"
            && option.Editor == AvaloniaSettingEditor.Selection
            && option.Values!.Contains("Off")
            && option.Values!.Contains("Alt")
            && option.Values!.Contains(localizedCombination)
            && !option.Values!.Contains("Alt + Ctrl + Shift"));
        page.Options.Should().Contain(option =>
            option.Key == "SmartKeySinglePressActions"
            && option.Editor == AvaloniaSettingEditor.MultiSelection);
        page.Options.Should().Contain(option =>
            option.Key == "SmartKeyDoublePressActions"
            && option.Editor == AvaloniaSettingEditor.MultiSelection);
    }

    [Fact]
    public async Task SmartFnLockFlags_SelectionRoundTripsThroughSettingsStore()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var pageBefore = await service.GetPageAsync("SmartKeys");
        var original = pageBefore.Options.Single(option => option.Key == "SmartFnLockFlags").SelectedValue;
        var combination = pageBefore.Options
            .Single(option => option.Key == "SmartFnLockFlags")
            .Values!
            .Single(value => value.Equals(
                (ModifierKey.Alt | ModifierKey.Ctrl | ModifierKey.Shift)
                    .GetFlagsDisplayName(ModifierKey.None),
                StringComparison.OrdinalIgnoreCase));

        try
        {
            await service.SetSelectionAsync("SmartKeys", "SmartFnLockFlags", combination);

            var pageAfter = await service.GetPageAsync("SmartKeys");
            pageAfter.Options.Single(option => option.Key == "SmartFnLockFlags")
                .SelectedValue.Should().Be(combination);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(original))
                await service.SetSelectionAsync("SmartKeys", "SmartFnLockFlags", original);
        }
    }

    [Fact]
    public async Task SmartKeyPipelineSelection_ThisAppClearsStoredManualActions()
    {
        var service = AvaloniaSettingsServiceFactory.Create();

        await service.SetMultiSelectionAsync(
            "SmartKeys",
            "SmartKeySinglePressActions",
            ["This app"]);
        await service.SetMultiSelectionAsync(
            "SmartKeys",
            "SmartKeyDoublePressActions",
            ["This app"]);

        var page = await service.GetPageAsync("SmartKeys");
        page.Options.Single(option => option.Key == "SmartKeySinglePressActions")
            .SelectedValues!.Should().ContainSingle().Which.Should().Be("This app");
        page.Options.Single(option => option.Key == "SmartKeyDoublePressActions")
            .SelectedValues!.Should().ContainSingle().Which.Should().Be("This app");
    }

    [Fact]
    public async Task SmartKeyPipelineSelection_ThisAppClearsStoredActionIdsAndLists()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var store = WindowsAvaloniaSettingsService.SharedApplicationSettings.Store;

        await service.SetMultiSelectionAsync(
            "SmartKeys",
            "SmartKeySinglePressActions",
            ["This app"]);
        await service.SetMultiSelectionAsync(
            "SmartKeys",
            "SmartKeyDoublePressActions",
            ["This app"]);

        store.SmartKeySinglePressActionId.Should().BeNull();
        store.SmartKeySinglePressActionList.Should().BeEmpty();
        store.SmartKeyDoublePressActionId.Should().BeNull();
        store.SmartKeyDoublePressActionList.Should().BeEmpty();
    }

    [Fact]
    public async Task SmartKeyPipelineSelection_DoublePressSelectionIsIndependentOfSinglePress()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var store = WindowsAvaloniaSettingsService.SharedApplicationSettings.Store;
        var previousSingleList = store.SmartKeySinglePressActionList.ToList();
        var previousSingleId = store.SmartKeySinglePressActionId;
        var previousDoubleList = store.SmartKeyDoublePressActionList.ToList();
        var previousDoubleId = store.SmartKeyDoublePressActionId;

        try
        {
            await service.SetMultiSelectionAsync(
                "SmartKeys",
                "SmartKeyDoublePressActions",
                ["This app"]);
            await service.SetMultiSelectionAsync(
                "SmartKeys",
                "SmartKeySinglePressActions",
                ["This app"]);

            store.SmartKeySinglePressActionId.Should().BeNull();
            store.SmartKeySinglePressActionList.Should().BeEmpty();
            store.SmartKeyDoublePressActionId.Should().BeNull();
            store.SmartKeyDoublePressActionList.Should().BeEmpty();

            await service.SetMultiSelectionAsync(
                "SmartKeys",
                "SmartKeySinglePressActions",
                ["This app"]);

            store.SmartKeySinglePressActionId.Should().BeNull();
            store.SmartKeySinglePressActionList.Should().BeEmpty();
        }
        finally
        {
            store.SmartKeySinglePressActionList.Clear();
            store.SmartKeySinglePressActionList.AddRange(previousSingleList);
            store.SmartKeySinglePressActionId = previousSingleId;
            store.SmartKeyDoublePressActionList.Clear();
            store.SmartKeyDoublePressActionList.AddRange(previousDoubleList);
            store.SmartKeyDoublePressActionId = previousDoubleId;
            WindowsAvaloniaSettingsService.SharedApplicationSettings.SynchronizeStore();
        }
    }
}

#endif
