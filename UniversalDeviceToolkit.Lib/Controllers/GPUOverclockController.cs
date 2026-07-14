using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.System.Management;
using UniversalDeviceToolkit.Lib.Utils;
using NvAPIWrapper.GPU;
using NvAPIWrapper.Native;
using NvAPIWrapper.Native.GPU;
using NvAPIWrapper.Native.GPU.Structures;

namespace UniversalDeviceToolkit.Lib.Controllers;

public class GPUOverclockController : IDisposable
{
    /// <summary>
    /// Serializes all NVAPI.Initialize/Unload pairs. NVAPI is not thread-safe;
    /// concurrent Initialize/Unload (e.g. UI reading max memory delta while ApplyStateAsync runs) is unsafe (H-015).
    /// </summary>
    private static readonly SemaphoreSlim NvApiLock = new(1, 1);

    private readonly GPUOverclockSettings _settings;
    private readonly VantageDisabler _vantageDisabler;
    private readonly LegionZoneDisabler _legionZoneDisabler;
    private readonly NativeWindowsMessageListener _nativeWindowsMessageListener;
    private int _disposed;

    public event EventHandler? Changed;

    public GPUOverclockController(GPUOverclockSettings settings,
        VantageDisabler vantageDisabler,
        LegionZoneDisabler legionZoneDisabler,
        NativeWindowsMessageListener nativeWindowsMessageListener)
    {
        _settings = settings;
        _vantageDisabler = vantageDisabler;
        _legionZoneDisabler = legionZoneDisabler;
        _nativeWindowsMessageListener = nativeWindowsMessageListener;
        _nativeWindowsMessageListener.Changed += NativeWindowsMessageListenerOnChanged;
    }

    public static int GetMaxCoreDeltaMhz() => 500;

