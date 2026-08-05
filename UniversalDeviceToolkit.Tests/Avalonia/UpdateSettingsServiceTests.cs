#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class UpdateSettingsServiceTests
{
    [Fact]
    public async Task UpdatePage_UsesLocalizedFrequencyAndDefaultRepositoryValues()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var before = await service.GetPageAsync("Update");
        var frequency = before.Options.Single(option => option.Key == "UpdateFrequency");
        var owner = before.Options.Single(option => option.Key == "RepositoryOwner");
        var name = before.Options.Single(option => option.Key == "RepositoryName");

        frequency.Values.Should().Contain(UpdateCheckFrequency.PerDay.GetDisplayName());
        frequency.SelectedValue.Should().NotBe(nameof(UpdateCheckFrequency.PerDay));

        try
        {
            await service.SetTextAsync("Update", "RepositoryOwner", null);
            await service.SetTextAsync("Update", "RepositoryName", null);

            var defaults = await service.GetPageAsync("Update");
            defaults.Options.Single(option => option.Key == "RepositoryOwner")
                .TextValue.Should().Be(AppIdentity.RepositoryOwner);
            defaults.Options.Single(option => option.Key == "RepositoryName")
                .TextValue.Should().Be(AppIdentity.RepositoryName);
        }
        finally
        {
            await service.SetTextAsync(
                "Update",
                "RepositoryOwner",
                string.Equals(owner.TextValue, AppIdentity.RepositoryOwner, StringComparison.Ordinal)
                    ? null
                    : owner.TextValue);
            await service.SetTextAsync(
                "Update",
                "RepositoryName",
                string.Equals(name.TextValue, AppIdentity.RepositoryName, StringComparison.Ordinal)
                    ? null
                    : name.TextValue);
        }
    }
}

#endif
