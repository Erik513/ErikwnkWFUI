using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Forms
{
    // The size of the toast and its place in the window: the scaling of the screen, the height for the message, the position.
    public partial class ToastForm
    {

        // The scaling of the screen a window is on. Replaceable so a test can give another one.
        internal static Func<Form, int> OwnerDpi = owner => owner.DeviceDpi;

        // The size of the toast and the places inside it follow the screen's scaling: a toast on a
        // screen at 150% is 1.5 times as large, so its text keeps its room. The window it is
        // shown for says which screen that is.
        internal void ApplyDpi(int dpi)
        {
            if (IsDisposed)
                return;

            _dpi = dpi;
            _contentPanel.Padding = new Padding(
                Dpi.Scale(ContentPaddingHorizontal96, dpi), Dpi.Scale(ContentPaddingVertical96, dpi),
                Dpi.Scale(ContentPaddingHorizontal96, dpi), Dpi.Scale(ContentPaddingVertical96, dpi));
            _iconLabel.Size = new Size(Dpi.Scale(IconSize96, dpi), Dpi.Scale(IconSize96, dpi));
            _closeLabel.Size = new Size(Dpi.Scale(CloseSpace96, dpi), Dpi.Scale(CloseSpace96, dpi));
            ApplyTextPadding();
            FitToMessage();
        }

        // The toast is as wide as always and as high as its message needs, from one line (the
        // usual height) to a few lines; what is more than that ends in an ellipsis. The symbol
        // sits in the middle of the height.
        private void FitToMessage()
        {
            int width = Dpi.Scale(ToastWidth96, _dpi);
            int textWidth = width - _contentPanel.Padding.Horizontal - _messageLabel.Padding.Horizontal;
            int lineHeight = TextRenderer.MeasureText("Ag", _messageLabel.Font, Size.Empty, TextFormatFlags.NoPadding).Height;
            Size measured = TextRenderer.MeasureText(
                _messageLabel.Text,
                _messageLabel.Font,
                new Size(Math.Max(1, textWidth), int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            // The title is one line, with a little room to the message below it.
            int titleHeight = 0;

            if (_content.Header != null)
            {
                titleHeight = TextRenderer.MeasureText("Ag", _titleLabel.Font, Size.Empty, TextFormatFlags.NoPadding).Height
                    + Dpi.Scale(TitleGap96, _dpi);
                _titleLabel.Height = titleHeight;
            }

            int textHeight = titleHeight + Math.Min(measured.Height, lineHeight * MaxMessageLines);
            int height = Math.Max(Dpi.Scale(ToastHeight96, _dpi), textHeight + 2 * Dpi.Scale(MinVerticalMargin96, _dpi));

            Size = new Size(width, height);

            // The room above and below the text is what is left of the height, less a little
            // slack (the label's lines are a bit higher than the measure), so the message shows
            // no more lines than its limit.
            int slack = string.IsNullOrEmpty(_messageLabel.Text) ? 0 : Dpi.Scale(LineSlack96, _dpi);
            int vertical = Math.Max(0, (height - textHeight - slack) / 2);
            _contentPanel.Padding = new Padding(_contentPanel.Padding.Left, vertical, _contentPanel.Padding.Right, vertical);
            _iconLabel.Location = new Point(Dpi.Scale(IconLeft96, _dpi), (height - _iconLabel.Height) / 2);
            _closeLabel.Location = new Point(width - _closeLabel.Width - Dpi.Scale(CloseInset96, _dpi), Dpi.Scale(CloseInset96, _dpi));

            // The picture of the window is made again (the controls in it are not the window itself, see PopupFormBase).
            Invalidate();
        }

        // The part of the window the toast is placed in: its inside (without the title bar and
        // the frame), as far as the screen shows it.
        private static Rectangle GetClientArea(Form owner)
        {
            Rectangle client = owner.RectangleToScreen(owner.ClientRectangle);
            Rectangle visible = Rectangle.Intersect(client, Screen.FromControl(owner).WorkingArea);

            return visible.IsEmpty ? client : visible;
        }

        // The top left of a toast of this size in the area. The edges and corners keep
        // <paramref name="edgeDistance"/> from the edge; BelowCenter and AboveCenter sit at three
        // and one quarter of the height.
        internal static Point GetToastLocation(Rectangle area, Size size, ToastPosition position, int edgeDistance)
        {
            int centerX = area.Left + (area.Width - size.Width) / 2;
            int left = area.Left + edgeDistance;
            int right = area.Right - edgeDistance - size.Width;
            int top = area.Top + edgeDistance;
            int bottom = area.Bottom - edgeDistance - size.Height;

            switch (position)
            {
                case ToastPosition.Center:
                    return new Point(centerX, area.Top + (area.Height - size.Height) / 2);
                case ToastPosition.AboveCenter:
                    return new Point(centerX, area.Top + (int)(area.Height * 0.25) - size.Height / 2);
                case ToastPosition.TopCenter:
                    return new Point(centerX, top);
                case ToastPosition.BottomCenter:
                    return new Point(centerX, bottom);
                case ToastPosition.TopLeft:
                    return new Point(left, top);
                case ToastPosition.TopRight:
                    return new Point(right, top);
                case ToastPosition.BottomLeft:
                    return new Point(left, bottom);
                case ToastPosition.BottomRight:
                    return new Point(right, bottom);
                default: // BelowCenter
                    return new Point(centerX, area.Top + (int)(area.Height * 0.75) - size.Height / 2);
            }
        }
    }
}
