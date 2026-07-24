using System.Text.RegularExpressions;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.DeviceSupport;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

/// <summary>
/// Keeps the installer's device-pack snapshot (Tools/Installer/DevicePackSnapshot.cs)
/// in sync with the app's built-in catalog. When this fails after catalog changes,
/// regenerate the snapshot by dumping LenovoDeviceSupportProvider.Instance.GetCatalogAsync()
/// (see Tools/Installer history for the generator).
/// </summary>
public class DevicePackSnapshotGuardTests
{
    [Fact]
    public async Task InstallerSnapshot_ShouldCoverEveryAppCatalogPack()
    {
        var catalog = await LenovoDeviceSupportProvider.Instance.GetCatalogAsync();
        var appIds = catalog.DevicePacks
            .Select(p => p.Id)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var snapshot = ReadSnapshotSource();
        var snapshotIds = Regex.Matches(snapshot, @"new\(\s*""(?<id>[a-z0-9-]+)"",\s*""", RegexOptions.IgnoreCase)
            .Select(m => m.Groups["id"].Value)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        snapshotIds.Should().BeEquivalentTo(appIds,
            because: "the installer device picker must offer the same packs as the app catalog");

        snapshot.Should().Contain(
            $"GenericBasicPackId = \"{CatalogDeviceSupportProvider.GenericBasicPackId}\"");
    }

    [Fact]
    public async Task InstallerSnapshot_HardwareFlags_ShouldMatchAppCatalog()
    {
        var catalog = await LenovoDeviceSupportProvider.Instance.GetCatalogAsync();
        var snapshot = ReadSnapshotSource();

        foreach (var pack in catalog.DevicePacks)
        {
            var isHardware = pack.EnabledFeatures.Contains("lenovo-hardware-controls", StringComparer.OrdinalIgnoreCase);
            var line = Regex.Match(snapshot, $@"new\(\s*""{Regex.Escape(pack.Id)}"",[^\n]+");
            line.Success.Should().BeTrue($"snapshot is missing pack '{pack.Id}'");
            line.Value.Should().EndWith(isHardware ? "true)," : "false),",
                because: $"pack '{pack.Id}' hardware flag must match the app catalog");
        }
    }

    private static string ReadSnapshotSource()
    {
        var repositoryRoot = RepositoryPaths.FindRoot();
        return File.ReadAllText(Path.Combine(repositoryRoot, "Tools", "Installer", "DevicePackSnapshot.cs"));
    }
}
