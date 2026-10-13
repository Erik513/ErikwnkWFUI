using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Native;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The windows of the system behind the box: the open list (its handle, its frame), the messages of the box itself and its text field.
    public partial class ComboBox
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Even flat, Windows paints a themed focus and hover border in the system's accent
            // around the box; it comes from the visual-styles engine and ignores every color
            // property. Detaching the control from its theme class stops it. The list that
            // opens is a window of its own (its handle comes from GetComboBoxInfo) with the
            // same themed border, which is painted over below; comctl32 creates it with the
            // box, so it is there already.
            // Done on every new handle, not once: the handle can be recreated.
            SetWindowTheme(Handle, "", "");

            NativeComboBoxInfo info = new NativeComboBoxInfo { cbSize = Marshal.SizeOf(typeof(NativeComboBoxInfo)) };

            if (GetComboBoxInfo(Handle, ref info) && info.hwndList != IntPtr.Zero)
            {
                // The list's own frame is the system's (blue in the dark style); the box's
                // border color is drawn over it. A Simple box shows its list all the time as
                // part of the box, which keeps the system's drawing.
                if (DropDownStyle != ComboBoxStyle.Simple)
                    _listFrame.AssignHandle(info.hwndList);
            }
        }

        private IntPtr GetListHandle()
        {
            NativeComboBoxInfo info = new NativeComboBoxInfo { cbSize = Marshal.SizeOf(typeof(NativeComboBoxInfo)) };

            return GetComboBoxInfo(Handle, ref info) ? info.hwndList : IntPtr.Zero;
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _listFrame.ReleaseHandle();
            base.OnHandleDestroyed(e);
        }

        // Watches the window of the open list and paints its outer edge after the system did.
        private sealed class ListFrame : NativeWindow
        {
            private readonly ComboBox _owner;

            public ListFrame(ComboBox owner)
            {
                _owner = owner;
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg != WM_NCPAINT)
                    return;

                NativeRect rect;
                IntPtr hdc = GetWindowDC(Handle);

                if (hdc == IntPtr.Zero || !GetWindowRect(Handle, out rect))
                    return;

                try
                {
                    using (Graphics g = Graphics.FromHdc(hdc))
                    using (Pen pen = new Pen(_owner.ListBorderColor))
                        g.DrawRectangle(pen, 0, 0, rect.Right - rect.Left - 1, rect.Bottom - rect.Top - 1);
                }
                finally
                {
                    ReleaseDC(Handle, hdc);
                }
            }
        }

        // The list's frame has the color the box shows while the list is open.
        internal Color ListBorderColor
        {
            get
            {
                if (!Enabled)
                    return UIColors.DisabledGray;

                return UIColors.EnsureContrast(GetColors().Pressed, BackColor, 6.0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (OwnsClosedPainting || OwnsEditableFramePainting)
            {
                if (m.Msg == WM_ERASEBKGND)
                {
                    m.Result = (IntPtr)1;

                    return;
                }

                if (m.Msg == WM_PAINT)
                {
                    NativePaintStruct paint;
                    IntPtr hdc = BeginPaint(Handle, out paint);

                    try
                    {
                        PaintBuffered(hdc);
                    }
                    finally
                    {
                        EndPaint(Handle, ref paint);
                    }

                    return;
                }

                // DrawToBitmap and printing ask the control to draw itself into a given DC.
                if (m.Msg == WM_PRINTCLIENT || (m.Msg == WM_PRINT && ((long)m.LParam & PRF_CLIENT) != 0))
                {
                    PaintBuffered(m.WParam);

                    return;
                }
            }

            // The text field of a disabled box asks for its colors as a "static" control, which
            // the system answers with white; it gets the box's colors with muted text instead.
            if (m.Msg == WM_CTLCOLORSTATIC && OwnsEditableFramePainting)
            {
                m.Msg = WM_CTLCOLOREDIT;
                base.WndProc(ref m);
                SetTextColor(m.WParam, ColorTranslator.ToWin32(Enabled ? ForeColor : UIColors.GetDisabledText(BackColor)));

                return;
            }

            base.WndProc(ref m);
        }

        private Rectangle GetTextFieldBounds()
        {
            NativeComboBoxInfo info = new NativeComboBoxInfo { cbSize = Marshal.SizeOf(typeof(NativeComboBoxInfo)) };

            if (!GetComboBoxInfo(Handle, ref info) || info.hwndEdit == IntPtr.Zero)
                return Rectangle.Empty;

            return Rectangle.FromLTRB(info.rcItem.Left, info.rcItem.Top, info.rcItem.Right, info.rcItem.Bottom);
        }
    }
}
