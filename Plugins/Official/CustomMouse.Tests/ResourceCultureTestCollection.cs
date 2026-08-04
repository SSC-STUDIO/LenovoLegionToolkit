using Xunit;

namespace UniversalDeviceToolkit.Plugins.CustomMouse.Tests;

/// <summary>
/// LocalizedTextTestsBase swings Resources.Resource.Culture through a static; the sibling
/// metadata tests read culture-dependent text properties. Put both classes in the same
/// non-parallel collection so xUnit never runs them concurrently and a half-mutated static
/// can never leak across Plugin_HasExpectedMetadata's reads.
/// </summary>
[CollectionDefinition("CustomMouseResourceCulture", DisableParallelization = true)]
public class CustomMouseResourceCultureCollectionDefinition
{
}
