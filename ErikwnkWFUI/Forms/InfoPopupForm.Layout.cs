using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Forms
{
    // Measuring the blocks of the content (header and text), the size of the popup and the painting of it.
    public partial class InfoPopupForm
    {

        // ---- measuring and painting ----

        private int Scale(int value96)
        {
            return (int)Math.Round(value96 * DeviceDpi / 96.0);
        }


        private bool IsFixed
        {
            get { return !_fixedSize.IsEmpty; }
        }


        // The width the text may take: the maximum width, or in the fixed size what is left.
        private int GetTextWidth()
        {
            return IsFixed
                ? Math.Max(1, _fixedSize.Width - 2 * Scale(FixedPadding96))
                : Scale(MaxTextWidth96);
        }


        // The size of one block's header and text, wrapped at the text width.
        private void MeasureBlock(InfoPopupSection section, int textWidth, out Size header, out Size text)
        {
            header = Size.Empty;
            text = Size.Empty;

            if (section.Header != null)
                header = TextRenderer.MeasureText(section.Header, UIFonts.Title, new Size(textWidth, int.MaxValue), MeasureFlags);

            if (section.Text != null)
                text = TextRenderer.MeasureText(section.Text, UIFonts.Normal, new Size(textWidth, int.MaxValue), MeasureFlags);
        }


        // Where each block's header and text lie, relative to the content, and the size of all of it. Measured when the
        // content, the size mode or the screen's scaling changes - not for every paint.
        private struct BlockLayout
        {
            public Rectangle Header;
            public Rectangle Text;
        }


        private BlockLayout[] _layout = new BlockLayout[0];

        private Size _contentSize;

        private int _layoutDpi;


        private void MeasureLayout()
        {
            int textWidth = GetTextWidth();
            BlockLayout[] blocks = new BlockLayout[_sections.Length];
            int width = 0;
            int y = 0;

            for (int i = 0; i < _sections.Length; i++)
            {
                Size header;
                Size text;
                MeasureBlock(_sections[i], textWidth, out header, out text);

                if (i > 0)
                    y += Scale(BlockGap96);

                if (header.Height > 0)
                {
                    blocks[i].Header = new Rectangle(0, y, textWidth, header.Height);
                    y += header.Height;
                }

                if (header.Height > 0 && text.Height > 0)
                    y += Scale(HeaderGap96);

                if (text.Height > 0)
                {
                    blocks[i].Text = new Rectangle(0, y, textWidth, text.Height);
                    y += text.Height;
                }

                width = Math.Max(width, Math.Max(header.Width, text.Width));
            }

            _layout = blocks;
            _contentSize = new Size(width, y);
            _layoutDpi = DeviceDpi;
        }


        // A borderless top-level form cannot go below the system's minimum tracking size
        // (about 136x39) unless MinimumSize is set: it is locked to the wanted size.
        private void UpdateSize()
        {
            // A form that is gone (disposed with its owner) cannot be resized any more.
            if (IsDisposed)
                return;

            MeasureLayout();

            Size client;

            if (IsFixed)
            {
                client = _fixedSize;
            }
            else
            {
                int padding = Scale(Padding96);
                client = new Size(_contentSize.Width + 2 * padding, _contentSize.Height + 2 * padding);
            }

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            ClientSize = client;
            MinimumSize = Size;
            MaximumSize = Size;

            // A popup that is up and changes size must still fit inside the window.
            if (Visible && _lastAnchor != null && !_lastAnchor.IsDisposed)
                Location = PopupSupport.KeepInside(Location, Size, PopupSupport.GetPlacementArea(_lastAnchor), Screen.FromControl(_lastAnchor).WorkingArea);

            ApplyRoundedCorners();
            Invalidate();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!HasContent)
                return;

            // The window moved to a screen with another scaling: measured again.
            if (_layoutDpi != DeviceDpi)
                MeasureLayout();

            int padding = IsFixed ? Scale(FixedPadding96) : Scale(Padding96);

            // In the fixed size the content sits in the middle, otherwise at the padding.
            int x = padding;
            int y = IsFixed ? Math.Max(0, (ClientSize.Height - _contentSize.Height) / 2) : padding;

            Color autoColor = GetForeColor();
            TextFormatFlags align = IsFixed ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left;
            TextFormatFlags flags = (MeasureFlags & ~TextFormatFlags.Left) | align;

            for (int i = 0; i < _sections.Length; i++)
            {
                InfoPopupSection section = _sections[i];
                BlockLayout block = _layout[i];

                if (!block.Header.IsEmpty)
                {
                    Rectangle header = new Rectangle(block.Header.X + x, block.Header.Y + y, block.Header.Width, block.Header.Height);
                    TextRenderer.DrawText(e.Graphics, section.Header, UIFonts.Title, header, section.HeaderColor.IsEmpty ? autoColor : section.HeaderColor, flags);
                }

                if (!block.Text.IsEmpty)
                {
                    Rectangle text = new Rectangle(block.Text.X + x, block.Text.Y + y, block.Text.Width, block.Text.Height);
                    TextRenderer.DrawText(e.Graphics, section.Text, UIFonts.Normal, text, section.TextColor.IsEmpty ? autoColor : section.TextColor, flags);
                }
            }
        }
    }
}
