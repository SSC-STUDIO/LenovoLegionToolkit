using System.Diagnostics;
using System.Management;
using System.Text.Json;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.PerformanceTest;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Universal Device Toolkit performance benchmark");
        Console.WriteLine("========================================");
        Console.WriteLine();

        var results = new Dictionary<string, PerformanceMetric>();

        Console.WriteLine("Starting performance benchmarks...\n");

        results["Logging"] = await TestLogPerformance();
        results["WMI queries"] = await TestWMIQueryPerformance();
        results["File IO"] = await TestFileIOPerformance();
        results["Settings load"] = await TestSettingsLoadPerformance();
        results["String processing"] = await TestStringPerformance();
        results["Collections"] = await TestCollectionPerformance();
        results["Parallel init"] = await TestParallelInitialization();

        Console.WriteLine("\n========================================");
        Console.WriteLine("Benchmark summary");
        Console.WriteLine("========================================\n");

        foreach (var result in results.OrderBy(r => r.Value.AverageTimeMs))
        {
            var status = result.Value.AverageTimeMs < 10 ? "excellent" :
                result.Value.AverageTimeMs < 50 ? "good" :
                result.Value.AverageTimeMs < 100 ? "fair" : "needs work";
            Console.WriteLine($"{result.Key,-20} | avg: {result.Value.AverageTimeMs,6:F2} ms | {status}");
        }

        Console.WriteLine("\n========================================");
        Console.WriteLine($"Total measured time: {results.Sum(r => r.Value.TotalTimeMs):F2} ms");
        Console.WriteLine("========================================");

        await SaveResultsToFile(results);
    }

    private static async Task<PerformanceMetric> TestLogPerformance()
    {
        Console.WriteLine("1. Logging performance...");
        var stopwatch = Stopwatch.StartNew();
        var times = new List<long>();

        for (var i = 0; i < 100; i++)
        {
            var sw = Stopwatch.StartNew();
            var tasks = new List<Task>();
            for (var j = 0; j < 10; j++)
            {
                var taskId = i * 10 + j;
                tasks.Add(Task.Run(() => { Log.Instance.Info($"Benchmark log message #{taskId}"); }));
            }

            await Task.WhenAll(tasks);
            sw.Stop();
            times.Add(sw.ElapsedMilliseconds);
        }

        stopwatch.Stop();
        Console.WriteLine($"   1000 log messages total: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"   Average per 10 messages: {times.Average():F2} ms");

        return new PerformanceMetric
        {
            Name = "Logging",
            TotalTimeMs = stopwatch.ElapsedMilliseconds,
            AverageTimeMs = times.Average(),
            MinTimeMs = times.Min(),
            MaxTimeMs = times.Max(),
            Operations = 1000
        };
    }

    private static async Task<PerformanceMetric> TestWMIQueryPerformance()
    {
        Console.WriteLine("\n2. WMI query performance...");
        var times = new List<long>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (var i = 0; i < 50; i++)
            {
                var sw = Stopwatch.StartNew();
                var mos = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                var results = await mos.GetAsync().ConfigureAwait(false);
                _ = results.Count();
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            stopwatch.Stop();
            Console.WriteLine($"   50 WMI queries total: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"   Average per query: {times.Average():F2} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   WMI query test failed: {ex.Message}");
        }

        return new PerformanceMetric
        {
            Name = "WMI queries",
            TotalTimeMs = stopwatch.ElapsedMilliseconds,
            AverageTimeMs = times.Count > 0 ? times.Average() : 0,
            MinTimeMs = times.Count > 0 ? times.Min() : 0,
            MaxTimeMs = times.Count > 0 ? times.Max() : 0,
            Operations = 50
        };
    }

    private static async Task<PerformanceMetric> TestFileIOPerformance()
    {
        Console.WriteLine("\n3. File IO performance...");
        var times = new List<long>();
        var stopwatch = Stopwatch.StartNew();
        var tempPath = Path.Combine(Path.GetTempPath(), "performance_test.txt");

        try
        {
            for (var i = 0; i < 100; i++)
            {
                var sw = Stopwatch.StartNew();
                var content = string.Join("\n", Enumerable.Range(0, 100).Select(j => $"line-{i}-{j}"));
                await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);
                _ = await File.ReadAllTextAsync(tempPath).ConfigureAwait(false);
                File.Delete(tempPath);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            stopwatch.Stop();
            Console.WriteLine($"   100 file read/write ops total: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"   Average per op: {times.Average():F2} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   File IO test failed: {ex.Message}");
        }

        return new PerformanceMetric
        {
            Name = "File IO",
            TotalTimeMs = stopwatch.ElapsedMilliseconds,
            AverageTimeMs = times.Count > 0 ? times.Average() : 0,
            MinTimeMs = times.Count > 0 ? times.Min() : 0,
            MaxTimeMs = times.Count > 0 ? times.Max() : 0,
            Operations = 100
        };
    }

    private static async Task<PerformanceMetric> TestSettingsLoadPerformance()
    {
        Console.WriteLine("\n4. Settings load performance...");
        var times = new List<long>();
        var stopwatch = Stopwatch.StartNew();
        var tempPath = Path.Combine(Path.GetTempPath(), "settings_test.json");

        try
        {
            var settings = new TestSettings(tempPath);
            var testData = new { Value = "benchmark-data", Count = 100 };
            var json = JsonSerializer.Serialize(testData);
            await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);

            for (var i = 0; i < 50; i++)
            {
                var sw = Stopwatch.StartNew();
                _ = settings.LoadStore();
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            File.Delete(tempPath);

            stopwatch.Stop();
            Console.WriteLine($"   50 settings loads total: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"   Average per load: {times.Average():F2} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Settings load test failed: {ex.Message}");
        }

        return new PerformanceMetric
        {
            Name = "Settings load",
            TotalTimeMs = stopwatch.ElapsedMilliseconds,
            AverageTimeMs = times.Count > 0 ? times.Average() : 0,
            MinTimeMs = times.Count > 0 ? times.Min() : 0,
            MaxTimeMs = times.Count > 0 ? times.Max() : 0,
            Operations = 50
        };
    }

    private static Task<PerformanceMetric> TestStringPerformance()
    {
        Console.WriteLine("\n5. String processing performance...");
        var times = new List<long>();
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
        {
            var sw = Stopwatch.StartNew();
            var text = string.Join(" ", Enumerable.Range(0, 100).Select(j => $"word{j}"));
            _ = text.Split(' ').Where(s => s.StartsWith("w", StringComparison.Ordinal)).ToList();
            var replaced = text.Replace("word", "token", StringComparison.Ordinal);
            _ = replaced.Contains("token50", StringComparison.Ordinal);
            sw.Stop();
            times.Add(sw.ElapsedMilliseconds);
        }

        stopwatch.Stop();
        Console.WriteLine($"   1000 string ops total: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"   Average per op: {times.Average():F2} ms");

        return Task.FromResult(new PerformanceMetric
        {
            Name = "String processing",
            TotalTimeMs = stopwatch.ElapsedMilliseconds,
            AverageTimeMs = times.Average(),
            MinTimeMs = times.Min(),
            MaxTimeMs = times.Max(),
            Operations = 1000
        });
    }

    private static Task<PerformanceMetric> TestCollectionPerformance()
    {
        Console.WriteLine("\n6. Collection performance...");
        var times = new List<long>();
        var stopwatch = Stopwatch.StartNew();

        for (var i = 0; i < 1000; i++)
        {
            var sw = Stopwatch.StartNew();
            var list = Enumerable.Range(0, 1000).ToList();
            var filtered = list.Where(x => x % 2 == 0).ToList();
            var dict = filtered.ToDictionary(x => x, x => x * 2);
            _ = dict.ContainsKey(500);
            sw.Stop();
            times.Add(sw.ElapsedMilliseconds);
        }

        stopwatch.Stop();
        Console.WriteLine($"   1000 collection ops total: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"   Average per op: {times.Average():F2} ms");

        return Task.FromResult(new PerformanceMetric
        {
            Name = "Collections",
            TotalTimeMs = stopwatch.ElapsedMilliseconds,
            AverageTimeMs = times.Average(),
            MinTimeMs = times.Min(),
            MaxTimeMs = times.Max(),
            Operations = 1000
        });
    }

    private static async Task<PerformanceMetric> TestParallelInitialization()
    {
        Console.WriteLine("\n7. Serial vs parallel init...");

        Func<Task>[] initializationSteps =
        [
            () => SimulateInitialization("step-1", 50),
            () => SimulateInitialization("step-2", 75),
            () => SimulateInitialization("step-3", 40),
            () => SimulateInitialization("step-4", 100),
            () => SimulateInitialization("step-5", 60)
        ];

        var stopwatch = Stopwatch.StartNew();

        foreach (var step in initializationSteps)
            await step();

        var serialTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        var parallelTasks = initializationSteps.Select(step => step()).ToArray();
        await Task.WhenAll(parallelTasks);
        var parallelTime = stopwatch.ElapsedMilliseconds;

        var improvement = ((double)(serialTime - parallelTime) / serialTime) * 100;

        Console.WriteLine($"   Serial init: {serialTime} ms");
        Console.WriteLine($"   Parallel init: {parallelTime} ms");
        Console.WriteLine($"   Improvement: {improvement:F1}% ({serialTime / (double)parallelTime:F2}x)");

        return new PerformanceMetric
        {
            Name = "Parallel init",
            TotalTimeMs = serialTime + parallelTime,
            AverageTimeMs = parallelTime,
            MinTimeMs = parallelTime,
            MaxTimeMs = serialTime,
            Operations = 5
        };
    }

    private static async Task SimulateInitialization(string name, int delayMs)
    {
        await Task.Delay(delayMs);
        Log.Instance.Info($"Simulated init {name} finished in {delayMs}ms");
    }

    private static async Task SaveResultsToFile(Dictionary<string, PerformanceMetric> results)
    {
        try
        {
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"PerformanceBenchmark_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt");

            var lines = new List<string>
            {
                "Universal Device Toolkit performance benchmark report",
                $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                "",
                "Results:",
                "".PadRight(80, '-'),
                $"{nameof(PerformanceMetric.Name),-20} | {nameof(PerformanceMetric.Operations),-10} | {nameof(PerformanceMetric.TotalTimeMs),-12} | {nameof(PerformanceMetric.AverageTimeMs),-12} | {nameof(PerformanceMetric.MinTimeMs),-12} | {nameof(PerformanceMetric.MaxTimeMs),-12}",
                "".PadRight(80, '-')
            };

            foreach (var result in results.Values)
            {
                lines.Add(
                    $"{result.Name,-20} | {result.Operations,-10} | {result.TotalTimeMs,12:F2} | {result.AverageTimeMs,12:F2} | {result.MinTimeMs,12:F2} | {result.MaxTimeMs,12:F2}");
            }

            lines.Add("".PadRight(80, '-'));
            lines.Add($"Total measured time: {results.Sum(r => r.Value.TotalTimeMs):F2} ms");
            lines.Add("");
            lines.Add("Notes:");
            lines.Add("1. Long WMI queries benefit from caching.");
            lines.Add("2. Prefer async and batching for frequent file IO.");
            lines.Add("3. Settings loads can use in-memory cache.");
            lines.Add("4. Production hardware init should stay serial for WMI/EC safety;");
            lines.Add("   only independent services may use limited parallelism.");

            await File.WriteAllLinesAsync(outputPath, lines).ConfigureAwait(false);
            Console.WriteLine($"\nDetailed report saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFailed to save report: {ex.Message}");
        }
    }

    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public long TotalTimeMs { get; set; }
        public double AverageTimeMs { get; set; }
        public long MinTimeMs { get; set; }
        public long MaxTimeMs { get; set; }
        public int Operations { get; set; }
    }

    private sealed class TestSettings(string path) : AbstractSettings<object>(Path.GetFileName(path))
    {
        protected override string SettingsFilePath => path;
    }
}
