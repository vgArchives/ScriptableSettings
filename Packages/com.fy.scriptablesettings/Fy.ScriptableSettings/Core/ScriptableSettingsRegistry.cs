using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fy.ScriptableSettings
{
    /// <summary>
    /// Central, type-keyed hub for every <see cref="ScriptableSettings"/> instance. Settings register
    /// themselves here on load, and consumers read them back through <see cref="TryGet{T}"/> or <see cref="Get{T}"/>.
    /// </summary>
    public static class ScriptableSettingsRegistry
    {
        private static readonly Dictionary<Type, ScriptableSettings> Map = new();

        /// <summary>
        /// Looks up the registered settings of type <typeparamref name="T"/>. Pure lookup: no side effects.
        /// </summary>
        public static bool TryGet<T>(out T result) where T : ScriptableSettings
        {
            if (TryGet(typeof(T), out ScriptableSettings value))
            {
                result = (T)value;

                return true;
            }

            result = null;

            return false;
        }

        /// <summary>
        /// Gets the registered settings of type <typeparamref name="T"/>. Unlike <see cref="TryGet{T}"/> this logs an
        /// error and returns <c>null</c> when none is registered. A settings type is only registered while its asset
        /// is loaded, so ensure the asset exists and is preloaded for it to be available at runtime.
        /// </summary>
        public static T Get<T>() where T : ScriptableSettings
        {
            if (TryGet(out T result))
            {
                return result;
            }

            Debug.LogError($"No {typeof(T).Name} is registered. Ensure its asset exists and is preloaded.");

            return null;
        }

        internal static bool TryGet(Type type, out ScriptableSettings result)
        {
            if (Map.TryGetValue(type, out ScriptableSettings value) && value != null)
            {
                result = value;

                return true;
            }

            result = null;

            return false;
        }

        /// <summary>
        /// Registers <paramref name="value"/> for <paramref name="type"/> only if no other instance is set yet
        /// (soft singleton). A second, different instance is warned about and ignored.
        /// </summary>
        internal static void Set(Type type, ScriptableSettings value)
        {
            if (value == null)
            {
                return;
            }

            if (Map.TryGetValue(type, out ScriptableSettings current) && current != null && current != value)
            {
                Debug.LogWarning($"{type.Name} is already registered to '{current.name}'. " +
                                 $"Keeping the first instance and ignoring '{value.name}'.", value);

                return;
            }

            Map[type] = value;
        }

        /// <summary>
        /// Forces <paramref name="value"/> as the registered instance for <paramref name="type"/>, or removes the
        /// entry entirely when <paramref name="value"/> is <c>null</c>.
        /// </summary>
        internal static void SetOrOverwrite(Type type, ScriptableSettings value)
        {
            if (value != null)
            {
                Map[type] = value;
            }
            else
            {
                Map.Remove(type);
            }
        }

        internal static IEnumerable<ScriptableSettingsSnapshot> Enumerate()
        {
            foreach (KeyValuePair<Type, ScriptableSettings> entry in Map)
            {
                yield return new ScriptableSettingsSnapshot(entry.Key, entry.Value);
            }
        }

        internal static void Reset()
        {
            Map.Clear();
        }
    }
}
