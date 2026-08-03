using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.CLI.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.WPF.Utils;

internal sealed record WindowsOptimizationOperation(
    string ActionKey,
    bool Apply,
    string VerificationActionKey,
    bool ExpectedAppliedState);

internal interface IWindowsOptimizationExecutor
{
    Task ExecuteAsync(
        IReadOnlyList<WindowsOptimizationOperation> operations,
        CancellationToken cancellationToken);
}

/// <summary>
/// Runs system optimization mutations in one elevated process. The normal WPF
/// process remains unelevated and only performs state discovery and UI work.
/// </summary>
internal sealed class WindowsOptimizationElevationClient : IWindowsOptimizationExecutor
{
    private readonly WindowsOptimizationService _localService;

    public WindowsOptimizationElevationClient(WindowsOptimizationService localService)
    {
        _localService = localService;
    }

    public Task ExecuteAsync(
        IReadOnlyList<WindowsOptimizationOperation> operations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operations);

        if (operations.Count == 0)
            return Task.CompletedTask;

        return ElevatedOptimizationWorker.IsCurrentProcessElevated()
            ? ElevatedOptimizationWorker.ExecuteOperationsAsync(_localService, operations, cancellationToken, requireBuiltInActions: false)
            : ExecuteViaWorkerAsync(operations, cancellationToken);
    }

    private static async Task ExecuteViaWorkerAsync(
        IReadOnlyList<WindowsOptimizationOperation> operations,
        CancellationToken cancellationToken)
    {
        var pipeName = $"udt-optimization-{Guid.NewGuid():N}";
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        await using var server = CreatePipeServer(pipeName);
        using var worker = StartWorker(pipeName, token);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));

        try
        {
            await server.WaitForConnectionAsync(timeout.Token).ConfigureAwait(false);
            server.ReadMode = PipeTransmissionMode.Message;

            var request = new WindowsOptimizationElevationRequest
            {
                Token = token,
                Operations = operations.ToList()
            };
            await server.WriteObjectAsync(request, timeout.Token).ConfigureAwait(false);

            var response = await server.ReadObjectAsync<WindowsOptimizationElevationResponse>(timeout.Token)
                .ConfigureAwait(false);
            if (response is null)
                throw new InvalidOperationException("The elevated optimization worker returned no response.");

            if (!response.Success)
                throw new InvalidOperationException(response.Error ?? "The elevated optimization worker failed.");

            await worker.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            if (worker.ExitCode != 0)
                throw new InvalidOperationException($"The elevated optimization worker exited with code {worker.ExitCode}.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            throw new OperationCanceledException("UAC elevation was cancelled by the user.", ex, cancellationToken);
        }
        finally
        {
            try
            {
                if (!worker.HasExited)
                    worker.Kill(true);
            }
            catch (InvalidOperationException)
            {
                // The worker exited between the check and cleanup.
            }
        }
    }

    private static NamedPipeServerStream CreatePipeServer(string pipeName)
    {
        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current Windows identity has no user SID.");

        var security = new PipeSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new PipeAccessRule(
            currentUser,
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));

        return NamedPipeServerStreamAcl.Create(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Message,
            PipeOptions.Asynchronous,
            0,
            0,
            security);
    }

    private static Process StartWorker(string pipeName, string token)
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
            throw new InvalidOperationException("The current process path could not be resolved for elevation.");

        var arguments = new List<string>();
        var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (string.Equals(Path.GetExtension(processPath), ".dll", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(entryAssemblyPath))
        {
            arguments.Add(entryAssemblyPath);
        }

        arguments.Add(ElevatedOptimizationWorker.WorkerSwitch);
        arguments.Add(ElevatedOptimizationWorker.PipeSwitch);
        arguments.Add(pipeName);
        arguments.Add(ElevatedOptimizationWorker.TokenSwitch);
        arguments.Add(token);

        var startInfo = new ProcessStartInfo
        {
            FileName = processPath,
            Arguments = string.Join(" ", arguments.Select(QuoteCommandLineArgument)),
            UseShellExecute = true,
            Verb = "runas",
            WorkingDirectory = AppContext.BaseDirectory,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("The elevated optimization worker could not be started.");
    }

    private static string QuoteCommandLineArgument(string value)
    {
        if (value.Length == 0)
            return "\"\"";

        if (value.All(static c => !char.IsWhiteSpace(c) && c != '"'))
            return value;

        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        var backslashes = 0;
        foreach (var c in value)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }

            if (c == '"')
            {
                builder.Append('\\', backslashes * 2 + 1);
                builder.Append('"');
                backslashes = 0;
                continue;
            }

            builder.Append('\\', backslashes);
            builder.Append(c);
            backslashes = 0;
        }

        builder.Append('\\', backslashes * 2);
        builder.Append('"');
        return builder.ToString();
    }
}

internal sealed class WindowsOptimizationElevationRequest
{
    public string Token { get; set; } = string.Empty;
    public List<WindowsOptimizationOperation> Operations { get; set; } = [];
}

internal sealed class WindowsOptimizationElevationResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

internal static class ElevatedOptimizationWorker
{
    internal const string WorkerSwitch = "--udt-elevated-optimization";
    internal const string PipeSwitch = "--udt-elevated-pipe";
    internal const string TokenSwitch = "--udt-elevated-token";

    private const int WorkerConnectTimeoutMilliseconds = 30_000;
    private const int MaximumOperationCount = 128;

    internal static async Task<int?> TryRunAsync(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Count == 0 || !string.Equals(arguments[0], WorkerSwitch, StringComparison.Ordinal))
            return null;

        if (!TryParseArguments(arguments, out var pipeName, out var token))
            return 2;

        try
        {
            using var pipe = new NamedPipeClientStream(
                ".",
                pipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(WorkerConnectTimeoutMilliseconds / 1000d));

            await pipe.ConnectAsync(timeout.Token).ConfigureAwait(false);
            pipe.ReadMode = PipeTransmissionMode.Message;

            var request = await pipe.ReadObjectAsync<WindowsOptimizationElevationRequest>(timeout.Token)
                .ConfigureAwait(false);
            WindowsOptimizationElevationResponse response;
            try
            {
                if (!IsCurrentProcessElevated())
                    throw new UnauthorizedAccessException("The optimization worker is not elevated.");

                if (request is null || !string.Equals(request.Token, token, StringComparison.Ordinal))
                    throw new UnauthorizedAccessException("The optimization request token is invalid.");

                if (request.Operations is null || request.Operations.Count == 0 || request.Operations.Count > MaximumOperationCount)
                    throw new InvalidOperationException("The optimization request contains an invalid operation count.");

                var settings = new ApplicationSettings();
                var service = new WindowsOptimizationService(new WindowsCleanupService(settings));
                await ExecuteOperationsAsync(service, request.Operations, timeout.Token, requireBuiltInActions: true)
                    .ConfigureAwait(false);

                response = new WindowsOptimizationElevationResponse { Success = true };
            }
            catch (Exception ex)
            {
                response = new WindowsOptimizationElevationResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }

            await pipe.WriteObjectAsync(response, timeout.Token).ConfigureAwait(false);
            return response.Success ? 0 : 1;
        }
        catch
        {
            return 1;
        }
    }

    internal static bool TryParseArguments(
        IReadOnlyList<string> arguments,
        out string pipeName,
        out string token)
    {
        pipeName = string.Empty;
        token = string.Empty;

        if (arguments.Count != 5 || !string.Equals(arguments[0], WorkerSwitch, StringComparison.Ordinal))
            return false;

        if (!string.Equals(arguments[1], PipeSwitch, StringComparison.Ordinal) ||
            !string.Equals(arguments[3], TokenSwitch, StringComparison.Ordinal))
        {
            return false;
        }

        pipeName = arguments[2];
        token = arguments[4];

        if (!pipeName.StartsWith("udt-optimization-", StringComparison.Ordinal) ||
            !Guid.TryParseExact(pipeName[17..], "N", out _))
        {
            pipeName = string.Empty;
            token = string.Empty;
            return false;
        }

        try
        {
            if (Convert.FromHexString(token).Length != 32)
            {
                pipeName = string.Empty;
                token = string.Empty;
                return false;
            }
        }
        catch (FormatException)
        {
            pipeName = string.Empty;
            token = string.Empty;
            return false;
        }

        return true;
    }

    internal static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    internal static async Task ExecuteOperationsAsync(
        WindowsOptimizationService service,
        IReadOnlyList<WindowsOptimizationOperation> operations,
        CancellationToken cancellationToken,
        bool requireBuiltInActions)
    {
        var allowedActions = service.GetCategories()
            .Where(category => !requireBuiltInActions || category.PluginId is null)
            .Where(category => !category.Key.StartsWith(WindowsOptimizationService.CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions)
            .Select(action => action.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var operation in operations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(operation.ActionKey) ||
                string.IsNullOrWhiteSpace(operation.VerificationActionKey) ||
                !allowedActions.Contains(operation.ActionKey) ||
                !allowedActions.Contains(operation.VerificationActionKey))
            {
                throw new InvalidOperationException("The optimization request contains an unsupported action key.");
            }

            if (operation.Apply)
                await service.ApplyActionAsync(operation.ActionKey, cancellationToken).ConfigureAwait(false);
            else
                await service.RevertActionAsync(operation.ActionKey, cancellationToken).ConfigureAwait(false);

            var applied = await service.TryGetActionAppliedAsync(
                operation.VerificationActionKey,
                cancellationToken).ConfigureAwait(false);
            if (!applied.HasValue || applied.Value != operation.ExpectedAppliedState)
            {
                throw new InvalidOperationException(
                    $"The optimization action '{operation.VerificationActionKey}' could not be verified.");
            }
        }
    }
}
