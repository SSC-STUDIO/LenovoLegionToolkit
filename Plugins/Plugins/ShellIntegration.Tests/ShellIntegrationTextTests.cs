using System;
using LenovoLegionToolkit.Plugins.TestCommon;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ShellIntegration.Tests;

[Collection("ShellIntegrationResourceCulture")]
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
