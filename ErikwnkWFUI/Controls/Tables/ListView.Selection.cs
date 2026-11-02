using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // Selecting: a click on the selected item deselects it, a press on a row and a move to another selects the rows between, a click elsewhere deselects.
    public partial class ListView : System.Windows.Forms.ListView
    {

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

        // A click on the selected item does not deselect it natively; here it does. At MouseDown it is noted whether the
        // pressed item is the only selected one, at MouseUp it is deselected if the button came up on the same item.
        private void OnListViewMouseDownForToggleDeselect(object sender, MouseEventArgs e)
        {
            _pendingToggleDeselectItemIndex = -1;

            // A new press ends the watch of the last toggle-deselect; otherwise a quick second click on the row would look like
            // the delayed reselect below and be undone.
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

                // comctl32 arms a "click to rename" timer for a press on a selected, focused item; a second later it selects the
                // item again. OnItemSelectionChangedForToggleDeselect undoes that at once, the timer is the safety net.
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

            // (The watch ends first: the setter below raises this event again.)
            _toggleDeselectWatchIndex = -1;
            _toggleDeselectSettleTimer.Stop();
            e.Item.Selected = false;
        }

        // The reselect did not come: the watch ends.
        private void OnToggleDeselectSettleTimerTick(object sender, EventArgs e)
        {
            _toggleDeselectSettleTimer.Stop();
            _toggleDeselectWatchIndex = -1;
        }

        // The native rubber band starts only on empty space. A press on a row and a move to another row while the button is
        // held select all rows between them, as in Explorer. A timer follows the mouse (MouseMove and MouseUp stop coming
        // once it leaves the list, which would end the scrolling at the edge).
        private void OnListViewMouseDownForRowRangeDrag(object sender, MouseEventArgs e)
        {
            _isRowRangeDragging = false;
            _rowRangeDragAnchorIndex = -1;
            _rowRangeDragPollTimer.Stop();

            if (e.Button != MouseButtons.Left || ModifierKeys != Keys.None)
            {
                return;
            }

            var hitTest = SafeHitTest(e.Location);
            if (hitTest.Item == null)
            {
                // (A press on empty space is the native rubber band's.)
                return;
            }

            _isRowRangeDragging = true;
            _rowRangeDragAnchorIndex = hitTest.Item.Index;
            _rowRangeDragLastAppliedIndex = hitTest.Item.Index;
            _rowRangeDragPollTimer.Start();
        }

        // The release on the list ends it at once; the timer is for a release elsewhere.
        private void OnListViewMouseUpForRowRangeDrag(object sender, MouseEventArgs e)
        {
            _isRowRangeDragging = false;
            _rowRangeDragAnchorIndex = -1;
            _rowRangeDragPollTimer.Stop();
        }

        // The same protection as WndProc's scrolling: TopItem scrolls by another path than those messages and would show the
        // same stray line.
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

            if ((PressedButtons() & MouseButtons.Left) == 0)
            {
                _isRowRangeDragging = false;
                _rowRangeDragPollTimer.Stop();
                return;
            }

            var location = PointToClient(CursorPosition());

            // (Only for the eye: GetNearestRowIndex clamps to the first and last row, the selection does not depend on this.)
            int direction = location.Y < _headerHeight ? -1 : location.Y >= ClientSize.Height ? 1 : 0;

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

        // The item under the point, or the nearest row in that direction (the first or last of the whole list).
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

        // (The native list view has no "select all".)
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

        // Whether a left-button press in the window counts as "somewhere else" for the selection. The list itself (client
        // area, scroll bars) and its header, a window of its own, do not; neither do the controls that give a command - a
        // button, a tool bar, a menu bar or its menus - because a command like "Delete" works on the selection and would
        // find it gone.
        private bool IsOutsideClickThatDeselects(System.IntPtr hwnd)
        {
            if (!DeselectOnOutsideClick || IsDisposed || !IsHandleCreated || SelectedItems.Count == 0)
            {
                return false;
            }

            // (The header is a window of its own, inside the list.)
            return SelectionOutsideClick.IsElsewhere(hwnd, this, null, HeaderInputSubclass.GetHeaderHandle(Handle));
        }

        private void DeselectAll()
        {
            foreach (ListViewItem item in SelectedItems.Cast<ListViewItem>().ToList())
            {
                item.Selected = false;
            }
        }
    }
}
