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

            MultiPanel counted = CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardStandard());
            counted.ShowPageCount = true;
            table.AddRow("ShowPageCount", MultiPanelRowHeight, counted);

            MultiPanel centered = CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardStandard());
            centered.TitleAlignment = HorizontalAlignment.Center;
            table.AddRow("TitleAlignment (Center)", MultiPanelRowHeight, centered);

            MultiPanel right = CreateMultiPanelDemo(UIStyles.MultiPanels.CreateCardStandard());
            right.ShowPageCount = true;
            right.TitleAlignment = HorizontalAlignment.Right;
            table.AddRow("TitleAlignment (Right), ShowPageCount", MultiPanelRowHeight, right);
        }

        private const int MultiPanelRowHeight = 150;

        // Three pages, each with a line of text and a button: the controls of a page are put on the page AddPage gives back.
        private static MultiPanel CreateMultiPanelDemo(MultiPanel panel)
        {
            panel.Dock = DockStyle.None;
            panel.Size = new Size(380, 130);
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
