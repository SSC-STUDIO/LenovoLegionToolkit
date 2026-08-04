using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using UniversalDeviceToolkit.Plugins.Shared;

namespace UniversalDeviceToolkit.Plugins.Tests.PerformanceTests;

/// <summary>
/// Diagnose SettingsManager.Save() performance bottleneck.
/// </summary>
public class SavePerformanceDiagnostics
{
    private readonly string _testPluginName = "PerfDiagPlugin";
    private SettingsManager<TestSettings>? _settingsManager;

    public void RunDiagnostics()
    {
        Console.WriteLine("=== Settings Save Performance Diagnostics ===");
        Console.WriteLine();

        // Setup
        var testDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UniversalDeviceToolkit", "plugins", _testPluginName);
        if (Directory.Exists(testDir)) Directory.Delete(testDir, recursive: true);
        _settingsManager = new SettingsManager<TestSettings>(_testPluginName);

        var settings = new TestSettings { Name = "Test", Enabled = true, Count = 42 };

        // Warm-up
        _settingsManager.Save(settings);

        // Diagnose individual steps
        DiagnoseJsonSerialization(settings);
        DiagnoseFileWrite(settings);
        DiagnoseFullSave(settings);

        // Cleanup
        _settingsManager.Clear(deleteFile: true);
        Console.WriteLine("=== Diagnostics Complete ===");
    }

    private void DiagnoseJsonSerialization(TestSettings settings)
    {
        Console.WriteLine("## JSON Serialization");
        var sw = Stopwatch.StartNew();
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        sw.Stop();
        Console.WriteLine($"  Serialize: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  JSON length: {json.Length} chars");
        Console.WriteLine();
    }

    private void DiagnoseFileWrite(TestSettings settings)
    {
        Console.WriteLine("## File Write (no serialize)");
        var tempPath = Path.GetTempFileName();
        var sw = Stopwatch.StartNew();
        File.WriteAllText(tempPath, "test", Encoding.UTF8);
        sw.Stop();
        Console.WriteLine($"  WriteAllText (small): {sw.ElapsedMilliseconds} ms");
        File.Delete(tempPath);

        // With actual JSON
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        tempPath = Path.GetTempFileName();
        sw.Restart();
        File.WriteAllText(tempPath, json, Encoding.UTF8);
        sw.Stop();
        Console.WriteLine($"  WriteAllText (real JSON): {sw.ElapsedMilliseconds} ms");
        File.Delete(tempPath);
        Console.WriteLine();
    }

    private void DiagnoseFullSave(TestSettings settings)
    {
        Console.WriteLine("## Full Save()");
        var sw = Stopwatch.StartNew();
        _settingsManager!.Save(settings);
        sw.Stop();
        Console.WriteLine($"  Save(): {sw.ElapsedMilliseconds} ms");
        Console.WriteLine();

        Console.WriteLine("## SaveWithDebounce (3 rapid calls)");
        var debounceManager = new SettingsManager<TestSettings>("PerfDiagPluginDebounce", null, null, false, true, 500);
        var saveTimes = new List<long>();
        for (int i = 0; i < 3; i++)
        {
            sw.Restart();
            debounceManager.SaveWithDebounce(settings);
            sw.Stop();
            saveTimes.Add(sw.ElapsedMilliseconds);
        }
        Console.WriteLine($"  Call 1: {saveTimes[0]}ms (queued)");
        Console.WriteLine($"  Call 2: {saveTimes[1]}ms (queued)");
        Console.WriteLine($"  Call 3: {saveTimes[2]}ms (queued)");
        Console.WriteLine("  Actual save will happen 500ms after last call");
        Console.WriteLine();

        // Wait for debounce to execute
        Thread.Sleep(1000);
        Console.WriteLine("  Debounce save completed");
        Console.WriteLine();
    }
}
