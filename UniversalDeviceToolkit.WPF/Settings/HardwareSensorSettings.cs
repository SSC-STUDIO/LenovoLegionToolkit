using LenovoLegionToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.WPF.Settings;

public class HardwareSensorSettings() : AbstractSettings<HardwareSensorSettings.HardwareSensorSettingsStore>("hardware_sensors.json")
{
    public class HardwareSensorSettingsStore
    {
        public bool SelectedGpuIsIgpu { get; set; }
        public bool ShowCpuAverageFrequency { get; set; }
        public bool DisplayMemoryInGigabytes { get; set; }
    }

    protected override HardwareSensorSettingsStore Default => new();
}
