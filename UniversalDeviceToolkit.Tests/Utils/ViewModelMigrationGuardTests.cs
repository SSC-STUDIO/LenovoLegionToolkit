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

    [Fact]
    public void Avalonia_Workspaces_ShouldUseSharedViewModelsAndKeepHostAdaptersSeparate()
    {
        var root = RepositoryPaths.FindRoot();
        var sharedRoot = Path.Combine(root, "UniversalDeviceToolkit.ViewModels");
        var avaloniaRoot = Path.Combine(root, "UniversalDeviceToolkit.Avalonia");

        File.Exists(Path.Combine(sharedRoot, "AutomationWorkspaceViewModel.cs"))
            .Should().BeTrue();

        File.ReadAllText(Path.Combine(avaloniaRoot, "Pages", "AutomationPage.axaml.cs"))
            .Should().Contain("AutomationWorkspaceViewModel");
        File.ReadAllText(Path.Combine(avaloniaRoot, "Pages", "KeyboardBacklightPage.axaml.cs"))
            .Should().Contain("KeyboardBacklightViewModel");
        File.ReadAllText(Path.Combine(avaloniaRoot, "Pages", "MacroPage.cs"))
            .Should().Contain("MacroViewModel");

        File.ReadAllText(Path.Combine(avaloniaRoot, "Services", "SharedWorkspaceAdapters.cs"))
            .Should().NotContain("UniversalDeviceToolkit.WPF");
    }
}
