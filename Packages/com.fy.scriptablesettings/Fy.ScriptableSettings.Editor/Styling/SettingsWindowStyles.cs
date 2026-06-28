using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fy.ScriptableSettings.Editor
{
    /// <summary>
    /// Shared visual vocabulary (theme colors, spacing, card / header / pill builders) for the settings hub window.
    /// Ported from the Fy Service Locator window so both editor surfaces read as one family.
    /// </summary>
    internal static class SettingsWindowStyles
    {
        internal const float Space1 = 4f;
        internal const float Space2 = 8f;
        internal const float CardRadius = 5f;

        internal static readonly Color IncludedColor = new(0.40f, 0.80f, 0.45f);
        internal static readonly Color EditorOnlyColor = new(0.90f, 0.72f, 0.32f);

        private static bool IsDark => EditorGUIUtility.isProSkin;

        internal static Color MutedTextColor => IsDark ? new Color(1f, 1f, 1f, 0.72f) : new Color(0f, 0f, 0f, 0.6f);

        /// <summary>
        /// Persistent highlight for the currently selected list row.
        /// </summary>
        internal static Color SelectedRowColor => IsDark ? new Color(0.22f, 0.38f, 0.57f) : new Color(0.36f, 0.56f, 0.86f);

        /// <summary>
        /// Darker overlay for the first (and every other) list row in the alternating (zebra) pattern.
        /// </summary>
        internal static Color RowDarkColor => IsDark ? new Color(0f, 0f, 0f, 0.18f) : new Color(0f, 0f, 0f, 0.06f);

        /// <summary>
        /// Lighter overlay for the second (and every other) list row in the alternating (zebra) pattern.
        /// </summary>
        internal static Color RowLightColor => IsDark ? new Color(1f, 1f, 1f, 0.06f) : new Color(1f, 1f, 1f, 0.5f);

        private static Color CardBackgroundColor =>
            IsDark ? new Color(1f, 1f, 1f, 0.035f) : new Color(0f, 0f, 0f, 0.02f);

        private static Color CardBorderColor => IsDark ? new Color(0f, 0f, 0f, 0.45f) : new Color(0f, 0f, 0f, 0.16f);

        internal static Color SeparatorColor => IsDark ? new Color(1f, 1f, 1f, 0.09f) : new Color(0f, 0f, 0f, 0.09f);

        internal static VisualElement CreateCard()
        {
            VisualElement card = new VisualElement();
            card.style.marginTop = Space1;
            card.style.marginBottom = Space2;
            card.style.marginLeft = Space2;
            card.style.marginRight = Space2;
            card.style.paddingTop = Space2;
            card.style.paddingBottom = Space2;
            card.style.paddingLeft = Space2;
            card.style.paddingRight = Space2;
            card.style.backgroundColor = CardBackgroundColor;
            SetBorderRadius(card, CardRadius);

            Color borderColor = CardBorderColor;
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = borderColor;
            card.style.borderBottomColor = borderColor;
            card.style.borderLeftColor = borderColor;
            card.style.borderRightColor = borderColor;

            return card;
        }

        internal static VisualElement CreateTypeHeader(string prefix, string name, string subtitle)
        {
            VisualElement header = new VisualElement();
            header.style.marginBottom = Space1;
            header.Add(CreatePrefixedLine(prefix, name));

            if (!string.IsNullOrEmpty(subtitle))
            {
                Label subtitleLabel = new Label(subtitle);
                subtitleLabel.style.color = MutedTextColor;
                subtitleLabel.style.fontSize = 12;
                header.Add(subtitleLabel);
            }

            return header;
        }

        internal static VisualElement CreatePrefixedLine(string prefix, string value)
        {
            VisualElement line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.style.alignItems = Align.Center;

            Label prefixLabel = new Label($"{prefix}:");
            prefixLabel.style.color = MutedTextColor;
            prefixLabel.style.fontSize = 11;
            prefixLabel.style.marginRight = Space1;
            line.Add(prefixLabel);

            Label valueLabel = new Label(value);
            valueLabel.selection.isSelectable = true;
            valueLabel.style.fontSize = 14;
            valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            line.Add(valueLabel);

            return line;
        }

        internal static VisualElement CreateSubSection(string title)
        {
            VisualElement section = new VisualElement();
            section.style.marginTop = Space2;

            VisualElement separator = new VisualElement();
            separator.style.height = 1;
            separator.style.backgroundColor = SeparatorColor;
            separator.style.marginBottom = Space1;
            section.Add(separator);

            Label header = new Label(title.ToUpperInvariant());
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.fontSize = 11;
            header.style.color = MutedTextColor;
            header.style.marginBottom = Space1;
            section.Add(header);

            return section;
        }

        internal static VisualElement CreateStatusPill(Color color, string text)
        {
            VisualElement pill = new VisualElement();
            pill.style.flexDirection = FlexDirection.Row;
            pill.style.alignItems = Align.Center;
            pill.style.flexShrink = 0;

            VisualElement dot = CreateDot(color);
            dot.style.marginRight = Space1;
            pill.Add(dot);

            Label label = new Label(text);
            label.style.color = color;
            label.style.fontSize = 12;
            pill.Add(label);

            return pill;
        }

        /// <summary>
        /// A small round status dot of the given color (the same marker used by the In-Build / Excluded pill).
        /// </summary>
        internal static VisualElement CreateDot(Color color)
        {
            VisualElement dot = new VisualElement();
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.flexShrink = 0;
            dot.style.backgroundColor = color;
            SetBorderRadius(dot, 4);

            return dot;
        }

        /// <summary>
        /// A small solid right-pointing triangle (built with the border-triangle trick), used as a selection marker.
        /// </summary>
        internal static VisualElement CreateRightArrow(Color color)
        {
            VisualElement arrow = new VisualElement();
            arrow.style.width = 0;
            arrow.style.height = 0;
            arrow.style.flexShrink = 0;
            arrow.style.borderTopWidth = 4f;
            arrow.style.borderBottomWidth = 4f;
            arrow.style.borderLeftWidth = 7;
            arrow.style.borderRightWidth = 1;
            arrow.style.borderTopColor = Color.clear;
            arrow.style.borderBottomColor = Color.clear;
            arrow.style.borderLeftColor = color;
            arrow.style.borderRightColor = Color.clear;

            return arrow;
        }

        internal static Label CreateInfoLabel(string text)
        {
            Label label = new Label(text);
            label.style.color = MutedTextColor;
            label.style.whiteSpace = WhiteSpace.Normal;

            return label;
        }

        internal static void SetBorderRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
