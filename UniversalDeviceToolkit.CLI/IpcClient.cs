using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.CLI.Lib;
using UniversalDeviceToolkit.CLI.Lib.Extensions;

namespace UniversalDeviceToolkit.CLI;

public static class IpcClient
{
    public static async Task<string> GetAppStatusAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetAppStatus
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static async Task<string> ListQuickActionsAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.ListQuickActions
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static Task RunQuickActionAsync(string name)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.QuickAction,
            Name = name
        };

        return SendRequestAsync(req);
    }

    public static async Task<string> ListFeaturesAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.ListFeatures,
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static async Task<string> ListFeatureValuesAsync(string name)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.ListFeatureValues,
            Name = name,
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static Task SetFeatureValueAsync(string name, string value)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.SetFeatureValue,
            Name = name,
            Value = value
        };

        return SendRequestAsync(req);
    }

    public static async Task<string> GetFeatureValueAsync(string name)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetFeatureValue,
            Name = name
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static async Task<string> GetSpectrumProfileAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetSpectrumProfile
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static Task SetSpectrumProfileAsync(string value)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.SetSpectrumProfile,
            Value = value
        };

        return SendRequestAsync(req);
    }

    public static async Task<string> GetSpectrumBrightnessAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetSpectrumBrightness
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static Task SetSpectrumBrightnessAsync(string value)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.SetSpectrumBrightness,
            Value = value
        };

        return SendRequestAsync(req);
    }

    public static async Task<string> GetRGBPresetAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetRGBPreset
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static Task SetRGBPresetAsync(string value)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.SetRGBPreset,
            Value = value
        };

        return SendRequestAsync(req);
    }

    public static async Task<bool> IsShellRegisteredAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.IsShellRegistered
        };

        var result = await SendRequestAsync(req).ConfigureAwait(false);
        return result?.ToLowerInvariant() == "true";
    }

    public static async Task<bool> IsShellInstalledAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.IsShellInstalled
        };

        var result = await SendRequestAsync(req).ConfigureAwait(false);
        return result?.ToLowerInvariant() == "true";
    }

    public static Task InstallShellAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.InstallShell
        };

        return SendRequestAsync(req);
    }

    public static Task UninstallShellAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.UninstallShell
        };

        return SendRequestAsync(req);
    }

    public static async Task<string> GetNetworkAccelerationStatusAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetNetworkAccelerationStatus
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static async Task<string> StartNetworkAccelerationAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.StartNetworkAcceleration
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static async Task<string> StopNetworkAccelerationAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.StopNetworkAcceleration
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    public static async Task<string> RunNetworkDiagnosticsAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.RunNetworkDiagnostics
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException(Strings.Get("CLI_IpcError_MissingReturnMessage", "Missing return message"));
    }

    private static async Task<string?> SendRequestAsync(IpcRequest req)
    {
        using var loading = ConsoleLoadingAnimation.Start(GetLoadingMessage(req));
        using var pipe = new NamedPipeClientStream(GetPipeName());

        await ConnectAsync(pipe).ConfigureAwait(false);

        var challengeResponse = await pipe.ReadObjectAsync<IpcResponse>().ConfigureAwait(false);
        if (challengeResponse is null || !challengeResponse.Success || challengeResponse.Message is null)
            throw new IpcException(Strings.Get("CLI_IpcError_AuthChallengeFailed", "Failed to receive authentication challenge"));

        var challenge = Convert.FromHexString(challengeResponse.Message);
        req.AuthToken = ComputeAuthToken(challenge);

        await pipe.WriteObjectAsync(req).ConfigureAwait(false);
        var res = await pipe.ReadObjectAsync<IpcResponse>().ConfigureAwait(false);

        if (res is null || !res.Success)
            throw new IpcException(res?.Message ?? Strings.Get("CLI_IpcError_UnknownFailure", "Unknown failure"));

        return res.Message;
    }

    private static string ComputeAuthToken(byte[] challenge) => Convert.ToHexString(challenge);

    private static string GetLoadingMessage(IpcRequest req)
    {
        var (key, fallback) = req.Operation switch
        {
            IpcRequest.OperationType.ListFeatures =>
                ("CLI_Loading_ListFeatures", "Loading features"),
            IpcRequest.OperationType.ListFeatureValues =>
                ("CLI_Loading_ListFeatureValues", "Loading values"),
            IpcRequest.OperationType.ListQuickActions =>
                ("CLI_Loading_ListQuickActions", "Loading quick actions"),
            IpcRequest.OperationType.GetFeatureValue =>
                ("CLI_Loading_GetFeatureValue", "Reading feature"),
            IpcRequest.OperationType.SetFeatureValue =>
                ("CLI_Loading_SetFeatureValue", "Applying feature"),
            IpcRequest.OperationType.GetSpectrumProfile =>
                ("CLI_Loading_GetSpectrumProfile", "Reading Spectrum profile"),
            IpcRequest.OperationType.SetSpectrumProfile =>
                ("CLI_Loading_SetSpectrumProfile", "Applying Spectrum profile"),
            IpcRequest.OperationType.GetSpectrumBrightness =>
                ("CLI_Loading_GetSpectrumBrightness", "Reading Spectrum brightness"),
            IpcRequest.OperationType.SetSpectrumBrightness =>
                ("CLI_Loading_SetSpectrumBrightness", "Applying Spectrum brightness"),
            IpcRequest.OperationType.GetRGBPreset =>
                ("CLI_Loading_GetRGBPreset", "Reading RGB preset"),
            IpcRequest.OperationType.SetRGBPreset =>
                ("CLI_Loading_SetRGBPreset", "Applying RGB preset"),
            IpcRequest.OperationType.QuickAction =>
                ("CLI_Loading_QuickAction", "Running quick action"),
            IpcRequest.OperationType.IsShellRegistered =>
                ("CLI_Loading_IsShellRegistered", "Checking shell registration"),
            IpcRequest.OperationType.IsShellInstalled =>
                ("CLI_Loading_IsShellInstalled", "Checking shell installation"),
            IpcRequest.OperationType.InstallShell =>
                ("CLI_Loading_InstallShell", "Starting shell installation"),
            IpcRequest.OperationType.UninstallShell =>
                ("CLI_Loading_UninstallShell", "Starting shell uninstallation"),
            IpcRequest.OperationType.GetAppStatus =>
                ("CLI_Loading_GetAppStatus", "Checking app status"),
            _ =>
                ("CLI_Loading_Default", "Waiting for Universal Device Toolkit")
        };

        return $"{Strings.Get(key, fallback)}{FormatTarget(req.Name)}";
    }

    private static string FormatTarget(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : $" '{value}'";

    private const int ConnectMaxAttempts = 40;

    private static async Task ConnectAsync(NamedPipeClientStream pipe)
    {
        for (var attempt = 0; attempt < ConnectMaxAttempts; attempt++)
        {
            try
            {
                await pipe.ConnectAsync(TimeSpan.FromMilliseconds(500), CancellationToken.None).ConfigureAwait(false);
                pipe.ReadMode = PipeTransmissionMode.Message;
                return;
            }
            catch (TimeoutException)
            {
                if (attempt < ConnectMaxAttempts - 1)
                {
                    var baseDelay = (int)Math.Min(200 * Math.Pow(2, attempt), 3000);
                    var jitter = Random.Shared.Next(-50, 51);
                    var delayMs = Math.Max(0, baseDelay + jitter);
                    await Task.Delay(delayMs, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        throw new IpcConnectException();
    }

    private static string GetPipeName()
        => Constants.GetPipeNameFromEnvironment();
}
