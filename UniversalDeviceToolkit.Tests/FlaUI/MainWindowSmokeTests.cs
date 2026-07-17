// <copyright file="MainWindowSmokeTests.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using FlaUI.Core.Definitions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Smoke tests for the main UDT window UI.
    /// These tests require a built UDT application to be available.
    /// Run with: dotnet test --filter "FullyQualifiedName~FlaUI.MainWindowSmokeTests"
    ///
    /// IMPORTANT: These tests launch the actual WPF application and interact with the UI.
    /// They are designed to run on a developer machine with UDT built, not in CI.
    /// The CI environment check in FlaUiTestBase will auto-skip these.
    /// </summary>
    [Trait("Category", "UI")]
    [Collection(TestCollections.FlaUI)]
    public class MainWindowSmokeTests : FlaUiTestBase
    {
        [SkippableFact]
        public async Task MainWindow_Launches_Successfully()
        {
            // Assert
            Assert.NotNull(MainWindow);
            Assert.False(App!.HasExited);

            // Verify the window title contains "Universal Device Toolkit"
            var title = MainWindow.Title;
            Assert.Contains("Universal Device Toolkit", title, System.StringComparison.OrdinalIgnoreCase);
        }

        [SkippableFact]
        public async Task MainWindow_Is_Visible_And_Interactive()
        {
            // Assert
            Assert.NotNull(MainWindow);
            Assert.True(MainWindow.IsAvailable);

            // Window should be visible (not offscreen)
            var isOffscreen = MainWindow.Properties.IsOffscreen.Value;
            Assert.False(isOffscreen, "Main window should be visible (not offscreen)");
        }

        [SkippableFact]
        public async Task MainWindow_Contains_Navigation_Or_Content()
        {
            // Arrange
            await Task.Delay(2000); // Allow UI to fully load

            // Act - find all descendant elements
            var allElements = MainWindow!.FindAllDescendants();

            // Assert - there should be multiple UI elements
            Assert.NotNull(allElements);
            Assert.True(allElements.Length > 0, "Main window should contain UI elements");
        }

        [SkippableFact]
        public async Task MainWindow_Has_Title_Element()
        {
            // Arrange
            await Task.Delay(2000);

            // Act - find all named elements (accessibility check)
            var allElements = MainWindow!.FindAllDescendants();
            var namedElements = allElements
                .Where(e => !string.IsNullOrWhiteSpace(e.Properties.Name.Value))
                .ToArray();

            // Assert
            Assert.True(allElements.Length > 0, "Should find UI elements in the window");
            Assert.True(namedElements.Length > 0,
                "Should find at least some named elements for accessibility");
        }

        [SkippableFact]
        public async Task MainWindow_CloseButton_Exists()
        {
            // Arrange
            await Task.Delay(2000);

            // Act - try to find button controls
            // Standard windows always have at least close/minimize/maximize
            var buttonElements = MainWindow!.FindAllDescendants(c =>
                c.ByControlType(ControlType.Button));

            // Assert
            Assert.NotNull(buttonElements);
            Assert.True(buttonElements.Length > 0,
                $"Main window should have button controls, found {buttonElements.Length}");
        }
    }
}
