using System;
using System.Collections.Generic;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.ViewModels;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Security)]
public sealed class WindowsOptimizationElevationTests
{
    [Fact]
    public void WorkerArguments_ShouldAcceptOnlyGeneratedPipeAndTokenValues()
    {
        var pipeName = $"udt-optimization-{Guid.NewGuid():N}";
        var token = new string('A', 64);
        var arguments = new[]
        {
            ElevatedOptimizationWorker.WorkerSwitch,
            ElevatedOptimizationWorker.PipeSwitch,
            pipeName,
            ElevatedOptimizationWorker.TokenSwitch,
            token
        };

        ElevatedOptimizationWorker.TryParseArguments(arguments, out var parsedPipe, out var parsedToken)
            .Should().BeTrue();
        parsedPipe.Should().Be(pipeName);
        parsedToken.Should().Be(token);
    }

    [Theory]
    [InlineData("--udt-elevated-optimization", "bad-pipe", "bad-token")]
    [InlineData("--unexpected-worker", "udt-optimization-00000000000000000000000000000000", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void WorkerArguments_ShouldRejectUntrustedValues(string switchValue, string pipeName, string token)
    {
        var arguments = new[]
        {
            switchValue,
            ElevatedOptimizationWorker.PipeSwitch,
            pipeName,
            ElevatedOptimizationWorker.TokenSwitch,
            token
        };

        ElevatedOptimizationWorker.TryParseArguments(arguments, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void BuildOptimizationOperations_ShouldMapUncheckToReverseExecution()
    {
        var action = CreateAction("performance.memory");
        action.IsApplied = true;
        action.IsSelected = false;

        var operations = WindowsOptimizationViewModel.BuildOptimizationOperations([action]);

        operations.Should().ContainSingle();
        operations[0].ActionKey.Should().Be("performance.memory");
        operations[0].Apply.Should().BeFalse();
        operations[0].VerificationActionKey.Should().Be("performance.memory");
        operations[0].ExpectedAppliedState.Should().BeFalse();
    }

    [Fact]
    public void BuildOptimizationOperations_ShouldBatchMultipleChanges()
    {
        var first = CreateAction("explorer.taskbar");
        first.IsApplied = false;
        first.IsSelected = true;

        var second = CreateAction("performance.memory");
        second.IsApplied = true;
        second.IsSelected = false;

        var operations = WindowsOptimizationViewModel.BuildOptimizationOperations([first, second]);

        operations.Should().HaveCount(2);
        operations[0].Apply.Should().BeTrue();
        operations[1].Apply.Should().BeFalse();
    }

    private static OptimizationActionViewModel CreateAction(string key)
    {
        var definition = new WindowsOptimizationActionDefinition(
            key,
            "title",
            "description",
            _ => System.Threading.Tasks.Task.CompletedTask,
            Recommended: true,
            IsAppliedAsync: _ => System.Threading.Tasks.Task.FromResult(true),
            RollbackAsync: _ => System.Threading.Tasks.Task.CompletedTask);
        return new OptimizationActionViewModel(definition, key, key, "Recommended");
    }
}
