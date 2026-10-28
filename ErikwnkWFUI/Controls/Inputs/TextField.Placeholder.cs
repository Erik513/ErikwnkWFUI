using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // The placeholder, and what is painted over what the system painted (the placeholder and the button in the box).
    public partial class TextField
    {
        private string _placeholder = "";

        // True while the standard box handles a message: what it asks the PlaceholderText for it must not get (.NET 5 and later, see there).
        private bool _baseIsPainting;

        /// <summary>
        /// A hint that is shown in the muted color while the box is empty and has not the focus (as the placeholder of a web page); it is
        /// painted over the box and never is its <see cref="TextBoxBase.Text"/>. Empty (the default) is none.
        /// </summary>
        public string Placeholder
        {
            get { return _placeholder; }
            set
            {
                _placeholder = value ?? "";
                Invalidate();
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// The <c>PlaceholderText</c> of the standard box (.NET 5 and later) is the <see cref="Placeholder"/> of this one: code that sets it gets the
        /// hint of the library (in the colors of the theme, where a text would stand) instead of the system's, which is drawn in a gray of its
        /// own.
        /// </summary>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override string PlaceholderText
        {
            // (While the window procedure of the standard box runs it is empty: that box paints a placeholder of its own as soon as this
            // property says there is one - two texts one over the other. The library paints it itself, see PaintPlaceholder.)
            get { return _baseIsPainting ? string.Empty : _placeholder; }
            set { Placeholder = value; }
        }
#endif

        // Whether there is anything to paint over what the system painted: a button in the box, a placeholder that is shown, or the edge at
        // the right of a text.
        private bool HasPaintOver
        {
            get { return _innerGlyph != null || (_placeholder.Length > 0 && TextLength == 0 && !Focused) || HasRightEdge; }
        }

        // What is painted over what the system painted: the edge at the right, the placeholder, and the button in the box.
        private void PaintOver(Graphics graphics)
        {
            PaintRightEdge(graphics);
            PaintPlaceholder(graphics);
            PaintInnerButton(graphics);
        }

        // A single line that is longer than the box is drawn by the system right up to the frame (the margin at the right keeps the caret and
        // the scrolling away from it, not the letters): the last pixels at the right are painted in the color of the box, so that the text
        // ends before the frame - with as much room between it and the frame as there is at the left - and not under the button in the box.
        private bool HasRightEdge
        {
            get { return !Multiline && BorderStyle != BorderStyle.None && TextLength > 0; }
        }

        // The system does not paint a single line only in answer to WM_PAINT: after a click, a key, a change of the selection or of the focus it
        // paints the line (and the background around it) at once, over the symbol in the box and over the edge at the right. Those messages ask
        // for a paint of the right end of the box, which paints what belongs there again.
        private static readonly int[] DirectPaintMessages =
        {
            0x0007, 0x0008, 0x000C, 0x0100, 0x0102, 0x0201, 0x0202, 0x0203, 0x00B1, 0x0300, 0x0301, 0x0302, 0x0303, 0x0304
        };

        private static bool PaintsDirectly(Message m)
        {
            // (WM_SETFOCUS, WM_KILLFOCUS, WM_SETTEXT, WM_KEYDOWN, WM_CHAR, the left button down, up and double click, EM_SETSEL, and cut, copy,
            // paste, clear, undo; a mouse move only while the left button is down: the selection is dragged.)
            if (m.Msg == 0x0200)
                return ((long)m.WParam & 0x0001) != 0;

            return Array.IndexOf(DirectPaintMessages, m.Msg) >= 0;
        }

        private void InvalidateOver()
        {
            if (!IsHandleCreated || (_innerGlyph == null && !HasRightEdge))
                return;

            int width = Math.Max(InnerWidth, Dpi.Scale(this, RightEdge96));
            Rectangle client = ClientRectangle;

            Invalidate(new Rectangle(client.Right - width, client.Top, width, client.Height));
        }

        private void PaintRightEdge(Graphics graphics)
        {
            if (!HasRightEdge)
                return;

            int width = Math.Max(InnerWidth, Dpi.Scale(this, RightEdge96));
            Rectangle client = ClientRectangle;

            using (SolidBrush brush = new SolidBrush(BackColor))
                graphics.FillRectangle(brush, client.Right - width, client.Top, width, client.Height);
        }

        private void PaintPlaceholder(Graphics graphics)
        {
            if (_placeholder.Length == 0 || TextLength > 0 || Focused || !IsHandleCreated)
                return;

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding
                | (Multiline ? TextFormatFlags.WordBreak : TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

            // Quieter than a text, but readable: at least 4.5:1 on the field (the muted text color of the theme was 2.9:1 on the dark one).
            TextRenderer.DrawText(graphics, _placeholder, Font, GetPlaceholderArea(), UIColors.GetMutedText(BackColor, 0.45, 4.5), flags);
        }

        // Where the system draws the text of the box: its formatting rectangle.
        private Rectangle GetTextArea()
        {
            RECT format = new RECT();
            SendMessage(Handle, EM_GETRECT, IntPtr.Zero, ref format);

            return new Rectangle(format.Left, format.Top, Math.Max(1, format.Right - format.Left), Math.Max(1, format.Bottom - format.Top));
        }

        // Where a text would stand: the formatting rectangle (a single line is also moved down by the inset of the frame), so that the
        // placeholder stands exactly where a text would - and not under the button in the box. (It once was moved two pixels to the left for a
        // single line, which put its first letter on the frame: the picture of the test did not look at the two columns at the left.)
        private Rectangle GetPlaceholderArea()
        {
            RECT format = new RECT();
            SendMessage(Handle, EM_GETRECT, IntPtr.Zero, ref format);

            int left = format.Left;
            int top = format.Top + (Multiline ? 0 : InputField.GetTextTopInset(this));
            Rectangle client = ClientRectangle;

            // (The formatting rectangle ends where the margin at the right begins: not under the button in the box, nor at the frame.)
            return new Rectangle(left, top, Math.Max(1, format.Right - left), Math.Max(1, client.Height - top));
        }

        private const int EM_GETRECT = 0x00B2;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, ref RECT rect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
    }
}
