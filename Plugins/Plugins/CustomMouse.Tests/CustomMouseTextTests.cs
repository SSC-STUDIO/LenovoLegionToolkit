using System;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.CustomMouse.Tests;

[Collection("CustomMouseResourceCulture")]
public class CustomMouseTextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof(CustomMouseText);
    protected override Type ResourceType => typeof(Resources.Resource);
    protected override string[] RequiredKeys =>
    [
        "PluginName",
        "SettingsPageTitle",
        "ApplyButton"
    ];
}
