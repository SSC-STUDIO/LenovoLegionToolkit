using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.Tools;

public class ToolPlugin : PluginBase
{
    private readonly string _toolsDirectory;
    private readonly Dictionary<string, ToolInfo> _tools = new();

    public ToolPlugin(string pluginId, string pluginName, string pluginDescription, string pluginVersion)
        : base(pluginId, pluginName, pluginDescription, pluginVersion)
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _toolsDirectory = Path.Combine(appDirectory, "tools");
    }

    public override void OnInitialize()
    {
        base.OnInitialize();
        LoadTools();
    }

    private void LoadTools()
    {
        try
        {
            var toolsJsonPath = Path.Combine(_toolsDirectory, "tools.json");
            if (!File.Exists(toolsJsonPath))
                return;

            var json = File.ReadAllText(toolsJsonPath);
            var toolsData = Newtonsoft.Json.JsonConvert.DeserializeObject<ToolsData>(json);
            
            if (toolsData?.Categories != null)
            {
                foreach (var category in toolsData.Categories)
                {
                    if (category?.Tools != null)
                    {
                        foreach (var tool in category.Tools)
                        {
                            if (tool != null && !string.IsNullOrEmpty(tool.Name))
                            {
                                _tools[tool.Name] = new ToolInfo
                                {
                                    Name = tool.Name,
                                    DisplayName = tool.DisplayName ?? tool.Name,
                                    Description = tool.Description ?? "",
                                    DescriptionEn = tool.DescriptionEn ?? "",
                                    Version = tool.Version ?? "1.0",
                                    Author = tool.Author ?? "",
                                    Category = category.Name,
                                    Command = tool.Command ?? ""
                                };
                            }
                        }
                    }
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ToolPlugin: Loaded {_tools.Count} tools from {toolsJsonPath}");
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"ToolPlugin: Error loading tools: {ex.Message}", ex);
        }
    }

    public bool LaunchTool(string toolName)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ToolPlugin: Tool not found: {toolName}");
            return false;
        }

        try
        {
            if (!string.IsNullOrEmpty(tool.Command))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {tool.Command}",
                    UseShellExecute = false
                });
            }
            else
            {
                var toolPath = FindToolPath(toolName);
                if (!string.IsNullOrEmpty(toolPath) && File.Exists(toolPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = toolPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"ToolPlugin: Tool file not found: {toolName}");
                    return false;
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ToolPlugin: Launched tool: {toolName}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"ToolPlugin: Error launching tool {toolName}: {ex.Message}", ex);
            return false;
        }
    }

    private string? FindToolPath(string toolName)
    {
        try
        {
            if (!Directory.Exists(_toolsDirectory))
                return null;

            var files = Directory.GetFiles(_toolsDirectory, $"{toolName}.*", SearchOption.AllDirectories);
            return files.FirstOrDefault(f => 
                f.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"ToolPlugin: Error finding tool path for {toolName}: {ex.Message}", ex);
            return null;
        }
    }

    public List<ToolInfo> GetToolsByCategory(string category)
    {
        return _tools.Values.Where(t => t.Category == category).ToList();
    }

    public List<ToolInfo> GetAllTools()
    {
        return _tools.Values.ToList();
    }

    public ToolInfo? GetTool(string toolName)
    {
        return _tools.TryGetValue(toolName, out var tool) ? tool : null;
    }
}

public class ToolsData
{
    public string? Version { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public List<ToolCategory>? Categories { get; set; }
}

public class ToolCategory
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? DisplayNameEn { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public List<Tool>? Tools { get; set; }
}

public class Tool
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? DescriptionEn { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public string? Command { get; set; }
}

public class ToolInfo
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
    public string Version { get; set; } = "1.0";
    public string Author { get; set; } = "";
    public string Category { get; set; } = "";
    public string Command { get; set; } = "";
}