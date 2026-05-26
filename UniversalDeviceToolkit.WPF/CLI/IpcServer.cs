using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UniversalDeviceToolkit.CLI.Lib;
using UniversalDeviceToolkit.CLI.Lib.Extensions;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.CLI.Features;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.CLI;

public class IpcServer(
    AutomationProcessor automationProcessor,
    SpectrumKeyboardBacklightController spectrumKeyboardBacklightController,
    RGBKeyboardBacklightController rgbKeyboardBacklightController,
    IntegrationsSettings settings,
    UpdateChecker updateChecker,
    UpdateCheckSettings updateCheckSettings
    )
{
    private const string RelaxedIpcAclEnvironmentVariable = "LLT_RELAXED_IPC_ACL";

    private CancellationTokenSource _cancellationTokenSource = new();
    private Task _handler = Task.CompletedTask;

    public async Task StartStopIfNeededAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (!settings.Store.CLI)
            return;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Starting...");

        _cancellationTokenSource = new();

        var token = _cancellationTokenSource.Token;
        _handler = Task.Run(() => Handler(token), token);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Started");
    }

    public async Task StopAsync()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Stopping...");

        await _cancellationTokenSource.CancelAsync();
        await _handler;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Stopped");
    }

    private async Task Handler(CancellationToken token)
    {
        try
        {
            await using var pipe = CreatePipeServerStream();

            while (!token.IsCancellationRequested)
            {
                await pipe.WaitForConnectionAsync(token).ConfigureAwait(false);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Connection received.");

                try
                {
                    var req = await pipe.ReadObjectAsync<IpcRequest>(token).ConfigureAwait(false);

                    if (req?.Operation is null)
                        throw new IpcException("Failed to deserialize request");

                    var res = await HandleRequest(req).ConfigureAwait(false);
                    await pipe.WriteObjectAsync(res, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var res = new IpcResponse { Success = false, Message = ex.Message };
                    await pipe.WriteObjectAsync(res, token).ConfigureAwait(false);
                }
                finally
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Disconnecting...");

                    pipe.Disconnect();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when server is shutting down, no action needed
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Unknown failure.", ex);
        }
    }

    private static NamedPipeServerStream CreatePipeServerStream()
    {
        var security = CreatePipeSecurity();
        return NamedPipeServerStreamAcl.Create(UniversalDeviceToolkit.CLI.Lib.Constants.PIPE_NAME,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.None,
            0,
            0,
            security);
    }

    private static PipeSecurity CreatePipeSecurity()
    {
        var security = new PipeSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var adminIdentity = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        security.AddAccessRule(new(adminIdentity, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        if (IsRelaxedIpcAclEnabled() && WindowsIdentity.GetCurrent().User is { } currentUser)
            security.AddAccessRule(new(currentUser, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        return security;
    }

    private static bool IsRelaxedIpcAclEnabled()
    {
        var rawValue = Environment.GetEnvironmentVariable(RelaxedIpcAclEnvironmentVariable);
        return string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IpcResponse> HandleRequest(IpcRequest req)
    {
        string? message;

        switch (req.Operation)
        {
            case IpcRequest.OperationType.ListQuickActions:
                message = await ListQuickActionsAsync().ConfigureAwait(false);
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.QuickAction when req is { Name: not null }:
                await RunQuickActionAsync(req.Name).ConfigureAwait(false);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.ListFeatures:
                message = await ListFeaturesAsync();
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.ListFeatureValues when req is { Name: not null }:
                message = await ListFeatureValuesAsync(req.Name);
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.GetFeatureValue when req is { Name: not null }:
                message = await GetFeatureValueAsync(req.Name);
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.SetFeatureValue when req is { Name: not null, Value: not null }:
                await SetFeatureValueAsync(req.Name, req.Value).ConfigureAwait(false);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.GetSpectrumProfile:
                message = await GetSpectrumProfileAsync();
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.SetSpectrumProfile when req is { Value: not null }:
                await SetSpectrumProfileAsync(req.Value);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.GetSpectrumBrightness:
                message = await GetSpectrumBrightnessAsync();
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.SetSpectrumBrightness when req is { Value: not null }:
                await SetSpectrumBrightnessAsync(req.Value);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.GetRGBPreset:
                message = await GetRGBPresetAsync();
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.SetRGBPreset when req is { Value: not null }:
                await SetRGBPresetAsync(req.Value);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.IsShellRegistered:
                message = await IsShellRegisteredAsync().ConfigureAwait(false);
                return new IpcResponse { Success = true, Message = message };

            case IpcRequest.OperationType.IsShellInstalled:
                message = IsShellInstalled();
                return new IpcResponse { Success = true, Message = message };
            case IpcRequest.OperationType.InstallShell:
                await InstallShellAsync().ConfigureAwait(false);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.UninstallShell:
                await UninstallShellAsync().ConfigureAwait(false);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.CaptureWindowVisual when req is { Name: not null, Value: not null }:
                await CaptureWindowVisualAsync(req.Name, req.Value).ConfigureAwait(false);
                return new IpcResponse { Success = true };
            case IpcRequest.OperationType.GetAppStatus:
                return new IpcResponse { Success = true, Message = BuildAppStatus() };
            default:
                throw new IpcException("Invalid request");
        }
    }

    private string BuildAppStatus()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
        var repositoryOwner = !string.IsNullOrWhiteSpace(updateCheckSettings.Store.UpdateRepositoryOwner)
            ? updateCheckSettings.Store.UpdateRepositoryOwner
            : UniversalDeviceToolkit.WPF.Constants.UpdateRepositoryOwner;
        var repositoryName = !string.IsNullOrWhiteSpace(updateCheckSettings.Store.UpdateRepositoryName)
            ? updateCheckSettings.Store.UpdateRepositoryName
            : UniversalDeviceToolkit.WPF.Constants.UpdateRepositoryName;
        var updateStatus = updateChecker.Disable
            ? $"disabled ({updateChecker.DisableReason ?? UniversalDeviceToolkit.WPF.Flags.DisableUpdateCheckerSwitch})"
            : "enabled";

        return string.Join(Environment.NewLine,
            $"{AppIdentity.DisplayName} is running.",
            $"Version: {version}",
            "CLI IPC: enabled",
            $"Update checker: {updateStatus}",
            $"Update repository: {repositoryOwner}/{repositoryName}");
    }

    private static Task CaptureWindowVisualAsync(string windowHandleValue, string outputPath)
    {
        if (!int.TryParse(windowHandleValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var windowHandle) || windowHandle == 0)
            throw new IpcException($"Invalid window handle '{windowHandleValue}'.");

        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = Application.Current.Windows
                .OfType<Window>()
                .FirstOrDefault(candidate => new WindowInteropHelper(candidate).Handle == (IntPtr)windowHandle)
                ?? throw new IpcException($"Window handle {windowHandle} is not tracked by the application.");

            var rootVisual = window.Content as FrameworkElement ?? window;
            rootVisual.ApplyTemplate();
            rootVisual.UpdateLayout();

            var width = rootVisual.ActualWidth > 0 ? rootVisual.ActualWidth : window.ActualWidth;
            var height = rootVisual.ActualHeight > 0 ? rootVisual.ActualHeight : window.ActualHeight;

            if (width <= 0 || height <= 0)
                throw new IpcException($"Window handle {windowHandle} has no renderable surface.");

            rootVisual.Measure(new Size(width, height));
            rootVisual.Arrange(new Rect(0, 0, width, height));
            rootVisual.UpdateLayout();

            var presentationSource = PresentationSource.FromVisual(rootVisual);
            var dpiX = 96d;
            var dpiY = 96d;
            if (presentationSource?.CompositionTarget is not null)
            {
                dpiX *= presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY *= presentationSource.CompositionTarget.TransformToDevice.M22;
            }

            var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * dpiX / 96d));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(height * dpiY / 96d));

            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, dpiX, dpiY, PixelFormats.Pbgra32);
            var drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                var background = ResolveCaptureBackgroundBrush(window, rootVisual);
                if (background is not null)
                    context.DrawRectangle(background, null, new Rect(0, 0, width, height));

                context.DrawRectangle(new VisualBrush(rootVisual), null, new Rect(0, 0, width, height));
            }

            bitmap.Render(drawingVisual);

            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            using var stream = File.Create(outputPath);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(stream);
        }, DispatcherPriority.Render).Task;
    }

    private static Brush? ResolveCaptureBackgroundBrush(Window window, FrameworkElement rootVisual)
    {
        Brush?[] candidates =
        {
            rootVisual switch
            {
                Control control => control.Background,
                Panel panel => panel.Background,
                Border border => border.Background,
                _ => null
            },
            window.Background,
            window.TryFindResource("ApplicationBackgroundBrush") as Brush,
            Application.Current.TryFindResource("ApplicationBackgroundBrush") as Brush
        };

        foreach (var candidate in candidates)
        {
            if (candidate is null)
                continue;

            if (candidate is SolidColorBrush solid && solid.Color.A == 0)
                continue;

            return candidate.CloneCurrentValue();
        }

        return Brushes.White;
    }

    private async Task<string> ListQuickActionsAsync()
    {
        var pipelines = await automationProcessor.GetPipelinesAsync().ConfigureAwait(false);
        var quickActions = pipelines
            .Where(p => p.Trigger is null)
            .Select(p => p.Name);

        return string.Join('\n', quickActions);
    }

    private async Task RunQuickActionAsync(string name)
    {
        var pipelines = await automationProcessor.GetPipelinesAsync().ConfigureAwait(false);
        var quickAction = pipelines
                              .Where(p => p.Trigger is null)
                              .FirstOrDefault(p => p.Name == name)
                          ?? throw new InvalidOperationException($"Quick Action \"{name}\" not found");

        await automationProcessor.RunNowAsync(quickAction.Id);
    }

    private static async Task<string?> ListFeaturesAsync()
    {
        var features = new List<string>();

        foreach (var feature in FeatureRegistry.All)
        {
            if (await feature.IsSupportedAsync().ConfigureAwait(false))
                features.Add(feature.Name);
        }

        return string.Join('\n', features);
    }

    private static async Task<string?> ListFeatureValuesAsync(string name)
    {
        var feature = FeatureRegistry.All.FirstOrDefault(f => f.Name == name)
                      ?? throw new IpcException("Invalid feature");
        var values = await feature.GetValuesAsync().ConfigureAwait(false);
        return string.Join('\n', values);
    }

    private static async Task<string> GetFeatureValueAsync(string name)
    {
        var feature = FeatureRegistry.All.FirstOrDefault(f => f.Name == name)
                      ?? throw new IpcException("Invalid feature");
        return await feature.GetValueAsync().ConfigureAwait(false);
    }

    private static async Task SetFeatureValueAsync(string name, string value)
    {
        var feature = FeatureRegistry.All.FirstOrDefault(f => f.Name == name)
                      ?? throw new IpcException("Invalid feature");
        await feature.SetValueAsync(value).ConfigureAwait(false);
    }

    private async Task<string> GetSpectrumProfileAsync()
    {
        if (!await spectrumKeyboardBacklightController.IsSupportedAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Spectrum is not supported");

        var profile = await spectrumKeyboardBacklightController.GetProfileAsync().ConfigureAwait(false);
        return $"{profile}";
    }

    private async Task SetSpectrumProfileAsync(string value)
    {
        if (!await spectrumKeyboardBacklightController.IsSupportedAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Spectrum is not supported");

        await spectrumKeyboardBacklightController.SetProfileAsync(Convert.ToInt32(value)).ConfigureAwait(false);

        MessagingCenter.Publish(new SpectrumBacklightChangedMessage());
    }

    private async Task<string> GetSpectrumBrightnessAsync()
    {
        if (!await spectrumKeyboardBacklightController.IsSupportedAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Spectrum is not supported");

        var profile = await spectrumKeyboardBacklightController.GetBrightnessAsync().ConfigureAwait(false);
        return $"{profile}";
    }

    private async Task SetSpectrumBrightnessAsync(string value)
    {
        if (!await spectrumKeyboardBacklightController.IsSupportedAsync().ConfigureAwait(false))
            throw new InvalidOperationException("Spectrum is not supported");

        await spectrumKeyboardBacklightController.SetBrightnessAsync(Convert.ToInt32(value)).ConfigureAwait(false);

        MessagingCenter.Publish(new SpectrumBacklightChangedMessage());
    }

    private async Task<string> GetRGBPresetAsync()
    {
        if (!await rgbKeyboardBacklightController.IsSupportedAsync().ConfigureAwait(false))
            throw new InvalidOperationException("RGB is not supported");

        var state = await rgbKeyboardBacklightController.GetStateAsync().ConfigureAwait(false);
        return $"{(int)state.SelectedPreset + 1}";
    }

    private async Task SetRGBPresetAsync(string value)
    {
        if (!await rgbKeyboardBacklightController.IsSupportedAsync().ConfigureAwait(false))
            throw new InvalidOperationException("RGB is not supported");

        var preset = (RGBKeyboardBacklightPreset)(Convert.ToInt32(value) - 1);

        if (!Enum.IsDefined(preset))
            throw new InvalidOperationException("Invalid preset");

        await rgbKeyboardBacklightController.SetLightControlOwnerAsync(true).ConfigureAwait(false);

        await rgbKeyboardBacklightController.SetPresetAsync(preset).ConfigureAwait(false);



        MessagingCenter.Publish(new RGBKeyboardBacklightChangedMessage());

    }



    private static Task<string> IsShellRegisteredAsync()
    {
        // Shell functionality moved to ShellIntegration plugin
        // Use plugin system to check shell status
        try
        {
            var pluginManager = IoCContainer.Resolve<IPluginManager>();
            var shellPlugin = pluginManager.GetRegisteredPlugins()
                .FirstOrDefault(p => p.Id == "shell-integration" && pluginManager.IsInstalled(p.Id));
            
            if (shellPlugin != null)
            {
                // Plugin provides shell functionality
                return Task.FromResult("true");
            }
            return Task.FromResult("false");
        }
        catch
        {
            return Task.FromResult("false");
        }
    }






    private static string IsShellInstalled()
    {
        // Shell integration is now handled by plugin. Use GUI for shell management.
        return "false";
    }

    private static Task InstallShellAsync()
    {
        // Shell integration is now handled by plugin. Use GUI for shell management.
        return Task.FromException(new IpcException("Shell installation is now managed through the Shell Integration plugin. Please use the Windows Optimization page in the application."));
    }

    private static Task UninstallShellAsync()
    {
        // Shell integration is now handled by plugin. Use GUI for shell management.
        throw new IpcException("Shell uninstallation is now managed through the Shell Integration plugin. Please use the Windows Optimization page in the application.");
    }
}
