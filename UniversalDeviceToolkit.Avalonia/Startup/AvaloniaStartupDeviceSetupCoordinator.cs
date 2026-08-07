#if WINDOWS

using Avalonia.Controls;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.DeviceSupport;
using UniversalDeviceToolkit.Lib.ResourcesCatalog;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Startup;

/// <summary>
/// Avalonia counterpart to WPF's first-run device pack selection. It owns no
/// WPF types and uses the shared provider, catalog and persisted state format.
/// </summary>
internal sealed class AvaloniaStartupDeviceSetupCoordinator
{
    private static readonly string SetupStatePath = Path.Combine(Folders.AppData, "device-setup");
    private readonly IDeviceSupportProvider _provider;
    private readonly DevicePackManager _packManager;

    public AvaloniaStartupDeviceSetupCoordinator()
        : this(
            LenovoDeviceSupportProvider.Instance,
            new DevicePackManager(new OnlineResourceCatalogClient(new HttpClientFactory())))
    {
    }

    internal AvaloniaStartupDeviceSetupCoordinator(IDeviceSupportProvider provider, DevicePackManager packManager)
    {
        _provider = provider;
        _packManager = packManager;
    }

    public async Task RunIfNeededAsync(Window? owner)
    {
        LoadInstalledCatalog();
        LoadPreferredPackFromSetupState();
        if (IsSetupComplete())
            return;

        var machine = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        var catalog = await GetCatalogOrBuiltInAsync().ConfigureAwait(false);
        var availability = _provider.Evaluate(machine, catalog);
        var recommended = FindRecommendedPack(availability, catalog);
        var selectable = BuildSelectablePacks(catalog, machine);

        var window = await ShowWindowAsync(owner, machine, recommended, availability.IsBasicMode, selectable)
            .ConfigureAwait(false);
        if (window is null)
            return;

        var result = await window.Decision.ConfigureAwait(false);
        if (!result.Confirmed)
            return;

        try
        {
            if (!result.IsBasicMode
                && !string.IsNullOrWhiteSpace(result.DevicePackId)
                && !await IsBuiltInPackAsync(result.DevicePackId).ConfigureAwait(false)
                && !_packManager.IsInstalled(result.DevicePackId))
            {
                Dispatcher.UIThread.Post(() => window.SetInstalling(
                    Get("DeviceSetupWindow_DownloadingPack", "Downloading the device support pack...")));
                await _packManager.InstallAsync(result.DevicePackId).ConfigureAwait(false);
                LoadInstalledCatalog();
            }

            SaveSetupState(result.DevicePackId, result.IsBasicMode);
            ApplyPreferredPack(result.DevicePackId);
            Dispatcher.UIThread.Post(window.CompleteAndClose);
        }
        catch (Exception exception)
        {
            Log.Instance.WarningOnce(
                $"avalonia-device-pack-install-{result.DevicePackId}",
                $"Avalonia failed to install device pack '{result.DevicePackId}'.",
                exception);
            Dispatcher.UIThread.Post(() => window.SetFailed(Get(
                "DeviceSetupWindow_PackDownloadFailed",
                "Could not download the device support pack. Skip for now and try again next launch.")));
        }
    }

    public void LoadPreferredPackFromSetupState()
    {
        try
        {
            if (!File.Exists(SetupStatePath))
                return;

            var packId = File.ReadLines(SetupStatePath)
                .FirstOrDefault(line => line.StartsWith("devicePackId=", StringComparison.OrdinalIgnoreCase))
                ?["devicePackId=".Length..]
                .Trim();
            ApplyPreferredPack(string.IsNullOrWhiteSpace(packId) ? null : packId);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia failed to load the selected device pack.", exception);
        }
    }

