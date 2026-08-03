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
                new("Device", machine.Model, machine.MachineType),
                new("BIOS", machine.BiosVersionRaw ?? "Unknown", machine.Generation > 0 ? $"Gen {machine.Generation}" : "Unknown"),
                new("Power Mode", "System power management",
                    string.Join(", ", machine.SupportedPowerModes)),
            };

            if (UniversalDeviceToolkit.Lib.System.Battery.IsBatteryMonitoringSupported())
            {
                var battery = UniversalDeviceToolkit.Lib.System.Battery.GetBatteryInformation();
                featureGroups.Add(new("Battery", "Battery management",
                    battery.IsCharging ? "Charging" : battery.BatteryPercentage > 0 ? $"{battery.BatteryPercentage}%" : "Monitoring"));
            }
            else
            {
                featureGroups.Add(new("Battery", "Battery management", "Not supported"));
            }

            featureGroups.Add(new("Keyboard", "Backlight control", await DetectKeyboardBacklightAsync().ConfigureAwait(false)));

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
                return "Spectrum";

            var rgb = UniversalDeviceToolkit.Lib.IoCContainer.Resolve<RGBKeyboardBacklightController>();
            if (await rgb.IsSupportedAsync().ConfigureAwait(false))
                return "Per-key";

            return "Not supported";
        }
        catch (Exception ex)
        {
            Log.Instance.Error("Failed to detect keyboard backlight type.", ex);
            return "Not supported";
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

            readings.Add(new("Battery Charge", battery.BatteryPercentage.ToString() + "%"));
            readings.Add(new("Battery Status", battery.IsCharging ? "Charging" : "Discharging"));
            readings.Add(new("Min Discharge Rate", battery.MinDischargeRate.ToString() + " mW"));
            readings.Add(new("Max Discharge Rate", battery.MaxDischargeRate.ToString() + " mW"));
            readings.Add(new("Cycle Count", battery.CycleCount.ToString()));

            return Task.FromResult<IReadOnlyList<SensorReadingItem>>(readings);
        }
        catch
        {
            return new SamplePlatformServices().GetSensorReadingsAsync();
        }
    }
}

#endif