using System;
using System.Collections.Generic;
using Fy.ScriptableSettings;
using UnityEngine;

namespace Fy.ScriptableSettings.Examples
{
    /// <summary>
    /// Holds a type-effectiveness grid (attacker x defender damage multipliers). Paired with a custom
    /// <c>SettingsDrawerBase</c> that renders the grid as an editable table — the headline drawer demo.
    /// </summary>
    public sealed class WeaknessTableSettings : ScriptableSettings
    {
        [SerializeField] private ElementType[] _elements;
        [SerializeField] private float[] _multipliers;

        /// <summary>
        /// Gets the number of elements on each axis of the grid.
        /// </summary>
        public int Count => _elements?.Length ?? 0;

        /// <summary>
        /// Gets the element axis labels.
        /// </summary>
        public IReadOnlyList<ElementType> Elements => _elements;

        /// <summary>
        /// Gets the damage multiplier applied when <paramref name="attacker"/> hits <paramref name="defender"/>.
        /// </summary>
        public float GetMultiplier(int attacker, int defender)
        {
            return _multipliers[(attacker * Count) + defender];
        }

        /// <summary>
        /// Fills the grid with every <see cref="ElementType"/> and a neutral 1x multiplier for each cell.
        /// </summary>
        public void ResetToDefault()
        {
            _elements = (ElementType[])Enum.GetValues(typeof(ElementType));

            int count = _elements.Length;
            _multipliers = new float[count * count];

            for (int i = 0; i < _multipliers.Length; i++)
            {
                _multipliers[i] = 1f;
            }
        }
    }
}
