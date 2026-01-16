using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.PerformanceTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Lenovo Legion Toolkit 性能基准测试");
            Console.WriteLine("========================================");
            Console.WriteLine();

            var results = new Dictionary<string, PerformanceMetric>();

            Console.WriteLine("开始性能基准测试...\n");

            results["日志系统"] = await TestLogPerformance();
            results["WMI查询"] = await TestWMIQueryPerformance();
            results["文件IO"] = await TestFileIOPerformance();
            results["设置加载"] = await TestSettingsLoadPerformance();
            results["字符串处理"] = await TestStringPerformance();
            results["集合操作"] = await TestCollectionPerformance();
            results["并行初始化"] = await TestParallelInitialization();

            Console.WriteLine("\n========================================");
            Console.WriteLine("性能基准测试结果汇总");
            Console.WriteLine("========================================\n");

            foreach (var result in results.OrderBy(r => r.Value.AverageTimeMs))
            {
                var status = result.Value.AverageTimeMs < 10 ? "优秀" :
                            result.Value.AverageTimeMs < 50 ? "良好" :
                            result.Value.AverageTimeMs < 100 ? "一般" : "需优化";
                Console.WriteLine($"{result.Key,-20} | 平均耗时: {result.Value.AverageTimeMs,6:F2} ms | {status}");
            }

            Console.WriteLine("\n========================================");
            Console.WriteLine($"总测试耗时: {results.Sum(r => r.Value.TotalTimeMs):F2} ms");
            Console.WriteLine("========================================");

            await SaveResultsToFile(results);
        }

        private static async Task<PerformanceMetric> TestLogPerformance()
        {
            Console.WriteLine("1. 测试日志系统性能...");
            var stopwatch = Stopwatch.StartNew();
            var times = new List<long>();

            for (int i = 0; i < 100; i++)
            {
                var sw = Stopwatch.StartNew();
                var tasks = new List<Task>();
                for (int j = 0; j < 10; j++)
                {
                    int taskId = i * 10 + j;
                    tasks.Add(Task.Run(() =>
                    {
                        Log.Instance.Info($"测试日志消息 #{taskId}");
                    }));
                }
                await Task.WhenAll(tasks);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            stopwatch.Stop();
            Console.WriteLine($"   1000 条日志消息总耗时: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"   平均每次10条耗时: {times.Average():F2} ms");

            return new PerformanceMetric
            {
                Name = "日志系统",
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                AverageTimeMs = times.Average(),
                MinTimeMs = times.Min(),
                MaxTimeMs = times.Max(),
                Operations = 1000
            };
        }

        private static async Task<PerformanceMetric> TestWMIQueryPerformance()
        {
            Console.WriteLine("\n2. 测试WMI查询性能...");
            var times = new List<long>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                for (int i = 0; i < 50; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var query = "SELECT * FROM Win32_OperatingSystem";
                    var mos = new ManagementObjectSearcher(query);
                    var results = await mos.GetAsync().ConfigureAwait(false);
                    var count = results.Count();
                    sw.Stop();
                    times.Add(sw.ElapsedMilliseconds);
                }

                stopwatch.Stop();
                Console.WriteLine($"   50 次WMI查询总耗时: {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine($"   平均每次查询耗时: {times.Average():F2} ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   WMI查询测试失败: {ex.Message}");
            }

            return new PerformanceMetric
            {
                Name = "WMI查询",
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                AverageTimeMs = times.Count > 0 ? times.Average() : 0,
                MinTimeMs = times.Count > 0 ? times.Min() : 0,
                MaxTimeMs = times.Count > 0 ? times.Max() : 0,
                Operations = 50
            };
        }

        private static async Task<PerformanceMetric> TestFileIOPerformance()
        {
            Console.WriteLine("\n3. 测试文件IO性能...");
            var times = new List<long>();
            var stopwatch = Stopwatch.StartNew();
            var tempPath = Path.Combine(Path.GetTempPath(), "performance_test.txt");

            try
            {
                for (int i = 0; i < 100; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var content = string.Join("\n", Enumerable.Range(0, 100).Select(j => $"测试行 {i}-{j}"));
                    await File.WriteAllTextAsync(tempPath, content).ConfigureAwait(false);
                    var readContent = await File.ReadAllTextAsync(tempPath).ConfigureAwait(false);
                    File.Delete(tempPath);
                    sw.Stop();
                    times.Add(sw.ElapsedMilliseconds);
                }

                stopwatch.Stop();
                Console.WriteLine($"   100 次文件读写操作总耗时: {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine($"   平均每次操作耗时: {times.Average():F2} ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   文件IO测试失败: {ex.Message}");
            }

            return new PerformanceMetric
            {
                Name = "文件IO",
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                AverageTimeMs = times.Count > 0 ? times.Average() : 0,
                MinTimeMs = times.Count > 0 ? times.Min() : 0,
                MaxTimeMs = times.Count > 0 ? times.Max() : 0,
                Operations = 100
            };
        }

        private static async Task<PerformanceMetric> TestSettingsLoadPerformance()
        {
            Console.WriteLine("\n4. 测试设置加载性能...");
            var times = new List<long>();
            var stopwatch = Stopwatch.StartNew();
            var tempPath = Path.Combine(Path.GetTempPath(), "settings_test.json");

            try
            {
                var settings = new TestSettings(tempPath);
                var testData = new { Value = "测试数据", Count = 100 };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(testData);
                await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);

                for (int i = 0; i < 50; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var loaded = settings.LoadStore();
                    sw.Stop();
                    times.Add(sw.ElapsedMilliseconds);
                }

                File.Delete(tempPath);

                stopwatch.Stop();
                Console.WriteLine($"   50 次设置加载操作总耗时: {stopwatch.ElapsedMilliseconds} ms");
                Console.WriteLine($"   平均每次加载耗时: {times.Average():F2} ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   设置加载测试失败: {ex.Message}");
            }

            return new PerformanceMetric
            {
                Name = "设置加载",
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                AverageTimeMs = times.Count > 0 ? times.Average() : 0,
                MinTimeMs = times.Count > 0 ? times.Min() : 0,
                MaxTimeMs = times.Count > 0 ? times.Max() : 0,
                Operations = 50
            };
        }

        private static async Task<PerformanceMetric> TestStringPerformance()
        {
            Console.WriteLine("\n5. 测试字符串处理性能...");
            var times = new List<long>();
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                var sw = Stopwatch.StartNew();
                var text = string.Join(" ", Enumerable.Range(0, 100).Select(j => $"单词{j}"));
                var result = text.Split(' ').Where(s => s.StartsWith("单")).ToList();
                var replaced = text.Replace("单词", "word");
                var contains = replaced.Contains("word50");
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            stopwatch.Stop();
            Console.WriteLine($"   1000 次字符串处理操作总耗时: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"   平均每次操作耗时: {times.Average():F2} ms");

            return new PerformanceMetric
            {
                Name = "字符串处理",
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                AverageTimeMs = times.Average(),
                MinTimeMs = times.Min(),
                MaxTimeMs = times.Max(),
                Operations = 1000
            };
        }

        private static async Task<PerformanceMetric> TestCollectionPerformance()
        {
            Console.WriteLine("\n6. 测试集合操作性能...");
            var times = new List<long>();
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                var sw = Stopwatch.StartNew();
                var list = Enumerable.Range(0, 1000).ToList();
                var filtered = list.Where(x => x % 2 == 0).ToList();
                var dict = filtered.ToDictionary(x => x, x => x * 2);
                var contains = dict.ContainsKey(500);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            stopwatch.Stop();
            Console.WriteLine($"   1000 次集合操作总耗时: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"   平均每次操作耗时: {times.Average():F2} ms");

            return new PerformanceMetric
            {
                Name = "集合操作",
                TotalTimeMs = stopwatch.ElapsedMilliseconds,
                AverageTimeMs = times.Average(),
                MinTimeMs = times.Min(),
                MaxTimeMs = times.Max(),
                Operations = 1000
            };
        }

        private static async Task<PerformanceMetric> TestParallelInitialization()
        {
            Console.WriteLine("\n7. 测试并行初始化性能...");

            var initializationSteps = new Func<Task>[]
            {
                () => SimulateInitialization("步骤1", 50),
                () => SimulateInitialization("步骤2", 75),
                () => SimulateInitialization("步骤3", 40),
                () => SimulateInitialization("步骤4", 100),
                () => SimulateInitialization("步骤5", 60)
            };

            var stopwatch = Stopwatch.StartNew();

            foreach (var step in initializationSteps)
                await step();

            var serialTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var parallelTasks = initializationSteps.Select(step => step()).ToArray();
            await Task.WhenAll(parallelTasks);
            var parallelTime = stopwatch.ElapsedMilliseconds;

            var improvement = ((double)(serialTime - parallelTime) / serialTime) * 100;

            Console.WriteLine($"   串行初始化耗时: {serialTime} ms");
            Console.WriteLine($"   并行初始化耗时: {parallelTime} ms");
            Console.WriteLine($"   性能提升: {improvement:F1}% ({serialTime / (double)parallelTime:F2}x)");

            return new PerformanceMetric
            {
                Name = "并行初始化",
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
            Log.Instance.Info($"初始化{name}完成，耗时{delayMs}ms");
        }

        private static async Task SaveResultsToFile(Dictionary<string, PerformanceMetric> results)
        {
            try
            {
                var outputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"PerformanceBenchmark_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                var lines = new List<string>
                {
                    "Lenovo Legion Toolkit 性能基准测试报告",
                    $"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    "",
                    "测试结果详情:",
                    "".PadRight(80, '-'),
                    $"{nameof(PerformanceMetric.Name),-20} | {nameof(PerformanceMetric.Operations),-10} | {nameof(PerformanceMetric.TotalTimeMs),-12} | {nameof(PerformanceMetric.AverageTimeMs),-12} | {nameof(PerformanceMetric.MinTimeMs),-12} | {nameof(PerformanceMetric.MaxTimeMs),-12}",
                    "".PadRight(80, '-')
                };

                foreach (var result in results.Values)
                {
                    lines.Add($"{result.Name,-20} | {result.Operations,-10} | {result.TotalTimeMs,12:F2} | {result.AverageTimeMs,12:F2} | {result.MinTimeMs,12:F2} | {result.MaxTimeMs,12:F2}");
                }

                lines.Add("".PadRight(80, '-'));
                lines.Add($"总测试耗时: {results.Sum(r => r.Value.TotalTimeMs):F2} ms");
                lines.Add("");
                lines.Add("性能分析建议:");
                lines.Add("1. WMI查询耗时较长，建议实施缓存机制");
                lines.Add("2. 文件IO操作频繁，建议使用异步操作和批量处理");
                lines.Add("3. 设置加载可考虑内存缓存");
                lines.Add("4. 并行初始化可显著提升启动性能");

                await File.WriteAllLinesAsync(outputPath, lines).ConfigureAwait(false);
                Console.WriteLine($"\n详细报告已保存至: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n保存报告失败: {ex.Message}");
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

        private class TestSettings : AbstractSettings<object>
        {
            public TestSettings(string filename) : base(filename) { }
        }
    }
}
