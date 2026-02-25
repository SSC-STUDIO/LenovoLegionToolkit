using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Script;

/// <summary>
/// Script manager service implementation
/// </summary>
public class ScriptManagerService : IScriptManagerService
{
    private bool _isEnabled;
    private readonly List<ScriptInfo> _scripts = new();
    private static readonly string ScriptsDirectory = Path.Combine(
        Folders.AppData,
        "NetworkAcceleration",
        "Scripts");
    private static readonly string ScriptsIndexFile = Path.Combine(
        ScriptsDirectory,
        "scripts.json");

    public bool IsEnabled => _isEnabled;

    public ScriptManagerService()
    {
        // Directory creation is deferred to first use to avoid blocking constructor
        // Directory will be created in LoadScriptsFromStorageAsync if needed
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Script injection {(enabled ? "enabled" : "disabled")}.");
    }

    public async Task<IEnumerable<ScriptInfo>> GetScriptsAsync()
    {
        try
        {
            await LoadScriptsFromStorageAsync();
            return _scripts.AsEnumerable();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading scripts: {ex.Message}", ex);
            return _scripts.AsEnumerable();
        }
    }

    public async Task<string?> LoadScriptContentAsync(string scriptId)
    {
        try
        {
            var script = _scripts.FirstOrDefault(s => s.Id == scriptId);
            if (script == null || string.IsNullOrEmpty(script.Path))
            {
                return null;
            }

            var scriptPath = script.Path;
            if (!Path.IsPathRooted(scriptPath))
            {
                // Relative path, combine with scripts directory
                scriptPath = Path.Combine(ScriptsDirectory, scriptPath);
            }

            if (File.Exists(scriptPath))
            {
                return await File.ReadAllTextAsync(scriptPath);
            }

            return null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading script content: {ex.Message}", ex);
            return null;
        }
    }

    private async Task LoadScriptsFromStorageAsync()
    {
        try
        {
            _scripts.Clear();

            // Ensure scripts directory exists
            if (!Directory.Exists(ScriptsDirectory))
            {
                Directory.CreateDirectory(ScriptsDirectory);
            }

            // Load scripts index file
            if (File.Exists(ScriptsIndexFile))
            {
                var json = await File.ReadAllTextAsync(ScriptsIndexFile);
                var scriptDataList = JsonSerializer.Deserialize<List<ScriptData>>(json);

                if (scriptDataList != null)
                {
                    foreach (var scriptData in scriptDataList)
                    {
                        if (string.IsNullOrEmpty(scriptData.Path))
                            continue;
                            
                        var scriptPath = scriptData.Path;
                        if (!Path.IsPathRooted(scriptPath))
                        {
                            scriptPath = Path.Combine(ScriptsDirectory, scriptPath);
                        }

                        // Only add scripts that exist
                        if (File.Exists(scriptPath))
                        {
                            _scripts.Add(new ScriptInfo
                            {
                                Id = scriptData.Id ?? Guid.NewGuid().ToString(),
                                Name = scriptData.Name ?? Path.GetFileName(scriptPath),
                                Path = scriptData.Path,
                                IsEnabled = scriptData.IsEnabled
                            });
                        }
                    }
                }
            }

            // Also scan scripts directory for any .js files not in index
            if (Directory.Exists(ScriptsDirectory))
            {
                var jsFiles = Directory.GetFiles(ScriptsDirectory, "*.js", SearchOption.TopDirectoryOnly);
                foreach (var jsFile in jsFiles)
                {
                    var fileName = Path.GetFileName(jsFile);
                    var relativePath = fileName;

                    // Check if already in list
                    if (!_scripts.Any(s => s.Path == relativePath || s.Path == jsFile))
                    {
                        _scripts.Add(new ScriptInfo
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = Path.GetFileNameWithoutExtension(fileName),
                            Path = relativePath,
                            IsEnabled = false // New scripts default to disabled
                        });
                    }
                }
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Loaded {_scripts.Count} scripts from storage.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading scripts from storage: {ex.Message}", ex);
        }
    }

    private class ScriptData
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public bool IsEnabled { get; set; }
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

