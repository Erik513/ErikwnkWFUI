using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A <see cref="Controls.VolumeSlider"/> - a <see cref="Controls.SliderBar"/> fixed to 0..1 that shows the current percentage in a small popup while dragged.</summary>
        public static class VolumeSliders
        {
            /// <summary>A fixed neutral-grey volume slider. Any other colour: set <see cref="SliderBar.FillColor"/> on one of these.</summary>
            public static VolumeSlider CreateStandard(double value = 0)
            {
                return UIVolumeSliderFactory.CreateStandard(value);
            }

            /// <summary>Same, but the fill and thumb follow the theme accent.</summary>
            public static VolumeSlider CreatePrimary(double value = 0)
            {
                return UIVolumeSliderFactory.CreatePrimary(value);
            }
        }
    }
}
