using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The progress bar of <c>UIStyles.ProgressBars</c>: the standard <see cref="ProgressBar"/> in the continuous style with the
    /// colors of the library (the native control ignores them while visual styles are on, so this one opts out of them), with a
    /// thin border or without one, and - for the status variants - a fill that follows the value from red over yellow to green.
    /// The factories hand it back as a <see cref="ProgressBar"/>.
    /// </summary>
    public class ThemedProgressBar : ProgressBar
    {
        // Draws a 1px border around the bar so its extent is still legible
        // even in the rare case a caller's panel happens to also be
        // BackgroundMedium - relying on BackColor contrast alone isn't
        // guaranteed the way it is for bordered buttons/textboxes.
        // ProgressBar is natively drawn (WM_PAINT bypasses .NET's owner-draw
        // pipeline, so Paint never fires for it - confirmed by testing),
        // hence painting straight onto the HWND after the base paint instead
        // of overriding OnPaint. BorderStyle None skips the border for callers
        // who want the bar to blend into its container.

        private readonly bool _useStatusGradient;
        private readonly ThemeColor _trackColor;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;

        internal ThemedProgressBar(bool useStatusGradient = false)
        {
            _useStatusGradient = useStatusGradient;

            // With visual styles enabled, the native ProgressBar ignores
            // ForeColor/BackColor entirely and always shows the system
            // green bar. SetWindowTheme("", "") opts just this control
            // out of visual styles so the custom colors actually take
            // effect.
            HandleCreated += (s, e) => SetWindowTheme(Handle, "", "");

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

        protected override void WndProc(ref Message m)
        {
            // The native fill is drawn by base.WndProc's own WM_PAINT
            // handling, reading whatever ForeColor currently is -
            // there's no ValueChanged event (WinForms' ProgressBar
            // doesn't have one) and Value isn't virtual, so recomputing
            // the color here, right before the native paint actually
            // happens, is simpler than trying to intercept every place
            // Value could change instead.
            if (_useStatusGradient && m.Msg == WM_PAINT)
            {
                Color statusColor = Enabled
                    ? UIColors.GetStatusColor(Value, Minimum, Maximum)
                    : UIColors.DisabledGray;

                if (ForeColor != statusColor)
                    ForeColor = statusColor;
            }

            base.WndProc(ref m);

            if (m.Msg != WM_PAINT)
                return;

            // The classic (unthemed) native ProgressBar can show a 1px
            // 3D sunken-edge frame independent of SetWindowTheme above -
            // confirmed only after a real theme/accent-triggered
            // rebuild (constructed while its parent hierarchy was
            // already visible), never on a fresh construction. This
            // affects BOTH variants. Painting over it via the CLIENT dc
            // (Graphics.FromHwnd/GetDC) had no effect at all, which
            // points at this edge being drawn into the window's
            // NON-CLIENT area specifically - GetWindowDC (unlike GetDC)
            // gives access to the full window surface, non-client area
            // included, so painting there reaches pixels the client dc
            // structurally never could.
            //
            // Erasing on the window dc first and then drawing a bordered
            // variant's own border separately on the client dc (two
            // draws, two device contexts) left a second, visibly
            // different-colored ring of its own: the erase used
            // BackColor (BackgroundMedium, this control's own recessed
            // track shade), one pixel further out than the border drawn
            // after it, which doesn't match the row's actual
            // surrounding background - so instead of one clean border,
            // there were two concentric rings in different colors,
            // which is likely the "line" the user spotted. Drawing the
            // final, correct color (the border color for a bordered
            // variant, or just BackColor without one)
            // directly on the window dc in one single pass avoids that
            // entirely - there's no separate erase step to leave a
            // mismatched trace behind.
            Color edgeColor = EdgeColor;

            System.IntPtr windowDc = GetWindowDC(Handle);

            if (windowDc != System.IntPtr.Zero)
            {
                try
                {
                    using (Graphics g = Graphics.FromHdc(windowDc))
                    using (Pen pen = new Pen(edgeColor))
                        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
                finally
                {
                    ReleaseDC(Handle, windowDc);
                }
            }
        }
    }
}
