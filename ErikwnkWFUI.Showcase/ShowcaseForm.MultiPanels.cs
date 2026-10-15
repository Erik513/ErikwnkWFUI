using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Showcase
{
    public partial class ShowcaseForm
    {
        private void AddMultiPanelsSection(PropertyTable table)
        {
            UIColorSet green = UIStyles.Colors.Sets.Green;

            table.AddRow("CreateStandard", MultiPanelRowHeight, CreateMultiPanelDemo(UIStyles.MultiPanels.CreateStandard()));
            table.AddRow("CreatePrimary", MultiPanelRowHeight, CreateMultiPanelDemo(UIStyles.MultiPanels.CreatePrimary()));
            table.AddRow("CreateStandard(colors: Green)", MultiPanelRowHeight, CreateMultiPanelDemo(UIStyles.MultiPanels.CreateStandard(green)));
            table.AddRow("CreateCardStandard", MultiPanelRowHeight, CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardStandard()));
            table.AddRow("CreateCardPrimary", MultiPanelRowHeight, CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardPrimary()));
            table.AddRow("CreateCardStandard(colors: Green)", MultiPanelRowHeight, CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardStandard(green)));

            // The options in one row: the page count and the line under the header, and the title in the center and on the right.
            MultiPanel centered = CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardStandard(), PairedWidth);
            centered.ShowPageCount = true;
            centered.ShowHeaderLine = true;
            centered.TitleAlignment = HorizontalAlignment.Center;

            MultiPanel right = CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardStandard(), PairedWidth);
            right.TitleAlignment = HorizontalAlignment.Right;

            table.AddRow(
                "CreateCardStandard (ShowPageCount, ShowHeaderLine, TitleAlignment)",
                MultiPanelRowHeight,
                UIColumn.Percent(centered, TwoColumnPercent),
                UIColumn.Percent(right, TwoColumnPercent));
        }

        private const int PairedWidth = 250;

        private const int MultiPanelRowHeight = 150;

        // Three pages, each with a line of text and a button: the controls of a page are put on the page AddPage gives back.
        private static MultiPanel CreateMultiPanelDemo(MultiPanel panel, int width = 380)
        {
            panel.Dock = DockStyle.None;
            panel.Size = new Size(width, 130);
            panel.MinimumSize = panel.Size;
            panel.MaximumSize = panel.Size;

            string[] titles = { "First page", "Second page", "Third page" };

            for (int i = 0; i < titles.Length; i++)
            {
                MultiPanelPage page = panel.AddPage(titles[i]);

                Label text = UIStyles.Labels.CreateStandard("This is page " + (i + 1) + " of " + titles.Length + ".");
                text.AutoSize = true;
                text.Location = new Point(0, 4);
                text.BackColor = Color.Transparent;
                FollowSurface(text, panel);
                page.Controls.Add(text);

                Button button = UIStyles.Buttons.CreateStandard("Button " + (i + 1), size: new Size(100, 30));
                button.Location = new Point(0, 34);
                page.Controls.Add(button);
            }

            return panel;
        }
    }
}
