using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>How <see cref="Controls.ListView"/> confirms a Ctrl+C/Ctrl+Shift+C copy.</summary>
    public enum CopyConfirmationStyle
    {
        /// <summary>A <see cref="Forms.ToastForm"/> popup (the original, default behavior).</summary>
        Toast,
        /// <summary>A brief standard <see cref="ToolTip"/> instead - for apps that don't want ToastForm's separate popup window.</summary>
        ToolTip,
        /// <summary>No visual confirmation at all - the copy still happens, just silently.</summary>
        None
    }

    /// <summary>
    /// A dark-themed, multi-column ListView (Details view). Row selection -
    /// click, Ctrl+click, Shift+click, rubber-band drag with auto-scroll -
    /// is entirely native, left to
    /// <see cref="System.Windows.Forms.ListView.MultiSelect"/> and
    /// <see cref="System.Windows.Forms.ListView.SelectedItems"/>. Only the
    /// visuals (row/header colors, fonts, the selection overlay) are this
    /// control's own.
    /// <list type="bullet">
    /// <item>Hovering a cell whose text is wider than its column shows the full text in a tooltip.</item>
    /// </list>
    /// Ctrl+C copies every selected row as tab-separated columns; Ctrl+Shift+C
    /// does the same with a leading row of column names. Both also put an
    /// HTML table on the clipboard alongside the plain text, so apps that
    /// understand it (Word, Outlook, browsers, Excel, ...) paste an actual
    /// table instead of raw tabs. The right-click menu shows "Copy
    /// selection"/"Copy all", each with a submenu for the plain action or
    /// "As table". Every copy is confirmed with a <see cref="Forms.ToastForm"/>.
    /// </summary>
    /// <remarks>
    /// Named the same as its own base class, like <see cref="Controls.DataGridView"/>
    /// and <see cref="Controls.ListBox"/> - the base type reference below
    /// stays fully qualified so the class doesn't try to inherit from itself.
    /// </remarks>
    public class ListView : System.Windows.Forms.ListView
    {
        private const int DefaultMinimumColumnWidth = 40;

        private int _headerHeight = 24;
        private int _pendingToggleDeselectItemIndex = -1;
        private readonly Timer _toggleDeselectSettleTimer;
        private int _toggleDeselectWatchIndex = -1;
        private bool _isRowRangeDragging;
        private int _rowRangeDragAnchorIndex = -1;
        private int _rowRangeDragLastAppliedIndex = -1;
        private int _rowRangeAutoScrollDirection;
        private readonly Timer _rowRangeDragPollTimer;
        private readonly OutsideClickDeselectFilter _outsideClickDeselectFilter;

        private Color _rowBackColor = UIColors.BackgroundMedium;
        private Color _alternateRowBackColor;
        private Color _rowForeColor = UIColors.TextPrimary;
        // Translucent gray so a row's own color still shows through under a
        // selection; CreatePrimary switches this to the accent. Alpha is
        // higher than UIColors.Selection's 60 - gray blends into a row's
        // own shade much more than blue does at the same opacity.
        private readonly ThemeColor _selectionOverlayColor = new ThemeColor(() => Color.FromArgb(130, UIColors.BorderLight));
        private Color _headerBackColor = UIColors.BackgroundDarkElevated;
        private Color _headerForeColor = UIColors.TextTertiary;
        private int _minimumColumnWidth = DefaultMinimumColumnWidth;
        private readonly ColumnFeatureSwitch _columnResizing = new ColumnFeatureSwitch();
        private readonly ColumnFeatureSwitch _columnReordering = new ColumnFeatureSwitch();
        // TextSecondary, not BorderLight - a passive grid line can afford
        // to be subtle, but this marks where a dragged column will land
        // and needs to actually stand out. BorderLight against the header
        // background measured under 3:1 contrast, barely brighter than the
        // ordinary divider line right next to it.
        private readonly ThemeColor _columnReorderIndicatorColor = new ThemeColor(() => UIColors.TextSecondary);
        private readonly ThemeColor _borderColor = new ThemeColor(() => UIColors.BorderMedium);
        // Matches Controls.ContextMenuStrip's own CreateStandard default.
        // BuildContextMenu reads this once, when it builds the menu in the
        // constructor. The property setter below also pushes a later
        // change straight into that already-built menu (found via the
        // inherited ContextMenuStrip property) - needed because
        // UIListViewFactory.CreatePrimary sets this through an object
        // initializer, which only runs after the constructor already built
        // the menu.
        private readonly ThemeColor _contextMenuSelectionColor = new ThemeColor(() => UIColors.BorderLight);
        private bool _isDraggingColumn;
        private int _dragColumnIndex = -1;
        private int _dragInsertBeforeDisplayIndex = -1;
        private Font _headerFontOverride;
        private ImageList _rowHeightImageList;
        private HeaderInputSubclass _headerInputSubclass;

        private readonly ToolTip _cellToolTip = new ToolTip { InitialDelay = 400, ReshowDelay = 100, AutoPopDelay = 8000, ShowAlways = true };
        private int _toolTipRow = -1;
        private int _toolTipDisplayColumn = -1;

        private readonly HashSet<int> _nonSortableColumns = new HashSet<int>();
        private readonly Dictionary<ColumnHeader, IComparer> _columnSortComparers = new Dictionary<ColumnHeader, IComparer>();
        private List<ListViewItem> _originalOrder;
        private int _sortedColumnIndex = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private bool _suppressNextColumnClickSort;

        /// <summary>No column can be resized narrower than this.</summary>
        public int MinimumColumnWidth
        {
            get { return _minimumColumnWidth; }
            set { _minimumColumnWidth = Math.Max(1, value); }
        }

        /// <summary>How a Ctrl+C/Ctrl+Shift+C copy is confirmed. Defaults to <see cref="CopyConfirmationStyle.Toast"/> (unchanged from before this existed).</summary>
        public CopyConfirmationStyle CopyConfirmation { get; set; } = CopyConfirmationStyle.Toast;

        /// <summary>Background color of a normal (not selected) row. Odd/even rows alternate between this and a slightly darker shade of it.</summary>
        public Color RowBackColor
        {
            get { return _rowBackColor; }
            set
            {
                _rowBackColor = value;
                _alternateRowBackColor = UIColors.Darken(value, 5);
                Invalidate();
            }
        }

        /// <summary>Text color of a normal (not selected) row.</summary>
        public Color RowForeColor
        {
            get { return _rowForeColor; }
            set
            {
                _rowForeColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Painted on top of a selected cell instead of replacing its
        /// background, so a row's own color (e.g. severity) still shows
        /// through. Defaults to translucent gray;
        /// <see cref="Factories.UIListViewFactory.CreatePrimary"/> sets it
        /// to <see cref="UIColors.Selection"/> for an accent-colored look.
        /// </summary>
        public Color SelectionOverlayColor
        {
            get { return _selectionOverlayColor.Value; }
            set
            {
                _selectionOverlayColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Color of the line shown while dragging a column to reorder it.
        /// Defaults to a fixed neutral gray, same pattern as
        /// <see cref="SelectionOverlayColor"/>. Column reordering is
        /// hand-rolled instead of using
        /// <see cref="System.Windows.Forms.ListView.AllowColumnReorder"/>,
        /// since the native drag line is comctl32 chrome with no way to
        /// recolor or reliably intercept.
        /// </summary>
        public Color ColumnReorderIndicatorColor
        {
            get { return _columnReorderIndicatorColor.Value; }
            set { _columnReorderIndicatorColor.Set(value); }
        }

        /// <summary>
        /// Color of each column header cell's own border. Defaults to
        /// <see cref="UIColors.BorderMedium"/> regardless of the current
        /// accent; <see cref="Factories.UIListViewFactory.CreatePrimary"/>
        /// sets this to <see cref="UIColors.Primary"/> instead.
        /// </summary>
        public Color BorderColor
        {
            get { return _borderColor.Value; }
            set
            {
                _borderColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Background of a hovered/selected item in this ListView's own
        /// right-click context menu. Defaults to a fixed neutral gray,
        /// matching <see cref="Controls.ContextMenuStrip"/>'s own
        /// CreateStandard default; <see cref="Factories.UIListViewFactory.CreatePrimary"/>
        /// sets this to <see cref="UIColors.Primary"/> instead, same pattern
        /// as <see cref="BorderColor"/>.
        /// </summary>
        public Color ContextMenuSelectionColor
        {
            get { return _contextMenuSelectionColor.Value; }
            set
            {
                _contextMenuSelectionColor.Set(value);

                if (ContextMenuStrip is ContextMenuStrip menu)
                {
                    menu.SelectionBackColor = value;
                }
            }
        }

        /// <summary>Background color of the column header row.</summary>
        public Color HeaderBackColor
        {
            get { return _headerBackColor; }
            set
            {
                _headerBackColor = value;
                Invalidate();
            }
        }

        /// <summary>Text color of the column header row.</summary>
        public Color HeaderForeColor
        {
            get { return _headerForeColor; }
            set
            {
                _headerForeColor = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Font for the column header row - defaults to a bold version of
        /// <see cref="Control.Font"/> if never set. Setting this to a larger
        /// size is also the way to make the native header band itself
        /// taller (there's no direct "header height" API on the underlying
        /// Win32 header control - it sizes itself from its own font
        /// metrics, same as row height below).
        /// </summary>
        public Font HeaderFont
        {
            // Returns a clone - OnDrawColumnHeader disposes the Font it
            // gets each paint, which would kill a shared instance after
            // the first use.
            get { return _headerFontOverride != null ? (Font)_headerFontOverride.Clone() : new Font(Font, FontStyle.Bold); }
            set
            {
                _headerFontOverride = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Forces every row to this exact height in pixels. Unset (0) lets
        /// rows size themselves from <see cref="Control.Font"/> as usual.
        /// Implemented via the classic Win32 trick of assigning a 1px-wide,
        /// N-tall <c>SmallImageList</c> - the native ListView derives its
        /// row height from the small image list's height when one is set,
        /// since there's no direct row-height API either. Don't also assign
        /// a real <c>SmallImageList</c> for per-item icons while this is
        /// set; the two would fight over the same slot.
        /// </summary>
        public int RowHeight
        {
            get { return _rowHeightImageList?.ImageSize.Height ?? 0; }
            set
            {
                if (value <= 0)
                {
                    _rowHeightImageList = null;
                    SmallImageList = null;
                    return;
                }

                _rowHeightImageList = new ImageList { ImageSize = new Size(1, value) };
                SmallImageList = _rowHeightImageList;
            }
        }

        public ListView()
        {
            View = View.Details;
            FullRowSelect = true;
            HideSelection = true;
            MultiSelect = true;
            // Column reordering is hand-rolled (see HeaderInputSubclass and
            // OnDragOver/OnDragDrop below) instead of using the native
            // drag - see ColumnReorderIndicatorColor's doc comment for why.
            AllowColumnReorder = false;

            // AllowDrop only supports that hand-rolled drag, not an
            // app-facing drop target - see DragDropSupport for the
            // apartment-state gating.
            DragDropSupport.EnableDropIfSta(this);
            BorderStyle = BorderStyle.None;
            BackColor = UIColors.BackgroundDark;
            ForeColor = _rowForeColor;
            Font = UIFonts.Normal;
            // Clickable so ColumnClick actually fires for header sorting
            // (see SortingEnabled). OwnerDraw paints every header pixel
            // regardless, so there's no native "pressed" chrome to worry
            // about.
            HeaderStyle = ColumnHeaderStyle.Clickable;
            OwnerDraw = true;

            _alternateRowBackColor = UIColors.Darken(_rowBackColor, 5);

            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint,
                true);
            DoubleBuffered = true;
            UpdateStyles();

            DrawColumnHeader += OnDrawColumnHeader;
            DrawItem += OnDrawItem;
            DrawSubItem += OnDrawSubItem;
            MouseDown += OnListViewMouseDownForToggleDeselect;
            MouseUp += OnListViewMouseUpForToggleDeselect;
            MouseDown += OnListViewMouseDownForRowRangeDrag;
            MouseUp += OnListViewMouseUpForRowRangeDrag;
            // Only ever running for the duration of one active row-range
            // drag - see OnListViewMouseDownForRowRangeDrag/
            // OnRowRangeDragPollTick.
            _rowRangeDragPollTimer = new Timer { Interval = 40 };
            _rowRangeDragPollTimer.Tick += OnRowRangeDragPollTick;
            ItemSelectionChanged += OnItemSelectionChangedForToggleDeselect;
            // Safety-net only, only ever running for ~1.5s after a toggle-
            // deselect - see OnListViewMouseUpForToggleDeselect for why
            // this exists at all.
            _toggleDeselectSettleTimer = new Timer { Interval = 1500 };
            _toggleDeselectSettleTimer.Tick += OnToggleDeselectSettleTimerTick;
            MouseMove += OnListViewMouseMoveForToolTip;
            MouseLeave += OnListViewMouseLeave;
            KeyDown += OnListViewKeyDown;
            ColumnWidthChanging += OnColumnWidthChanging;

            ContextMenuStrip = BuildContextMenu();

            // Message filter, not a poll, for "a click happened somewhere
            // else" - see OutsideClickDeselectFilter for what counts as
            // outside.
            _outsideClickDeselectFilter = new OutsideClickDeselectFilter(this);
            Application.AddMessageFilter(_outsideClickDeselectFilter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cellToolTip.Dispose();
                _toggleDeselectSettleTimer.Stop();
                _toggleDeselectSettleTimer.Dispose();
                _rowRangeDragPollTimer.Stop();
                _rowRangeDragPollTimer.Dispose();
                Application.RemoveMessageFilter(_outsideClickDeselectFilter);
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Master switch for whether any column can be resized at all.
        /// <see cref="SetColumnResizable"/> still applies per column among
        /// whichever this allows; false overrides everything. Defaults to
        /// true.
        /// </summary>
        public bool AllowColumnResizing
        {
            get => _columnResizing.AllowedByDefault;
            set => _columnResizing.AllowedByDefault = value;
        }

        /// <summary>
        /// Master switch for whether any column can be dragged to reorder
        /// at all. <see cref="SetColumnReorderable"/> still applies per
        /// column among whichever this allows; false overrides everything.
        /// Defaults to true.
        /// </summary>
        public bool AllowColumnReordering
        {
            get => _columnReordering.AllowedByDefault;
            set => _columnReordering.AllowedByDefault = value;
        }

        /// <summary>
        /// Per-column override, independent of <see cref="MinimumColumnWidth"/> -
        /// locks a column at its current width (e.g. a narrow status
        /// column) while others stay resizable.
        /// </summary>
        public void SetColumnResizable(int columnIndex, bool resizable)
        {
            _columnResizing.SetAllowed(columnIndex, resizable);
        }

        public bool IsColumnResizable(int columnIndex)
        {
            return _columnResizing.IsAllowed(columnIndex);
        }

        /// <summary>
        /// Per-column override, independent of <see cref="AllowColumnReordering"/> -
        /// pins a column in place (e.g. a leading "Time" column) while the
        /// rest can still be dragged into a new order.
        /// </summary>
        public void SetColumnReorderable(int columnIndex, bool reorderable)
        {
            _columnReordering.SetAllowed(columnIndex, reorderable);
        }

        public bool IsColumnReorderable(int columnIndex)
        {
            return _columnReordering.IsAllowed(columnIndex);
        }

        /// <summary>
        /// Whether clicking a column header cycles it through ascending,
        /// descending, and original item order - mirrors
        /// <see cref="Controls.ReadOnlyDataGridView.SortingEnabled"/>. Defaults
        /// to true. Turn a single column off instead via
        /// <see cref="SetColumnSortable"/> if only that one shouldn't sort.
        /// </summary>
        public bool SortingEnabled { get; set; } = true;

        /// <summary>
        /// Configurable per column, independent of <see cref="SortingEnabled"/> -
        /// a column can be excluded from sorting (e.g. one showing icons or
        /// actions rather than comparable data) while the rest stay sortable.
        /// </summary>
        public void SetColumnSortable(int columnIndex, bool sortable)
        {
            if (sortable)
            {
                _nonSortableColumns.Remove(columnIndex);
            }
            else
            {
                _nonSortableColumns.Add(columnIndex);
            }
        }

        public bool IsColumnSortable(int columnIndex)
        {
            return SortingEnabled && !_nonSortableColumns.Contains(columnIndex);
        }

        /// <summary>
        /// Registers a custom comparer for a column's header-click sort,
        /// replacing the default (the sorted column's own text, compared
        /// the same "natural" way Windows Explorer's own file listing does -
        /// see CompareItemText/StrCmpLogicalW - so e.g. "Row 2" sorts before
        /// "Row 10"). The comparer receives the two
        /// <see cref="ListViewItem"/>s being compared, so it can read other
        /// SubItems or <see cref="ListViewItem.Tag"/> instead of just the
        /// sorted column's text. Compare in plain ascending order - this
        /// control applies descending itself. Pass <c>null</c> to remove a
        /// comparer and revert that column to the default.
        /// </summary>
        public void SetSortComparer(ColumnHeader column, IComparer comparer)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            if (comparer == null)
            {
                _columnSortComparers.Remove(column);
            }
            else
            {
                _columnSortComparers[column] = comparer;
            }
        }

        /// <summary>
        /// Sizes every column to fit its current content and header text.
        /// Call this again after rebuilding the rows, since content
        /// driving the "right" width may have changed.
        /// </summary>
        public void AutoFitColumnsToContent()
        {
            if (!IsHandleCreated || Columns.Count == 0)
            {
                return;
            }

            for (var index = 0; index < Columns.Count; index++)
            {
                AutoResizeColumn(index, ColumnHeaderAutoResizeStyle.ColumnContent);
                var minimumWidth = GetEffectiveMinimumWidth(index);
                if (Columns[index].Width < minimumWidth)
                {
                    Columns[index].Width = minimumWidth;
                }
            }
        }

        // Turns on the native ListView's own double buffering - without it,
        // scrolling can leave stray gray streaks, since owner-drawn content
        // doesn't go through .NET's own OnPaint/DoubleBuffered at all.
        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
        private const int LVS_EX_DOUBLEBUFFER = 0x00010000;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct NativeRectangle
        {
            public int Left, Top, Right, Bottom;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
        private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, ref NativeRectangle rectangle);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int MapWindowPoints(System.IntPtr from, System.IntPtr to, ref NativeRectangle rectangle, uint count);

        private bool TryGetHeaderBounds(int columnIndex, out Rectangle bounds)
        {
            bounds = Rectangle.Empty;
            if (!IsHandleCreated || columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return false;
            }

            var header = HeaderInputSubclass.GetHeaderHandle(Handle);
            var rectangle = new NativeRectangle();
            const int HDM_GETITEMRECT = 0x1207;
            if (header == System.IntPtr.Zero ||
                SendMessage(header, HDM_GETITEMRECT, (System.IntPtr)columnIndex, ref rectangle) == System.IntPtr.Zero)
            {
                return false;
            }

            MapWindowPoints(header, Handle, ref rectangle, 2);
            bounds = Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
            return true;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Re-applied on every handle (re)creation - this extended style
            // lives on the native control, not anything .NET persists
            // across a recreate.
            SendMessage(Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, (System.IntPtr)LVS_EX_DOUBLEBUFFER, (System.IntPtr)LVS_EX_DOUBLEBUFFER);

            // The header is a separate native child window ("SysHeader32"),
            // recreated along with this control's own handle - reattach
            // every time instead of once in the constructor.
            _headerInputSubclass?.ReleaseHandle();
            System.IntPtr headerHandle = HeaderInputSubclass.GetHeaderHandle(Handle);
            if (headerHandle != System.IntPtr.Zero)
            {
                _headerInputSubclass = new HeaderInputSubclass(this);
                _headerInputSubclass.AssignHandle(headerHandle);
            }

            // The first paint after a handle recreate (e.g. a theme switch)
            // can land before colors have settled - defer one tick to
            // repaint with the final ones.
            BeginInvoke(new MethodInvoker(Invalidate));
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _headerInputSubclass?.ReleaseHandle();
            _headerInputSubclass = null;

            base.OnHandleDestroyed(e);
        }

        // Scrolling can leave a stray gray edge behind for a frame (native
        // partial-repaint artifact on an owner-drawn control) - forcing a
        // full repaint on every scroll message gets rid of it.
        private const int WM_VSCROLL = 0x0115;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_PAINT = 0x000F;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool LockWindowUpdate(System.IntPtr hWndLock);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                // LockWindowUpdate blocks any pixel of this window (and its
                // header) from reaching the screen while the native scroll
                // handling runs - plain Invalidate() afterward still let
                // the bad frame flash through first.
                LockWindowUpdate(Handle);
                try
                {
                    base.WndProc(ref m);
                }
                finally
                {
                    LockWindowUpdate(System.IntPtr.Zero);
                }

                Invalidate();
                Update();
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_PAINT)
            {
                FillRowAreaTrailingBackground();
            }
        }

        // OnDrawSubItem only ever paints an actual column's own cell -
        // same gap FillHeaderTrailingBackground fills for the header,
        // just one native window down: whatever's right of the last
        // column, below the header, never gets a DrawSubItem call of its
        // own, so it showed through as this control's plain (darker)
        // BackColor instead of matching the header's own trailing fill
        // directly above it. Filled with the same HeaderBackColor so the
        // whole strip - header and rows - reads as one uninterrupted band
        // instead of two subtly different darks with a seam between them.
        private void FillRowAreaTrailingBackground()
        {
            if (Columns.Count == 0)
            {
                return;
            }

            var rightEdge = GetColumnsTotalWidth();
            if (rightEdge >= ClientSize.Width)
            {
                return;
            }

            using (var graphics = Graphics.FromHwnd(Handle))
            using (var background = new SolidBrush(_headerBackColor))
            {
                graphics.FillRectangle(background, rightEdge, _headerHeight, ClientSize.Width - rightEdge, ClientSize.Height - _headerHeight);
            }
        }

        // The columns' combined width, i.e. where the real header/row
        // content ends and the trailing background begins - computed fresh
        // from Columns every time, rather than cached from the header's own
        // DrawColumnHeader event the way this used to work. That cache
        // (_lastColumnHeaderRightEdge) could go stale: the header and this
        // control's own row area are two separate native windows that don't
        // repaint in lockstep, so during a resize drag whichever one
        // happened to redraw most recently could leave the OTHER window
        // painting the trailing strip at the wrong x for a frame - or, if
        // the header simply didn't need to repaint on the drag's very last
        // step, permanently, since nothing else would ever correct it.
        // Columns[i].Width is always current the instant a resize sets it,
        // so this can't go stale the same way.
        private int GetColumnsTotalWidth()
        {
            int total = 0;
            foreach (ColumnHeader column in Columns)
            {
                total += column.Width;
            }

            return total;
        }

        private void OnColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            _headerInputSubclass?.CancelPendingReorder();
            _suppressNextColumnClickSort = true;

            if (!IsColumnResizable(e.ColumnIndex))
            {
                e.NewWidth = Columns[e.ColumnIndex].Width;
                e.Cancel = true;
                return;
            }

            var minimumWidth = GetEffectiveMinimumWidth(e.ColumnIndex);
            if (e.NewWidth < minimumWidth)
            {
                e.NewWidth = minimumWidth;
                e.Cancel = true;
                return;
            }

            // No live cap beyond the minimum-width floor above. A column
            // growing past what currently fits just means a horizontal
            // scrollbar appears, same as a plain, unmodified ListView.
        }

        // MinimumColumnWidth is a floor the caller chose, but a column must
        // never end up narrower than its own header text needs, or the
        // caption itself gets clipped - whichever of the two is larger wins.
        private int GetEffectiveMinimumWidth(int columnIndex)
        {
            return Math.Max(_minimumColumnWidth, MeasureHeaderTextWidth(columnIndex));
        }

        private int MeasureHeaderTextWidth(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return 0;
            }

            using (var font = new Font(Font, FontStyle.Bold))
            {
                var textSize = TextRenderer.MeasureText(
                    Columns[columnIndex].Text,
                    font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.Left | TextFormatFlags.NoPadding);

                // Matches the 7px left / 3px right padding OnDrawColumnHeader
                // draws the caption with.
                return textSize.Width + 10;
            }
        }

        private List<ColumnHeader> GetColumnsInDisplayOrder()
        {
            var columns = new List<ColumnHeader>();
            foreach (ColumnHeader column in Columns)
            {
                columns.Add(column);
            }

            return ColumnLayoutMath.OrderByDisplayIndex(columns, column => column.DisplayIndex);
        }

        // The top level only ever shows "Copy selection" / "Copy all" -
        // each is a plain submenu parent (native arrow, opens on hover, no
        // split-button chrome) whose flyout holds the actual two actions,
        // plain and "As table".
        private ContextMenuStrip BuildContextMenu()
        {
            // None of this menu's items (or its submenus) ever get an
            // Image, so the native reserved left-hand icon gutter just
            // showed up as a blank strip nothing ever used.
            var menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                SelectionBackColor = ContextMenuSelectionColor
            };

            string copySelectionText = UIStrings.Get("ListView.CopySelection");
            string copyAllText = UIStrings.Get("ListView.CopyAll");
            string asTableText = UIStrings.Get("ListView.AsTable");

            var copySelection = new ToolStripMenuItem(copySelectionText);
            copySelection.DropDownItems.Add(copySelectionText, null, (sender, e) => CopySelection());
            copySelection.DropDownItems.Add(asTableText, null, (sender, e) => CopySelectionAsTable());

            var copyAll = new ToolStripMenuItem(copyAllText);
            copyAll.DropDownItems.Add(copyAllText, null, (sender, e) => { SelectAll(); CopySelection(); });
            copyAll.DropDownItems.Add(asTableText, null, (sender, e) => { SelectAll(); CopySelectionAsTable(); });

            menu.Items.Add(copySelection);
            menu.Items.Add(copyAll);

            menu.Opening += (sender, e) =>
            {
                copySelection.Enabled = SelectedItems.Count > 0;
                copyAll.Enabled = Items.Count > 0;
            };

            return menu;
        }

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            _headerHeight = e.Bounds.Height;

            using (var background = new SolidBrush(_headerBackColor))
            using (var divider = new Pen(BorderColor))
            using (var font = HeaderFont)
            {
                e.Graphics.FillRectangle(background, e.Bounds);
                e.Graphics.DrawRectangle(divider, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 1, e.Bounds.Height - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Header.Text,
                    font,
                    new Rectangle(e.Bounds.X + 7, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 10), e.Bounds.Height),
                    GetEffectiveColor(_headerForeColor),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }

            DrawSortGlyph(e);
            DrawColumnDragInsertionLine(e);
        }

        private void DrawSortGlyph(DrawListViewColumnHeaderEventArgs e)
        {
            if (e.Header.Index != _sortedColumnIndex)
            {
                return;
            }

            ColumnHeaderPainting.DrawSortGlyph(e.Graphics, e.Bounds, Font, GetEffectiveColor(_headerForeColor), _sortOrder);
        }

        protected override void OnColumnClick(ColumnClickEventArgs e)
        {
            base.OnColumnClick(e);

            if (_suppressNextColumnClickSort)
            {
                _suppressNextColumnClickSort = false;
                return;
            }

            if (!IsColumnSortable(e.Column))
            {
                return;
            }

            CycleSort(e.Column);
        }

        /// <summary>
        /// Three-state header click: ascending, then descending, then back
        /// to the order items were in the first time this was ever called -
        /// mirrors ReadOnlyDataGridView's own CycleSort. Clicking a
        /// different column starts that column fresh at ascending.
        /// </summary>
        private void CycleSort(int columnIndex)
        {
            if (_sortedColumnIndex < 0)
            {
                // Snapshot the unsorted order lazily, on first sort - there's
                // no DataSource/Reset event to hook here like DataGridView has.
                _originalOrder = new List<ListViewItem>(Items.Cast<ListViewItem>());
            }

            if (columnIndex != _sortedColumnIndex)
            {
                _sortedColumnIndex = columnIndex;
                _sortOrder = SortOrder.Ascending;
            }
            else if (_sortOrder == SortOrder.Ascending)
            {
                _sortOrder = SortOrder.Descending;
            }
            else if (_sortOrder == SortOrder.Descending)
            {
                _sortOrder = SortOrder.None;
            }
            else
            {
                _sortOrder = SortOrder.Ascending;
            }

            if (_sortOrder == SortOrder.None)
            {
                _sortedColumnIndex = -1;
                ApplyItemOrder(BuildOriginalOrder());
            }
            else
            {
                ApplyItemOrder(BuildSortedOrder(columnIndex, _sortOrder));
            }

            // Matches ReadOnlyDataGridView's own ClearSelection()/CurrentCell
            // = null after a sort - the rows themselves just moved out from
            // under whatever was selected, so keeping a stale selection
            // pointed at the wrong row would be actively misleading.
            foreach (ListViewItem item in Items)
            {
                item.Selected = false;
            }

            InvalidateHeader();
        }

        // BeginUpdate/EndUpdate - this control's own equivalent of
        // ReadOnlyDataGridView's ApplyBatchedDataSourceChange, suppressing
        // the native control's own per-item repaint/layout while every item
        // is removed and re-added, then repainting once at the end instead
        // of once per item.
        private void ApplyItemOrder(List<ListViewItem> desiredOrder)
        {
            BeginUpdate();
            try
            {
                Items.Clear();
                Items.AddRange(desiredOrder.ToArray());
            }
            finally
            {
                EndUpdate();
            }
        }

        // Mirrors ReadOnlyDataGridView's own BuildSortedOrder, adapted for
        // plain text SubItems instead of typed bound cell values - see
        // SetSortComparer for sorting by anything else.
        private List<ListViewItem> BuildSortedOrder(int columnIndex, SortOrder sortOrder)
        {
            int direction = sortOrder == SortOrder.Descending ? -1 : 1;
            List<ListViewItem> items = new List<ListViewItem>(Items.Cast<ListViewItem>());

            _columnSortComparers.TryGetValue(Columns[columnIndex], out IComparer customComparer);

            items.Sort((a, b) => customComparer != null
                ? customComparer.Compare(a, b) * direction
                : CompareItemText(GetSubItemText(a, columnIndex), GetSubItemText(b, columnIndex), direction));

            return items;
        }

        // Mirrors DataGridView's own BuildOriginalOrder (see
        // ItemOrderMath.BuildOriginalOrder for the shared "unindexed items
        // sort last" logic both use).
        private List<ListViewItem> BuildOriginalOrder()
        {
            return ItemOrderMath.BuildOriginalOrder(Items.Cast<ListViewItem>(), _originalOrder);
        }

        private static string GetSubItemText(ListViewItem item, int columnIndex)
        {
            return columnIndex < item.SubItems.Count ? item.SubItems[columnIndex].Text : string.Empty;
        }

        // "Row 2" before "Row 10", not after - plain string comparison
        // sorts lexicographically and puts "Row 10" between them.
        // StrCmpLogicalW is the same native function Explorer's own file
        // listing uses: numeric runs compare numerically, everything else
        // as text.
        [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        private static int CompareItemText(string textX, string textY, int direction)
        {
            return StrCmpLogicalW(textX ?? string.Empty, textY ?? string.Empty) * direction;
        }

        // Drawn as part of the header cell's own owner-draw pass, not a
        // separate overlay - see ColumnReorderIndicatorColor's doc comment
        // for why. Mirrors DataGridView's own DrawColumnDragInsertionLine.
        private void DrawColumnDragInsertionLine(DrawListViewColumnHeaderEventArgs e)
        {
            if (!_isDraggingColumn)
            {
                return;
            }

            ColumnHeaderPainting.DrawColumnDragInsertionLine(
                e.Graphics, e.Bounds, e.Header.DisplayIndex, Columns.Count, _dragInsertBeforeDisplayIndex, ColumnReorderIndicatorColor);
        }

        private static void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Details view paints complete rows in OnDrawSubItem.
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // Use the native header layout, including horizontal scrolling
            // and column reordering, for the cell's horizontal bounds.
            var bounds = TryGetHeaderBounds(e.ColumnIndex, out Rectangle headerBounds)
                ? new Rectangle(headerBounds.Left, e.Bounds.Top, headerBounds.Width, e.Bounds.Height)
                : e.Bounds;

            var baseBackColor = e.ItemIndex % 2 == 0 ? _rowBackColor : _alternateRowBackColor;
            using (var background = new SolidBrush(baseBackColor))
            {
                e.Graphics.FillRectangle(background, bounds);
            }

            // FullRowSelect + native SelectedItems - the whole row's worth
            // of cells share one selected/not-selected state, straight from
            // the native control, not any hand-tracked range.
            if (e.Item.Selected)
            {
                using (var overlay = new SolidBrush(GetEffectiveColor(SelectionOverlayColor)))
                {
                    e.Graphics.FillRectangle(overlay, bounds);
                }
            }

            var text = e.ColumnIndex < e.Item.SubItems.Count ? e.Item.SubItems[e.ColumnIndex].Text : string.Empty;

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.Item.Font ?? Font,
                new Rectangle(bounds.X + 6, bounds.Y, Math.Max(0, bounds.Width - 9), bounds.Height),
                GetEffectiveColor(e.Item.ForeColor),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // Flat gray when disabled, same as ReadOnlyDataGridView.ApplyStyles -
        // applied at paint time so nothing needs restoring once re-enabled.
        private Color GetEffectiveColor(Color normalColor)
        {
            return Enabled ? normalColor : UIColors.DisabledGray;
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
            _headerInputSubclass?.InvalidateHeaderNow();
        }

        // Called from HeaderInputSubclass once a header click passes the
        // drag threshold. From here, WinForms' own DoDragDrop/OnDragOver/
        // OnDragDrop - the same mechanism ListBox uses for item reordering -
        // drives the rest as a native OLE drag.
        private void BeginColumnDragDrop(int columnIndex)
        {
            _dragColumnIndex = columnIndex;
            _isDraggingColumn = true;
            _dragInsertBeforeDisplayIndex = -1;

            // A drag still ends with mouse-up on a header cell as far as
            // native click detection is concerned, which can silently
            // trigger a sort as a side effect - suppress it preemptively,
            // same as DataGridView's own hand-rolled resize does.
            _suppressNextColumnClickSort = true;

            try
            {
                DoDragDrop(columnIndex, DragDropEffects.Move);
            }
            finally
            {
                // Covers every way the drag can end, including a cancel
                // (Escape, or a drop where OnDragDrop never fires) -
                // OnDragDrop itself only needs to perform the move.
                _isDraggingColumn = false;
                _dragColumnIndex = -1;
                _dragInsertBeforeDisplayIndex = -1;

                InvalidateHeader();
            }
        }

        // Invalidate() alone only reaches this control's own client area -
        // the header is a separate native child window, so without this
        // the drag insertion line would never actually repaint.
        private void InvalidateHeader()
        {
            Invalidate();
            _headerInputSubclass?.InvalidateHeaderNow();
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            var point = PointToClient(new Point(drgevent.X, drgevent.Y));
            var insertBefore = GetColumnDropInsertionIndex(point.X);

            drgevent.Effect = DragDropEffects.Move;

            if (insertBefore != _dragInsertBeforeDisplayIndex)
            {
                _dragInsertBeforeDisplayIndex = insertBefore;
                InvalidateHeader();
            }

            base.OnDragOver(drgevent);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            if (_dragColumnIndex >= 0 && _dragInsertBeforeDisplayIndex >= 0)
            {
                MoveColumnToDisplayIndex(_dragColumnIndex, _dragInsertBeforeDisplayIndex);
            }

            base.OnDragDrop(drgevent);
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            gfbevent.UseDefaultCursors = false;
            Cursor.Current = Cursors.SizeWE;

            base.OnGiveFeedback(gfbevent);
        }

        // Mirrors DataGridView's own MoveColumnToDisplayIndex.
        private void MoveColumnToDisplayIndex(int columnIndex, int insertBeforeDisplayIndex)
        {
            if (columnIndex < 0 || columnIndex >= Columns.Count)
            {
                return;
            }

            var column = Columns[columnIndex];
            int? targetDisplayIndex = ColumnLayoutMath.GetMoveTargetDisplayIndex(column.DisplayIndex, insertBeforeDisplayIndex);
            if (targetDisplayIndex == null)
            {
                return;
            }

            column.DisplayIndex = targetDisplayIndex.Value;
        }

        private const int ResizeGripWidth = 5;

        // The resize grip stays native - only clicks clearly inside a
        // column's body start our own reorder drag, so resizing isn't
        // accidentally hijacked into a reorder attempt.
        private bool IsNearColumnBorder(int x)
        {
            return ColumnLayoutMath.TryGetColumnAtBorder(GetColumnsInDisplayOrder(), c => c.Width, x, ResizeGripWidth, out _);
        }

        // comctl32 always shows the resize cursor near a column boundary,
        // regardless of SetColumnResizable - dragging is already blocked
        // (see TryBeginResize/OnColumnWidthChanging), but the cursor would
        // still lie about it. A boundary resizes the column to its LEFT
        // (comctl32's own convention), so that's the column whose
        // resizability governs this specific boundary.
        private bool IsNearNonResizableColumnBorder(int x)
        {
            return ColumnLayoutMath.TryGetColumnAtBorder(GetColumnsInDisplayOrder(), c => c.Width, x, ResizeGripWidth, out ColumnHeader column) &&
                !IsColumnResizable(column.Index);
        }

        // Mirrors DataGridView's own GetColumnDropInsertionIndex - see
        // ColumnLayoutMath.GetDropInsertionIndex's own remarks on the
        // coordinate space this expects x in.
        private int GetColumnDropInsertionIndex(int x)
        {
            var orderedColumns = GetColumnsInDisplayOrder();
            return ColumnLayoutMath.GetDropInsertionIndex(orderedColumns, column => column.Width, x);
        }

        // Callers need only the row. GetItemAt avoids HitTest's unchecked
        // SubItems[-1] access when native hit testing finds no subitem.
        private ListViewHitTestInfo SafeHitTest(Point location)
        {
            if (!IsHandleCreated || Columns.Count == 0 || !ClientRectangle.Contains(location))
            {
                return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);
            }

            var item = GetItemAt(location.X, location.Y);
            return new ListViewHitTestInfo(item, null, ListViewHitTestLocations.None);
        }

        // Native click never toggles an already-selected item back off -
        // this restores that: remember at MouseDown whether the pressed
        // item was the sole selection, and deselect it if MouseUp lands
        // back on the same item (not a drag elsewhere).
        private void OnListViewMouseDownForToggleDeselect(object sender, MouseEventArgs e)
        {
            _pendingToggleDeselectItemIndex = -1;

            // A new press cancels any pending watch from a PREVIOUS
            // toggle-deselect - otherwise a quick second click on the same
            // row looked identical to the delayed quirk below and got
            // silently reverted too.
            _toggleDeselectWatchIndex = -1;
            _toggleDeselectSettleTimer.Stop();

            if (e.Button != MouseButtons.Left || ModifierKeys != Keys.None)
            {
                return;
            }

            var hitTest = SafeHitTest(e.Location);
            if (hitTest.Item != null && hitTest.Item.Selected && SelectedItems.Count == 1)
            {
                _pendingToggleDeselectItemIndex = hitTest.Item.Index;
            }
        }

        private void OnListViewMouseUpForToggleDeselect(object sender, MouseEventArgs e)
        {
            var pendingIndex = _pendingToggleDeselectItemIndex;
            _pendingToggleDeselectItemIndex = -1;

            if (pendingIndex < 0 || e.Button != MouseButtons.Left)
            {
                return;
            }

            var hitTest = SafeHitTest(e.Location);
            if (hitTest.Item != null && hitTest.Item.Index == pendingIndex && hitTest.Item.Selected)
            {
                hitTest.Item.Selected = false;

                // comctl32 arms a "click to rename" timer for any press on
                // an already-selected, focused item - it fires ~1s later
                // and re-selects the item, undoing the deselect above.
                // OnItemSelectionChangedForToggleDeselect reverts that the
                // instant it fires; this timer is just the safety net.
                _toggleDeselectWatchIndex = pendingIndex;
                _toggleDeselectSettleTimer.Stop();
                _toggleDeselectSettleTimer.Start();
            }
        }

        private void OnItemSelectionChangedForToggleDeselect(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_toggleDeselectWatchIndex < 0 || e.ItemIndex != _toggleDeselectWatchIndex || !e.IsSelected)
            {
                return;
            }

            // Clear the watch before reverting - Selected's setter below
            // raises this same event again, and an uncleared guard would
            // try to act on it a second time.
            _toggleDeselectWatchIndex = -1;
            _toggleDeselectSettleTimer.Stop();
            e.Item.Selected = false;
        }

        // Only reached if the native reselect quirk never actually fired
        // for this click - just stops watching, since
        // OnItemSelectionChangedForToggleDeselect already handles the real
        // case the instant it happens.
        private void OnToggleDeselectSettleTimerTick(object sender, EventArgs e)
        {
            _toggleDeselectSettleTimer.Stop();
            _toggleDeselectWatchIndex = -1;
        }

        // Native marquee-select only arms for a press on empty space, not
        // on an item - this fills that gap: a plain press on a row, then
        // moving to a different row while held, selects everything between
        // them (Explorer-style drag-select). Tracked via a poll timer
        // instead of MouseMove/MouseUp, since those can stop arriving once
        // the drag leaves this control's bounds, which would break
        // auto-scroll past an edge.
        private void OnListViewMouseDownForRowRangeDrag(object sender, MouseEventArgs e)
        {
            _isRowRangeDragging = false;
            _rowRangeDragAnchorIndex = -1;
            _rowRangeAutoScrollDirection = 0;
            _rowRangeDragPollTimer.Stop();

            if (e.Button != MouseButtons.Left || ModifierKeys != Keys.None)
            {
                return;
            }

            var hitTest = SafeHitTest(e.Location);
            if (hitTest.Item == null)
            {
                // An empty-space press is already native marquee-select's
                // own job - nothing to add here.
                return;
            }

            _isRowRangeDragging = true;
            _rowRangeDragAnchorIndex = hitTest.Item.Index;
            _rowRangeDragLastAppliedIndex = hitTest.Item.Index;
            _rowRangeDragPollTimer.Start();
        }

        // A release on this control still stops things immediately - the
        // poll tick is only the fallback for a release somewhere else.
        private void OnListViewMouseUpForRowRangeDrag(object sender, MouseEventArgs e)
        {
            _isRowRangeDragging = false;
            _rowRangeDragAnchorIndex = -1;
            _rowRangeAutoScrollDirection = 0;
            _rowRangeDragPollTimer.Stop();
        }

        // Same LockWindowUpdate + forced repaint as WndProc's scroll
        // handling above - TopItem's setter scrolls via its own path, not
        // through WM_VSCROLL/HSCROLL/MOUSEWHEEL, so it never got that
        // protection and could show the same stray-line seam.
        private void ScrollToItemWithoutArtifacts(ListViewItem item)
        {
            LockWindowUpdate(Handle);
            try
            {
                TopItem = item;
            }
            finally
            {
                LockWindowUpdate(System.IntPtr.Zero);
            }

            Invalidate();
            Update();
        }

        private void OnRowRangeDragPollTick(object sender, EventArgs e)
        {
            if (!_isRowRangeDragging)
            {
                _rowRangeDragPollTimer.Stop();
                return;
            }

            if ((MouseButtons & MouseButtons.Left) == 0)
            {
                _isRowRangeDragging = false;
                _rowRangeAutoScrollDirection = 0;
                _rowRangeDragPollTimer.Stop();
                return;
            }

            var location = PointToClient(Cursor.Position);

            // Purely a visual aid - GetNearestRowIndex below already
            // clamps to the true first/last row regardless of scroll
            // position, so the selection itself doesn't depend on this.
            int direction = location.Y < _headerHeight ? -1 : location.Y >= ClientSize.Height ? 1 : 0;
            _rowRangeAutoScrollDirection = direction;

            if (direction != 0)
            {
                var topItem = TopItem;
                if (topItem != null)
                {
                    var newTopIndex = topItem.Index + direction;
                    if (newTopIndex >= 0 && newTopIndex < Items.Count)
                    {
                        ScrollToItemWithoutArtifacts(Items[newTopIndex]);
                    }
                }
            }

            var currentIndex = GetNearestRowIndex(location);
            if (currentIndex < 0 || currentIndex == _rowRangeDragLastAppliedIndex)
            {
                return;
            }

            _rowRangeDragLastAppliedIndex = currentIndex;
            ApplyRowRangeSelection(_rowRangeDragAnchorIndex, currentIndex);
        }

        // Resolves to the item under the point, or the nearest row in that
        // direction - clamped to the true first/last row in the whole
        // list, not just what's currently scrolled into view.
        private int GetNearestRowIndex(Point location)
        {
            if (Items.Count == 0)
            {
                return -1;
            }

            var hitTest = SafeHitTest(location);
            if (hitTest.Item != null)
            {
                return hitTest.Item.Index;
            }

            if (location.Y <= Items[0].Bounds.Top)
            {
                return 0;
            }

            if (location.Y >= Items[Items.Count - 1].Bounds.Bottom)
            {
                return Items.Count - 1;
            }

            for (var index = 0; index < Items.Count; index++)
            {
                if (location.Y < Items[index].Bounds.Bottom)
                {
                    return index;
                }
            }

            return Items.Count - 1;
        }

        private void ApplyRowRangeSelection(int anchorIndex, int currentIndex)
        {
            if (anchorIndex < 0 || anchorIndex >= Items.Count || currentIndex < 0 || currentIndex >= Items.Count)
            {
                return;
            }

            var rangeStart = Math.Min(anchorIndex, currentIndex);
            var rangeEnd = Math.Max(anchorIndex, currentIndex);

            BeginUpdate();
            try
            {
                for (var index = 0; index < Items.Count; index++)
                {
                    var shouldBeSelected = index >= rangeStart && index <= rangeEnd;
                    if (Items[index].Selected != shouldBeSelected)
                    {
                        Items[index].Selected = shouldBeSelected;
                    }
                }
            }
            finally
            {
                EndUpdate();
            }
        }

        // Shows the full cell text on hover whenever OnDrawSubItem would
        // have had to ellipsize it - the same 6px left / 3px right padding
        // it draws text with is subtracted here to decide if it actually
        // overflows the column.
        private void OnListViewMouseMoveForToolTip(object sender, MouseEventArgs e)
        {
            var hitTest = SafeHitTest(e.Location);
            if (hitTest.Item == null)
            {
                HideCellToolTip();
                return;
            }

            var row = hitTest.Item.Index;
            var displayColumn = -1;
            foreach (ColumnHeader candidate in Columns)
            {
                if (TryGetHeaderBounds(candidate.Index, out Rectangle bounds) &&
                    e.X >= bounds.Left && e.X < bounds.Right)
                {
                    displayColumn = candidate.DisplayIndex;
                    break;
                }
            }
            if (row == _toolTipRow && displayColumn == _toolTipDisplayColumn)
            {
                return;
            }

            _toolTipRow = row;
            _toolTipDisplayColumn = displayColumn;

            var orderedColumns = GetColumnsInDisplayOrder();
            if (displayColumn < 0 || displayColumn >= orderedColumns.Count)
            {
                _cellToolTip.Hide(this);
                return;
            }

            var column = orderedColumns[displayColumn];
            var item = hitTest.Item;
            var text = column.Index < item.SubItems.Count ? item.SubItems[column.Index].Text : string.Empty;

            if (string.IsNullOrEmpty(text) || !IsTextTruncated(text, item.Font ?? Font, column.Width))
            {
                _cellToolTip.Hide(this);
                return;
            }

            _cellToolTip.Show(text, this, e.Location.X + 12, e.Location.Y + 18, 8000);
        }

        private void OnListViewMouseLeave(object sender, EventArgs e)
        {
            HideCellToolTip();
        }

        private void HideCellToolTip()
        {
            _toolTipRow = -1;
            _toolTipDisplayColumn = -1;
            _cellToolTip.Hide(this);
        }

        private static bool IsTextTruncated(string text, Font font, int columnWidth)
        {
            var availableWidth = columnWidth - 9;
            if (availableWidth <= 0)
            {
                return true;
            }

            var measured = TextRenderer.MeasureText(
                text,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return measured.Width > availableWidth;
        }

        // Arrow-key navigation/range-extension (Up/Down/Shift+Up/Down, plus
        // Ctrl-navigate-without-selecting) is entirely native - only Ctrl+C/
        // Ctrl+Shift+C/Ctrl+A need handling here, since the native control
        // has no built-in accelerator for any of those three.
        private void OnListViewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.Shift && e.KeyCode == Keys.C)
            {
                CopySelectionAsTable();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelection();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                SelectAll();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        // The native ListView has no built-in "select everything" - unlike
        // ListBox, which does.
        private void SelectAll()
        {
            if (Items.Count == 0)
            {
                return;
            }

            BeginUpdate();
            try
            {
                foreach (ListViewItem item in Items)
                {
                    item.Selected = true;
                }
            }
            finally
            {
                EndUpdate();
            }
        }

        // Returns a DISPLAY index (position in visual column order) - used
        // for header-reorder/tooltip column hit-testing, unrelated to
        // selection (which is now entirely native/row-based).
        private int GetColumnIndexAtX(int x)
        {
            var orderedColumns = GetColumnsInDisplayOrder();
            var cumulativeWidth = 0;
            for (var displayIndex = 0; displayIndex < orderedColumns.Count; displayIndex++)
            {
                cumulativeWidth += orderedColumns[displayIndex].Width;
                if (x < cumulativeWidth)
                {
                    return displayIndex;
                }
            }

            return Math.Max(0, orderedColumns.Count - 1);
        }

        private void CopySelection()
        {
            CopySelectionCore(includeHeader: false);
        }

        // Same selected rectangle as CopySelection, but with a leading row of
        // column captions - so the clipboard content pastes as a proper
        // table (e.g. into a spreadsheet) instead of bare data rows.
        private void CopySelectionAsTable()
        {
            CopySelectionCore(includeHeader: true);
        }

        private void CopySelectionCore(bool includeHeader)
        {
            // SelectedItems isn't guaranteed to be in visual order - sorting
            // by Index keeps the copied rows in the same top-to-bottom order
            // they're actually shown in, regardless of the order they were
            // clicked/dragged into the selection.
            var selectedItems = SelectedItems.Cast<ListViewItem>().OrderBy(item => item.Index).ToList();
            if (selectedItems.Count == 0)
            {
                return;
            }

            // Every column, in DISPLAY order - a whole-row copy always
            // includes every column, so (unlike the old cell-range copy)
            // there's no column span to compute here.
            var orderedColumns = GetColumnsInDisplayOrder();

            List<string> headerCells = null;
            if (includeHeader)
            {
                headerCells = orderedColumns.Select(column => column.Text).ToList();
            }

            var plainTextBuilder = new StringBuilder();
            if (headerCells != null)
            {
                plainTextBuilder.AppendLine(string.Join("\t", headerCells));
            }

            var rows = new List<List<string>>();
            foreach (var item in selectedItems)
            {
                var cells = new List<string>();
                foreach (var column in orderedColumns)
                {
                    var dataColumn = column.Index;
                    cells.Add(dataColumn < item.SubItems.Count ? item.SubItems[dataColumn].Text : string.Empty);
                }

                rows.Add(cells);
                plainTextBuilder.AppendLine(string.Join("\t", cells));
            }

            // Plain text is the universal fallback; "HTML Format" rides
            // alongside it so apps that understand it (Word, Outlook,
            // browsers, Excel, ...) paste an actual table instead.
            var dataObject = new DataObject();
            dataObject.SetText(plainTextBuilder.ToString(), TextDataFormat.UnicodeText);
            dataObject.SetData(DataFormats.Html, BuildCfHtmlTable(headerCells, rows));
            Clipboard.SetDataObject(dataObject, true);

            var message = selectedItems.Count == 1
                ? UIStrings.Get("ListView.RowCopied")
                : string.Format(UIStrings.Get("ListView.RowsCopied"), selectedItems.Count);
            ShowCopyToast(includeHeader ? message + UIStrings.Get("ListView.WithHeaderSuffix") : message);
        }

        // Wraps an HTML <table> in the CF_HTML clipboard envelope Windows
        // requires (Version/StartHTML/EndHTML/StartFragment/EndFragment byte
        // offsets around an <html><body> shell). See the CF_HTML spec - the
        // offsets are byte counts, and .NET writes "HTML Format" clipboard
        // data as UTF-8, so they're computed in UTF-8 bytes rather than .NET
        // char counts (matters here since summaries/titles routinely contain
        // German umlauts).
        private static string BuildCfHtmlTable(IReadOnlyList<string> headerCells, IReadOnlyList<List<string>> rows)
        {
            var table = BuildHtmlTable(headerCells, rows);

            const string HeaderTemplate =
                "Version:0.9\r\n" +
                "StartHTML:{0:0000000000}\r\n" +
                "EndHTML:{1:0000000000}\r\n" +
                "StartFragment:{2:0000000000}\r\n" +
                "EndFragment:{3:0000000000}\r\n";
            const string HtmlPrefix = "<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->";
            const string HtmlSuffix = "<!--EndFragment--></body></html>";

            // Every offset is zero-padded to a fixed width, so the header's
            // own byte length is identical whether computed from placeholder
            // zeros or from the real (larger) offsets it ends up holding.
            var headerLength = Encoding.UTF8.GetByteCount(string.Format(HeaderTemplate, 0, 0, 0, 0));
            var startHtml = headerLength;
            var startFragment = startHtml + Encoding.UTF8.GetByteCount(HtmlPrefix);
            var endFragment = startFragment + Encoding.UTF8.GetByteCount(table);
            var endHtml = endFragment + Encoding.UTF8.GetByteCount(HtmlSuffix);

            return string.Format(HeaderTemplate, startHtml, endHtml, startFragment, endFragment) + HtmlPrefix + table + HtmlSuffix;
        }

        private static string BuildHtmlTable(IReadOnlyList<string> headerCells, IReadOnlyList<List<string>> rows)
        {
            var builder = new StringBuilder();
            builder.Append("<table style=\"border-collapse:collapse;font-family:Segoe UI,sans-serif;font-size:9pt;\">");

            if (headerCells != null)
            {
                builder.Append("<tr>");
                foreach (var cell in headerCells)
                {
                    builder.Append("<th style=\"border:1px solid #999;padding:4px 8px;background:#eee;text-align:left;\">");
                    builder.Append(WebUtility.HtmlEncode(cell));
                    builder.Append("</th>");
                }

                builder.Append("</tr>");
            }

            foreach (var row in rows)
            {
                builder.Append("<tr>");
                foreach (var cell in row)
                {
                    builder.Append("<td style=\"border:1px solid #999;padding:4px 8px;\">");
                    builder.Append(WebUtility.HtmlEncode(cell));
                    builder.Append("</td>");
                }

                builder.Append("</tr>");
            }

            builder.Append("</table>");
            return builder.ToString();
        }

        // Reuses the same ToolTip instance already used for overflow-text
        // hover previews elsewhere in this class, rather than owning a
        // second ToolTip component.
        private void ShowCopyToast(string message)
        {
            CopyConfirmationDisplay.Show(CopyConfirmation, message, this, _cellToolTip);
        }

        // Header input lives on a separate HWND. Divider drags use screen
        // deltas for live resizing; other drags retain column reordering.
        private sealed class HeaderInputSubclass : NativeWindow
        {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_MOUSEMOVE = 0x0200;
            private const int WM_LBUTTONUP = 0x0202;
            private const int WM_CANCELMODE = 0x001F;
            private const int WM_CAPTURECHANGED = 0x0215;
            private const int WM_PAINT = 0x000F;
            private const int LVM_FIRST = 0x1000;
            private const int LVM_GETHEADER = LVM_FIRST + 31;

            private readonly ListView _owner;
            private int _pendingColumnIndex = -1;
            private int _pendingStartX;
            private ColumnHeader _resizingColumn;
            private int _resizeStartScreenX;
            private int _resizeStartWidth;

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            private struct HeaderHitTest
            {
                public int X, Y;
                public uint Flags;
                public int Item;
            }

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            private struct Rect
            {
                public int Left, Top, Right, Bottom;
            }

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern bool GetClientRect(System.IntPtr hWnd, out Rect rect);

            [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SendMessageW")]
            private static extern System.IntPtr SendMessage(System.IntPtr window, int message, System.IntPtr wParam, ref HeaderHitTest hit);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr SetCapture(System.IntPtr window);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr GetCapture();

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern bool ReleaseCapture();

            private const uint RDW_INVALIDATE = 0x0001;
            private const uint RDW_ERASE = 0x0004;
            private const uint RDW_UPDATENOW = 0x0100;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern System.IntPtr SendMessage(System.IntPtr hWnd, int msg, System.IntPtr wParam, System.IntPtr lParam);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern bool RedrawWindow(System.IntPtr hWnd, System.IntPtr lprcUpdate, System.IntPtr hrgnUpdate, uint flags);

            public HeaderInputSubclass(ListView owner)
            {
                _owner = owner;
            }

            public void CancelPendingReorder()
            {
                _pendingColumnIndex = -1;
            }

            public static System.IntPtr GetHeaderHandle(System.IntPtr listViewHandle)
            {
                return SendMessage(listViewHandle, LVM_GETHEADER, System.IntPtr.Zero, System.IntPtr.Zero);
            }

            // The header is a separate native window - Invalidate() on the
            // owner doesn't reach it, so this forces an immediate repaint.
            public void InvalidateHeaderNow()
            {
                RedrawWindow(Handle, System.IntPtr.Zero, System.IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_UPDATENOW);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_LBUTTONDOWN && TryBeginResize(m.LParam))
                {
                    m.Result = System.IntPtr.Zero;
                    return;
                }

                if (_resizingColumn != null)
                {
                    if (m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONUP)
                    {
                        if (m.Msg == WM_LBUTTONUP || (Control.MouseButtons & MouseButtons.Left) != 0)
                        {
                            ApplyResize();
                        }
                        if (m.Msg == WM_LBUTTONUP || (Control.MouseButtons & MouseButtons.Left) == 0)
                        {
                            EndResize();
                        }
                        m.Result = System.IntPtr.Zero;
                        return;
                    }
                    if (m.Msg == WM_CANCELMODE || m.Msg == WM_CAPTURECHANGED)
                    {
                        EndResize();
                    }
                }

                base.WndProc(ref m);

                switch (m.Msg)
                {
                    case WM_LBUTTONDOWN:
                        OnMouseDown(GetX(m.LParam));
                        break;

                    case WM_MOUSEMOVE:
                        // Runs after base.WndProc: the header sets its own
                        // resize cursor from its own mouse-move handling,
                        // so correcting it after (not via WM_SETCURSOR
                        // first) is what actually sticks.
                        TrySuppressResizeCursor(GetX(m.LParam));
                        OnMouseMove(GetX(m.LParam));
                        break;

                    case WM_LBUTTONUP:
                    case WM_CANCELMODE:
                    case WM_CAPTURECHANGED:
                        // A plain click (never past the threshold) just
                        // clears the pending state - nothing to reorder or
                        // undo.
                        _pendingColumnIndex = -1;
                        break;

                    case WM_PAINT:
                        FillHeaderTrailingBackground();
                        break;
                }
            }

            private static int GetX(System.IntPtr lParam)
            {
                return unchecked((short)((long)lParam & 0xFFFF));
            }

            // comctl32 clips per-item custom-draw to that item's own rect,
            // so drawing past a column's edge from OnDrawColumnHeader never
            // showed up - painting straight onto the header's DC here isn't
            // clipped that way.
            private void FillHeaderTrailingBackground()
            {
                if (_owner.Columns.Count == 0 || !GetClientRect(Handle, out Rect clientRect))
                {
                    return;
                }

                var rightEdge = _owner.GetColumnsTotalWidth();
                if (rightEdge >= clientRect.Right)
                {
                    return;
                }

                using (var graphics = Graphics.FromHwnd(Handle))
                using (var background = new SolidBrush(_owner.HeaderBackColor))
                {
                    graphics.FillRectangle(background, rightEdge, clientRect.Top, clientRect.Right - rightEdge, clientRect.Bottom - clientRect.Top);
                }
            }

            private bool TryBeginResize(System.IntPtr coordinates)
            {
                var hit = new HeaderHitTest
                {
                    X = GetX(coordinates),
                    Y = unchecked((short)((long)coordinates >> 16))
                };
                const int HDM_HITTEST = 0x1206;
                const uint DividerFlags = 0x0004 | 0x0008;
                SendMessage(Handle, HDM_HITTEST, System.IntPtr.Zero, ref hit);
                if ((hit.Flags & DividerFlags) == 0 || hit.Item < 0 || hit.Item >= _owner.Columns.Count)
                {
                    return false;
                }

                CancelPendingReorder();
                _owner._suppressNextColumnClickSort = true;
                if (!_owner.IsColumnResizable(hit.Item))
                {
                    return true;
                }

                _resizingColumn = _owner.Columns[hit.Item];
                _resizeStartScreenX = Cursor.Position.X;
                _resizeStartWidth = _resizingColumn.Width;
                SetCapture(Handle);
                Cursor.Current = Cursors.SizeWE;
                return true;
            }

            private void ApplyResize()
            {
                var column = _resizingColumn;
                if (column == null || column.Index < 0 || column.ListView != _owner)
                {
                    EndResize();
                    return;
                }

                // A scrolling header changes client coordinates; screen deltas stay 1:1.
                int width = Math.Max(_owner.GetEffectiveMinimumWidth(column.Index),
                    _resizeStartWidth + Cursor.Position.X - _resizeStartScreenX);
                if (column.Width != width)
                {
                    column.Width = width;
                }
                Cursor.Current = Cursors.SizeWE;
            }

            private void EndResize()
            {
                _resizingColumn = null;
                CancelPendingReorder();
                if (GetCapture() == Handle)
                {
                    ReleaseCapture();
                }
                Cursor.Current = Cursors.Default;

                // One guaranteed-correct repaint of both windows now that
                // the drag is over, regardless of whether Windows' own
                // invalidate-on-resize already repainted either of them for
                // this exact final width - closes out the case where it
                // didn't (e.g. only a small strip near the dragged border
                // was considered dirty), which used to leave the trailing
                // background wrong until some unrelated repaint fixed it.
                _owner.Invalidate();
                _owner.Update();
                InvalidateHeaderNow();
            }

            // See the WM_MOUSEMOVE case above for why this runs after
            // base.WndProc instead of intercepting WM_SETCURSOR.
            private void TrySuppressResizeCursor(int x)
            {
                if (_owner.IsNearNonResizableColumnBorder(x))
                {
                    Cursor.Current = Cursors.Default;
                }
            }

            private void OnMouseDown(int x)
            {
                _pendingColumnIndex = -1;

                // A new press starts fresh - only a drag that reaches
                // BeginColumnDragDrop should suppress the click-to-sort
                // that follows it.
                _owner._suppressNextColumnClickSort = false;

                // !AllowDrop also covers the MTA case guarded in the
                // constructor - without a drop target, DoDragDrop has
                // nothing to hand the drag to.
                if (!_owner.AllowDrop || _owner.Columns.Count == 0 || _owner.IsNearColumnBorder(x))
                {
                    return;
                }

                var displayIndex = _owner.GetColumnIndexAtX(x);
                var orderedColumns = _owner.GetColumnsInDisplayOrder();
                if (displayIndex < 0 || displayIndex >= orderedColumns.Count)
                {
                    return;
                }

                var clickedColumn = orderedColumns[displayIndex];
                if (!_owner.IsColumnReorderable(clickedColumn.Index))
                {
                    return;
                }

                _pendingColumnIndex = clickedColumn.Index;
                _pendingStartX = Cursor.Position.X;
            }

            private void OnMouseMove(int x)
            {
                if ((Control.MouseButtons & MouseButtons.Left) == 0)
                {
                    CancelPendingReorder();
                    return;
                }

                if (_pendingColumnIndex < 0 || _owner._isDraggingColumn)
                {
                    return;
                }

                // Header coordinates move when the ListView scrolls during a drag.
                if (Math.Abs(Cursor.Position.X - _pendingStartX) < SystemInformation.DragSize.Width)
                {
                    return;
                }

                var columnIndex = _pendingColumnIndex;
                _pendingColumnIndex = -1;

                // DoDragDrop blocks for the whole drag, running its own
                // internal message loop.
                _owner.BeginColumnDragDrop(columnIndex);
            }
        }

        // Clears the selection on a left-button press anywhere else in the
        // app. This control's own handle (client area or scrollbar) is
        // never "outside"; the header is a separate native window and
        // needs its own exclusion.
        private sealed class OutsideClickDeselectFilter : IMessageFilter
        {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_NCLBUTTONDOWN = 0x00A1;

            private readonly ListView _owner;

            public OutsideClickDeselectFilter(ListView owner)
            {
                _owner = owner;
            }

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg != WM_LBUTTONDOWN && m.Msg != WM_NCLBUTTONDOWN)
                {
                    return false;
                }

                if (_owner.IsDisposed || !_owner.IsHandleCreated || _owner.SelectedItems.Count == 0)
                {
                    return false;
                }

                if (m.HWnd == _owner.Handle)
                {
                    return false;
                }

                var headerHandle = HeaderInputSubclass.GetHeaderHandle(_owner.Handle);
                if (headerHandle != System.IntPtr.Zero && m.HWnd == headerHandle)
                {
                    return false;
                }

                var clickedControl = Control.FromChildHandle(m.HWnd);
                if (clickedControl == null)
                {
                    // Not one of this process's own windows (e.g. a click
                    // in a different application) - nothing to react to.
                    return false;
                }

                if (clickedControl is ToolStripDropDown)
                {
                    // A context menu isn't "outside" either - otherwise
                    // clicking "Copy selection" cleared the very selection
                    // its own Click handler was about to read.
                    return false;
                }

                foreach (ListViewItem item in _owner.SelectedItems.Cast<ListViewItem>().ToList())
                {
                    item.Selected = false;
                }

                return false;
            }
        }
    }
}
