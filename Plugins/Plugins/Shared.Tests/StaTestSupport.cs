using System;
using System.Threading;
using Xunit;

namespace LenovoLegionToolkit.Plugins.Shared.Tests;

/// <summary>
/// Collection definition for STA-thread tests required by WPF.
/// </summary>
[CollectionDefinition("STA", DisableParallelization = true)]
public class StaCollectionDefinition
{
    // This class is never instantiated. It's just a marker for the collection.
}

/// <summary>
/// StaFact attribute for tests that require STA thread context for WPF.
/// </summary>
public class StaFactAttribute : FactAttribute
{
    // xUnit doesn't have built-in STA support, so we need to use a custom wrapper
}