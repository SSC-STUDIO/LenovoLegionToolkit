using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Windows;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaInputDialogContractTests
{
    [Theory]
    [InlineData(null, false, false)]
    [InlineData("", false, false)]
    [InlineData("   ", false, false)]
    [InlineData("value", false, true)]
    [InlineData(null, true, true)]
    [InlineData("", true, true)]
    [InlineData("   ", true, true)]
    [InlineData("value", true, true)]
    public void ValidateInput_ShouldFollowAllowEmptyContract(string? text, bool allowEmpty, bool expected) =>
        AvaloniaInputDialogWindow.IsValidInput(text, allowEmpty).Should().Be(expected);

    [Fact]
    public void AvaloniaHost_ShouldOfferDependencyFreeInputDialogContract()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Windows",
            "AvaloniaInputDialogWindow.cs"));

        source.Should().Contain("AvaloniaInputDialogWindow");
        source.Should().Contain("InputText");
        source.Should().Contain("IsDefault = true");
        source.Should().Contain("IsCancel = true");
        source.Should().Contain("IsValidInput");
        source.Should().Contain("allowEmpty");
        source.Should().Contain("AvaloniaLocalization.GetString");
        source.Should().Contain("Task<string?>");
        source.Should().NotContain("UniversalDeviceToolkit.WPF");
        source.Should().NotContain("IoCContainer");
    }
}
