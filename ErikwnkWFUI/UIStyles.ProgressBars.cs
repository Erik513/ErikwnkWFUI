using System.Windows.Forms;
using ErikwnkWFUI.Factories;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A themed <see cref="ProgressBar"/> (continuous style, not the native blocky one).</summary>
        public static class ProgressBars
        {
            /// <summary>Neutral gray fill by default; set <see cref="Control.ForeColor"/> for any other color.</summary>
            public static ProgressBar CreateStandard()
            {
                return UIProgressBarFactory.CreateStandard();
            }

            /// <summary>Like <see cref="CreateStandard"/> but without the border, and BackColor left for you to match your own panel.</summary>
            public static ProgressBar CreateStandardBorderless()
            {
                return UIProgressBarFactory.CreateStandardBorderless();
            }

            /// <summary>Like <see cref="CreateStandard"/> but a fixed green fill.</summary>
            public static ProgressBar CreateGreen()
            {
                return UIProgressBarFactory.CreateGreen();
            }

            /// <summary>Like <see cref="CreateGreen"/> but without the border, and BackColor left for you to match your own panel - the closest a ProgressBar can get to a transparent background.</summary>
            public static ProgressBar CreateGreenBorderless()
            {
                return UIProgressBarFactory.CreateGreenBorderless();
            }

            /// <summary>Like <see cref="CreateGreen"/> but the fill follows the app-wide accent instead of the fixed green.</summary>
            public static ProgressBar CreatePrimary()
            {
                return UIProgressBarFactory.CreatePrimary();
            }

            /// <summary>Like <see cref="CreatePrimary"/> but without the border, and BackColor left for you to match your own panel - the accent-following equivalent of <see cref="CreateGreenBorderless"/>.</summary>
            public static ProgressBar CreatePrimaryBorderless()
            {
                return UIProgressBarFactory.CreatePrimaryBorderless();
            }

            /// <summary>Fill color follows Value instead of being fixed - red at Minimum, yellow at the midpoint, green at Maximum, blending smoothly between them. For a "status/health" bar where color communicates good/bad.</summary>
            public static ProgressBar CreateStatus()
            {
                return UIProgressBarFactory.CreateStatus();
            }

            /// <summary>Like <see cref="CreateStatus"/> but without the border, and BackColor left for you to match your own panel - the status-gradient equivalent of <see cref="CreateGreenBorderless"/>.</summary>
            public static ProgressBar CreateStatusBorderless()
            {
                return UIProgressBarFactory.CreateStatusBorderless();
            }
        }
    }
}
