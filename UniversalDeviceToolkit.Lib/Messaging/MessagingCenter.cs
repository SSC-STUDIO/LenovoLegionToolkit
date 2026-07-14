using System;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Utils;
using PubSub;

namespace UniversalDeviceToolkit.Lib.Messaging;

// TODO(#143): Replace PubSub with a maintained in-process messaging solution
// (e.g. CommunityToolkit.Mvvm.Messaging or a hand-rolled dispatcher). The current
// PubSub 4.0.2 dependency is unmaintained and surfaces in Dependabot/Renovate
// alerts. This is acknowledged as a large refactor because PubSub is used as the
// process-wide bus for many subsystems; see issue #143 for tracking.
public static class MessagingCenter
{
    public static void Publish<T>(T data) where T : IMessage
    {
        try
        {
            Hub.Default.Publish(data);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"MessagingCenter.Publish<{typeof(T).Name}> failed: {ex.Message}", ex);
        }
    }

    public static void Subscribe<T>(object subscriber, Action<T> handler) where T : IMessage
    {
        Hub.Default.Subscribe(subscriber, (T msg) =>
        {
            try
            {
                handler(msg);
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"MessagingCenter handler for {typeof(T).Name} failed: {ex.Message}", ex);
            }
        });
    }

    public static void Subscribe<T>(object subscriber, Action handler) where T : IMessage
    {
        Hub.Default.Subscribe<T>(subscriber, _ =>
        {
            try
            {
                handler();
            }
            catch (Exception ex)
            {
                Log.Instance.Warning($"MessagingCenter handler for {typeof(T).Name} failed: {ex.Message}", ex);
            }
        });
    }

    public static void Unsubscribe<T>(object subscriber) where T : IMessage => Hub.Default.Unsubscribe<T>(subscriber);

    public static void Unsubscribe(object subscriber) => Hub.Default.Unsubscribe(subscriber);
}
