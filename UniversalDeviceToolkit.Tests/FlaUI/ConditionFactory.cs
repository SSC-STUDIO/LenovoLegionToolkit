// <copyright file="ConditionFactory.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System;
using FlaUI.Core.Conditions;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Provides convenience static access to common FlaUI conditions.
    /// In FlaUI 5, the recommended API is to use the built-in ConditionFactory
    /// via the lambda pattern (e.g., c => c.ByAutomationId("foo")) on AutomationElement methods.
    /// This class provides the "True" condition for matching all elements.
    /// </summary>
    internal static class ConditionFactory
    {
        /// <summary>
        /// Condition that matches all elements (always true).
        /// In FlaUI 5, ConditionBase has no static True — we use an empty AndCondition.
        /// </summary>
        public static ConditionBase TrueCondition { get; } = new AndCondition(Array.Empty<ConditionBase>());
    }
}
