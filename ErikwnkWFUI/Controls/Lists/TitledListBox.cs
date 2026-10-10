using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A <see cref="ListBox"/> with an optional header bar above it - the
    /// same list box in every other way: every member of <see cref="ListBox"/>
    /// (and the usual ones of the standard list box) is here too and works on
    /// the inner list box (see <see cref="InnerListBox"/>, for anything not
    /// offered here).
    /// </summary>
    public class TitledListBox : UserControl
    {
        private const int DefaultHeaderHeight = 30;
        private const int HeaderSidePadding = 10;

        private readonly Panel _headerPanel;
        private readonly Label _titleLabel;
        private readonly ListBox _listBox;

        public event EventHandler SelectedIndexChanged;
        public event EventHandler ItemsReordered;

        public TitledListBox()
            : this(null, false, false, null, ContentAlignment.MiddleLeft)
        {
        }

        /// <summary>headerTitle left null/empty hides the header bar entirely (the default) - set <see cref="Title"/> later to show it.</summary>
        public TitledListBox(
            string displayMember = null,
            bool allowReorder = false,
            bool showEnumeration = false,
            string headerTitle = null,
            ContentAlignment headerTextAlign = ContentAlignment.MiddleLeft)
        {
            ConfigureControl();

            _headerPanel = CreateHeaderPanel();
            _titleLabel = CreateTitleLabel(headerTextAlign);
            _listBox = CreateListBox(displayMember, allowReorder, showEnumeration);

            _headerPanel.Controls.Add(_titleLabel);

            Controls.Add(_listBox);
            Controls.Add(_headerPanel);

            WireEvents();

            Title = headerTitle;
            HeaderTextAlign = headerTextAlign;

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw,
                true);

            UpdateStyles();
        }

        /// <summary>The wrapped list box - use this for anything (events, colors, ...) not already proxied by a member on this class.</summary>
        public ListBox InnerListBox
        {
            get { return _listBox; }
        }

        public Func<object, bool> IsItemDisabled
        {
            get { return _listBox.IsItemDisabled; }
            set { _listBox.IsItemDisabled = value; }
        }

        public Func<object, Image> IconProvider
        {
            get { return _listBox.IconProvider; }
            set { _listBox.IconProvider = value; }
        }

        public Func<object, string> DisplayTextProvider
        {
            get { return _listBox.DisplayTextProvider; }
            set { _listBox.DisplayTextProvider = value; }
        }

        /// <summary>The title in the bar above the list; the bar is not shown while it is empty.</summary>
        public string Title
        {
            get { return _titleLabel.Text; }
            set
            {
                string title = value ?? "";

                _titleLabel.Text = title;
                _headerPanel.Visible = !string.IsNullOrWhiteSpace(title);
            }
        }

        /// <summary>Height of the title bar in pixels (30 by default).</summary>
        public int HeaderHeight
        {
            get { return _headerPanel.Height; }
            set { _headerPanel.Height = Math.Max(0, value); }
        }

        /// <summary>Background of the title bar. The default is the theme's dark background and stays so whatever color set the list was made with: only setting it changes it.</summary>
        public Color HeaderBackColor
        {
            get { return _headerPanel.BackColor; }
            set { _headerPanel.BackColor = value; }
        }

        /// <summary>Text color of the title. The default is the theme's primary text color; only setting it changes it.</summary>
        public Color HeaderForeColor
        {
            get { return _titleLabel.ForeColor; }
            set { _titleLabel.ForeColor = value; }
        }

        /// <summary>Font of the title (the library's title font by default).</summary>
        public Font HeaderFont
        {
            get { return _titleLabel.Font; }
            set { _titleLabel.Font = value; }
        }

        /// <summary>Where the title stands in its bar (left and centered vertically by default).</summary>
        public ContentAlignment HeaderTextAlign
        {
            get { return _titleLabel.TextAlign; }
            set
            {
                _titleLabel.TextAlign = value;
                _titleLabel.Padding = GetHeaderPadding(value);
            }
        }

        public ListBox.ObjectCollection Items
        {
            get { return _listBox.Items; }
        }

        public object SelectedItem
        {
            get { return _listBox.SelectedItem; }
            set { _listBox.SelectedItem = value; }
        }

        public int SelectedIndex
        {
            get { return _listBox.SelectedIndex; }
            set { _listBox.SelectedIndex = value; }
        }

        public int ItemHeight
        {
            get { return _listBox.ItemHeight; }
            set { _listBox.ItemHeight = value; }
        }

        public Color DragIndicatorColor
        {
            get { return _listBox.DragIndicatorColor; }
            set { _listBox.DragIndicatorColor = value; }
        }

        public Color SelectedForeColor
        {
            get { return _listBox.SelectedForeColor; }
            set { _listBox.SelectedForeColor = value; }
        }

        public Color SelectedBackColor
        {
            get { return _listBox.SelectedBackColor; }
            set { _listBox.SelectedBackColor = value; }
        }

        // ---- the rest of the list box: every member of ListBox is here too, so a TitledListBox is used exactly
        // like a ListBox (the header is all that is added) ----

        public bool ShowEnumeration
        {
            get { return _listBox.ShowEnumeration; }
            set { _listBox.ShowEnumeration = value; }
        }

        public bool AllowReorder
        {
            get { return _listBox.AllowReorder; }
            set { _listBox.AllowReorder = value; }
        }

        public Color ItemBackColor
        {
            get { return _listBox.ItemBackColor; }
            set { _listBox.ItemBackColor = value; }
        }

        public Color ItemForeColor
        {
            get { return _listBox.ItemForeColor; }
            set { _listBox.ItemForeColor = value; }
        }

        public Color HoverBackColor
        {
            get { return _listBox.HoverBackColor; }
            set { _listBox.HoverBackColor = value; }
        }

        public Color DisabledForeColor
        {
            get { return _listBox.DisabledForeColor; }
            set { _listBox.DisabledForeColor = value; }
        }

        public Color DisabledBackColor
        {
            get { return _listBox.DisabledBackColor; }
            set { _listBox.DisabledBackColor = value; }
        }

        public Color DragHandleColor
        {
            get { return _listBox.DragHandleColor; }
            set { _listBox.DragHandleColor = value; }
        }

        public SelectionMode SelectionMode
        {
            get { return _listBox.SelectionMode; }
            set { _listBox.SelectionMode = value; }
        }

        public ListBox.SelectedObjectCollection SelectedItems
        {
            get { return _listBox.SelectedItems; }
        }

        public ListBox.SelectedIndexCollection SelectedIndices
        {
            get { return _listBox.SelectedIndices; }
        }

        public object DataSource
        {
            get { return _listBox.DataSource; }
            set { _listBox.DataSource = value; }
        }

        public string DisplayMember
        {
            get { return _listBox.DisplayMember; }
            set { _listBox.DisplayMember = value; }
        }

        public string ValueMember
        {
            get { return _listBox.ValueMember; }
            set { _listBox.ValueMember = value; }
        }

        public object SelectedValue
        {
            get { return _listBox.SelectedValue; }
            set { _listBox.SelectedValue = value; }
        }

        public int TopIndex
        {
            get { return _listBox.TopIndex; }
            set { _listBox.TopIndex = value; }
        }

        public bool Sorted
        {
            get { return _listBox.Sorted; }
            set { _listBox.Sorted = value; }
        }

        public bool HorizontalScrollbar
        {
            get { return _listBox.HorizontalScrollbar; }
            set { _listBox.HorizontalScrollbar = value; }
        }

        public bool ScrollAlwaysVisible
        {
            get { return _listBox.ScrollAlwaysVisible; }
            set { _listBox.ScrollAlwaysVisible = value; }
        }

        public bool MultiColumn
        {
            get { return _listBox.MultiColumn; }
            set { _listBox.MultiColumn = value; }
        }

        public int ColumnWidth
        {
            get { return _listBox.ColumnWidth; }
            set { _listBox.ColumnWidth = value; }
        }

        public bool IntegralHeight
        {
            get { return _listBox.IntegralHeight; }
            set { _listBox.IntegralHeight = value; }
        }

        public event EventHandler SelectedValueChanged
        {
            add { _listBox.SelectedValueChanged += value; }
            remove { _listBox.SelectedValueChanged -= value; }
        }

        public int FindString(string text)
        {
            return _listBox.FindString(text);
        }

        public int FindString(string text, int startIndex)
        {
            return _listBox.FindString(text, startIndex);
        }

        public int FindStringExact(string text)
        {
            return _listBox.FindStringExact(text);
        }

        public bool GetSelected(int index)
        {
            return _listBox.GetSelected(index);
        }

        public void SetSelected(int index, bool value)
        {
            _listBox.SetSelected(index, value);
        }

        public string GetItemText(object item)
        {
            return _listBox.GetItemText(item);
        }

        public Rectangle GetItemRectangle(int index)
        {
            return _listBox.GetItemRectangle(index);
        }

        /// <summary>The font of the items (as on the list box; the title has <see cref="HeaderFont"/>).</summary>
        public override Font Font
        {
            get { return _listBox != null ? _listBox.Font : base.Font; }
            set
            {
                if (_listBox != null)
                    _listBox.Font = value;
                else
                    base.Font = value;
            }
        }

        /// <summary>The text color of the items (as on the list box; the title has <see cref="HeaderForeColor"/>).</summary>
        public override Color ForeColor
        {
            get { return _listBox != null ? _listBox.ForeColor : base.ForeColor; }
            set
            {
                if (_listBox != null)
                    _listBox.ForeColor = value;
                else
                    base.ForeColor = value;
            }
        }

        /// <summary>The context menu of the list (a right click on an item or on the room below the items).</summary>
        public override System.Windows.Forms.ContextMenuStrip ContextMenuStrip
        {
            get { return base.ContextMenuStrip; }
            set
            {
                base.ContextMenuStrip = value;

                if (_listBox != null)
                    _listBox.ContextMenuStrip = value;
            }
        }

        /// <summary>Gives the focus to the list.</summary>
        public new bool Focus()
        {
            return _listBox.Focus();
        }

        /// <summary>Selects the list (it is the one that takes the keys).</summary>
        public new void Select()
        {
            _listBox.Select();
        }

        public int IndexFromPoint(Point point)
        {
            return _listBox.IndexFromPoint(point);
        }

        public void ClearSelected()
        {
            _listBox.ClearSelected();
        }

        public void MoveItem(int fromIndex, int toIndex)
        {
            _listBox.MoveItem(fromIndex, toIndex);
        }

        public void BeginUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.BeginUpdate();
        }

        public void EndUpdate()
        {
            if (_listBox == null)
                return;

            _listBox.EndUpdate();
        }

        private void ConfigureControl()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Margin = new Padding(0);
            Padding = new Padding(0);
        }

        private Panel CreateHeaderPanel()
        {
            return new Panel
            {
                Dock = DockStyle.Top,
                Height = DefaultHeaderHeight,
                BackColor = UIColors.BackgroundDark,
                Visible = false,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
        }

        private Label CreateTitleLabel(ContentAlignment textAlign)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "",
                ForeColor = UIColors.TextPrimary,
                Font = UIFonts.Title,
                TextAlign = textAlign,
                Padding = GetHeaderPadding(textAlign),
                BackColor = Color.Transparent,
                AutoEllipsis = true,
                UseMnemonic = false
            };
        }

        private ListBox CreateListBox(
            string displayMember,
            bool allowReorder,
            bool showEnumeration)
        {
            return new ListBox
            {
                Dock = DockStyle.Fill,
                DisplayMember = displayMember,
                AllowReorder = allowReorder,
                ShowEnumeration = showEnumeration,
                Margin = new Padding(0)
            };
        }

        private void WireEvents()
        {
            if (_listBox == null)
                return;

            _listBox.SelectedIndexChanged += OnListBoxSelectedIndexChanged;
            _listBox.ItemsReordered += OnListBoxItemsReordered;
            _listBox.MouseMove += OnListBoxMouseMove;
            _listBox.MouseLeave += OnListBoxMouseLeave;
            _listBox.Click += OnListBoxClick;
            _listBox.DoubleClick += OnListBoxDoubleClick;
            _listBox.MouseClick += OnListBoxMouseClick;
            _listBox.MouseDoubleClick += OnListBoxMouseDoubleClick;
            _listBox.MouseDown += OnListBoxMouseDown;
            _listBox.MouseUp += OnListBoxMouseUp;
            _listBox.KeyDown += OnListBoxKeyDown;
            _listBox.KeyUp += OnListBoxKeyUp;
            _listBox.KeyPress += OnListBoxKeyPress;
        }

        // The events of the list reach the ones on this control with this control as the sender, and the
        // places of the mouse in this control's own coordinates (the list is below the title bar).
        private void OnListBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (SelectedIndexChanged != null)
                SelectedIndexChanged(this, e);
        }

        private void OnListBoxItemsReordered(object sender, EventArgs e)
        {
            if (ItemsReordered != null)
                ItemsReordered(this, e);
        }

        private MouseEventArgs InOwnCoordinates(MouseEventArgs e)
        {
            return new MouseEventArgs(e.Button, e.Clicks, e.X + _listBox.Left, e.Y + _listBox.Top, e.Delta);
        }

        private void OnListBoxMouseMove(object sender, MouseEventArgs e)
        {
            OnMouseMove(InOwnCoordinates(e));
        }

        private void OnListBoxClick(object sender, EventArgs e)
        {
            OnClick(e);
        }

        private void OnListBoxDoubleClick(object sender, EventArgs e)
        {
            OnDoubleClick(e);
        }

        private void OnListBoxMouseClick(object sender, MouseEventArgs e)
        {
            OnMouseClick(InOwnCoordinates(e));
        }

        private void OnListBoxMouseDoubleClick(object sender, MouseEventArgs e)
        {
            OnMouseDoubleClick(InOwnCoordinates(e));
        }

        private void OnListBoxMouseDown(object sender, MouseEventArgs e)
        {
            OnMouseDown(InOwnCoordinates(e));
        }

        private void OnListBoxMouseUp(object sender, MouseEventArgs e)
        {
            OnMouseUp(InOwnCoordinates(e));
        }

        private void OnListBoxKeyDown(object sender, KeyEventArgs e)
        {
            OnKeyDown(e);
        }

        private void OnListBoxKeyUp(object sender, KeyEventArgs e)
        {
            OnKeyUp(e);
        }

        private void OnListBoxKeyPress(object sender, KeyPressEventArgs e)
        {
            OnKeyPress(e);
        }

        private void OnListBoxMouseLeave(object sender, EventArgs e)
        {
            OnMouseLeave(e);
        }

        private Padding GetHeaderPadding(ContentAlignment textAlign)
        {
            switch (textAlign)
            {
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.TopLeft:
                case ContentAlignment.BottomLeft:
                    return new Padding(HeaderSidePadding, 0, 0, 0);

                case ContentAlignment.MiddleRight:
                case ContentAlignment.TopRight:
                case ContentAlignment.BottomRight:
                    return new Padding(0, 0, HeaderSidePadding, 0);

                default:
                    return new Padding(0);
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnwireEvents();
            }

            base.Dispose(disposing);
        }

        private void UnwireEvents()
        {
            if (_listBox == null)
                return;

            _listBox.SelectedIndexChanged -= OnListBoxSelectedIndexChanged;
            _listBox.ItemsReordered -= OnListBoxItemsReordered;
            _listBox.MouseMove -= OnListBoxMouseMove;
            _listBox.MouseLeave -= OnListBoxMouseLeave;
            _listBox.Click -= OnListBoxClick;
            _listBox.DoubleClick -= OnListBoxDoubleClick;
            _listBox.MouseClick -= OnListBoxMouseClick;
            _listBox.MouseDoubleClick -= OnListBoxMouseDoubleClick;
            _listBox.MouseDown -= OnListBoxMouseDown;
            _listBox.MouseUp -= OnListBoxMouseUp;
            _listBox.KeyDown -= OnListBoxKeyDown;
            _listBox.KeyUp -= OnListBoxKeyUp;
            _listBox.KeyPress -= OnListBoxKeyPress;
        }
    }
}