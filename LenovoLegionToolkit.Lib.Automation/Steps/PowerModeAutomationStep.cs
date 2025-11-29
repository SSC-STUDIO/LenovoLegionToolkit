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

            if (machineInformation.SupportedPowerModes is null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"SupportedPowerModes is null, PowerModeAutomationStep is not supported.");
                return false;
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
