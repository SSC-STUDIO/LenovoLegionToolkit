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

        // Property names seen across Legion firmware generations for Fan_GetCurrentFanSpeed out-params.
        private static readonly string[] FanSpeedPropertyNames =
        [
            "CurrentFanSpeed",
            "FanSpeed",
            "CurrentSpeed",
            "Speed",
            "Data",
            "Value"
        ];

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
        /// Historical primary fan RPM path used by LLT V1/V2 (direct FanID call).
        /// Soft-fails without throwing when the firmware method is missing.
        /// </summary>
        public static Task<int> FanGetCurrentFanSpeedAsync(int fanId) =>
            FanGetCurrentFanSpeedPreferAsync(fanId);

        /// <summary>
        /// Try preferred fan IDs in order (restores multi-generation support:
        /// V1/V2 used 0/1, V3+ used 1/2). First positive RPM wins; explicit 0 means parked
        /// only when the WMI call itself succeeded.
        /// </summary>
        public static async Task<int> FanGetCurrentFanSpeedPreferAsync(params int[] fanIds)
        {
            if (fanIds is null || fanIds.Length == 0)
                return -1;

            // Do not hard-skip on soft-fail cache alone for a single generation — still
            // attempt once; permanent soft-fail only applies after InvalidMethod.
            var sawSuccessfulZero = false;
            var anyAttemptSucceeded = false;

            foreach (var fanId in fanIds.Distinct())
            {
                var (ok, rpm) = await TryCallAsync(
                    FanMethodScope,
                    $"{FanMethodQuery}",
                    "Fan_GetCurrentFanSpeed",
                    new() { { "FanID", fanId } },
                    ExtractFanSpeedRpm,
                    fallback: -1).ConfigureAwait(false);

                if (!ok)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Fan_GetCurrentFanSpeed unavailable for fanId={fanId}.");
                    continue;
                }

                anyAttemptSucceeded = true;
                if (rpm > 0)
                    return rpm;

                // Explicit 0 = fan parked at this id (valid reading only when invoke succeeded).
                sawSuccessfulZero = true;
            }

            if (sawSuccessfulZero)
                return 0;

            return anyAttemptSucceeded ? 0 : -1;
        }

        /// <summary>
        /// Extract RPM from WMI out-params. Older/newer firmware disagree on property names.
        /// </summary>
        private static int ExtractFanSpeedRpm(PropertyDataCollection properties)
        {
            foreach (var name in FanSpeedPropertyNames)
            {
                try
                {
                    var raw = properties[name]?.Value;
                    if (raw is null)
                        continue;
                    var value = Convert.ToInt32(raw);
                    if (value >= 0)
                        return value;
                }
                catch
                {
                    // try next property name
                }
            }

            // Last resort: first non-negative convertible property.
            foreach (PropertyData property in properties)
            {
                try
                {
                    if (property.Value is null)
                        continue;
                    var value = Convert.ToInt32(property.Value);
                    if (value >= 0)
                        return value;
                }
                catch
                {
                    // keep scanning
                }
            }

            throw new InvalidOperationException("Fan_GetCurrentFanSpeed returned no readable RPM property.");
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
