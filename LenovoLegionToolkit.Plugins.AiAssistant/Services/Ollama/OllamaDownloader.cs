using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.AiAssistant.Services.Ollama;

/// <summary>
/// Handles automatic download of Ollama executable
/// </summary>
public class OllamaDownloader
{
    // Ollama Windows download URL (latest stable version)
    private const string OllamaDownloadUrl = "https://github.com/ollama/ollama/releases/latest/download/OllamaSetup.exe";
    
    /// <summary>
    /// Get the target directory for downloaded Ollama
    /// </summary>
    private static string GetOllamaDirectory()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyLocation = assembly.Location;
        
        if (string.IsNullOrEmpty(assemblyLocation))
            throw new InvalidOperationException("Cannot determine plugin directory");
        
        var pluginDirectory = Path.GetDirectoryName(assemblyLocation);
        if (string.IsNullOrEmpty(pluginDirectory))
            throw new InvalidOperationException("Cannot determine plugin directory");
        
        return Path.Combine(pluginDirectory, "Ollama");
    }
    
    /// <summary>
    /// Check if Ollama is already downloaded
    /// </summary>
    public static bool IsOllamaDownloaded()
    {
        try
        {
            var ollamaDir = GetOllamaDirectory();
            var ollamaExe = Path.Combine(ollamaDir, "ollama.exe");
            return File.Exists(ollamaExe);
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Download Ollama installer and extract executable
    /// </summary>
    public static async Task<string> DownloadOllamaAsync(IProgress<float>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var ollamaDir = GetOllamaDirectory();
            Directory.CreateDirectory(ollamaDir);
            
            var installerPath = Path.Combine(Path.GetTempPath(), $"OllamaSetup_{Guid.NewGuid()}.exe");
            var ollamaExePath = Path.Combine(ollamaDir, "ollama.exe");
            
            // Download installer
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Downloading Ollama from: {OllamaDownloadUrl}");
            
            // Report download start
            progress?.Report(0.0f);
            
            await using (var fileStream = File.OpenWrite(installerPath))
            {
                await httpClient.DownloadAsync(OllamaDownloadUrl, fileStream, progress, cancellationToken);
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama installer downloaded to: {installerPath}");
            
            // Report extraction start (90% progress)
            progress?.Report(0.9f);
            
            // Extract ollama.exe from installer
            var extractResult = await ExtractOllamaFromInstallerAsync(installerPath, ollamaDir, progress, cancellationToken);
            
            // Clean up installer
            try
            {
                if (File.Exists(installerPath))
                    File.Delete(installerPath);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error cleaning up installer: {ex.Message}");
            }
            
            if (extractResult && File.Exists(ollamaExePath))
            {
                // Report completion
                progress?.Report(1.0f);
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Ollama extracted successfully to: {ollamaExePath}");
                return ollamaExePath;
            }
            
            throw new InvalidOperationException("Failed to extract Ollama executable from installer. Please try downloading manually from https://ollama.com");
        }
        catch (OperationCanceledException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Ollama download was cancelled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            var errorMsg = $"Network error while downloading Ollama: {ex.Message}. Please check your internet connection.";
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Network error while downloading Ollama: {ex.Message}. Please check your internet connection.", ex);
            throw new InvalidOperationException(errorMsg, ex);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error downloading Ollama: {ex.Message}";
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error downloading Ollama: {ex.Message}", ex);
            throw new InvalidOperationException(errorMsg, ex);
        }
    }
    
    /// <summary>
    /// Extract Ollama executable from installer
    /// </summary>
    private static async Task<bool> ExtractOllamaFromInstallerAsync(string installerPath, string targetDir, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to use 7-Zip or similar to extract
            // For now, we'll try to run the installer in silent mode and extract from temp directory
            // This is a simplified approach - in production, you might want to use a proper extraction library
            
            // Alternative: Download the portable version directly if available
            // Or use the installer's /SILENT /DIR= option to install to a temp location, then copy
            
            // For simplicity, we'll try to find ollama.exe in common temp locations after silent install
            var tempInstallDir = Path.Combine(Path.GetTempPath(), "OllamaTemp");
            Directory.CreateDirectory(tempInstallDir);
            
            try
            {
                // Try multiple silent install parameters
                var installArgs = new[] { "/S", "/SILENT", "/VERYSILENT" };
                
                foreach (var arg in installArgs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = installerPath,
                        Arguments = $"{arg} /D={tempInstallDir}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    
                    using var process = Process.Start(processStartInfo);
                    if (process != null)
                    {
                        await process.WaitForExitAsync(cancellationToken);
                        
                        // Look for ollama.exe in the temp install directory
                        var ollamaExe = Path.Combine(tempInstallDir, "ollama.exe");
                        if (File.Exists(ollamaExe))
                        {
                            progress?.Report(0.95f);
                            
                            var targetExe = Path.Combine(targetDir, "ollama.exe");
                            File.Copy(ollamaExe, targetExe, true);
                            
                            progress?.Report(0.98f);
                            
                            // Clean up temp directory
                            try
                            {
                                Directory.Delete(tempInstallDir, true);
                            }
                            catch (Exception ex)
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"Error cleaning up temp directory: {ex.Message}");
                            }
                            
                            return true;
                        }
                        
                        // Also check subdirectories
                        var subDirs = Directory.GetDirectories(tempInstallDir, "*", SearchOption.AllDirectories);
                        foreach (var subDir in subDirs)
                        {
                            var ollamaExeInSubDir = Path.Combine(subDir, "ollama.exe");
                            if (File.Exists(ollamaExeInSubDir))
                            {
                                progress?.Report(0.95f);
                                
                                var targetExe = Path.Combine(targetDir, "ollama.exe");
                                File.Copy(ollamaExeInSubDir, targetExe, true);
                                
                                progress?.Report(0.98f);
                                
                                try
                                {
                                    Directory.Delete(tempInstallDir, true);
                                }
                                catch
                                {
                                    // Ignore cleanup errors
                                }
                                
                                return true;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error during extraction: {ex.Message}");
            }
            
            // Clean up temp directory
            try
            {
                if (Directory.Exists(tempInstallDir))
                    Directory.Delete(tempInstallDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
}

