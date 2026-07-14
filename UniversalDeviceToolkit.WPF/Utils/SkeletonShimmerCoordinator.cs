using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class SkeletonShimmerCoordinator
{
    private static readonly Dictionary<DependencyObject, DispatcherOperation> PendingRestarts = new();

    internal static void Restart(DependencyObject root, bool force = false)
    {
        var dispatcher = GetDispatcher(root);
        if (!dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(() => Restart(root, force), DispatcherPriority.Render);
            return;
        }

        CancelPending(root);
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            return;

        // Coalesce: later Restart(force:true) must not be starved by an earlier soft restart.
        DispatcherOperation? operation = null;
        operation = dispatcher.BeginInvoke(() =>
        {
            if (operation is not null
                && PendingRestarts.TryGetValue(root, out var pending)
                && ReferenceEquals(pending, operation))
            {
                PendingRestarts.Remove(root);
            }

            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                return;

            var index = 0;
            SkeletonShimmer.RestartSubtreeCore(root, ref index, force);
        }, DispatcherPriority.Render);

        PendingRestarts[root] = operation;
    }

    internal static void Stop(DependencyObject root)
    {
        var dispatcher = GetDispatcher(root);
        if (!dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(() => Stop(root), DispatcherPriority.Send);
            return;
        }

        CancelPending(root);
        SkeletonShimmer.StopSubtreeCore(root);
    }

    private static void CancelPending(DependencyObject root)
    {
        if (!PendingRestarts.Remove(root, out var operation))
            return;

        if (operation.Status == DispatcherOperationStatus.Pending)
            operation.Abort();
    }

    private static Dispatcher GetDispatcher(DependencyObject root) =>
        root is DispatcherObject dispatcherObject
            ? dispatcherObject.Dispatcher
            : Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
}
