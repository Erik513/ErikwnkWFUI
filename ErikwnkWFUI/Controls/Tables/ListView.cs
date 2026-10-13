using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Localization;
using static ErikwnkWFUI.Native.NativeMethods;

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
    /// A dark-themed, multi-column ListView (Details view only: the other views - list, small and
    /// large icons, tiles - draw nothing, and <c>CheckBoxes</c> shows no boxes; group headers keep
    /// the system's color). Row selection -
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
    // In files by task: this one (the state, the constructor, the handle and the messages), ListView.Theme.cs (colors and
    // painting), ListView.Columns.cs (resizing and reordering), ListView.HeaderInput.cs (the input of the header),
    // ListView.Sorting.cs, ListView.Selection.cs, ListView.Copy.cs (copying and the menu), ListView.ToolTip.cs and
    // ListView.Display.cs (a list as big as its content).
    public partial class ListView : System.Windows.Forms.ListView
    {
        private const int DefaultMinimumColumnWidth = 40;

        // The measures at 96 dpi: they are scaled with the screen (see Px).
        private const int HeaderTextLeft96 = 7;
        private const int HeaderTextRight96 = 3;
        private const int CellTextLeft96 = 6;
        private const int CellTextRight96 = 3;
        private const int CheckBoxSize96 = 16;
        private const int CheckBoxLeft96 = 2;
        private const int CheckBoxGap96 = 6;
        private const int ResizeGripWidth96 = 5;
        private const int ToolTipOffsetX96 = 12;
        private const int ToolTipOffsetY96 = 18;

        // A measure of 96 dpi in the pixels of this screen.
        private int Px(int value96)
        {
            return Dpi.Scale(this, value96, DpiOverride);
        }

        // A seam for the tests: pretends another screen (0 is the real one).
        internal int DpiOverride;

        private int _headerHeight = 24;
        private int _pendingToggleDeselectItemIndex = -1;
        private readonly Timer _toggleDeselectSettleTimer;
        private int _toggleDeselectWatchIndex = -1;
        private bool _isRowRangeDragging;
        private int _rowRangeDragAnchorIndex = -1;
        private int _rowRangeDragLastAppliedIndex = -1;
        private readonly Timer _rowRangeDragPollTimer;
        private readonly OutsideClickFilter _outsideClickDeselectFilter;

        // The colors that follow the theme until they are set (see ThemeWatcher).
        private readonly ThemeColor _rowBackColor = new ThemeColor(() => UIColors.BackgroundMedium);
        private readonly ThemeColor _rowForeColor = new ThemeColor(() => UIColors.TextPrimary);
        private bool _backColorWasSet;
        // A translucent gray, so that the color of a row still shows through the selection (a gray needs more opacity than
        // a color to be seen on a row); a color set makes it the accent.
        private readonly ThemeColor _selectionOverlayColor = new ThemeColor(() => Color.FromArgb(130, UIColors.BorderLight));
        private readonly ThemeColor _headerBackColor = new ThemeColor(() => UIColors.BackgroundDarkElevated);
        private readonly ThemeColor _headerForeColor = new ThemeColor(() => UIColors.TextTertiary);
        private int _minimumColumnWidth = DefaultMinimumColumnWidth;
        private readonly ColumnFeatureSwitch _columnResizing = new ColumnFeatureSwitch();
        private readonly ColumnFeatureSwitch _columnReordering = new ColumnFeatureSwitch();
        // TextSecondary, not BorderLight: this marks where a dragged column will land and has to stand out (BorderLight on
        // the header measured under 3:1, hardly brighter than the divider next to it).
        private readonly ThemeColor _columnReorderIndicatorColor = new ThemeColor(() => UIColors.TextSecondary);
        private readonly ThemeColor _borderColor = new ThemeColor(() => UIColors.BorderMedium);
        // The default of Controls.ContextMenuStrip. The menu built in the constructor takes its highlight from here each
        // time it is painted (see BuildContextMenu), so a later change is seen by it.
        private readonly ThemeColor _contextMenuSelectionColor = new ThemeColor(() => UIColors.BorderLight);
        private bool _isDraggingColumn;
        private int _dragColumnIndex = -1;
        private int _dragInsertBeforeDisplayIndex = -1;
        private Font _headerFontOverride;
        private Font _defaultHeaderFont;
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
                ImageList previous = _rowHeightImageList;

                if (value <= 0)
                {
                    _rowHeightImageList = null;
                    SmallImageList = null;
                }
                else
                {
                    _rowHeightImageList = new ImageList { ImageSize = new Size(1, value) };
                    SmallImageList = _rowHeightImageList;
                }

                // (A list of images holds a native handle: the one that is replaced is given back.)
                previous?.Dispose();
                RequestFixedSize();
            }
        }

        public ListView()
        {
            // The system's own dark scroll bars on a dark theme. The other dark style for lists (ItemsView) would make
            // the group headers light and leave the scroll bars light, so only the header gets it.
            NativeControlTheme.Track(this);
            NativeControlTheme.Track(this, () => HeaderInputSubclass.GetHeaderHandle(Handle), NativeControlTheme.ApplyToItemsView);
            View = View.Details;
            FullRowSelect = true;
            HideSelection = true;
            MultiSelect = true;
            // Column reordering is done by the list itself (HeaderInputSubclass, OnDragOver/OnDragDrop), not by the native drag
            // (see ColumnReorderIndicatorColor). AllowDrop is only for that drag, not a drop target for the application
            // (and only on an STA thread, see DragDropSupport).
            AllowColumnReorder = false;
            DragDropSupport.EnableDropIfSta(this);
            BorderStyle = BorderStyle.None;
            base.BackColor = UIColors.BackgroundDark;
            ForeColor = RowForeColor;
            Font = UIFonts.Normal;
            // Clickable, so that ColumnClick is raised for sorting (SortingEnabled); the owner-drawn header has no native
            // "pressed" look.
            HeaderStyle = ColumnHeaderStyle.Clickable;
            OwnerDraw = true;

            ThemeWatcher.Attach(this, OnThemeChanged);

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
            // Runs only while a row-range drag is going on (OnListViewMouseDownForRowRangeDrag, OnRowRangeDragPollTick).
            _rowRangeDragPollTimer = new Timer { Interval = 40 };
            _rowRangeDragPollTimer.Tick += OnRowRangeDragPollTick;
            ItemSelectionChanged += OnItemSelectionChangedForToggleDeselect;
            // A safety net that runs for 1.5 seconds after a toggle-deselect (see OnListViewMouseUpForToggleDeselect).
            _toggleDeselectSettleTimer = new Timer { Interval = 1500 };
            _toggleDeselectSettleTimer.Tick += OnToggleDeselectSettleTimerTick;
            MouseMove += OnListViewMouseMoveForToolTip;
            MouseLeave += OnListViewMouseLeave;
            KeyDown += OnListViewKeyDown;
            ColumnWidthChanging += OnColumnWidthChanging;

            ContextMenuStrip = BuildContextMenu();
            UIStrings.LanguageChanged += OnUIStringsLanguageChanged;

            // A click somewhere else deselects (IsOutsideClickThatDeselects says what counts as somewhere else).
            _outsideClickDeselectFilter = new OutsideClickFilter(IsOutsideClickThatDeselects, DeselectAll, leftButtonOnly: true);
            _outsideClickDeselectFilter.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UIStrings.LanguageChanged -= OnUIStringsLanguageChanged;
                _cellToolTip.Dispose();
                _toggleDeselectSettleTimer.Stop();
                _toggleDeselectSettleTimer.Dispose();
                _rowRangeDragPollTimer.Stop();
                _rowRangeDragPollTimer.Dispose();
                _outsideClickDeselectFilter.Stop();
                _rowHeightImageList?.Dispose();
                _rowHeightImageList = null;
                _defaultHeaderFont?.Dispose();
                _defaultHeaderFont = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // The native list's own double buffering - without it, scrolling can leave stray gray streaks, since
            // owner-drawn content does not go through .NET's OnPaint/DoubleBuffered at all. Applied on every handle
            // (re)creation: this style lives on the native control, not anything .NET persists across a recreate.
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

            ApplyHeaderSizingStyle();

            // A display list now knows how high its header and rows really are.
            UpdateFixedSize();

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

        // Scrolling can leave a stray gray edge behind for a frame (native partial-repaint artifact on an owner-drawn
        // control) - forcing a full repaint on every scroll message gets rid of it.
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

            // A list that is as big as its content: a column or an item that comes, goes or changes is seen
            // here (the list is told once the message is done).
            NoteMessageForSize(m.Msg);

            if (m.Msg == WM_NCPAINT)
                PaintScrollBarCorner();

            if (m.Msg == WM_PAINT)
            {
                FillRowAreaTrailingBackground();
            }
        }
    }
}
