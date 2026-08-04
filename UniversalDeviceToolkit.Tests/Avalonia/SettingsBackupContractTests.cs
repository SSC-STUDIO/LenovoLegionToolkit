#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class SettingsBackupContractTests
{
    [Fact]
    public async Task ApplicationPage_ExposesPortableBackupActions()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var page = await service.GetPageAsync("Application");

        page.Options.Should().Contain(option =>
            option.Key == "ExportSettings"
            && option.Editor == AvaloniaSettingEditor.Action
            && option.IsEnabled);
        page.Options.Should().Contain(option =>
            option.Key == "ImportSettings"
            && option.Editor == AvaloniaSettingEditor.Action
            && option.IsEnabled);
    }
}

#endif
