using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI
{
    public static partial class UIStyles
    {
        /// <summary>A small message that closes itself, shown in a window (below the middle by default). It belongs to that window (moves with it, never takes the focus) and is shown only while the window is in front; a toast that cannot be shown waits for it. See <see cref="ToastForm"/>.</summary>
        public static class Toasts
        {
            /// <summary>Creates the settings of a toast as properties, to show toasts with them again and again (<see cref="Toaster.Show(string, Form)"/>); in the neutral gray unless <paramref name="colors"/> says otherwise.</summary>
            /// <param name="colors">A color set for the background instead of the neutral gray: one of <c>UIStyles.Colors.Sets</c>, <c>UIStyles.Colors.CreateSet(color)</c> or simply a <see cref="System.Drawing.Color"/>.</param>
            /// <param name="transparency">How see-through the toasts are; medium by default.</param>
            /// <param name="duration">How long they stay (1.5, 2.5 or 4 seconds); medium by default.</param>
            /// <param name="delay">How long to wait before one is shown; not at all by default.</param>
            /// <param name="icon">The symbol for the kind of message: none (the default), a check mark, an info, a warning or an error.</param>
            /// <param name="symbol">Any other symbol instead of <paramref name="icon"/>, one of <c>UIStyles.Symbols</c> (<c>UIStyles.Symbols.Copy</c>, say); null (the default) uses <paramref name="icon"/>.</param>
            /// <param name="position">Where the toast is shown in the window: below the middle (centered, in the lower part; the default), the middle, above the middle, or at the top or bottom, centered or in a corner; the corners and edges keep a fixed distance from the window's edge.</param>
            public static Toaster CreateStandard(
                UIColorSet colors = null,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant,
                ToastIcon icon = ToastIcon.None,
                string symbol = null,
                ToastPosition position = ToastPosition.BelowCenter)
            {
                return UIToastFactory.CreateStandard(colors, transparency, duration, delay, icon, symbol, position);
            }

            /// <summary>Like <see cref="CreateStandard"/> but in the current accent color - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static Toaster CreatePrimary(
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant,
                ToastIcon icon = ToastIcon.None,
                string symbol = null,
                ToastPosition position = ToastPosition.BelowCenter)
            {
                return UIToastFactory.CreatePrimary(transparency, duration, delay, icon, symbol, position);
            }

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
            /// <param name="icon">The symbol for the kind of message: none (the default), a check mark, an info, a warning or an error.</param>
            /// <param name="symbol">Any other symbol instead of <paramref name="icon"/>, one of <c>UIStyles.Symbols</c> (<c>UIStyles.Symbols.Copy</c>, say); null (the default) uses <paramref name="icon"/>.</param>
            /// <param name="position">Where the toast is shown in the window: below the middle (centered, in the lower part; the default), the middle, above the middle, or at the top or bottom, centered or in a corner; the corners and edges keep a fixed distance from the window's edge.</param>
            public static void ShowStandard(
                string message,
                Form owner,
                UIColorSet colors = null,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant,
                ToastIcon icon = ToastIcon.None,
                string symbol = null,
                ToastPosition position = ToastPosition.BelowCenter)
            {
                UIToastFactory.ShowStandard(message, owner, colors, transparency, duration, delay, icon, symbol, position);
            }

            /// <summary>Shows a toast in the neutral gray with a title above the message and, if wanted, own colors for each (see <see cref="InfoPopupSection"/>).</summary>
            /// <param name="content">The title (<see cref="InfoPopupSection.Header"/>) and the message (<see cref="InfoPopupSection.Text"/>).</param>
            /// <param name="owner">The window the toast belongs to and is shown over.</param>
            /// <param name="colors">A color set for the background instead of the neutral gray.</param>
            /// <param name="transparency">How see-through the toast is; medium by default.</param>
            /// <param name="duration">How long it stays (1.5, 2.5 or 4 seconds); medium by default.</param>
            /// <param name="delay">How long to wait before it is shown; not at all by default.</param>
            /// <param name="icon">The symbol for the kind of message: none (the default), a check mark, an info, a warning or an error.</param>
            /// <param name="symbol">Any other symbol instead of <paramref name="icon"/>, one of <c>UIStyles.Symbols</c> (<c>UIStyles.Symbols.Copy</c>, say); null (the default) uses <paramref name="icon"/>.</param>
            /// <param name="position">Where the toast is shown in the window: below the middle (centered, in the lower part; the default), the middle, above the middle, or at the top or bottom, centered or in a corner; the corners and edges keep a fixed distance from the window's edge.</param>
            public static void ShowStandard(
                InfoPopupSection content,
                Form owner,
                UIColorSet colors = null,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant,
                ToastIcon icon = ToastIcon.None,
                string symbol = null,
                ToastPosition position = ToastPosition.BelowCenter)
            {
                UIToastFactory.ShowStandard(content, owner, colors, transparency, duration, delay, icon, symbol, position);
            }

            /// <summary>Like <see cref="ShowStandard(string, Form, UIColorSet, PopupTransparency, PopupDuration, PopupDelay, ToastIcon, string, ToastPosition)"/> but in the current accent color instead of the neutral gray - mirrors <see cref="Buttons.CreatePrimary"/>.</summary>
            public static void ShowPrimary(
                string message,
                Form owner,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant,
                ToastIcon icon = ToastIcon.None,
                string symbol = null,
                ToastPosition position = ToastPosition.BelowCenter)
            {
                UIToastFactory.ShowPrimary(message, owner, transparency, duration, delay, icon, symbol, position);
            }

            /// <summary>Like <see cref="ShowPrimary(string, Form, PopupTransparency, PopupDuration, PopupDelay, ToastIcon, string, ToastPosition)"/>, with a title and, if wanted, own colors for the title and the message.</summary>
            public static void ShowPrimary(
                InfoPopupSection content,
                Form owner,
                PopupTransparency transparency = PopupTransparency.Medium,
                PopupDuration duration = PopupDuration.Medium,
                PopupDelay delay = PopupDelay.Instant,
                ToastIcon icon = ToastIcon.None,
                string symbol = null,
                ToastPosition position = ToastPosition.BelowCenter)
            {
                UIToastFactory.ShowPrimary(content, owner, transparency, duration, delay, icon, symbol, position);
            }
        }
    }
}
