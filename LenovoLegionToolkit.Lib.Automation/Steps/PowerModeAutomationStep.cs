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

            return machineInformation.SupportedPowerModes.Contains(State);
        }
        catch
        {
            return false;
        }
    }

    public override IAutomationStep DeepCopy() => new PowerModeAutomationStep(State);
}
