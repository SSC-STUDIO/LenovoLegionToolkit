using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.WPF.Settings;

public class HardwareSensorSettings() : AbstractSettings<HardwareSensorSettings.HardwareSensorSettingsStore>("hardware_sensors.json")
{
    public event System.EventHandler? SectionsChanged;

    public class HardwareSensorSettingsStore
    {
        public bool SelectedGpuIsIgpu { get; set; }
        public bool ShowCpuAverageFrequency { get; set; }
        public bool DisplayMemoryInGigabytes { get; set; }
        public string[] VisibleSections { get; set; } = ["CPU", "Battery", "GPU"];
        public string[] SectionOrder { get; set; } = ["CPU", "Battery", "GPU"];
    }

    protected override HardwareSensorSettingsStore Default => new();

    public void NotifySectionsChanged() => SectionsChanged?.Invoke(this, System.EventArgs.Empty);
}
