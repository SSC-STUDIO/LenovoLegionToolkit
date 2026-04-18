using Xunit;

namespace LenovoLegionToolkit.Tests.Settings;

[CollectionDefinition("Settings Tests", DisableParallelization = true)]
public class SettingsTestCollection
{
    // This class is never instantiated. It's just a marker for xUnit
    // to disable parallelization for tests in this collection.
}