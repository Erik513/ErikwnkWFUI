using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A small confirmation that closes itself, shown near the bottom of a window. It belongs to that window (moves with it, never takes the focus) and is shown only while the window is in front; a toast that cannot be shown waits for it. See <see cref="ToastForm"/>.</summary>
        public static class Toasts
        {
            /// <summary>Shows a toast in the neutral gray.</summary>
            /// <param name="message">The text of the toast.</param>
            /// <param name="owner">The window the toast belongs to and is shown over.</param>
            /// <param name="colors">
            /// A color set for the background (the text is white or black) instead of the neutral gray:
            /// one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a
            /// <see cref="System.Drawing.Color"/>.
            /// </param>
            /// <param name="transparency">How see-through the toast is; medium by default.</param>
            /// <param name="duration">How long it stays (1.5, 2.5 or 4 seconds); medium by default.</param>
            /// <param name="delay">How long to wait before it is shown; not at all by default.</param>
            public static void ShowStandard(
                string message,
                Form owner,
                UIColorSet colors = null,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant)
            {
                UIToastFactory.ShowStandard(message, owner, colors, transparency, duration, delay);
            }

            /// <summary>Shows a toast in the neutral gray with a title above the message and, if wanted, own colors for each (see <see cref="InfoPopupSection"/>).</summary>
            /// <param name="content">The title (<see cref="InfoPopupSection.Header"/>) and the message (<see cref="InfoPopupSection.Text"/>).</param>
            /// <param name="owner">The window the toast belongs to and is shown over.</param>
            /// <param name="colors">A color set for the background instead of the neutral gray.</param>
            /// <param name="transparency">How see-through the toast is; medium by default.</param>
            /// <param name="duration">How long it stays (1.5, 2.5 or 4 seconds); medium by default.</param>
            /// <param name="delay">How long to wait before it is shown; not at all by default.</param>
            public static void ShowStandard(
                InfoPopupSection content,
                Form owner,
                UIColorSet colors = null,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant)
            {
                UIToastFactory.ShowStandard(content, owner, colors, transparency, duration, delay);
            }

            /// <summary>Like <see cref="ShowStandard(string, Form, UIColorSet, PopupTransparency, PopupDuration, PopupDelay)"/> but in the current accent color instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static void ShowPrimary(
                string message,
                Form owner,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant)
            {
                UIToastFactory.ShowPrimary(message, owner, transparency, duration, delay);
            }

            /// <summary>Like <see cref="ShowPrimary(string, Form, PopupTransparency, PopupDuration, PopupDelay)"/>, with a title and, if wanted, own colors for the title and the message.</summary>
            public static void ShowPrimary(
                InfoPopupSection content,
                Form owner,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant)
            {
                UIToastFactory.ShowPrimary(content, owner, transparency, duration, delay);
            }
        }
    }
}
