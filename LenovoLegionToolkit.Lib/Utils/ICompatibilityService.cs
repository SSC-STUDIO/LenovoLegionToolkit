using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Utils;

/// <summary>
/// Abstract interface for compatibility checking operations
/// </summary>
public interface ICompatibilityService
{
    /// <summary>
    /// Get machine information including all hardware capabilities
    /// </summary>
    Task<MachineInformation> GetMachineInformationAsync();

    /// <summary>
    /// Check if the machine is a supported Legion device
    /// </summary>
    bool IsSupportedLegionMachine(MachineInformation machineInformation);

    /// <summary>
    /// Check basic compatibility (WMI availability)
    /// </summary>
    Task<bool> CheckBasicCompatibilityAsync();

    /// <summary>
    /// Check full compatibility with machine information
    /// </summary>
    Task<(bool isCompatible, MachineInformation machineInformation)> IsCompatibleAsync();
}