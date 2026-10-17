using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The text next to the ring ("Loading data...").
    public partial class Spinner
    {
        private const TextFormatFlags TextFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
            | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;

        private const float MinimumDigitSize = 7f;  // smaller digits in the ring are not legible, so they are left out

        private SpinnerTextPosition _textPosition = SpinnerTextPosition.Right;

        // The font of the digits in the ring, worked out once for the size of the ring (null: too small for legible digits).
        private Font _digitFont;
        private float _digitInner = -1f;
        private string _digitFamily;

        /// <summary>
        /// A text next to the ring (empty by default: only the ring). Make the control as wide as ring and text need - the ring
        /// is a circle as high as the control (with the text left or right of it) or as large as the room left by the text
        /// (above or below it), and a text that does not fit is cut with an ellipsis. A screen reader reads it as the name.
        /// </summary>
        [Browsable(true)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        [DefaultValue("")]
        public override string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        /// <summary>Where the <see cref="Text"/> stands in relation to the ring (right of it by default).</summary>
        [DefaultValue(SpinnerTextPosition.Right)]
        public SpinnerTextPosition TextPosition
        {
            get { return _textPosition; }
            set
            {
                if (value == _textPosition)
                    return;

                _textPosition = value;
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
            AccessibilityNotifyClients(AccessibleEvents.NameChange, -1);
        }

        // The area the ring is drawn in (it is centred in it) and the place of the text; the whole control when there is no text.
        private Rectangle GetLayout(out Rectangle textArea, out TextFormatFlags alignment)
        {
            textArea = Rectangle.Empty;
            alignment = TextFormatFlags.Left;

            if (string.IsNullOrWhiteSpace(Text))
                return ClientRectangle;

            int gap = Math.Max(4, Font.Height / 2);
            int side = Math.Min(Width, Height);

            switch (_textPosition)
            {
                case SpinnerTextPosition.Left:
                    alignment = TextFormatFlags.Right;
                    textArea = new Rectangle(0, 0, Width - side - gap, Height);

                    return new Rectangle(Width - side, 0, side, Height);

                case SpinnerTextPosition.Above:
                    alignment = TextFormatFlags.HorizontalCenter;
                    textArea = new Rectangle(0, 0, Width, Font.Height);

                    return new Rectangle(0, Font.Height + gap, Width, Height - Font.Height - gap);

                case SpinnerTextPosition.Below:
                    alignment = TextFormatFlags.HorizontalCenter;
                    textArea = new Rectangle(0, Height - Font.Height, Width, Font.Height);

                    return new Rectangle(0, 0, Width, Height - Font.Height - gap);

                default:
                    textArea = new Rectangle(side + gap, 0, Width - side - gap, Height);

                    return new Rectangle(0, 0, side, Height);
            }
        }

        private void DrawText(Graphics g, Rectangle area, TextFormatFlags alignment)
        {
            if (area.Width <= 0 || area.Height <= 0)
                return;

            TextRenderer.DrawText(g, Text, Font, area, GetTextColor(), TextFlags | alignment);
        }

        // The digits sit inside the ring stroke; the widest they may get is the inner diameter. The size is worked out for "100"
        // whatever the value is, so the digits do not change their size while the number counts up; where that size is not legible
        // (a small ring), they are left out.
        private void DrawProgressText(Graphics g, RectangleF ring, float stroke, string text)
        {
            float inner = ring.Width - stroke * 2f - 2f;
            if (inner <= 3f)
                return;

            Font font = GetDigitFont(g, inner);
            if (font == null)
                return;

            using (SolidBrush brush = new SolidBrush(GetTextColor()))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString(text, font, brush, ring, sf);
            }
        }

        // Measuring the font down until "100" fits takes a few fonts: it is done again only when the ring or the font family changes,
        // not on every picture of the turning arc.
        private Font GetDigitFont(Graphics g, float inner)
        {
            if (Math.Abs(inner - _digitInner) < 0.01f && _digitFamily == Font.FontFamily.Name)
                return _digitFont;

            _digitFont?.Dispose();
            _digitFont = null;
            _digitInner = inner;
            _digitFamily = Font.FontFamily.Name;

            float size = inner * 0.7f;
            for (int i = 0; i < 8; i++)
            {
                Font font = new Font(Font.FontFamily, size, FontStyle.Bold, GraphicsUnit.Pixel);
                SizeF measured = g.MeasureString("100", font);

                if (measured.Width <= inner && measured.Height <= inner)
                {
                    if (size >= MinimumDigitSize)
                        _digitFont = font;
                    else
                        font.Dispose();

                    return _digitFont;
                }

                font.Dispose();
                size *= 0.85f;
            }

            return null;
        }
    }
}
