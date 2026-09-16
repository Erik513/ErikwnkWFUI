using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A themed <see cref="System.Windows.Forms.DataGridView"/> for displaying
    /// data bound via <see cref="System.Windows.Forms.DataGridView.DataSource"/>
    /// (a DataTable, a List&lt;T&gt;, a BindingSource, ...). Unlike
    /// <see cref="ListView"/> - a plain native ListView has no
    /// data-binding support at all, and neither does anything built on top
    /// of it - this is the control to reach for when rows come from a bound
    /// source rather than being added by hand. A genuine WinForms control
    /// (not a thin wrapper around a native Win32 common control the way
    /// ListView is), so unlike ListView this needs none of that
    /// header-subclassing/WM_PAINT-interception - every visual it has is
    /// reachable through its own style properties.
    /// </summary>
    /// <remarks>
    /// Named the same as its own base class (a WinForms convention this
    /// library is moving toward, replacing the old "Styled" prefix) - the
    /// base type reference below has to stay fully qualified so the class
    /// doesn't try to inherit from itself. Consumers never need to spell out
    /// <c>ErikwnkWFUI.Controls.DataGridView</c> either: go through
    /// <see cref="UIStyles.DataGridViews.CreateStandard"/>, which hands back
    /// a plain <see cref="System.Windows.Forms.DataGridView"/>-typed
    /// reference.
    /// </remarks>
    public class DataGridView : System.Windows.Forms.DataGridView
    {
        private Color _headerBackColor = UIColors.BackgroundDarkElevated;
        private Color _headerForeColor = UIColors.TextTertiary;
        private Color _rowBackColor = UIColors.BackgroundMedium;
        private Color _alternateRowBackColor;
        private Color _rowForeColor = UIColors.TextPrimary;
        private Color _selectionBackColorOverride;
        private bool _selectionBackColorIsOverridden;

        // Snapshot of DataBoundItem references in the order they were bound,
        // captured on each fresh bind/reset - lets a header click's third
        // state ("original") restore that order without the underlying data
        // source needing to support sorting itself.
        private List<object> _originalOrder = new List<object>();
        private int _sortedColumnIndex = -1;
        private SortOrder _sortOrder = SortOrder.None;
        private bool _clearSelectionOnNextVisible;
        private bool _isApplyingInternalDataChange;

        /// <summary>Background color of the column header row.</summary>
        public Color HeaderBackColor
        {
            get => _headerBackColor;
            set { _headerBackColor = value; ApplyStyles(); }
        }

        /// <summary>Text color of the column header row.</summary>
        public Color HeaderForeColor
        {
            get => _headerForeColor;
            set { _headerForeColor = value; ApplyStyles(); }
        }

        /// <summary>Background color of a normal (not selected) row. Odd/even rows alternate between this and a slightly darker shade of it.</summary>
        public Color RowBackColor
        {
            get => _rowBackColor;
            set
            {
                _rowBackColor = value;
                _alternateRowBackColor = Darken(value, 5);
                ApplyStyles();
            }
        }

        /// <summary>Text color of a normal (not selected) row.</summary>
        public Color RowForeColor
        {
            get => _rowForeColor;
            set { _rowForeColor = value; ApplyStyles(); }
        }

        /// <summary>
        /// Background color of a selected cell/row. Follows the current
        /// accent (<see cref="UIColors.Primary"/>) live until explicitly
        /// set - same pattern as ListView's SelectionOverlayColor.
        /// </summary>
        public Color SelectionBackColor
        {
            get => _selectionBackColorIsOverridden ? _selectionBackColorOverride : UIColors.Primary;
            set
            {
                _selectionBackColorOverride = value;
                _selectionBackColorIsOverridden = true;
                ApplyStyles();
            }
        }

        /// <summary>
        /// Whether clicking a column header cycles it through ascending,
        /// descending, and original row order (see the "Column header
        /// sorting" remarks on <see cref="CycleSort"/>). Defaults to true;
        /// a consumer sets this to false - any time before rows are bound,
        /// since <see cref="OnColumnAdded"/> reads it once per column - to
        /// keep headers as plain labels for a grid where reordering rows
        /// wouldn't make sense. Clicking a header still clears a stray
        /// selection either way (see <see cref="HandleMouseDown"/>); only
        /// the sorting itself is affected. The glyph/arrow shown for the
        /// active sort column is always this control's own concern, not
        /// something a consumer sets.
        /// </summary>
        public bool SortingEnabled { get; set; } = true;

        public DataGridView()
        {
            _alternateRowBackColor = Darken(_rowBackColor, 5);

            BackgroundColor = UIColors.BackgroundDark;
            GridColor = UIColors.BorderMedium;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            RowHeadersVisible = false;

            // Without this, the column headers ignore
            // ColumnHeadersDefaultCellStyle entirely and always render
            // with the OS's own visual-style theme instead - the
            // DataGridView equivalent of the SetWindowTheme("", "") dance
            // BorderedProgressBar needs for the exact same reason.
            EnableHeadersVisualStyles = false;

            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            MultiSelect = true;
            SelectionMode = DataGridViewSelectionMode.CellSelect;
            Font = UIFonts.Normal;
            RowTemplate.Height = Font.Height + 12;

            EnableDoubleBuffering();
            ApplyStyles();

            MouseDown += HandleMouseDown;
        }

        protected override void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)
        {
            base.OnDataBindingComplete(e);

            // Binding to a new DataSource replaces the row collection
            // entirely, which resets per-row visual state - reapplying
            // here keeps alternating-row/selection colors correct for
            // whatever just got bound in, not just whatever existed at
            // construction time.
            ApplyStyles();

            if (_isApplyingInternalDataChange)
            {
                // This Reset was caused by CycleSort/ReorderDataSource
                // rewriting the bound list's contents to apply or clear a
                // sort, not a real rebind - the bookkeeping below would
                // otherwise stomp on the sort state being applied right now.
                return;
            }

            // A Reset is a fresh bind (DataSource assigned, or the bound
            // list reset) - that's the only time a "no cell selected on
            // load" default and a fresh original-order snapshot make sense.
            // Incremental changes (a row added/edited) leave both alone, so
            // typing into the grid doesn't fight the user's own selection.
            if (e.ListChangedType == ListChangedType.Reset)
            {
                ClearSelection();
                CurrentCell = null;

                // Binding normally happens before the control is ever shown,
                // and WinForms auto-selects the first cell the first time a
                // grid with rows but no CurrentCell receives focus - which
                // happens later, as part of the containing form being shown.
                // Clearing it again once actually visible is what defeats
                // that (setting it here alone doesn't stick).
                _clearSelectionOnNextVisible = true;

                _originalOrder = new List<object>();
                foreach (DataGridViewRow row in Rows)
                {
                    if (row.DataBoundItem != null)
                    {
                        _originalOrder.Add(row.DataBoundItem);
                    }
                }

                _sortedColumnIndex = -1;
                _sortOrder = SortOrder.None;

                foreach (DataGridViewColumn column in Columns)
                {
                    column.HeaderCell.SortGlyphDirection = SortOrder.None;
                    column.HeaderText = StripSortSuffix(column.HeaderText);
                }
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);

            if (!Visible || !_clearSelectionOnNextVisible || !IsHandleCreated)
            {
                return;
            }

            _clearSelectionOnNextVisible = false;

            // Queued rather than called directly - becoming visible is what
            // triggers the framework's own auto-select in the first place,
            // so clearing it has to happen after that finishes, not during.
            BeginInvoke(new Action(() =>
            {
                ClearSelection();
                CurrentCell = null;
            }));
        }

        protected override void OnColumnAdded(DataGridViewColumnEventArgs e)
        {
            base.OnColumnAdded(e);

            if (!SortingEnabled)
            {
                return;
            }

            // Programmatic hands full control of header-click behavior to
            // CycleSort below - Automatic would have the base DataGridView
            // attempt its own sort first (and throw if the bound list
            // doesn't support IBindingList sorting, which a plain
            // BindingList&lt;T&gt; doesn't).
            e.Column.SortMode = DataGridViewColumnSortMode.Programmatic;
        }

        protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
        {
            base.OnColumnHeaderMouseClick(e);

            // base's own header-click handling can move CurrentCell to the
            // column's first row as a side effect - SortMode.Programmatic
            // only suppresses its automatic sort, not that. Clearing again
            // here (in addition to HandleMouseDown's clear on the way in)
            // is what stops that from ever reaching the screen.
            ClearSelection();
            CurrentCell = null;

            if (SortingEnabled && e.Button == MouseButtons.Left && e.ColumnIndex >= 0)
            {
                CycleSort(e.ColumnIndex);
            }
        }

        // Clicking truly empty space below the rows (no cell there at all,
        // so OnCellMouseDown below never fires) leaves a previously
        // selected cell visually selected by default - not a meaningful
        // selection target, so this clears it.
        private void HandleMouseDown(object sender, MouseEventArgs e)
        {
            HitTestInfo hitTest = HitTest(e.X, e.Y);

            if (hitTest.Type == DataGridViewHitTestType.None)
            {
                ClearSelection();
                CurrentCell = null;
            }
        }

        protected override void OnCellMouseDown(DataGridViewCellMouseEventArgs e)
        {
            base.OnCellMouseDown(e);

            if (e.RowIndex != -1)
            {
                return;
            }

            // The MouseDown event (see HandleMouseDown) fires from
            // Control.OnMouseDown, which DataGridView's own OnMouseDown
            // override calls partway through its own processing - moving
            // CurrentCell to this column's first row, as a side effect of
            // pressing a header, happens to fall AFTER that point, so
            // clearing selection there never stuck (visible for as long as
            // the button stayed down, before any Click/MouseUp fired).
            // base.OnCellMouseDown above is exactly where that side effect
            // itself happens, so clearing immediately after it - still
            // within the same synchronous mouse-down handling, before the
            // control repaints - is what actually keeps it from showing.
            ClearSelection();
            CurrentCell = null;
        }

        /// <summary>
        /// Three-state header click: ascending, then descending, then back
        /// to the order rows were in when last bound - clicking a different
        /// column starts that column fresh at ascending.
        /// </summary>
        private void CycleSort(int columnIndex)
        {
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

            foreach (DataGridViewColumn column in Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
                column.HeaderText = StripSortSuffix(column.HeaderText);
            }

            if (_sortOrder == SortOrder.None)
            {
                _sortedColumnIndex = -1;
                ReorderDataSource(BuildOriginalOrder());
            }
            else
            {
                DataGridViewColumn sortedColumn = Columns[columnIndex];
                sortedColumn.HeaderCell.SortGlyphDirection = _sortOrder;
                sortedColumn.HeaderText = StripSortSuffix(sortedColumn.HeaderText) +
                    (_sortOrder == SortOrder.Ascending ? AscendingSuffix : DescendingSuffix);
                ReorderDataSource(BuildSortedOrder(columnIndex, _sortOrder));
            }

            ClearSelection();
            CurrentCell = null;
        }

        // SortGlyphDirection alone doesn't reliably render against this
        // control's dark custom styling (EnableHeadersVisualStyles = false
        // bypasses the OS glyph painting it normally rides along with), so
        // the direction is spelled out in the header text itself instead -
        // guaranteed visible regardless of theme.
        private const string AscendingSuffix = " ▲";
        private const string DescendingSuffix = " ▼";

        private static string StripSortSuffix(string headerText)
        {
            if (headerText == null)
            {
                return headerText;
            }

            if (headerText.EndsWith(AscendingSuffix, StringComparison.Ordinal))
            {
                return headerText.Substring(0, headerText.Length - AscendingSuffix.Length);
            }

            if (headerText.EndsWith(DescendingSuffix, StringComparison.Ordinal))
            {
                return headerText.Substring(0, headerText.Length - DescendingSuffix.Length);
            }

            return headerText;
        }

        // DataGridView.Sort(IComparer) is a no-op for the display order when
        // the control is data bound (it only works unbound, or against a
        // source that implements IBindingListView) - so instead, the bound
        // list itself is rewritten in the desired order. Any IList data
        // source works, not just BindingList&lt;T&gt;.
        // A plain Clear()+per-item Add() fires one change notification per
        // call - each one gets picked up and repainted immediately, which
        // includes the moment the list is briefly empty and whatever
        // DataGridView's own reaction to that Reset selects as a result.
        // That intermediate repaint is what showed up as the first row
        // flashing "selected" while a sort was applied. Suppressing
        // notifications for the whole rewrite and raising exactly one
        // Reset via ResetBindings() at the end (both BindingList&lt;T&gt;
        // members - public and protected respectively, reached via
        // reflection the same way EnableDoubleBuffering reaches a
        // protected Control property) collapses that into a single
        // repaint, with no intermediate state to flash.
        private static readonly BindingFlags AnyInstanceMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private void ReorderDataSource(List<object> desiredOrder)
        {
            if (!(DataSource is IList list))
            {
                return;
            }

            object dataSource = DataSource;
            Type dataSourceType = dataSource.GetType();

            PropertyInfo raiseEventsProperty =
                dataSourceType.GetProperty("RaiseListChangedEvents", AnyInstanceMember);
            bool canSuppressEvents =
                raiseEventsProperty != null && raiseEventsProperty.CanRead && raiseEventsProperty.CanWrite;
            bool previousRaiseEvents = true;

            if (canSuppressEvents)
            {
                previousRaiseEvents = (bool)raiseEventsProperty.GetValue(dataSource);
                raiseEventsProperty.SetValue(dataSource, false);
            }

            _isApplyingInternalDataChange = true;

            try
            {
                list.Clear();

                foreach (object item in desiredOrder)
                {
                    list.Add(item);
                }
            }
            finally
            {
                // Stays true through ResetBindings() below, not just the
                // Clear()/Add() loop above - ResetBindings() is what
                // actually raises the (suppressed-until-now) Reset
                // notification, so it re-enters OnDataBindingComplete on
                // this same call stack. Flipping the flag off before that
                // would let that reentrant call run the "fresh bind"
                // bookkeeping again mid-sort - wiping out the glyph/order
                // CycleSort just set, and calling ClearSelection() /
                // CurrentCell = null while the grid is mid-reset, which is
                // exactly the kind of reentrant state change DataGridView
                // can throw InvalidOperationException over.
                if (canSuppressEvents)
                {
                    raiseEventsProperty.SetValue(dataSource, previousRaiseEvents);

                    MethodInfo resetBindingsMethod = dataSourceType.GetMethod(
                        "ResetBindings",
                        AnyInstanceMember,
                        null,
                        Type.EmptyTypes,
                        null);

                    resetBindingsMethod?.Invoke(dataSource, null);
                }

                _isApplyingInternalDataChange = false;
            }

            ClearSelection();
            CurrentCell = null;
        }

        private List<object> BuildSortedOrder(int columnIndex, SortOrder sortOrder)
        {
            int direction = sortOrder == SortOrder.Descending ? -1 : 1;
            List<KeyValuePair<object, object>> itemsWithValues = new List<KeyValuePair<object, object>>();

            foreach (DataGridViewRow row in Rows)
            {
                if (row.DataBoundItem != null)
                {
                    itemsWithValues.Add(new KeyValuePair<object, object>(
                        row.DataBoundItem,
                        row.Cells[columnIndex].Value));
                }
            }

            itemsWithValues.Sort((a, b) => CompareCellValues(a.Value, b.Value, direction));

            List<object> ordered = new List<object>(itemsWithValues.Count);

            foreach (KeyValuePair<object, object> pair in itemsWithValues)
            {
                ordered.Add(pair.Key);
            }

            return ordered;
        }

        // Rows added after the last bind/reset (e.g. a new row the user just
        // typed into) sort after every row that was present at that
        // snapshot, rather than colliding at index -1.
        private List<object> BuildOriginalOrder()
        {
            List<KeyValuePair<object, int>> itemsWithIndex = new List<KeyValuePair<object, int>>();

            foreach (DataGridViewRow row in Rows)
            {
                if (row.DataBoundItem == null)
                {
                    continue;
                }

                int index = _originalOrder.IndexOf(row.DataBoundItem);
                itemsWithIndex.Add(new KeyValuePair<object, int>(
                    row.DataBoundItem,
                    index >= 0 ? index : _originalOrder.Count));
            }

            itemsWithIndex.Sort((a, b) => a.Value.CompareTo(b.Value));

            List<object> ordered = new List<object>(itemsWithIndex.Count);

            foreach (KeyValuePair<object, int> pair in itemsWithIndex)
            {
                ordered.Add(pair.Key);
            }

            return ordered;
        }

        private static int CompareCellValues(object valueX, object valueY, int direction)
        {
            if (valueX == null && valueY == null)
            {
                return 0;
            }

            if (valueX == null)
            {
                return -1 * direction;
            }

            if (valueY == null)
            {
                return 1 * direction;
            }

            if (valueX is IComparable comparable)
            {
                try
                {
                    return comparable.CompareTo(valueY) * direction;
                }
                catch (ArgumentException)
                {
                    // Falls through to the string comparison below when the
                    // two values aren't directly comparable to each other
                    // (e.g. different underlying cell types).
                }
            }

            return string.Compare(
                Convert.ToString(valueX),
                Convert.ToString(valueY),
                StringComparison.CurrentCultureIgnoreCase) * direction;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            ApplyStyles();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            ApplyStyles();
        }

        private void ApplyStyles()
        {
            // Flat gray when disabled - every control in this library mutes
            // this same way, no exceptions for a control's own fixed/
            // accent colors (see ListView's SetEnabledStyle-style
            // handling, and UIProgressBarFactory's disabled fill).
            Color rowForeColor = Enabled ? _rowForeColor : UIColors.DisabledGray;
            Color headerForeColor = Enabled ? _headerForeColor : UIColors.DisabledGray;
            Color selectionBackColor = Enabled ? SelectionBackColor : UIColors.DisabledGray;
            Color selectionForeColor = UIColors.GetContrastingForeColor(selectionBackColor);

            using (var headerFont = new Font(Font, FontStyle.Bold))
            {
                ColumnHeadersDefaultCellStyle.BackColor = _headerBackColor;
                ColumnHeadersDefaultCellStyle.ForeColor = headerForeColor;
                ColumnHeadersDefaultCellStyle.Font = headerFont;
                ColumnHeadersDefaultCellStyle.SelectionBackColor = _headerBackColor;
                ColumnHeadersDefaultCellStyle.SelectionForeColor = headerForeColor;
            }

            DefaultCellStyle.BackColor = _rowBackColor;
            DefaultCellStyle.ForeColor = rowForeColor;
            DefaultCellStyle.SelectionBackColor = selectionBackColor;
            DefaultCellStyle.SelectionForeColor = selectionForeColor;

            AlternatingRowsDefaultCellStyle.BackColor = _alternateRowBackColor;
            AlternatingRowsDefaultCellStyle.ForeColor = rowForeColor;
            AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionBackColor;
            AlternatingRowsDefaultCellStyle.SelectionForeColor = selectionForeColor;

            Invalidate();
        }

        private void EnableDoubleBuffering()
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(this, true, null);
        }

        private static Color Darken(Color color, int amount)
        {
            return Color.FromArgb(
                Math.Max(0, color.R - amount),
                Math.Max(0, color.G - amount),
                Math.Max(0, color.B - amount));
        }
    }
}
