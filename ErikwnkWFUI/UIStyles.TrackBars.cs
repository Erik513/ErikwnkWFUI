using System;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A slim, theme-matching horizontal <see cref="Controls.TrackBar"/>; with the value popup and a percent format it is a volume slider.</summary>
        public static class TrackBars
        {
            /// <summary>A neutral-gray slider, or one in the color of a color set; set <see cref="TrackBar.FillColor"/> for any other color.</summary>
            /// <param name="value">The start value, from 0 to <paramref name="maximum"/>.</param>
            /// <param name="maximum">The top of the range (1 by default).</param>
            /// <param name="colors">A color set for the filled part and the thumb: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. They have the main color of the set (its <c>Hover</c> color); null (the default) is the neutral gray.</param>
            /// <param name="showValuePopup">Whether the value is shown in a small popup above the thumb (<see cref="TrackBar.ShowValuePopup"/>).</param>
            /// <param name="formatValue">How the value is written in the popup and for a screen reader (<see cref="TrackBar.FormatValue"/>); for a volume from 0 to 1 give <see cref="TrackBar.FormatPercent"/>. Null: a number.</param>
            public static TrackBar CreateStandard(double value = 0, double maximum = 1.0, UIColorSet colors = null,
                bool showValuePopup = false, Func<double, string> formatValue = null)
            {
                return UITrackBarFactory.CreateStandard(value, maximum, colors, showValuePopup, formatValue);
            }

            /// <summary>Same, but the fill and thumb follow the theme accent (<paramref name="value"/>, <paramref name="maximum"/>, <paramref name="showValuePopup"/> and <paramref name="formatValue"/> as there).</summary>
            public static TrackBar CreatePrimary(double value = 0, double maximum = 1.0,
                bool showValuePopup = false, Func<double, string> formatValue = null)
            {
                return UITrackBarFactory.CreatePrimary(value, maximum, showValuePopup, formatValue);
            }
        }
    }
}
