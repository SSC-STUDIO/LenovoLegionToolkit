using System;
using LenovoLegionToolkit.Plugins.TestCommon;
using Xunit;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Tests;

public class NetworkAccelerationTextTests : LocalizedTextTestsBase
{
    protected override Type TextType => typeof(NetworkAccelerationText);
    protected override Type ResourceType => typeof(Resources.Resource);
    protected override string[] RequiredKeys =>
    [
        "PluginName",
        "ServiceStateRunning",
        "ServiceStateStopped",
        "ModeBalanced",
        "ModeGaming",
        "StatusServiceStarted",
        "StatusServiceStopped"
    ];

    [Fact]
    public void ChineseResourceFile_ContainsExpectedKeys()
    {
        AssertTranslationCoverage("zh", 0.9);
    }
}
