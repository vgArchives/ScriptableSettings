using System;
using System.Collections.Generic;
using UnityEditor;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Discovers every concrete <see cref="SettingsDrawerBase{T}"/> via <see cref="TypeCache"/> and maps each one to the
    /// settings type it renders. Types without a custom drawer fall back to <see cref="DefaultSettingsDrawer"/>.
    /// </summary>
    public static class SettingsDrawerRegistry
    {
        private static Dictionary<Type, ISettingsDrawer> _drawersBySettingsType;

        /// <summary>
        /// Returns the custom drawer registered for <paramref name="settingsType"/>, or a
        /// <see cref="DefaultSettingsDrawer"/> when none is registered.
        /// </summary>
        public static ISettingsDrawer GetDrawer(Type settingsType)
        {
            EnsureBuilt();

            if (_drawersBySettingsType.TryGetValue(settingsType, out ISettingsDrawer drawer))
            {
                return drawer;
            }

            return new DefaultSettingsDrawer();
        }

        private static void EnsureBuilt()
        {
            if (_drawersBySettingsType != null)
            {
                return;
            }

            _drawersBySettingsType = new Dictionary<Type, ISettingsDrawer>();

            foreach (Type drawerType in TypeCache.GetTypesDerivedFrom(typeof(SettingsDrawerBase<>)))
            {
                if (drawerType.IsAbstract)
                {
                    continue;
                }

                Type settingsType = GetSettingsType(drawerType);

                if (settingsType == null)
                {
                    continue;
                }

                if (Activator.CreateInstance(drawerType) is ISettingsDrawer instance)
                {
                    _drawersBySettingsType[settingsType] = instance;
                }
            }
        }

        private static Type GetSettingsType(Type drawerType)
        {
            Type current = drawerType;

            while (current != null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(SettingsDrawerBase<>))
                {
                    return current.GetGenericArguments()[0];
                }

                current = current.BaseType;
            }

            return null;
        }
    }
}
