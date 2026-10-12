using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Native;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// What the three input fields of the library (text box, combo box, numeric up/down) have in common: the one
    /// background color, the dimmed one when disabled, and the color of the frame around the field.
    /// </summary>
    internal static class InputField
    {
        /// <summary>The background of an input field in the current theme - one role for all of them.</summary>
        public static Color Background
        {
            get { return UIColors.BackgroundLight; }
        }

        /// <summary>
        /// The background of a disabled field: a recessed one, a little darker than the enabled field (less on a light
        /// theme, where a dark field would be a heavy block).
        /// </summary>
        public static Color GetDisabledBackground(Color enabled)
        {
            return UIColors.Darken(enabled, UIColors.IsLight(enabled) ? 0.06 : 0.25);
        }

        /// <summary>
        /// Keeps the background of the field the one of its state: the field background when enabled, the dimmed one
        /// when disabled, taken anew on a change of the theme. A color set by hand stays.
        /// </summary>
        public static void FollowBackground(Control control)
        {
            Color applied = Resolve(control);
            control.BackColor = applied;

            Action refresh = () =>
            {
                if (control.BackColor != applied)
                    return; // set by hand

                applied = Resolve(control);
                control.BackColor = applied;
            };

            control.EnabledChanged += (sender, e) => refresh();
            ThemeWatcher.Attach(control, refresh);
        }

        private static Color Resolve(Control control)
        {
            return control.Enabled ? Background : GetDisabledBackground(Background);
        }

        /// <summary>The color of the page a control stands on: the first opaque background of its parents.</summary>
        public static Color GetPageColor(Control control)
        {
            for (Control parent = control.Parent; parent != null; parent = parent.Parent)
            {
                if (parent.BackColor.A == 255)
                    return parent.BackColor;
            }

            return UIColors.BackgroundMedium;
        }

        /// <summary>
        /// The frame around a field: a quiet outline seen against the page, and with the focus the color of the color
        /// set (brought to where it is seen against the field) - or, without a set, the text color of the field.
        /// </summary>
        public static Color GetFrameColor(Control control, UIColorSet colors, bool focused)
        {
            if (!focused || !control.Enabled)
                return UIColors.GetOutlineColor(GetPageColor(control));

            Color source = colors != null ? colors.Hover : control.ForeColor;

            return UIColors.EnsureContrast(source, control.BackColor, colors != null ? 3.0 : 4.5);
        }

        /// <summary>
        /// Gives a text box with a border (<see cref="BorderStyle.FixedSingle"/>) the frame of the library instead of the
        /// system's. The border is part of the window outside its client area, so it is painted over after the system
        /// has drawn it.
        /// </summary>
        public static void FrameTextBox(TextBox textBox)
        {
            AttachFrame(textBox, () => textBox.BorderStyle == BorderStyle.FixedSingle, () => GetFrameColor(textBox, null, textBox.Focused));
        }

        /// <summary>
        /// The same for any control whose border is part of its window: <paramref name="hasBorder"/> says whether it has
        /// one now, <paramref name="color"/> gives the color of the frame in the current state.
        /// </summary>
        public static void AttachFrame(Control control, Func<bool> hasBorder, Func<Color> color)
        {
            FrameWindow frame = new FrameWindow(control, hasBorder, color);

            control.HandleCreated += (sender, e) => frame.AssignHandle(control.Handle);
            control.HandleDestroyed += (sender, e) => frame.ReleaseHandle();
            control.Enter += (sender, e) => frame.Redraw();
            control.Leave += (sender, e) => frame.Redraw();
            control.GotFocus += (sender, e) => frame.Redraw();
            control.LostFocus += (sender, e) => frame.Redraw();
            control.EnabledChanged += (sender, e) => frame.Redraw();
            control.BackColorChanged += (sender, e) => frame.Redraw();
            control.ParentChanged += (sender, e) => frame.Redraw();
            ThemeWatcher.Attach(control, frame.Redraw);

            if (control.IsHandleCreated)
                frame.AssignHandle(control.Handle);
        }

        private sealed class FrameWindow : NativeWindow
        {
            private const int WM_NCPAINT = 0x0085;
            private const int WM_PRINT = 0x0317;
            private const int PRF_NONCLIENT = 0x0002;
            private const uint RDW_INVALIDATE = 0x0001;
            private const uint RDW_FRAME = 0x0400;

            private readonly Control _control;
            private readonly Func<bool> _hasBorder;
            private readonly Func<Color> _color;

            public FrameWindow(Control control, Func<bool> hasBorder, Func<Color> color)
            {
                _control = control;
                _hasBorder = hasBorder;
                _color = color;
            }

            public void Redraw()
            {
                if (Handle != IntPtr.Zero)
                    RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE);
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (!_hasBorder())
                    return;

                if (m.Msg == WM_NCPAINT)
                {
                    IntPtr hdc = GetWindowDC(Handle);

                    if (hdc == IntPtr.Zero)
                        return;

                    try
                    {
                        PaintFrame(hdc);
                    }
                    finally
                    {
                        ReleaseDC(Handle, hdc);
                    }
                }
                else if (m.Msg == WM_PRINT && ((long)m.LParam & PRF_NONCLIENT) != 0)
                {
                    PaintFrame(m.WParam);
                }
            }

            private void PaintFrame(IntPtr hdc)
            {
                NativeRect window;

                if (!GetWindowRect(Handle, out window))
                    return;

                int width = window.Right - window.Left;
                int height = window.Bottom - window.Top;

                using (Graphics g = Graphics.FromHdc(hdc))
                using (Pen pen = new Pen(_color()))
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
            }
        }
    }
}
