using System;
using LenovoLegionToolkit.Plugins.TestCommon;
using Xunit;

namespace LenovoLegionToolkit.Plugins.CustomMouse.Tests;

[Collection("CustomMouseResourceCulture")]
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
