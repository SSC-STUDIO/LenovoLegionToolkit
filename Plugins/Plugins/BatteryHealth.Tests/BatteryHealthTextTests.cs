using System;
using UniversalDeviceToolkit.Plugins.BatteryHealth.Resources;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.BatteryHealth.Tests;

[Collection("BatteryHealthResourceCulture")]
public sealed class BatteryHealthTextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof(BatteryHealthText);
    protected override Type ResourceType => typeof(Resource);
    protected override string[] RequiredKeys => ["PluginName", "FeaturePageTitle", "SettingsPageTitle"];
}
