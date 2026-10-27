using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The picture of the moving text and how it is painted.
    public sealed partial class ScrollingLabel
    {

        // ---- painting the moving text ----

        // The picture of the text from the place it has moved to, shown only inside the label (the padding
        // stays free). The place may lie between two pixels; the picture is then mixed between them. It is
        // also where a changed UseMnemonic is noticed, because the label is not told about it.
        private void PaintMovingText(Graphics graphics)
        {
            Rectangle inner = new Rectangle(
                Padding.Left,
                Padding.Top,
                Math.Max(0, ClientSize.Width - Padding.Horizontal),
                Math.Max(0, ClientSize.Height - Padding.Vertical));

            // UseMnemonic cannot tell the label when it changes: it is seen here. With or without the
            // ampersand the text has another width (measure again), only the underline: another picture.
            HotkeyPrefix prefix = GetHotkeyPrefix();

            if ((prefix == HotkeyPrefix.None) != (_measuredPrefix == HotkeyPrefix.None))
                Restart();
            else if (prefix != _pictureHotkeyPrefix)
                DisposePicture();

            if (_maxOffset <= 0 && _cycle <= 0)
            {
                // The new measure says the text fits: nothing to move.
                base.OnPaint(new PaintEventArgs(graphics, ClientRectangle));

                return;
            }

            Bitmap picture = GetPicture();
            int y = GetPictureTop(inner, picture.Height);

            // Back and forth: the picture moves to the left as far as it has gone. Endless: round after
            // round, to the left or to the right, so the picture is put again and again, one round apart,
            // as often as the label needs to be covered.
            float x = inner.Left + _textInset - _offset;

            if (_cycle > 0 && _scrollDirection == LabelScrollDirection.Right)
                x = inner.Left + _textInset + _offset - _cycle;

            GraphicsState state = graphics.Save();

            try
            {
                graphics.IntersectClip(inner);
                graphics.InterpolationMode = InterpolationMode.Bilinear;
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.DrawImage(
                    picture,
                    new RectangleF(x, y, picture.Width, picture.Height),
                    new RectangleF(0, 0, picture.Width, picture.Height),
                    GraphicsUnit.Pixel);

                if (_cycle > 0)
                {
                    for (float next = x + _cycle; next < inner.Right; next += _cycle)
                    {
                        graphics.DrawImage(
                            picture,
                            new RectangleF(next, y, picture.Width, picture.Height),
                            new RectangleF(0, 0, picture.Width, picture.Height),
                            GraphicsUnit.Pixel);
                    }
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        // The room the screen font puts before the text (a few pixels, as much as the text of a label that
        // does not move has), so that the text does not jump when the label starts to move.
        private int GetTextInset()
        {
            Size big = new Size(int.MaxValue, int.MaxValue);
            int padded = TextRenderer.MeasureText("A", Font, big, TextFormatFlags.Default).Width;
            int bare = TextRenderer.MeasureText("A", Font, big, TextFormatFlags.NoPadding).Width;

            return Math.Max(0, (padded - bare) / 2);
        }

        // The top of the picture: at the top, at the bottom or in the middle of the room, as TextAlign says.
        private int GetPictureTop(Rectangle inner, int height)
        {
            switch (GetVerticalFlag(TextAlign))
            {
                case TextFormatFlags.Top:
                    return inner.Top;
                case TextFormatFlags.Bottom:
                    return inner.Bottom - height;
                default:
                    return inner.Top + (inner.Height - height) / 2;
            }
        }

        // The picture of the text in the color it has now.
        private Bitmap GetPicture()
        {
            Color color = ForeColor;

            if (_picture != null && _pictureColor == color)
                return _picture;

            DisposePicture();

            // The endless picture is one round: the text, a gap, the separator and a gap.
            int width = _cycle > 0 ? _cycle : _textWidth + EndRoom;
            Bitmap picture = new Bitmap(Math.Max(1, width), Math.Max(1, (int)Math.Ceiling(Font.GetHeight())) + 2, PixelFormat.Format32bppPArgb);

            using (Graphics graphics = Graphics.FromImage(picture))
            using (SolidBrush brush = new SolidBrush(color))
            using (StringFormat format = CreateMovingTextFormat())
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.Clear(Color.Transparent);

                if (_cycle <= 0)
                {
                    graphics.DrawString(Text, Font, brush, new PointF(0f, 1f), format);
                }
                else
                {
                    // Each text is followed by the same room, the separator in the middle of it (a gap on both
                    // sides when the round is as short as it can be, more when the label is wide).
                    string[] texts = _pieceWidths != null ? _texts : new[] { Text };
                    int[] widths = _pieceWidths ?? new[] { _textWidth };
                    float room = (_cycle - _textWidth) / (float)texts.Length;
                    float left = 0f;

                    for (int i = 0; i < texts.Length; i++)
                    {
                        graphics.DrawString(texts[i], Font, brush, new PointF(left, 1f), format);

                        if (_scrollSeparator != LabelScrollSeparator.None)
                            graphics.DrawString(_scrollSeparator.ToText(), Font, brush, new PointF(left + widths[i] + (room - _separatorWidth) / 2f, 1f), format);

                        left += widths[i] + room;
                    }
                }
            }

            _picture = picture;
            _pictureColor = color;
            _pictureHotkeyPrefix = GetHotkeyPrefix();

            return _picture;
        }

        private void DisposePicture()
        {
            if (_picture != null)
            {
                _picture.Dispose();
                _picture = null;
            }
        }

        // One line from the left, as it is, without the room GDI+ would add around it; what an ampersand is
        // follows UseMnemonic.
        private StringFormat CreateMovingTextFormat()
        {
            StringFormat format = new StringFormat(StringFormat.GenericTypographic);
            format.FormatFlags |= StringFormatFlags.NoWrap | StringFormatFlags.MeasureTrailingSpaces;
            format.HotkeyPrefix = GetHotkeyPrefix();

            return format;
        }

        // An ampersand is a sign of the text, or - with UseMnemonic - the mark of a key (underlined only
        // while the keyboard cues are shown).
        private HotkeyPrefix GetHotkeyPrefix()
        {
            return !UseMnemonic ? HotkeyPrefix.None : ShowKeyboardCues ? HotkeyPrefix.Show : HotkeyPrefix.Hide;
        }
    }
}
