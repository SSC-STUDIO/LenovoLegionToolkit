using System;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using Newtonsoft.Json;

namespace LenovoLegionToolkit.Lib.Automation.Steps;

[method: JsonConstructor]
public class PowerModeAutomationStep(PowerModeState state)
    : AbstractFeatureAutomationStep<PowerModeState>(state)
{
    public override async Task<bool> IsSupportedAsync()
    {
        if (!await base.IsSupportedAsync().ConfigureAwait(false))
            return false;

        try
        {
            var (_, machineInformation) = await Compatibility.IsCompatibleAsync().ConfigureAwait(false);
            
            if (!Compatibility.IsSupportedLegionMachine(machineInformation))
                return false;

            // For backward compatibility, when SupportedPowerModes is null, allow Quiet, Balance, and Performance
            // This matches the behavior in PowerModeFeature.GetAllStatesAsync() which always includes these three modes
            if (machineInformation.SupportedPowerModes is null)
            {
                // Quiet, Balance, and Performance are always available when SupportedPowerModes is null (backward compatibility)
                // GodMode requires explicit support check
                return State is PowerModeState.Quiet or PowerModeState.Balance or PowerModeState.Performance;
            }

            return machineInformation.SupportedPowerModes.Contains(State);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception occurred while checking PowerModeAutomationStep support.", ex);
            return false;
        }
    }

    public override IAutomationStep DeepCopy() => new PowerModeAutomationStep(State);
}
