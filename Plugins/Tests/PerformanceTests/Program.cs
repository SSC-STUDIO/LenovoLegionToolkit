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

    /// <summary>
    /// Benchmark: Settings save performance.
    /// </summary>
    [Benchmark(Description = "Settings Save")]
    [BenchmarkCategory("Settings")]
    public bool SettingsSave()
    {
        var settings = new TestSettings
        {
            Name = "Test",
            Enabled = true,
            Count = 42
        };
        return _settingsManager!.Save(settings);
    }

    /// <summary>
    /// Benchmark: Settings update (load + modify + save).
    /// </summary>
    [Benchmark(Description = "Settings Update (load+modify+save)")]
    [BenchmarkCategory("Settings")]
    public bool SettingsUpdate()
    {
        return _settingsManager!.Update(s =>
        {
            s.Count++;
            s.Name = $"Updated at {DateTime.Now:HH:mm:ss.fff}";
        });
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
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Universal Device Toolkit Plugins — Performance Benchmarks ===");
        Console.WriteLine();

        // Run simple stopwatch-based benchmarks (no DefaultConfig dependency)
        RunSimpleBenchmarks();

        Console.WriteLine();
        Console.WriteLine("=== Benchmark Complete ===");
    }

    private static void RunSimpleBenchmarks()
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
        var iterations = 100;
        var coldStartTimes = new List<long>();
        var warmStartTimes = new List<long>();
        var saveTimes = new List<long>();
        var updateTimes = new List<long>();

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
            benchmarks.SettingsSave();
            sw.Stop();
            saveTimes.Add(sw.ElapsedMilliseconds);
        }

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            benchmarks.SettingsUpdate();
            sw.Stop();
            updateTimes.Add(sw.ElapsedMilliseconds);
        }

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

        Console.WriteLine($"Settings Save:");
        Console.WriteLine($"  Average: {saveTimes.Average():F2} ms");
        Console.WriteLine($"  Min: {saveTimes.Min()} ms");
        Console.WriteLine($"  Max: {saveTimes.Max()} ms");
        Console.WriteLine();

        Console.WriteLine($"Settings Update (load+modify+save):");
        Console.WriteLine($"  Average: {updateTimes.Average():F2} ms");
        Console.WriteLine($"  Min: {updateTimes.Min()} ms");
        Console.WriteLine($"  Max: {updateTimes.Max()} ms");
        Console.WriteLine();

        benchmarks.Cleanup();
    }
}
