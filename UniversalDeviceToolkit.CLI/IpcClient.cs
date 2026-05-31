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
               ?? throw new IpcException("Missing return message");
    }

    public static async Task<string> ListQuickActionsAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.ListQuickActions
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException("Missing return message");
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
               ?? throw new IpcException("Missing return message");
    }

    public static async Task<string> ListFeatureValuesAsync(string name)
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.ListFeatureValues,
            Name = name,
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException("Missing return message");
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
               ?? throw new IpcException("Missing return message");
    }

    public static async Task<string> GetSpectrumProfileAsync()
    {
        var req = new IpcRequest
        {
            Operation = IpcRequest.OperationType.GetSpectrumProfile
        };

        return await SendRequestAsync(req).ConfigureAwait(false)
               ?? throw new IpcException("Missing return message");
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
               ?? throw new IpcException("Missing return message");
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

               ?? throw new IpcException("Missing return message");

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

    private static async Task<string?> SendRequestAsync(IpcRequest req)
    {
        using var loading = ConsoleLoadingAnimation.Start(GetLoadingMessage(req));
        await using var pipe = new NamedPipeClientStream(GetPipeName());

        await ConnectAsync(pipe).ConfigureAwait(false);

        await pipe.WriteObjectAsync(req).ConfigureAwait(false);
        var res = await pipe.ReadObjectAsync<IpcResponse>().ConfigureAwait(false);

        if (res is null || !res.Success)
            throw new IpcException(res?.Message ?? "Unknown failure");

        return res.Message;
    }

    private static string GetLoadingMessage(IpcRequest req)
        => req.Operation switch
        {
            IpcRequest.OperationType.ListFeatures => "Loading features",
            IpcRequest.OperationType.ListFeatureValues => $"Loading values{FormatTarget(req.Name)}",
            IpcRequest.OperationType.ListQuickActions => "Loading quick actions",
            IpcRequest.OperationType.GetFeatureValue => $"Reading feature{FormatTarget(req.Name)}",
            IpcRequest.OperationType.SetFeatureValue => $"Applying feature{FormatTarget(req.Name)}",
            IpcRequest.OperationType.GetSpectrumProfile => "Reading Spectrum profile",
            IpcRequest.OperationType.SetSpectrumProfile => "Applying Spectrum profile",
            IpcRequest.OperationType.GetSpectrumBrightness => "Reading Spectrum brightness",
            IpcRequest.OperationType.SetSpectrumBrightness => "Applying Spectrum brightness",
            IpcRequest.OperationType.GetRGBPreset => "Reading RGB preset",
            IpcRequest.OperationType.SetRGBPreset => "Applying RGB preset",
            IpcRequest.OperationType.QuickAction => $"Running quick action{FormatTarget(req.Name)}",
            IpcRequest.OperationType.IsShellRegistered => "Checking shell registration",
            IpcRequest.OperationType.IsShellInstalled => "Checking shell installation",
            IpcRequest.OperationType.InstallShell => "Starting shell installation",
            IpcRequest.OperationType.UninstallShell => "Starting shell uninstallation",
            IpcRequest.OperationType.GetAppStatus => "Checking app status",
            _ => "Waiting for Universal Device Toolkit"
        };

    private static string FormatTarget(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : $" '{value}'";

    private static async Task ConnectAsync(NamedPipeClientStream pipe)
    {
        var retries = 3;

        while (retries >= 0)
        {
            try
            {
                await pipe.ConnectAsync(TimeSpan.FromMilliseconds(500), CancellationToken.None).ConfigureAwait(false);
                pipe.ReadMode = PipeTransmissionMode.Message;
                return;
            }
            catch (TimeoutException)
            {
                // Expected when connection times out, will retry
            }

            retries--;
        }

        throw new IpcConnectException();
    }

    private static string GetPipeName()
        => Constants.GetPipeNameFromEnvironment();
}
