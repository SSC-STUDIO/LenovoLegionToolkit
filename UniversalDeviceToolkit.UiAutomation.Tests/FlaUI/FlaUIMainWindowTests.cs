// <copyright file="FlaUIMainWindowTests.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// FlaUI tests that verify the UDT main window UI elements are correctly rendered.
    /// These tests require a running desktop session and administrator privileges.
    /// </summary>
    [Trait("Category", "UI.MainWindow")]
    [Collection(TestCollections.FlaUI)]
    public class FlaUIMainWindowTests : FlaUiTestBase
    {
        [SkippableFact]
        public void AppLaunches_AndMainWindowAppears()
        {
            Assert.NotNull(MainWindow);
            Assert.False(App!.HasExited);
            Assert.Contains("Universal Device Toolkit", MainWindow!.Properties.Name.Value, StringComparison.OrdinalIgnoreCase);
        }

        [SkippableFact]
        public void MainWindow_HasExpectedStructure()
        {
            var children = MainWindow!.FindAllDescendants();
            Assert.True(children.Length > 0,
                "Main window should have at least one child element (navigation, content, etc.)");
        }

        [SkippableFact]
        public async Task MainWindow_ContainsNativeWindowText()
        {
            var texts = await ExtractTextFromWindowAsync();
            Assert.NotEmpty(texts);
            Assert.Contains(
                texts,
                text => text.Contains("Universal Device Toolkit", StringComparison.OrdinalIgnoreCase));
        }
    }
}
