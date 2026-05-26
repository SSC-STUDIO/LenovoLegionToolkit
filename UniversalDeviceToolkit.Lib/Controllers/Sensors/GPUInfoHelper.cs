using System;
using System.Collections;
using System.Linq;
using LenovoLegionToolkit.Lib.Utils;
using NvAPIWrapper.Native.GPU;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public static class GPUInfoHelper
{
    public static int GetWattage(object gpu)
    {
        try
        {
            var powerInfoProp = gpu.GetType().GetProperty("PowerInformation");
            if (powerInfoProp == null) return -1;

            var powerInfo = powerInfoProp.GetValue(gpu);
            if (powerInfo == null) return -1;

            var powerEntriesProp = powerInfo.GetType().GetProperty("PowerEntries");
            if (powerEntriesProp == null) return -1;

            var powerEntries = powerEntriesProp.GetValue(powerInfo) as IEnumerable;
            if (powerEntries == null) return -1;

            var firstEntry = powerEntries.Cast<object>().FirstOrDefault();
            if (firstEntry == null) return -1;

            var powerProp = firstEntry.GetType().GetProperty("Power");
            if (powerProp == null) return -1;

            var powerValue = powerProp.GetValue(firstEntry);
            if (powerValue == null) return -1;

            return (int)(Convert.ToDouble(powerValue) / 1000.0);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get NVML data: {ex.Message}", ex);
            return -1;
        }
    }

    public static double GetVoltage(object gpu)
    {
        try
        {
            var voltageSensorProp = gpu.GetType().GetProperty("VoltageSensor");

            if (voltageSensorProp == null) return 0;

            var voltageSensor = voltageSensorProp.GetValue(gpu);
            if (voltageSensor == null) return 0;

            var isAvailableProp = voltageSensor.GetType().GetProperty("IsAvailable");
            var currentVoltageProp = voltageSensor.GetType().GetProperty("CurrentVoltage");

            if (isAvailableProp == null || currentVoltageProp == null) return 0;

            var isAvailable = isAvailableProp.GetValue(voltageSensor);
            if (isAvailable is not bool available || !available) return 0;

            var voltageValue = currentVoltageProp.GetValue(voltageSensor);
            if (voltageValue == null) return 0;

            var currentVoltage = voltageValue switch
            {
                uint voltageUint => voltageUint / 1000.0,
                int voltageInt => voltageInt / 1000.0,
                float voltageFloat => voltageFloat,
                double voltageDouble => voltageDouble,
                _ => Convert.ToDouble(voltageValue)
            };

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GPU voltage: {currentVoltage}V (raw: {voltageValue})");

            return currentVoltage;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get GPU voltage: {ex.Message}");
            return 0;
        }
    }

    public static int GetWattageFromPowerTopology(object gpu)
    {
        try
        {
            var powerTopologyProp = gpu.GetType().GetProperty("PowerTopology");
            if (powerTopologyProp == null) return -1;

            var powerTopology = powerTopologyProp.GetValue(gpu);
            var statusProp = powerTopology?.GetType().GetProperty("Status");
            if (statusProp == null) return -1;

            var status = statusProp.GetValue(powerTopology);
            var entriesProp = status?.GetType().GetProperty("PowerPolicyStatusEntries");
            if (entriesProp == null) return -1;

            var entries = entriesProp.GetValue(status) as Array;
            if (entries == null) return -1;

            foreach (var entry in entries)
            {
                if (entry == null) continue;

                var domainProp = entry.GetType().GetProperty("Domain");
                var usageProp = entry.GetType().GetProperty("PowerUsageInPCM");

                if (domainProp == null || usageProp == null) continue;

                var domainValue = domainProp.GetValue(entry);
                if (domainValue == null) continue;

                var domain = domainValue.ToString();
                if (domain != "GPU" && domain != "Board") continue;

                var val = Convert.ToUInt32(usageProp.GetValue(entry));
                if (val > 0)
                    return (int)(val / 1000);
            }

            return -1;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to get GPU info: {ex.Message}", ex);
            return -1;
        }
    }
}
