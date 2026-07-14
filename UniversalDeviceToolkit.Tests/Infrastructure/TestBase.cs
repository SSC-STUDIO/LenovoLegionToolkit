using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;

namespace UniversalDeviceToolkit.Tests;

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
        var culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

        try { UniversalDeviceToolkit.WPF.Resources.Resource.Culture = null; } catch { /* Culture may not be initialized in test context */ }
        try { LenovoLegionToolkit.Lib.Resources.Resource.Culture = null; } catch { /* Culture may not be initialized in test context */ }
        try { UniversalDeviceToolkit.Lib.Automation.Resources.Resource.Culture = null; } catch { /* Culture may not be initialized in test context */ }

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
/// Assertion helpers for common test scenarios
/// </summary>
public static class TestAssertions
{
    public static void ShouldBeSuccessful(this Action action)
    {
        action.Should().NotThrow();
    }

    public static void ShouldFailWith<TException>(this Action action) where TException : Exception
    {
        action.Should().Throw<TException>();
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
}

/// <summary>
/// Async test helpers
/// </summary>
public static class AsyncTestHelpers
{
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
