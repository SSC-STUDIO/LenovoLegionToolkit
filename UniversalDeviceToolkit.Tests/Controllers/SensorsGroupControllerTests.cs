using FluentAssertions;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Controllers;

[Trait("Category", TestCategories.Controller)]
public class SensorsGroupControllerTests
{
    [Fact]
    public void SelectCpuTemperatureSensorName_ShouldPreferPackageSensors()
    {
        var result = SensorsGroupController.SelectCpuTemperatureSensorName(
        [
            "Core #1",
            "CPU Package",
            "Core Max"
        ]);

        result.Should().Be("CPU Package");
    }

    [Fact]
    public void SelectCpuTemperatureSensorName_WhenPackageMissing_ShouldPreferCoreMax()
    {
        var result = SensorsGroupController.SelectCpuTemperatureSensorName(
        [
            "Core #1",
            "Core Max",
            "VRM"
        ]);

        result.Should().Be("Core Max");
    }

    [Fact]
    public void SelectCpuTemperatureSensorName_ShouldPreferTctlTdieBeforeGenericCpuSensors()
    {
        var result = SensorsGroupController.SelectCpuTemperatureSensorName(
        [
            "CPU",
            "Tctl/Tdie",
            "Core #1"
        ]);

        result.Should().Be("Tctl/Tdie");
    }

    [Fact]
    public void SelectCpuVoltageSensorName_ShouldPreferCoreVoltageAliases()
    {
        var result = SensorsGroupController.SelectCpuVoltageSensorName(
        [
            "System Agent Voltage",
            "CPU Core Voltage",
            "Cache Voltage"
        ]);

        result.Should().Be("CPU Core Voltage");
    }

    [Fact]
    public void SelectCpuVoltageSensorName_ShouldPreferVidBeforeGenericAgentOrCacheVoltage()
    {
        var result = SensorsGroupController.SelectCpuVoltageSensorName(
        [
            "Cache Voltage",
            "IA VID",
            "System Agent Voltage"
        ]);

        result.Should().Be("IA VID");
    }

    [Fact]
    public void SelectCpuPackagePowerSensorName_ShouldRecognizeAmdPptAlias()
    {
        var result = SensorsGroupController.SelectCpuPackagePowerSensorName(
        [
            "GPU Package",
            "CPU Core",
            "CPU PPT"
        ]);

        result.Should().Be("CPU PPT");
    }

    [Fact]
    public void SelectCpuUsageSensorName_ShouldPreferCpuTotalBeforeCoreMaxOrThreadLoads()
    {
        var result = SensorsGroupController.SelectCpuUsageSensorName(
        [
            "CPU Core Max",
            "CPU Core #1 Thread #1",
            "CPU Total"
        ]);

        result.Should().Be("CPU Total");
    }

    [Fact]
    public void SelectCpuUsageSensorName_ShouldPreferCpuUsageAliasOverPerCoreEntries()
    {
        var result = SensorsGroupController.SelectCpuUsageSensorName(
        [
            "CPU Usage",
            "CPU Core #9"
        ]);

        result.Should().Be("CPU Usage");
    }

    [Fact]
    public void SelectGpuVramTemperatureSensorName_ShouldPreferMemorySpecificTemperatureNames()
    {
        var result = SensorsGroupController.SelectGpuVramTemperatureSensorName(
        [
            "GPU Hot Spot",
            "GPU Memory Junction",
            "Core"
        ]);

        result.Should().Be("GPU Memory Junction");
    }

    [Fact]
    public void SelectGpuVramTemperatureSensorName_ShouldRecognizeVramTemperatureAliases()
    {
        var result = SensorsGroupController.SelectGpuVramTemperatureSensorName(
        [
            "Core Temperature",
            "VRAM Temperature"
        ]);

        result.Should().Be("VRAM Temperature");
    }

    [Fact]
    public void SelectGpuHotSpotTemperatureSensorName_ShouldRecognizeGpuHotSpotAliases()
    {
        var result = SensorsGroupController.SelectGpuHotSpotTemperatureSensorName(
        [
            "GPU Temperature",
            "GPU Hot Spot",
            "GPU Memory Junction"
        ]);

        result.Should().Be("GPU Hot Spot");
    }

