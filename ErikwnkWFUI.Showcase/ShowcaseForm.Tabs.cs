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
    // The tab controls, including the "all variants" overview.
    public partial class ShowcaseForm
    {
        private void AddTabControlSection(PropertyTable table)
        {
            // The editable variants: right-click a tab to add a new one
            // behind it or close it (the last tab always stays).
            table.AddRow(
                "CreateStandard",
                140,
                CreateTabControlDemo(UIStyles.TabControls.CreateStandard(), 3, "Right-click a tab to add, rename or close tabs (double-click renames)"));

            // The first tab is locked: it can be neither renamed nor closed.
            Controls.TabControl lockedFirst = UIStyles.TabControls.CreatePrimary();
            CreateTabControlDemo(lockedFirst, 3, "First tab is locked: no rename, no close");
            lockedFirst.SetTabRenamable(lockedFirst.TabPages[0], false);
            lockedFirst.SetTabClosable(lockedFirst.TabPages[0], false);
            table.AddRow("CreatePrimary", 140, lockedFirst);

            // CreateStandard with a color set.
            table.AddRow(
                "CreateStandard(colors: Green)",
                140,
                CreateTabControlDemo(UIStyles.TabControls.CreateStandard(UIStyles.Colors.Sets.Green), 3, "Green set"));

            // The display-only variants: tabs can be switched, not edited.
            table.AddRow(
                "CreateReadOnlyStandard",
                140,
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyStandard(), 3));

            // Two ways to handle more tabs than fit in a row: scroll arrows
            // at the right end of the strip (left), or wrapping onto
            // several rows (right).
            System.Windows.Forms.TabControl multiline = UIStyles.TabControls.CreateReadOnlyPrimary();
            multiline.Multiline = true;
            table.AddRow(
                "CreateReadOnlyPrimary",
                160,
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyPrimary(), 7, "TabControl with overflow arrows"),
                CreateTabControlDemo(multiline, 7, "TabControl with Multiline"));

            table.AddRow(
                "CreateReadOnlyStandard(colors: Green)",
                140,
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyStandard(UIStyles.Colors.Sets.Green), 3));
        }

        private static System.Windows.Forms.Label CreateTabPageLabel(string text)
        {
            System.Windows.Forms.Label label = UIStyles.Labels.CreateStandard(text);
            label.AutoSize = true;
            label.Location = new System.Drawing.Point(10, 10);
            return label;
        }

        private static System.Windows.Forms.Control CreateTabControlDemo(System.Windows.Forms.TabControl tabs, int pageCount, string description = null)
        {
            string[] titles = { "Overview", "Details", "Disabled" };

            for (int i = 0; i < pageCount; i++)
            {
                string title = i < titles.Length ? titles[i] : "Tab number " + (i + 1);
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
                page.Controls.Add(CreateTabPageLabel(
                    description == null
                        ? "Content of the " + title + " tab"
                        : title + ": " + description));
                tabs.TabPages.Add(page);
            }

            if (pageCount == 3)
            {
                tabs.TabPages[2].Enabled = false;
            }

            return tabs;
        }
    }
}
