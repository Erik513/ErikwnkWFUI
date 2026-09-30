using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // The themed drawing: page area, tabs, accent, content.
    public partial class ReadOnlyTabControl
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawHeaderBackground(e.Graphics);

            DrawPageArea(e.Graphics);

            // Tabs scrolled out of view keep their (off-strip) rectangles, so
            // drawing is limited to the stretch of the strip that shows tabs.
            e.Graphics.SetClip(GetVisibleStripBounds());

            // The selected tab goes last: it overlaps its neighbours by a
            // couple of pixels and has to stay on top.
            for (int i = 0; i < TabCount; i++)
            {
                if (i != SelectedIndex)
                {
                    DrawTab(e.Graphics, i);
                }
            }

            if (SelectedIndex >= 0 && SelectedIndex < TabCount)
            {
                DrawTab(e.Graphics, SelectedIndex);
            }

            e.Graphics.ResetClip();

            CoverOutsideVisibleStrip(e.Graphics);
        }

        // The page contents plus the border band around them - what the tab
        // strip leaves over. Its strip-side edge sits a couple of pixels
        // behind the tabs, which is what lets the selected tab overlap it.
        private Rectangle GetPageAreaBounds()
        {
            Rectangle area = DisplayRectangle;
            area.Inflate(NativePageBorder, NativePageBorder);

            Rectangle inset = ClientRectangle;
            inset.Inflate(-NativeStripMargin, -NativeStripMargin);

            return Rectangle.Intersect(inset, area);
        }

        // A transparent (or translucent) header lets the parent's own
        // background show through, drawn by the parent itself so images and
        // gradients come along - same approach ToggleSwitch uses.
        private void DrawHeaderBackground(Graphics graphics)
        {
            Color color = HeaderBackColor;

            if (color.A < 255 && Parent != null)
            {
                ParentBackground.Paint(this, graphics, args => InvokePaintBackground(Parent, args));
            }

            if (color.A > 0)
            {
                using (SolidBrush brush = new SolidBrush(color))
                {
                    graphics.FillRectangle(brush, ClientRectangle);
                }
            }
        }

        // The part of the client area tabs may be drawn in: inside the
        // strip's margin at the start, and stopping where the scroll arrows
        // begin at the end. A tab reaching past either end is cut off there
        // (name included), which is what shows there are more tabs.
        private Rectangle GetVisibleStripBounds()
        {
            Rectangle bounds = ClientRectangle;
            bounds.Inflate(-NativeStripMargin, -NativeStripMargin);

            Rectangle arrows = _scrollButtons.GetBounds();

            if (arrows.IsEmpty)
                return bounds;

            if (IsVertical())
                return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, Math.Min(bounds.Bottom, arrows.Top));

            return Rectangle.FromLTRB(bounds.Left, bounds.Top, Math.Min(bounds.Right, arrows.Left), bounds.Bottom);
        }

        // The strip along the tabs' side of the control, including the row
        // the selected tab reaches into.
        private Rectangle GetTabStripBounds()
        {
            Rectangle area = GetPageAreaBounds();
            Rectangle client = ClientRectangle;

            switch (Alignment)
            {
                case TabAlignment.Bottom:
                    return Rectangle.FromLTRB(client.Left, area.Bottom - 1, client.Right, client.Bottom);

                case TabAlignment.Left:
                    return Rectangle.FromLTRB(client.Left, client.Top, area.Left + 1, client.Bottom);

                case TabAlignment.Right:
                    return Rectangle.FromLTRB(area.Right - 1, client.Top, client.Right, client.Bottom);

                default:
                    return Rectangle.FromLTRB(client.Left, client.Top, client.Right, area.Top + 1);
            }
        }

        // Text can slip past the graphics clip (seen on screen: a cut-off
        // name in the margin beyond the scroll arrows). So whatever part of
        // the strip is neither tab area nor arrows is painted over with the
        // header background again.
        private void CoverOutsideVisibleStrip(Graphics graphics)
        {
            using (Region cover = new Region(GetTabStripBounds()))
            {
                cover.Exclude(GetVisibleStripBounds());
                cover.Exclude(_scrollButtons.GetBounds());

                graphics.SetClip(cover, System.Drawing.Drawing2D.CombineMode.Replace);
                DrawHeaderBackground(graphics);
                graphics.ResetClip();
            }
        }

        private void DrawPageArea(Graphics graphics)
        {
            Rectangle area = GetPageAreaBounds();

            if (area.Width <= 0 || area.Height <= 0)
                return;

            using (SolidBrush brush = new SolidBrush(PageBackColor))
            {
                graphics.FillRectangle(brush, area);
            }

            using (Pen pen = new Pen(BorderColor))
            {
                graphics.DrawRectangle(pen, area.X, area.Y, area.Width - 1, area.Height - 1);
            }
        }

        private void DrawTab(Graphics graphics, int index)
        {
            if (DrawMode == TabDrawMode.OwnerDrawFixed)
            {
                DrawTabByOwner(graphics, index);
                return;
            }

            bool selected = index == SelectedIndex;
            Rectangle bounds = GetTabRect(index);

            // Only the selected tab reaches over the page border.
            if (!selected)
            {
                bounds = TrimToPageEdge(bounds, 0);
            }

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            bool hovered = index == _hoveredTabIndex;
            TabPage page = TabPages[index];
            Rectangle outline = selected ? TrimToPageEdge(bounds, 1) : bounds;

            using (SolidBrush brush = new SolidBrush(GetTabBackColor(selected, hovered)))
            {
                graphics.FillRectangle(brush, outline);
            }

            DrawTabBorder(graphics, outline);

            if (selected)
            {
                DrawAccent(graphics, bounds);
            }

            Color foreColor = GetTabForeColor(selected, page.Enabled && Enabled);
            DrawTabContent(graphics, index, bounds, page, foreColor);
            DrawTabOverlay(graphics, index, bounds, foreColor);

            if (selected && Focused && ShowFocusCues)
            {
                Rectangle focus = Rectangle.Inflate(bounds, -3, -3);
                ControlPaint.DrawFocusRectangle(graphics, focus, GetTabForeColor(true, true), GetTabBackColor(true, false));
            }
        }

        // With DrawMode = OwnerDrawFixed the standard control leaves each tab
        // to the DrawItem event. This control draws itself, so it raises the
        // event itself, with the same arguments the native one would carry -
        // the colors are the themed ones for that tab's state.
        private void DrawTabByOwner(Graphics graphics, int index)
        {
            bool selected = index == SelectedIndex;
            bool hovered = index == _hoveredTabIndex;
            bool enabled = TabPages[index].Enabled && Enabled;
            DrawItemState state = DrawItemState.None;

            if (selected)
                state |= DrawItemState.Selected;

            if (hovered)
                state |= DrawItemState.HotLight;

            if (!enabled)
                state |= DrawItemState.Disabled;

            if (selected && Focused && ShowFocusCues)
                state |= DrawItemState.Focus;

            OnDrawItem(new DrawItemEventArgs(
                graphics,
                Font,
                GetTabRect(index),
                index,
                state,
                GetTabForeColor(selected, enabled),
                GetTabBackColor(selected, hovered)));
        }

        private Color GetTabBackColor(bool selected, bool hovered)
        {
            if (selected)
                return SelectedTabBackColor;

            return hovered ? HoverTabBackColor : TabBackColor;
        }

        private Color GetTabForeColor(bool selected, bool enabled)
        {
            if (!enabled)
                return DisabledTabForeColor;

            return selected ? SelectedTabForeColor : TabForeColor;
        }

        // Cuts a tab's bounds off at the page area's edge on the strip side.
        // "reach" is how far past that edge it may still go: the selected
        // tab reaches a few pixels behind the page border, but its outline
        // must end on the border line (reach 1) - carried on further it
        // pokes out as little stubs into the page.
        private Rectangle TrimToPageEdge(Rectangle bounds, int reach)
        {
            Rectangle area = GetPageAreaBounds();

            switch (Alignment)
            {
                case TabAlignment.Bottom:
                    return Rectangle.FromLTRB(bounds.Left, Math.Max(bounds.Top, area.Bottom - reach), bounds.Right, bounds.Bottom);

                case TabAlignment.Left:
                    return Rectangle.FromLTRB(bounds.Left, bounds.Top, Math.Min(bounds.Right, area.Left + reach), bounds.Bottom);

                case TabAlignment.Right:
                    return Rectangle.FromLTRB(Math.Max(bounds.Left, area.Right - reach), bounds.Top, bounds.Right, bounds.Bottom);

                default:
                    return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, Math.Min(bounds.Bottom, area.Top + reach));
            }
        }

        // Outline on the three sides facing away from the page - the fourth
        // one is the page's own border (or, for the selected tab, nothing,
        // so it flows into the page).
        private void DrawTabBorder(Graphics graphics, Rectangle bounds)
        {
            int left = bounds.Left;
            int top = bounds.Top;
            int right = bounds.Right - 1;
            int bottom = bounds.Bottom - 1;

            using (Pen pen = new Pen(BorderColor))
            {
                switch (Alignment)
                {
                    case TabAlignment.Bottom:
                        graphics.DrawLine(pen, left, top, left, bottom);
                        graphics.DrawLine(pen, right, top, right, bottom);
                        graphics.DrawLine(pen, left, bottom, right, bottom);
                        break;

                    case TabAlignment.Left:
                        graphics.DrawLine(pen, left, top, right, top);
                        graphics.DrawLine(pen, left, bottom, right, bottom);
                        graphics.DrawLine(pen, left, top, left, bottom);
                        break;

                    case TabAlignment.Right:
                        graphics.DrawLine(pen, left, top, right, top);
                        graphics.DrawLine(pen, left, bottom, right, bottom);
                        graphics.DrawLine(pen, right, top, right, bottom);
                        break;

                    default:
                        graphics.DrawLine(pen, left, top, left, bottom);
                        graphics.DrawLine(pen, right, top, right, bottom);
                        graphics.DrawLine(pen, left, top, right, top);
                        break;
                }
            }
        }

        private void DrawAccent(Graphics graphics, Rectangle bounds)
        {
            Rectangle bar;

            switch (Alignment)
            {
                case TabAlignment.Bottom:
                    bar = new Rectangle(bounds.Left, bounds.Bottom - AccentThickness, bounds.Width, AccentThickness);
                    break;

                case TabAlignment.Left:
                    bar = new Rectangle(bounds.Left, bounds.Top, AccentThickness, bounds.Height);
                    break;

                case TabAlignment.Right:
                    bar = new Rectangle(bounds.Right - AccentThickness, bounds.Top, AccentThickness, bounds.Height);
                    break;

                default:
                    bar = new Rectangle(bounds.Left, bounds.Top, bounds.Width, AccentThickness);
                    break;
            }

            using (SolidBrush brush = new SolidBrush(SelectedTabIndicatorColor))
            {
                graphics.FillRectangle(brush, bar);
            }
        }

        /// <summary>
        /// Draws the image of a tab. The default draws it as it is; a
        /// derived control can draw something else in its place.
        /// </summary>
        protected virtual void DrawTabImage(Graphics graphics, int index, Image image, Rectangle bounds, Color foreColor)
        {
            graphics.DrawImage(image, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        /// <summary>
        /// How far, in pixels, the image and name of a tab with an image are
        /// moved sideways from the centered position (negative: to the left).
        /// Zero by default.
        /// </summary>
        protected virtual int GetTabContentOffset(int index)
        {
            return 0;
        }

        /// <summary>
        /// Called after a tab has been drawn, to add to it - a marker in a
        /// corner, say. Does nothing by default; not called for tabs drawn
        /// through <c>DrawItem</c>.
        /// </summary>
        protected virtual void DrawTabOverlay(Graphics graphics, int index, Rectangle bounds, Color foreColor)
        {
        }

        // Image and text, centered together. On a vertical strip the text
        // is rotated to read along the tab, like the standard control.
        private void DrawTabContent(Graphics graphics, int index, Rectangle bounds, TabPage page, Color foreColor)
        {
            Image image = GetTabImage(page);
            string text = page.Text ?? "";

            TextFormatFlags flags =
                TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.HorizontalCenter;

            if (!ShowKeyboardCues)
            {
                flags |= TextFormatFlags.HidePrefix;
            }

            if (!IsVertical())
            {
                DrawHorizontalContent(graphics, index, bounds, image, text, foreColor, flags);
                return;
            }

            DrawVerticalContent(graphics, index, bounds, image, text, foreColor, flags);
        }

        private void DrawHorizontalContent(Graphics graphics, int index, Rectangle bounds, Image image, string text, Color foreColor, TextFormatFlags flags)
        {
            if (image == null)
            {
                TextRenderer.DrawText(graphics, text, Font, bounds, foreColor, flags);
                return;
            }

            int imageWidth = image.Width + (text.Length > 0 ? ImageTextGap : 0);
            int textWidth = MeasureTextWidth(graphics, text);
            int contentWidth = Math.Min(bounds.Width, imageWidth + textWidth);
            int x = bounds.Left + (bounds.Width - contentWidth) / 2 + GetTabContentOffset(index);

            DrawTabImage(graphics, index, image, new Rectangle(x, bounds.Top + (bounds.Height - image.Height) / 2, image.Width, image.Height), foreColor);

            Rectangle textBounds = new Rectangle(x + imageWidth, bounds.Top, Math.Max(0, contentWidth - imageWidth), bounds.Height);
            TextRenderer.DrawText(graphics, text, Font, textBounds, foreColor, flags);
        }

        private void DrawVerticalContent(Graphics graphics, int index, Rectangle bounds, Image image, string text, Color foreColor, TextFormatFlags flags)
        {
            // Left tabs read bottom-to-top, right tabs top-to-bottom.
            bool bottomToTop = Alignment == TabAlignment.Left;
            int imageHeight = image == null ? 0 : image.Height + (text.Length > 0 ? ImageTextGap : 0);
            int contentHeight = image == null
                ? bounds.Height
                : Math.Min(bounds.Height, imageHeight + MeasureTextWidth(graphics, text));
            int y = bounds.Top + (bounds.Height - contentHeight) / 2;

            if (image != null)
            {
                int imageY = bottomToTop ? y + contentHeight - image.Height : y;
                DrawTabImage(graphics, index, image, new Rectangle(bounds.Left + (bounds.Width - image.Width) / 2, imageY, image.Width, image.Height), foreColor);
            }

            int textLength = Math.Max(0, contentHeight - imageHeight);
            int textStart = bottomToTop ? y : y + imageHeight;

            System.Drawing.Drawing2D.GraphicsState state = graphics.Save();

            try
            {
                if (bottomToTop)
                {
                    graphics.TranslateTransform(bounds.Left, textStart + textLength);
                    graphics.RotateTransform(-90);
                }
                else
                {
                    graphics.TranslateTransform(bounds.Right, textStart);
                    graphics.RotateTransform(90);
                }

                // TextRenderer ignores the rotation, so this one goes through GDI+.
                using (SolidBrush brush = new SolidBrush(foreColor))
                using (StringFormat format = new StringFormat(StringFormatFlags.NoWrap))
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;
                    format.HotkeyPrefix = ShowKeyboardCues
                        ? System.Drawing.Text.HotkeyPrefix.Show
                        : System.Drawing.Text.HotkeyPrefix.Hide;

                    graphics.DrawString(text, Font, brush, new RectangleF(0, 0, textLength, bounds.Width), format);
                }
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        // Measured without the alignment/ellipsis flags - those make the
        // measurement itself come back too narrow, which cut the text off
        // on tabs that carry an image.
        private int MeasureTextWidth(Graphics graphics, string text)
        {
            TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

            if (!ShowKeyboardCues)
            {
                flags |= TextFormatFlags.HidePrefix;
            }

            return TextRenderer.MeasureText(graphics, text, Font, Size.Empty, flags).Width;
        }

        /// <summary>The image a tab shows from <c>ImageList</c>, or null when it has none.</summary>
        protected Image GetTabImage(TabPage page)
        {
            if (ImageList == null)
                return null;

            if (page.ImageIndex >= 0 && page.ImageIndex < ImageList.Images.Count)
                return ImageList.Images[page.ImageIndex];

            if (!string.IsNullOrEmpty(page.ImageKey) && ImageList.Images.ContainsKey(page.ImageKey))
                return ImageList.Images[page.ImageKey];

            return null;
        }
    }
}