    [Fact]
    public void SelectGpuVramUsedSensorName_ShouldRecognizeDedicatedMemoryAliases()
    {
        var result = SensorsGroupController.SelectGpuVramUsedSensorName(
        [
            "Board Power Draw",
            "Dedicated Memory Used"
        ]);

        result.Should().Be("Dedicated Memory Used");
    }

    [Fact]
    public void SelectGpuVramUsedSensorName_ShouldPreferGpuMemoryUsedBeforeD3DDedicatedMemoryUsed()
    {
        var result = SensorsGroupController.SelectGpuVramUsedSensorName(
        [
            "D3D Dedicated Memory Used",
            "GPU Memory Used"
        ]);

        result.Should().Be("GPU Memory Used");
    }

    [Fact]
    public void SelectGpuVramTotalSensorName_ShouldRecognizeVramTotalAliases()
    {
        var result = SensorsGroupController.SelectGpuVramTotalSensorName(
        [
            "GPU Clock",
            "VRAM Total"
        ]);

        result.Should().Be("VRAM Total");
    }

    [Fact]
    public void SelectGpuVramUsedSensorName_ShouldRecognizeSharedMemoryAliases()
    {
        var result = SensorsGroupController.SelectGpuVramUsedSensorName(
        [
            "D3D Shared Memory Used",
            "GPU Clock"
        ]);

        result.Should().Be("D3D Shared Memory Used");
    }

    [Fact]
    public void SelectGpuVramTotalSensorName_ShouldRecognizeSharedMemoryTotalAliases()
    {
        var result = SensorsGroupController.SelectGpuVramTotalSensorName(
        [
            "D3D Shared Memory Total",
            "GPU Core"
        ]);

        result.Should().Be("D3D Shared Memory Total");
    }

    [Fact]
    public void SelectGpuVramFreeSensorName_ShouldRecognizeGpuMemoryFreeAliases()
    {
        var result = SensorsGroupController.SelectGpuVramFreeSensorName(
        [
            "GPU Core",
            "GPU Memory Free"
        ]);

        result.Should().Be("GPU Memory Free");
    }

    [Fact]
    public void SelectGpuPcieRxThroughputSensorName_ShouldRecognizeGpuPcieRxAlias()
    {
        var result = SensorsGroupController.SelectGpuPcieRxThroughputSensorName(
        [
            "GPU Memory",
            "GPU PCIe Rx"
        ]);

        result.Should().Be("GPU PCIe Rx");
    }

    [Fact]
    public void SelectGpuPcieTxThroughputSensorName_ShouldRecognizeGpuPcieTxAlias()
    {
        var result = SensorsGroupController.SelectGpuPcieTxThroughputSensorName(
        [
            "GPU Core",
            "GPU PCIe Tx"
        ]);

        result.Should().Be("GPU PCIe Tx");
    }

    [Fact]
    public void SelectGpuUsageSensorName_ShouldPreferD3D3DBeforeGenericGpuCoreLoad()
    {
        var result = SensorsGroupController.SelectGpuUsageSensorName(
        [
            "GPU Core",
            "D3D 3D"
        ]);

        result.Should().Be("D3D 3D");
    }

    [Fact]
    public void SelectGpuUsageSensorName_ShouldRecognizeGpuUtilizationAliases()
    {
        var result = SensorsGroupController.SelectGpuUsageSensorName(
        [
            "GPU Utilization",
            "GPU Memory Controller"
        ]);

        result.Should().Be("GPU Utilization");
    }

    [Fact]
    public void SelectGpuUsageSensorName_ShouldFallbackToUnnamedD3DLoadPrefix()
    {
        var result = SensorsGroupController.SelectGpuUsageSensorName(
        [
            "GPU Memory Controller",
            "D3D "
        ]);

        result.Should().Be("D3D ");
    }

