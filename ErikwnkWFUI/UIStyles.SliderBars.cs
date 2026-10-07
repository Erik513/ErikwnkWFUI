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
        /// <summary>A slim, theme-matching horizontal <see cref="Controls.SliderBar"/> (WinForms has none of its own).</summary>
        public static class SliderBars
        {
            /// <summary>A fixed neutral-grey slider. Any other colour: set <see cref="SliderBar.FillColor"/> on one of these.</summary>
            public static SliderBar CreateStandard(double value = 0, double maximum = 1.0)
            {
                return UISliderBarFactory.CreateStandard(value, maximum);
            }

            /// <summary>Same, but the fill and thumb follow the theme accent.</summary>
            public static SliderBar CreatePrimary(double value = 0, double maximum = 1.0)
            {
                return UISliderBarFactory.CreatePrimary(value, maximum);
            }
        }
    }
}
