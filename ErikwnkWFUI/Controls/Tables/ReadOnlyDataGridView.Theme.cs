using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Controls
{
    // Colors and the look: the theme and accent, the cell styles, the header and cell painting, the scroll bars.
    public partial class ReadOnlyDataGridView
    {

        // The theme or the accent changed while the grid is shown: what was not set by hand follows (the scroll bars
        // through NativeControlTheme.Track), and the grid is painted anew.
        private void OnThemeChanged()
        {
            if (BackgroundColor == _followedBackgroundColor)
                BackgroundColor = UIColors.BackgroundDark;

            _followedBackgroundColor = UIColors.BackgroundDark;

            if (GridColor == _followedGridColor)
                GridColor = UIColors.BorderMedium;

            _followedGridColor = UIColors.BorderMedium;

            ApplyStyles();

            foreach (DataGridViewColumn column in Columns)
                ThemeColumnType(column, onlyWhatFollowed: true);

            Invalidate(true);
        }


        // The look of a color set (what the factories make with colors and CreatePrimary): the set's hover color is the
        // color of the look. Asked for again whenever the theme or the accent changes.
        internal void FollowColors(Func<UIColorSet> colors)
        {
            _borderColor.SetDefault(() => colors().Hover);
            _selectionBackColor.SetDefault(() => colors().Hover);
            _columnReorderIndicatorColor.SetDefault(() => colors().Hover);
            _contextMenuSelectionColor.SetDefault(() => colors().Hover);

            ApplyStyles();
        }


        // The columns that are not text keep the system's look unless told otherwise: white
        // buttons and combo boxes and dark-blue links on the dark rows. Put in the grid's
        // colors here, when the column comes in; a consumer can still change them afterwards.
        // (Check box and image columns already follow the cell colors.)
        private void ThemeColumnType(DataGridViewColumn column, bool onlyWhatFollowed = false)
        {
            DataGridViewButtonColumn button = column as DataGridViewButtonColumn;
            DataGridViewComboBoxColumn combo = column as DataGridViewComboBoxColumn;
            DataGridViewLinkColumn link = column as DataGridViewLinkColumn;

            if (button != null)
            {
                button.FlatStyle = FlatStyle.Flat;

                // Changed again only where it still has the color the grid gave it.
                if (!onlyWhatFollowed || button.DefaultCellStyle.BackColor == _followedButtonBackColor)
                    button.DefaultCellStyle.BackColor = UIColors.BackgroundLighter;

                if (!onlyWhatFollowed || button.DefaultCellStyle.ForeColor == _followedButtonForeColor)
                    button.DefaultCellStyle.ForeColor = UIColors.TextPrimary;

                _followedButtonBackColor = UIColors.BackgroundLighter;
                _followedButtonForeColor = UIColors.TextPrimary;
            }
            else if (combo != null)
            {
                // The arrow button of a flat combo cell is still a white block in the system's
                // button color: it shows on the current cell only, where the choice is made.
                combo.FlatStyle = FlatStyle.Flat;
                combo.DisplayStyleForCurrentCellOnly = true;
            }
            else if (link != null)
            {
                // A link has to be seen against the row color in both themes.
                Color row = RowBackColor;
                Color[] followed = _followedLinkColors;
                Color[] wanted =
                {
                    UIColors.EnsureContrast(UIColors.Primary, row, 4.5),
                    UIColors.EnsureContrast(UIColors.ShiftLightness(UIColors.Primary, -0.15), row, 4.5),
                    UIColors.TextPrimary
                };

                if (!onlyWhatFollowed || link.LinkColor == followed[0])
                    link.LinkColor = wanted[0];

                if (!onlyWhatFollowed || link.VisitedLinkColor == followed[1])
                    link.VisitedLinkColor = wanted[1];

                if (!onlyWhatFollowed || link.ActiveLinkColor == followed[2])
                    link.ActiveLinkColor = wanted[2];

                _followedLinkColors = wanted;
            }
        }


        // Visually neutral, matching DataGridView's own delete-row column -
        // WinForms has no built-in "this column can't be selected" switch,
        // so a click still technically moves CurrentCell there, but giving
        // it the exact same SelectionBackColor/ForeColor as its own resting
        // colors means it never actually LOOKS selected, reading as a
        // passive label instead of a normal data cell.
        protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
        {
            base.OnCellFormatting(e);

            if (_enumerationColumn == null || e.ColumnIndex != _enumerationColumn.Index || e.RowIndex < 0)
            {
                return;
            }

            e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
            e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
        }


        // ColumnHeaderPainting.DrawSortGlyph below draws the triangle. The
        // native SortGlyphDirection glyph is never set on a column (see
        // CycleSort): it does paint, in a style of its own, and the two
        // overlap into a "bowtie" blob.
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);

            if (e.RowIndex != -1)
            {
                return;
            }

            // The default header painting (background/native border/text)
            // has to happen FIRST and be marked Handled here, same reason
            // as always with this trick - anything drawn before this
            // point in the method would otherwise just get painted over
            // once DataGridView's own default painting runs right after
            // this event handler returns (confirmed live: the insertion
            // line below drew, then immediately vanished, because it used
            // to run before this call).
            e.Paint(e.ClipBounds, e.PaintParts);
            e.Handled = true;

            // Every header cell's own border is redrawn here in
            // BorderColor - not the native ColumnHeadersBorderStyle one
            // (GridColor-driven, and GridColor is deliberately fixed - see
            // its own remarks on why), the same way ListView's own
            // OnDrawColumnHeader draws its header divider in BorderColor
            // regardless of what its (non-existent, for data rows) grid
            // lines look like. Together, every cell's own rectangle forms
            // one continuous frame around the whole header strip, matching
            // ListView's own look - CreatePrimary's header now reads as
            // accent-colored the same way ListView's does, while the
            // gridlines between actual rows underneath stay neutral.
            DrawHeaderCellBorder(e);

            DrawColumnDragInsertionLine(e);

            if (e.ColumnIndex != _sortedColumnIndex)
            {
                return;
            }

            ColumnHeaderPainting.DrawSortGlyph(e.Graphics, e.CellBounds, Font, HeaderForeColor, _sortOrder);
        }


        private void DrawHeaderCellBorder(DataGridViewCellPaintingEventArgs e)
        {
            using (Pen pen = new Pen(BorderColor))
            {
                e.Graphics.DrawRectangle(
                    pen,
                    e.CellBounds.Left,
                    e.CellBounds.Top,
                    e.CellBounds.Width - 1,
                    e.CellBounds.Height - 1);
            }
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
            Color rowForeColor = Enabled ? RowForeColor : UIColors.DisabledGray;
            Color headerForeColor = Enabled ? HeaderForeColor : UIColors.DisabledGray;
            Color selectionBackColor = Enabled ? SelectionBackColor : UIColors.DisabledGray;
            // The true best contrast, not a brightness rule of thumb: on the mid
            // tones a color set can bring (lime green, say) the rule picks the
            // wrong text color.
            Color selectionForeColor = UIColors.GetBestTextColor(selectionBackColor);

            ColumnHeadersDefaultCellStyle.BackColor = HeaderBackColor;
            ColumnHeadersDefaultCellStyle.ForeColor = headerForeColor;
            ApplyHeaderFont();
            ColumnHeadersDefaultCellStyle.SelectionBackColor = HeaderBackColor;
            ColumnHeadersDefaultCellStyle.SelectionForeColor = headerForeColor;

            DefaultCellStyle.BackColor = RowBackColor;
            DefaultCellStyle.ForeColor = rowForeColor;
            DefaultCellStyle.SelectionBackColor = selectionBackColor;
            DefaultCellStyle.SelectionForeColor = selectionForeColor;

            AlternatingRowsDefaultCellStyle.BackColor = UIColors.Darken(RowBackColor, 5);
            AlternatingRowsDefaultCellStyle.ForeColor = rowForeColor;
            AlternatingRowsDefaultCellStyle.SelectionBackColor = selectionBackColor;
            AlternatingRowsDefaultCellStyle.SelectionForeColor = selectionForeColor;

            Invalidate();
        }


        private void ApplyHeaderFont()
        {
            if (_headerFont != null && ReferenceEquals(_headerFontSource, Font))
                return;

            Font previous = _headerFont;
            _headerFont = new Font(Font, FontStyle.Bold);
            _headerFontSource = Font;
            ColumnHeadersDefaultCellStyle.Font = _headerFont;

            // After the style has the new one.
            previous?.Dispose();
        }


        private void ThemeScrollBars()
        {
            List<Control> found = new List<Control>();

            foreach (string field in new[] { "horizScrollBar", "_horizScrollBar", "vertScrollBar", "_vertScrollBar" })
            {
                Control bar = typeof(System.Windows.Forms.DataGridView)
                    .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(this) as Control;

                if (bar == null)
                    continue;

                found.Add(bar);
                NativeControlTheme.Track(bar);
            }

            _scrollBars = found.ToArray();
        }


        // Where the two scroll bars meet is a square the standard grid fills with the system's
        // button color - a white block on the dark grid. Painted over in the grid's own color.
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Control horizontal = null;
            Control vertical = null;

            foreach (Control bar in _scrollBars)
            {
                if (!bar.Visible)
                    continue;

                if (bar is HScrollBar)
                    horizontal = bar;
                else if (bar is VScrollBar)
                    vertical = bar;
            }

            if (horizontal != null && vertical != null)
            {
                using (SolidBrush brush = new SolidBrush(BackgroundColor))
                    e.Graphics.FillRectangle(brush, new Rectangle(vertical.Left, horizontal.Top, vertical.Width, horizontal.Height));
            }
        }


        private void EnableDoubleBuffering()
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(this, true, null);
        }
    }
}
