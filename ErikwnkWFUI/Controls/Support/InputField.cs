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
    /// background color, the dimmed one when disabled, and the color of the frame around the field. The frame of a field
    /// whose border is part of its window (the text box and the numeric up/down with a border) is painted over the
    /// system's in <see cref="AttachFrame"/>; a text box also gets its line moved to the middle there (the system puts it at
    /// the top edge), by making the client area start lower and painting that strip in the color of the field.
    /// The combo box draws its own frame and only takes the background from here.
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
        /// theme, where a dark field would be a heavy block). Dark enough on a dark theme that the text Windows draws in
        /// a disabled field (its gray, 109) keeps a contrast of 3:1.
        /// </summary>
        public static Color GetDisabledBackground(Color enabled)
        {
            return UIColors.Darken(enabled, UIColors.IsLight(enabled) ? 0.06 : 0.30);
        }

        /// <summary>
        /// The background of a text box that is read-only: set apart from an editable one, but less than a disabled one (half as far): the text
        /// can be read, selected and copied, only not changed.
        /// </summary>
        public static Color GetReadOnlyBackground(Color enabled)
        {
            return UIColors.Darken(enabled, UIColors.IsLight(enabled) ? 0.03 : 0.15);
        }

        /// <summary>
        /// Keeps the background of the field the one of its state: the field background when enabled, the dimmed one
        /// when disabled (a text box that is read-only has a slightly dimmed one), taken anew on a change of the theme. A color set by
        /// hand stays.
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

            TextBoxBase textBox = control as TextBoxBase;

            if (textBox != null)
                textBox.ReadOnlyChanged += (sender, e) => refresh();

            ThemeWatcher.Attach(control, refresh);
        }

        private static Color Resolve(Control control)
        {
            if (!control.Enabled)
                return GetDisabledBackground(Background);

            TextBoxBase textBox = control as TextBoxBase;

            return textBox != null && textBox.ReadOnly ? GetReadOnlyBackground(Background) : Background;
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
        /// How far the single line of a text box is moved down to stand in the middle of the box. The system puts the line
        /// at the top edge, so with the usual small fonts it sits two pixels too high (measured for 8 to 24 points: 2 up to
        /// a line height of 16, 1 up to 22, then none; with very large fonts the line is slightly too low, which an inset
        /// cannot correct).
        /// </summary>
        public static int GetTextTopInset(System.Windows.Forms.TextBox textBox)
        {
            // (Without a border the box is as high as its line: nothing to center in.)
            if (textBox.Multiline || textBox.BorderStyle == BorderStyle.None)
                return 0;

            int lineHeight = textBox.Font.Height;

            return lineHeight <= 16 ? 2 : lineHeight <= 22 ? 1 : 0;
        }

        /// <summary>
        /// The same for any control whose border is part of its window: <paramref name="hasBorder"/> says whether it has
        /// one now, <paramref name="color"/> gives the color of the frame in the current state. The border is part of the window outside its
        /// client area, so it is painted over after the system has drawn it. Returns what paints the frame again (after the color has changed).
        /// </summary>
        public static Action AttachFrame(Control control, Func<bool> hasBorder, Func<Color> color, Func<int> topInset = null)
        {
            FrameWindow frame = new FrameWindow(control, hasBorder, color, topInset ?? (() => 0));

            control.HandleCreated += (sender, e) => frame.AssignHandle(control.Handle);
            control.HandleDestroyed += (sender, e) => frame.ReleaseHandle();
            control.Enter += (sender, e) => frame.Redraw();
            control.Leave += (sender, e) => frame.Redraw();
            control.GotFocus += (sender, e) => frame.Redraw();
            control.LostFocus += (sender, e) => frame.Redraw();
            control.EnabledChanged += (sender, e) => frame.Redraw();
            control.BackColorChanged += (sender, e) => frame.Redraw();
            control.ParentChanged += (sender, e) => frame.Redraw();
            control.FontChanged += (sender, e) => frame.Recalculate();
            ThemeWatcher.Attach(control, frame.Redraw);

            if (control.IsHandleCreated)
                frame.AssignHandle(control.Handle);

            return frame.Redraw;
        }

        private sealed class FrameWindow : NativeWindow
        {

            private readonly Control _control;
            private readonly Func<bool> _hasBorder;
            private readonly Func<Color> _color;
            private readonly Func<int> _topInset;

            public FrameWindow(Control control, Func<bool> hasBorder, Func<Color> color, Func<int> topInset)
            {
                _control = control;
                _hasBorder = hasBorder;
                _color = color;
                _topInset = topInset;
            }

            protected override void OnHandleChange()
            {
                base.OnHandleChange();

                // The window was made before this took it over: its client area is measured again with the inset.
                if (Handle != IntPtr.Zero)
                    Recalculate();
            }

            public void Redraw()
            {
                if (Handle != IntPtr.Zero)
                    RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE);
            }

            // The client area is measured anew (the inset depends on the font).
            public void Recalculate()
            {
                if (Handle != IntPtr.Zero)
                    SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED);
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == WM_NCCALCSIZE)
                {
                    Inset(m);
                }
                else if (m.Msg == WM_NCPAINT || m.Msg == WM_PAINT)
                {
                    // Also after the paint of the client area: the system draws a line of its own in the strip that the inset makes, and not
                    // only with the paint of the frame - on the screen it was there again after the client area had been painted.
                    IntPtr hdc = GetWindowDC(Handle);

                    if (hdc == IntPtr.Zero)
                        return;

                    try
                    {
                        PaintNonClient(hdc);
                    }
                    finally
                    {
                        ReleaseDC(Handle, hdc);
                    }
                }
                else if (m.Msg == WM_PRINT && ((long)m.LParam & PRF_NONCLIENT) != 0)
                {
                    PaintNonClient(m.WParam);
                }
            }

            // The client area the system worked out (the first rectangle of the parameters, or the only one) starts lower.
            private void Inset(Message m)
            {
                int inset = _topInset();

                if (inset <= 0 || m.LParam == IntPtr.Zero)
                    return;

                NativeRect client = (NativeRect)System.Runtime.InteropServices.Marshal.PtrToStructure(m.LParam, typeof(NativeRect));

                if (client.Bottom - client.Top <= inset + 4)
                    return;

                client.Top += inset;
                System.Runtime.InteropServices.Marshal.StructureToPtr(client, m.LParam, false);
            }

            // What the system leaves to the window around its client area: the strip made by the inset in the color of the
            // field, and the frame.
            private void PaintNonClient(IntPtr hdc)
            {
                NativeRect window;
                NativeRect client;

                if (!GetWindowRect(Handle, out window) || !GetClientRect(Handle, out client))
                    return;

                int width = window.Right - window.Left;
                int height = window.Bottom - window.Top;
                int inset = _topInset();
                bool hasBorder = _hasBorder();
                int border = hasBorder ? Math.Max(0, (width - (client.Right - client.Left)) / 2) : 0;

                using (Graphics g = Graphics.FromHdc(hdc))
                {
                    if (inset > 0)
                    {
                        // One row more than the inset: the system draws a line of its own at the lower edge of the area it
                        // thinks is its border, which would show as a second line under the frame.
                        using (SolidBrush back = new SolidBrush(_control.BackColor))
                            g.FillRectangle(back, border, border, width - 2 * border, inset + 1);
                    }

                    if (hasBorder)
                    {
                        using (Pen pen = new Pen(_color()))
                            g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                    }
                }
            }
        }
    }
}
