using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.TabControl"/>. Behaves exactly
    /// like the standard one (same <c>TabPages</c>, keyboard handling,
    /// alignment, multiline rows, images, events) - only the drawing is
    /// replaced with the library's own look.
    /// </summary>
    /// <remarks>
    /// Named the same as its own base class, same as <see cref="ListBox"/> -
    /// the base type stays fully qualified so the class doesn't inherit from
    /// itself. Go through <see cref="UIStyles.TabControls.CreateStandard"/> to
    /// get one without spelling out <c>ErikwnkWFUI.Controls.TabControl</c>.
    /// Differences from the standard control: <see cref="System.Windows.Forms.TabControl.Appearance"/>
    /// is not supported (tabs are always drawn as tabs, never as buttons),
    /// <c>DrawItem</c> is never raised because the control draws everything
    /// itself, and <see cref="PageBackColor"/> replaces a page's own
    /// <c>BackColor</c> when the page is added.
    /// </remarks>
    public class TabControl : System.Windows.Forms.TabControl
    {
        private const int AccentThickness = 2;
        private const int ImageTextGap = 4;

        // Width of the native border band around the page contents, which
        // DisplayRectangle already excludes.
        private const int NativePageBorder = 4;

        // The native tab strip sits this far in from the control's edges.
        // The page box keeps the same margin so its edge lines up with the
        // outer edge of the first tab instead of sticking out past it.
        private const int NativeStripMargin = 2;

        private readonly ThemeColor _headerBackColor = new ThemeColor(() => Color.Transparent);
        private readonly ThemeColor _tabBackColor = new ThemeColor(() => UIColors.BackgroundDark);
        private readonly ThemeColor _hoverTabBackColor = new ThemeColor(() => UIColors.BackgroundLight);
        private readonly ThemeColor _selectedTabBackColor = new ThemeColor(() => UIColors.BackgroundMedium);
        private readonly ThemeColor _tabForeColor = new ThemeColor(() => UIColors.TextSecondary);
        private readonly ThemeColor _selectedTabForeColor = new ThemeColor(() => UIColors.TextPrimary);
        private readonly ThemeColor _disabledTabForeColor = new ThemeColor(() => UIColors.TextDisabled);
        private readonly ThemeColor _accentColor = new ThemeColor(() => UIColors.BorderLight);
        private readonly ThemeColor _borderColor = new ThemeColor(() => UIColors.BorderMedium);
        private readonly ThemeColor _pageBackColor = new ThemeColor(() => UIColors.BackgroundMedium);

        private const int WM_PARENTNOTIFY = 0x0210;

        private readonly TabScrollButtons _scrollButtons;

        private int _hoveredTabIndex = -1;

        /// <summary>
        /// Background behind the tabs, where no tab is drawn. Transparent by
        /// default, so the parent shows through and only the tabs and the
        /// page box are visible.
        /// </summary>
        public Color HeaderBackColor
        {
            get => _headerBackColor.Value;
            set
            {
                _headerBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Background of a tab that is neither selected nor hovered.</summary>
        public Color TabBackColor
        {
            get => _tabBackColor.Value;
            set
            {
                _tabBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Background of the tab under the mouse. Only used while <see cref="System.Windows.Forms.TabControl.HotTrack"/> is on, like the standard control.</summary>
        public Color HoverTabBackColor
        {
            get => _hoverTabBackColor.Value;
            set
            {
                _hoverTabBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Background of the selected tab. Defaults to the same color as <see cref="PageBackColor"/> so the tab flows into its page.</summary>
        public Color SelectedTabBackColor
        {
            get => _selectedTabBackColor.Value;
            set
            {
                _selectedTabBackColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Text color of a tab that is not selected.</summary>
        public Color TabForeColor
        {
            get => _tabForeColor.Value;
            set
            {
                _tabForeColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Text color of the selected tab.</summary>
        public Color SelectedTabForeColor
        {
            get => _selectedTabForeColor.Value;
            set
            {
                _selectedTabForeColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Text color of a tab whose page is disabled.</summary>
        public Color DisabledTabForeColor
        {
            get => _disabledTabForeColor.Value;
            set
            {
                _disabledTabForeColor.Set(value);
                Repaint();
            }
        }

        /// <summary>
        /// Color of the bar on the outer edge of the selected tab. Defaults
        /// to a neutral gray (<see cref="UIColors.BorderLight"/>), not the
        /// current accent - set it to <see cref="UIColors.Primary"/> for an
        /// accent-colored bar, which is what <see cref="UIStyles.TabControls.CreatePrimary"/> does.
        /// </summary>
        public Color AccentColor
        {
            get => _accentColor.Value;
            set
            {
                _accentColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Color of the tab outlines and the border around the page area.</summary>
        public Color BorderColor
        {
            get => _borderColor.Value;
            set
            {
                _borderColor.Set(value);
                Repaint();
            }
        }

        /// <summary>Background color applied to every page, including pages added later. Overrides a page's own <c>BackColor</c>.</summary>
        public Color PageBackColor
        {
            get => _pageBackColor.Value;
            set
            {
                _pageBackColor.Set(value);
                ApplyPageBackColor();
                Repaint();
            }
        }

        public TabControl()
        {
            Font = UIFonts.Normal;
            ForeColor = UIColors.TextPrimary;

            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            UpdateStyles();

            _scrollButtons = new TabScrollButtons(this);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // The scroll arrows are a child window the native control creates
            // on its own, only while the tabs overflow - this is where that
            // creation shows up.
            if (m.Msg == WM_PARENTNOTIFY && IsHandleCreated)
            {
                _scrollButtons.AttachTo(Handle);
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            _scrollButtons.AttachTo(Handle);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            _scrollButtons.AttachTo(Handle);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scrollButtons.ReleaseHandle();
            }

            base.Dispose(disposing);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            if (e.Control is TabPage page)
            {
                ApplyPageBackColor(page);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawHeaderBackground(e.Graphics);

            DrawPageArea(e.Graphics);

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
        }

        protected override void OnParentBackColorChanged(EventArgs e)
        {
            base.OnParentBackColorChanged(e);

            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            SetHoveredTabIndex(HotTrack ? GetTabIndexAt(e.Location) : -1);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            SetHoveredTabIndex(-1);
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);

            Repaint();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            Invalidate();
        }

        private void Repaint()
        {
            Invalidate();
            _scrollButtons.Refresh();
        }

        private void ApplyPageBackColor()
        {
            foreach (TabPage page in TabPages)
            {
                ApplyPageBackColor(page);
            }
        }

        private void ApplyPageBackColor(TabPage page)
        {
            // The visual-style page background is a themed texture that
            // ignores BackColor entirely.
            page.UseVisualStyleBackColor = false;
            page.BackColor = PageBackColor;
        }

        private void SetHoveredTabIndex(int index)
        {
            if (_hoveredTabIndex == index)
                return;

            _hoveredTabIndex = index;
            Invalidate();
        }

        private int GetTabIndexAt(Point location)
        {
            for (int i = 0; i < TabCount; i++)
            {
                if (GetTabRect(i).Contains(location))
                    return i;
            }

            return -1;
        }

        private bool IsVertical()
        {
            return Alignment == TabAlignment.Left || Alignment == TabAlignment.Right;
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
                System.Drawing.Drawing2D.GraphicsState state = graphics.Save();

                try
                {
                    graphics.TranslateTransform(-Left, -Top);
                    InvokePaintBackground(Parent, new PaintEventArgs(graphics, Parent.ClientRectangle));
                }
                finally
                {
                    graphics.Restore(state);
                }
            }

            if (color.A > 0)
            {
                using (SolidBrush brush = new SolidBrush(color))
                {
                    graphics.FillRectangle(brush, ClientRectangle);
                }
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
            bool selected = index == SelectedIndex;
            Rectangle bounds = GetTabRect(index);

            // Only the selected tab reaches over the page border.
            if (!selected)
            {
                bounds = TrimToStrip(bounds);
            }

            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            bool hovered = index == _hoveredTabIndex;
            TabPage page = TabPages[index];
            Rectangle outline = selected ? TrimToBorderLine(bounds) : bounds;

            using (SolidBrush brush = new SolidBrush(GetTabBackColor(selected, hovered)))
            {
                graphics.FillRectangle(brush, outline);
            }

            DrawTabBorder(graphics, outline);

            if (selected)
            {
                DrawAccent(graphics, bounds);
            }

            DrawTabContent(graphics, bounds, page, GetTabForeColor(selected, page.Enabled && Enabled));

            if (selected && Focused && ShowFocusCues)
            {
                Rectangle focus = Rectangle.Inflate(bounds, -3, -3);
                ControlPaint.DrawFocusRectangle(graphics, focus, GetTabForeColor(true, true), GetTabBackColor(true, false));
            }
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

        private Rectangle TrimToStrip(Rectangle bounds)
        {
            Rectangle area = GetPageAreaBounds();

            switch (Alignment)
            {
                case TabAlignment.Bottom:
                    return Rectangle.FromLTRB(bounds.Left, Math.Max(bounds.Top, area.Bottom), bounds.Right, bounds.Bottom);

                case TabAlignment.Left:
                    return Rectangle.FromLTRB(bounds.Left, bounds.Top, Math.Min(bounds.Right, area.Left), bounds.Bottom);

                case TabAlignment.Right:
                    return Rectangle.FromLTRB(Math.Max(bounds.Left, area.Right), bounds.Top, bounds.Right, bounds.Bottom);

                default:
                    return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, Math.Min(bounds.Bottom, area.Top));
            }
        }

        // The selected tab reaches a few pixels behind the page border, but
        // its outline must end on that border line - carried on further it
        // pokes out as little stubs into the page.
        private Rectangle TrimToBorderLine(Rectangle bounds)
        {
            Rectangle area = GetPageAreaBounds();

            switch (Alignment)
            {
                case TabAlignment.Bottom:
                    return Rectangle.FromLTRB(bounds.Left, Math.Max(bounds.Top, area.Bottom - 1), bounds.Right, bounds.Bottom);

                case TabAlignment.Left:
                    return Rectangle.FromLTRB(bounds.Left, bounds.Top, Math.Min(bounds.Right, area.Left + 1), bounds.Bottom);

                case TabAlignment.Right:
                    return Rectangle.FromLTRB(Math.Max(bounds.Left, area.Right - 1), bounds.Top, bounds.Right, bounds.Bottom);

                default:
                    return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, Math.Min(bounds.Bottom, area.Top + 1));
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

            using (SolidBrush brush = new SolidBrush(AccentColor))
            {
                graphics.FillRectangle(brush, bar);
            }
        }

        // Image and text, centered together. On a vertical strip the text
        // is rotated to read along the tab, like the standard control.
        private void DrawTabContent(Graphics graphics, Rectangle bounds, TabPage page, Color foreColor)
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
                DrawHorizontalContent(graphics, bounds, image, text, foreColor, flags);
                return;
            }

            DrawVerticalContent(graphics, bounds, image, text, foreColor, flags);
        }

        private void DrawHorizontalContent(Graphics graphics, Rectangle bounds, Image image, string text, Color foreColor, TextFormatFlags flags)
        {
            if (image == null)
            {
                TextRenderer.DrawText(graphics, text, Font, bounds, foreColor, flags);
                return;
            }

            int imageWidth = image.Width + (text.Length > 0 ? ImageTextGap : 0);
            int textWidth = MeasureTextWidth(graphics, text);
            int contentWidth = Math.Min(bounds.Width, imageWidth + textWidth);
            int x = bounds.Left + (bounds.Width - contentWidth) / 2;

            graphics.DrawImage(image, x, bounds.Top + (bounds.Height - image.Height) / 2, image.Width, image.Height);

            Rectangle textBounds = new Rectangle(x + imageWidth, bounds.Top, Math.Max(0, contentWidth - imageWidth), bounds.Height);
            TextRenderer.DrawText(graphics, text, Font, textBounds, foreColor, flags);
        }

        private void DrawVerticalContent(Graphics graphics, Rectangle bounds, Image image, string text, Color foreColor, TextFormatFlags flags)
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
                graphics.DrawImage(image, bounds.Left + (bounds.Width - image.Width) / 2, imageY, image.Width, image.Height);
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

        private Image GetTabImage(TabPage page)
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
