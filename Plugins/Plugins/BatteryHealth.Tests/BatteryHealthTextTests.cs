using System;
using LenovoLegionToolkit.Plugins.BatteryHealth.Resources;
using LenovoLegionToolkit.Plugins.TestCommon;
using Xunit;

namespace LenovoLegionToolkit.Plugins.BatteryHealth.Tests;

[Collection("BatteryHealthResourceCulture")]
public sealed class BatteryHealthTextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof(BatteryHealthText);
    protected override Type ResourceType => typeof(Resource);
    protected override string[] RequiredKeys => ["PluginName", "FeaturePageTitle", "SettingsPageTitle"];
}
