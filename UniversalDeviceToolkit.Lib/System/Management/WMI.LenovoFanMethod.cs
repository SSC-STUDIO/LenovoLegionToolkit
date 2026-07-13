using System;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace LenovoLegionToolkit.Lib.System.Management;

public static partial class WMI
{
    public static class LenovoFanMethod
    {
        private const string FanMethodScope = "root\\WMI";
        private const string FanMethodQuery = "SELECT * FROM LENOVO_FAN_METHOD";

        public static Task FanSetTableAsync(byte[] fanTable) => CallAsync(FanMethodScope,
            $"{FanMethodQuery}",
            "Fan_Set_Table",
            new() { { "FanTable", fanTable } });

        public static Task<bool> FanGetFullSpeedAsync() => CallAsync(FanMethodScope,
            $"{FanMethodQuery}",
            "Fan_Get_FullSpeed",
            [],
            pdc => (bool)pdc["Status"].Value);

        public static Task FanSetFullSpeedAsync(int status) => CallAsync(FanMethodScope,
            $"{FanMethodQuery}",
            "Fan_Set_FullSpeed",
            new() { { "Status", status } });

        public static async Task<int> FanGetCurrentSensorTemperatureAsync(int sensorId)
        {
            var (ok, t) = await TryCallAsync(
                FanMethodScope,
                $"{FanMethodQuery}",
                "Fan_GetCurrentSensorTemperature",
                new() { { "SensorID", sensorId } },
                pdc => Convert.ToInt32(pdc["CurrentSensorTemperature"].Value),
                fallback: -1).ConfigureAwait(false);
            return ok && t > 0 ? t : -1;
        }

        /// <summary>
        /// Historical primary fan RPM path (pre-capability). Some Legion firmwares expose
        /// LENOVO_FAN_METHOD without this method — returns -1 without throwing.
        /// Soft-fail cache only when the method is truly missing (not per-id invoke fails).
        /// </summary>
        public static Task<int> FanGetCurrentFanSpeedAsync(int fanId) =>
            FanGetCurrentFanSpeedPreferAsync(fanId);

        /// <summary>
        /// Try preferred fan IDs in order (restores multi-generation support:
        /// V1/V2 used 0/1, V3+ used 1/2). First positive RPM wins; explicit 0 means parked.
        /// </summary>
        public static async Task<int> FanGetCurrentFanSpeedPreferAsync(params int[] fanIds)
        {
            if (fanIds is null || fanIds.Length == 0)
                return -1;

            if (IsWmiMethodSoftFailed(FanMethodScope, FanMethodQuery, "Fan_GetCurrentFanSpeed"))
                return -1;

            var sawParkedZero = false;

            foreach (var fanId in fanIds.Distinct())
            {
                var (ok, rpm) = await TryCallAsync(
                    FanMethodScope,
                    $"{FanMethodQuery}",
                    "Fan_GetCurrentFanSpeed",
                    new() { { "FanID", fanId } },
                    pdc => Convert.ToInt32(pdc["CurrentFanSpeed"].Value),
                    fallback: -1).ConfigureAwait(false);

                if (!ok)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Fan_GetCurrentFanSpeed unavailable for fanId={fanId}.");
                    continue;
                }

                if (rpm > 0)
                    return rpm;

                // Explicit 0 = fan parked at this id (still a valid reading).
                sawParkedZero = true;
            }

            return sawParkedZero ? 0 : -1;
        }

        public static async Task<int> GetCurrentFanMaxSpeedAsync(int sensorId, int fanId)
        {
            try
            {
                var result = await ReadAsync("root\\WMI",
                    $"SELECT * FROM LENOVO_FAN_TABLE_DATA WHERE Sensor_ID = {sensorId} AND Fan_Id = {fanId}",
                    pdc => Convert.ToInt32(pdc["CurrentFanMaxSpeed"].Value)).ConfigureAwait(false);
                return result.DefaultIfEmpty(-1).Max();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ManagementException)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(
                        $"GetCurrentFanMaxSpeed unavailable. [sensorId={sensorId}, fanId={fanId}]",
                        ex);
                return -1;
            }
        }

        public static async Task<int> GetDefaultFanMaxSpeedAsync(int sensorId, int fanID)
        {
            try
            {
                var result = await ReadAsync("root\\WMI",
                    $"SELECT * FROM LENOVO_FAN_TABLE_DATA WHERE Sensor_ID = {sensorId} AND Fan_Id = {fanID}",
                    pdc => Convert.ToInt32(pdc["DefaultFanMaxSpeed"].Value)).ConfigureAwait(false);
                return result.DefaultIfEmpty(-1).Max();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ManagementException)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(
                        $"GetDefaultFanMaxSpeed unavailable. [sensorId={sensorId}, fanId={fanID}]",
                        ex);
                return -1;
            }
        }
    }
}
