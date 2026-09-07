using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlexibleTaskSystem
{
    public sealed class TaskContext
    {
        private readonly Dictionary<string, object> blackboard = new();

        public AllTasks Runner { get; }
        public GameObject Owner => Runner.gameObject;
        public TaskSignalBus Signals { get; } = new();

        public TaskContext(AllTasks runner)
        {
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public void Set<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "Blackboard key cannot be empty.",
                    nameof(key));
            }

            blackboard[key] = value;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (blackboard.TryGetValue(key, out object raw) &&
                raw is T result)
            {
                value = result;
                return true;
            }

            value = default;
            return false;
        }

        public bool Remove(string key)
        {
            return blackboard.Remove(key);
        }

        internal void Reset()
        {
            Signals.Clear();
            blackboard.Clear();
        }
    }

    public sealed class TaskSignalBus
    {
        private readonly Dictionary<string, Action<object>> listeners = new();

        public void Subscribe(string signal, Action<object> listener)
        {
            if (string.IsNullOrWhiteSpace(signal))
            {
                throw new ArgumentException(
                    "Signal name cannot be empty.",
                    nameof(signal));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (listeners.TryGetValue(signal, out Action<object> callbacks))
            {
                listeners[signal] = callbacks + listener;
            }
            else
            {
                listeners.Add(signal, listener);
            }
        }

        public void Unsubscribe(string signal, Action<object> listener)
        {
            if (string.IsNullOrWhiteSpace(signal) || listener == null)
            {
                return;
            }

            if (!listeners.TryGetValue(signal, out Action<object> callbacks))
            {
                return;
            }

            callbacks -= listener;

            if (callbacks == null)
            {
                listeners.Remove(signal);
            }
            else
            {
                listeners[signal] = callbacks;
            }
        }

        public void Publish(string signal, object payload = null)
        {
            if (string.IsNullOrWhiteSpace(signal))
            {
                return;
            }

            if (listeners.TryGetValue(signal, out Action<object> callbacks))
            {
                callbacks?.Invoke(payload);
            }
        }

        public void Clear()
        {
            listeners.Clear();
        }
    }
}