using System;
using System.Drawing;
using System.Windows.Forms;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    // ListView as big as its content: a fixed number of rows (FixedRowCount)
    // or as many as there are items (AutoSizeToContent).
    public partial class ListView
    {
        private int _fixedRowCount;
        private bool _autoSizeToContent;
        private bool _allowSelection = true;

        /// <summary>
        /// The number of rows the list has room for: 0 (the default) is an ordinary list, one that is as big as
        /// it is made and scrolls. More than 0 makes it a <b>display</b>: it is exactly as wide as its columns and
        /// as high as the header and this many rows, whatever size it is given, so it never needs a scroll bar
        /// (a <see cref="Control.MaximumSize"/> still limits it, as it does any control). Everything that does not change that size stays
        /// as it is: sorting, reordering the columns, selecting (switch them off with <see cref="SortingEnabled"/>,
        /// <see cref="AllowColumnReordering"/> and <see cref="AllowSelection"/> where the list only shows). A column
        /// that is made wider makes the list wider (<see cref="AllowColumnResizing"/> keeps them as they are).
        /// </summary>
        public int FixedRowCount
        {
            get { return _fixedRowCount; }
            set
            {
                _fixedRowCount = Math.Max(0, value);
                Scrollable = _fixedRowCount == 0;
                UpdateFixedSize();
            }
        }

        /// <summary>
        /// Whether the list is as big as its content: as wide as its columns and as high as the header and its
        /// items, so it grows and shrinks with them. <see cref="Control.MaximumSize"/> (a width or a height above 0)
        /// limits it; what does not fit then scrolls, and the scroll bar gets the room it needs on top of the
        /// content (a vertical one makes the list wider, a horizontal one higher, as far as the maximum allows),
        /// so it never covers part of the content. Off by default. <see cref="FixedRowCount"/> overrules it.
        /// </summary>
        public bool AutoSizeToContent
        {
            get { return _autoSizeToContent; }
            set
            {
                _autoSizeToContent = value;
                UpdateFixedSize();
            }
        }

        public override Size MaximumSize
        {
            get { return base.MaximumSize; }
            set
            {
                base.MaximumSize = value;
                RequestFixedSize();
            }
        }

        // Whether the size follows the content.
        private bool IsSizedToContent
        {
            get { return _fixedRowCount > 0 || _autoSizeToContent; }
        }

        /// <summary>
        /// Whether a row can be selected (by the mouse or the keyboard). True by default; a list that only shows
        /// its rows can turn it off, so a click does not highlight anything.
        /// </summary>
        public bool AllowSelection
        {
            get { return _allowSelection; }
            set
            {
                _allowSelection = value;

                if (!value)
                    SelectedItems.Clear();
            }
        }

        protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e)
        {
            base.OnItemSelectionChanged(e);

            if (!_allowSelection && e.IsSelected)
                e.Item.Selected = false;
        }

        // A list that is as big as its content is always exactly that, whatever size is asked for.
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (IsSizedToContent)
            {
                Size size = GetContentSize();
                width = size.Width;
                height = size.Height;
            }

            base.SetBoundsCore(x, y, width, height, specified);
        }

        private void UpdateFixedSize()
        {
            if (IsSizedToContent)
                Size = GetContentSize();
        }

        // After a change of the columns, the items, the fonts or the row height: the native list needs a moment
        // to lay itself out again before it can say how much room it needs.
        private void RequestFixedSize()
        {
            if (!IsSizedToContent)
                return;

            if (IsHandleCreated)
                BeginInvoke(new MethodInvoker(UpdateFixedSize));
            else
                UpdateFixedSize();
        }

        // What a message of the native list means for the size.
        private void NoteMessageForSize(int message)
        {
            if (message == LVM_INSERTCOLUMNW || message == LVM_DELETECOLUMN || message == LVM_SETCOLUMNW
                || message == LVM_INSERTITEMW || message == LVM_DELETEITEM || message == LVM_DELETEALLITEMS)
            {
                RequestFixedSize();
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            // The bold header font of the old font is not the one for the new.
            _defaultHeaderFont?.Dispose();
            _defaultHeaderFont = null;

            base.OnFontChanged(e);
            RequestFixedSize();
        }

        protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
        {
            base.OnColumnWidthChanged(e);
            RequestFixedSize();
        }

        // The size of the content: the width of the columns, the height of the header and the rows. A fixed number
        // of rows is as it is; with AutoSizeToContent the maximum limits it and the scroll bars that then appear
        // get their room on top (a limit is met by the content first, the bar takes its width or height from what
        // the maximum still allows).
        private Size GetContentSize()
        {
            int columns = 0;

            foreach (ColumnHeader column in Columns)
                columns += column.Width;

            int rows = _fixedRowCount > 0 ? _fixedRowCount : Items.Count;
            int rowsHeight = GetRowsHeight(rows);

            if (_fixedRowCount > 0)
                return new Size(columns, rowsHeight);

            Size maximum = MaximumSize;
            int barWidth = SystemInformation.VerticalScrollBarWidth * DeviceDpi / 96;
            int barHeight = SystemInformation.HorizontalScrollBarHeight * DeviceDpi / 96;
            int width = columns;
            int height = rowsHeight;
            bool vertical = false;
            bool horizontal = false;

            // (Each bar can make the other one necessary: two rounds settle it.)
            for (int round = 0; round < 2; round++)
            {
                width = columns + (vertical ? barWidth : 0);
                height = rowsHeight + (horizontal ? barHeight : 0);

                if (maximum.Width > 0 && width > maximum.Width)
                {
                    width = maximum.Width;
                    horizontal = true;
                }

                if (maximum.Height > 0 && height > maximum.Height)
                {
                    height = maximum.Height;
                    vertical = true;
                }
            }

            return new Size(width, height);
        }

        // The height of the header and this many rows. With a window the native list says how much room it needs
        // (its estimate is one row too high: it counts a row more than it was asked for); before it has one, an
        // estimate from the fonts.
        private int GetRowsHeight(int rows)
        {
            if (IsHandleCreated)
            {
                int stride = GetApproximateHeight(2) - GetApproximateHeight(1);

                return GetApproximateHeight(rows) - stride;
            }

            int row = (RowHeight > 0 ? RowHeight : Font.Height + 4) + 1;

            return HeaderFont.Height + 8 + rows * row;
        }

        private int GetApproximateHeight(int rows)
        {
            long result = (long)SendMessage(Handle, LVM_APPROXIMATEVIEWRECT, (IntPtr)rows, (IntPtr)(-1));

            return (int)((result >> 16) & 0xFFFF);
        }
    }
}
