using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// An owner-drawn <see cref="System.Windows.Forms.ListBox"/> with
    /// per-item icons, optional drag-to-reorder (a drag handle appears on
    /// the right of each item), enumeration numbers, and fully overridable
    /// item colors - a themed alternative to the native list box for
    /// anything beyond plain text rows. Use <see cref="TitledListBox"/>
    /// instead if you also want a header bar above the list.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What of the standard list box works as it does there:</b> <c>Items</c>, <c>DataSource</c>,
    /// <c>DisplayMember</c>, <c>ValueMember</c>, <c>FormatString</c> (they decide the text of an item),
    /// <c>SelectedIndex/Item/Value</c> and their events, <c>SelectionMode</c> (one, none or several;
    /// clicking the selected item clears it only in a single selection), <c>Sorted</c> and <c>MultiColumn</c> (both
    /// without reordering), <c>IntegralHeight</c>, <c>ScrollAlwaysVisible</c>, <c>TopIndex</c>, the finding and selecting
    /// methods, <c>ForeColor</c> (the text color of the items, the same as <see cref="ItemForeColor"/>),
    /// <c>Enabled</c> (every item shows disabled) and the <c>DrawItem</c> event (raised after the item is painted,
    /// to paint on top of it).
    /// </para>
    /// <para>
    /// <b>What is deliberately not followed:</b> <c>DrawMode</c> (always owner-drawn, fixed height - the colors and
    /// icons depend on it), <c>BackColor</c> is the color behind and below the items (the items have
    /// <see cref="ItemBackColor"/>), <c>BackgroundImage</c>, the typing of the first letters of an item (it looks at the text of the item - its
    /// <c>ToString()</c> or <c>DisplayMember</c> - not at <see cref="DisplayTextProvider"/>), <c>UseTabStops</c> and
    /// <c>CustomTabOffsets</c> (the text is
    /// one line of drawn text), <c>HorizontalScrollbar</c> and <c>HorizontalExtent</c> (a long text wraps and ends in an
    /// ellipsis instead of running out of the list) and <c>RightToLeft</c> (not supported in the library).
    /// <see cref="DisplayTextProvider"/> (a function) comes before <c>DisplayMember</c>.
    /// </para>
    /// <para>
    /// Named the same as its own base class, same as
    /// <see cref="Controls.DataGridView"/> - the base type reference below
    /// stays fully qualified so the class doesn't try to inherit from
    /// itself. Go through <see cref="UIStyles.ListBoxes.CreateStandard"/> to
    /// get one without ever having to spell out
    /// <c>ErikwnkWFUI.Controls.ListBox</c>.
    /// </para>
    /// </remarks>
    public partial class ListBox : System.Windows.Forms.ListBox
    {
        // The measures at 96 dpi: they are scaled with the screen (see Px).
        private const int BaseItemHeight = 35;
        private const int DragHandleWidth = 30;
        private const int DragHandleHitAreaPadding = 5;
        private const int ItemIconSize = 22;
        private const int ItemIconMargin = 8;

        // How much of the selection color covers the row: a gray (no hue to stand out by) more than a color. A little
        // more than the list view has, a list of single lines asks for a selection that is seen at a glance.
        private const int SelectionAlphaGray = 175;
        private const int SelectionAlphaColor = 120;

        private static readonly StringFormat CenterLeftFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };

        // The colors that follow the theme until they are set (see ThemeWatcher).
        private readonly ThemeColor _itemBackColor = new ThemeColor(() => UIColors.BackgroundMedium);
        private readonly ThemeColor _itemForeColor = new ThemeColor(() => UIColors.TextPrimary);
        private readonly ThemeColor _disabledBackColor = new ThemeColor(() => UIColors.BackgroundDarkElevated);
        private bool _backColorWasSet;
        private readonly ThemeColor _selectedBackColor = new ThemeColor(() => UIColors.BorderLight);
        private Color? _selectedForeColor;
        private Color? _hoverBackColor;
        private Color? _dragHandleColor;
        private readonly ThemeColor _dragIndicatorColor = new ThemeColor(() => UIColors.BorderLight);
        private Color? _disabledForeColor;

        private int _itemHeight = BaseItemHeight;
        private bool _itemHeightWasSet;
        private int _hoverIndex = -1;

        private bool _allowReorder = true;
        private bool _showEnumeration;
        private int _dragIndex = -1;
        private bool _isDragging;
        private Point _dragStartPoint;
        private int _dragInsertPosition = -1;
        private int _pendingToggleDeselectIndex = -1;

        // The item that was pressed, as it stood BEFORE the native list handled the press (it selects the item
        // and, where only a part of it shows, scrolls it into view - after that the same point can lie on another
        // item or on the room below the items).
        private bool _pressedKnown;
        private int _pressedIndex = -1;
        private Rectangle _pressedRectangle;

        private Func<object, string> _displayTextProvider;
        private Func<object, Image> _iconProvider;
        private Func<object, bool> _isItemDisabled;

        /// <summary>Raised after a drag-to-reorder moves an item to a new position (see <see cref="AllowReorder"/>).</summary>
        public event EventHandler ItemsReordered;

        /// <summary>
        /// Computes each item's display text from the item itself, e.g.
        /// <c>o => ((Track)o).Title</c>. Takes priority over
        /// <c>DisplayMember</c>; without it the text is what the standard list
        /// box makes of an item (<c>DisplayMember</c>, <c>FormatString</c>, or
        /// <c>item.ToString()</c>).
        /// </summary>
        public Func<object, string> DisplayTextProvider
        {
            get => _displayTextProvider;
            set
            {
                _displayTextProvider = value;
                Invalidate();
            }
        }

        /// <summary>Prefixes each item with its 1-based position ("1.", "2.", ...).</summary>
        public bool ShowEnumeration
        {
            get => _showEnumeration;
            set
            {
                _showEnumeration = value;
                Invalidate();
            }
        }

        /// <summary>Optional per-item icon, drawn to the left of the display text.</summary>
        public Func<object, Image> IconProvider
        {
            get => _iconProvider;
            set
            {
                _iconProvider = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Optional per-item disabled <b>look</b> (grayed out): only how the item is drawn. The item can still be
        /// clicked, selected and dragged like any other. Independent of the whole control's own <see cref="Control.Enabled"/>.
        /// </summary>
        public Func<object, bool> IsItemDisabled
        {
            get => _isItemDisabled;
            set
            {
                _isItemDisabled = value;
                Invalidate();
            }
        }

        // Reordering is for a list of one column whose items are the ones in Items, in the order they were put: with
        // MultiColumn, Sorted (the list puts every item in its own place) or a DataSource (the items cannot be
        // moved) the handle and the drop line are left out.
        private bool CanReorder
        {
            get { return _allowReorder && !MultiColumn && !Sorted && DataSource == null; }
        }

        /// <summary>Whether items can be dragged to reorder them (drag handle on the right of each item) - also controls <see cref="Control.AllowDrop"/>. Not with <c>MultiColumn</c>, <c>Sorted</c> or a <c>DataSource</c>.</summary>
        public bool AllowReorder
        {
            get => _allowReorder;
            set
            {
                _allowReorder = value;

                if (value)
                {
                    // See the constructor's own remarks on why enabling
                    // this is apartment-state gated - disabling it
                    // (AllowDrop = false, the other branch below) doesn't
                    // register anything, so it has no such risk.
                    DragDropSupport.EnableDropIfSta(this);
                }
                else
                {
                    AllowDrop = false;
                }

                Invalidate();
            }
        }

        /// <summary>Row height in pixels. Replaces the inherited <see cref="System.Windows.Forms.ListBox.ItemHeight"/> (so the internal default stays in sync, and a height of 0 is not accepted).</summary>
        public new int ItemHeight
        {
            get => _itemHeight;
            set
            {
                // The native ItemHeight setter itself only rejects a
                // negative value, not zero - but EnsureItemVisible divides
                // ClientSize.Height by this during a drag, which a zero
                // height would turn into a DivideByZeroException the
                // moment a user actually dragged an item to reorder it.
                _itemHeightWasSet = true;
                _itemHeight = Math.Max(1, value);
                base.ItemHeight = _itemHeight;
                Invalidate();
            }
        }

        /// <summary>
        /// Color of the horizontal line shown at the drop position while
        /// dragging an item to reorder it. Defaults to a fixed, neutral
        /// gray (<see cref="UIColors.BorderLight"/>), matching this
        /// control's own <see cref="SelectedBackColor"/> - set this
        /// explicitly (e.g. to <see cref="UIColors.Primary"/>) for an
        /// accent-colored indicator instead.
        /// </summary>
        public Color DragIndicatorColor
        {
            get => _dragIndicatorColor.Value;
            set
            {
                _dragIndicatorColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Text color for an item where <see cref="IsItemDisabled"/> returns true, and for every item of a list that
        /// is disabled as a whole. Left alone it is worked out from the background the text is on (light or dark,
        /// blended toward it); set to <see cref="Color.Empty"/> to have it worked out again.
        /// </summary>
        public Color DisabledForeColor
        {
            get => _disabledForeColor ?? UIColors.TextDisabled;
            set
            {
                _disabledForeColor = value.IsEmpty ? (Color?)null : value;
                Invalidate();
            }
        }

        /// <summary>Background color for an item where <see cref="IsItemDisabled"/> returns true. (The selected item of a list that is disabled as a whole shows the selection color dimmed.)</summary>
        public Color DisabledBackColor
        {
            get => _disabledBackColor.Value;
            set
            {
                _disabledBackColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>
        /// The color of the selection. The selected item shows it as a see-through tint over its row (a gray is
        /// stronger than a color, as in <see cref="ListView.SelectionOverlayColor"/>, but a little stronger still), the
        /// text stays the text of the item. Defaults to a fixed, neutral gray (<see cref="UIColors.BorderLight"/>), not the current accent.
        /// <c>UIStyles.ListBoxes.CreatePrimary</c> (or <c>CreateStandard</c> with a color set) sets it to the accent
        /// (the color of the set), the same split as <see cref="ReadOnlyDataGridView.SelectionBackColor"/> and
        /// <see cref="ListView.SelectionOverlayColor"/>.
        /// </summary>
        public Color SelectedBackColor
        {
            get => _selectedBackColor.Value;
            set
            {
                _selectedBackColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Text color of the selected item. Left alone it is the text color of the other items (the selection is only
        /// a tint over the row); set to a color to have another one on the selected item, <see cref="Color.Empty"/> to
        /// have the item color again.
        /// </summary>
        public Color SelectedForeColor
        {
            get => _selectedForeColor ?? _itemForeColor.Value;
            set
            {
                _selectedForeColor = value.IsEmpty ? (Color?)null : value;
                Invalidate();
            }
        }

        /// <summary>Background color of a normal (not selected/hovered/disabled) item. Odd/even rows alternate between this and a slightly darker shade of it.</summary>
        public Color ItemBackColor
        {
            get => _itemBackColor.Value;
            set
            {
                _itemBackColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>Text color of a normal (not selected/disabled) item. The same as <see cref="ForeColor"/>.</summary>
        public Color ItemForeColor
        {
            get => _itemForeColor.Value;
            set
            {
                _itemForeColor.Set(value);
                base.ForeColor = value;
                Invalidate();
            }
        }

        /// <summary>The text color of the items (see <see cref="ItemForeColor"/>), as in the standard list box.</summary>
        public override Color ForeColor
        {
            get => base.ForeColor;
            set
            {
                base.ForeColor = value;

                if (!value.IsEmpty)
                    _itemForeColor.Set(value);

                Invalidate();
            }
        }

        /// <summary>
        /// Background color of the item currently under the mouse (not shown while the list is disabled or an item is
        /// being dragged). Left alone it is the row's own color a little way toward the text color - a step lighter on
        /// the dark theme, darker on the light one, so it shows on both (this reads the color of an even row); set to a
        /// color to have that one on every row, <see cref="Color.Empty"/> to have it worked out again.
        /// </summary>
        public Color HoverBackColor
        {
            get => _hoverBackColor ?? GetHoverBackColor(ItemBackColor);
            set
            {
                _hoverBackColor = value.IsEmpty ? (Color?)null : value;
                Invalidate();
            }
        }

        /// <summary>
        /// Color of the drag-handle glyph shown on the right of each item when <see cref="AllowReorder"/> is true.
        /// Left alone each item has the handle in a quiet version of its own text color (so it reads on a selected
        /// item in any color too); set to a color to have that one on every item, <see cref="Color.Empty"/> to
        /// have it worked out again.
        /// </summary>
        public Color DragHandleColor
        {
            get => _dragHandleColor ?? UIColors.TextTertiary;
            set
            {
                _dragHandleColor = value.IsEmpty ? (Color?)null : value;
                Invalidate();
            }
        }

        public ListBox()
        {
            ThemeWatcher.Attach(this, OnThemeChanged);
            NativeControlTheme.Track(this); // the scroll bars are the system's own

            DrawMode = DrawMode.OwnerDrawFixed;
            base.ItemHeight = _itemHeight;
            base.BackColor = UIColors.BackgroundDark;
            base.ForeColor = ItemForeColor;
            BorderStyle = BorderStyle.None;
            Font = UIFonts.Normal;
            IntegralHeight = false;

            // AllowDrop registers this control as an OLE drop target
            // (needed here since TryStartDrag's own DoDragDrop below drops
            // back onto this same control) - that registration needs an
            // STA thread (true for any real WinForms UI thread), and was
            // confirmed on DataGridView's/ListView's own identical fix to
            // silently block for many seconds on an MTA one (e.g. a test
            // harness thread with no message loop) instead of throwing.
            // _allowReorder is always true here (nothing sets it to false
            // before this line runs), so this always attempts to enable it,
            // same as the plain assignment this replaced - just skipped
            // entirely off STA, where the drag couldn't have worked anyway.
            if (_allowReorder)
            {
                DragDropSupport.EnableDropIfSta(this);
            }

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint,
                true);

            EnableDoubleBuffering();

            UpdateStyles();
        }

        /// <summary>The color behind and below the items (the items have <see cref="ItemBackColor"/>); follows the theme until it is set.</summary>
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                _backColorWasSet = !value.IsEmpty;
                base.BackColor = value;
            }
        }

        // The look of a color set (what UIStyles.ListBoxes.CreateStandard(colors) and CreatePrimary make): the selection and
        // the drop line take its hover color - read again each time, so the primary set follows the accent.
        internal void FollowColors(Func<UIColorSet> colors)
        {
            _selectedBackColor.SetDefault(() => colors().Hover);
            _dragIndicatorColor.SetDefault(() => colors().Hover);
            Invalidate();
        }

        // The theme or the accent changed while the list is shown: what was not set by hand follows, the list is painted anew
        // (the scroll bars follow through NativeControlTheme.Track).
        private void OnThemeChanged()
        {
            if (!_backColorWasSet)
                base.BackColor = UIColors.BackgroundDark;

            if (!_itemForeColor.IsSet)
                base.ForeColor = _itemForeColor.Value;

            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            ApplyDefaultItemHeight();
        }

        // On a screen with more dots per inch the rows are higher: the default height of an item is 35 pixels at 96 dpi.
        // A height that was set stays as it was set.
        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyDefaultItemHeight();
        }

        private void ApplyDefaultItemHeight()
        {
            if (_itemHeightWasSet)
                return;

            _itemHeight = Px(BaseItemHeight);
            base.ItemHeight = _itemHeight;
            Invalidate();
        }

        // A measure of 96 dpi in the pixels of this screen.
        private int Px(int value96)
        {
            return Dpi.Scale(this, value96, DpiOverride);
        }

        // A seam for the tests: pretends another screen (0 is the real one).
        internal int DpiOverride;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            UpdateHoverIndex(e.Location);
            TryStartDrag(e);

            base.OnMouseMove(e);
        }

        // A native ListBox commits its own click-to-select synchronously as
        // part of its native WM_LBUTTONDOWN handling - which happens
        // BEFORE OnMouseDown/MouseDown ever fires, unlike ListView's own
        // toggle-deselect (SysListView32 doesn't commit until mouse-up,
        // which is why that one CAN check this from OnMouseDown/a MouseDown
        // handler). Checking SelectedIndex from OnMouseDown here would
        // always see the NEW selection already applied - confirmed live:
        // every plain click toggled straight back off on release, not just
        // a second click on an already-selected item, because by the time
        // OnMouseDown ran, the item just clicked was already "the current
        // selection" regardless of what it was before. Intercepting the
        // raw message here, before base.WndProc forwards it to the native
        // control, is the only point that still sees the pre-click state.

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_LBUTTONDOWN)
            {
                CapturePendingToggleDeselect(m.LParam);
            }

            base.WndProc(ref m);

            // The items move under a mouse that stands still: the hover goes with the item that is there now.
            if ((m.Msg == WM_MOUSEWHEEL || m.Msg == WM_VSCROLL) && IsHandleCreated)
                UpdateHoverIndex(PointToClient(Cursor.Position));
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            // A list that is switched off is not under the mouse any more, and not when it is back on either until the
            // mouse moves again.
            _hoverIndex = -1;

            base.OnEnabledChanged(e);
            Invalidate();
        }

        private void CapturePendingToggleDeselect(IntPtr lParam)
        {
            int x = unchecked((short)(long)lParam);
            int y = unchecked((short)((long)lParam >> 16));
            int index = IndexFromPoint(new Point(x, y));

            _pressedKnown = true;
            _pressedIndex = index;
            _pressedRectangle = index != -1 ? GetItemRectangle(index) : Rectangle.Empty;

            // Only a list of one selected item toggles: in a multiple selection a click on a selected item means
            // something else (keep, extend, take out one), and the native list does that itself.
            if (SelectionMode != SelectionMode.One)
            {
                _pendingToggleDeselectIndex = -1;
                return;
            }

            _pendingToggleDeselectIndex = index != -1 && index == SelectedIndex ? index : -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // What was under the mouse when it was pressed - not what is there now: the native list may have scrolled
            // the pressed item into view meanwhile.
            int index = _pressedKnown ? _pressedIndex : IndexFromPoint(e.Location);
            Rectangle itemRectangle = _pressedKnown ? _pressedRectangle : (index != -1 ? GetItemRectangle(index) : Rectangle.Empty);
            _pressedKnown = false;

            if (e.Button == MouseButtons.Left && TryPrepareDrag(index, itemRectangle, e.Location))
            {
                base.OnMouseDown(e);
                return;
            }

            // Below the items: the selection goes (only for the left button - the others leave it alone).
            if (index == -1 && e.Button == MouseButtons.Left)
            {
                ClearSelected();
                _dragIndex = -1;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            ResetDragState();

            int pendingIndex = _pendingToggleDeselectIndex;
            _pendingToggleDeselectIndex = -1;

            // Only deselects if the button actually came back up on the
            // same item that was already selected going in (a toggle is only
            // armed in a single selection, see CapturePendingToggleDeselect) -
            // there is no drag-select gesture to tell it from, unlike
            // ListView's equivalent toggle-deselect, so a plain "still the
            // same item" check is enough here.
            if (pendingIndex >= 0 && e.Button == MouseButtons.Left &&
                IndexFromPoint(e.Location) == pendingIndex && SelectedIndex == pendingIndex)
            {
                ClearSelected();
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_isDragging)
            {
                base.OnMouseLeave(e);
                return;
            }

            int oldHoverIndex = _hoverIndex;
            _hoverIndex = -1;

            InvalidateItem(oldHoverIndex);

            base.OnMouseLeave(e);
        }

        /// <summary>Clears the selection (every selected item) and repaints; does nothing when nothing is selected. Hides (does not override) the inherited <see cref="System.Windows.Forms.ListBox.ClearSelected"/>.</summary>
        public new void ClearSelected()
        {
            if (SelectedIndex == -1)
                return;

            SelectedIndex = -1;
            Invalidate();
        }

        private void UpdateHoverIndex(Point location)
        {
            if (_isDragging)
                return;

            int index = IndexFromPoint(location);

            if (_hoverIndex == index)
                return;

            int oldHoverIndex = _hoverIndex;
            _hoverIndex = index;

            InvalidateItem(oldHoverIndex);
            InvalidateItem(_hoverIndex);
        }

        private int GetDragIndicatorYPosition()
        {
            int yPosition;

            if (_dragInsertPosition == 0)
            {
                yPosition = 0;
            }
            else if (_dragInsertPosition >= Items.Count)
            {
                yPosition = Items.Count > 0
                    ? GetItemRectangle(Items.Count - 1).Bottom
                    : 0;
            }
            else
            {
                yPosition = GetItemRectangle(_dragInsertPosition).Top;
            }

            return Math.Max(0, yPosition);
        }

        private void InvalidateItem(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;

            Invalidate(GetItemRectangle(index));
        }

        private void EnableDoubleBuffering()
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(this, true, null);
        }
    }
}
