using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.DeviceSupport;
using UniversalDeviceToolkit.Lib.ResourcesCatalog;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Windows.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

public sealed class StartupDeviceSetupCoordinator
{
    private static readonly string SetupStatePath = Path.Combine(Folders.AppData, "device-setup");

    private readonly IDeviceSupportProvider _deviceSupportProvider;
    private readonly DevicePackManager _devicePackManager;
    private readonly Func<MachineInformation, DevicePack?, bool, IReadOnlyList<DevicePack>, DeviceSetupWindow> _createWindow;
    private readonly Func<bool> _isSetupComplete;
    private readonly Action<string?, bool> _saveSetupState;

    public StartupDeviceSetupCoordinator(IDeviceSupportProvider deviceSupportProvider, DevicePackManager devicePackManager)
        : this(deviceSupportProvider, devicePackManager, CreateWindow, IsSetupComplete, SaveSetupState)
    {
    }

    internal StartupDeviceSetupCoordinator(
        IDeviceSupportProvider deviceSupportProvider,
        DevicePackManager devicePackManager,
        Func<MachineInformation, DevicePack?, bool, IReadOnlyList<DevicePack>, DeviceSetupWindow> createWindow,
        Func<bool> isSetupComplete,
        Action<string?, bool> saveSetupState)
    {
        _deviceSupportProvider = deviceSupportProvider;
        _devicePackManager = devicePackManager;
        _createWindow = createWindow;
        _isSetupComplete = isSetupComplete;
        _saveSetupState = saveSetupState;
    }

    public static StartupDeviceSetupCoordinator CreateDefault() =>
        CreateDefault(new HttpClientFactory());

    public static StartupDeviceSetupCoordinator CreateDefault(HttpClientFactory httpClientFactory) =>
        new(
            LenovoDeviceSupportProvider.Instance,
            new DevicePackManager(new OnlineResourceCatalogClient(httpClientFactory)));

