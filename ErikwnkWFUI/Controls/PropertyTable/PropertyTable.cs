using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using static ErikwnkWFUI.UIStyles;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// A two-column "label: editor" table, grouped into optional labeled
    /// sections - typical use is a settings/options panel. Build it up with
    /// <see cref="AddSection"/> and <see cref="AddRow(string,Control[])"/>;
    /// a row can hold more than one editor control side by side via the
    /// <see cref="PropertyColumn"/>-based overloads. Both return a
    /// <see cref="PropertyRow"/> to hide or disable the row later. The width of the
    /// label column is <see cref="LabelWidth"/>, the line at the section headers
    /// follows <see cref="Colors"/> (none by default; see <c>UIStyles.PropertyTables</c>).
    /// </summary>
    public partial class PropertyTable : UserControl
    {
        private const int DefaultLabelWidth96 = 130;
        private const int DefaultRowHeight = 42;
        private const int DefaultSectionHeight = 32;
        private const int LabelLeftPadding = 10;

        private readonly System.Windows.Forms.TableLayoutPanel _layout;
        private readonly ColorSetHolder _colorSet = new ColorSetHolder();

        // The editors the caller gave (they are detached, not disposed, by ClearRows) and the section headers (they paint the line).
        private readonly List<Control> _editors = new List<Control>();
        private readonly List<Label> _sectionLabels = new List<Label>();
        private readonly List<PropertyRow> _rows = new List<PropertyRow>();
        private int _labelWidth = DefaultLabelWidth96;
        private int _dpiOverride;

        public PropertyTable()
        {
            Dock = DockStyle.Top;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ThemeWatcher.FollowBackColor(this, () => UIStyles.Colors.BackgroundMedium);
            Margin = new Padding(0);
            Padding = new Padding(0);

            _layout = CreateMainLayout();

            Controls.Add(_layout);
            ApplyLabelWidth();
        }

        /// <summary>
        /// The width of the label column in pixels at 96 dpi (130 by default; scaled to the screen). A label that is longer than this
        /// wraps onto more lines; what does not fit the row then is cut, and the whole text is its tooltip. Takes effect at once for the rows that are there.
        /// </summary>
        [DefaultValue(DefaultLabelWidth96)]
        public int LabelWidth
        {
            get { return _labelWidth; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), "The label column needs a width.");

                _labelWidth = value;
                ApplyLabelWidth();
            }
        }

        private void ApplyLabelWidth()
        {
            _layout.ColumnStyles[0].Width = Scale(_labelWidth);
        }

        // The sizes the table lays out (label column, rows, the room around the editors) are given for 96 dpi and scaled to the screen; the
        // test seam pretends another screen.
        internal int DpiOverride
        {
            get { return _dpiOverride; }
            set
            {
                _dpiOverride = value;
                ApplyScale();
            }
        }

        private int Dpi96
        {
            get { return _dpiOverride > 0 ? _dpiOverride : DeviceDpi; }
        }

        private int Scale(int value96)
        {
            return Dpi.Scale(this, value96, _dpiOverride);
        }

        // The label column and the heights of the rows that are there follow a change of the screen (the room around the editors is set when a row is made).
        private void ApplyScale()
        {
            ApplyLabelWidth();

            foreach (PropertyRow row in _rows)
                row.Rescale(Scale);
        }

        protected override void OnDpiChangedAfterParent(EventArgs e)
        {
            base.OnDpiChangedAfterParent(e);
            ApplyScale();
        }

        /// <summary>
        /// Removes every row and section, leaving an empty table. The editor
        /// controls added via <see cref="AddRow(string,Control[])"/> are only
        /// detached - <see cref="Control.Dispose"/> is NOT called on them, so they
        /// keep whatever state they had (text, selection, checked state,
        /// ...) and can be re-added afterward via a fresh AddRow. Handy for
        /// rebuilding a table live (e.g. on a language switch, re-adding the
        /// same controls with new labels) without losing in-progress editor
        /// state; if a caller genuinely wants the old controls gone for
        /// good, dispose them explicitly first. Everything the table built
        /// itself (labels, cells) is disposed, and the <see cref="PropertyRow"/>
        /// handles of the old rows do nothing any more.
        /// </summary>
        public void ClearRows()
        {
            _sectionLabels.Clear();
            _rows.Clear();

            // The editors belong to the caller and stay alive (they can be added again); everything the table built around them
            // is disposed, so that it does not stay registered at the theme.
            foreach (Control editor in _editors)
                editor.Parent?.Controls.Remove(editor);

            _editors.Clear();

            foreach (Control built in _layout.Controls.Cast<Control>().ToList())
                built.Dispose();

            _layout.Controls.Clear();
            _layout.RowStyles.Clear();
            _layout.RowCount = 0;
        }

        /// <summary>Adds a full-width section header row; the handle can hide or disable it.</summary>
        public PropertyRow AddSection(string title)
        {
            int row = AddRowStyle(DefaultSectionHeight);

            Label label = CreateSectionLabel(title);
            label.Paint += (sender, e) => PaintSectionLine(label, e.Graphics);
            _sectionLabels.Add(label);

            _layout.Controls.Add(label, 0, row);
            _layout.SetColumnSpan(label, 2);

            return AddHandle(new PropertyRow(_layout.RowStyles[row], DefaultSectionHeight, label));
        }

        /// <summary>
        /// Adds a row: a label on the left, one or more editor controls
        /// sharing the remaining width equally on the right (each gets
        /// <see cref="PropertyColumn.Auto"/> sizing). Use the
        /// <see cref="AddRow(string,PropertyColumn[])"/> overload instead if the
        /// controls need different widths. The row is 42 pixels high (the
        /// overloads with a row height: that high at least), or as high as its highest editor needs; a longer label wraps onto a second line. Returns the handle of the row.
        /// </summary>
        public PropertyRow AddRow(
            string labelText,
            params Control[] controls)
        {
            PropertyColumn[] columns = CreateAutoColumns(controls);

            return AddRow(
                labelText,
                DefaultRowHeight,
                columns);
        }

        /// <inheritdoc cref="AddRow(string,Control[])"/>
        public PropertyRow AddRow(
            string labelText,
            int rowHeight,
            params Control[] controls)
        {
            PropertyColumn[] columns = CreateAutoColumns(controls);

            return AddRow(
                labelText,
                rowHeight,
                columns);
        }

        /// <summary>Adds a row with explicit per-column sizing (a column may hold no control) - see <see cref="PropertyColumn.Auto"/>/<see cref="PropertyColumn.Absolute"/>/<see cref="PropertyColumn.Percent"/>.</summary>
        public PropertyRow AddRow(
            string labelText,
            params PropertyColumn[] columns)
        {
            return AddRow(
                labelText,
                DefaultRowHeight,
                columns);
        }

        /// <inheritdoc cref="AddRow(string,PropertyColumn[])"/>
        public PropertyRow AddRow(
            string labelText,
            int rowHeight,
            params PropertyColumn[] columns)
        {
            int height = Math.Max(rowHeight, GetRequiredRowHeight(columns));
            int row = AddRowStyle(height);

            Label label = CreateRowLabel(labelText);
            System.Windows.Forms.Panel editorArea = CreateEditorAreaPanel();
            System.Windows.Forms.TableLayoutPanel editorLayout = CreateEditorLayout(columns);

            editorArea.Controls.Add(editorLayout);

            _layout.Controls.Add(label, 0, row);
            _layout.Controls.Add(editorArea, 1, row);

            return AddHandle(new PropertyRow(_layout.RowStyles[row], height, label, editorArea));
        }

        private PropertyRow AddHandle(PropertyRow row)
        {
            _rows.Add(row);

            return row;
        }

        // The room a cell takes around its editor: the margins of the cell panel (2 above and below) and its padding (1 on each side, the frame).
        private int CellChromeHeight
        {
            get { return 2 * Scale(2) + 2 * Scale(1); }
        }

        // The row (in 96 dpi) is as high as the highest editor needs (the height it has when it is added, in the pixels of the screen) - an
        // editor is never cut off by the row.
        private int GetRequiredRowHeight(PropertyColumn[] columns)
        {
            int required = 0;

            if (columns != null)
            {
                foreach (PropertyColumn column in columns)
                {
                    if (column != null && column.Control != null)
                        required = Math.Max(required, column.Control.Height + CellChromeHeight);
                }
            }

            return (int)Math.Ceiling(required * 96.0 / Dpi96);
        }

        private PropertyColumn[] CreateAutoColumns(Control[] controls)
        {
            if (controls == null || controls.Length == 0)
                return new[] { PropertyColumn.Auto(null) };

            PropertyColumn[] columns = new PropertyColumn[controls.Length];

            for (int i = 0; i < controls.Length; i++)
                columns[i] = PropertyColumn.Auto(controls[i]);

            return columns;
        }

        // Every panel/TableLayoutPanel in this file used to have
        // BackColor=Transparent, relying on WinForms' built-in "ask my
        // parent to paint what's behind me" fake-transparency support to
        // make them blend into their surroundings. With this many nested
        // transparent layers (main layout > editor area > editor layout >
        // wrapper, four levels deep for a single control), that mechanism
        // showed real, reproducible ghosting on live screen captures
        // (CopyFromScreen and PrintWindow both, so not a capture-tooling
        // artifact): a control's neighbor - another row's label, or another
        // control entirely - leaking into its painted area. Every layer now
        // gets the same opaque color it would have resolved to anyway, so
        // there is no "ask parent" indirection left for anything to get
        // wrong.
        private System.Windows.Forms.TableLayoutPanel CreateMainLayout()
        {
            System.Windows.Forms.TableLayoutPanel layout = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 0,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(layout, () => UIStyles.Colors.BackgroundMedium);

            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Absolute,
                    DefaultLabelWidth96));

            layout.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    100));

            return layout;
        }

        private System.Windows.Forms.TableLayoutPanel CreateEditorLayout(PropertyColumn[] columns)
        {
            int columnCount = columns == null || columns.Length == 0
                ? 1
                : columns.Length;

            System.Windows.Forms.TableLayoutPanel layout = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = columnCount,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(layout, () => UIStyles.Colors.BackgroundMedium);

            layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    100));

            for (int i = 0; i < columnCount; i++)
            {
                PropertyColumn column = columns != null && i < columns.Length
                    ? columns[i]
                    : PropertyColumn.Auto(null);

                layout.ColumnStyles.Add(
                    column.Style);

                System.Windows.Forms.Panel cellPanel = CreateEditorCellPanel();

                if (column.Control != null)
                {
                    ConfigureEditorControl(column.Control);
                    AddControlToCell(cellPanel, column.Control);
                }

                layout.Controls.Add(cellPanel, i, 0);
            }

            return layout;
        }

        private void AddControlToCell(
            System.Windows.Forms.Panel cellPanel,
            Control control)
        {
            if (cellPanel == null || control == null)
                return;

            System.Windows.Forms.TableLayoutPanel wrapper = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(Scale(6), 0, Scale(8), 0),
                // (A Dock=Fill child in a Panel ignores its Margin: the room for the
                // border of the cell panel is the Padding of that panel, see
                // CreateEditorCellPanel.)
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(wrapper, () => UIStyles.Colors.BackgroundLight);

            wrapper.ColumnStyles.Add(
                new ColumnStyle(SizeType.Percent, 100));

            wrapper.RowStyles.Add(
                new RowStyle(SizeType.Percent, 50));

            wrapper.RowStyles.Add(
                new RowStyle(SizeType.AutoSize));

            wrapper.RowStyles.Add(
                new RowStyle(SizeType.Percent, 50));

            wrapper.Controls.Add(control, 0, 1);
            _editors.Add(control);

            cellPanel.Controls.Add(wrapper);
        }

        private int AddRowStyle(int height)
        {
            int row = _layout.RowCount;

            _layout.RowCount++;

            _layout.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    Scale(height)));

            return row;
        }

        /// <summary>
        /// The color of the line at the left edge of each section header: the <c>Hover</c> color of the set is the main color (brought to where it
        /// is seen against the header). Null (the default) is no line; <c>UIStyles.PropertyTables.CreatePrimary</c> makes one that follows the
        /// accent; <c>CreateStandard(colors: ...)</c> takes a color set or simply a color. The labels, the cells and the controls in them
        /// stay as they are (the controls have colors of their own). Not written by the designer (a set is code).
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UIColorSet Colors
        {
            get { return _colorSet.Value; }
            set
            {
                _colorSet.Value = value;
                InvalidateSections();
            }
        }

        // A set that is asked for again each time it is needed (the primary set, which follows the accent); a later Colors replaces it.
        internal void FollowColors(Func<UIColorSet> source)
        {
            _colorSet.Follow(source);
            InvalidateSections();
        }

        private void InvalidateSections()
        {
            foreach (Label label in _sectionLabels)
                label.Invalidate();
        }

        // The line at the left edge of a section header, in the main color of the set (3 px at 96 dpi), nothing without a set.
        private void PaintSectionLine(Label header, Graphics graphics)
        {
            UIColorSet set = Colors;

            if (set == null)
                return;

            Color color = UIColors.EnsureContrast(set.Hover, header.BackColor, SectionLineContrast);

            using (SolidBrush brush = new SolidBrush(color))
                graphics.FillRectangle(brush, 0, 0, Dpi.Scale(header, SectionLineWidth96), header.Height);
        }

        private const int SectionLineWidth96 = 3;
        private const double SectionLineContrast = 3.0;

        private Label CreateRowLabel(string text)
        {
            ScrollingLabel label = Labels.CreateStandard(text ?? "");

            // A long text wraps onto more lines (the row has room for two or three); only what does not fit then is cut, with the tooltip below.
            label.AutoEllipsis = false;

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Padding = new Padding(Scale(LabelLeftPadding), 0, 0, 0);
            label.Margin = new Padding(0);
            ThemeWatcher.FollowBackColor(label, () => UIStyles.Colors.BackgroundMedium);

            // A label whose text does not fit its row even wrapped is cut: the whole text is its tooltip (asked when the pointer comes, so a change of the
            // width, the text or the font is followed).
            FullTextToolTip toolTip = new FullTextToolTip(label);
            label.MouseEnter += (sender, e) => toolTip.Update(label.Text, IsCut(label));
            label.Disposed += (sender, e) => toolTip.Dispose();

            return label;
        }

        internal static bool IsCut(ScrollingLabel label)
        {
            Size room = new Size(label.ClientSize.Width - label.Padding.Horizontal, label.ClientSize.Height - label.Padding.Vertical);

            if (room.Width <= 0 || label.Text.Length == 0)
                return false;

            // The label cuts its single line with an ellipsis (the text is measured without a limit) or wraps it (cut when it needs more lines than fit).
            TextFormatFlags flags = label.GetTextFormatFlags();

            if ((flags & TextFormatFlags.WordBreak) == 0)
                return TextRenderer.MeasureText(label.Text, label.Font, new Size(int.MaxValue, int.MaxValue), flags).Width > room.Width;

            return TextRenderer.MeasureText(label.Text, label.Font, new Size(room.Width, int.MaxValue), flags).Height > room.Height;
        }

        private Label CreateSectionLabel(string title)
        {
            Label label = Labels.CreateTitle(title ?? "");

            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Padding = new Padding(Scale(LabelLeftPadding), 0, 0, 0);
            label.Margin = new Padding(0);
            ThemeWatcher.FollowBackColor(label, () => UIStyles.Colors.BackgroundDark);

            return label;
        }

        private System.Windows.Forms.Panel CreateEditorAreaPanel()
        {
            System.Windows.Forms.Panel panel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            ThemeWatcher.FollowBackColor(panel, () => UIStyles.Colors.BackgroundMedium);

            return panel;
        }

        private System.Windows.Forms.Panel CreateEditorCellPanel()
        {
            System.Windows.Forms.Panel panel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                // 1px all around: the room for the border painted below. Without it the wrapper (Dock=Fill) covers the
                // whole cell, and the border was never seen.
                Padding = new Padding(Scale(1)),
                // Right margin matches the existing top/bottom margin, so
                // the table's own background shows as a border on all three
                // sides - previously only 0 on the right, so each editor
                // cell ran flush to the table's right edge with no border
                // there.
                Margin = new Padding(0, Scale(2), Scale(2), Scale(2))
            };

            ThemeWatcher.FollowBackColor(panel, () => UIStyles.Colors.BackgroundLight);

            // A themed border around each editor cell, independent of how
            // close BackgroundLight happens to sit next to the surrounding
            // BackgroundMedium in the active theme - Light theme's own
            // near-white palette leaves very little room between adjacent
            // "surface" shades (confirmed live: editor cells read as
            // indistinguishable from their surroundings there), where Dark
            // theme's much wider spread never had this problem. A border
            // reads clearly regardless of how far apart the two fill colors
            // are. The padding of this panel leaves the
            // 1px the border needs.
            panel.Paint += (sender, e) =>
            {
                using (Pen pen = new Pen(UIColors.GetLineColor(UIStyles.Colors.BackgroundMedium)))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.ClientSize.Width - 1, panel.ClientSize.Height - 1);
                }
            };

            return panel;
        }

        private void ConfigureEditorControl(Control control)
        {
            if (control == null)
                return;

            if (control is ToggleSwitch)
            {
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left;
                control.Margin = new Padding(0);
                return;
            }

            // A checkbox's box+label has a natural, fixed preferred size -
            // stretching it to Fill this row's whole (often much wider)
            // cell doesn't gain anything, so it gets the same left-anchored
            // treatment as ToggleSwitch/Button instead of the Fill every
            // other control type gets by default below.
            if (control is System.Windows.Forms.CheckBox)
            {
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left;
                control.Margin = new Padding(0);
                return;
            }

            if (control is TextBox)
            {
                TextBox textBox = (TextBox)control;

                textBox.Multiline = false;
                textBox.BorderStyle = BorderStyle.None;
                textBox.Dock = DockStyle.Fill;
                textBox.Margin = new Padding(0);

                return;
            }

            if (control is System.Windows.Forms.ComboBox)
            {
                control.Dock = DockStyle.Fill;
                control.Margin = new Padding(0);
                return;
            }

            if (control is Button)
            {
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left;
                control.Margin = new Padding(0);
                return;
            }

            // A Spinner is as big as it was made (a square, or as wide as ring and text need) - Fill would stretch it wide and its
            // circle would just sit centred in the extra width; a volume control is as wide as it was made, too. Left-anchor it at its natural
            // size like the Button/CheckBox cases above.
            if (control is Spinner || control is VolumeControl)
            {
                control.Dock = DockStyle.None;
                control.Anchor = AnchorStyles.Left;
                control.Margin = new Padding(0);
                return;
            }
            if (control is System.Windows.Forms.NumericUpDown)
            {
                // The cell has its own frame.
                ((System.Windows.Forms.NumericUpDown)control).BorderStyle = BorderStyle.None;
                control.Dock = DockStyle.Left;
                control.Margin = new Padding(0);
                return;
            }

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0);
        }
    }
}
