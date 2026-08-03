using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class OptimizationActionViewModelTests
{
    [Fact]
    public void IsSelected_ShouldOnlyChangePendingState()
    {
        var action = CreateAction();
        action.IsApplied = true;

        action.IsSelected = false;

        action.IsApplied.Should().BeTrue();
        action.IsSelected.Should().BeFalse();
        action.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void IsApplied_ShouldProvideTheBaselineForCancel()
    {
        var action = CreateAction();
        action.IsApplied = false;
        action.IsSelected = true;

        action.IsSelected = action.IsApplied.Value;

        action.IsDirty.Should().BeFalse();
        action.IsSelected.Should().BeFalse();
    }

    [Fact]
    public void CheckState_ShouldBeIndeterminateWhenMachineStateIsUnknown()
    {
        var action = CreateAction();

        action.IsApplied.Should().BeNull();
        action.IsStateKnown.Should().BeFalse();
        action.AllowsIndeterminate.Should().BeTrue();
        action.CheckState.Should().BeNull();

        action.IsSelected = true;
        action.IsApplied = true;

        action.IsStateKnown.Should().BeTrue();
        action.AllowsIndeterminate.Should().BeFalse();
        action.CheckState.Should().BeTrue();
    }

    private static OptimizationActionViewModel CreateAction()
    {
        var definition = new WindowsOptimizationActionDefinition(
            "system.test.enable",
            "Title",
            "Description",
            _ => Task.CompletedTask,
            recommended: true,
            isAppliedAsync: _ => Task.FromResult(false));

        return new OptimizationActionViewModel(definition, "Title", "Description", "Recommended");
    }
}
