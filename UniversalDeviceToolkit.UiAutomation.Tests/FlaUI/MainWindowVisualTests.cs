// <copyright file="MainWindowVisualTests.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Visual and text-extraction tests for the main UDT window.
    /// Text assertions use native UI Automation properties only.
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

            Assert.NotEmpty(visibleText);
            var extractedText = string.Join(" ", visibleText);
            Assert.Contains("Universal Device Toolkit", extractedText, StringComparison.OrdinalIgnoreCase);

            var allText = string.Join(" ", visibleText);
            Assert.NotEmpty(allText);
            Assert.True(allText.Length > 3,
                $"Extracted text seems too short: '{allText}'");
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
