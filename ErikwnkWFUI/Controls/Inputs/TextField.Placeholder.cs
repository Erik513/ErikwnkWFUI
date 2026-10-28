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

        // Whether there is anything to paint over what the system painted: a button in the box, or a placeholder that is shown.
        private bool HasPaintOver
        {
            get { return _innerGlyph != null || (_placeholder.Length > 0 && TextLength == 0 && !Focused); }
        }

        // What is painted over what the system painted: the placeholder, and the button in the box.
        private void PaintOver(Graphics graphics)
        {
            PaintPlaceholder(graphics);
            PaintInnerButton(graphics);
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

            return new Rectangle(left, top, Math.Max(1, client.Width - InnerWidth - left), Math.Max(1, client.Height - top));
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
