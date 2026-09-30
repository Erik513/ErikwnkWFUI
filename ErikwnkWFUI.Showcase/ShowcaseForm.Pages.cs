using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Showcase
{
    // The page frame: one tab per control, an explanation on top and the
    // variants below. The popup buttons stay outside, in a column on the
    // right.
    public partial class ShowcaseForm
    {
        // What a tab needs to build its content when it is first opened.
        private sealed class PageInfo
        {
            public string Description;
            public Action<PropertyTable> Fill;
            public bool Built;
        }

        // Alphabetical by title, so the tab order does not silently depend
        // on this list being kept in order elsewhere.
        private (string Title, string Description, Action<PropertyTable> Fill)[] GetPages()
        {
            return new (string, string, Action<PropertyTable>)[]
            {
                ("Buttons",
                 "Buttons in the library's own design. Standard is the neutral one, Primary follows the current accent color, Green confirms and Red is for destructive actions; Browse is the small button next to a path field. Each row shows the enabled and the disabled look.",
                 AddButtonsSection),

                ("CheckBoxes",
                 "Check boxes in the theme colors. Standard carries a text, Compact is just the box. The columns show checked, unchecked and disabled.",
                 AddCheckBoxesSection),

                ("ComboBoxes",
                 "Drop-down list in the theme colors, including the list that opens. Left enabled, right disabled.",
                 AddComboBoxesSection),

                ("ContextMenuStrip",
                 "The themed context menu that the other controls use as well. Standard has a neutral selection, Primary follows the accent color. Right-click the buttons: the menu shows an icon, a check mark, a disabled item, a separator and a sub menu.",
                 AddContextMenusSection),

                ("DataGridView",
                 "Tables bound to a list. The editable ones (CreateStandard, CreatePrimary) let the user add, delete, cut and paste rows; the read-only ones (CreateReadOnlyStandard, CreateReadOnlyPrimary) only sort by clicking a column header, and the right one of each pair is disabled. Primary uses the accent color for the selection.",
                 AddDataGridSection),

                ("Labels",
                 "Three text styles: Title, Normal and Muted (for secondary text). Left enabled, right disabled.",
                 AddLabelsSection),

                ("ListBoxControl",
                 "A list with a caption, numbering and drag-to-reorder. Long items wrap onto a second line and are cut off with an ellipsis after that. Standard is neutral, Primary follows the accent color.",
                 AddListBoxControlSection),

                ("ListView",
                 "The themed list view with sortable and resizable columns. Clicking outside of it clears the selection. Standard is neutral, Primary follows the accent color.",
                 AddListViewSection),

                ("NumericUpDown",
                 "Number input with themed arrows. Left enabled, right disabled.",
                 AddNumericUpDownsSection),

                ("Panels",
                 "The background shades the layout is built from: Dark, Medium, Elevated and Primary (the accent color).",
                 AddPanelsSection),

                ("ProgressBars",
                 "Progress bars in grey, green, primary (accent color) and status (the color shifts from red over yellow to green with the value). The Transparent ones let the background show through. All enabled bars run on one shared animation, the disabled ones stay at a fixed value.",
                 AddProgressBarsSection),

                ("SliderBar",
                 "A slider for a value between 0 and 1. Standard is neutral, Primary follows the accent color. Left enabled, right disabled.",
                 AddSliderBarSection),

                ("SlimProgressBars",
                 "A thin progress bar for tight places: Standard, Green, Primary and Status (the color follows the value). Left animated, right disabled.",
                 AddSlimProgressBarsSection),

                ("Spinner",
                 "Loading circles. Standard, Green and Primary spin endlessly, in the default size and in a larger, heavier one. The Progress variants show a percentage instead; Progress Status shifts from red to green.",
                 AddSpinnerSection),

                ("TabControl",
                 "The tabs this window is made of. ReadOnlyTabControl can be switched but not edited; TabControl also lets the user add, rename and close tabs with a right-click menu, and individual tabs can be locked. Tabs that do not fit scroll with arrows, and a cut-off tab shows its full name as a tooltip. The last row shows every variant.",
                 AddTabControlSection),

                ("TextBoxes",
                 "Text input in the theme colors. Standard has a border, Borderless blends into the surface behind it. Left enabled, right disabled.",
                 AddTextBoxesSection),

                ("ToggleSwitches",
                 "An on/off switch in three sizes (Small, Standard, Large) with optional captions. The columns show on, off and disabled.",
                 AddToggleSwitchesSection),

                ("VolumeSlider",
                 "A slider with a popup that shows the value while it is dragged; drag the enabled one to see it. Standard is neutral, Primary follows the accent color.",
                 AddVolumeSliderSection)
            };
        }

        private Control BuildPages()
        {
            ReadOnlyTabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();
            tabs.Dock = DockStyle.Fill;

            // The header is transparent by default and would show the window
            // color behind it, which is dark even in the light theme.
            tabs.HeaderBackColor = UIColors.BackgroundMedium;

            foreach ((string title, string description, Action<PropertyTable> fill) in GetPages())
            {
                TabPage page = new TabPage(title)
                {
                    Tag = new PageInfo { Description = description, Fill = fill }
                };

                tabs.TabPages.Add(page);
            }

            tabs.SelectedIndex = Math.Min(_selectedPageIndex, tabs.TabCount - 1);

            // A page is built when it is first shown, not all at once: the
            // window opens faster and a theme switch only rebuilds the page
            // that is actually in view.
            tabs.SelectedIndexChanged += delegate { ShowSelectedPage(); };
            return tabs;
        }

        private void ShowSelectedPage()
        {
            if (!(_pageTabs is System.Windows.Forms.TabControl tabs) || tabs.SelectedTab == null)
                return;

            TabPage page = tabs.SelectedTab;
            PageInfo info = (PageInfo)page.Tag;

            if (info.Built)
                return;

            info.Built = true;
            page.SuspendLayout();

            // Filled first, so the scroll area (and not the description) is
            // what the docking leaves the remaining space to.
            Panel scrollHost = UIStyles.Panels.CreateMedium();
            scrollHost.Dock = DockStyle.Fill;
            scrollHost.AutoScroll = true;

            PropertyTable table = UIStyles.PropertyTables.CreateStandard();
            table.Dock = DockStyle.Top;
            info.Fill(table);
            scrollHost.Controls.Add(table);

            page.Controls.Add(scrollHost);
            page.Controls.Add(CreateDescription(info.Description));
            page.ResumeLayout();
        }

        // The explanation on top of a page: a wrapped text on an elevated
        // strip that grows to fit it.
        private static Panel CreateDescription(string text)
        {
            Panel strip = UIStyles.Panels.CreateElevated();
            strip.Dock = DockStyle.Top;
            strip.Padding = new Padding(16, 12, 16, 12);

            Label label = UIStyles.Labels.CreateNormal(text);
            label.AutoSize = false;
            label.AutoEllipsis = false;
            label.TextAlign = ContentAlignment.TopLeft;
            label.Dock = DockStyle.Fill;
            strip.Controls.Add(label);

            strip.SizeChanged += delegate { FitDescriptionHeight(strip, label); };
            FitDescriptionHeight(strip, label);

            return strip;
        }

        private static void FitDescriptionHeight(Panel strip, Label label)
        {
            int width = strip.Width - strip.Padding.Horizontal;

            if (width <= 0)
                return;

            Size needed = TextRenderer.MeasureText(
                label.Text,
                label.Font,
                new Size(width, int.MaxValue),
                TextFormatFlags.WordBreak);

            int height = needed.Height + strip.Padding.Vertical;

            if (strip.Height != height)
                strip.Height = height;
        }

        // The buttons that open a popup (MessageBox, ToastForm, ...). They
        // belong to no single control, so they stay out of the tabs.
        private Panel BuildPopupsHost()
        {
            Panel host = UIStyles.Panels.CreateMedium();
            host.Dock = DockStyle.Right;
            host.Width = 380;

            PropertyTable table = UIStyles.PropertyTables.CreateStandard();
            table.Dock = DockStyle.Top;
            AddPopupsSection(table);
            host.Controls.Add(table);

            return host;
        }
    }
}
