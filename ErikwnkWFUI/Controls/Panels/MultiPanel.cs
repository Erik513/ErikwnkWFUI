using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// One page of a <see cref="MultiPanel"/>: a transparent panel that fills the page area. Put the controls of the page on it
    /// like on any panel; its <see cref="Title"/> is shown in the header of the <see cref="MultiPanel"/> while the page is shown.
    /// </summary>
    public class MultiPanelPage : ThemedPanel
    {
        // Made by the multi panel (AddPage): a page that is not on one cannot be added to it.
        internal MultiPanelPage(string title)
        {
            Text = title ?? string.Empty;
            Dock = DockStyle.Fill;
            Margin = new Padding(0);
            Padding = new Padding(0);
            BackColor = Color.Transparent;
        }

        /// <summary>The title in the header of the <see cref="MultiPanel"/>; the same as <see cref="Control.Text"/>.</summary>
        public string Title
        {
            get { return Text; }
            set { Text = value ?? string.Empty; }
        }
    }

    /// <summary>
    /// A panel with several pages, of which one is shown at a time - like the pages of a book. A header on top holds, from left to
    /// right: the number of the page ("3/12", off by default), the title of the page and two small arrow buttons to go to the previous
    /// and the next page. It is made by <c>UIStyles.MultiPanels</c>, as a flat panel or as a card (rounded, with a thin line).
    /// </summary>
    /// <remarks>
    /// Add the pages with <see cref="AddPage"/> and put the controls on the page that comes back. Controls cannot be added to the
    /// multi panel itself. The arrows are disabled on the first and the last page, take no focus (it stays in the page) and show
    /// the title of the page they lead to as their tooltip. With the focus inside the multi panel, Ctrl+PageDown or Ctrl+Tab
    /// shows the next page and Ctrl+PageUp or Ctrl+Shift+Tab the previous one (on the first and the last page the key goes on to
    /// the parent, so that a tab control around it can use it). <see cref="ShowHeaderLine"/> adds a thin line under the header.
    /// A card in every other way (<see cref="ThemedCardPanel"/>): <c>CornerRadius</c>, <c>ShowBorder</c>, <c>BorderColor</c> and
    /// <c>Padding</c> work as there. Right-to-left layouts are not supported.
    /// </remarks>
    public class MultiPanel : ThemedCardPanel
    {
        private const int HeaderHeight96 = 36;
        private const int ButtonSize96 = 26;
        private const int Gap96 = 6;
        private const int HeaderTextMargin96 = 12;
        private const double CounterBlend = 0.35;
        private const double DisabledFade = 0.5;

        private readonly List<MultiPanelPage> _pages = new List<MultiPanelPage>();
        private readonly Button _previousButton;
        private readonly Button _nextButton;
        private int _selectedIndex = -1;
        private bool _showPageCount;
        private bool _showHeaderLine;
        private HorizontalAlignment _titleAlignment = HorizontalAlignment.Left;
        private bool _building = true;

        public MultiPanel()
            : this(null)
        {
        }

        // The arrows are symbol buttons in the colors of the multi panel (a color set, asked for again each time), or in the
        // neutral look without one.
        internal MultiPanel(Func<UIColorSet> arrowColors)
        {
            _previousButton = UIButtonFactory.CreateSymbol(UIStyles.Symbols.ChevronLeft, "", new Size(ButtonSize96, ButtonSize96), arrowColors);
            _nextButton = UIButtonFactory.CreateSymbol(UIStyles.Symbols.ChevronRight, "", new Size(ButtonSize96, ButtonSize96), arrowColors);
            // The tooltip of an arrow is the title of the page it leads to (see UpdateToolTips), so the name for a screen reader is its own.
            UpdateAccessibleNames();
            UIStrings.LanguageChanged += OnLanguageChanged;

            // The arrows do not take the focus: it stays in the page, and the keyboard turns the pages with Ctrl+PageUp/PageDown.
            ((SymbolButton)_previousButton).TakeNoFocus();
            ((SymbolButton)_nextButton).TakeNoFocus();
            _previousButton.Click += (sender, e) => Previous();
            _nextButton.Click += (sender, e) => Next();
            _previousButton.Dock = DockStyle.None;
            _nextButton.Dock = DockStyle.None;

            Controls.Add(_previousButton);
            Controls.Add(_nextButton);
            _building = false;

            UpdateNavigation();
        }

        /// <summary>Raised when another page is shown.</summary>
        public event EventHandler SelectedIndexChanged;

        /// <summary>The pages, in the order of their addition.</summary>
        public ReadOnlyCollection<MultiPanelPage> Pages
        {
            get { return _pages.AsReadOnly(); }
        }

        /// <summary>The number of pages.</summary>
        public int PageCount
        {
            get { return _pages.Count; }
        }

        /// <summary>The index of the page that is shown; -1 while there is no page. The first page added is shown at once.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not that of a page (-1 only while there is none).</exception>
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value < -1 || value >= _pages.Count || (value == -1 && _pages.Count > 0))
                    throw new ArgumentOutOfRangeException(nameof(value));

                Select(value, true);
            }
        }

        /// <summary>The page that is shown; null while there is no page.</summary>
        public MultiPanelPage SelectedPage
        {
            get { return _selectedIndex < 0 ? null : _pages[_selectedIndex]; }
        }

        /// <summary>Whether the number of the page is shown at the left of the header, like "3/12" (off by default).</summary>
        public bool ShowPageCount
        {
            get { return _showPageCount; }
            set
            {
                _showPageCount = value;
                InvalidateHeader();
            }
        }

        /// <summary>
        /// Whether a thin line separates the header from the page (off by default). It has the <see cref="ThemedCardPanel.BorderColor"/> of
        /// the multi panel, and the page area starts a little lower to leave room for it.
        /// </summary>
        public bool ShowHeaderLine
        {
            get { return _showHeaderLine; }
            set
            {
                _showHeaderLine = value;
                PerformLayout();
                Invalidate();
            }
        }

        /// <summary>Where the title stands between the page count and the arrows: left (the default), center or right.</summary>
        public HorizontalAlignment TitleAlignment
        {
            get { return _titleAlignment; }
            set
            {
                _titleAlignment = value;
                InvalidateHeader();
            }
        }

        /// <summary>Adds a page behind the others and returns it for its controls. The first page added is shown.</summary>
        /// <param name="title">The title in the header while the page is shown.</param>
        public MultiPanelPage AddPage(string title = "")
        {
            MultiPanelPage page = new MultiPanelPage(title) { Visible = false };
            page.TextChanged += OnPageTextChanged;
            page.Disposed += OnPageDisposed;
            page.ParentChanged += OnPageParentChanged;

            AddToControls(page);
            _pages.Add(page);

            if (_selectedIndex < 0)
                Select(0, true);
            else
                UpdateNavigation();

            InvalidateHeader();

            return page;
        }

        /// <summary>
        /// Takes a page out of the multi panel (it is not disposed, so it can be added to another control or disposed by the
        /// caller). If it was shown, the page that took its place is shown.
        /// </summary>
        /// <returns>False if the page does not belong to the multi panel.</returns>
        public bool RemovePage(MultiPanelPage page)
        {
            int index = _pages.IndexOf(page);

            if (index < 0)
                return false;

            RemoveFromControls(page);
            page.Visible = true;
            Forget(page);

            return true;
        }

        // The controls of the multi panel are not for anyone to add (see OnControlAdded): the pages and the arrows are put in and taken out here.
        private void AddToControls(Control control)
        {
            _building = true;

            try
            {
                Controls.Add(control);
            }
            finally
            {
                _building = false;
            }
        }

        private void RemoveFromControls(Control control)
        {
            _building = true;

            try
            {
                Controls.Remove(control);
            }
            finally
            {
                _building = false;
            }
        }

        // The names the arrows have for a screen reader follow a switch of the language (the tooltips are the titles of the pages).
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            UpdateAccessibleNames();
        }

        private void UpdateAccessibleNames()
        {
            _previousButton.AccessibleName = UIStrings.Get("MultiPanel.PreviousPage");
            _nextButton.AccessibleName = UIStrings.Get("MultiPanel.NextPage");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                UIStrings.LanguageChanged -= OnLanguageChanged;

            base.Dispose(disposing);
        }

        private void OnPageTextChanged(object sender, EventArgs e)
        {
            UpdateToolTips();
            InvalidateHeader();
        }

        // A page that is disposed by its owner leaves the multi panel with it.
        private void OnPageDisposed(object sender, EventArgs e)
        {
            Forget((MultiPanelPage)sender);
        }

        // A page that is put on another control by its owner leaves the multi panel (and is shown there).
        private void OnPageParentChanged(object sender, EventArgs e)
        {
            MultiPanelPage page = (MultiPanelPage)sender;

            if (_building || page.Parent == this)
                return;

            if (!page.Disposing && !page.IsDisposed)
                page.Visible = true;

            Forget(page);
        }

        // The page is no longer one of the pages: the one that was shown is replaced by the one that took its place.
        private void Forget(MultiPanelPage page)
        {
            page.Disposed -= OnPageDisposed;
            page.ParentChanged -= OnPageParentChanged;
            page.TextChanged -= OnPageTextChanged;
            int index = _pages.IndexOf(page);

            // (While the multi panel itself is disposed, its pages go with it.)
            if (index < 0 || Disposing || IsDisposed)
                return;

            _pages.RemoveAt(index);

            if (index < _selectedIndex)
            {
                _selectedIndex--;
                UpdateNavigation();
            }
            else if (index == _selectedIndex)
            {
                _selectedIndex = -2;
                Select(Math.Min(index, _pages.Count - 1), true);
            }
            else
            {
                UpdateNavigation();
            }

            InvalidateHeader();
        }

        /// <summary>Shows the next page; nothing happens on the last one.</summary>
        public void Next()
        {
            if (_selectedIndex < _pages.Count - 1)
                SelectedIndex = _selectedIndex + 1;
        }

        /// <summary>Shows the previous page; nothing happens on the first one.</summary>
        public void Previous()
        {
            if (_selectedIndex > 0)
                SelectedIndex = _selectedIndex - 1;
        }

        // Where the header is: the top of the area inside the padding.
        private Rectangle HeaderBounds
        {
            get
            {
                Rectangle area = base.DisplayRectangle;

                return new Rectangle(area.X, area.Y, area.Width, HeaderHeight);
            }
        }

        // The usual height, or more for a large font: the title and the count must fit.
        private int HeaderHeight
        {
            get { return Math.Max(Dpi.Scale(this, HeaderHeight96), Font.Height + Dpi.Scale(this, HeaderTextMargin96)); }
        }

        /// <summary>The area for the pages: inside the padding and below the header (and the line of <see cref="ShowHeaderLine"/>).</summary>
        public override Rectangle DisplayRectangle
        {
            get
            {
                Rectangle area = base.DisplayRectangle;
                int header = HeaderHeight + (_showHeaderLine ? Dpi.Scale(this, Gap96) : 0);

                return new Rectangle(area.X, area.Y + header, area.Width, Math.Max(0, area.Height - header));
            }
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);

            if (_building)
                return;

            Controls.Remove(e.Control);

            throw new InvalidOperationException("Controls are put on the pages of a MultiPanel (AddPage), not on the multi panel itself.");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            bool forward = keyData == (Keys.Control | Keys.PageDown) || keyData == (Keys.Control | Keys.Tab);
            bool backward = keyData == (Keys.Control | Keys.PageUp) || keyData == (Keys.Control | Keys.Shift | Keys.Tab);
            int target = _selectedIndex + (forward ? 1 : -1);

            if ((forward || backward) && target >= 0 && target < _pages.Count)
            {
                SelectedIndex = target;

                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            PerformLayout();
            Invalidate();
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // The base constructor already lays out (it sets the padding), before the buttons exist.
            if (_nextButton == null)
                return;

            Rectangle header = HeaderBounds;
            int size = Dpi.Scale(this, ButtonSize96);
            int gap = Dpi.Scale(this, Gap96) / 2;
            int top = header.Y + (header.Height - size) / 2;

            // Too narrow for both arrows: they stay at the left edge (and reach over the right one) instead of leaving the panel.
            int nextLeft = Math.Max(header.Left + size + gap, header.Right - size);
            _nextButton.SetBounds(nextLeft, top, size, size);
            _previousButton.SetBounds(nextLeft - gap - size, top, size, size);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle header = HeaderBounds;
            int gap = Dpi.Scale(this, Gap96);
            Color surface = SurfaceColor.Of(this);
            Color text = Enabled ? UIColors.GetBestTextColor(surface) : UIColors.GetDisabledText(surface);

            if (_showHeaderLine)
            {
                int thickness = Math.Max(1, Dpi.Scale(this, 1));

                using (SolidBrush line = new SolidBrush(BorderColor))
                    e.Graphics.FillRectangle(line, header.Left, header.Bottom + Dpi.Scale(this, Gap96) / 2 - thickness / 2, header.Width, thickness);
            }

            const TextFormatFlags common = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

            // Count and title only get the room before the arrows (DrawText cuts the text at its rectangle).
            int limit = _previousButton.Left - gap;
            int titleLeft = header.Left;

            if (_showPageCount && _pages.Count > 0)
            {
                string widest = _pages.Count + "/" + _pages.Count;
                int width = TextRenderer.MeasureText(e.Graphics, widest, Font, Size.Empty, common).Width;
                Rectangle counter = new Rectangle(header.Left, header.Y, Math.Min(width, limit - header.Left), header.Height);

                if (counter.Width > 0)
                    TextRenderer.DrawText(e.Graphics, (_selectedIndex + 1) + "/" + _pages.Count, Font, counter, Enabled ? UIColors.GetMutedText(surface, CounterBlend) : UIColors.BlendToward(UIColors.GetMutedText(surface, CounterBlend), surface, DisabledFade), common | TextFormatFlags.Left);

                titleLeft = header.Left + width + gap;
            }

            MultiPanelPage page = SelectedPage;
            Rectangle titleArea = new Rectangle(titleLeft, header.Y, limit - titleLeft, header.Height);

            if (page == null || page.Title.Length == 0 || titleArea.Width <= 0)
                return;

            TextFormatFlags align = _titleAlignment == HorizontalAlignment.Center ? TextFormatFlags.HorizontalCenter
                : _titleAlignment == HorizontalAlignment.Right ? TextFormatFlags.Right
                : TextFormatFlags.Left;

            using (Font bold = new Font(Font, FontStyle.Bold))
                TextRenderer.DrawText(e.Graphics, page.Title, bold, titleArea, text, common | align | TextFormatFlags.EndEllipsis);
        }

        private void Select(int index, bool raise)
        {
            if (index == _selectedIndex)
                return;

            // Focus in the page that is left (a text box, say) would be lost with it: it goes on to the new page.
            MultiPanelPage old = SelectedPage;
            Control active = FindForm()?.ActiveControl;
            bool focusMoves = old != null && active != null && old.Contains(active);

            _selectedIndex = index;

            for (int i = 0; i < _pages.Count; i++)
                _pages[i].Visible = i == index;

            if (focusMoves && index >= 0)
                _pages[index].SelectNextControl(null, true, true, true, false);

            UpdateNavigation();
            InvalidateHeader();

            if (raise)
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateNavigation()
        {
            _previousButton.Enabled = _selectedIndex > 0;
            _nextButton.Enabled = _selectedIndex >= 0 && _selectedIndex < _pages.Count - 1;
            UpdateToolTips();
        }

        // Each arrow shows the title of the page it leads to - and nothing where there is no such page or it has no title.
        private void UpdateToolTips()
        {
            UIButtonFactory.UpdateTooltip(_previousButton, _selectedIndex > 0 ? _pages[_selectedIndex - 1].Title : "");
            UIButtonFactory.UpdateTooltip(_nextButton, _selectedIndex >= 0 && _selectedIndex < _pages.Count - 1 ? _pages[_selectedIndex + 1].Title : "");
        }

        private void InvalidateHeader()
        {
            Invalidate(HeaderBounds);
        }
    }
}
