// <copyright file="FlaUIMainWindowTests.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// FlaUI tests that verify the UDT main window UI elements are correctly rendered.
    /// These tests require a running desktop session and administrator privileges.
    /// </summary>
    [Trait("Category", "UI.MainWindow")]
    [Collection("FlaUI Tests")]
    public class FlaUIMainWindowTests : FlaUiTestBase
    {
        [SkippableFact]
        public async Task AppLaunches_AndMainWindowAppears()
        {
            // Arrange + Act: InitializeAsync() in base class launches the app
            await InitializeAsync();

            // Assert: main window should be found and not closed
            Assert.NotNull(MainWindow);
            Assert.False(App!.HasExited);
            Assert.True(MainWindow!.Properties.Name.Value.Length > 0);
        }

        [SkippableFact]
        public async Task MainWindow_HasExpectedStructure()
        {
            await InitializeAsync();

            // The main window should have child elements (navigation, content, etc.)
            var children = MainWindow!.FindAllDescendants();
            Assert.True(children.Length > 0,
                "Main window should have at least one child element (navigation, content, etc.)");
        }

        [SkippableFact]
        public async Task MainWindow_CanBeVerifiedWithOcr()
        {
            await InitializeAsync();

            // Use WinRT OCR (or element tree) to verify visible text
            var texts = await ExtractTextFromWindowAsync();
            var allText = string.Join(" ", texts);

            // Should contain at least some recognizable text from the UI
            Assert.True(texts.Length > 0 || allText.Length > 0,
                "Should be able to extract some text from the main window (either via element tree or OCR)");
        }
    }
}
