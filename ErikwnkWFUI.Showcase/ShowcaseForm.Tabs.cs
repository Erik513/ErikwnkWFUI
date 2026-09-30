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

            // Everything the tab controls can do, in one tab control: each
            // page of this read-only one shows a group of variants.
            table.AddRow("All variants", OverviewHeight + 20, CreateTabOverviewDemo());
        }

        private static System.Windows.Forms.Control CreateTabOverviewDemo()
        {
            System.Windows.Forms.TabControl overview = UIStyles.TabControls.CreateReadOnlyPrimary();

            AddOverviewPage(
                overview,
                "Read-only",
                "ReadOnlyTabControl: the user can switch tabs, not add, rename or close them. Standard (neutral bar) and Primary (accent bar).",
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyStandard(), 3),
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyPrimary(), 3));

            // Locked first tab, and every new tab starts out in rename mode.
            Controls.TabControl lockedFirst = UIStyles.TabControls.CreatePrimary();
            CreateTabControlDemo(lockedFirst, 3, "First tab is locked, new tabs start in rename mode");
            lockedFirst.SetTabRenamable(lockedFirst.TabPages[0], false);
            lockedFirst.SetTabClosable(lockedFirst.TabPages[0], false);
            lockedFirst.RenameTabAfterAdding = true;

            AddOverviewPage(
                overview,
                "Editable",
                "TabControl: right-click a tab to add one behind it, rename it (or double-click) or close it. The last tab always stays; the right-click also selects. Right: first tab locked.",
                CreateTabControlDemo(UIStyles.TabControls.CreateStandard(), 3, "Right-click a tab to add, rename or close tabs"),
                lockedFirst);

            System.Windows.Forms.TabControl multiline = UIStyles.TabControls.CreateReadOnlyPrimary();
            multiline.Multiline = true;
            AddOverviewPage(
                overview,
                "Many tabs",
                "More tabs than fit: scroll arrows at the end of the strip (left), or wrapping onto several rows with Multiline (right).",
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyPrimary(), 9, "TabControl with overflow arrows"),
                CreateTabControlDemo(multiline, 9, "TabControl with Multiline"));

            AddOverviewPage(
                overview,
                "Alignment",
                "Alignment = Top, Bottom, Left and Right.",
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Top),
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Bottom),
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Left),
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Right));

            AddOverviewPage(
                overview,
                "Icons and states",
                "Tab images from an ImageList, the hover highlight (HotTrack, on by default) and a disabled page (left). Right: AllowSelectingDisabledTabs = false - the disabled tabs cannot be selected, the arrow keys skip them.",
                CreateTabIconsDemo(),
                CreateTabDisabledTabsDemo());

            AddOverviewPage(
                overview,
                "Customization",
                "The customization routes of the standard control still work: DrawMode = OwnerDrawFixed with your own DrawItem (left) and pages with their own BackColor (right).",
                CreateTabOwnerDrawDemo(),
                CreateTabPageColorsDemo());

            // PropertyTable centers a control in its row at the control's own
            // height, it does not stretch it to the row - so the height has
            // to come from the control itself.
            overview.MinimumSize = new System.Drawing.Size(0, OverviewHeight);
            return overview;
        }

        private const int OverviewHeight = 320;

        // One page of the overview: a line of explanation on top, the demos
        // side by side underneath, sharing the width equally.
        private static void AddOverviewPage(
            System.Windows.Forms.TabControl overview,
            string title,
            string description,
            params System.Windows.Forms.Control[] demos)
        {
            System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
            page.Padding = new System.Windows.Forms.Padding(8);

            System.Windows.Forms.TableLayoutPanel grid = UIStyles.TableLayoutPanels.CreateStandard(demos.Length, 2);
            grid.BackColor = UIColors.BackgroundMedium;
            grid.Dock = System.Windows.Forms.DockStyle.Fill;

            for (int i = 0; i < demos.Length; i++)
            {
                grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f / demos.Length));
            }

            grid.RowStyles.Clear();
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));

            System.Windows.Forms.Label text = UIStyles.Labels.CreateMuted(description);
            text.AutoSize = true;
            text.MaximumSize = new System.Drawing.Size(900, 0);
            text.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            grid.Controls.Add(text, 0, 0);
            grid.SetColumnSpan(text, demos.Length);

            for (int i = 0; i < demos.Length; i++)
            {
                demos[i].Dock = System.Windows.Forms.DockStyle.Fill;
                demos[i].Margin = new System.Windows.Forms.Padding(0, 0, i < demos.Length - 1 ? 8 : 0, 0);
                grid.Controls.Add(demos[i], i, 1);
            }

            page.Controls.Add(grid);
            overview.TabPages.Add(page);
        }

        // Tab images and a disabled page.
        private static System.Windows.Forms.Control CreateTabIconsDemo()
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();

            System.Windows.Forms.ImageList images = new System.Windows.Forms.ImageList
            {
                ImageSize = new System.Drawing.Size(16, 16),
                ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit
            };

            System.Collections.Generic.List<System.Drawing.Bitmap> dots = new System.Collections.Generic.List<System.Drawing.Bitmap>();

            foreach (System.Drawing.Color color in new[] { UIColors.GreenLight, UIColors.YellowLight, UIColors.RedLight })
            {
                System.Drawing.Bitmap dot = new System.Drawing.Bitmap(16, 16);

                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(dot))
                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(color))
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.FillEllipse(brush, 2, 2, 12, 12);
                }

                images.Images.Add(dot);
                dots.Add(dot);
            }

            // The list only copies the images once its handle exists.
            _ = images.Handle;

            foreach (System.Drawing.Bitmap dot in dots)
            {
                dot.Dispose();
            }

            tabs.ImageList = images;
            tabs.Disposed += (sender, e) => images.Dispose();

            string[] titles = { "Done", "Pending", "Failed", "Disabled" };

            for (int i = 0; i < titles.Length; i++)
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(titles[i]);
                page.Controls.Add(CreateTabPageLabel(i < 3 ? "Tab with image " + i : "A disabled page: its content is disabled too"));

                if (i < 3)
                {
                    page.ImageIndex = i;
                }
                else
                {
                    page.Enabled = false;
                }

                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        // Disabled tabs that cannot be selected at all.
        private static System.Windows.Forms.Control CreateTabDisabledTabsDemo()
        {
            Controls.ReadOnlyTabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();
            tabs.AllowSelectingDisabledTabs = false;

            string[] titles = { "One", "Two (disabled)", "Three", "Four (disabled)", "Five" };

            for (int i = 0; i < titles.Length; i++)
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(titles[i]);
                page.Controls.Add(CreateTabPageLabel("Tab " + (i + 1) + ": click the tabs or use the arrow keys"));
                page.Enabled = i % 2 == 0;
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        // DrawMode = OwnerDrawFixed: the DrawItem handler draws every tab.
        private static System.Windows.Forms.Control CreateTabOwnerDrawDemo()
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();
            tabs.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;

            tabs.DrawItem += (sender, e) =>
            {
                bool selected = (e.State & System.Windows.Forms.DrawItemState.Selected) != 0;
                System.Drawing.Color back = selected
                    ? System.Drawing.Color.FromArgb(30, 150, 70)
                    : System.Drawing.Color.FromArgb(70, 30, 30);

                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(back))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                System.Windows.Forms.TextRenderer.DrawText(
                    e.Graphics,
                    tabs.TabPages[e.Index].Text,
                    e.Font,
                    e.Bounds,
                    System.Drawing.Color.White,
                    System.Windows.Forms.TextFormatFlags.HorizontalCenter | System.Windows.Forms.TextFormatFlags.VerticalCenter);
            };

            foreach (string title in new[] { "Overview", "Details", "Settings" })
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
                page.Controls.Add(CreateTabPageLabel("Tabs drawn by DrawItem"));
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        // Pages with their own BackColor keep it; the middle one follows
        // the control's PageBackColor.
        private static System.Windows.Forms.Control CreateTabPageColorsDemo()
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();

            System.Drawing.Color[] colors =
            {
                System.Drawing.Color.FromArgb(90, 30, 30),
                System.Drawing.Color.Empty,
                System.Drawing.Color.FromArgb(20, 80, 50)
            };
            string[] titles = { "Own color", "Default", "Own color 2" };

            for (int i = 0; i < titles.Length; i++)
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(titles[i]);

                if (!colors[i].IsEmpty)
                {
                    page.BackColor = colors[i];
                }

                page.Controls.Add(CreateTabPageLabel(
                    colors[i].IsEmpty ? "No BackColor set: PageBackColor" : "TabPage.BackColor set on this page"));
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        private static System.Windows.Forms.Control CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment alignment)
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreatePrimary();
            tabs.Alignment = alignment;

            foreach (string title in new[] { "Overview", "Details" })
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
                page.Controls.Add(CreateTabPageLabel("Alignment = " + alignment));
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        private static System.Windows.Forms.Label CreateTabPageLabel(string text)
        {
            System.Windows.Forms.Label label = UIStyles.Labels.CreateNormal(text);
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
