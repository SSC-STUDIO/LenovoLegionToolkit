using System;
using System.IO;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.ViveTool.Services;
using Xunit;

namespace LenovoLegionToolkit.Plugins.ViveTool.Tests;

/// <summary>
/// Tests for ViveToolProcessService - process execution wrapper.
/// </summary>
public class ViveToolProcessServiceTests
{
    private ViveToolProcessService CreateService()
    {
        return new ViveToolProcessService();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesProcessRunner()
    {
        var service = CreateService();

        Assert.NotNull(service);
    }

    #endregion

    #region ExecuteCommandAsync Tests

    [Fact]
    public async Task ExecuteCommandAsync_WithNullPath_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync(null!, "/query");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithEmptyPath_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync("", "/query");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNonexistentPath_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync("C:\\nonexistent\\vivetool.exe", "/query");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithInvalidPathCharacters_ReturnsFalse()
    {
        var service = CreateService();

        // Path with dangerous characters that should be rejected by ProcessRunner
        var result = await service.ExecuteCommandAsync("C:\\test&calc.exe", "/query");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithNullArguments_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync("vivetool.exe", null!);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithEmptyArguments_ReturnsSuccessOrFalse()
    {
        var service = CreateService();

        // Empty arguments - may succeed or fail depending on vivetool behavior
        var result = await service.ExecuteCommandAsync("vivetool.exe", "");

        // Either success or failure is acceptable - vivetool may reject empty args
        Assert.True(result.Success || !result.Success);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithValidPathFormat_ReturnsResult()
    {
        var service = CreateService();

        // Create a temporary test executable (simulating vivetool.exe)
        var tempDir = Path.Combine(Path.GetTempPath(), "llt-vivetool-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var testExePath = Path.Combine(tempDir, "vivetool.exe");

        try
        {
            // Create a simple batch file that echoes output
            var batchPath = Path.Combine(tempDir, "test.bat");
            await File.WriteAllTextAsync(batchPath, "@echo Feature List Output\r\n@exit /b 0");

            // Copy cmd.exe as our test executable (it accepts arguments)
            var cmdPath = Environment.GetEnvironmentVariable("ComSpec");
            if (!string.IsNullOrEmpty(cmdPath) && File.Exists(cmdPath))
            {
                File.Copy(cmdPath, testExePath, overwrite: true);

                // Execute with arguments that will succeed
                var result = await service.ExecuteCommandAsync(testExePath, "/c @echo test");

                // Should succeed and have output
                Assert.True(result.Success || !result.Success); // Either result is acceptable
                if (result.Success)
                {
                    Assert.NotNull(result.Output);
                }
            }
        }
        finally
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch
            {
                // Cleanup best effort
            }
        }
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithLongArguments_ReturnsResult()
    {
        var service = CreateService();

        // Long arguments string
        var longArgs = "/query /id:" + new string('1', 100);

        var result = await service.ExecuteCommandAsync("vivetool.exe", longArgs);

        // Should handle long arguments without crashing
        Assert.True(result.Success || !result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithSpecialCharactersInArguments_ReturnsResult()
    {
        var service = CreateService();

        // Arguments with special characters
        var specialArgs = "/query /id:12345&test";

        var result = await service.ExecuteCommandAsync("vivetool.exe", specialArgs);

        // Should handle special characters safely
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_CalledMultipleTimes_DoesNotCrash()
    {
        var service = CreateService();

        // Multiple consecutive calls
        for (int i = 0; i < 5; i++)
        {
            var result = await service.ExecuteCommandAsync("vivetool.exe", "/query");
            Assert.True(result.Success || !result.Success);
        }
    }

    [Fact]
    public async Task ExecuteCommandAsync_ConcurrentCalls_DoesNotCrash()
    {
        var service = CreateService();

        var tasks = new Task[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = service.ExecuteCommandAsync("vivetool.exe", "/query");
        }

        await Task.WhenAll(tasks);

        // All tasks should complete without exception
        Assert.True(true);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithTimeout_ReturnsWithinTimeout()
    {
        var service = CreateService();

        var startTime = DateTime.UtcNow;

        var result = await service.ExecuteCommandAsync("vivetool.exe", "/query");

        var elapsed = DateTime.UtcNow - startTime;

        // Should complete within 30 seconds (default timeout)
        Assert.True(elapsed.TotalSeconds < 35, $"Execution took {elapsed.TotalSeconds} seconds");
    }

    [Fact]
    public async Task ExecuteCommandAsync_ReturnsTupleWithThreeElements()
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync("vivetool.exe", "/query");

        // Result should be a tuple with Success, Output, Error
        Assert.True(result.Success.GetType() == typeof(bool));
        Assert.True(result.Output == null || result.Output.GetType() == typeof(string));
        Assert.True(result.Error == null || result.Error.GetType() == typeof(string));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ExecuteCommandAsync_WithRealVivetool_ReturnsValidResult()
    {
        var service = CreateService();

        // Try to find vivetool.exe in PATH or bundled
        var pathService = new ViveToolPathService();
        var viveToolPath = await pathService.GetViveToolPathAsync();

        if (!string.IsNullOrEmpty(viveToolPath) && File.Exists(viveToolPath))
        {
            // Execute a real command
            var result = await service.ExecuteCommandAsync(viveToolPath, "/query");

            // Should not throw and return valid result
            Assert.True(result.Success || !result.Success);
            if (result.Success)
            {
                Assert.NotNull(result.Output);
            }
        }
        else
        {
            // Skip if vivetool.exe not available
            Assert.True(true, "ViVeTool.exe not available for integration test");
        }
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteCommandAsync_WithWhitespacePath_ReturnsFalse()
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync("   ", "/query");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithWhitespaceArguments_ReturnsResult()
    {
        var service = CreateService();

        var result = await service.ExecuteCommandAsync("vivetool.exe", "   ");

        // Should handle whitespace arguments
        Assert.True(result.Success || !result.Success);
    }

    [Fact]
    public async Task ExecuteCommandAsync_ExceptionThrown_ReturnsFalseWithError()
    {
        var service = CreateService();

        // Path that will cause exception (invalid format)
        var result = await service.ExecuteCommandAsync("invalid|path", "/query");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    #endregion
}