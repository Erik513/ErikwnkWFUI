using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A slim, theme-matching horizontal slider: the library's version of the WinForms <c>TrackBar</c>, fully custom-drawn. The
    /// <see cref="Value"/> runs from <see cref="Minimum"/> (0 by default) to <see cref="Maximum"/> (1 by default) and is a fraction,
    /// not a whole number. The mouse drags the thumb (a press on the thumb takes hold of it where it was pressed, a press on the track
    /// puts it there), and the keys (arrows by <see cref="SmallChange"/>, Page Up and Down by <see cref="LargeChange"/>, Home and End to
    /// the ends) and the mouse wheel work as on the original. <see cref="ValueChanged"/> fires on every change from the user (drag,
    /// click on the track, keys, wheel); unlike the original it does not fire when <see cref="Value"/> is set from code, so that code
    /// which keeps the slider up to date cannot start a loop. The filled part and the thumb are gray, the color of <see cref="Colors"/>
    /// (a color set or simply a color; <c>UIStyles.TrackBars.CreatePrimary</c> follows the accent), moved as far as needed to stand
    /// out from what the slider lies on, in both themes. The popup has the same colors unless <see cref="ValuePopupColors"/> says otherwise. <see cref="ShowValuePopup"/>
    /// shows the value above the thumb, written as <see cref="FormatValue"/> says (<see cref="FormatPercent"/> for a volume from 0 to 1);
    /// a screen reader is told it is a slider and its value. The measures are scaled with the screen. Not offered: tick marks, the
    /// vertical orientation, <c>AutoSize</c>; right-to-left layouts are not supported (<c>RightToLeft</c> is ignored).
    /// </summary>
    public partial class TrackBar : Control
    {

        // The measures at 96 dpi; the screen of the control scales them (see TrackHeight, ThumbRadius, TrackInsetPixels).
        private const int TrackHeight96 = 4;
        private const int ThumbRadius96 = 6;

        /// <summary>
        /// Pixels the track is inset from each edge (room for the thumb) at 96 dpi. Anything meant to line up with the track (e.g. a
        /// progress bar next to the slider) should use the same left/right margin - <see cref="TrackInsetPixels"/> where the screen is
        /// scaled.
        /// </summary>
        public const int TrackInset = ThumbRadius96 + 1;

        /// <summary>The same in the pixels of the screen this slider is on (more than <see cref="TrackInset"/> where the screen is scaled).</summary>
        [Browsable(false)]
        public int TrackInsetPixels
        {
            get { return ThumbRadius + 1; }
        }

        // Another dpi than the one of the screen, for the tests.
        internal int DpiOverride;

        private int TrackHeight
        {
            get { return Dpi.Scale(this, TrackHeight96, DpiOverride); }
        }

        private int ThumbRadius
        {
            get { return Dpi.Scale(this, ThumbRadius96, DpiOverride); }
        }

        public TrackBar()
        {
            // Everything is read while painting: a change of the theme only needs a repaint.
            ThemeWatcher.Attach(this, Invalidate);
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.Selectable,
                true);
            Height = 20;
            TabStop = true;
            BackColor = Color.Transparent;

            InitializePopup();
        }

        /// <summary>Raised when the user changes the value (drag, click on the track, keys, wheel), once for each other value; not when code sets it.</summary>
        public event EventHandler ValueChanged;

        /// <summary>Raised when the user presses the thumb or the track to begin a drag (before the value follows the mouse).</summary>
        public event EventHandler DragStarted;

        /// <summary>Raised once when a drag ends: the mouse is released, taken away (a dialog, another window) or the slider is disabled.</summary>
        public event EventHandler DragEnded;

        /// <summary>True while the user is dragging the thumb - callers can pause external updates.</summary>
        [Browsable(false)]
        public bool IsDragging
        {
            get { return _dragging; }
        }

        private int TrackLeft
        {
            get { return TrackInsetPixels; }
        }

        private int TrackRight
        {
            get { return Width - TrackInsetPixels; }
        }

        private int TrackWidth
        {
            get { return Math.Max(1, TrackRight - TrackLeft); }
        }

        // The value at a pixel of the track: its left end is the minimum, its right end the maximum (beyond the ends they are the ends).
        private double ValueFromX(int x)
        {
            double fraction = Clamp((x - TrackLeft) / (double)TrackWidth, 0, 1);

            return _minimum * (1 - fraction) + _maximum * fraction;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int midY = Height / 2;
            int trackTop = midY - TrackHeight / 2;
            int thumbX = TrackLeft + (int)Math.Round(Fraction * TrackWidth);

            Color trackColor = GetTrackColor();
            Color fillColor = GetFillColor();

            using (SolidBrush track = new SolidBrush(trackColor))
            {
                g.FillRectangle(track, TrackLeft, trackTop, TrackWidth, TrackHeight);
            }
            using (SolidBrush fill = new SolidBrush(fillColor))
            {
                g.FillRectangle(fill, TrackLeft, trackTop, Math.Max(0, thumbX - TrackLeft), TrackHeight);
            }

            if (Enabled)
            {
                // The thumb grows slightly on hover, drag or keyboard focus -
                // that is the only focus cue (a dotted rectangle around a slider
                // looks out of place).
                int r = _hover || _dragging || Focused ? ThumbRadius : ThumbRadius - Math.Max(1, ThumbRadius / 6);
                using (SolidBrush thumb = new SolidBrush(fillColor))
                {
                    g.FillEllipse(thumb, thumbX - r, midY - r, r * 2, r * 2);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposePopup();
            }

            base.Dispose(disposing);
        }
    }
}
