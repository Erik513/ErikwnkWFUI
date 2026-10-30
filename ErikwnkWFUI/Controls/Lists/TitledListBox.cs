using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A <see cref="ListBox"/> with an optional header bar above it - the
    /// same list box in every other way: every member of <see cref="ListBox"/>
    /// (and the usual ones of the standard list box) is here too and works on
    /// the inner list box (see <see cref="InnerListBox"/>, for anything not
    /// offered here).
    /// </summary>
    /// <remarks>
    /// Of the events of the list, the ones for the mouse (click, double click, move, down, up, leave), the keys, the selection,
    /// the data source and its members, the reordering and <c>DrawItem</c> are passed on, with this control as the sender
    /// (the places of the mouse in its own coordinates). Others - <c>MouseWheel</c>, <c>GotFocus</c> and <c>LostFocus</c> among
    /// them - are on <see cref="InnerListBox"/>.
    /// </remarks>
    // In two files: this one (the title bar, the events, the layout) and TitledListBox.Members.cs (the members of the list box that are
    // passed on to the list inside).
    public partial class TitledListBox : UserControl
    {
        private const int DefaultHeaderHeight = 30;
        private const int HeaderSidePadding = 10;

        private readonly System.Windows.Forms.Panel _headerPanel;
        private readonly Label _titleLabel;
        private readonly ListBox _listBox;

        /// <summary>Raised when the selected item changes (the same event of the list inside, with this control as the sender).</summary>
        public event EventHandler SelectedIndexChanged;
        /// <summary>Raised after a drag-to-reorder moves an item (see <see cref="ListBox.ItemsReordered"/>).</summary>
        public event EventHandler ItemsReordered;

        // These events of the list box say this control is the sender, like the ones above.
        /// <summary>Raised when the selected value changes.</summary>
        public event EventHandler SelectedValueChanged;
        /// <summary>Raised when the data source changes.</summary>
        public event EventHandler DataSourceChanged;
        /// <summary>Raised when the display member changes.</summary>
        public event EventHandler DisplayMemberChanged;
        /// <summary>Raised when the value member changes.</summary>
        public event EventHandler ValueMemberChanged;
        /// <summary>Raised after an item is painted, to paint on top of it (see <see cref="ListBox"/>).</summary>
        public event DrawItemEventHandler DrawItem;

        /// <summary>Makes a list with no title bar, in the look of <see cref="ListBox"/>; <c>UIStyles.TitledListBoxes</c> makes the usual ones.</summary>
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
            ThemeWatcher.Attach(this, OnThemeChanged);

            Title = headerTitle;
            HeaderTextAlign = headerTextAlign;
            ApplyBorderColor();

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

        /// <summary>The title in the bar above the list; the bar is not shown while it is empty.</summary>
        [DefaultValue("")]
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

        /// <summary>Height of the title bar in pixels (30 at 96 dpi by default, more on a screen with more dots per inch).</summary>
        public int HeaderHeight
        {
            get { return _headerPanel.Height; }
            set
            {
                _headerHeightWasSet = true;
                _headerPanel.Height = Math.Max(0, value);
            }
        }

        private bool _headerHeightWasSet;

        // The colors of the title bar follow the theme until they are set.
        private readonly ThemeColor _headerBackColor = new ThemeColor(() => UIColors.BackgroundDark);
        private readonly ThemeColor _headerForeColor = new ThemeColor(() => UIColors.TextPrimary);

        // The theme or the accent changed while the control is shown: the title bar that was not colored by hand
        // follows (the list inside follows by itself).
        private void OnThemeChanged()
        {
            ApplyHeaderColors();
            ApplyBorderColor();
            Invalidate(true);
        }

        /// <summary>The color of the frame around the title and the list: the one of <see cref="InnerListBox"/> (the neutral gray border color in every variant; set it for another color).</summary>
        public Color BorderColor
        {
            get { return _listBox.BorderColor; }
            set
            {
                _listBox.BorderColor = value;
                ApplyBorderColor();
            }
        }

        // The frame is this control's own color behind the title and the list (see ConfigureControl).
        internal void ApplyBorderColor()
        {
            BackColor = _listBox.BorderColor;
        }

        private void ApplyHeaderColors()
        {
            _headerPanel.BackColor = _headerBackColor.Value;
            _titleLabel.ForeColor = _headerForeColor.Value;
        }

        // A measure of 96 dpi in the pixels of this screen.
        private int Px(int value96)
        {
            return Dpi.Scale(this, value96, DpiOverride);
        }

        // A seam for the tests: pretends another screen (0 is the real one).
        internal int DpiOverride;

        /// <inheritdoc/>
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);

            if (!_headerHeightWasSet)
                _headerPanel.Height = Px(DefaultHeaderHeight);

            _titleLabel.Padding = GetHeaderPadding(_titleLabel.TextAlign);
        }

        /// <summary>Background of the title bar. The default is the theme's dark background and stays so whatever color set the list was made with: only setting it changes it. <see cref="Color.Empty"/> gives the default back.</summary>
        public Color HeaderBackColor
        {
            get { return _headerBackColor.Value; }
            set
            {
                SetThemeColor(_headerBackColor, value);
                ApplyHeaderColors();
            }
        }

        /// <summary>Text color of the title. The default is the theme's primary text color; only setting it changes it. <see cref="Color.Empty"/> gives the default back.</summary>
        public Color HeaderForeColor
        {
            get { return _headerForeColor.Value; }
            set
            {
                SetThemeColor(_headerForeColor, value);
                ApplyHeaderColors();
            }
        }

        private static void SetThemeColor(ThemeColor color, Color value)
        {
            if (value.IsEmpty)
                color.Reset();
            else
                color.Set(value);
        }

        /// <summary>Font of the title (the library's title font by default).</summary>
        public Font HeaderFont
        {
            get { return _titleLabel.Font; }
            set { _titleLabel.Font = value; }
        }

        /// <summary>Where the title stands in its bar (left and centered vertically by default).</summary>
        [DefaultValue(ContentAlignment.MiddleLeft)]
        public ContentAlignment HeaderTextAlign
        {
            get { return _titleLabel.TextAlign; }
            set
            {
                _titleLabel.TextAlign = value;
                _titleLabel.Padding = GetHeaderPadding(value);
            }
        }

        // The designer writes a color, the height or the font of the title bar only where it was set (the defaults follow the theme and the screen,
        // they are not stored); Reset gives the default back.
        private bool ShouldSerializeHeaderBackColor()
        {
            return _headerBackColor.IsSet;
        }

        private void ResetHeaderBackColor()
        {
            HeaderBackColor = Color.Empty;
        }

        private bool ShouldSerializeHeaderForeColor()
        {
            return _headerForeColor.IsSet;
        }

        private void ResetHeaderForeColor()
        {
            HeaderForeColor = Color.Empty;
        }

        private bool ShouldSerializeHeaderHeight()
        {
            return _headerHeightWasSet;
        }

        private void ResetHeaderHeight()
        {
            _headerHeightWasSet = false;
            _headerPanel.Height = Px(DefaultHeaderHeight);
        }

        private bool ShouldSerializeHeaderFont()
        {
            return !_titleLabel.Font.Equals(UIFonts.Title);
        }

        private void ResetHeaderFont()
        {
            _titleLabel.Font = UIFonts.Title;
        }

        private bool ShouldSerializeBorderColor()
        {
            return InnerShouldSerialize("BorderColor");
        }

        private void ResetBorderColor()
        {
            InnerReset("BorderColor");
            ApplyBorderColor();
        }

        private void ConfigureControl()
        {
            DoubleBuffered = true;
            // The frame is the one pixel of this control around the title and the list: its own color shows there.
            BackColor = UIColors.BorderMedium;
            Margin = new Padding(0);
            Padding = new Padding(1);
        }

        private System.Windows.Forms.Panel CreateHeaderPanel()
        {
            return new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = Px(DefaultHeaderHeight),
                BackColor = _headerBackColor.Value,
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
                ForeColor = _headerForeColor.Value,
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
                Margin = new Padding(0),
                BorderStyle = BorderStyle.None
            };
        }

        // The events of the list reach the ones on this control with this control as the sender, and the places of the
        // mouse in this control's own coordinates (the list is below the title bar). The list is a child of this control
        // and goes with it, so nothing needs to be taken off again.
        private void WireEvents()
        {
            _listBox.SelectedIndexChanged += (s, e) => SelectedIndexChanged?.Invoke(this, e);
            _listBox.ItemsReordered += (s, e) => ItemsReordered?.Invoke(this, e);
            _listBox.SelectedValueChanged += (s, e) => SelectedValueChanged?.Invoke(this, e);
            _listBox.DataSourceChanged += (s, e) => DataSourceChanged?.Invoke(this, e);
            _listBox.DisplayMemberChanged += (s, e) => DisplayMemberChanged?.Invoke(this, e);
            _listBox.ValueMemberChanged += (s, e) => ValueMemberChanged?.Invoke(this, e);
            _listBox.DrawItem += (s, e) => DrawItem?.Invoke(this, e);

            _listBox.MouseMove += (s, e) => OnMouseMove(InOwnCoordinates(e));
            _listBox.MouseClick += (s, e) => OnMouseClick(InOwnCoordinates(e));
            _listBox.MouseDoubleClick += (s, e) => OnMouseDoubleClick(InOwnCoordinates(e));
            _listBox.MouseDown += (s, e) => OnMouseDown(InOwnCoordinates(e));
            _listBox.MouseUp += (s, e) => OnMouseUp(InOwnCoordinates(e));
            _listBox.MouseLeave += (s, e) => OnMouseLeave(e);
            _listBox.Click += (s, e) => OnClick(e);
            _listBox.DoubleClick += (s, e) => OnDoubleClick(e);

            _listBox.KeyDown += (s, e) => OnKeyDown(e);
            _listBox.KeyUp += (s, e) => OnKeyUp(e);
            _listBox.KeyPress += (s, e) => OnKeyPress(e);
        }

        private MouseEventArgs InOwnCoordinates(MouseEventArgs e)
        {
            return new MouseEventArgs(e.Button, e.Clicks, e.X + _listBox.Left, e.Y + _listBox.Top, e.Delta);
        }

        private Padding GetHeaderPadding(ContentAlignment textAlign)
        {
            switch (textAlign)
            {
                case ContentAlignment.MiddleLeft:
                case ContentAlignment.TopLeft:
                case ContentAlignment.BottomLeft:
                    return new Padding(Px(HeaderSidePadding), 0, 0, 0);

                case ContentAlignment.MiddleRight:
                case ContentAlignment.TopRight:
                case ContentAlignment.BottomRight:
                    return new Padding(0, 0, Px(HeaderSidePadding), 0);

                default:
                    return new Padding(0);
            }
        }
    }
}
