using System.Text.Json;
using System.Text.Json.Serialization;
using LibreHardwareMonitor.Hardware;

var options = DumpOptions.Parse(args);
var jsonSerializerOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var computer = new Computer
{
    IsCpuEnabled = true,
    IsGpuEnabled = true,
    IsMemoryEnabled = true,
    IsMotherboardEnabled = true,
    IsStorageEnabled = true,
    IsControllerEnabled = true,
    IsNetworkEnabled = false,
};

try
{
    computer.Open();
    computer.Accept(new UpdateVisitor());

    var snapshot = new DumpSnapshot(
        DateTimeOffset.UtcNow,
        Environment.MachineName,
        computer.Hardware.Select(HardwareSnapshot.FromHardware).ToArray());

    var json = JsonSerializer.Serialize(snapshot, jsonSerializerOptions);

    if (!string.IsNullOrWhiteSpace(options.OutputPath))
    {
        var outputPath = Path.GetFullPath(options.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        await File.WriteAllTextAsync(outputPath, json);
        Console.WriteLine($"Wrote sensor inventory to: {outputPath}");
    }
    else
    {
        Console.WriteLine(json);
    }
}
finally
{
    computer.Close();
}

return 0;

sealed record DumpOptions(string? OutputPath)
{
    public static DumpOptions Parse(string[] args)
    {
        string? outputPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            if (argument.Equals("--output", StringComparison.OrdinalIgnoreCase) && index + 1 < args.Length)
            {
                outputPath = args[++index];
                continue;
            }

            if (argument.StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
            {
                outputPath = argument["--output=".Length..];
            }
        }

        return new DumpOptions(outputPath);
    }
}

sealed record DumpSnapshot(
    DateTimeOffset GeneratedAtUtc,
    string MachineName,
    HardwareSnapshot[] Hardware);

sealed record HardwareSnapshot(
    string Name,
    string Type,
    string Identifier,
    SensorSnapshot[] Sensors,
    HardwareSnapshot[] Children)
{
    public static HardwareSnapshot FromHardware(IHardware hardware)
    {
        var sensors = hardware.Sensors
            .OrderBy(sensor => sensor.SensorType.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(sensor => sensor.Name, StringComparer.OrdinalIgnoreCase)
            .Select(SensorSnapshot.FromSensor)
            .ToArray();

        var children = hardware.SubHardware
            .Select(FromHardware)
            .ToArray();

        return new HardwareSnapshot(
            hardware.Name,
            hardware.HardwareType.ToString(),
            hardware.Identifier.ToString(),
            sensors,
            children);
    }
}

sealed record SensorSnapshot(
    string Name,
    string Type,
    string Identifier,
    float? Value,
    float? Min,
    float? Max)
{
    public static SensorSnapshot FromSensor(ISensor sensor) =>
        new(
            sensor.Name,
            sensor.SensorType.ToString(),
            sensor.Identifier.ToString(),
            sensor.Value,
            sensor.Min,
            sensor.Max);
}

sealed class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();

        foreach (var subHardware in hardware.SubHardware)
            subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor)
    {
    }

    public void VisitParameter(IParameter parameter)
    {
    }
}
