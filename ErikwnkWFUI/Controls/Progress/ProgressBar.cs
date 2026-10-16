using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The progress bar of <c>UIStyles.ProgressBars</c>: the standard <see cref="ProgressBar"/> (<c>Minimum</c>, <c>Maximum</c>,
    /// <c>Value</c>, <c>Step</c>, <c>PerformStep</c>, <c>Increment</c> and the <c>Style</c>s Blocks, Continuous and Marquee with its
    /// <c>MarqueeAnimationSpeed</c>), drawn by the library in its colors (the native control ignores them while visual styles are
    /// on), with a thin border or without one, and - for the status variants - a fill that follows the value from red over yellow
    /// to green. Right-to-left layouts are not supported (<c>RightToLeft</c> is ignored). The factories hand it back as a
    /// <see cref="ProgressBar"/>.
    /// </summary>
    public class ProgressBar : System.Windows.Forms.ProgressBar
    {
        // The 1 px border keeps the extent of the bar legible even when the panel it lies on is BackgroundMedium too (BackColor
        // alone does not guarantee a contrast); BorderStyle None skips it for callers who want the bar to blend into its container.

        private readonly bool _useStatusGradient;
        private readonly ThemeColor _trackColor;
        private readonly MarqueeAnimator _marquee;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;

        internal ProgressBar(bool useStatusGradient = false)
        {
            _useStatusGradient = useStatusGradient;

            // The native bar ignores ForeColor and BackColor while visual styles are on, so it is not used for painting at all.
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _marquee = new MarqueeAnimator(this, () => Style == ProgressBarStyle.Marquee, () => MarqueeAnimationSpeed);

            // The track: the recessed surface of the theme (until BackColor is set).
            _trackColor = new ThemeColor(() => UIColors.BackgroundMedium);
            ApplyTrack();
            ThemeWatcher.Attach(this, () =>
            {
                ApplyTrack();
                Invalidate();
            });
        }

        /// <summary>
        /// The color of the track. By default the recessed surface of the theme (it follows a change of the theme). Set a color for another
        /// one. <see cref="Color.Transparent"/> - which a <see cref="ProgressBar"/> cannot have itself - makes the bar look transparent: the
        /// track takes the color of the panel it lies on, also when that changes (and in both themes). <see cref="Color.Empty"/> gives the
        /// default back. Independent of <see cref="BorderStyle"/>.
        /// </summary>
        public override Color BackColor
        {
            get { return base.BackColor; }
            set
            {
                if (_trackColor == null)
                {
                    base.BackColor = value;

                    return;
                }

                if (value.IsEmpty)
                    _trackColor.Reset();
                else
                    _trackColor.Set(value);

                ApplyTrack();
            }
        }

        // What the bar lies on: the first color that is not see-through behind it.
        private Color GetParentSurface()
        {
            return Parent != null ? SurfaceColor.Of(Parent) : UIColors.BackgroundMedium;
        }

        // The native bar is given a color of its own always: Transparent would throw. Transparent means the one of the panel.
        private void ApplyTrack()
        {
            Color track = _trackColor.Value;

            if (track.A != 255)
                track = GetParentSurface();

            if (base.BackColor != track)
                base.BackColor = track;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            ApplyTrack();
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);
            ApplyTrack();
        }

        /// <summary>
        /// The thin border around the bar: <see cref="BorderStyle.FixedSingle"/> (the default; <see cref="BorderStyle.Fixed3D"/> looks the
        /// same) or <see cref="BorderStyle.None"/>, where the bar has no frame of its own and its edge is the track.
        /// Only the border: the color of the track is <see cref="BackColor"/>, which can be transparent.
        /// </summary>
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                if (value == _borderStyle)
                    return;

                _borderStyle = value;
                Invalidate();
            }
        }

        // The color of the 1 px edge around the bar: the border, or - without one - the track itself (what is seen, when it is transparent).
        internal Color EdgeColor
        {
            get { return _borderStyle != BorderStyle.None ? UIColors.BorderMedium : BackColor; }
        }

        // The bar draws itself: the native control paints in WM_PAINT without the Paint event, and with visual styles off it shows
        // nothing at all for the marquee style. UserPaint makes WM_PAINT reach OnPaint (the native control still tells us when the
        // value changes: Value, Step, PerformStep and Increment need nothing here).
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle area = ClientRectangle;

            using (SolidBrush track = new SolidBrush(BackColor))
                g.FillRectangle(track, area);

            // The edge: the border, or - without one - the track itself.
            using (Pen edge = new Pen(EdgeColor))
                g.DrawRectangle(edge, 0, 0, area.Width - 1, area.Height - 1);

            Color fill;

            if (!Enabled)
                fill = UIColors.DisabledGray;
            else if (_useStatusGradient)
                fill = UIColors.GetStatusColor(Value, Minimum, Maximum);
            else
                fill = ForeColor;

            area.Inflate(-1, -1);
            ProgressBarPainting.PaintFill(g, area, Style, ProgressBarPainting.GetFraction(Minimum, Maximum, Value), fill, _marquee.Offset);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // The native bar has a sunken edge of its own around the client area (WS_EX_STATICEDGE, which the control gives itself
            // when it is created): the whole window is drawn by the library instead, so the edge is taken away and the border
            // of BorderStyle is the only one.
            int exStyle = GetWindowLong(Handle, GWL_EXSTYLE);

            if ((exStyle & WS_EX_STATICEDGE) != 0)
            {
                SetWindowLong(Handle, GWL_EXSTYLE, exStyle & ~WS_EX_STATICEDGE);
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }

            // Changing the Style creates the handle again: the right moment to start or stop the marquee.
            _marquee.Update();
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            _marquee.Update();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _marquee.Dispose();

            base.Dispose(disposing);
        }
    }
}
