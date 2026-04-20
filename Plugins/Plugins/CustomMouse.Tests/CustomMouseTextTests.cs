using System;
using LenovoLegionToolkit.Plugins.TestCommon;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Tests;

public class CustomMouseTextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof(CustomMouseText);
    protected override Type ResourceType => typeof(Resources.Resource);
    protected override string[] RequiredKeys =>
    [
        "PluginName",
        "SettingsPageTitle",
        "DpiLabel",
        "PollingRateLabel",
        "ApplyButton"
    ];
}
