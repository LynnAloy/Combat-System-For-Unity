using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayEvents
{
    public static class GameEventHub
    {
        private static readonly Dictionary<TaskSignalSO, Delegate> listeners = new();

        public static IDisposable Subscribe<TPayload>(
            GameplaySignalSO<TPayload> signal,
            Action<TPayload> listener)
        {
            if (signal == null)
            {
                throw new ArgumentNullException(nameof(signal));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (listeners.TryGetValue(signal, out Delegate callbacks))
            {
                if (callbacks is not Action<TPayload> typedCallbacks)
                {
                    throw CreatePayloadMismatchException<TPayload>(signal);
                }

                listeners[signal] = typedCallbacks + listener;
            }
            else
            {
                listeners.Add(signal, listener);
            }

            return new Subscription<TPayload>(signal, listener);
        }

        public static void Unsubscribe<TPayload>(
            GameplaySignalSO<TPayload> signal,
            Action<TPayload> listener)
        {
            if (signal == null || listener == null)
            {
                return;
            }

            if (!listeners.TryGetValue(signal, out Delegate callbacks))
            {
                return;
            }

            if (callbacks is not Action<TPayload> typedCallbacks)
            {
                throw CreatePayloadMismatchException<TPayload>(signal);
            }

            typedCallbacks -= listener;

            if (typedCallbacks == null)
            {
                listeners.Remove(signal);
            }
            else
            {
                listeners[signal] = typedCallbacks;
            }
        }

        public static void Publish<TPayload>(GameplaySignalSO<TPayload> signal, TPayload payload)
        {
            if (signal == null)
            {
                throw new ArgumentNullException(nameof(signal));
            }

            if (!listeners.TryGetValue(signal, out Delegate callbacks))
            {
                return;
            }

            if (callbacks is not Action<TPayload> typedCallbacks)
            {
                throw CreatePayloadMismatchException<TPayload>(signal);
            }

            Delegate[] invocationList = typedCallbacks.GetInvocationList();

            foreach (Delegate callback in invocationList)
            {
                try
                {
                    ((Action<TPayload>)callback).Invoke(payload);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Listener failed while handling signal '{signal.name}'.");
                    Debug.LogException(exception);
                }
            }
        }

        private static InvalidOperationException CreatePayloadMismatchException<TPayload>(
            TaskSignalSO signal)
        {
            return new InvalidOperationException(
                $"Signal '{signal.name}' expects {signal.PayloadType.Name}, " +
                $"but received {typeof(TPayload).Name}.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            listeners.Clear();
        }

        private sealed class Subscription<TPayload> : IDisposable
        {
            private GameplaySignalSO<TPayload> signal;
            private Action<TPayload> listener;

            public Subscription(
                GameplaySignalSO<TPayload> signal,
                Action<TPayload> listener)
            {
                this.signal = signal;
                this.listener = listener;
            }

            public void Dispose()
            {
                if (signal == null || listener == null)
                {
                    return;
                }

                GameEventHub.Unsubscribe(signal, listener);
                signal = null;
                listener = null;
            }
        }
    }
}