    [Fact]
    public void SelectGpuPowerSensorName_ShouldPreferGpuPackageBeforeGenericPowerNames()
    {
        var result = SensorsGroupController.SelectGpuPowerSensorName(
        [
            "Board Power Draw",
            "GPU Package"
        ]);

        result.Should().Be("GPU Package");
    }

    [Fact]
    public void SelectGpuPowerSensorName_ShouldRecognizeGpuPowerAlias()
    {
        var result = SensorsGroupController.SelectGpuPowerSensorName(
        [
            "Power",
            "GPU Power"
        ]);

        result.Should().Be("GPU Power");
    }

    [Fact]
    public void SelectGpuTemperatureSensorName_ShouldPreferGpuCoreTemperatureAliases()
    {
        var result = SensorsGroupController.SelectGpuTemperatureSensorName(
        [
            "Temperature",
            "GPU Core"
        ]);

        result.Should().Be("GPU Core");
    }

    [Fact]
    public void SelectGpuCoreClockSensorName_ShouldPreferGpuCoreClockAliases()
    {
        var result = SensorsGroupController.SelectGpuCoreClockSensorName(
        [
            "Clock",
            "GPU Core"
        ]);

        result.Should().Be("GPU Core");
    }

    [Fact]
    public void SelectGpuMemoryClockSensorName_ShouldPreferGpuMemoryClockAliases()
    {
        var result = SensorsGroupController.SelectGpuMemoryClockSensorName(
        [
            "Clock",
            "GPU Memory"
        ]);

        result.Should().Be("GPU Memory");
    }

    [Fact]
    public void SelectGpuVoltageSensorName_ShouldPreferGpuCoreVoltage()
    {
        var result = SensorsGroupController.SelectGpuVoltageSensorName(
        [
            "Voltage",
            "GPU Core Voltage"
        ]);

        result.Should().Be("GPU Core Voltage");
    }

    [Fact]
    public void SelectGpuVoltageSensorName_ShouldRecognizeGpuCoreAlias()
    {
        var result = SensorsGroupController.SelectGpuVoltageSensorName(
        [
            "Voltage",
            "GPU Core"
        ]);

        result.Should().Be("GPU Core");
    }

    [Fact]
    public void SelectMemoryUsedSensorName_ShouldRecognizeUsedMemoryAlias()
    {
        var result = SensorsGroupController.SelectMemoryUsedSensorName(
        [
            "Used Memory",
            "GPU Memory Used"
        ]);

        result.Should().Be("Used Memory");
    }

    [Fact]
    public void SelectMemoryAvailableSensorName_ShouldRecognizeFreeMemoryAlias()
    {
        var result = SensorsGroupController.SelectMemoryAvailableSensorName(
        [
            "GPU Memory Free",
            "Free Memory"
        ]);

        result.Should().Be("Free Memory");
    }

    [Fact]
    public void SelectMemoryLoadSensorName_ShouldPreferSystemMemoryLoadOverGpuMemoryLoad()
    {
        var result = SensorsGroupController.SelectMemoryLoadSensorName(
        [
            "GPU Memory",
            "Memory"
        ]);

        result.Should().Be("Memory");
    }

    [Fact]
    public void SelectStorageTemperatureSensorName_ShouldPreferCompositeTemperature()
    {
        var result = SensorsGroupController.SelectStorageTemperatureSensorName(
        [
            "Temperature",
            "Composite"
        ]);

        result.Should().Be("Composite");
    }

    [Fact]
    public void SelectStorageTemperatureSensorName_ShouldRecognizeDriveTemperatureAlias()
    {
        var result = SensorsGroupController.SelectStorageTemperatureSensorName(
        [
            "Drive Temperature",
            "Temperature"
        ]);

        result.Should().Be("Drive Temperature");
    }

    [Fact]
    public void SelectMemoryHardwareName_ShouldPreferTotalMemory()
    {
        var result = SensorsGroupController.SelectMemoryHardwareName(
        [
            "Physical Memory",
            "Total Memory"
        ]);

        result.Should().Be("Total Memory");
    }

