using System.Collections.Generic;
using UnityEngine;

namespace SteelTempest.Pooling
{
    /// <summary>
    /// Minimal factory-free object pool. One pool per prefab instance ID.
    /// Returns component instances so callers receive the expected type.
    /// </summary>
    public static class ObjectPool
    {
        private static readonly Dictionary<int, Stack<Component>> Pools = new();

        /// <summary>
        /// Gets a pooled instance of <typeparamref name="T"/> (a component whose
        /// GameObject is prefab) or spawns a fresh one. Instances are returned to
        /// the pool via <see cref="Despawn"/>.
        /// </summary>
        public static T Spawn<T>(T prefab, Transform parent, Vector3 localPosition) where T : Component
        {
            var key = prefab.name;
            if (!Pools.TryGetValue(key.GetHashCode(), out var stack))
            {
                stack = new Stack<Component>();
                Pools.Add(key.GetHashCode(), stack);
            }

            T instance = null;
            while (stack.Count > 0)
            {
                var candidate = stack.Pop();
                if (candidate != null)
                {
                    instance = (T)candidate;
                    break;
                }
            }

            if (instance == null)
            {
                instance = Object.Instantiate(prefab);
            }

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
                instance.transform.localPosition = localPosition;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
            }

            instance.gameObject.SetActive(true);
            return instance;
        }

        /// <summary>Returns an instance to the pool by deactivating it.</summary>
        public static void Despawn(Component instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
        }

        public static void Clear()
        {
            Pools.Clear();
        }
    }
}