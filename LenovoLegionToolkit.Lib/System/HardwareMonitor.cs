using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.System;

public class HardwareMonitor : IDisposable
{
    private static readonly Lazy<HardwareMonitor> _instance = new(() => new HardwareMonitor());
    public static HardwareMonitor Instance => _instance.Value;

    private readonly Computer _computer;
    private bool _isInitialized;

    private HardwareMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = false, // We already have GPUController for GPU
            IsMemoryEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsStorageEnabled = false
        };
    }

    public void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            _computer.Open();
            _isInitialized = true;
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"HardwareMonitor initialized successfully with LibreHardwareMonitorLib.");
        }
        catch (Exception ex)
        {
            Log.Instance.Error($"Failed to initialize HardwareMonitor", ex);
        }
    }

    public double GetCpuVoltage()
    {
        if (!_isInitialized) Initialize();
        if (!_isInitialized) return 0;

        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.Cpu)
                {
                    hardware.Update();
                    // Look for voltage sensors
                    var voltageSensor = hardware.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Voltage && (s.Name.Contains("Core") || s.Name.Contains("VCore") || s.Name.Contains("VID")));
                    if (voltageSensor != null)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"CPU Voltage found via {voltageSensor.Name}: {voltageSensor.Value}V");
                        return voltageSensor.Value ?? 0;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error reading CPU voltage from HardwareMonitor", ex);
        }

        return 0;
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            _computer.Close();
            _isInitialized = false;
        }
    }
}
