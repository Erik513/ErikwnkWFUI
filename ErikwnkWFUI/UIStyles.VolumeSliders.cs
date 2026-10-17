using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A <see cref="Controls.VolumeSlider"/> - a <see cref="Controls.TrackBar"/> fixed to 0..1 that shows the current percentage in a small popup while dragged.</summary>
        public static class VolumeSliders
        {
            /// <summary>A neutral-gray volume slider, or one in the color of a color set; set <see cref="TrackBar.FillColor"/> for any other color.</summary>
            /// <param name="value">The start value, from 0 to 1.</param>
            /// <param name="colors">A color set for the filled part and the thumb: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>. Null (the default) is the neutral gray.</param>
            public static VolumeSlider CreateStandard(double value = 0, UIColorSet colors = null)
            {
                return UIVolumeSliderFactory.CreateStandard(value, colors);
            }

            /// <summary>Same, but the fill and thumb follow the theme accent.</summary>
            public static VolumeSlider CreatePrimary(double value = 0)
            {
                return UIVolumeSliderFactory.CreatePrimary(value);
            }
        }
    }
}
