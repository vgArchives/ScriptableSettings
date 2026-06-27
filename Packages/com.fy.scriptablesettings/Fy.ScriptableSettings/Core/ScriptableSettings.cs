using System;
using UnityEngine;

namespace Fy.ScriptableSettings
{
    /// <summary>
    /// Base class for globally-accessible, ScriptableObject-backed settings data. Each subclass is a soft
    /// singleton that registers itself with <see cref="ScriptableSettingsRegistry"/> when loaded and unregisters
    /// when unloaded.
    /// </summary>
    public abstract class ScriptableSettings : ScriptableObject
    {
        [Tooltip("Should this setting be included in the preloaded assets so it is available in builds?")]
        [SerializeField] private bool _preload = true;

        /// <summary>
        /// Gets a value indicating whether this setting is included in the build's preloaded assets.
        /// </summary>
        public bool Preload => _preload;

        /// <summary>
        /// Non-virtual by design. Override <see cref="OnLoaded"/> for one-time initialization instead.
        /// </summary>
        private void OnEnable()
        {
            OnLoaded();
        }

        /// <summary>
        /// Non-virtual by design. Override <see cref="OnUnload"/> for one-time teardown instead.
        /// </summary>
        private void OnDisable()
        {
            Type type = GetType();

            if (ScriptableSettingsRegistry.TryGet(type, out ScriptableSettings current) && current == this)
            {
                ScriptableSettingsRegistry.SetOrOverwrite(type, null);
            }

            OnUnload();
        }

        /// <summary>
        /// One-time initialization hook. Runs from <see cref="OnEnable"/>, including in edit mode.
        /// </summary>
        protected virtual void OnLoaded()
        {
            ScriptableSettingsRegistry.Set(GetType(), this);
        }

        /// <summary>
        /// One-time teardown hook. Runs from <see cref="OnDisable"/>, including in edit mode.
        /// </summary>
        protected virtual void OnUnload()
        {
        }
    }
}
