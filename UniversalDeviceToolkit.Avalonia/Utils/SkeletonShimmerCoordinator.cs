using System.Collections.Generic;
using Avalonia;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Utils;

internal static class SkeletonShimmerCoordinator
{
    // Root → latest restart request (coalesced on the UI thread; the posted pass uses the
    // most recent force flag so a later force restart is never starved by an earlier soft one).
    private static readonly Dictionary<Visual, bool> PendingRestarts = new();
    private static readonly HashSet<Visual> PendingPosted = new();

    internal static void Restart(Visual root, bool force = false)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Restart(root, force));
            return;
        }

        PendingRestarts[root] = force;
        if (!PendingPosted.Add(root))
            return;

        Dispatcher.UIThread.Post(() =>
        {
            PendingPosted.Remove(root);

            if (!PendingRestarts.Remove(root, out var latestForce))
                return;

            var index = 0;
            SkeletonShimmer.RestartSubtreeCore(root, ref index, latestForce);
        });
    }

    internal static void Stop(Visual root)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => Stop(root));
            return;
        }

        CancelPending(root);
        SkeletonShimmer.StopSubtreeCore(root);
    }

    private static void CancelPending(Visual root)
    {
        PendingRestarts.Remove(root);
        PendingPosted.Remove(root);
    }
}
