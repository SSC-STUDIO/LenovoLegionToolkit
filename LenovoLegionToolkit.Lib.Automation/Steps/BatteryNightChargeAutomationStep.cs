using System.Text.Json.Serialization;

namespace LenovoLegionToolkit.Lib.Automation.Steps;

[method: JsonConstructor]
public class BatteryNightChargeAutomationStep(BatteryNightChargeState state)
    : AbstractFeatureAutomationStep<BatteryNightChargeState>(state)
{
    public override IAutomationStep DeepCopy() => new BatteryNightChargeAutomationStep(State);
}
