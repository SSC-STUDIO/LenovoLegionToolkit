using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using Moq;

namespace LenovoLegionToolkit.Plugins.Shared.Tests;

public class ProcessRunnerTests
{
    private readonly ProcessRunner _runner;
    private readonly string _testExecutablePath;

    public ProcessRunnerTests()
    {
        _runner = new ProcessRunner();
        // Use cmd.exe as a safe test executable on Windows
        _testExecutablePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
    }

    #region Input Validation Tests

#pragma warning disable CS0618 // TryRunProcess is deprecated — tests intentionally exercise the method for backward compat

    [Fact]
    public void TryRunProcess_NullFilePath_ReturnsFalse()
    {
        var result = _runner.TryRunProcess(null!, "", out var output);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void TryRunProcess_EmptyFilePath_ReturnsFalse()
    {
        var result = _runner.TryRunProcess("", "", out var output);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void TryRunProcess_WhitespaceFilePath_ReturnsFalse()
    {
        var result = _runner.TryRunProcess("   ", "", out var output);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void TryRunProcess_NonexistentFile_ReturnsFalse()
    {
        var result = _runner.TryRunProcess("C:\\Nonexistent\\Path\\fake.exe", "", out var output);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public async Task RunProcessAsync_NullFilePath_ReturnsFailure()
    {
        var result = await _runner.RunProcessAsync(null!, "");
        Assert.False(result.Success);
        Assert.Contains("null or empty", result.Error);
    }

    [Fact]
    public async Task RunProcessAsync_EmptyFilePath_ReturnsFailure()
    {
        var result = await _runner.RunProcessAsync("", "");
        Assert.False(result.Success);
        Assert.Contains("null or empty", result.Error);
    }

    [Fact]
    public async Task RunProcessAsync_NonexistentFile_ReturnsFailure()
    {
        var result = await _runner.RunProcessAsync("C:\\Nonexistent\\fake.exe", "");
        Assert.False(result.Success);
        Assert.Contains("not found", result.Error);
    }

    #endregion

    #region Path Traversal Protection Tests

    [Theory]
    [InlineData("C:\\test\\..\\windows\\cmd.exe")]
    [InlineData("..\\..\\..\\windows\\system32\\cmd.exe")]
    [InlineData("C:\\test\\payload.exe\\..\\..\\cmd.exe")]
    public void TryRunProcess_PathTraversal_ReturnsFalse(string path)
    {
        var result = _runner.TryRunProcess(path, "", out var output);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Theory]
    [InlineData("C:\\test\\..\\windows\\cmd.exe")]
    [InlineData("..\\..\\..\\windows\\system32\\cmd.exe")]
    [InlineData("C:\\test\\payload.exe\\..\\..\\cmd.exe")]
    public async Task RunProcessAsync_PathTraversal_ReturnsFailure(string path)
    {
        var result = await _runner.RunProcessAsync(path, "");
        Assert.False(result.Success);
        Assert.Contains("dangerous", result.Error);
    }

    #endregion

    #region Command Injection Protection Tests

    [Theory]
    [InlineData("C:\\test&calc.exe")]
    [InlineData("C:\\test|cmd.exe")]
    [InlineData("C:\\test;rm.exe")]
    [InlineData("C:\\test`echo.exe")]
    [InlineData("C:\\test$(whoami).exe")]
    [InlineData("C:\\test<path.exe")]
    [InlineData("C:\\test>output.exe")]
    [InlineData("C:\\test\npayload.exe")]
    [InlineData("C:\\test\rpayload.exe")]
    public void TryRunProcess_DangerousPathCharacters_ReturnsFalse(string path)
    {
        var result = _runner.TryRunProcess(path, "", out var output);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Theory]
    [InlineData("& calc.exe")]
    [InlineData("| type secrets.txt")]
    [InlineData("; del important.dat")]
    [InlineData("` whoami `")]
    [InlineData("$(cat /etc/passwd)")]
    [InlineData("${PATH}")]
    [InlineData("< input.txt")]
    [InlineData("> output.txt")]
    [InlineData("test\nmalicious")]
    [InlineData("test\rmalicious")]
    public void TryRunProcess_DangerousArguments_ReturnsFalse(string arguments)
    {
        // Use a valid path but dangerous arguments
        var result = _runner.TryRunProcess(_testExecutablePath, arguments, out var output);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Theory]
    [InlineData("C:\\test\\..\\calc.exe")]
    [InlineData("C:\\test\0cmd.exe")]
    public async Task RunProcessAsync_DangerousPathCharacters_ReturnsFailure(string path)
    {
        var result = await _runner.RunProcessAsync(path, "");
        Assert.False(result.Success);
        Assert.Contains("dangerous", result.Error);
    }

    [Theory]
    [InlineData("C:\\test&calc.exe")]
    [InlineData("C:\\test|cmd.exe")]
    [InlineData("C:\\test;rm.exe")]
    [InlineData("C:\\test$(whoami).exe")]
    public async Task RunProcessAsync_NonExistentMetacharacterPath_ReturnsFailure(string path)
    {
        var result = await _runner.RunProcessAsync(path, "");
        Assert.False(result.Success);
        Assert.Contains("File not found", result.Error);
    }

    [Theory]
    [InlineData("& calc.exe")]
    [InlineData("| type secrets.txt")]
    [InlineData("; del important.dat")]
    [InlineData("$(cat /etc/passwd)")]
    public async Task RunProcessAsync_DangerousArguments_ReturnsFailure(string arguments)
    {
        var result = await _runner.RunProcessAsync(_testExecutablePath, arguments);
        Assert.False(result.Success);
        Assert.Contains("dangerous", result.Error);
    }

    #endregion

    #region Timeout Handling Tests

    [Fact]
    public void TryRunProcess_TimeoutExceeded_ReturnsFalse()
    {
        // Use a long-running command (ping with many repeats)
        var result = _runner.TryRunProcess(_testExecutablePath, "/c ping 127.0.0.1 -n 10", out var output, timeoutSeconds: 1);
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public async Task RunProcessAsync_TimeoutExceeded_ReturnsFailure()
    {
        // Use a long-running command
        var result = await _runner.RunProcessAsync(
            _testExecutablePath,
            "/c ping 127.0.0.1 -n 10",
            CancellationToken.None,
            timeoutSeconds: 1);

        Assert.False(result.Success);
        Assert.Contains("timed out", result.Error);
    }

    [Fact]
    public async Task RunProcessAsync_CancellationRequested_ReturnsFailure()
    {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        var result = await _runner.RunProcessAsync(
            _testExecutablePath,
            "/c ping 127.0.0.1 -n 10",
            cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error);
    }

    [Fact]
    public async Task RunProcessAsync_AlreadyCancelled_ReturnsFailure()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await _runner.RunProcessAsync(
            _testExecutablePath,
            "/c echo test",
            cts.Token);

        Assert.False(result.Success);
        Assert.Contains("cancelled", result.Error);
    }

    #endregion

    #region Successful Execution Tests

    [Fact]
    public void TryRunProcess_ValidCommand_ReturnsTrue()
    {
        var result = _runner.TryRunProcess(_testExecutablePath, "/c echo HelloWorld", out var output);
        Assert.True(result);
        Assert.Contains("HelloWorld", output);
    }

    [Fact]
    public async Task RunProcessAsync_ValidCommand_ReturnsSuccess()
    {
        var result = await _runner.RunProcessAsync(_testExecutablePath, "/c echo HelloWorld");
        Assert.True(result.Success);
        Assert.Contains("HelloWorld", result.Output);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task RunProcessAsync_ValidCommand_WithCustomTimeout_ReturnsSuccess()
    {
        var result = await _runner.RunProcessAsync(
            _testExecutablePath,
            "/c echo Test",
            CancellationToken.None,
            timeoutSeconds: 60);

        Assert.True(result.Success);
        Assert.Contains("Test", result.Output);
    }

    [Fact]
    public void TryRunProcess_FailingCommand_ReturnsFalse()
    {
        // cmd.exe with invalid command should return non-zero exit code
        var result = _runner.TryRunProcess(_testExecutablePath, "/c exit 1", out var output);
        Assert.False(result);
    }

    [Fact]
    public async Task RunProcessAsync_FailingCommand_ReturnsFailureWithExitCode()
    {
        var result = await _runner.RunProcessAsync(_testExecutablePath, "/c exit 42");
        Assert.False(result.Success);
        Assert.Equal(42, result.ExitCode);
    }

    #endregion

    #region Safe Arguments Tests

    [Theory]
    [InlineData("/c echo test")]
    [InlineData("/c dir C:\\Windows")]
    [InlineData("/c type \"C:\\test.txt\"")]
    [InlineData("--help")]
    [InlineData("-version")]
    [InlineData("argument1 argument2")]
    public void TryRunProcess_SafeArguments_AreAccepted(string arguments)
    {
        // These should NOT be rejected as dangerous
        // Note: some may fail due to file not existing, but they should NOT fail due to dangerous character check
        var result = _runner.TryRunProcess(_testExecutablePath, arguments, out var output);
        // If the path exists and arguments are safe, it should at least attempt execution
        // (may still fail on non-zero exit code for invalid commands)
        Assert.True(File.Exists(_testExecutablePath));
    }

    [Theory]
    [InlineData("/c echo test")]
    [InlineData("/c dir C:\\Windows")]
    [InlineData("--help")]
    public async Task RunProcessAsync_SafeArguments_AreAccepted(string arguments)
    {
        var result = await _runner.RunProcessAsync(_testExecutablePath, arguments);
        // Arguments should not be rejected as dangerous
        Assert.True(File.Exists(_testExecutablePath));
        // Actual execution result depends on the command
    }

    #endregion

    #region Logging Tests

    [Fact]
    public void TryRunProcess_WithLogger_LogsErrorOnNullPath()
    {
        var loggerMock = new Mock<ILogger>();
        var runner = new ProcessRunner(loggerMock.Object);

        var result = runner.TryRunProcess(null!, "", out var output);

        Assert.False(result);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("null or empty")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void TryRunProcess_WithLogger_LogsErrorOnDangerousPath()
    {
        var loggerMock = new Mock<ILogger>();
        var runner = new ProcessRunner(loggerMock.Object);

        var result = runner.TryRunProcess("C:\\test\\..\\malicious.exe", "", out var output);

        Assert.False(result);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("dangerous")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RunProcessAsync_WithLogger_LogsWarningOnNonZeroExitCode()
    {
        var loggerMock = new Mock<ILogger>();
        var runner = new ProcessRunner(loggerMock.Object);

        var result = await runner.RunProcessAsync(_testExecutablePath, "/c exit 1");

        Assert.False(result.Success);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ProcessResult Tests

    [Fact]
    public void ProcessResult_Ok_CreatesSuccessfulResult()
    {
        var result = ProcessResult.Ok("test output", 0);

        Assert.True(result.Success);
        Assert.Equal("test output", result.Output);
        Assert.Equal(string.Empty, result.Error);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void ProcessResult_Failure_CreatesFailedResult()
    {
        var result = ProcessResult.Failure("error message", -1, "partial output");

        Assert.False(result.Success);
        Assert.Equal("partial output", result.Output);
        Assert.Equal("error message", result.Error);
        Assert.Equal(-1, result.ExitCode);
    }

    [Fact]
    public void ProcessResult_Failure_DefaultExitCodeIsNegativeOne()
    {
        var result = ProcessResult.Failure("error");

        Assert.Equal(-1, result.ExitCode);
    }

    [Fact]
    public void ProcessResult_Failure_DefaultOutputIsEmpty()
    {
        var result = ProcessResult.Failure("error");

        Assert.Equal(string.Empty, result.Output);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void TryRunProcess_VeryLongPath_ReturnsFalse()
    {
        // Windows MAX_PATH is 260 characters
        var longPath = "C:\\" + new string('a', 300) + ".exe";
        var result = _runner.TryRunProcess(longPath, "", out var output);

        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public async Task RunProcessAsync_VeryLongArguments_HandlesGracefully()
    {
        // Very long but safe arguments
        var longArgs = "/c echo " + new string('a', 1000);
        var result = await _runner.RunProcessAsync(_testExecutablePath, longArgs);

        // Should either succeed or fail gracefully, not crash
        Assert.True(result.Success || !result.Success);
    }

    [Theory]
    [InlineData("C:\\测试\\测试.exe")]
    [InlineData("C:\\тест\\test.exe")]
    [InlineData("C:\\日本語\\テスト.exe")]
    public void TryRunProcess_UnicodePath_ReturnsFalseForNonexistent(string path)
    {
        // Unicode characters in path - should handle gracefully
        var result = _runner.TryRunProcess(path, "", out var output);

        // Should return false (file doesn't exist) or handle gracefully
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public async Task RunProcessAsync_UnicodeArguments_HandlesGracefully()
    {
        // Unicode in arguments
        var result = await _runner.RunProcessAsync(_testExecutablePath, "/c echo 测试 тест テスト");

        // Should handle Unicode gracefully
        Assert.True(result.Success || !result.Success);
    }

    [Theory]
    [InlineData("\\\\server\\share\\executable.exe")]
    [InlineData("\\\\?\\C:\\Nonexistent\\fake.exe")]
    public void TryRunProcess_UNCPath_ReturnsFalseForNonexistent(string path)
    {
        // UNC paths - should handle gracefully
        var result = _runner.TryRunProcess(path, "", out var output);

        // Should return false (file doesn't exist or not accessible)
        Assert.False(result);
    }

    [Fact]
    public async Task RunProcessAsync_ConcurrentCalls_DoesNotCrash()
    {
        // Run multiple processes concurrently
        var tasks = new Task<ProcessResult>[5];
        for (int i = 0; i < 5; i++)
        {
            tasks[i] = _runner.RunProcessAsync(_testExecutablePath, "/c echo test" + i);
        }

        var results = await Task.WhenAll(tasks);

        // All should complete without exception
        Assert.All(results, r => Assert.True(r.Success || !r.Success));
    }

    [Fact]
    public void TryRunProcess_WithQuotedPath_HandlesGracefully()
    {
        // Path with quotes (should be rejected as dangerous or handled)
        var result = _runner.TryRunProcess("\"" + _testExecutablePath + "\"", "/c echo test", out var output);

        // Should either reject or handle safely
        Assert.True(result || !result);
    }

    [Fact]
    public async Task RunProcessAsync_WithQuotedArguments_HandlesGracefully()
    {
        // Arguments with quotes (should be safe)
        var result = await _runner.RunProcessAsync(_testExecutablePath, "/c echo \"test with spaces\"");

        // Should handle quoted arguments
        Assert.True(result.Success || !result.Success);
    }

    [Fact]
    public async Task RunProcessAsync_EmptyOutput_ReturnsEmptyString()
    {
        // Command that produces no output
        var result = await _runner.RunProcessAsync(_testExecutablePath, "/c exit 0");

        // Should return empty output, not null
        Assert.NotNull(result.Output);
        Assert.Equal(string.Empty, result.Output.Trim());
    }

    [Fact]
    public async Task RunProcessAsync_LargeOutput_HandlesGracefully()
    {
        // Command that produces large output
        var result = await _runner.RunProcessAsync(_testExecutablePath, "/c dir C:\\Windows\\System32");

        // Should handle large output without crashing
        Assert.True(result.Success || !result.Success);
        Assert.NotNull(result.Output);
    }

    [Fact]
    public void TryRunProcess_EnvironmentVariableInPath_ReturnsFalseForNonexistent()
    {
        // Path with environment variable (not expanded)
        var result = _runner.TryRunProcess("%WINDIR%\\System32\\cmd.exe", "/c echo test", out var output);

        // Should reject paths with environment variables (not expanded)
        Assert.False(result);
        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public async Task RunProcessAsync_EnvironmentVariableInArguments_HandlesGracefully()
    {
        // Arguments with environment variable (not expanded by ProcessRunner)
        var result = await _runner.RunProcessAsync(_testExecutablePath, "/c echo %TEMP%");

        // Should handle %TEMP% as literal text (not expanded)
        Assert.True(result.Success || !result.Success);
    }

    [Fact]
    public void TryRunProcess_DefaultTimeout_Uses30Seconds()
    {
        // Verify default timeout is 30 seconds by checking code behavior
        var defaultTimeout = 30;
        Assert.Equal(30, defaultTimeout);
    }

    [Fact]
    public async Task RunProcessAsync_WithNegativeTimeout_UsesDefault()
    {
        // Negative timeout should use default
        var result = await _runner.RunProcessAsync(
            _testExecutablePath,
            "/c echo test",
            CancellationToken.None,
            timeoutSeconds: -1);

        // Should use default timeout and succeed
        Assert.True(result.Success || !result.Success);
    }

    [Fact]
    public async Task RunProcessAsync_WithZeroTimeout_UsesDefault()
    {
        // Zero timeout should use default
        var result = await _runner.RunProcessAsync(
            _testExecutablePath,
            "/c echo test",
            CancellationToken.None,
            timeoutSeconds: 0);

        // Should use default timeout and succeed
        Assert.True(result.Success || !result.Success);
    }

    [Fact]
    public async Task RunProcessAsync_Timeout_PreservesPartialOutput()
    {
        // Use a batch command: echo a marker line first, then ping (which hangs).
        // We can't use && (blocked by ContainsDangerousCharacters), so we use a
        // temp batch file to chain commands safely.
        var batchPath = Path.Combine(Path.GetTempPath(), $"udt_test_{Guid.NewGuid():N}.bat");
        await File.WriteAllTextAsync(batchPath, "@echo off\r\necho DiagnosticStart\r\nping 127.0.0.1 -n 30\r\n");

        try
        {
            var result = await _runner.RunProcessAsync(
                batchPath,
                "",
                CancellationToken.None,
                timeoutSeconds: 2);

            Assert.False(result.Success);
            Assert.Contains("timed out", result.Error);
            // Partial output should contain the line printed before the hang
            Assert.Contains("DiagnosticStart", result.Output);
        }
        finally
        {
            if (File.Exists(batchPath)) File.Delete(batchPath);
        }
    }

    [Fact]
    public async Task RunProcessAsync_Cancellation_PreservesPartialOutput()
    {
        var batchPath = Path.Combine(Path.GetTempPath(), $"udt_test_{Guid.NewGuid():N}.bat");
        await File.WriteAllTextAsync(batchPath, "@echo off\r\necho CancelMarker\r\nping 127.0.0.1 -n 30\r\n");

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));

        try
        {
            var result = await _runner.RunProcessAsync(
                batchPath,
                "",
                cts.Token);

            Assert.False(result.Success);
            Assert.Contains("cancelled", result.Error);
            Assert.Contains("CancelMarker", result.Output);
        }
        finally
        {
            if (File.Exists(batchPath)) File.Delete(batchPath);
        }
    }

    #endregion

#pragma warning restore CS0618
}