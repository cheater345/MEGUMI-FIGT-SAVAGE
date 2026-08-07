using System;
using System.Collections.Generic;
using UnityEngine;

namespace SteelTempest.Core.Events
{
    /// <summary>
    /// Global event aggregator decoupling systems with zero external dependencies.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        private static EventBus _instance;
        public static EventBus Instance => _instance ??= new EventBus();

        private EventBus() { }

        public void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }
            list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
            {
                list.Remove(handler);
            }
        }

        public void Publish<T>(T payload) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var list))
            {
                return;
            }
            for (var i = list.Count - 1; i >= 0; i--)
            {
                (list[i] as Action<T>)?.Invoke(payload);
            }
        }
    }
}