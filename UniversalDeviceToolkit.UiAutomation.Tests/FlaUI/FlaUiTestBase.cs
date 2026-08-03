// <copyright file="FlaUiTestBase.cs" company="SSC-STUDIO">
// Copyright (c) SSC-STUDIO. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace UniversalDeviceToolkit.Tests.FlaUI
{
    /// <summary>
    /// Base class for FlaUI-based UI automation tests.
    /// Launches the UDT application under test and provides native UI Automation and diagnostics helpers.
    /// </summary>
    public abstract class FlaUiTestBase : IAsyncLifetime
    {
        protected const int DefaultTimeoutMs = 30_000;
        protected const int PollIntervalMs = 500;

        protected Application? App { get; private set; }
        protected UIA3Automation? Automation { get; private set; }
        protected Window? MainWindow { get; private set; }

        private string _appPath = string.Empty;

        protected FlaUiTestBase()
        {
            var solutionDir = FindSolutionDirectory();
            _appPath = Path.Combine(
                solutionDir,
                "Build",
                "Universal Device Toolkit",
                "Universal Device Toolkit.exe");
        }

        protected FlaUiTestBase(string appPath)
        {
            _appPath = appPath;
        }

        /// <summary>
        /// Detects whether the test is running in a CI/headless environment.
        /// Checks for common CI environment variables and the absence of an interactive desktop session.
        /// </summary>
        public static bool IsCiEnvironment()
        {
            var sessionName = Environment.GetEnvironmentVariable("SESSIONNAME");
            if (string.Equals(sessionName, "Services", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(
                    Environment.GetEnvironmentVariable("UDT_ALLOW_FLAUI_TESTS"),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(
                    Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT"),
                    "self-hosted",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var ciVars = new[]
            {
                "CI", "GITHUB_ACTIONS", "TF_BUILD", "JENKINS_URL",
                "GITLAB_CI", "APPVEYOR", "TRAVIS", "CIRCLECI"
            };
            if (ciVars.Any(v => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(v))))
            {
                return true;
            }

            return false;
        }

        public virtual async Task InitializeAsync()
        {
            if (App is not null && !App.HasExited && Automation is not null && MainWindow is not null)
                return;

            if (IsCiEnvironment())
            {
                throw new InvalidOperationException(
                    "FlaUI requires an interactive desktop session. Run the desktop preflight before starting UI tests.");
            }

            _appPath = LocateAppExecutable(_appPath);

            if (!File.Exists(_appPath))
            {
                throw new FileNotFoundException(
                    $"UDT application executable not found at '{_appPath}'. " +
                    "Build the WPF project first (run Make.bat or build UniversalDeviceToolkit.WPF).");
            }

            Automation = new UIA3Automation();

            // Kill any existing UDT instance (SingleInstanceGuard prevents multiples)
            KillExistingInstances();

            try
            {
                App = Application.Launch(_appPath);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                var msg = ex.Message.ToLowerInvariant();
                var isElevationError = msg.Contains("提升") || msg.Contains("elevat") ||
                                        msg.Contains("administrator") || msg.Contains("permission") ||
                                        msg.Contains("denied") || msg.Contains("拒绝");

                if (isElevationError)
                {
                    await DisposeAsync();
                    throw new InvalidOperationException(
                        $"UDT application requires administrator privileges to launch. " +
                        $"The desktop preflight must verify elevation before running FlaUI tests. Original error: {ex.Message}",
                        ex);
                }

                await DisposeAsync();
                throw;
            }

            try
            {
                App.WaitWhileMainHandleIsMissing();
                MainWindow = WaitForMainWindow();
                Assert.NotNull(MainWindow);
            }
            catch
            {
                CaptureDiagnosticScreenshot("InitializeFailure");
                await DisposeAsync();
                throw;
            }
        }

        public virtual async Task DisposeAsync()
        {
            var cleanupFailures = new List<Exception>();

            try
            {
                if (App != null && !App.HasExited)
                {
                    App.Close();
                    // Give the app time to close gracefully
                    await Task.Delay(2000);
                    if (!App.HasExited)
                    {
                        App.Kill();
                        await Task.Delay(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                cleanupFailures.Add(ex);
                CaptureDiagnosticScreenshot("CleanupFailure");
            }
            finally
            {
                try
                {
                    Automation?.Dispose();
                }
                catch (Exception ex)
                {
                    cleanupFailures.Add(ex);
                }

                try
                {
                    App?.Dispose();
                }
                catch (Exception ex)
                {
                    cleanupFailures.Add(ex);
                }

                Automation = null;
                App = null;
                MainWindow = null;
            }

            if (cleanupFailures.Count > 0)
            {
                throw new AggregateException("FlaUI resource cleanup failed.", cleanupFailures);
            }
        }

        /// <summary>
        /// Waits for the main window to appear.
        /// Uses multiple strategies to find the window:
        ///   1. ByProcessId on the desktop
        ///   2. By title substring match
        ///   3. By iterating all top-level windows
        /// </summary>
        protected virtual Window WaitForMainWindow()
        {
            var processId = App!.ProcessId;
            var deadline = DateTime.UtcNow.AddMilliseconds(DefaultTimeoutMs);

            while (DateTime.UtcNow < deadline)
            {
                var desktop = Automation!.GetDesktop();

                // Strategy 1: ByProcessId
                var byPid = desktop.FindFirstChild(c => c.ByProcessId(processId));
                if (byPid != null)
                {
                    return (Window)byPid;
                }

                // Strategy 2: Iterate all top-level windows and check ProcessId
                var allTop = desktop.FindAllChildren();
                foreach (var elem in allTop)
                {
                    try
                    {
                        if (elem.Properties.ProcessId.Value == processId)
                        {
                            return (Window)elem;
                        }
                    }
                    catch
                    {
                        // Skip elements where ProcessId is not available
                    }
                }

                // Strategy 3: By title substring
                foreach (var elem in allTop)
                {
                    try
                    {
                        if (elem.Properties.Name.Value.IndexOf(
                            "Universal Device Toolkit", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return (Window)elem;
                        }
                    }
                    catch
                    {
                        // Skip
                    }
                }

                Thread.Sleep(PollIntervalMs);
            }
            throw new TimeoutException($"Main window not found after {DefaultTimeoutMs}ms (process ID: {processId}).");
        }

        /// <summary>
        /// Captures a screenshot of the main window and extracts visible text using
        /// native FlaUI element tree inspection and screenshot diagnostics.
        /// </summary>
        protected virtual async Task<string[]> ExtractTextFromWindowAsync()
        {
            return await WinRTOcrHelper.ExtractVisibleTextAsync(MainWindow!);
        }

        private static string LocateAppExecutable(string currentPath)
        {
            if (File.Exists(currentPath))
            {
                return currentPath;
            }

            var solutionDir = FindSolutionDirectory();
            var candidates = new List<string>
            {
                // Make.bat / packaging layouts
                Path.Combine(solutionDir, "Build", "Universal Device Toolkit", "Universal Device Toolkit.exe"),
                Path.Combine(solutionDir, "Build", "Universal Device Toolkit.exe"),
                Path.Combine(solutionDir, "Build", "UniversalDeviceToolkit", "Universal Device Toolkit.exe"),
                // x64 platform builds (solution default)
                Path.Combine(solutionDir, "UniversalDeviceToolkit.WPF", "bin", "x64", "Debug", "net10.0-windows10.0.26100.0", "win-x64", "Universal Device Toolkit.exe"),
                Path.Combine(solutionDir, "UniversalDeviceToolkit.WPF", "bin", "x64", "Release", "net10.0-windows10.0.26100.0", "win-x64", "Universal Device Toolkit.exe"),
                // Legacy/non-x64 output layouts
                Path.Combine(solutionDir, "UniversalDeviceToolkit.WPF", "bin", "Debug", "net10.0-windows10.0.26100.0", "win-x64", "Universal Device Toolkit.exe"),
                Path.Combine(solutionDir, "UniversalDeviceToolkit.WPF", "bin", "Debug", "net10.0-windows10.0.26100.0", "win-x86", "Universal Device Toolkit.exe"),
                Path.Combine(solutionDir, "UniversalDeviceToolkit.WPF", "bin", "Release", "net10.0-windows10.0.26100.0", "win-x64", "Universal Device Toolkit.exe"),
                Path.Combine(solutionDir, "UniversalDeviceToolkit.WPF", "bin", "Release", "net10.0-windows10.0.26100.0", "win-x86", "Universal Device Toolkit.exe"),
            };

            // Shallow scan under Build/ for renamed folder layouts used by CI artifacts.
            var buildRoot = Path.Combine(solutionDir, "Build");
            if (Directory.Exists(buildRoot))
            {
                try
                {
                    foreach (var hit in Directory.EnumerateFiles(
                                 buildRoot,
                                 "Universal Device Toolkit.exe",
                                 SearchOption.AllDirectories)
                                 .Take(8))
                    {
                        candidates.Add(hit);
                    }
                }
                catch (IOException)
                {
                    // Ignore scan failures; fall through to known paths.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return currentPath;
        }

        private static string FindSolutionDirectory()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "UniversalDeviceToolkit.sln")))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            return Environment.CurrentDirectory;
        }

        private void CaptureDiagnosticScreenshot(string reason)
        {
            if (MainWindow is null)
            {
                return;
            }

            try
            {
                using var bitmap = WinRTOcrHelper.CaptureElement(MainWindow);
                if (bitmap is null)
                {
                    return;
                }

                var directory = Path.Combine(AppContext.BaseDirectory, "TestResults");
                Directory.CreateDirectory(directory);
                var safeReason = string.Concat(reason.Where(char.IsLetterOrDigit));
                var fileName = $"FlaUI_{safeReason}_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.png";
                bitmap.Save(Path.Combine(directory, fileName), ImageFormat.Png);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"FlaUI diagnostic screenshot failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Kills all existing UDT process instances so a fresh launch can succeed.
        /// UDT uses SingleInstanceGuard (Mutex) which prevents multiple instances.
        /// </summary>
        private static void KillExistingInstances()
        {
            var failures = new List<Exception>();

            try
            {
                var existingProcesses = Process.GetProcessesByName("Universal Device Toolkit");
                foreach (var p in existingProcesses)
                {
                    try
                    {
                        p.Kill(entireProcessTree: true);
                        p.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        failures.Add(new InvalidOperationException(
                            $"Could not terminate existing UDT process {p.Id}.", ex));
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }

                // Brief delay for the OS to clean up
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException("Could not enumerate existing UDT processes.", ex));
                // Best effort — if we can't kill existing instances, the launch may fail
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Existing UDT processes must be cleaned up before FlaUI launch.",
                    failures);
            }
        }
    }
}
