using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// ViveToolPage/ViveToolSettingsPage resolve localized strings through the static
/// <c>LenovoLegionToolkit.Plugins.ViveTool.Resources.Resource</c> singleton (lazy
/// <c>ResourceManager</c> + mutable <c>Culture</c>). Several ViveTool test classes read those
/// culture-dependent <c>Resource.*</c> properties, so xUnit must never run them concurrently:
/// a half-mutated static culture or a racing lazy cache initialization could leak across reads.
/// Mirrors the <c>DisableParallelization</c> collection already enforced by the other four
/// plugins (BatteryHealth/CustomMouse/NetworkAcceleration/ShellIntegration) per Pillar B.
/// </summary>
[CollectionDefinition("ViveToolResourceCulture", DisableParallelization = true)]
public class ViveToolResourceCultureCollectionDefinition
{
}
