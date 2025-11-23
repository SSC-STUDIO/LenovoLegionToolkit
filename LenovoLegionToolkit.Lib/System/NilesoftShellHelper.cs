using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

using ToolkitRegistry = LenovoLegionToolkit.Lib.System.Registry;

namespace LenovoLegionToolkit.Lib.System;

public static class NilesoftShellHelper
{
    private const string NilesoftShellContextMenuClsid = "{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}";

    public static bool IsInstalled()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking if Nilesoft Shell is installed...");

        var shellExePath = GetNilesoftShellExePath();
        var isInstalled = !string.IsNullOrWhiteSpace(shellExePath) && File.Exists(shellExePath);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Nilesoft Shell installation check result: {isInstalled}, path: {shellExePath ?? "null"}");

        return isInstalled;
    }

    public static bool IsInstalledUsingShellExe()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking if Nilesoft Shell is installed using shell.exe API...");

        try
        {
            var shellExePath = GetNilesoftShellExePath();
            if (string.IsNullOrWhiteSpace(shellExePath) || !File.Exists(shellExePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell executable not found at path: {shellExePath ?? "null"}");
                return false;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Querying shell.exe installation status via: {shellExePath} -isinstalled");

            // First, try to read from registry (shell.exe writes status there to avoid console output issues)
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\LenovoLegionToolkit", false);
                if (key != null)
                {
                    var value = key.GetValue("ShellInstalled");
                    if (value != null && value is int intValue)
                    {
                        var isInstalledFromRegistry = intValue != 0;
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Nilesoft Shell installation status (from registry): {isInstalledFromRegistry}");
                        return isInstalledFromRegistry;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to read from registry, falling back to stdout", ex);
            }

            // Fallback: Use shell.exe's stdout output (for backward compatibility)
            // Use shell.exe's built-in API to check installation status
            // This is more accurate as shell.exe can check its own installation state
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = shellExePath,
                    Arguments = "-isinstalled",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            // Read streams asynchronously to prevent deadlock
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            // Wait for both read tasks to complete before calling WaitForExit
            // This prevents deadlock when process output exceeds buffer capacity
            // The process can write to buffers while we're reading from them
            Task.WaitAll(outputTask, errorTask);

            // Now safe to wait for process exit since all output has been read
            process.WaitForExit();

            // Get the results (already completed)
            var output = outputTask.Result;
            var error = errorTask.Result;

            // Parse output: shell.exe outputs "true" or "false"
            if (string.IsNullOrWhiteSpace(output))
            {
                // If no output, check error output as fallback
                if (!string.IsNullOrWhiteSpace(error))
                {
                    var errorResult = error.Trim().ToLowerInvariant();
                    if (errorResult == "true" || errorResult == "false")
                    {
                        var isInstalledFromError = errorResult == "true";
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Nilesoft Shell installation status (from stderr): {isInstalledFromError}");
                        return isInstalledFromError;
                    }
                }
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Nilesoft Shell installation status query returned empty output");
                return false;
            }

            var outputResult = output.Trim().ToLowerInvariant();
            var isInstalled = outputResult == "true";
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Nilesoft Shell installation status (from shell.exe stdout): {isInstalled}, output: {output.Trim()}");

            return isInstalled;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to query shell.exe installation status, falling back to file/registry check", ex);
            
            // Fallback: If we can't query shell.exe, just check if file exists
            // Note: Without shell.exe API, we can't accurately determine registration status
            return IsInstalled();
        }
    }

    public static string? GetNilesoftShellExePath()
    {
        try
        {
            // 1) Prefer current process directory (where our app runs)
            var baseDir = AppContext.BaseDirectory;
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Searching for shell.exe, AppContext.BaseDirectory: {baseDir ?? "null"}");
            
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                // direct file in base dir
                var directCandidate = Path.Combine(baseDir, "shell.exe");
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Checking direct path: {directCandidate}, exists: {File.Exists(directCandidate)}");
                
                if (File.Exists(directCandidate))
                {
                    var dllPath = Path.Combine(baseDir, "shell.dll");
                    if (File.Exists(dllPath))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe at direct path: {directCandidate}");
                        return directCandidate;
                    }
                    else if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"shell.exe found but shell.dll missing at: {dllPath}");
                }

                // search recursively under base dir (covers ThirdParty/Shell/src/bin/**)
                try
                {
                    var files = Directory.GetFiles(baseDir, "shell.exe", SearchOption.AllDirectories);
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Found {files.Length} shell.exe file(s) in recursive search");
                    
                    foreach (var file in files)
                    {
                        var dir = Path.GetDirectoryName(file);
                        if (!string.IsNullOrWhiteSpace(dir))
                        {
                            var dllPath = Path.Combine(dir, "shell.dll");
                            if (File.Exists(dllPath))
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Found shell.exe at recursive path: {file}");
                                return file;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Recursive search failed: {ex.Message}");
                }
            }

            // 2) Check build directory (common location during development)
            try
            {
                var currentDir = Directory.GetCurrentDirectory();
                var buildDir = Path.Combine(currentDir, "build");
                if (Directory.Exists(buildDir))
                {
                    var buildCandidate = Path.Combine(buildDir, "shell.exe");
                    var buildDll = Path.Combine(buildDir, "shell.dll");
                    if (File.Exists(buildCandidate) && File.Exists(buildDll))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe in build directory: {buildCandidate}");
                        return buildCandidate;
                    }
                }
                
                // Also check parent directory's build folder
                var parentDir = Directory.GetParent(currentDir)?.FullName;
                if (!string.IsNullOrWhiteSpace(parentDir))
                {
                    var parentBuildDir = Path.Combine(parentDir, "build");
                    if (Directory.Exists(parentBuildDir))
                    {
                        var parentBuildCandidate = Path.Combine(parentBuildDir, "shell.exe");
                        var parentBuildDll = Path.Combine(parentBuildDir, "shell.dll");
                        if (File.Exists(parentBuildCandidate) && File.Exists(parentBuildDll))
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Found shell.exe in parent build directory: {parentBuildCandidate}");
                            return parentBuildCandidate;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Build directory search failed: {ex.Message}");
            }

            // 3) Fallback to default installation path
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                var candidate = Path.Combine(programFiles, "Nilesoft Shell", "shell.exe");
                if (File.Exists(candidate))
                {
                    var dllPath = Path.Combine(programFiles, "Nilesoft Shell", "shell.dll");
                    if (File.Exists(dllPath))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Found shell.exe in ProgramFiles: {candidate}");
                        return candidate;
                    }
                }
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"shell.exe not found in any searched location");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"GetNilesoftShellExePath failed: {ex.Message}", ex);
        }
        return null;
    }

}

