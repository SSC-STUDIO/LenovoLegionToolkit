using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.Tests.PerformanceTests;

/// <summary>
/// Performance benchmarks for plugin loading and settings management.
/// Measures cold start, warm start, and settings I/O performance.
/// </summary>
public class PluginLoadBenchmarks
{
    private readonly string _testPluginName = "PerformanceTestPlugin";
    private SettingsManager<TestSettings>? _settingsManager;

    /// <summary>
    /// Setup for benchmarks.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var testDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LenovoLegionToolkit",
            "plugins",
            _testPluginName);

        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }

        _settingsManager = new SettingsManager<TestSettings>(_testPluginName);
    }

    /// <summary>
    /// Cleanup after benchmarks.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        var testDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LenovoLegionToolkit",
            "plugins",
            _testPluginName);

        if (Directory.Exists(testDirectory))
        {
            try { Directory.Delete(testDirectory, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Benchmark: Cold start — first settings load (file doesn't exist).
    /// </summary>
    [Benchmark(Description = "Settings Cold Start (no file)")]
    [BenchmarkCategory("Settings")]
    public TestSettings SettingsColdStart()
    {
        // Simulate cold start by clearing cache
        _settingsManager!.Clear();
        return _settingsManager.Load();
    }

    /// <summary>
    /// Benchmark: Warm start — cached settings load.
    /// </summary>
    [Benchmark(Description = "Settings Warm Start (cached)")]
    [BenchmarkCategory("Settings")]
    public TestSettings SettingsWarmStart()
    {
        return _settingsManager!.Load();
    }

    [Benchmark(Description = "Settings Save Async")]
    [BenchmarkCategory("Settings")]
    public async Task<bool> SettingsSaveAsync()
    {
        var settings = new TestSettings
        {
            Name = "Test",
            Enabled = true,
            Count = 42
        };
        return await _settingsManager!.SaveAsync(settings);
    }

    [Benchmark(Description = "Settings Save (with memory transaction)")]
    [BenchmarkCategory("Settings")]
    public bool SettingsSaveWithMemoryTransaction()
    {
        var settings = new TestSettings
        {
            Name = "Test",
            Enabled = true,
            Count = 42
        };
        // First save (actual I/O)
        _settingsManager!.Save(settings);

        // Second save (should skip - memory transaction)
        return _settingsManager.Save(settings);
    }
}

/// <summary>
/// Test settings class for benchmarking.
/// </summary>
public class TestSettings
{
    public string Name { get; set; } = "Default";
    public bool Enabled { get; set; } = false;
    public int Count { get; set; } = 0;
}

/// <summary>
/// Program entry point for running benchmarks.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Universal Device Toolkit Plugins — Performance Benchmarks ===");
        Console.WriteLine();

        // Run performance diagnostics first
        var diagnostics = new SavePerformanceDiagnostics();
        diagnostics.RunDiagnostics();
        Console.WriteLine();

        // Run simple stopwatch-based benchmarks (no DefaultConfig dependency)
        await RunSimpleBenchmarks();

        Console.WriteLine();
        Console.WriteLine("=== Benchmark Complete ===");
    }

    private static async Task RunSimpleBenchmarks()
    {
        var benchmarks = new PluginLoadBenchmarks();
        benchmarks.Setup();

        Console.WriteLine("Running benchmarks...");
        Console.WriteLine();

        // Warm-up
        for (int i = 0; i < 3; i++)
        {
            benchmarks.SettingsColdStart();
            benchmarks.SettingsWarmStart();
        }

        // Actual benchmarks
        var iterations = 10; // Reduced for async
        var coldStartTimes = new List<long>();
        var warmStartTimes = new List<long>();
        var saveAsyncTimes = new List<long>();
        var saveWithTransactionTimes = new List<long>();

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            benchmarks.SettingsColdStart();
            sw.Stop();
            coldStartTimes.Add(sw.ElapsedMilliseconds);
        }

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            benchmarks.SettingsWarmStart();
            sw.Stop();
            warmStartTimes.Add(sw.ElapsedMilliseconds);
        }

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            benchmarks.SettingsSaveWithMemoryTransaction();
            sw.Stop();
            saveWithTransactionTimes.Add(sw.ElapsedMilliseconds);
        }

        Console.WriteLine("Running benchmarks (simple stopwatch)...");
        Console.WriteLine();

        Console.WriteLine($"Settings Cold Start (no file):");
        Console.WriteLine($"  Average: {coldStartTimes.Average():F2} ms");
        Console.WriteLine($"  Min: {coldStartTimes.Min()} ms");
        Console.WriteLine($"  Max: {coldStartTimes.Max()} ms");
        Console.WriteLine();

        Console.WriteLine($"Settings Warm Start (cached):");
        Console.WriteLine($"  Average: {warmStartTimes.Average():F2} ms");
        Console.WriteLine($"  Min: {warmStartTimes.Min()} ms");
        Console.WriteLine($"  Max: {warmStartTimes.Max()} ms");
        Console.WriteLine();

        Console.WriteLine($"Settings Save (with memory transaction):");
        Console.WriteLine($"  Average: {saveWithTransactionTimes.Average():F2} ms");
        Console.WriteLine($"  Min: {saveWithTransactionTimes.Min()} ms");
        Console.WriteLine($"  Max: {saveWithTransactionTimes.Max()} ms");
        Console.WriteLine($"  Note: First call does I/O, second call skips (memory transaction)");
        Console.WriteLine();

        benchmarks.Cleanup();
    }
}