    public static int GetMaxMemoryDeltaMhz()
    {
        NvApiLock.Wait();
        try
        {
            try
            {
                NVAPI.Initialize();
                return GetMaxMemoryDeltaMhz(NVAPI.GetGPU());
            }
            finally
            {
                try { NVAPI.Unload(); } catch
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Failed to unload NVAPI in GetMaxMemoryDeltaMhz");
                }
            }
        }
        finally
        {
            NvApiLock.Release();
        }
    }

    public async Task<bool> IsSupportedAsync()
    {
        bool isSupported;

        try
        {
            var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

            if (!Compatibility.IsSupportedLegionMachine(mi))
                return false;

            await NvApiLock.WaitAsync().ConfigureAwait(false);
            try
            {
                try
                {
                    NVAPI.Initialize();
                    isSupported = NVAPI.GetGPU() is not null;
                }
                catch
                {
                    isSupported = false;
                }
                finally
                {
                    try { NVAPI.Unload(); } catch
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace("Failed to unload NVAPI in IsSupportedAsync");
                    }
                }
            }
            finally
            {
                NvApiLock.Release();
            }
        }
        catch
        {
            isSupported = false;
        }

        Log.Instance.Info($"NVAPI status: {isSupported}.");

        if (!isSupported)
            return isSupported;

        try
        {
            isSupported = await WMI.LenovoGameZoneData.IsSupportGpuOCAsync().ConfigureAwait(false) > 0;

            if (!isSupported)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Clearing settings...");

                SaveState(false, GPUOverclockInfo.Zero);
            }
        }
        catch
        {
            isSupported = false;
        }

        Log.Instance.Info($"Supports GPU OC status: {isSupported}");

        return isSupported;
    }

    public (bool, GPUOverclockInfo) GetState()
    {
        var (_, profile) = GetActiveProfile();
        return (_settings.Store.Enabled, profile.Info);
    }

    public Guid GetActiveProfileId() => GetActiveProfile().Item1;

    public IReadOnlyDictionary<Guid, GPUOverclockSettings.GPUOverclockSettingsStore.Profile> GetProfiles()
    {
        EnsureProfiles();
        return _settings.Store.Profiles.AsReadOnlyDictionary();
    }

    public void SaveState(bool enabled, GPUOverclockInfo info)
    {
        var activeProfileId = GetActiveProfileId();
        SaveState(enabled, activeProfileId, info);
    }

    public void SaveState(bool enabled, Guid activeProfileId, GPUOverclockInfo info)
    {
        _settings.Store.Enabled = enabled;
        SaveProfile(activeProfileId, info);
    }

    public void SaveProfile(Guid profileId, GPUOverclockInfo info)
    {
        EnsureProfiles();

        var store = _settings.Store;
        if (!store.Profiles.TryGetValue(profileId, out var profile))
            profileId = store.ActiveProfileId;

        profile = store.Profiles[profileId];
        store.Profiles[profileId] = new()
        {
            Name = profile.Name,
            Info = info
        };
        store.ActiveProfileId = profileId;
        store.Info = info;
        _settings.SynchronizeStore();
    }

    public Guid AddProfile(string name, GPUOverclockInfo info)
    {
        EnsureProfiles();

        var profileId = Guid.NewGuid();
        _settings.Store.Profiles[profileId] = new()
        {
            Name = GetUniqueProfileName(name, _settings.Store.Profiles),
            Info = info
        };
        _settings.Store.ActiveProfileId = profileId;
        _settings.Store.Info = info;
        _settings.SynchronizeStore();

        return profileId;
    }

    public void RenameProfile(Guid profileId, string name)
    {
        EnsureProfiles();

        if (!_settings.Store.Profiles.TryGetValue(profileId, out var profile))
            return;

        _settings.Store.Profiles[profileId] = new()
        {
            Name = GetUniqueProfileName(name, _settings.Store.Profiles, profileId),
            Info = profile.Info
        };
        _settings.SynchronizeStore();
    }

    public void DeleteProfile(Guid profileId)
    {
        EnsureProfiles();

        var store = _settings.Store;
        if (store.Profiles.Count <= 1)
            return;

        store.Profiles.Remove(profileId);

        if (store.ActiveProfileId == profileId || !store.Profiles.ContainsKey(store.ActiveProfileId))
            store.ActiveProfileId = store.Profiles.OrderBy(kv => kv.Value.Name).First().Key;

        store.Info = store.Profiles[store.ActiveProfileId].Info;
        store.Profiles = new Dictionary<Guid, GPUOverclockSettings.GPUOverclockSettingsStore.Profile>(store.Profiles);
        _settings.SynchronizeStore();
    }

    public void SetActiveProfile(Guid profileId)
    {
        EnsureProfiles();

        var profile = _settings.Store.Profiles[profileId];
        _settings.Store.ActiveProfileId = profileId;
        _settings.Store.Info = profile.Info;
        _settings.SynchronizeStore();
    }

    public async Task ApplyStateAsync(bool force = false)
    {
        if (await _vantageDisabler.GetStatusAsync().ConfigureAwait(false) == SoftwareStatus.Enabled)
        {
            Log.Instance.Warning($"Can't correctly apply state when Vantage is running.");

            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (await _legionZoneDisabler.GetStatusAsync().ConfigureAwait(false) == SoftwareStatus.Enabled)
        {
            Log.Instance.Warning($"Can't correctly apply state when Legion Zone is running.");

            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }

        var (enabled, info) = GetState();

        if (force)
        {
            info = enabled ? info : GPUOverclockInfo.Zero;
            enabled = true;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Forcing... [enabled=true, info={info}]");
        }

        if (!enabled)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Not enabled.");

            Changed?.Invoke(this, EventArgs.Empty);

            return;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Applying overclock: {info}.");

        try
        {
            await NvApiLock.WaitAsync().ConfigureAwait(false);
            try
            {
                NVAPI.Initialize();

                var gpu = NVAPI.GetGPU();
                if (gpu is null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"dGPU not found.");

                    Changed?.Invoke(this, EventArgs.Empty);

                    return;
                }

                SetOverclockInfo(gpu, info);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Applied overclock: {info}, current: {GetOverclockInfo(gpu)}.");
            }
            finally
            {
                try { NVAPI.Unload(); } catch
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Failed to unload NVAPI in ApplyStateAsync");
                }

                NvApiLock.Release();
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to apply overclock: {info}, clearing settings...", ex);

            SaveState(false, GPUOverclockInfo.Zero);
        }
        finally
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<bool> EnsureOverclockIsAppliedAsync()
    {
        var (enabled, _) = GetState();
        if (!enabled)
            return false;

        await ApplyStateAsync().ConfigureAwait(false);
        return true;
    }

private async Task NativeWindowsMessageListenerOnChangedAsync(object? sender, NativeWindowsMessageListener.ChangedEventArgs e)
    {
        try
        {
            if (e.Message != NativeWindowsMessage.OnDisplayDeviceArrival)
                return;

            if (await IsSupportedAsync().ConfigureAwait(false))
                await ApplyStateAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Error in NativeWindowsMessageListenerOnChanged: {ex.Message}", ex);
        }
    }

    // Event handler wrapper that properly handles async task
    private void NativeWindowsMessageListenerOnChanged(object? sender, NativeWindowsMessageListener.ChangedEventArgs e)
    {
        _ = NativeWindowsMessageListenerOnChangedAsync(sender, e);
    }

    private static int GetMaxMemoryDeltaMhz(PhysicalGPU? gpu) => gpu?.MemoryInformation.RAMMaker switch
    {
        GPUMemoryMaker.Samsung => 1500,
        _ => 750
    };

    private static void SetOverclockInfo(PhysicalGPU gpu, GPUOverclockInfo info)
    {
        var coreDelta = Math.Clamp(info.CoreDeltaMhz, 0, GetMaxCoreDeltaMhz());
        var memoryDelta = Math.Clamp(info.MemoryDeltaMhz, 0, GetMaxMemoryDeltaMhz(gpu));

        var clockEntries = new[]
        {
            new PerformanceStates20ClockEntryV1(PublicClockDomain.Graphics, new PerformanceStates20ParameterDelta(coreDelta * 1000)),
            new PerformanceStates20ClockEntryV1(PublicClockDomain.Memory, new PerformanceStates20ParameterDelta(memoryDelta * 1000))
        };
        var voltageEntries = Array.Empty<PerformanceStates20BaseVoltageEntryV1>();
        var performanceStateInfo = new[] { new PerformanceStates20InfoV1.PerformanceState20(PerformanceStateId.P0_3DPerformance, clockEntries, voltageEntries) };

        var overclock = new PerformanceStates20InfoV1(performanceStateInfo, 2, 0);
        GPUApi.SetPerformanceStates20(gpu.Handle, overclock);
    }

    private static GPUOverclockInfo GetOverclockInfo(PhysicalGPU gpu)
    {
        var states = GPUApi.GetPerformanceStates20(gpu.Handle);
        var core = states.Clocks[PerformanceStateId.P0_3DPerformance][0].FrequencyDeltaInkHz.DeltaValue / 1000;
        var memory = states.Clocks[PerformanceStateId.P0_3DPerformance][1].FrequencyDeltaInkHz.DeltaValue / 1000;
        return new(core, memory);
    }

    private (Guid, GPUOverclockSettings.GPUOverclockSettingsStore.Profile) GetActiveProfile()
    {
        EnsureProfiles();

        var store = _settings.Store;
        return (store.ActiveProfileId, store.Profiles[store.ActiveProfileId]);
    }

    private void EnsureProfiles()
    {
        var store = _settings.Store;
        var changed = false;

        if (store.Profiles.Count == 0)
        {
            var profileId = Guid.NewGuid();
            store.ActiveProfileId = profileId;
            store.Profiles = new Dictionary<Guid, GPUOverclockSettings.GPUOverclockSettingsStore.Profile>
            {
                [profileId] = new()
                {
                    Name = GPUOverclockSettings.DefaultProfileName,
                    Info = store.Info
                }
            };
            _settings.SynchronizeStore();
            return;
        }

        if (!store.Profiles.ContainsKey(store.ActiveProfileId))
        {
            store.ActiveProfileId = store.Profiles.OrderBy(kv => kv.Value.Name).First().Key;
            changed = true;
        }

        store.Info = store.Profiles[store.ActiveProfileId].Info;
        if (changed)
            _settings.SynchronizeStore();
    }

    internal static string NormalizeProfileName(string? name)
    {
        var normalized = name?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? GPUOverclockSettings.DefaultProfileName
            : normalized;
    }

    internal static string GetUniqueProfileName(
        string? requestedName,
        IReadOnlyDictionary<Guid, GPUOverclockSettings.GPUOverclockSettingsStore.Profile> profiles,
        Guid? excludeProfileId = null)
    {
        var normalizedRequestedName = NormalizeProfileName(requestedName);
        var existingNames = profiles
            .Where(kv => !excludeProfileId.HasValue || kv.Key != excludeProfileId.Value)
            .Select(kv => NormalizeProfileName(kv.Value.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!existingNames.Contains(normalizedRequestedName))
            return normalizedRequestedName;

        var suffix = 2;
        while (true)
        {
            var candidate = $"{normalizedRequestedName} ({suffix})";
            if (!existingNames.Contains(candidate))
                return candidate;

            suffix++;
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
            return;

        _nativeWindowsMessageListener.Changed -= NativeWindowsMessageListenerOnChanged;
        GC.SuppressFinalize(this);
    }
}
