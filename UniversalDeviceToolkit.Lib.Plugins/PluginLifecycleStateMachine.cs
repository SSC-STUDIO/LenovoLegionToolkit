using System;
using System.Collections.Generic;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Reason supplied with a <see cref="PluginLifecycleStateMachine.TransitionResult"/>
/// when the requested transition is rejected.
/// </summary>
public enum PluginTransitionRejectionReason
{
    /// <summary>
    /// The transition is permitted by the state machine.
    /// </summary>
    None = 0,

    /// <summary>
    /// The transition is not allowed from the current state.
    /// </summary>
    IllegalTransition = 1,

    /// <summary>
    /// The supplied <see cref="PluginState"/> value is not a defined enum member.
    /// </summary>
    UnknownState = 2,
}

/// <summary>
/// Result of evaluating a <see cref="PluginState"/> transition.
/// </summary>
public readonly record struct PluginTransitionResult(
    bool IsAllowed,
    PluginState From,
    PluginState To,
    PluginTransitionRejectionReason Reason)
{
    /// <summary>
    /// Convenience: rejected transitions.
    /// </summary>
    public static PluginTransitionResult Reject(
        PluginState from,
        PluginState to,
        PluginTransitionRejectionReason reason) =>
        new(false, from, to, reason);

    /// <summary>
    /// Convenience: permitted transitions.
    /// </summary>
    public static PluginTransitionResult Allow(PluginState from, PluginState to) =>
        new(true, from, to, PluginTransitionRejectionReason.None);
}

/// <summary>
/// Encapsulates the legal transitions between <see cref="PluginState"/> values
/// for a single plugin. The state machine is the single source of truth for
/// "from -&gt; to" validity and is the only component allowed to enforce
/// lifecycle rules; callers (<see cref="PluginManager"/> and friends) should
/// ask the state machine before mutating plugin state.
///
/// Default transitions:
/// <list type="bullet">
///   <item><c>NotInstalled</c> -&gt; <c>Installed</c> (install)</item>
///   <item><c>NotInstalled</c> -&gt; <c>Error</c> (load failure)</item>
///   <item><c>Installed</c> -&gt; <c>Enabled</c> (startup)</item>
///   <item><c>Installed</c> -&gt; <c>Disabled</c> (user disables before start)</item>
///   <item><c>Installed</c> -&gt; <c>NotInstalled</c> (uninstall while stopped)</item>
///   <item><c>Installed</c> -&gt; <c>Error</c> (load/setup error after install)</item>
///   <item><c>Enabled</c> -&gt; <c>Installed</c> (stop)</item>
///   <item><c>Enabled</c> -&gt; <c>Disabled</c> (user disables at runtime)</item>
///   <item><c>Enabled</c> -&gt; <c>NotInstalled</c> (uninstall while running)</item>
///   <item><c>Enabled</c> -&gt; <c>Error</c> (runtime failure)</item>
///   <item><c>Disabled</c> -&gt; <c>Enabled</c> (user re-enables)</item>
///   <item><c>Disabled</c> -&gt; <c>Installed</c> (reset to installed baseline)</item>
///   <item><c>Disabled</c> -&gt; <c>NotInstalled</c> (uninstall while disabled)</item>
///   <item><c>Error</c> -&gt; <c>Installed</c> (recovery / re-install)</item>
///   <item><c>Error</c> -&gt; <c>NotInstalled</c> (uninstall of a broken plugin)</item>
/// </list>
///
/// All other transitions are rejected and logged as invalid attempts.
/// </summary>
public sealed class PluginLifecycleStateMachine
{
    private static readonly HashSet<(PluginState From, PluginState To)> AllowedTransitions = new()
    {
        (PluginState.NotInstalled, PluginState.Installed),
        (PluginState.NotInstalled, PluginState.Error),

        (PluginState.Installed, PluginState.Enabled),
        (PluginState.Installed, PluginState.Disabled),
        (PluginState.Installed, PluginState.NotInstalled),
        (PluginState.Installed, PluginState.Error),

        (PluginState.Enabled, PluginState.Installed),
        (PluginState.Enabled, PluginState.Disabled),
        (PluginState.Enabled, PluginState.NotInstalled),
        (PluginState.Enabled, PluginState.Error),

        (PluginState.Disabled, PluginState.Enabled),
        (PluginState.Disabled, PluginState.Installed),
        (PluginState.Disabled, PluginState.NotInstalled),

        (PluginState.Error, PluginState.Installed),
        (PluginState.Error, PluginState.NotInstalled),
    };

    /// <summary>
    /// Validate a candidate transition without mutating any state.
    /// </summary>
    /// <param name="from">Current state.</param>
    /// <param name="to">Desired next state.</param>
    /// <returns>
    /// A <see cref="PluginTransitionResult"/> describing whether the transition
    /// is allowed. The caller decides what to do with rejected transitions.
    /// </returns>
    public PluginTransitionResult Validate(PluginState from, PluginState to)
    {
        if (!Enum.IsDefined(from) || !Enum.IsDefined(to))
        {
            return PluginTransitionResult.Reject(
                from,
                to,
                PluginTransitionRejectionReason.UnknownState);
        }

        if (from == to)
        {
            return PluginTransitionResult.Reject(
                from,
                to,
                PluginTransitionRejectionReason.IllegalTransition);
        }

        return AllowedTransitions.Contains((from, to))
            ? PluginTransitionResult.Allow(from, to)
            : PluginTransitionResult.Reject(from, to, PluginTransitionRejectionReason.IllegalTransition);
    }

    /// <summary>
    /// Convenience wrapper for <see cref="Validate"/>. Returns <c>true</c>
    /// when the transition is allowed.
    /// </summary>
    public bool CanTransition(PluginState from, PluginState to) =>
        Validate(from, to).IsAllowed;

    /// <summary>
    /// Validate a transition and, when the result is a rejection, emit a
    /// trace-level log entry. The plugin id is included so the log line is
    /// useful even when a single host instance has many plugins in flight.
    /// </summary>
    /// <param name="pluginId">Plugin identifier used purely for diagnostics.</param>
    /// <param name="from">Current state.</param>
    /// <param name="to">Desired next state.</param>
    /// <returns>The validation result.</returns>
    public PluginTransitionResult ValidateAndLog(string? pluginId, PluginState from, PluginState to)
    {
        var result = Validate(from, to);
        if (!result.IsAllowed)
        {
            if (Log.Instance.IsTraceEnabled)
            {
                var id = string.IsNullOrWhiteSpace(pluginId) ? "<unknown>" : pluginId;
                Log.Instance.Trace(
                    $"Plugin lifecycle: rejected transition {from} -> {to} for plugin '{id}' (reason={result.Reason}).");
            }
        }
        return result;
    }

    /// <summary>
    /// Validate a transition; when it is allowed, mutate <paramref name="current"/>
    /// to <paramref name="to"/> and return <c>true</c>. When it is rejected,
    /// log the rejection and leave <paramref name="current"/> unchanged.
    /// </summary>
    /// <param name="pluginId">Plugin identifier used for diagnostics.</param>
    /// <param name="current">
    /// Reference to the caller's current state. Updated in place when the
    /// transition is allowed.
    /// </param>
    /// <param name="to">Desired next state.</param>
    /// <returns><c>true</c> when the transition was applied.</returns>
    public bool TryTransition(string? pluginId, ref PluginState current, PluginState to)
    {
        var result = ValidateAndLog(pluginId, current, to);
        if (!result.IsAllowed)
            return false;

        current = to;
        return true;
    }
}
