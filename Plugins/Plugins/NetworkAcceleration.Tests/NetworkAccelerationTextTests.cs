using System;
using UniversalDeviceToolkit.Plugins.TestCommon;
using Xunit;

namespace UniversalDeviceToolkit.Plugins.NetworkAcceleration.Tests;

[Collection("NetworkAccelerationResourceCulture")]
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
