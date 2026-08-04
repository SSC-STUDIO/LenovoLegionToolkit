#if WINDOWS

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Windows implementation backed by UniversalDeviceToolkit.Lib real services.
/// Falls back to sample data when the platform cannot provide telemetry
/// (e.g. the device is not a supported Lenovo machine).
/// </summary>
public sealed class WindowsPlatformServices : IPlatformServices
{
    private static readonly object InitializeLock = new();
    private static bool _initialized;
    private static bool _available;

    public static IPlatformServices Create() => CreateInternal();

    private static IPlatformServices CreateInternal()
    {
        lock (InitializeLock)
        {
            if (!_initialized)
            {
                try
                {
                    InitializeIoC();
                    _available = true;
                }
                catch
                {
                    _available = false;
                }
                _initialized = true;
            }

            return _available ? new WindowsPlatformServices() : new SamplePlatformServices();
        }
    }

    private static void InitializeIoC()
    {
        // IoCContainer.Initialize throws if already initialized; treat "already" as success.
        try
        {
            UniversalDeviceToolkit.Lib.IoCContainer.Initialize(
                preBuild: builder => builder.RegisterType<ApplicationSettings>()
                    .AsImplementedInterfaces()
                    .AsSelf()
                    .SingleInstance(),
                new UniversalDeviceToolkit.Lib.IoCModule());
        }
        catch (Exception ex) when (ex is not object)
        {
            throw;
        }
    }

    private WindowsPlatformServices() { }

    public async Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync()
    {
        try
        {
            var machine = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

            var featureGroups = new List<FeatureGroupItem>
            {
                new(DashboardLocalization.Get("Dashboard_Feature_Device", "Device"), machine.Model, machine.MachineType),
                new(DashboardLocalization.Get("Dashboard_Feature_Bios", "BIOS"), machine.BiosVersionRaw ?? Unknown(), machine.Generation > 0 ? DashboardLocalization.Format("Dashboard_Status_Generation", "Gen {0}", machine.Generation) : Unknown()),
                new(DashboardLocalization.Get("Dashboard_Feature_PowerMode", "Power Mode"), DashboardLocalization.Get("Dashboard_Description_PowerManagement", "System power management"),
                    string.Join(", ", machine.SupportedPowerModes)),
            };

            if (UniversalDeviceToolkit.Lib.System.Battery.IsBatteryMonitoringSupported())
            {
                var battery = UniversalDeviceToolkit.Lib.System.Battery.GetBatteryInformation();
                featureGroups.Add(new(DashboardLocalization.Get("Dashboard_Feature_Battery", "Battery"), DashboardLocalization.Get("Dashboard_Description_BatteryManagement", "Battery management"),
                    battery.IsCharging ? DashboardLocalization.Get("Dashboard_Status_Charging", "Charging") : battery.BatteryPercentage > 0 ? $"{battery.BatteryPercentage}%" : DashboardLocalization.Get("Dashboard_Status_Monitoring", "Monitoring")));
            }
            else
            {
                featureGroups.Add(new(DashboardLocalization.Get("Dashboard_Feature_Battery", "Battery"), DashboardLocalization.Get("Dashboard_Description_BatteryManagement", "Battery management"), NotSupported()));
            }

            featureGroups.Add(new(DashboardLocalization.Get("Dashboard_Feature_Keyboard", "Keyboard"), DashboardLocalization.Get("Dashboard_Description_BacklightControl", "Backlight control"), await DetectKeyboardBacklightAsync().ConfigureAwait(false)));

            return featureGroups;
        }
        catch
        {
            return await new SamplePlatformServices().GetFeatureGroupsAsync().ConfigureAwait(false);
        }
    }

    private static async Task<string> DetectKeyboardBacklightAsync()
    {
        try
        {
            var spectrum = UniversalDeviceToolkit.Lib.IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
            if (await spectrum.IsSupportedAsync().ConfigureAwait(false))
                return DashboardLocalization.Get("Dashboard_Status_Spectrum", "Spectrum");

            var rgb = UniversalDeviceToolkit.Lib.IoCContainer.Resolve<RGBKeyboardBacklightController>();
            if (await rgb.IsSupportedAsync().ConfigureAwait(false))
                return DashboardLocalization.Get("Dashboard_Status_PerKey", "Per-key");

            return NotSupported();
        }
        catch (Exception ex)
        {
            Log.Instance.Error("Failed to detect keyboard backlight type.", ex);
            return NotSupported();
        }
    }

    public async Task<bool> IsSupportedLegionMachineAsync()
    {
        try
        {
            var machine = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
            return Compatibility.IsSupportedLegionMachine(machine);
        }
        catch
        {
            return false;
        }
    }

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync()
    {
        try
        {
            var battery = UniversalDeviceToolkit.Lib.System.Battery.GetBatteryInformation();
            var readings = new List<SensorReadingItem>();

            readings.Add(new(DashboardLocalization.Get("Dashboard_Sensor_BatteryCharge", "Battery Charge"), battery.BatteryPercentage.ToString() + "%"));
            readings.Add(new(DashboardLocalization.Get("Dashboard_Sensor_BatteryStatus", "Battery Status"), battery.IsCharging ? DashboardLocalization.Get("Dashboard_Status_Charging", "Charging") : DashboardLocalization.Get("Dashboard_Status_Discharging", "Discharging")));
            readings.Add(new(DashboardLocalization.Get("Dashboard_Sensor_MinDischargeRate", "Min Discharge Rate"), battery.MinDischargeRate.ToString() + " mW"));
            readings.Add(new(DashboardLocalization.Get("Dashboard_Sensor_MaxDischargeRate", "Max Discharge Rate"), battery.MaxDischargeRate.ToString() + " mW"));
            readings.Add(new(DashboardLocalization.Get("Dashboard_Sensor_CycleCount", "Cycle Count"), battery.CycleCount.ToString()));

            return Task.FromResult<IReadOnlyList<SensorReadingItem>>(readings);
        }
        catch
        {
            return new SamplePlatformServices().GetSensorReadingsAsync();
        }
    }

    private static string Unknown() =>
        DashboardLocalization.Get("Dashboard_Status_Unknown", "Unknown");

    private static string NotSupported() =>
        DashboardLocalization.Get("Dashboard_Status_NotSupported", "Not supported");
}

#endif