    internal static IReadOnlyList<DevicePack> BuildSelectablePacks(
        DeviceSupportCatalog catalog,
        MachineInformation machine)
    {
        var all = (catalog.DevicePacks ?? [])
            .Where(pack => !string.IsNullOrWhiteSpace(pack.Id))
            .GroupBy(pack => pack.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (all.Count == 0)
            return all;

        var vendor = machine.Vendor ?? string.Empty;
        var model = machine.Model ?? string.Empty;
        var related = all.Where(pack =>
                pack.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase)
                || (pack.Vendor.Equals("LENOVO", StringComparison.OrdinalIgnoreCase)
                    && vendor.Contains("LENOVO", StringComparison.OrdinalIgnoreCase))
                || pack.VendorAliases.Any(alias => !string.IsNullOrWhiteSpace(alias)
                    && vendor.Contains(alias, StringComparison.OrdinalIgnoreCase))
                || pack.ModelKeywords.Any(keyword => !string.IsNullOrWhiteSpace(keyword)
                    && model.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var remaining = all.Except(related).ToList();
        var hardware = remaining
            .Where(IsHardwarePack)
            .Take(12);
        var basic = remaining
            .Where(pack => !IsHardwarePack(pack))
            .OrderBy(pack => pack.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(24);

        return related.Concat(hardware).Concat(basic)
            .GroupBy(pack => pack.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private async Task<AvaloniaDeviceSetupWindow?> ShowWindowAsync(
        Window? owner,
        MachineInformation machine,
        DevicePack? recommended,
        bool basicMode,
        IReadOnlyList<DevicePack> selectable)
    {
        if (owner is null)
            return null;

        var created = new TaskCompletionSource<AvaloniaDeviceSetupWindow>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            var window = new AvaloniaDeviceSetupWindow(machine, recommended, basicMode, selectable);
            var restoreOwnerEnabled = owner.IsEnabled;
            if (owner.IsVisible)
                owner.IsEnabled = false;
            window.Closed += (_, _) =>
            {
                if (owner.IsVisible)
                    owner.IsEnabled = restoreOwnerEnabled;
            };
            window.Show(owner);
            window.Activate();
            created.TrySetResult(window);
        });
        return await created.Task.ConfigureAwait(false);
    }

    private void LoadInstalledCatalog()
    {
        if (_provider is IInstalledDeviceSupportProvider installed)
            installed.SetInstalledCatalog(_packManager.GetInstalledCatalog());
    }

    private void ApplyPreferredPack(string? packId)
    {
        if (_provider is IInstalledDeviceSupportProvider installed)
            installed.SetPreferredDevicePackId(packId);
    }

    private async Task<DeviceSupportCatalog> GetCatalogOrBuiltInAsync()
    {
        try
        {
            return await _provider.GetCatalogAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia failed to load the device pack catalog.", exception);
            return new DeviceSupportCatalog();
        }
    }

    private async Task<bool> IsBuiltInPackAsync(string packId)
    {
        var catalog = await GetCatalogOrBuiltInAsync().ConfigureAwait(false);
        return (catalog.DevicePacks ?? []).Any(pack =>
            pack.Id.Equals(packId, StringComparison.OrdinalIgnoreCase));
    }

    private static DevicePack? FindRecommendedPack(DeviceFeatureAvailability availability, DeviceSupportCatalog catalog) =>
        string.IsNullOrWhiteSpace(availability.DevicePackId)
            ? null
            : (catalog.DevicePacks ?? []).FirstOrDefault(pack =>
                pack.Id.Equals(availability.DevicePackId, StringComparison.OrdinalIgnoreCase));

    private static bool IsHardwarePack(DevicePack pack) =>
        pack.EnabledFeatures.Any(feature =>
            feature.Equals("lenovo-hardware-controls", StringComparison.OrdinalIgnoreCase));

    private static bool IsSetupComplete()
    {
        try { return File.Exists(SetupStatePath); }
        catch { return false; }
    }

    private static void SaveSetupState(string? packId, bool basicMode)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SetupStatePath)!);
            File.WriteAllLines(SetupStatePath,
            [
                $"devicePackId={packId ?? string.Empty}",
                $"basicMode={basicMode}",
                $"confirmedAtUtc={DateTimeOffset.UtcNow:O}",
            ]);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia failed to save device setup state.", exception);
        }
    }

    private static string Get(string key, string fallback) =>
        Localization.AvaloniaLocalization.GetString(key, fallback);
}

#endif
