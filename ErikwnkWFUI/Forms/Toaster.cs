using System;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// The settings of a toast kept as properties, to show toasts with them again and again
    /// (like <see cref="InfoPopupForm"/> keeps its settings): created with
    /// <c>UIStyles.Toasts.CreateStandard</c> or <c>CreatePrimary</c>, changed at any time, used
    /// by every toast it shows from then on. A toast that is already up is not changed.
    /// It holds no window of its own, so it needs no disposing; the toasts it shows are
    /// <see cref="ToastForm"/>s with all their rules (one at a time, waiting for the window).
    /// </summary>
    public class Toaster
    {
        /// <summary>The colors of the toast: null is the neutral gray; see <see cref="ToastForm.Colors"/>.</summary>
        public UIColorSet Colors
        {
            get { return _colorsSource?.Invoke() ?? _colors; }
            set
            {
                _colors = value;
                _colorsSource = null;
            }
        }

        private UIColorSet _colors;
        private Func<UIColorSet> _colorsSource;

        // A set that is asked for again at each toast (the primary set, which follows the accent): a toaster that is
        // kept for the whole run shows the accent as it is now, not as it was when the toaster was made.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorsSource = source;
            _colors = null;
        }

        /// <summary>How see-through the toasts are; medium by default.</summary>
        public PopupTransparency Transparency { get; set; } = PopupTransparency.Medium;

        /// <summary>How long a toast stays; medium (2.5 seconds) by default.</summary>
        public PopupDuration Duration { get; set; } = PopupDuration.Medium;

        /// <summary>How long to wait before a toast is shown; not at all by default.</summary>
        public PopupDelay Delay { get; set; } = PopupDelay.Instant;

        /// <summary>The symbol for the kind of message; none by default.</summary>
        public ToastIcon Icon { get; set; } = ToastIcon.None;

        /// <summary>Any other symbol instead of <see cref="Icon"/>, one of <c>UIStyles.Symbols</c>; null (the default) uses <see cref="Icon"/>.</summary>
        public string CustomSymbol { get; set; }

        /// <summary>Where the toast is shown in the window; below the middle (centered, in the lower part) by default.</summary>
        public ToastPosition Position { get; set; } = ToastPosition.BelowCenter;

        /// <summary>Shows a toast with this message in <paramref name="owner"/>, at <see cref="Position"/>.</summary>
        public void Show(string message, Form owner)
        {
            ToastForm.ShowToast(message, owner, Colors, Transparency, Duration, Delay, Icon, CustomSymbol, Position);
        }

        /// <summary>Shows a toast with a title and a message (and their own colors) in <paramref name="owner"/>, at <see cref="Position"/>.</summary>
        public void Show(InfoPopupSection content, Form owner)
        {
            ToastForm.ShowToast(content, owner, Colors, Transparency, Duration, Delay, Icon, CustomSymbol, Position);
        }
    }
}
