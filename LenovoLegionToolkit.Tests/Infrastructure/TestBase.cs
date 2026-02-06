using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Settings;
using Moq;

namespace LenovoLegionToolkit.Tests;

/// <summary>
/// Test categories for organizing tests
/// </summary>
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string Performance = "Performance";
    public const string Plugin = "Plugin";
    public const string Settings = "Settings";
    public const string Utils = "Utils";
    public const string Controller = "Controller";
}

/// <summary>
/// Base class for all unit tests with common setup and teardown
/// </summary>
public abstract class UnitTestBase : IDisposable
{
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    protected UnitTestBase()
    {
        Setup();
    }

    protected virtual void Setup()
    {
    }

    public virtual void Dispose()
    {
        Cleanup();
    }

    protected virtual void Cleanup()
    {
    }
}

/// <summary>
/// Base class for tests that require temporary files
/// </summary>
public abstract class TemporaryFileTestBase : UnitTestBase
{
    protected readonly List<string> TempFiles = new();
    protected readonly List<string> TempDirectories = new();

    protected string CreateTempFile(string? content = null)
    {
        var tempPath = Path.GetTempFileName();
        TempFiles.Add(tempPath);
        
        if (content != null)
        {
            File.WriteAllText(tempPath, content);
        }
        
        return tempPath;
    }

    protected string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        TempDirectories.Add(tempDir);
        return tempDir;
    }

    protected override void Cleanup()
    {
        foreach (var file in TempFiles.Where(File.Exists))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        foreach (var dir in TempDirectories.Where(Directory.Exists))
        {
            try
            {
                Directory.Delete(dir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        base.Cleanup();
    }
}

/// <summary>
/// Base class for tests that need to mock application settings
/// </summary>
public abstract class SettingsTestBase : UnitTestBase
{
    protected ApplicationSettings CreateMockSettings()
    {
        return new ApplicationSettings();
    }

    protected ApplicationSettings.ApplicationSettingsStore CreateMockSettingsStore()
    {
        return new ApplicationSettings.ApplicationSettingsStore();
    }
}

/// <summary>
/// Assertion helpers for common test scenarios
/// </summary>
public static class TestAssertions
{
    public static void ShouldBeSuccessful(this Action action)
    {
        action.Should().NotThrow();
    }

    public static async Task ShouldBeSuccessfulAsync(this Func<Task> action)
    {
        await action.Should().NotThrowAsync();
    }

    public static void ShouldFailWith<TException>(this Action action) where TException : Exception
    {
        action.Should().Throw<TException>();
    }

    public static async Task ShouldFailWithAsync<TException>(this Func<Task> action) where TException : Exception
    {
        await action.Should().ThrowAsync<TException>();
    }

    public static void ShouldBeEquivalentTo<T>(this T actual, T expected)
    {
        actual.Should().BeEquivalentTo(expected);
    }

    public static void ShouldHaveCount<T>(this IEnumerable<T> collection, int expectedCount)
    {
        collection.Should().HaveCount(expectedCount);
    }

    public static void ShouldBeEmpty<T>(this IEnumerable<T> collection)
    {
        collection.Should().BeEmpty();
    }

    public static void ShouldNotBeEmpty<T>(this IEnumerable<T> collection)
    {
        collection.Should().NotBeEmpty();
    }

    public static void ShouldContain<T>(this IEnumerable<T> collection, T expected)
    {
        collection.Should().Contain(expected);
    }

    public static void ShouldNotContain<T>(this IEnumerable<T> collection, T expected)
    {
        collection.Should().NotContain(expected);
    }

    public static void ShouldHaveProperty<T>(this T obj, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        property.Should().NotBeNull();
    }

    public static void ShouldHaveMethod<T>(this T obj, string methodName)
    {
        var method = typeof(T).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        method.Should().NotBeNull();
    }
}

/// <summary>
/// Test data generation utilities
/// </summary>
public static class TestDataGenerator
{
    private static int _counter = 0;

    public static string GenerateUniqueString(string prefix = "Test")
    {
        return $"{prefix}_{Guid.NewGuid():N}_{++_counter}";
    }

    public static int GenerateUniqueNumber()
    {
        return ++_counter;
    }

    public static Version GenerateVersion(int major = 1, int minor = 0, int build = 0, int revision = 0)
    {
        return new Version(major, minor, build, revision);
    }

    public static IEnumerable<T> CreateUniqueList<T>(int count, Func<int, T> factory)
    {
        return Enumerable.Range(0, count).Select(factory);
    }

    public static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        new Random(42).NextBytes(bytes);
        return bytes;
    }

    public static string GenerateRandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[new Random(42).Next(chars.Length)])
            .ToArray());
    }

    public static DateTime GenerateRandomDate(DateTime? start = null, DateTime? end = null)
    {
        var startDate = start ?? DateTime.Now.AddYears(-1);
        var endDate = end ?? DateTime.Now;
        var range = (endDate - startDate).Days;
        return startDate.AddDays(new Random(42).Next(range));
    }
}

/// <summary>
/// Async test helpers
/// </summary>
public static class AsyncTestHelpers
{
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        return await task.WaitAsync(cts.Token);
    }

    public static async Task WithTimeout(this Task task, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        await task.WaitAsync(cts.Token);
    }

    public static async Task RetryAsync(Func<Task> action, int maxRetries = 3, int delayMs = 100)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await action();
                return;
            }
            catch
            {
                if (i == maxRetries - 1)
                    throw;
                await Task.Delay(delayMs);
            }
        }
    }
}
