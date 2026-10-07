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
        /// <summary>
        /// A thin, fully custom-drawn <see cref="SlimProgressBar"/> - no
        /// native Win32 control underneath (unlike <see cref="ProgressBars"/>),
        /// so no border/theming quirks to work around. For slim status-strip-
        /// style progress indicators rather than a prominent, full-size bar.
        /// </summary>
        public static class SlimProgressBars
        {
            /// <summary>Neutral grey fill by default; set <see cref="Control.ForeColor"/> for any other colour.</summary>
            public static SlimProgressBar CreateStandard()
            {
                return UISlimProgressBarFactory.CreateStandard();
            }

            /// <summary>Like <see cref="CreateStandard"/> but a fixed green fill.</summary>
            public static SlimProgressBar CreateGreen()
            {
                return UISlimProgressBarFactory.CreateGreen();
            }

            /// <summary>Like <see cref="CreateGreen"/> but the fill follows the app-wide accent instead of the fixed green.</summary>
            public static SlimProgressBar CreatePrimary()
            {
                return UISlimProgressBarFactory.CreatePrimary();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static SlimProgressBar CreateStatus()
            {
                return UISlimProgressBarFactory.CreateStatus();
            }
        }
    }
}
