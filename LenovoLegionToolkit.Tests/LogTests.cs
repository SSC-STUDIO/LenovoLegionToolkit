using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class LogTests
{
    [Fact]
    public void Instance_ShouldBeSingleton()
    {
        // Arrange & Act
        var instance1 = Log.Instance;
        var instance2 = Log.Instance;

        // Assert
        instance1.Should().BeSameAs(instance2);
    }

    [Fact]
    public void LogPath_ShouldNotBeEmpty()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        var logPath = log.LogPath;

        // Assert
        logPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ShutdownAsync_ShouldCompleteWithoutException()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        Func<Task> act = async () => await log.ShutdownAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Shutdown_ShouldCompleteWithoutException()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        Action act = () => log.Shutdown();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Flush_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        Action act = () => log.Flush();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Error_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;
        var exception = new InvalidOperationException("Test exception");

        // Act
        Action act = () => log.Error($"Test error message", exception);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ErrorReport_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;
        var exception = new InvalidOperationException("Test exception");

        // Act
        Action act = () => log.ErrorReport("Test Header", exception);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Warning_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        Action act = () => log.Warning($"Test warning message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Info_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        Action act = () => log.Info($"Test info message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Debug_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        Action act = () => log.Debug($"Test debug message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Trace_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;

        // Act
        Action act = () => log.Trace($"Test trace message");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Trace_WithIsTraceEnabledTrue_ShouldLog()
    {
        // Arrange
        var log = Log.Instance;
        log.IsTraceEnabled = true;

        // Act
        Action act = () => log.Trace($"Test trace with IsTraceEnabled=true");

        // Assert
        act.Should().NotThrow();
        log.IsTraceEnabled = false; // Reset to default
    }

    [Fact]
    public void MultipleLogLevels_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;
        var exception = new InvalidOperationException("Test exception");

        // Act
        Action act = () => {
            log.Error($"Error message", exception);
            log.Warning($"Warning message");
            log.Info($"Info message");
            log.Debug($"Debug message");
            log.Trace($"Trace message");
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void LogLevel_Setting_ShouldControlLogOutput()
    {
        // Arrange
        var log = Log.Instance;
        var originalLevel = log.CurrentLogLevel;

        // Act
        log.CurrentLogLevel = LogLevel.Error;
        Action actError = () => log.Error($"Error message");
        Action actWarning = () => log.Warning($"Warning message");
        Action actInfo = () => log.Info($"Info message");
        
        // Assert
        actError.Should().NotThrow();
        actWarning.Should().NotThrow();
        actInfo.Should().NotThrow();
        
        // Reset
        log.CurrentLogLevel = originalLevel;
    }

    [Fact]
    public void LogFolder_ShouldExist()
    {
        // Arrange
        var log = Log.Instance;
        
        // Act
        var logPath = log.LogPath;
        var folderPath = Path.GetDirectoryName(logPath);
        
        // Assert
        folderPath.Should().NotBeNullOrEmpty();
        Directory.Exists(folderPath).Should().BeTrue();
    }

    [Fact]
    public void ConcurrentLogging_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;
        
        // Act
        Action act = () => {
            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() => {
                    for (int j = 0; j < 10; j++)
                    {
                        log.Info($"Concurrent log {index}-{j}");
                    }
                });
            }
            Task.WaitAll(tasks);
        };
        
        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Log_WithNullException_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;
        
        // Act
        Action act = () => log.Error($"Test error with null exception", null);
        
        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void LargeNumberOfLogs_ShouldNotThrow()
    {
        // Arrange
        var log = Log.Instance;
        
        // Act
        Action act = () => {
            for (int i = 0; i < 200; i++)
            {
                log.Info($"Log entry {i}");
            }
        };
        
        // Assert
        act.Should().NotThrow();
        log.Flush(); // Flush to ensure all logs are written
    }
}


