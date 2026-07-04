using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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


    private static readonly SemaphoreSlim SupportedFeaturesCacheSemaphore = new(1, 1);
    private static string? _supportedFeaturesCache;

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
                    pipe.ReadMode = PipeTransmissionMode.Message;

                    var challenge = RandomNumberGenerator.GetBytes(32);
                    var encryptedChallenge = ProtectedData.Protect(challenge, null, DataProtectionScope.CurrentUser);
                    var challengeResponse = new IpcResponse { Success = true, Message = Convert.ToHexString(encryptedChallenge) };
                    await pipe.WriteObjectAsync(challengeResponse, token).ConfigureAwait(false);

                    var req = await pipe.ReadObjectAsync<IpcRequest>(token).ConfigureAwait(false);

                    if (req?.Operation is null)
                        throw new IpcException("Failed to deserialize request");

                    if (string.IsNullOrWhiteSpace(req.AuthToken))
                        throw new IpcException("Unauthorized");

                    try
                    {
                        var clientResponse = Convert.FromHexString(req.AuthToken);
                        var decryptedChallenge = ProtectedData.Unprotect(clientResponse, null, DataProtectionScope.CurrentUser);
                        if (!challenge.AsSpan().SequenceEqual(decryptedChallenge))
                            throw new IpcException("Unauthorized");
                    }
                    catch (Exception ex) when (ex is FormatException or CryptographicException)
                    {
                        throw new IpcException("Unauthorized");
                    }

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
        return NamedPipeServerStreamAcl.Create(GetPipeName(),
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

        if (WindowsIdentity.GetCurrent().User is { } currentUser)
            security.AddAccessRule(new(currentUser, PipeAccessRights.ReadWrite, AccessControlType.Allow));

        return security;
    }

    private static string GetPipeName()
    {
#if UDT_TEST_HOOKS
        var isolationPath = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        return UniversalDeviceToolkit.CLI.Lib.Constants.GetPipeName(isolationPath);
#else
        return UniversalDeviceToolkit.CLI.Lib.Constants.DEFAULT_PIPE_NAME;
#endif
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

    private static async Task<string?> ListFeaturesAsync(CancellationToken cancellationToken = default)
    {
        if (_supportedFeaturesCache is { } cached)
            return cached;

        await SupportedFeaturesCacheSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_supportedFeaturesCache is { } cachedAfterWait)
                return cachedAfterWait;

            var result = await BuildSupportedFeatureListAsync(FeatureRegistry.All).ConfigureAwait(false);
            if (!result.HasProbeFailures)
                _supportedFeaturesCache = result.FeatureList;

            return result.FeatureList;
        }
        finally
        {
            SupportedFeaturesCacheSemaphore.Release();
        }
    }

    private static async Task<SupportedFeatureProbeResult> GetSupportedFeatureNameAsync(IFeatureRegistration feature)
    {
        try
        {
            var name = await feature.IsSupportedAsync().ConfigureAwait(false)
                ? feature.Name
                : null;

            return new(name, Failed: false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Feature support probe failed. [name={feature.Name}]", ex);

            return new(Name: null, Failed: true);
        }
    }

    private static async Task<SupportedFeatureListResult> BuildSupportedFeatureListAsync(IEnumerable<IFeatureRegistration> registrations)
    {
        var features = await Task.WhenAll(registrations.Select(GetSupportedFeatureNameAsync)).ConfigureAwait(false);
        return new(
            string.Join('\n', features.Select(feature => feature.Name).OfType<string>()),
            features.Any(feature => feature.Failed));
    }

    private readonly record struct SupportedFeatureProbeResult(string? Name, bool Failed);

    private readonly record struct SupportedFeatureListResult(string FeatureList, bool HasProbeFailures);

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
