using Xunit;

namespace UniversalDeviceToolkit.Plugins.CustomMouse.Tests;

/// <summary>
/// LocalizedTextTestsBase swings Resources.Resource.Culture through a static; the sibling
/// metadata tests read culture-dependent text properties. ThemeWatcherRuntime and
/// SetAutoThemeCursorStyle subscribe to the process-wide SystemEvents.UserPreferenceChanged
/// static. Put these classes in the same non-parallel collection so xUnit never runs them
/// concurrently and a half-mutated static can never leak across Plugin_HasExpectedMetadata
/// or leave a live UserPreferenceChanged handler for another test.
/// </summary>
[CollectionDefinition("CustomMouseResourceCulture", DisableParallelization = true)]
public class CustomMouseResourceCultureCollectionDefinition
{
}
