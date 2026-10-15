using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
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
        public MultiPanelPage(string title)
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
    /// multi panel itself. The arrows are disabled on the first and the last page.
    /// </remarks>
    public class MultiPanel : ThemedCardPanel
    {
        private const int HeaderHeight96 = 36;
        private const int ButtonSize96 = 26;
        private const int Gap96 = 6;
        private const double CounterBlend = 0.35;
        private const double DisabledBlend = 0.5;

        private readonly List<MultiPanelPage> _pages = new List<MultiPanelPage>();
        private readonly Button _previousButton;
        private readonly Button _nextButton;
        private int _selectedIndex = -1;
        private bool _showPageCount;
        private HorizontalAlignment _titleAlignment = HorizontalAlignment.Left;
        private bool _building = true;

        public MultiPanel()
            : this(null)
        {
        }

        // The arrows are symbol buttons in the colors of the multi panel (a color set), or in the neutral look without one.
        internal MultiPanel(UIColorSet arrowColors)
        {
            _previousButton = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.ChevronLeft, UIStrings.Get("MultiPanel.PreviousPage"), new Size(ButtonSize96, ButtonSize96), arrowColors);
            _nextButton = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.ChevronRight, UIStrings.Get("MultiPanel.NextPage"), new Size(ButtonSize96, ButtonSize96), arrowColors);
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
            page.TextChanged += (sender, e) => InvalidateHeader();
            page.Disposed += OnPageDisposed;
            page.ParentChanged += OnPageParentChanged;

            _building = true;
            Controls.Add(page);
            _building = false;
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

            _building = true;
            Controls.Remove(page);
            _building = false;
            page.Visible = true;
            Forget(page);

            return true;
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

                return new Rectangle(area.X, area.Y, area.Width, Dpi.Scale(this, HeaderHeight96));
            }
        }

        /// <summary>The area for the pages: inside the padding and below the header.</summary>
        public override Rectangle DisplayRectangle
        {
            get
            {
                Rectangle area = base.DisplayRectangle;
                int header = Dpi.Scale(this, HeaderHeight96);

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

            _nextButton.SetBounds(header.Right - size, top, size, size);
            _previousButton.SetBounds(_nextButton.Left - gap - size, top, size, size);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle header = HeaderBounds;
            int gap = Dpi.Scale(this, Gap96);
            Color text = UIColors.GetBestTextColor(BackColor);

            if (!Enabled)
                text = UIColors.BlendToward(text, BackColor, DisabledBlend);

            const TextFormatFlags common = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            int titleLeft = header.Left;

            if (_showPageCount && _pages.Count > 0)
            {
                string widest = _pages.Count + "/" + _pages.Count;
                int width = TextRenderer.MeasureText(e.Graphics, widest, Font, Size.Empty, common).Width;
                Rectangle counter = new Rectangle(header.Left, header.Y, width, header.Height);
                TextRenderer.DrawText(e.Graphics, (_selectedIndex + 1) + "/" + _pages.Count, Font, counter, UIColors.BlendToward(text, BackColor, CounterBlend), common | TextFormatFlags.Left);
                titleLeft = counter.Right + gap;
            }

            MultiPanelPage page = SelectedPage;

            if (page == null || page.Title.Length == 0)
                return;

            int titleRight = _previousButton.Left - gap;
            Rectangle titleArea = new Rectangle(titleLeft, header.Y, Math.Max(0, titleRight - titleLeft), header.Height);
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

            _selectedIndex = index;

            for (int i = 0; i < _pages.Count; i++)
                _pages[i].Visible = i == index;

            UpdateNavigation();
            InvalidateHeader();

            if (raise)
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateNavigation()
        {
            _previousButton.Enabled = _selectedIndex > 0;
            _nextButton.Enabled = _selectedIndex >= 0 && _selectedIndex < _pages.Count - 1;
        }

        private void InvalidateHeader()
        {
            Invalidate(HeaderBounds);
        }
    }
}
