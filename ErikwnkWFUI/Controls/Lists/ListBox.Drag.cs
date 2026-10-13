using System;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Controls
{
    // The reordering of the list box: the drag by the handle, the drop line, and MoveItem. The settings, the mouse and
    // the colors are in ListBox.cs, the painting in ListBox.Drawing.cs.
    public partial class ListBox
    {
        protected override void OnDragLeave(EventArgs e)
        {
            InvalidateDragIndicator();

            _dragInsertPosition = -1;

            base.OnDragLeave(e);
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            // Only a drag that started on one of the handles of this list is a reordering; anything else that is
            // dragged over it (files, items of another control) is left to the handlers of the application, which
            // decide what it may do.
            if (!CanReorder || !_isDragging || _dragIndex == -1)
            {
                base.OnDragOver(drgevent);
                return;
            }

            Point point = PointToClient(new Point(drgevent.X, drgevent.Y));
            int newInsertPosition = GetInsertPosition(point, out DragDropEffects effect);

            drgevent.Effect = effect;

            if (_dragInsertPosition != newInsertPosition)
            {
                InvalidateDragIndicator();
                _dragInsertPosition = newInsertPosition;
                InvalidateDragIndicator();
            }

            AutoScrollDuringDrag(point, drgevent);

            base.OnDragOver(drgevent);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            Point point = PointToClient(new Point(drgevent.X, drgevent.Y));

            bool droppedInside = ClientRectangle.Contains(point);

            if (droppedInside && CanReorder && _dragIndex != -1 && _dragInsertPosition != -1)
            {
                ReorderDraggedItem();
            }

            ResetDragState();

            _hoverIndex = -1;
            Invalidate();

            base.OnDragDrop(drgevent);
        }

        protected override void OnGiveFeedback(GiveFeedbackEventArgs gfbevent)
        {
            gfbevent.UseDefaultCursors = false;
            Cursor.Current = Cursors.SizeAll;

            base.OnGiveFeedback(gfbevent);
        }

        /// <summary>
        /// Moves an item to a new index without user drag interaction - does not raise <see cref="ItemsReordered"/>.
        /// An index that is not there, or the same one twice, changes nothing. Not for a sorted list (it puts every
        /// item in its own place) or one with a <c>DataSource</c> (its items cannot be moved): that is an
        /// <see cref="InvalidOperationException"/>, before anything is touched.
        /// </summary>
        public void MoveItem(int fromIndex, int toIndex)
        {
            if (Sorted || DataSource != null)
                throw new InvalidOperationException("The items of a sorted list, or of one with a DataSource, cannot be moved.");

            if (fromIndex < 0 || fromIndex >= Items.Count || toIndex < 0 || toIndex >= Items.Count || fromIndex == toIndex)
                return;

            BeginUpdate();

            try
            {
                object item = Items[fromIndex];
                Items.RemoveAt(fromIndex);
                Items.Insert(toIndex, item);

                SelectIndex(toIndex);
            }
            finally
            {
                EndUpdate();
            }

            ItemsReordered?.Invoke(this, EventArgs.Empty);
        }

        private bool TryPrepareDrag(int index, Rectangle itemRect, Point location)
        {
            if (!CanReorder || index == -1)
                return false;

            Rectangle dragRect = GetDragHandleRectangle(itemRect);

            if (!dragRect.Contains(location))
                return false;

            _dragIndex = index;
            _dragStartPoint = location;
            SelectIndex(index);

            return true;
        }

        private void TryStartDrag(MouseEventArgs e)
        {
            if (!CanReorder || e.Button != MouseButtons.Left || _isDragging || _dragIndex == -1)
                return;

            bool movedEnough =
                Math.Abs(e.X - _dragStartPoint.X) > SystemInformation.DragSize.Width ||
                Math.Abs(e.Y - _dragStartPoint.Y) > SystemInformation.DragSize.Height;

            if (!movedEnough)
                return;

            _isDragging = true;

            try
            {
                // Returns when the item was dropped (here or elsewhere) or the drag was given up (Esc): the mouse button
                // went up inside the drag, so no MouseUp tells this list that it is over.
                if (DragDropStarter != null)
                    DragDropStarter(_dragIndex);
                else
                    DoDragDrop(_dragIndex, DragDropEffects.Move);
            }
            finally
            {
                ResetDragState();
            }
        }

        // What starts the drag of the system (a seam for the tests, which have no message loop for it).
        internal Action<int> DragDropStarter;

        // A list without a selection (SelectionMode.None) can still be reordered: nothing is selected there.
        private void SelectIndex(int index)
        {
            if (SelectionMode != SelectionMode.None)
                SelectedIndex = index;
        }

        private int GetInsertPosition(Point point, out DragDropEffects effect)
        {
            effect = DragDropEffects.None;

            if (Items.Count == 0)
            {
                effect = DragDropEffects.Move;
                return 0;
            }

            int targetIndex = IndexFromPoint(point);

            if (targetIndex != -1)
            {
                Rectangle itemRect = GetItemRectangle(targetIndex);
                bool isTopHalf = point.Y < itemRect.Y + itemRect.Height / 2;

                effect = DragDropEffects.Move;
                return isTopHalf ? targetIndex : targetIndex + 1;
            }

            if (point.Y > GetItemRectangle(Items.Count - 1).Bottom)
            {
                effect = DragDropEffects.Move;
                return Items.Count;
            }

            return -1;
        }

        // The list scrolls while a dragged item is within this distance (96 dpi) of its top or bottom edge.
        private const int AutoScrollZone96 = 20;

        private void AutoScrollDuringDrag(Point point, DragEventArgs drgevent)
        {
            int zone = Px(AutoScrollZone96);

            if (point.Y < zone && TopIndex > 0)
            {
                TopIndex--;
                drgevent.Effect = DragDropEffects.Move;
            }
            else if (point.Y > ClientSize.Height - zone && TopIndex < Items.Count - 1)
            {
                TopIndex++;
                drgevent.Effect = DragDropEffects.Move;
            }
        }

        private void ReorderDraggedItem()
        {
            // The items may have changed while the item was dragged (a timer, another thread's result): what is
            // no longer there cannot be moved.
            if (_dragIndex < 0 || _dragIndex >= Items.Count)
                return;

            int targetPosition = _dragInsertPosition;

            if (targetPosition > _dragIndex)
                targetPosition--;

            targetPosition = Math.Max(0, Math.Min(targetPosition, Items.Count));

            if (targetPosition == _dragIndex)
                return;

            BeginUpdate();

            try
            {
                object draggedItem = Items[_dragIndex];

                Items.RemoveAt(_dragIndex);

                if (targetPosition > Items.Count)
                    targetPosition = Items.Count;

                Items.Insert(targetPosition, draggedItem);
                SelectIndex(targetPosition);

                EnsureItemVisible(targetPosition);
            }
            finally
            {
                EndUpdate();
            }

            ItemsReordered?.Invoke(this, EventArgs.Empty);
        }

        private void EnsureItemVisible(int index)
        {
            if (index < TopIndex)
            {
                TopIndex = index;
                return;
            }

            int visibleItemCount = ClientSize.Height / ItemHeight;
            int lastVisibleIndex = TopIndex + visibleItemCount - 1;

            if (index > lastVisibleIndex)
            {
                TopIndex = index - visibleItemCount + 1;
            }
        }

        private void ResetDragState()
        {
            bool needsInvalidate = _isDragging || _dragIndex != -1 || _dragInsertPosition != -1;

            _isDragging = false;
            _dragIndex = -1;
            _dragInsertPosition = -1;

            if (needsInvalidate)
                Invalidate();
        }

        // (The rectangle of an item already stops where the scroll bar starts, so the handle needs no room for it.)
        private Rectangle GetDragHandleRectangle(Rectangle itemRect)
        {
            int x = itemRect.Right - Px(DragHandleWidth);

            return new Rectangle(
                x - Px(DragHandleHitAreaPadding),
                itemRect.Y,
                Px(DragHandleWidth) + Px(DragHandleHitAreaPadding) * 2,
                itemRect.Height);
        }

        private void InvalidateDragIndicator()
        {
            if (_dragInsertPosition == -1)
                return;

            int y = GetDragIndicatorYPosition();
            Invalidate(new Rectangle(0, Math.Max(0, y - 4), Width, 8));
        }
    }
}
