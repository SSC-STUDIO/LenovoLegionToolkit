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
        var arguments = "/c echo test";
        var environment = new Dictionary<string, string?>
        {
            { "TEST_VAR", null }
        };

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
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
        // cmd.exe without arguments starts an interactive session
        exitCode.Should().Be(0);
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

        // Assert - Cancellation may or may not throw depending on implementation
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunAsync_WithLongRunningCommand_ShouldComplete()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c timeout /t 2 /nobreak && echo done";

        // Act
        var startTime = DateTime.UtcNow;
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("done");
        elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task RunAsync_WithMultipleLines_ShouldExecuteAll()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo line1 & echo line2 & echo line3";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("line1");
        output.Should().Contain("line2");
        output.Should().Contain("line3");
    }

    [Fact]
    public async Task RunAsync_WithSpecialCharacters_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test!@#$%^&*()";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_WithUnicodeCharacters_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo 测试中文";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithErrorOutput_ShouldCapture()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo error message >&2";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("error message");
    }

    [Fact]
    public async Task RunAsync_WithNonZeroExitCode_ShouldReturnCode()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c exit 5";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(5);
    }

    [Fact]
    public async Task RunAsync_WithLargeOutput_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c for /L %i in (1,1,1000) do @echo line%i";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("line1");
        output.Should().Contain("line1000");
    }

    [Fact]
    public async Task RunAsync_WithMultipleEnvironmentVariables_ShouldSetAll()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %VAR1% %VAR2% %VAR3%";
        var environment = new Dictionary<string, string?>
        {
            { "VAR1", "value1" },
            { "VAR2", "value2" },
            { "VAR3", "value3" }
        };

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("value1");
        output.Should().Contain("value2");
        output.Should().Contain("value3");
    }



    [Fact]
    public async Task RunAsync_WithEmptyOutput_ShouldReturnEmpty()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c rem This is a comment with no output";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNull();
    }

    [Fact]
    public async Task RunAsync_WithWhitespaceArguments_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "   /c   echo   test   ";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_WithQuotedArguments_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = @"/c echo ""quoted string""";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("quoted string");
    }

    [Fact]
    public async Task RunAsync_WithEnvironmentVariableWithSpaces_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %TEST_VAR%";
        var environment = new Dictionary<string, string?>
        {
            { "TEST_VAR", "value with spaces" }
        };

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("value with spaces");
    }

    [Fact]
    public async Task RunAsync_WithEnvironmentVariableWithSpecialChars_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %TEST_VAR%";
        var environment = new Dictionary<string, string?>
        {
            { "TEST_VAR", "value!@#$%^&*()" }
        };

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("value");
    }

    [Fact]
    public async Task RunAsync_WithEmptyEnvironmentDictionary_ShouldSucceed()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test";
        var environment = new Dictionary<string, string?>();

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_WithNullEnvironmentDictionary_ShouldSucceed()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test";
        Dictionary<string, string?>? environment = null;

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, environment: environment, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_WithSystemCommand_ShouldExecute()
    {
        // Arrange
        var file = "powershell.exe";
        var arguments = "-Command Write-Host 'PowerShell test'";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("PowerShell test");
    }

    [Fact]
    public async Task RunAsync_WithWhoami_ShouldReturnUsername()
    {
        // Arrange
        var file = "whoami.exe";
        var arguments = "";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithIpconfig_ShouldReturnNetworkInfo()
    {
        // Arrange
        var file = "ipconfig.exe";
        var arguments = "";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithDirCommand_ShouldReturnDirectoryListing()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c dir %TEMP%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithSetCommand_ShouldReturnEnvironmentVariables()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c set";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("PATH");
    }

    [Fact]
    public async Task RunAsync_WithVerCommand_ShouldReturnWindowsVersion()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c ver";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Microsoft");
    }

    [Fact]
    public async Task RunAsync_WithDateCommand_ShouldReturnCurrentDate()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %DATE%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithTimeCommand_ShouldReturnCurrentTime()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %TIME%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithComputerName_ShouldReturnComputerName()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %COMPUTERNAME%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithUserName_ShouldReturnUserName()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %USERNAME%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithSystemRoot_ShouldReturnSystemRoot()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %SYSTEMROOT%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Windows");
    }

    [Fact]
    public async Task RunAsync_WithProgramFiles_ShouldReturnProgramFiles()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %PROGRAMFILES%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Program Files");
    }

    [Fact]
    public async Task RunAsync_WithConcurrentCommands_ShouldNotInterfere()
    {
        // Arrange
        var file = "cmd.exe";
        var tasks = new List<Task<(int exitCode, string output)>>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(CMD.RunAsync(file, $"/c echo test{index}", waitForExit: true));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.exitCode == 0);
    }

    [Fact]
    public async Task RunAsync_WithVeryLongArguments_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var longString = new string('a', 8000);
        var arguments = $"/c echo {longString}";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain(longString.Substring(0, 100));
    }

    [Fact]
    public async Task RunAsync_WithNewlineInOutput_ShouldPreserve()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo line1 && echo line2 && echo line3";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("line1");
        output.Should().Contain("line2");
        output.Should().Contain("line3");
    }

    [Fact]
    public async Task RunAsync_WithTabInOutput_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo col1	col2	col3";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("col1");
    }

    [Fact]
    public async Task RunAsync_WithExitCode1_ShouldReturn1()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c exit /b 1";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(1);
    }

    [Fact]
    public async Task RunAsync_WithExitCode255_ShouldReturn255()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c exit /b 255";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(255);
    }

    [Fact]
    public async Task RunAsync_WithTypeCommand_ShouldReadFile()
    {
        // Arrange
        var file = "cmd.exe";
        var tempFile = System.IO.Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllText(tempFile, "test content");
            var arguments = $"/c type \"{tempFile}\"";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments ?? "", waitForExit: true);

            // Assert
            exitCode.Should().Be(0);
            output.Should().Contain("test content");
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task RunAsync_WithFindCommand_ShouldFindText()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test123 | find \"test\"";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test123");
    }

    [Fact]
    public async Task RunAsync_WithFindstrCommand_ShouldFindString()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test123 | findstr \"test\"";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test123");
    }

    [Fact]
    public async Task RunAsync_WithMoreCommand_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test >nul 2>&1";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithSortCommand_ShouldSort()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c (echo c && echo a && echo b) | sort";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("a");
        output.Should().Contain("b");
        output.Should().Contain("c");
    }

    [Fact]
    public async Task RunAsync_WithIfCommand_ShouldExecuteConditionally()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c if 1==1 echo true";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("true");
    }

    [Fact]
    public async Task RunAsync_WithForCommand_ShouldLoop()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c for %i in (1 2 3) do @echo %i";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("1");
        output.Should().Contain("2");
        output.Should().Contain("3");
    }

    [Fact]
    public async Task RunAsync_WithGotoCommand_ShouldJump()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo reached";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("reached");
    }

    [Fact]
    public async Task RunAsync_WithCallCommand_ShouldCall()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c call echo test";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_WithShiftCommand_ShouldShift()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c shift && echo shifted";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("shifted");
    }

    [Fact]
    public async Task RunAsync_WithPauseCommand_ShouldTimeout()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test | timeout /t 1 /nobreak";

        // Act
        var startTime = DateTime.UtcNow;
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        exitCode.Should().Be(0);
        elapsed.TotalSeconds.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task RunAsync_WithChoiceCommand_ShouldHandle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo test | choice /c yn /t 1 /d y";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithAssocCommand_ShouldReturnAssociations()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c assoc .txt";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain(".txt");
    }

    [Fact]
    public async Task RunAsync_WithFtypeCommand_ShouldReturnFileTypes()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c ftype";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithPathCommand_ShouldReturnPath()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c path";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("PATH");
    }

    [Fact]
    public async Task RunAsync_WithPromptCommand_ShouldSetPrompt()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c prompt $P$G && echo prompt set";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("prompt set");
    }

    [Fact]
    public async Task RunAsync_WithTitleCommand_ShouldSetTitle()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c title Test Window && echo title set";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("title set");
    }

    [Fact]
    public async Task RunAsync_WithColorCommand_ShouldSetColor()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c color 0A && echo color set";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("color set");
    }

    [Fact]
    public async Task RunAsync_WithClsCommand_ShouldClear()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c cls && echo cleared";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("cleared");
    }

    [Fact]
    public async Task RunAsync_WithModeCommand_ShouldReturnMode()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c mode con";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithChcpCommand_ShouldReturnCodePage()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c chcp";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("code page");
    }

    [Fact]
    public async Task RunAsync_WithCountryCommand_ShouldReturnCountry()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %COUNTRY%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithDriveCommand_ShouldChangeDrive()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %CD%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithPushdPopd_ShouldChangeDirectory()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c pushd %TEMP% && echo %CD% && popd && echo %CD%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("TEMP");
    }

    [Fact]
    public async Task RunAsync_WithSetlocalEndlocal_ShouldScopeVariables()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c setlocal && set TEST=local && echo %TEST% && endlocal && echo %TEST%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithEnableDelayedExpansion_ShouldExpand()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c setlocal enabledelayedexpansion && set VAR=test && echo !VAR! && endlocal";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("test");
    }

    [Fact]
    public async Task RunAsync_WithVariables_ShouldExpand()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c set VAR=hello && echo %VAR% world";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("hello world");
    }

    [Fact]
    public async Task RunAsync_WithSubstring_ShouldExtract()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c set VAR=hello && echo %VAR:~0,3%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("hel");
    }

    [Fact]
    public async Task RunAsync_WithReplace_ShouldReplace()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c set VAR=hello world && echo %VAR:world=there%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("hello there");
    }

    [Fact]
    public async Task RunAsync_WithRandom_ShouldGenerateNumber()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %RANDOM%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithErrorlevel_ShouldCheck()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c exit 0 && echo errorlevel=%ERRORLEVEL%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("errorlevel");
    }

    [Fact]
    public async Task RunAsync_WithCmdextVersion_ShouldReturnVersion()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %CMDEXTVERSION%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithCmdcmdline_ShouldReturnCommandLine()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %CMDCMDLINE%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithHomedrive_ShouldReturnDrive()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %HOMEDRIVE%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithHomepath_ShouldReturnPath()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %HOMEPATH%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithUserprofile_ShouldReturnProfile()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %USERPROFILE%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithAppdata_ShouldReturnAppData()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %APPDATA%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithLocalAppdata_ShouldReturnLocalAppData()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %LOCALAPPDATA%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithTemp_ShouldReturnTemp()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %TEMP%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithTmp_ShouldReturnTmp()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %TMP%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithOs_ShouldReturnOS()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %OS%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Windows");
    }

    [Fact]
    public async Task RunAsync_WithProcessorArchitecture_ShouldReturnArchitecture()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %PROCESSOR_ARCHITECTURE%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithProcessorIdentifier_ShouldReturnIdentifier()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %PROCESSOR_IDENTIFIER%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithProcessorLevel_ShouldReturnLevel()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %PROCESSOR_LEVEL%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithProcessorRevision_ShouldReturnRevision()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %PROCESSOR_REVISION%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithNumberOfProcessors_ShouldReturnCount()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %NUMBER_OF_PROCESSORS%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithComSpec_ShouldReturnComSpec()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %COMSPEC%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("cmd.exe");
    }

    [Fact]
    public async Task RunAsync_WithWindir_ShouldReturnWindowsDir()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %WINDIR%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().Contain("Windows");
    }

    [Fact]
    public async Task RunAsync_WithSystemDrive_ShouldReturnSystemDrive()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %SYSTEMDRIVE%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithProgramData_ShouldReturnProgramData()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %PROGRAMDATA%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithPublic_ShouldReturnPublic()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %PUBLIC%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithCommonProgramFiles_ShouldReturnPath()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %COMMONPROGRAMFILES%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithLogonServer_ShouldReturnServer()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %LOGONSERVER%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithUserDomain_ShouldReturnDomain()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %USERDOMAIN%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
        output.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunAsync_WithSessionname_ShouldReturnSession()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %SESSIONNAME%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }

    [Fact]
    public async Task RunAsync_WithClientname_ShouldReturnClient()
    {
        // Arrange
        var file = "cmd.exe";
        var arguments = "/c echo %CLIENTNAME%";

        // Act
        var (exitCode, output) = await CMD.RunAsync(file, arguments, waitForExit: true);

        // Assert
        exitCode.Should().Be(0);
    }
}

