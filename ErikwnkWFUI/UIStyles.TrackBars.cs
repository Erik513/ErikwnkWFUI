using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A slim, theme-matching horizontal <see cref="Controls.TrackBar"/> (WinForms has none of its own).</summary>
        public static class TrackBars
        {
            /// <summary>A neutral-gray slider, or one in the color of a color set; set <see cref="TrackBar.FillColor"/> for any other color.</summary>
            /// <param name="value">The start value, from 0 to <paramref name="maximum"/>.</param>
            /// <param name="maximum">The top of the range (1 by default).</param>
            /// <param name="colors">A color set for the filled part and the thumb: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. They have the main color of the set (its <c>Hover</c> color); null (the default) is the neutral gray.</param>
            public static TrackBar CreateStandard(double value = 0, double maximum = 1.0, UIColorSet colors = null)
            {
                return UITrackBarFactory.CreateStandard(value, maximum, colors);
            }

            /// <summary>Same, but the fill and thumb follow the theme accent.</summary>
            public static TrackBar CreatePrimary(double value = 0, double maximum = 1.0)
            {
                return UITrackBarFactory.CreatePrimary(value, maximum);
            }
        }
    }
}
