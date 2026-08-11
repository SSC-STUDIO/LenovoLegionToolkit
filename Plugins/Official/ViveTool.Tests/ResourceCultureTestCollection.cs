using Xunit;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Several ViveTool test classes read culture-dependent <c>Resource.*</c> properties through
/// the static <c>UniversalDeviceToolkit.Plugins.ViveTool.Resources.Resource</c> singleton (lazy
/// <c>ResourceManager</c> + mutable <c>Culture</c>), so xUnit must never run them concurrently:
/// a half-mutated static culture or a racing lazy cache initialization could leak across reads.
/// Mirrors the <c>DisableParallelization</c> collection already enforced by the other four
/// plugins (CustomMouse/ShellIntegration/ViveTool) per Pillar B.
/// </summary>
[CollectionDefinition("ViveToolResourceCulture", DisableParallelization = true)]
public class ViveToolResourceCultureCollectionDefinition
{
}
