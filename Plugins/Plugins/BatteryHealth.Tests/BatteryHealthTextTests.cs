using LenovoLegionToolkit.Plugins.BatteryHealth.Resources;
using LenovoLegionToolkit.Plugins.TestCommon;

namespace LenovoLegionToolkit.Plugins.BatteryHealth.Tests;

public sealed class BatteryHealthTextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof(BatteryHealthText);
    protected override Type ResourceType => typeof(Resource);
    protected override string[] RequiredKeys => ["PluginName", "FeaturePageTitle", "SettingsPageTitle"];
}