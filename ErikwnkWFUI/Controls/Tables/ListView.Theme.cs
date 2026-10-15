using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // Colors and the look: the properties of the colors, the theme and the accent, the header and the cells painted, the room right of the columns, the corner of the scroll bars.
    public partial class ListView : System.Windows.Forms.ListView
    {

        /// <summary>The color behind and below the rows; follows the theme until it is set.</summary>
        public override Color BackColor
        {
            get { return base.BackColor; }
            set
            {
                _backColorWasSet = !value.IsEmpty;
                base.BackColor = value;
            }
        }

        // The theme or the accent changed while the list is shown: what was not set by hand follows (the scroll bars
        // and the header's style through NativeControlTheme.Track), the list is painted anew.
        private void OnThemeChanged()
        {
            if (!_backColorWasSet)
                base.BackColor = UIColors.BackgroundDark;

            if (!_rowForeColor.IsSet)
                ForeColor = RowForeColor;

            Invalidate(true);
        }

        // The look of a color set (what UIStyles.ListViews.CreateStandard(colors) and CreatePrimary make): the set's
        // hover color is the color of the look. Asked for again whenever the theme or the accent changes.
        internal void FollowColors(Func<UIColorSet> colors)
        {
            // (No set handed over: the list keeps its own colors.)
            if (colors == null)
                return;

            _borderColor.SetDefault(() => colors().Hover);
            _selectionOverlayColor.SetDefault(() => UIColors.CreateSelectionOverlay(colors().Hover));
            _columnReorderIndicatorColor.SetDefault(() => colors().Hover);
            _contextMenuSelectionColor.SetDefault(() => colors().Hover);

            Invalidate();
        }

        /// <summary>Background color of a normal (not selected) row. Odd/even rows alternate between this and a slightly darker shade of it.</summary>
        public Color RowBackColor
        {
            get { return _rowBackColor.Value; }
            set
            {
                _rowBackColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>Text color of a normal (not selected) row.</summary>
        public Color RowForeColor
        {
            get { return _rowForeColor.Value; }
            set
            {
                _rowForeColor.Set(value);
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
            set { _contextMenuSelectionColor.Set(value); }
        }

        /// <summary>Background color of the column header row.</summary>
        public Color HeaderBackColor
        {
            get { return _headerBackColor.Value; }
            set
            {
                _headerBackColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>Text color of the column header row.</summary>
        public Color HeaderForeColor
        {
            get { return _headerForeColor.Value; }
            set
            {
                _headerForeColor.Set(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Font for the column header row - defaults to a bold version of
        /// <see cref="Control.Font"/> if never set. Setting this to a larger
        /// size is also the way to make the native header band itself
        /// taller (there's no direct "header height" API on the underlying
        /// Win32 header control - it sizes itself from its own font
        /// metrics, same as row height below). The list holds the font it
        /// returns: a font that was set stays the caller's to dispose, one the
        /// list made goes with it - do not dispose what is read here.
        /// </summary>
        public Font HeaderFont
        {
            get { return _headerFontOverride ?? _defaultHeaderFont ?? (_defaultHeaderFont = new Font(Font, FontStyle.Bold)); }
            set
            {
                _headerFontOverride = value;
                Invalidate();
                RequestFixedSize();
            }
        }

        // The room right of the last column, below the header, gets no DrawSubItem call: it would show the plain BackColor
        // and make a seam under the header's own trailing fill. It is filled with the HeaderBackColor, so that header and
        // rows read as one band.
        private void FillRowAreaTrailingBackground()
        {
            if (Columns.Count == 0)
            {
                return;
            }

            var rightEdge = GetColumnsTotalWidth();

            using (var graphics = Graphics.FromHwnd(Handle))
            {
                // Below the last row (the strip a list that is not a whole number of rows high leaves, or the room
                // of rows that are not there): the native list paints its column dividers there in the dark theme,
                // as thin lines over an empty area. Painted over with the plain background.
                int below = GetRowsBottom();

                if (below < ClientSize.Height)
                {
                    using (var plain = new SolidBrush(BackColor))
                    {
                        graphics.FillRectangle(plain, 0, below, Math.Min(rightEdge, ClientSize.Width), ClientSize.Height - below);
                    }
                }

                if (rightEdge >= ClientSize.Width)
                {
                    return;
                }

                using (var background = new SolidBrush(HeaderBackColor))
                {
                    graphics.FillRectangle(background, rightEdge, _headerHeight, ClientSize.Width - rightEdge, ClientSize.Height - _headerHeight);
                }
            }
        }

        // Where the rows end: the bottom of the last one, or the bottom of the header while there is none. At or
        // beyond the bottom of the list when the rows fill it (or go on below it).
        private int GetRowsBottom()
        {
            if (Items.Count == 0)
                return _headerHeight;

            return Math.Max(_headerHeight, Items[Items.Count - 1].Bounds.Bottom);
        }

        // The columns' combined width: where the content ends and the trailing background begins. Read from Columns each
        // time (the widths are current the moment a resize sets them), not kept from the last header paint: the header
        // and the rows are two native windows that do not repaint in step, and a kept value left one of them with the
        // strip in the wrong place.
        private int GetColumnsTotalWidth()
        {
            int total = 0;
            foreach (ColumnHeader column in Columns)
            {
                total += column.Width;
            }

            return total;
        }

        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            _headerHeight = e.Bounds.Height;

            Font font = HeaderFont;

            using (var background = new SolidBrush(HeaderBackColor))
            using (var divider = new Pen(BorderColor))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
                e.Graphics.DrawRectangle(divider, e.Bounds.Left, e.Bounds.Top, e.Bounds.Width - 1, e.Bounds.Height - 1);
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Header.Text,
                    font,
                    new Rectangle(e.Bounds.X + Px(HeaderTextLeft96), e.Bounds.Y, Math.Max(0, e.Bounds.Width - Px(HeaderTextLeft96 + HeaderTextRight96)), e.Bounds.Height),
                    GetEffectiveColor(HeaderForeColor),
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

            ColumnHeaderPainting.DrawSortGlyph(e.Graphics, e.Bounds, Font, GetEffectiveColor(HeaderForeColor), _sortOrder);
        }

        // Drawn with the header cell, not as an overlay (see ColumnReorderIndicatorColor).
        private void DrawColumnDragInsertionLine(DrawListViewColumnHeaderEventArgs e)
        {
            if (!_columnDrag.IsDragging)
            {
                return;
            }

            ColumnHeaderPainting.DrawColumnDragInsertionLine(
                e.Graphics, e.Bounds, e.Header.DisplayIndex, Columns.Count, _columnDrag.InsertBeforeDisplayIndex, ColumnReorderIndicatorColor);
        }

        private static void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // Details view paints complete rows in OnDrawSubItem.
        }

        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // The horizontal bounds of the cell are the header's, which know scrolling and the reordered columns.
            var bounds = TryGetHeaderBounds(e.ColumnIndex, out Rectangle headerBounds)
                ? new Rectangle(headerBounds.Left, e.Bounds.Top, headerBounds.Width, e.Bounds.Height)
                : e.Bounds;

            var baseBackColor = e.ItemIndex % 2 == 0 ? RowBackColor : UIColors.Darken(RowBackColor, 5);
            using (var background = new SolidBrush(baseBackColor))
            {
                e.Graphics.FillRectangle(background, bounds);
            }

            // (FullRowSelect: all cells of a row are selected or not, as the native list says.)
            if (e.Item.Selected)
            {
                using (var overlay = new SolidBrush(GetEffectiveColor(SelectionOverlayColor)))
                {
                    e.Graphics.FillRectangle(overlay, bounds);
                }
            }

            var text = e.ColumnIndex < e.Item.SubItems.Count ? e.Item.SubItems[e.ColumnIndex].Text : string.Empty;

            // With CheckBoxes, the first cell starts with the box the system keeps room for (and
            // toggles on a click there); drawn here because the owner-drawn rows would otherwise
            // leave that room empty.
            if (HasCheckBox(e.ColumnIndex))
            {
                Rectangle box = GetCheckBoxRectangle(bounds);
                bool isChecked = e.Item.Checked;
                Color fill = Enabled ? BorderColor : UIColors.DisabledGray;
                Color outline = Enabled ? UIColors.GetOutlineColor(baseBackColor) : UIColors.DisabledGray;

                CheckGlyph.Draw(
                    e.Graphics,
                    box,
                    isChecked ? CheckState.Checked : CheckState.Unchecked,
                    outline,
                    fill,
                    UIColors.GetBestTextColor(fill),
                    DeviceDpi);
            }

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.Item.Font ?? Font,
                GetCellTextRectangle(bounds, e.ColumnIndex),
                GetEffectiveColor(e.Item.ForeColor),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        // Only the first cell of a list with CheckBoxes has the box.
        private bool HasCheckBox(int columnIndex)
        {
            return CheckBoxes && columnIndex == 0;
        }

        private Rectangle GetCheckBoxRectangle(Rectangle cell)
        {
            int side = Px(CheckBoxSize96);

            return new Rectangle(cell.X + Px(CheckBoxLeft96), cell.Y + (cell.Height - side) / 2, side, side);
        }

        // Where the text of a cell goes: from a little behind its left edge (behind the box, in the first cell of a list with
        // CheckBoxes) to a little before its right edge. What is drawn there and what the tooltip measures are the same.
        private Rectangle GetCellTextRectangle(Rectangle cell, int columnIndex)
        {
            int left = HasCheckBox(columnIndex)
                ? GetCheckBoxRectangle(cell).Right + Px(CheckBoxGap96)
                : cell.X + Px(CellTextLeft96);

            return new Rectangle(left, cell.Y, Math.Max(0, cell.Right - Px(CellTextRight96) - left), cell.Height);
        }

        // The gray of a disabled control, worked out when painting, so that nothing needs to be given back.
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
    }
}
