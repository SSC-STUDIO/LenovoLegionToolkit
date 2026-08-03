using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Guard)]
public sealed class ViewModelMigrationGuardTests
{
    [Fact]
    public void Wpf_ShouldUseSharedKeyboardAndMacroViewModels()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfRoot = Path.Combine(root, "UniversalDeviceToolkit.WPF");
        var sharedRoot = Path.Combine(root, "UniversalDeviceToolkit.ViewModels");

        File.Exists(Path.Combine(wpfRoot, "ViewModels", "KeyboardBacklightViewModel.cs"))
            .Should().BeFalse("the WPF project must not reintroduce a duplicate keyboard ViewModel");
        File.Exists(Path.Combine(wpfRoot, "ViewModels", "MacroViewModel.cs"))
            .Should().BeFalse("the WPF project must not reintroduce a duplicate macro ViewModel");

        File.Exists(Path.Combine(sharedRoot, "KeyboardBacklightViewModel.cs"))
            .Should().BeTrue();
        File.Exists(Path.Combine(sharedRoot, "MacroViewModel.cs"))
            .Should().BeTrue();

        var keyboardPage = File.ReadAllText(Path.Combine(wpfRoot, "Pages", "KeyboardBacklightPage.xaml.cs"));
        var macroPage = File.ReadAllText(Path.Combine(wpfRoot, "Pages", "MacroPage.xaml.cs"));
        keyboardPage.Should().Contain("using UniversalDeviceToolkit.ViewModels;");
        macroPage.Should().Contain("using UniversalDeviceToolkit.ViewModels;");
    }
}