    [Fact]
    public void SelectMemoryHardwareName_ShouldFallbackToMemoryNamedHardware()
    {
        var result = SensorsGroupController.SelectMemoryHardwareName(
        [
            "Memory Device",
            "Physical"
        ]);

        result.Should().Be("Memory Device");
    }

    [Fact]
    public void ResolveGpuVramMetrics_WhenUsedIsMissing_ShouldDeriveUsedAndUtilizationFromTotalAndFree()
    {
        var result = SensorsGroupController.ResolveGpuVramMetrics(-1f, 8192f, 7168f);

        result.used.Should().Be(1024f);
        result.total.Should().Be(8192f);
        result.utilization.Should().BeApproximately(12.5f, 0.01f);
    }

    [Fact]
    public void ResolveGpuVramMetrics_WhenTotalIsMissing_ShouldDeriveTotalFromUsedAndFree()
    {
        var result = SensorsGroupController.ResolveGpuVramMetrics(232f, -1f, 7956f);

        result.used.Should().Be(232f);
        result.total.Should().Be(8188f);
        result.utilization.Should().BeApproximately((232f / 8188f) * 100f, 0.01f);
    }

    [Fact]
    public void IsLikelyCpuComponentPowerSensorName_ShouldRecognizeCpuComponentPowerAliases()
    {
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("CPU Cores").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("CPU Core").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("IA Cores").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("CPU Memory").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("DRAM").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("CPU Platform").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("CPU Graphics").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("GT Cores").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("CPU SoC").Should().BeTrue();
        SensorsGroupController.IsLikelyCpuComponentPowerSensorName("GPU Package").Should().BeFalse();
    }

    [Fact]
    public void ResolveCpuPower_WhenPackagePowerMissing_ShouldSumValidComponentPowers()
    {
        var result = SensorsGroupController.ResolveCpuPower(-1f, [24f, 3.5f, 7f]);

        result.Should().BeApproximately(34.5f, 0.001f);
    }

    [Fact]
    public void ResolveCpuPower_WhenPackagePowerExists_ShouldPreferPackagePower()
    {
        var result = SensorsGroupController.ResolveCpuPower(58f, [24f, 3.5f, 7f]);

        result.Should().Be(58f);
    }

    [Fact]
    public void ResolveCpuComponentPowers_ShouldMapCoresMemoryAndPlatform()
    {
        var result = SensorsGroupController.ResolveCpuComponentPowers(
        [
            ("CPU Cores", 24f),
            ("CPU Memory", 3.5f),
            ("CPU Platform", 7f)
        ]);

        result.cores.Should().Be(24f);
        result.memory.Should().Be(3.5f);
        result.platform.Should().Be(7f);
    }

    [Fact]
    public void ResolveCpuComponentPowers_ShouldMapAmdCoreSocAndMemoryAliases()
    {
        var result = SensorsGroupController.ResolveCpuComponentPowers(
        [
            ("CPU Core", 24f),
            ("CPU SoC", 6f),
            ("DRAM", 3f)
        ]);

        result.cores.Should().Be(24f);
        result.memory.Should().Be(3f);
        result.platform.Should().Be(6f);
    }

    [Fact]
    public void ResolveCpuComponentPowers_ShouldMapIntelRaplAliases()
    {
        var result = SensorsGroupController.ResolveCpuComponentPowers(
        [
            ("IA Cores", 28f),
            ("CPU Graphics", 4f),
            ("DRAM", 2f)
        ]);

        result.cores.Should().Be(28f);
        result.memory.Should().Be(2f);
        result.platform.Should().Be(4f);
    }

    [Fact]
    public void ResolveCpuComponentPowers_ShouldMapIntelGtCoresAsPlatformPower()
    {
        var result = SensorsGroupController.ResolveCpuComponentPowers(
        [
            ("IA Cores", 24f),
            ("GT Cores", 3.5f)
        ]);

        result.cores.Should().Be(24f);
        result.platform.Should().Be(3.5f);
    }
}
