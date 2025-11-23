using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.PerformanceTest
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Lenovo Legion Toolkit 性能测试");
            Console.WriteLine("==================================");

            // 测试日志性能
            await TestLogPerformance();
            
            // 测试并行初始化性能
            await TestParallelInitialization();
            
            Console.WriteLine("\n性能测试完成。");
        }

        private static async Task TestLogPerformance()
        {
            Console.WriteLine("\n1. 测试日志性能...");
            var stopwatch = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (int i = 0; i < 1000; i++)
            {
                int taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    Log.Instance.Info($"测试日志消息 #{taskId}");
                }));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            Console.WriteLine($"   1000 条并发日志消息耗时: {stopwatch.ElapsedMilliseconds} ms");
        }

        private static async Task TestParallelInitialization()
        {
            Console.WriteLine("\n2. 测试并行初始化性能...");
            
            // 模拟初始化步骤
            var initializationSteps = new Func<Task>[]
            {
                () => SimulateInitialization("步骤1", 100),
                () => SimulateInitialization("步骤2", 150),
                () => SimulateInitialization("步骤3", 80),
                () => SimulateInitialization("步骤4", 200),
                () => SimulateInitialization("步骤5", 120)
            };

            // 串行执行
            var stopwatch = Stopwatch.StartNew();
            foreach (var step in initializationSteps)
                await step();
            stopwatch.Stop();
            var serialTime = stopwatch.ElapsedMilliseconds;

            // 并行执行
            stopwatch.Restart();
            var parallelTasks = new List<Task>();
            foreach (var step in initializationSteps)
                parallelTasks.Add(step());
            await Task.WhenAll(parallelTasks);
            stopwatch.Stop();
            var parallelTime = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"   串行初始化耗时: {serialTime} ms");
            Console.WriteLine($"   并行初始化耗时: {parallelTime} ms");
            Console.WriteLine($"   性能提升: {Math.Round((double)serialTime / parallelTime, 2)}x");
        }

        private static async Task SimulateInitialization(string name, int delayMs)
        {
            await Task.Delay(delayMs); // 模拟初始化耗时操作
            Log.Instance.Info($"初始化{name}完成，耗时{delayMs}ms");
        }
    }
}