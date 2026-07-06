using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.Tests.PerformanceTests;

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
            "LenovoLegionToolkit", "plugins", _testPluginName);
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

        Console.WriteLine("## Full Save() with MessagePack (after warm-up)");
        var settingsManagerMpck = new SettingsManager<TestSettings>("PerfDiagPluginMpck", null, null, true);
        // Warm-up: serialize once to cache reflection
        settingsManagerMpck.Save(settings);
        // Actual measurement
        sw.Restart();
        settingsManagerMpck.Save(settings);
        sw.Stop();
        Console.WriteLine($"  Save() [MessagePack, warmed up]: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine();

        // Compare file sizes
        var jsonFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LenovoLegionToolkit", "plugins", _testPluginName, "settings.json");
        var mpckFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LenovoLegionToolkit", "plugins", "PerfDiagPluginMpck", "settings.mpack");
        if (File.Exists(jsonFile))
            Console.WriteLine($"  JSON file size: {new FileInfo(jsonFile).Length} bytes");
        if (File.Exists(mpckFile))
            Console.WriteLine($"  MessagePack file size: {new FileInfo(mpckFile).Length} bytes");
        Console.WriteLine();
    }
}
