using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Services.Ollama;

/// <summary>
/// Manages Ollama service lifecycle (detection, startup, shutdown)
/// </summary>
public class OllamaServiceManager
{
    private static readonly string[] CommonOllamaPaths = new[]
    {
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ollama", "ollama.exe"),
        "ollama.exe" // In PATH
    };

    private Process? _ollamaProcess;
    private readonly object _lock = new();

    /// <summary>
    /// Get the built-in Ollama executable path (if available)
    /// </summary>
    private static string? GetBuiltInOllamaPath()
    {
        try
        {
            // Get the plugin assembly location
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyLocation = assembly.Location;
            
            if (string.IsNullOrEmpty(assemblyLocation))
                return null;

            var pluginDirectory = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrEmpty(pluginDirectory))
                return null;

            // Check for built-in Ollama in the plugin directory
            var builtInOllamaPath = Path.Combine(pluginDirectory, "Ollama", "ollama.exe");
            if (File.Exists(builtInOllamaPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Found built-in Ollama at: {builtInOllamaPath}");
                return builtInOllamaPath;
            }

            // Also check for ollama.exe directly in Ollama directory (for portable versions)
            var builtInOllamaDir = Path.Combine(pluginDirectory, "Ollama");
            if (Directory.Exists(builtInOllamaDir))
            {
                var ollamaExe = Directory.GetFiles(builtInOllamaDir, "ollama.exe", SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
                if (!string.IsNullOrEmpty(ollamaExe) && File.Exists(ollamaExe))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Found built-in Ollama at: {ollamaExe}");
                    return ollamaExe;
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error checking for built-in Ollama: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Find Ollama executable path (prioritizes built-in version)
    /// </summary>
    public static string? FindOllamaExecutable()
    {
        // First, check for built-in Ollama (highest priority)
        var builtInPath = GetBuiltInOllamaPath();
        if (!string.IsNullOrEmpty(builtInPath))
            return builtInPath;

        // Then check common installation paths
        foreach (var path in CommonOllamaPaths)
        {
            if (path == "ollama.exe")
            {
                // Check if ollama is in PATH
                try
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "where.exe",
                        Arguments = "ollama",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processStartInfo);
                    if (process != null)
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        
                        if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            var firstPath = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .FirstOrDefault();
                            if (!string.IsNullOrEmpty(firstPath) && File.Exists(firstPath))
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Found Ollama in PATH: {firstPath}");
                                return firstPath;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error checking PATH for Ollama: {ex.Message}");
                }
            }
            else if (File.Exists(path))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Found Ollama at: {path}");
                return path;
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Ollama executable not found in common locations");
        return null;
    }

    /// <summary>
    /// Check if Ollama service is running
    /// </summary>
    public static async Task<bool> IsServiceRunningAsync(string baseUrl = "http://localhost:11434", CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(5)
            };

            var response = await client.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Start Ollama service if not running
    /// </summary>
    public async Task<bool> StartServiceAsync(string? ollamaPath = null, CancellationToken cancellationToken = default)
    {
        // Check if already running
        lock (_lock)
        {
            if (_ollamaProcess != null && !_ollamaProcess.HasExited)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama process already running");
                return true;
            }
        }

        // Check if service is already running (maybe started externally)
        if (await IsServiceRunningAsync(cancellationToken: cancellationToken))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama service already running (external)");
            return true;
        }

        // Find Ollama executable if not provided
        // If ollamaPath is null, FindOllamaExecutable will check built-in first, then external
        // If ollamaPath is provided, use it directly (user specified external path)
        var executablePath = ollamaPath ?? FindOllamaExecutable();
        if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama executable not found");
            throw new FileNotFoundException("Ollama executable not found. Please install Ollama, use the built-in version, or specify the path in settings.");
        }

        try
        {
            lock (_lock)
            {
                // Start Ollama service
                _ollamaProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = "serve",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                _ollamaProcess.Start();

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Started Ollama service: {executablePath}");
            }

            // Wait for service to be ready (poll with timeout)
            var maxWaitTime = TimeSpan.FromSeconds(30);
            var checkInterval = TimeSpan.FromSeconds(1);
            var elapsed = TimeSpan.Zero;

            while (elapsed < maxWaitTime)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (await IsServiceRunningAsync(cancellationToken: cancellationToken))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Ollama service is ready");
                    return true;
                }

                await Task.Delay(checkInterval, cancellationToken);
                elapsed = elapsed.Add(checkInterval);
            }

            // Timeout - service didn't start
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama service startup timeout");
            
            lock (_lock)
            {
                _ollamaProcess?.Kill();
                _ollamaProcess?.Dispose();
                _ollamaProcess = null;
            }

            return false;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to start Ollama service: {ex.Message}", ex);
            
            lock (_lock)
            {
                _ollamaProcess?.Dispose();
                _ollamaProcess = null;
            }
            
            throw;
        }
    }

    /// <summary>
    /// Stop Ollama service (if started by this manager)
    /// </summary>
    public void StopService()
    {
        lock (_lock)
        {
            if (_ollamaProcess != null && !_ollamaProcess.HasExited)
            {
                try
                {
                    _ollamaProcess.Kill();
                    _ollamaProcess.WaitForExit(5000);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Stopped Ollama service");
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error stopping Ollama service: {ex.Message}", ex);
                }
                finally
                {
                    _ollamaProcess?.Dispose();
                    _ollamaProcess = null;
                }
            }
        }
    }

    /// <summary>
    /// Ensure Ollama service is running, start if needed
    /// </summary>
    public async Task<bool> EnsureServiceRunningAsync(string? ollamaPath = null, bool autoStart = true, CancellationToken cancellationToken = default)
    {
        // Check if already running
        if (await IsServiceRunningAsync(cancellationToken: cancellationToken))
            return true;

        if (!autoStart)
            return false;

        // Try to start
        return await StartServiceAsync(ollamaPath, cancellationToken);
    }
}

