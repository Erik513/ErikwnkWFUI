using System;
using System.Drawing;
using System.Windows.Forms;
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
        // of overriding OnPaint. drawBorder lets the Borderless variants
        // skip this entirely for callers who want the bar to blend into
        // its container.

        private readonly bool _drawBorder;
        private readonly bool _useStatusGradient;

        internal ThemedProgressBar(bool drawBorder, bool useStatusGradient = false)
        {
            _drawBorder = drawBorder;
            _useStatusGradient = useStatusGradient;

            // With visual styles enabled, the native ProgressBar ignores
            // ForeColor/BackColor entirely and always shows the system
            // green bar. SetWindowTheme("", "") opts just this control
            // out of visual styles so the custom colors actually take
            // effect.
            HandleCreated += (s, e) => SetWindowTheme(Handle, "", "");
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
            // variant, or just BackColor for a *Transparent one)
            // directly on the window dc in one single pass avoids that
            // entirely - there's no separate erase step to leave a
            // mismatched trace behind.
            Color edgeColor = _drawBorder ? UIColors.BorderMedium : BackColor;

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