    public async Task RunIfNeededAsync(MachineInformation machineInformation)
    {
        if (System.Windows.Application.Current?.Dispatcher is not null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            // InvokeAsync returns the inner Task; await it so setup actually completes on the UI thread.
            var uiTask = await System.Windows.Application.Current.Dispatcher.InvokeAsync(
                () => RunIfNeededAsync(machineInformation));
            await uiTask.ConfigureAwait(false);
            return;
        }
        LoadInstalledCatalog();
        LoadPreferredPackFromSetupState();

        if (_isSetupComplete())
            return;

        var catalog = await GetCatalogOrBuiltInAsync();
        var recommendedPack = FindRecommendedPack(machineInformation, catalog, _deviceSupportProvider);
        var availability = _deviceSupportProvider.Evaluate(machineInformation, catalog);
        var selectablePacks = BuildSelectablePacks(catalog, machineInformation);

        var window = _createWindow(machineInformation, recommendedPack, availability.IsBasicMode, selectablePacks);
        LocalizationHelper.ApplyStartupTheme(window);

        // Never leave MainWindow interactive under the setup dialog.
        // Prefer showing setup while MainWindow is still hidden (startup order).
        var mainWindow = System.Windows.Application.Current?.MainWindow;
        var restoredMainEnabled = true;
        var disabledMain = false;
        if (mainWindow is not null
            && !ReferenceEquals(mainWindow, window)
            && mainWindow.IsVisible)
        {
            window.Owner = mainWindow;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            restoredMainEnabled = mainWindow.IsEnabled;
            mainWindow.IsEnabled = false;
            disabledMain = true;
        }
        else
        {
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        try
        {
            window.Show();
            window.Activate();
            _ = window.Focus();
            var result = await window.ShouldContinue.ConfigureAwait(true);

            if (!result.Confirmed)
                return;

            // Prefer the user's pick for basic vs hardware over auto-detect alone.
            _saveSetupState(result.DevicePackId, result.IsBasicMode);
            ApplyPreferredPack(result.DevicePackId);
            result.Window?.CompleteAndClose();
        }
        finally
        {
            if (disabledMain && mainWindow is not null)
                mainWindow.IsEnabled = restoredMainEnabled;
        }
    }

    /// <summary>Apply saved device-setup pack so feature gates honor user confirmation every launch.</summary>
    public void LoadPreferredPackFromSetupState()
    {
        try
        {
            if (!File.Exists(SetupStatePath))
                return;

            string? packId = null;
            foreach (var line in File.ReadAllLines(SetupStatePath))
            {
                if (line.StartsWith("devicePackId=", StringComparison.OrdinalIgnoreCase))
                    packId = line["devicePackId=".Length..].Trim();
            }

            ApplyPreferredPack(string.IsNullOrWhiteSpace(packId) ? null : packId);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to load preferred device pack from setup state.", ex);
        }
    }

    private void ApplyPreferredPack(string? devicePackId)
    {
        if (_deviceSupportProvider is IInstalledDeviceSupportProvider installed)
            installed.SetPreferredDevicePackId(devicePackId);
    }

    /// <summary>
    /// Prefer packs that match this vendor / model family so the combo is usable,
    /// then append remaining catalog packs (hardware first).
    /// </summary>
    internal static IReadOnlyList<DevicePack> BuildSelectablePacks(
        DeviceSupportCatalog catalog,
        MachineInformation machineInformation)
    {
        var all = (catalog.DevicePacks ?? [])
            .Where(p => p is not null && !string.IsNullOrWhiteSpace(p.Id))
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (all.Count == 0)
            return all;

        var vendor = machineInformation.Vendor ?? string.Empty;
        var model = machineInformation.Model ?? string.Empty;

        bool VendorRelated(DevicePack pack)
        {
            if (string.IsNullOrWhiteSpace(pack.Vendor) || pack.Vendor == "*")
                return false;
            if (pack.Vendor.Equals(vendor, StringComparison.OrdinalIgnoreCase))
                return true;
            if (pack.Vendor.Equals("LENOVO", StringComparison.OrdinalIgnoreCase) &&
                vendor.Contains("LENOVO", StringComparison.OrdinalIgnoreCase))
                return true;
            return pack.VendorAliases.Any(a =>
                !string.IsNullOrWhiteSpace(a) &&
                (vendor.Contains(a, StringComparison.OrdinalIgnoreCase) ||
                 a.Contains(vendor, StringComparison.OrdinalIgnoreCase)));
        }

        bool ModelRelated(DevicePack pack) =>
            pack.ModelKeywords.Any(k =>
                !string.IsNullOrWhiteSpace(k) &&
                model.Contains(k, StringComparison.OrdinalIgnoreCase));

        var related = all.Where(p => VendorRelated(p) || ModelRelated(p)).ToList();
        var rest = all.Except(related).ToList();

        // Cap list size for usability: related first, then top hardware + popular basic.
        var hardwareRest = rest
            .Where(p => p.EnabledFeatures.Contains("lenovo-hardware-controls", StringComparer.OrdinalIgnoreCase))
            .Take(12)
            .ToList();
        var basicRest = rest
            .Where(p => !p.EnabledFeatures.Contains("lenovo-hardware-controls", StringComparer.OrdinalIgnoreCase))
            .OrderBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Take(24)
            .ToList();

        return related
            .Concat(hardwareRest)
            .Concat(basicRest)
            .GroupBy(p => p.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private void LoadInstalledCatalog()
    {
        if (_deviceSupportProvider is not IInstalledDeviceSupportProvider installedDeviceSupportProvider)
            return;

        installedDeviceSupportProvider.SetInstalledCatalog(_devicePackManager.GetInstalledCatalog());
    }

    private static DeviceSetupWindow CreateWindow(
        MachineInformation machineInformation,
        DevicePack? recommendedPack,
        bool isBasicMode,
        IReadOnlyList<DevicePack> selectablePacks) =>
        new(machineInformation, recommendedPack, isBasicMode, selectablePacks);

    private async Task<DeviceSupportCatalog> GetCatalogOrBuiltInAsync()
    {
        try
        {
            return await _deviceSupportProvider.GetCatalogAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to load built-in device support catalog.", ex);

            return new DeviceSupportCatalog();
        }
    }

    private static DevicePack? FindRecommendedPack(
        MachineInformation machineInformation,
        DeviceSupportCatalog catalog,
        IDeviceSupportProvider deviceSupportProvider)
    {
        var availability = deviceSupportProvider.Evaluate(machineInformation, catalog);
        if (string.IsNullOrWhiteSpace(availability.DevicePackId))
            return null;

        return catalog.DevicePacks.FirstOrDefault(pack =>
            pack.Id.Equals(availability.DevicePackId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSetupComplete()
    {
        try
        {
            return File.Exists(SetupStatePath);
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "device-setup-complete-check",
                "Failed to check device-setup completion file; treating as incomplete.",
                ex);
            return false;
        }
    }

    private static void SaveSetupState(string? devicePackId, bool isBasicMode)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SetupStatePath)!);
            File.WriteAllLines(SetupStatePath,
            [
                $"devicePackId={devicePackId ?? string.Empty}",
                $"basicMode={isBasicMode}",
                $"confirmedAtUtc={DateTimeOffset.UtcNow:O}"
            ]);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to save startup device setup state.", ex);
        }
    }
}
