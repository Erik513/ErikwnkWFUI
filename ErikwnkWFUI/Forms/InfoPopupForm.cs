using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using System;
using System.Drawing;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Forms
{
    /// <summary>
    /// A small, borderless popup with extra info next to a control or the mouse cursor, like a
    /// tooltip: one color, and content of one or more blocks, each with an optional header and
    /// an optional text (<see cref="SetContent(InfoPopupSection[])"/>). Reuse a single instance
    /// for many show calls rather than creating a new one each time: it is repositioned and
    /// refilled instead of closing and opening again. Get one through
    /// <see cref="UIStyles.InfoPopups"/>.
    /// </summary>
    /// <remarks>
    /// The text, header and text alike, is white, or black when the background is too light
    /// for white; each header and each text can be given its own color
    /// (<see cref="InfoPopupSection.HeaderColor"/>, <see cref="InfoPopupSection.TextColor"/>).
    ///
    /// Content and place are two steps: set the content, then show it next to a control
    /// (<see cref="ShowNextTo"/>), at the mouse (<see cref="ShowAtMouse"/>), centered above a
    /// point of the control (<see cref="ShowCenteredAbove"/>) or at the mouse after a delay
    /// (<see cref="ShowAtMouseDelayed"/>). Content without any text shows nothing: the popup
    /// stays or goes hidden.
    ///
    /// The size follows the content, with a maximum text width after which the text wraps.
    /// With <see cref="FixedSize"/> the popup keeps one size and centers its content, for a
    /// short value that changes while it is shown (a percentage over a slider).
    ///
    /// The popup belongs to the window of its control: it is shown only while the application
    /// is in front (not when another program covers the window, and not over a minimized
    /// window), it stays above its window but not above other programs, and it moves with the
    /// window. It never takes the activation (not on show, not on a click), so the window next
    /// to it keeps the focus. Hide it with <c>Hide</c>; <c>Close</c> disposes it, as for any
    /// form shown with an owner, and it closes with its owner form.
    ///
    /// The library sets, and an application's own values are replaced: <c>BackColor</c> and the
    /// text colors (use <see cref="Colors"/>), the fonts, the border style and
    /// <c>ShowInTaskbar</c>. <c>Opacity</c> can be changed. The rounded corners come from
    /// Windows 11; on Windows 10 the popup is a plain rectangle. <c>RightToLeft</c> is not
    /// supported.
    /// </remarks>
    public class InfoPopupForm : Form
    {
        private const int MaxTextWidth96 = 260;
        private const int Padding96 = 20;
        private const int FixedPadding96 = 4;
        private const int BlockGap96 = 8;
        private const int HeaderGap96 = 3;
        private const int ScreenMargin = 10;
        private const int OwnerOffsetX = 8;
        private const int OwnerOffsetY = -10;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const TextFormatFlags MeasureFlags =
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.Left;

        private readonly OwnerFollower _follower = new OwnerFollower();
        private Timer _showDelayTimer;
        private Control _pendingOwner;
        private Point _pendingMouseScreenPosition;
        private InfoPopupSection[] _pendingSections;
        private InfoPopupSection[] _sections = new InfoPopupSection[0];
        private UIColorSet _colors;
        private Size _fixedSize = Size.Empty;

        public InfoPopupForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Opacity = 0.80;
            BackColor = GetBackColor();

            _showDelayTimer = new Timer { Interval = 400 };
            _showDelayTimer.Tick += OnShowDelayTimerTick;

            Load += delegate { ApplyRoundedCorners(); };
        }

        /// <summary>
        /// The colors of the popup: its background is the set's basic color. Left null, the
        /// neutral gray (<c>UIStyles.Colors.Sets.Gray</c>);
        /// <c>UIStyles.InfoPopups.CreatePrimary</c> makes one in the accent color.
        /// </summary>
        public UIColorSet Colors
        {
            get { return _colors; }
            set
            {
                _colors = value;
                BackColor = GetBackColor();
                Invalidate();
            }
        }

        /// <summary>
        /// The client size the popup keeps whatever its content, with the content centered; for
        /// one short value that changes while the popup is shown ("9%" and "100%" must not
        /// resize it). Empty (the default) sizes the popup to its content. In pixels.
        /// </summary>
        public Size FixedSize
        {
            get { return _fixedSize; }
            set
            {
                _fixedSize = value;
                UpdateSize();
            }
        }

        // A value popup must never take activation away from the window whose slider is being
        // dragged.
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        // The same for a click on the popup: it does not activate either.
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_NOACTIVATE;

                return parameters;
            }
        }

        // ---- the content ----

        /// <summary>
        /// Sets what the popup shows: one block per section, each with a header, a text, or
        /// both; a section without either is left out.
        /// </summary>
        public void SetContent(params InfoPopupSection[] sections)
        {
            InfoPopupSection[] kept = Normalize(sections);

            if (SameContent(kept, _sections))
                return;

            _sections = kept;
            UpdateSize();
        }

        /// <summary>Sets a single text, without a header.</summary>
        public void SetContent(string text)
        {
            SetContent(new InfoPopupSection(null, text));
        }

        /// <summary>Sets a header with its text, each in its own color or, left empty, the automatic one.</summary>
        public void SetContent(string header, string text, Color headerColor = default(Color), Color textColor = default(Color))
        {
            SetContent(new InfoPopupSection(header, text) { HeaderColor = headerColor, TextColor = textColor });
        }

        private static InfoPopupSection[] Normalize(InfoPopupSection[] sections)
        {
            System.Collections.Generic.List<InfoPopupSection> kept = new System.Collections.Generic.List<InfoPopupSection>();

            if (sections != null)
            {
                foreach (InfoPopupSection section in sections)
                {
                    if (section == null)
                        continue;

                    bool hasHeader = !string.IsNullOrWhiteSpace(section.Header);
                    bool hasText = !string.IsNullOrWhiteSpace(section.Text);

                    if (hasHeader || hasText)
                        kept.Add(new InfoPopupSection(hasHeader ? section.Header : null, hasText ? section.Text : null)
                        {
                            HeaderColor = section.HeaderColor,
                            TextColor = section.TextColor
                        });
                }
            }

            return kept.ToArray();
        }

        private static bool SameContent(InfoPopupSection[] first, InfoPopupSection[] second)
        {
            if (first.Length != second.Length)
                return false;

            for (int i = 0; i < first.Length; i++)
            {
                if (first[i].Header != second[i].Header || first[i].Text != second[i].Text
                    || first[i].HeaderColor != second[i].HeaderColor || first[i].TextColor != second[i].TextColor)
                    return false;
            }

            return true;
        }

        private bool HasContent
        {
            get { return _sections.Length > 0; }
        }

        // ---- showing it ----

        /// <summary>Shows (or moves, if already visible) the popup anchored to the bottom-right of <paramref name="owner"/>.</summary>
        public void ShowNextTo(Control owner)
        {
            if (!CanShow(owner))
                return;

            Place(owner, GetLocationNextTo(owner));
        }

        /// <summary>Shows (or moves) the popup near <paramref name="mouseScreenPosition"/> - typical use is a MouseMove handler.</summary>
        public void ShowAtMouse(Control owner, Point mouseScreenPosition)
        {
            if (!CanShow(owner))
                return;

            Place(owner, GetLocationNearMouse(owner, mouseScreenPosition));
        }

        /// <summary>
        /// Shows (or moves) the popup centered horizontally on <paramref name="ownerLocalX"/> (a pixel x
        /// inside <paramref name="owner"/>) and just above <paramref name="owner"/>; below it if there is
        /// no room above. Typical use is a live value readout over a slider thumb.
        /// </summary>
        public void ShowCenteredAbove(Control owner, int ownerLocalX)
        {
            if (!CanShow(owner))
                return;

            Point ownerTop = owner.PointToScreen(new Point(ownerLocalX, 0));
            int x = ownerTop.X - Width / 2;
            int y = ownerTop.Y - Height - 6;

            Rectangle screen = Screen.FromControl(owner).WorkingArea;

            if (x < screen.Left + ScreenMargin)
                x = screen.Left + ScreenMargin;

            if (x + Width > screen.Right - ScreenMargin)
                x = screen.Right - ScreenMargin - Width;

            if (y < screen.Top + ScreenMargin)
                y = ownerTop.Y + owner.Height + 6;

            Place(owner, new Point(x, y));
        }

        /// <summary>
        /// Like <see cref="ShowAtMouse"/> with the given content, but after 400 ms - for hover
        /// tooltips, so passing the mouse over several items does not flash a popup for each.
        /// Call <see cref="CancelPendingShow"/> on MouseLeave to cancel a show that has not
        /// happened yet.
        /// </summary>
        public void ShowAtMouseDelayed(Control owner, Point mouseScreenPosition, params InfoPopupSection[] sections)
        {
            if (owner == null || owner.IsDisposed)
                return;

            _pendingOwner = owner;
            _pendingMouseScreenPosition = mouseScreenPosition;
            _pendingSections = sections;

            _showDelayTimer.Stop();
            _showDelayTimer.Start();
        }

        /// <summary>Cancels a show scheduled by <see cref="ShowAtMouseDelayed"/> that has not happened yet - does not hide the popup if it is already showing.</summary>
        public void CancelPendingShow()
        {
            if (_showDelayTimer != null)
                _showDelayTimer.Stop();

            _pendingOwner = null;
            _pendingSections = null;
        }

        private void OnShowDelayTimerTick(object sender, EventArgs e)
        {
            _showDelayTimer.Stop();

            Control owner = _pendingOwner;
            InfoPopupSection[] sections = _pendingSections;

            if (owner == null || owner.IsDisposed || sections == null)
                return;

            SetContent(sections);
            ShowAtMouse(owner, _pendingMouseScreenPosition);
        }

        // Nothing to show without an owner or without content; a popup that is up goes away
        // when its content is taken.
        private bool CanShow(Control owner)
        {
            if (owner == null || owner.IsDisposed)
                return false;

            if (!HasContent)
            {
                Hide();

                return false;
            }

            return true;
        }

        private void Place(Control owner, Point location)
        {
            Form ownerForm = owner.FindForm();

            if (!PopupSupport.CanShowOver(ownerForm))
                return;

            BackColor = GetBackColor();
            Location = location;

            if (!Visible)
            {
                Show(ownerForm);
                BringToFront();
            }

            _follower.Attach(this, ownerForm);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible)
                _follower.Detach();
        }

        private Point GetLocationNextTo(Control owner)
        {
            Point location = owner.PointToScreen(
                new Point(owner.Width + OwnerOffsetX, -Height + owner.Height + OwnerOffsetY));

            Rectangle screen = Screen.FromControl(owner).WorkingArea;

            if (location.Y < screen.Top + ScreenMargin)
                location.Y = screen.Top + ScreenMargin;

            if (location.X + Width > screen.Right)
                location.X = screen.Right - Width - ScreenMargin;

            if (location.X < screen.Left + ScreenMargin)
                location.X = screen.Left + ScreenMargin;

            if (location.Y + Height > screen.Bottom)
                location.Y = screen.Bottom - Height - ScreenMargin;

            return location;
        }

        private Point GetLocationNearMouse(Control owner, Point mouseScreenPosition)
        {
            Point result = new Point(mouseScreenPosition.X + 12, mouseScreenPosition.Y + 12);

            Form ownerForm = owner.FindForm();

            Rectangle bounds = ownerForm != null
                ? ownerForm.Bounds
                : Screen.FromControl(owner).WorkingArea;

            if (result.X + Width > bounds.Right)
                result.X = mouseScreenPosition.X - Width - 12;

            if (result.Y + Height > bounds.Bottom)
                result.Y = mouseScreenPosition.Y - Height - 12;

            if (result.X < bounds.Left)
                result.X = bounds.Left + 10;

            if (result.Y < bounds.Top)
                result.Y = bounds.Top + 10;

            return result;
        }

        // ---- colors ----

        private UIColorSet GetColors()
        {
            return _colors ?? UIStyles.Colors.Sets.Gray;
        }

        // The background is the set's basic color as it is.
        private Color GetBackColor()
        {
            return GetColors().Basic;
        }

        // The automatic text color: white, or black when the background is too light for white.
        // One of the two always reads (at least 4.58:1).
        private Color GetForeColor()
        {
            Color back = GetBackColor();

            return UIColors.GetContrastRatio(Color.White, back) >= UIColors.GetContrastRatio(Color.Black, back)
                ? Color.White
                : Color.Black;
        }

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

        private Size MeasureContent(int textWidth)
        {
            int width = 0;
            int height = 0;

            for (int i = 0; i < _sections.Length; i++)
            {
                Size header;
                Size text;
                MeasureBlock(_sections[i], textWidth, out header, out text);

                if (i > 0)
                    height += Scale(BlockGap96);

                height += header.Height + text.Height;

                if (header.Height > 0 && text.Height > 0)
                    height += Scale(HeaderGap96);

                width = Math.Max(width, Math.Max(header.Width, text.Width));
            }

            return new Size(width, height);
        }

        // A borderless top-level form cannot go below the system's minimum tracking size
        // (about 136x39) unless MinimumSize is set: it is locked to the wanted size.
        private void UpdateSize()
        {
            Size client;

            if (IsFixed)
            {
                client = _fixedSize;
            }
            else
            {
                Size content = MeasureContent(GetTextWidth());
                int padding = Scale(Padding96);
                client = new Size(content.Width + 2 * padding, content.Height + 2 * padding);
            }

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            ClientSize = client;
            MinimumSize = Size;
            MaximumSize = Size;

            ApplyRoundedCorners();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!HasContent)
                return;

            int textWidth = GetTextWidth();
            Size content = MeasureContent(textWidth);
            int padding = IsFixed ? Scale(FixedPadding96) : Scale(Padding96);

            // In the fixed size the content sits in the middle, otherwise at the padding.
            int x = padding;
            int y = IsFixed ? Math.Max(0, (ClientSize.Height - content.Height) / 2) : padding;

            Color autoColor = GetForeColor();
            TextFormatFlags align = IsFixed ? TextFormatFlags.HorizontalCenter : TextFormatFlags.Left;
            TextFormatFlags flags = (MeasureFlags & ~TextFormatFlags.Left) | align;

            for (int i = 0; i < _sections.Length; i++)
            {
                InfoPopupSection section = _sections[i];
                Size header;
                Size text;
                MeasureBlock(section, textWidth, out header, out text);

                if (i > 0)
                    y += Scale(BlockGap96);

                if (header.Height > 0)
                {
                    TextRenderer.DrawText(e.Graphics, section.Header, UIFonts.Title, new Rectangle(x, y, textWidth, header.Height), section.HeaderColor.IsEmpty ? autoColor : section.HeaderColor, flags);
                    y += header.Height;
                }

                if (header.Height > 0 && text.Height > 0)
                    y += Scale(HeaderGap96);

                if (text.Height > 0)
                {
                    TextRenderer.DrawText(e.Graphics, section.Text, UIFonts.Normal, new Rectangle(x, y, textWidth, text.Height), section.TextColor.IsEmpty ? autoColor : section.TextColor, flags);
                    y += text.Height;
                }
            }
        }

        // Win11 rounds window corners itself via DWM (anti-aliased, all four identical): just
        // ask for the radius. A window Region fights this and leaves one corner square; on
        // Win10 the call does nothing and the popup is a plain rectangle.
        private void ApplyRoundedCorners()
        {
            if (!IsHandleCreated)
                return;

            int preference = IsFixed ? DWMWCP_ROUNDSMALL : DWMWCP_ROUND;

            try
            {
                DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            }
            catch
            {
                // before Windows 11: no rounded corners
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyRoundedCorners();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _follower.Detach();

            if (disposing && _showDelayTimer != null)
            {
                _showDelayTimer.Stop();
                _showDelayTimer.Tick -= OnShowDelayTimerTick;
                _showDelayTimer.Dispose();
                _showDelayTimer = null;
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>One block of content for <see cref="InfoPopupForm"/>: a header, a text, or both. A part left null or empty is not shown.</summary>
    public class InfoPopupSection
    {
        public string Header { get; set; }
        public string Text { get; set; }

        /// <summary>The color of the header. Left empty, it is white, or black on a background too light for white.</summary>
        public Color HeaderColor { get; set; }

        /// <summary>The color of the text. Left empty, it is white, or black on a background too light for white.</summary>
        public Color TextColor { get; set; }

        public InfoPopupSection(string header, string text)
        {
            Header = header;
            Text = text;
        }
    }
}
