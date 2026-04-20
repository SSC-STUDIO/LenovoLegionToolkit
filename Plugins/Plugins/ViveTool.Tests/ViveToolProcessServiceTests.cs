using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Tests for ViveToolProcessService - process execution wrapper.
/// </summary>
public class ViveToolProcessServiceTests
{
    [Fact]
    public void Constructor_CreatesProcessRunner()
    {
        var service = CreateService();

        Assert.NotNull(service);
    }

    [Theory]
    [InlineData(null, "/query")]
    [InlineData("", "/query")]
    [InlineData("   ", "/query")]
    [InlineData("C:\\nonexistent\\vivetool.exe", "/query")]
    [InlineData("C:\\test&calc.exe", "/query")]
    [InlineData("invalid|path", "/query")]
    [InlineData("vivetool.exe", null)]
    public async Task ExecuteCommandAsync_WithRejectedInput_ReturnsFalseWithError(string? path, string? arguments)
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync(path!, arguments!);

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithDangerousArguments_ReturnsFalseWithError()
    {
        var service = CreateService();
        var commandProcessorPath = GetCommandProcessorPath();

        var result = await service.ExecuteCommandAsync(commandProcessorPath, "/c echo safe&echo unsafe");

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithExistingExecutable_ReturnsCapturedOutput()
    {
        var service = CreateService();
        var commandProcessorPath = GetCommandProcessorPath();

        var result = await service.ExecuteCommandAsync(commandProcessorPath, "/c echo Feature List Output");

        Assert.True(result.Success);
        Assert.Contains("Feature List Output", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public async Task ExecuteCommandAsync_CalledMultipleTimes_ReturnsSuccessfulOutputEachTime()
    {
        var service = CreateService();
        var commandProcessorPath = GetCommandProcessorPath();

        for (var i = 0; i < 5; i++)
        {
            var result = await service.ExecuteCommandAsync(commandProcessorPath, $"/c echo run-{i}");

            Assert.True(result.Success);
            Assert.Contains($"run-{i}", result.Output, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ExecuteCommandAsync_ConcurrentCalls_AllCompleteSuccessfully()
    {
        var service = CreateService();
        var commandProcessorPath = GetCommandProcessorPath();

        var tasks = Enumerable.Range(0, 5)
            .Select(index => service.ExecuteCommandAsync(commandProcessorPath, $"/c echo concurrent-{index}"))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(
            results.Select((result, index) => (result, index)),
            item =>
            {
                Assert.True(item.result.Success);
                Assert.Contains($"concurrent-{item.index}", item.result.Output, StringComparison.OrdinalIgnoreCase);
            });
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithMissingExecutable_FailsFast()
    {
        var service = CreateService();
        var startTime = DateTime.UtcNow;

        var result = await service.ExecuteCommandAsync("C:\\nonexistent\\vivetool.exe", "/query");

        var elapsed = DateTime.UtcNow - startTime;

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        Assert.True(elapsed.TotalSeconds < 5, $"Execution took {elapsed.TotalSeconds} seconds");
    }

    [Fact]
    public async Task ExecuteCommandAsync_ReturnsExpectedTupleShape()
    {
        var service = CreateService();
        var commandProcessorPath = GetCommandProcessorPath();

        var result = await service.ExecuteCommandAsync(commandProcessorPath, "/c echo tuple-shape");

        Assert.IsType<bool>(result.Success);
        Assert.IsType<string>(result.Output);
        Assert.IsType<string>(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithResolvedViveToolPath_ReturnsStructuredResult()
    {
        var service = CreateService();
        var pathService = new ViveToolPathService();
        var viveToolPath = await pathService.GetViveToolPathAsync();

        Assert.False(string.IsNullOrWhiteSpace(viveToolPath));
        Assert.True(File.Exists(viveToolPath));

        var result = await service.ExecuteCommandAsync(viveToolPath!, "/query");

        if (result.Success)
            Assert.False(string.IsNullOrWhiteSpace(result.Output));
        else
            Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    private static ViveToolProcessService CreateService()
    {
        return new ViveToolProcessService();
    }

    private static string GetCommandProcessorPath()
    {
        var commandProcessorPath = Environment.GetEnvironmentVariable("ComSpec");
        Assert.False(string.IsNullOrWhiteSpace(commandProcessorPath));
        Assert.True(File.Exists(commandProcessorPath));
        return commandProcessorPath!;
    }
}
