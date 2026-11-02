using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;

namespace ErikwnkWFUI.Showcase
{
    // The property table. Every page of this window is one, so here the tables to look at are small ones inside the rows of the page's own.
    public partial class ShowcaseForm
    {
        // The height of a row with a sample table in it: what the sample needs and the frame of its cell.
        private const int SampleFrameHeight = 8;

        private void AddPropertyTablesSection(PropertyTable table)
        {
            table.AddSection("PropertyTable (the line at the left edge of the section headers: none | the accent | the color of a set)");

            AddSampleRow(table, "CreateStandard", CreateSample(UIStyles.PropertyTables.CreateStandard()));
            AddSampleRow(table, "CreatePrimary", CreateSample(UIStyles.PropertyTables.CreatePrimary()));
            AddSampleRow(table, "CreateStandard(colors: Yellow)", CreateSample(UIStyles.PropertyTables.CreateStandard(UIStyles.Colors.Sets.Yellow)));

            table.AddSection("Labels and rows: LabelWidth | long label | rowHeight | columns");

            AddSampleRow(table, "LabelWidth = 220 (130 by default)", CreateWideLabelSample());
            AddSampleRow(table, "A long label wraps; what does not fit has a tooltip", CreateLongLabelSample());
            AddSampleRow(table, "AddRow(label, rowHeight, ...) | PropertyColumn.Absolute | Percent | Auto", CreateColumnsSample());

            AddSampleRow(table, "A tall editor makes its row higher (here a list of 120 px)", CreateTallEditorSample());

            table.AddSection("The handle of a row (PropertyRow) and ClearRows");

            AddSampleRow(table, "Visible | Enabled | ClearRows", CreateHandleSample());
        }

        private static void AddSampleRow(PropertyTable table, string label, PropertyTable sample)
        {
            table.AddRow(label, sample.GetPreferredSize(Size.Empty).Height + SampleFrameHeight, sample);
        }

        private static PropertyTable CreateSample(PropertyTable sample)
        {
            sample.AddSection("General");
            sample.AddRow("Name", UIStyles.TextBoxes.CreateStandard("Erik"));
            sample.AddRow("Notifications", UIStyles.ToggleSwitches.CreateStandard());
            sample.AddSection("Advanced");
            sample.AddRow("Mode", UIStyles.ToggleSwitches.CreatePrimary(false));

            return sample;
        }

        private static PropertyTable CreateWideLabelSample()
        {
            PropertyTable sample = UIStyles.PropertyTables.CreateStandard();
            sample.LabelWidth = 220;
            sample.AddRow("A label that needs more room", UIStyles.TextBoxes.CreateStandard("Fits on one line"));

            return sample;
        }

        private static PropertyTable CreateLongLabelSample()
        {
            PropertyTable sample = UIStyles.PropertyTables.CreateStandard();
            sample.AddRow("A long label of two lines", UIStyles.TextBoxes.CreateStandard("Wrapped"));
            sample.AddRow("A label so long that even two lines do not hold all of it - hover it", UIStyles.TextBoxes.CreateStandard("Cut, with a tooltip"));

            return sample;
        }

        private static PropertyTable CreateTallEditorSample()
        {
            PropertyTable sample = UIStyles.PropertyTables.CreateStandard();
            ErikwnkWFUI.Controls.ListBox list = UIStyles.ListBoxes.CreateStandard();
            list.Height = 120;
            sample.AddRow("Name", UIStyles.TextBoxes.CreateStandard("A normal row"));
            sample.AddRow("List", list);

            return sample;
        }

        private static PropertyTable CreateColumnsSample()
        {
            PropertyTable sample = UIStyles.PropertyTables.CreateStandard();

            sample.AddRow("Two equal controls", UIStyles.Buttons.CreateStandard("One"), UIStyles.Buttons.CreatePrimary("Two"));
            sample.AddRow("Absolute 100 | Percent 30 | Auto",
                PropertyColumn.Absolute(UIStyles.Buttons.CreateStandard("100 px"), 100),
                PropertyColumn.Percent(UIStyles.Buttons.CreateStandard("30 %"), 30f),
                PropertyColumn.Auto(UIStyles.Buttons.CreateStandard("The rest")));
            sample.AddRow("rowHeight: 70", 70, UIStyles.Buttons.CreateStandard("A higher row"));

            return sample;
        }

        // The toggle switches drive the row below them; the button builds the table anew with the same controls (ClearRows keeps them).
        private static PropertyTable CreateHandleSample()
        {
            PropertyTable sample = UIStyles.PropertyTables.CreateStandard();
            ToggleSwitch show = UIStyles.ToggleSwitches.CreateStandard();
            ToggleSwitch enable = UIStyles.ToggleSwitches.CreateStandard();
            System.Windows.Forms.TextBox details = UIStyles.TextBoxes.CreateStandard("Details");
            Button rebuild = UIStyles.Buttons.CreateStandard("ClearRows and add the rows again");
            PropertyRow detailsRow = null;

            void Build()
            {
                sample.ClearRows();
                sample.AddRow("Row.Visible", show);
                sample.AddRow("Row.Enabled", enable);
                detailsRow = sample.AddRow("The row they drive", details);
                detailsRow.Visible = show.Checked;
                detailsRow.Enabled = enable.Checked;
                sample.AddRow("ClearRows", rebuild);
            }

            show.CheckedChanged += delegate { detailsRow.Visible = show.Checked; };
            enable.CheckedChanged += delegate { detailsRow.Enabled = enable.Checked; };
            rebuild.Click += delegate { Build(); };
            Build();

            return sample;
        }
    }
}
