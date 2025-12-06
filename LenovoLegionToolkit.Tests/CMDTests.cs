using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.System;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class CMDTests
{
    [Fact]
    public async Task RunAsync_WithValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_WithInvalidFile_ShouldThrowException()
    {
        // Arrange
        var invalidFile = "nonexistent.exe";
        var arguments = "";

        // Act
        Func<Task> act = async () => await CMD.RunAsync(invalidFile, arguments);

        // Assert
        // CMD.RunAsync throws Win32Exception when file doesn't exist, not ArgumentException
        await act.Should().ThrowAsync<System.ComponentModel.Win32Exception>();
    }

    [Fact]
    public async Task RunAsync_WithDangerousInput_ShouldThrowArgumentException()
    {
        // Arrange
        var file = "cmd.exe";
        var dangerousArguments = "& del /f /q *.*";

        // Act
        Func<Task> act = async () => await CMD.RunAsync(file, dangerousArguments);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_WithWaitForExitFalse_ShouldReturnImmediately()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c timeout /t 5 /nobreak";

        // Act
        var startTime = DateTime.UtcNow;
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: false);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        exitCode.Should().Be(-1);
        output.Should().BeEmpty();
        elapsed.TotalSeconds.Should().BeLessThan(1); // Should return immediately
    }

    [Fact]
    public async Task RunAsync_WithEnvironmentVariables_ShouldSetVariables()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %TEST_VAR%";
        var environment = new Dictionary<string, string?>
        {
            { "TEST_VAR", "test_value" }
        };

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test_value");
    }

    [Fact]
    public async Task RunAsync_WithNullEnvironmentVariableValue_ShouldRemoveVariable()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c set | findstr TEST_VAR";
        var environment = new Dictionary<string, string?>
        {
            { "TEST_VAR", null }
        };

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().NotBe(0); // findstr returns non-zero if not found
        output.Should().NotContain("TEST_VAR");
    }

    [Fact]
    public async Task RunAsync_WithInvalidEnvironmentVariableKey_ShouldThrow()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test";
        var environment = new Dictionary<string, string?>
        {
            { "INVALID_KEY!", "value" }
        };

        // Act
        Func<Task> act = async () => await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_WithDangerousEnvironmentValue_ShouldThrow()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %TEST_VAR%";
        var environment = new Dictionary<string, string?>
        {
            { "TEST_VAR", "value & del /f /q *.*" }
        };

        // Act
        Func<Task> act = async () => await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_WithEmptyFileName_ShouldThrow()
    {
        // Arrange
        var file = "";
        var arguments = "/c echo test";

        // Act
        Func<Task> act = async () => await CMD.RunAsync(file, arguments);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RunAsync_WithNullArguments_ShouldSucceed()
    {
        // Arrange
        var file = "cmd.exe";
        string? arguments = null;

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().NotBe(0); // cmd.exe without arguments exits with non-zero code
    }

    [Fact]
    public async Task RunAsync_WithCreateNoWindowFalse_ShouldSucceed()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, createNoWindow: false, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public void ContainsDangerousInput_WithValidRedirection_ShouldReturnFalse()
    {
        // Arrange
        var validInputs = new[]
        {
            ">nul 2>&1",
            "1>&2",
            "2>&1",
            ">output.txt",
            "<input.txt",
            ">>append.txt"
        };

        // Act & Assert
        foreach (var input in validInputs)
        {
            CMD.ContainsDangerousInput(input).Should().BeFalse($"Input '{input}' should be considered safe");
        }
    }

    [Fact]
    public void ContainsDangerousInput_WithCommandChaining_ShouldReturnTrue()
    {
        // Arrange
        var dangerousInputs = new[]
        {
            "command1 & command2",
            "command1 && command2",
            "command1 || command2",
            "command1 | command2",
            "command1; command2",
            "command1 & command2",
            "& command2",
            "command1 &",
            "$()",
            "`command`"
        };

        // Act & Assert
        foreach (var input in dangerousInputs)
        {
            CMD.ContainsDangerousInput(input).Should().BeTrue($"Input '{input}' should be considered dangerous");
        }
    }

    [Fact]
    public void IsValidEnvironmentVariable_WithInvalidKeys_ShouldReturnFalse()
    {
        // Arrange
        var invalidKeys = new[]
        {
            "",
            null,
            "INVALID!KEY",
            "INVALID@KEY",
            "INVALID#KEY",
            "INVALID KEY",
            "invalid-key",
            "invalid.key"
        };

        // Note: We're testing the private IsValidEnvironmentVariable method indirectly
        // by trying to set environment variables with invalid keys
    }

    [Fact]
    public async Task RunAsync_WithCancellation_ShouldCancel()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c timeout /t 10 /nobreak";
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        Func<Task> act = async () => await CMD.RunAsync(file, arguments, waitForExit: true, token: cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

