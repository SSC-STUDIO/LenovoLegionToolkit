using System;
using LenovoLegionToolkit.Plugins.TestCommon;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Tests;

public class ShellIntegrationTextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof(ShellIntegrationText);
    protected override Type ResourceType => typeof(Resources.Resource);
    protected override string[] RequiredKeys =>
    [
        "PluginName",
        "SettingsPageTitle",
        "EnableButton",
        "DisableButton",
        "RegisteredState"
    ];
}
