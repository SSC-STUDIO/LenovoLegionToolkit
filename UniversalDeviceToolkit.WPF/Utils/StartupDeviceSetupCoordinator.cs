using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.DeviceSupport;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Windows.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

public sealed class StartupDeviceSetupCoordinator
{
    private static readonly string SetupStatePath = Path.Combine(Folders.AppData, "device-setup");

    private readonly IDeviceSupportProvider _deviceSupportProvider;
    private readonly DevicePackManager _devicePackManager;
    private readonly Func<MachineInformation, DevicePack?, bool, DeviceSetupWindow> _createWindow;
    private readonly Func<bool> _isSetupComplete;
    private readonly Action<string?, bool> _saveSetupState;

    public StartupDeviceSetupCoordinator(IDeviceSupportProvider deviceSupportProvider, DevicePackManager devicePackManager)
        : this(deviceSupportProvider, devicePackManager, CreateWindow, IsSetupComplete, SaveSetupState)
    {
    }

    internal StartupDeviceSetupCoordinator(
        IDeviceSupportProvider deviceSupportProvider,
        DevicePackManager devicePackManager,
        Func<MachineInformation, DevicePack?, bool, DeviceSetupWindow> createWindow,
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
        LoadInstalledCatalog();

        if (_isSetupComplete())
            return;

        var catalog = await GetCatalogOrBuiltInAsync();
        var recommendedPack = FindRecommendedPack(machineInformation, catalog, _deviceSupportProvider);
        var availability = _deviceSupportProvider.Evaluate(machineInformation, catalog);

        var window = _createWindow(machineInformation, recommendedPack, availability.IsBasicMode);
        LocalizationHelper.ApplyStartupTheme(window);
        window.Show();
        var result = await window.ShouldContinue;

        if (!result.Confirmed)
            return;

        _saveSetupState(result.DevicePackId, availability.IsBasicMode);
        result.Window?.CompleteAndClose();
    }

    private void LoadInstalledCatalog()
    {
        if (_deviceSupportProvider is not IInstalledDeviceSupportProvider installedDeviceSupportProvider)
            return;

        installedDeviceSupportProvider.SetInstalledCatalog(_devicePackManager.GetInstalledCatalog());
    }

    private static DeviceSetupWindow CreateWindow(MachineInformation machineInformation, DevicePack? recommendedPack, bool isBasicMode) =>
        new(machineInformation, recommendedPack, isBasicMode);

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
        catch
        {
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
