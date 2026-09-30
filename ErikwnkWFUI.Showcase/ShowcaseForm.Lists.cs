using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using MessageBox = ErikwnkWFUI.Forms.MessageBox;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Showcase
{
    // List box, list view and data grid.
    public partial class ShowcaseForm
    {
        private void AddListBoxControlSection(PropertyTable table)
        {
            ListBoxControl listBox = UIStyles.ListBoxControls.CreateStandard(
                "Sample list",
                allowReorder: true,
                showEnumeration: true);
            listBox.Items.Add("First item");
            listBox.Items.Add("Second item");
            listBox.Items.Add("Third item (drag to reorder)");
            listBox.Items.Add("A much longer item whose text wraps onto a second line instead of getting cut off");
            listBox.Items.Add("An even longer item whose text keeps going well past what even two full lines could ever hold, so the second line itself ends up ellipsized instead of overflowing into a third");

            table.AddRow("CreateStandard", 260, listBox);

            ListBoxControl primaryListBox = UIStyles.ListBoxControls.CreatePrimary(
                "Sample list",
                allowReorder: true,
                showEnumeration: true);
            primaryListBox.Items.Add("First item");
            primaryListBox.Items.Add("Second item");
            primaryListBox.Items.Add("Third item (drag to reorder)");
            primaryListBox.Items.Add("A much longer item whose text wraps onto a second line instead of getting cut off");
            primaryListBox.Items.Add("An even longer item whose text keeps going well past what even two full lines could ever hold, so the second line itself ends up ellipsized instead of overflowing into a third");

            table.AddRow("CreatePrimary", 260, primaryListBox);
        }

        private void AddListViewSection(PropertyTable table)
        {
            const int demoWidth = 300;

            var standardListView = UIStyles.ListViews.CreateStandard();
            PopulateListViewDemo(standardListView);
            table.AddRow("CreateStandard", 260, UIColumn.Absolute(standardListView, demoWidth));
            PinToDemoWidth(standardListView, demoWidth);

            var primaryListView = UIStyles.ListViews.CreatePrimary();
            PopulateListViewDemo(primaryListView);
            table.AddRow("CreatePrimary", 260, UIColumn.Absolute(primaryListView, demoWidth));
            PinToDemoWidth(primaryListView, demoWidth);
        }

        // AddRow's UIColumn.Absolute only bounds the WRAPPER cell to
        // demoWidth - PropertyTable.ConfigureEditorControl (run as part of
        // AddRow, no special case for ListView) still sets Dock = Fill on
        // the control itself afterward, which stretches it right back out
        // to fill that cell/wrapper rather than actually sizing it to
        // demoWidth. Overriding Dock/Width here, after AddRow has already
        // run, is what actually keeps the control narrow.
        private static void PinToDemoWidth(System.Windows.Forms.Control control, int width)
        {
            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.Width = width;

            // Widening a column past this fixed width makes a horizontal
            // scrollbar appear, which shrinks ClientSize.Height - and
            // ListView's own whole-rows-only height snapping (SnapHeight-
            // ToWholeRows) then reacts to THAT by shrinking the control's
            // actual Height too, even though nothing about the demo's own
            // requested size changed. Locking both dimensions here, not
            // just Width, keeps the whole footprint constant regardless of
            // column widths.
            //
            // Deferred one more tick than the fix above needs strictly for
            // Width, so this runs AFTER ListView's own initial whole-rows
            // snap (itself deferred via BeginInvoke from OnHandleCreated)
            // has already settled - capturing Height any earlier would
            // lock in the raw, pre-snap value and immediately fight that
            // snap the moment it actually ran. BeginInvoke needs a real
            // handle to post to - this control may not have one yet at
            // this point in the Showcase's own construction, so that case
            // waits for HandleCreated first.
            void LockCurrentSize()
            {
                var lockedSize = control.Size;
                control.Resize += (sender, e) =>
                {
                    if (control.Size != lockedSize)
                    {
                        control.Size = lockedSize;
                    }
                };
            }

            if (control.IsHandleCreated)
            {
                control.BeginInvoke(new MethodInvoker(LockCurrentSize));
            }
            else
            {
                control.HandleCreated += (sender, e) => control.BeginInvoke(new MethodInvoker(LockCurrentSize));
            }
        }

        private static void PopulateListViewDemo(System.Windows.Forms.ListView listView)
        {
            listView.Columns.Add("Item", 180);
            listView.Columns.Add("Status", 100);

            // PropertyTable centers a row's editor control inside an
            // AutoSize middle row rather than stretching it (see
            // PropertyTable.AddControlToCell) - so the row height passed to
            // AddRow above only changes how much padding surrounds the
            // control, not the control's own size. The control's actual
            // rendered height comes from this Height instead.
            listView.Height = 220;

            // Enough rows to force a vertical scrollbar - only ~7 fit in
            // that height at once, so this doubles as a way to actually
            // exercise drag-select + scroll behavior here instead of only
            // in a consuming app.
            for (int i = 1; i <= 40; i++)
            {
                listView.Items.Add(new ListViewItem(new[] { "Row " + i, i % 2 == 0 ? "OK" : "Pending" }));
            }
        }

        private void AddDataGridSection(PropertyTable table)
        {
            // Demonstrates actual DataSource binding - the one thing a
            // plain ListView (and anything built on it, like
            // ListView) simply cannot do at all. A BindingList<T>, not a
            // DataTable - column-header sorting rewrites the bound list
            // itself (see ReadOnlyDataGridView.ReorderDataSource), which
            // needs an IList data source; a DataTable isn't one, so
            // sorting silently did nothing against it here.
            AddEditableDataGridRow(table, "CreateStandard", UIStyles.DataGridViews.CreateStandard);
            AddEditableDataGridRow(table, "CreatePrimary", UIStyles.DataGridViews.CreatePrimary);

            // The lighter of the two variants above - column-header sorting
            // still works, but no adding/deleting/cutting/pasting rows, so
            // neither the delete-row column nor the "type here to add a
            // row" placeholder the editable rows above demo has anything
            // to show here.
            var readOnlyGrid = UIStyles.DataGridViews.CreateReadOnlyStandard(CreateSampleTracks());
            readOnlyGrid.Dock = DockStyle.Fill;
            NarrowLengthColumn(readOnlyGrid);

            var disabledReadOnlyGrid = UIStyles.DataGridViews.CreateReadOnlyStandard(CreateSampleTracks());
            disabledReadOnlyGrid.Dock = DockStyle.Fill;
            disabledReadOnlyGrid.Enabled = false;
            NarrowLengthColumn(disabledReadOnlyGrid);

            table.AddRow(
                "CreateReadOnlyStandard",
                180,
                UIColumn.Percent(readOnlyGrid, 50),
                UIColumn.Percent(disabledReadOnlyGrid, 50));

            var readOnlyPrimaryGrid = UIStyles.DataGridViews.CreateReadOnlyPrimary(CreateSampleTracks());
            readOnlyPrimaryGrid.Dock = DockStyle.Fill;
            NarrowLengthColumn(readOnlyPrimaryGrid);

            var disabledReadOnlyPrimaryGrid = UIStyles.DataGridViews.CreateReadOnlyPrimary(CreateSampleTracks());
            disabledReadOnlyPrimaryGrid.Dock = DockStyle.Fill;
            disabledReadOnlyPrimaryGrid.Enabled = false;
            NarrowLengthColumn(disabledReadOnlyPrimaryGrid);

            table.AddRow(
                "CreateReadOnlyPrimary",
                180,
                UIColumn.Percent(readOnlyPrimaryGrid, 50),
                UIColumn.Percent(disabledReadOnlyPrimaryGrid, 50));
        }

        // "Length" only ever holds a short "m:ss" string - narrowed so the
        // three grids sharing one row here (each barely a third of the
        // row's own width, unlike the two-up ReadOnly rows below) don't
        // need a horizontal scrollbar just to fit a column whose default
        // auto-generated width is far wider than its content ever needs.
        private static void NarrowLengthColumn(System.Windows.Forms.DataGridView grid)
        {
            if (grid.Columns["Length"] != null)
            {
                grid.Columns["Length"].Width = 55;
            }
        }

        // Two states for both CreateStandard and CreatePrimary - plain
        // (without the "type here to add a row" placeholder) and with the
        // delete-row/enumeration columns (which keeps the placeholder,
        // being the one state actually meant to show off adding as well as
        // deleting rows). No disabled state here - the ReadOnlyStandard/
        // ReadOnlyPrimary rows below already demo one each; a third,
        // editable-but-disabled grid would just be the same thing shown
        // twice. showDeleteColumn/showEnumerationColumn let a caller demo
        // just one of the two pinned columns instead of always both
        // together - CreateStandard's own row only shows the enumeration
        // one, CreatePrimary's shows both coexisting.
        private void AddEditableDataGridRow(
            PropertyTable table,
            string labelText,
            Func<object, System.Windows.Forms.DataGridView> createGrid,
            bool showDeleteColumn = true,
            bool showEnumerationColumn = true)
        {
            var grid = createGrid(CreateSampleTracks());
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            NarrowLengthColumn(grid);

            var gridWithDeleteColumn = (ErikwnkWFUI.Controls.DataGridView)createGrid(CreateSampleTracks());
            gridWithDeleteColumn.Dock = DockStyle.Fill;
            gridWithDeleteColumn.ShowDeleteRowColumn = showDeleteColumn;
            gridWithDeleteColumn.ShowEnumeration = showEnumerationColumn;
            NarrowLengthColumn(gridWithDeleteColumn);

            table.AddRow(
                labelText,
                180,
                UIColumn.Percent(grid, 50),
                UIColumn.Percent(gridWithDeleteColumn, 50));
        }

        // A fresh list each call - CreateStandard/CreateReadOnly bind
        // their own independent DataSource, and a BindingList<T> (unlike
        // DataTable) has no built-in Copy() to hand out separate
        // instances backed by the same starting values instead.
        private static BindingList<SampleTrack> CreateSampleTracks()
        {
            return new BindingList<SampleTrack>
            {
                new SampleTrack { Track = "Sample Song", Artist = "Sample Artist", Length = "3:42" },
                new SampleTrack { Track = "Another Track", Artist = "Someone Else", Length = "4:15" },
                new SampleTrack { Track = "Third One", Artist = "Someone Else", Length = "2:58" },
            };
        }

        private sealed class SampleTrack
        {
            public string Track { get; set; }
            public string Artist { get; set; }
            public string Length { get; set; }
        }
    }
}
