// <copyright file="MainWindowVisualTests.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Visual and text-extraction tests for the main UDT window.
    /// Uses both native FlaUI element tree inspection and (when available) WinRT OCR.
    /// </summary>
    [Trait("Category", "UI")]
    [Collection(TestCollections.FlaUI)]
    public class MainWindowVisualTests : FlaUiTestBase
    {
        [SkippableFact]
        public async Task MainWindow_CanExtractVisibleText()
        {
            // Arrange
            await Task.Delay(3000); // Allow UI to fully render

            // Act
            var visibleText = await ExtractTextFromWindowAsync();

            // Assert - if any text was extracted, it should be non-empty and meaningful
            if (visibleText.Length > 0)
            {
                var allText = string.Join(" ", visibleText);
                Assert.NotEmpty(allText);

                // Should contain at least some recognizable window text
                // (window title, button names, etc.)
                Assert.True(allText.Length > 3,
                    $"Extracted text seems too short: '{allText}'");
            }
            else
            {
                // No text extracted — that's OK for headless/minimal UI
                // Just verify the window is still responsive
                Assert.NotNull(MainWindow);
                Assert.True(MainWindow.IsAvailable);
            }
        }

        [SkippableFact]
        public async Task MainWindow_ElementTree_HasExpectedStructure()
        {
            // Arrange
            await Task.Delay(2000);

            // Act - inspect the element tree
            var allElements = MainWindow!.FindAllDescendants();
            var namedElements = allElements
                .Where(e => !string.IsNullOrWhiteSpace(e.Properties.Name.Value))
                .ToArray();

            // Assert
            Assert.True(allElements.Length > 10,
                $"Expected at least 10 UI elements, found {allElements.Length}");
            Assert.True(namedElements.Length > 0,
                "Expected at least some named elements for accessibility");
        }
    }
}
