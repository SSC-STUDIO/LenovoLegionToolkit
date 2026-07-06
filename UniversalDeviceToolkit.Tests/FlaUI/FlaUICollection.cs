// <copyright file="FlaUICollection.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Collection definition for FlaUI tests.
    /// All FlaUI tests share this collection to prevent parallel execution,
    /// since they interact with a single running UDT application instance.
    /// </summary>
    [CollectionDefinition("FlaUI Tests")]
    public class FlaUICollection
    {
        // This class has no code; it's just to define the collection
    }
}
