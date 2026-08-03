using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace UniversalDeviceToolkit.Lib.System.Management;

public static partial class WMI
{
    public static partial class LenovoGameZoneData
    {
        [GeneratedRegex("PCIVEN_([0-9A-F]{4})|DEV_([0-9A-F]{4})")]
        private static partial Regex DGPUHWIdRegex();

        [GeneratedRegex(@"PCI\\VEN_([0-9A-F]{4})|DEV_([0-9A-F]{4})")]
        private static partial Regex DGPUHWIdRegexV2();

        public static Task<bool> ExistsAsync() => WMI.ExistsAsync("root\\WMI", $"SELECT * FROM LENOVO_GAMEZONE_DATA");

        public static Task<(bool Success, int Value)> TryGetFanCountAsync() =>
            TryCallAsync(
                "root\\WMI",
                $"SELECT * FROM LENOVO_GAMEZONE_DATA",
                "GetFanCount",
                [],
                pdc => Convert.ToInt32(pdc["Data"].Value),
                fallback: -1);

        public static Task<(bool Success, int Value)> TryGetFan1SpeedAsync() =>
            TryGetFanSpeedAsync("GetFan1Speed");

        public static Task<(bool Success, int Value)> TryGetFan2SpeedAsync() =>
            TryGetFanSpeedAsync("GetFan2Speed");

        private static Task<(bool Success, int Value)> TryGetFanSpeedAsync(string methodName) =>
            TryCallAsync(
                "root\\WMI",
                $"SELECT * FROM LENOVO_GAMEZONE_DATA",
                methodName,
                [],
                pdc => Convert.ToInt32(pdc["Data"].Value),
                fallback: -1);

        internal static async Task<(bool Success, int Rpm)> TryGetCpuFanSpeedAsync()
        {
            var (countSuccess, count) = await TryGetFanCountAsync().ConfigureAwait(false);
            return countSuccess && count >= 1
                ? await TryGetFan1SpeedAsync().ConfigureAwait(false)
                : (false, -1);
        }

        internal static async Task<(bool Success, int Rpm)> TryGetGpuFanSpeedAsync()
        {
            var (countSuccess, count) = await TryGetFanCountAsync().ConfigureAwait(false);
            return countSuccess && count >= 2
                ? await TryGetFan2SpeedAsync().ConfigureAwait(false)
                : (false, -1);
        }

        public static Task<int> GetBIOSOCMode() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetBIOSOCMode",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> IsSupportSmartFanAsync() => CallGameZoneMethodWithCimFallbackAsync("IsSupportSmartFan", []);

        public static Task<int> GetSmartFanModeAsync() => CallGameZoneMethodWithCimFallbackAsync("GetSmartFanMode", []);

        public static async Task SetSmartFanModeAsync(int data)
        {
            // Classic invoke first (works on most machines); the CIM-process invoke makes the
            // write also land on providers that do not marshal out-parameters via
            // System.Management. Same-value double write is idempotent.
            await CallAsync("root\\WMI",
                $"SELECT * FROM LENOVO_GAMEZONE_DATA",
                "SetSmartFanMode",
                new() { { "Data", data } }).ConfigureAwait(false);
            await InvokeGameZoneMethodViaCimProcessAsync("SetSmartFanMode", data).ConfigureAwait(false);
        }

        private static async Task<int> CallGameZoneMethodWithCimFallbackAsync(string methodName, Dictionary<string, object> methodParams)
        {
            var classic = await CallAsync("root\\WMI",
                $"SELECT * FROM LENOVO_GAMEZONE_DATA",
                methodName,
                methodParams,
                pdc => Convert.ToInt32(pdc["Data"].Value)).ConfigureAwait(false);

            // Mode/support values are always >= 1; 0 means the provider returned empty out-parameters.
            if (classic > 0)
                return classic;

            return await InvokeGameZoneMethodViaCimProcessAsync(methodName).ConfigureAwait(false);
        }

        public static Task<int> GetIntelligentSubModeAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetIntelligentSubMode",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task SetIntelligentSubModeAsync(int data) => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "SetIntelligentSubMode",
            new() { { "Data", data } });

        public static Task<int> IsSupportGSyncAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsSupportGSync",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> GetGSyncStatusAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetGSyncStatus",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task SetGSyncStatusAsync(int data) => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "SetGSyncStatus",
            new() { { "Data", data } });

        public static Task<int> IsSupportIGPUModeAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsSupportIGPUMode",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> GetIGPUModeStatusAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetIGPUModeStatus",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task SetIGPUModeStatusAsync(int mode) => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "SetIGPUModeStatus",
            new() { { "mode", mode } });

        public static Task NotifyDGPUStatusAsync(int status) => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "NotifyDGPUStatus",
            new() { { "Status", status } });

        public static Task<HardwareId> GetDGPUHWIdAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetDGPUHWId",
            [],
            pdc =>
            {
                var id = pdc["Data"].Value.ToString();

                if (id is null)
                    return HardwareId.Empty;

                try
                {
                    var matches = DGPUHWIdRegex().Matches(id);
                    if (matches.Count != 2)
                    {
                        matches = DGPUHWIdRegexV2().Matches(id);
                        if (matches.Count != 2)
                            return HardwareId.Empty;
                    }

                    var vendor = matches[0].Groups[1].Value;
                    var device = matches[1].Groups[2].Value;

                    return new HardwareId(vendor, device);
                }
                catch (Exception ex)
                {
                    Log.Instance.TraceOnce(
                        "wmi-gamezone-dgpu-hwid-parse",
                        "Failed to parse dGPU hardware id from GameZone WMI.",
                        ex);
                    return HardwareId.Empty;
                }
            });

        public static Task<(bool Success, int Value)> TryIsSupportGpuOCAsync() => TryCallAsync(
            "ROOT\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsSupportGpuOC",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value),
            fallback: 0);

        public static Task<int> IsSupportGpuOCAsync() => CallAsync("ROOT\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsSupportGpuOC",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> IsSupportDisableTPAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsSupportDisableTP",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> GetTPStatusStatusAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetTPStatus",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task SetTPStatusAsync(int data) => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "SetTPStatus",
            new() { { "Data", data } });

        public static Task<int> IsSupportDisableWinKeyAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsSupportDisableWinKey",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> GetWinKeyStatusAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetWinKeyStatus",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task SetWinKeyStatusAsync(int data) => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "SetWinKeyStatus",
            new() { { "Data", data } });

        public static Task<int> IsSupportODAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsSupportOD",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> GetODStatusAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetODStatus",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task SetODStatusAsync(int data) => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "SetODStatus",
            new() { { "Data", data } });

        public static Task SetLightControlOwnerAsync(int data) => CallAsync("ROOT\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "SetLightControlOwner",
            new() { { "Data", data } });

        public static Task<int> IsACFitForOCAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "IsACFitForOC",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> GetPowerChargeModeAsync() => CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetPowerChargeMode",
            [],
            pdc => Convert.ToInt32(pdc["Data"].Value));

        public static Task<int> GetCPUFrequencyAsync() => WMI.CallAsync("root\\WMI",
            $"SELECT * FROM LENOVO_GAMEZONE_DATA",
            "GetCPUFrequency",
            [],
            pdc =>
            {
                var value = Convert.ToInt32(pdc["Data"].Value);
                var low = value & 0xFFFF;
                var high = value >> 16;
                return Math.Max(low, high);
            });
    }
}
