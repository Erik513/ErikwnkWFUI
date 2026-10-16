using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using Panel = System.Windows.Forms.Panel;

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

            // A page that is not a description plus a table of variants
            // (the guide) builds its whole content itself.
            public Func<Control> Custom;

            public bool Built;
        }

        // Alphabetical by title, so the tab order does not silently depend
        // on this list being kept in order elsewhere.
        private (string Title, string Description, Action<PropertyTable> Fill)[] GetPages()
        {
            return new (string, string, Action<PropertyTable>)[]
            {
                ("Buttons",
                 "Buttons in the library's own design. Standard is the neutral one, Primary follows the current accent color, Green confirms and Red is for destructive actions; CreateSymbol makes a button for any symbol of UIStyles.Symbols or one of your own, neutral or in a color - or from a preset (UIStyles.Buttons.Presets.Add, .Delete, .Browse ... or your own UIButtonPreset), which brings the symbol and the colors, and for any other color pass colors: (a ready-made set from Colors.Sets, a set from CreateSet, or simply a Color) - hover, pressed and disabled looks and the text on each are worked out for you. Each row shows the enabled and the disabled look.",
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
                 "Tables bound to a list. The editable ones (CreateStandard, CreatePrimary) let the user add, delete, cut and paste rows; the read-only ones (CreateReadOnlyStandard, CreateReadOnlyPrimary) only sort by clicking a column header, and the right one of each pair is disabled. Primary uses the accent color for the selection, and with colors: a color set (see the Buttons tab) any other color does.",
                 AddDataGridSection),

                ("InfoPopupForm",
                 "A small popup with extra info next to a control or the mouse. Its content is blocks of an optional header and an optional text; the text is white, or black on a light background, and every header and text can have its own color. The popup is sized to its content (long text wraps) or keeps one fixed size for a single short value that changes while it is shown (the percentage over a slider), and is opaque or slightly, clearly (the default) or strongly transparent. It hides itself after a short, medium (the default) or long time; every show call starts the time again, so it stays while the mouse moves over its control. For a hover popup the wait before it appears is none, short, medium (the default) or long. Standard is neutral gray, Primary follows the accent color, and with colors: a color set (see the Buttons tab) any other color does. AttachTo makes it the tooltip of any control (one popup serves many controls). Press the buttons to see the popups; the last four show the places: next to the control, at the mouse (move over the button), centered above, and at the mouse after a delay.",
                 AddInfoPopupsSection),

                ("Labels",
                 "Text in the theme colors: Standard (body text), Primary (accent color), Title (bold, larger) and Muted (quiet), each also in a color set. Left enabled, right disabled. A text that is too long can move instead of ending in an ellipsis: back and forth with a rest at both ends, or endlessly, one text or several one behind the other.",
                 AddLabelsSection),

                ("ListBox",
                 "A list with icons, numbering and drag-to-reorder (the handle on the right of each item). Long items wrap onto a second line and are cut off with an ellipsis after that. Standard is neutral, Primary follows the accent color, and with colors: a color set (see the Buttons tab) any other color does - it colors the selected item and the line that shows where a dragged item lands. The rows below are the same list box with a title above it (TitledListBoxes): in every other way it is the list box.",
                 AddListBoxSection),

                ("ListView",
                 "The themed list view with sortable and resizable columns. Clicking outside of it clears the selection. Standard is neutral, Primary follows the accent color, and with colors: a color set (see the Buttons tab) any other color does. FixedRowCount makes it exactly as big as its columns and a number of rows (here 5), without scroll bars; sorting and reordering the columns stay. AutoSizeToContent grows with the items up to a maximum size and then scrolls, the scroll bar next to the columns.",
                 AddListViewSection),

                ("NumericUpDown",
                 "Number input with the arrow fields drawn by the library. Standard is neutral, Primary follows the accent color, and with colors: a color set (see the Buttons tab) any other color does. Suffix (and Prefix) show a unit in the field while the value stays a number. Left enabled, right disabled.",
                 AddNumericUpDownsSection),

                ("Panels",
                 "The surfaces a layout is built from, as panels, tables and flow panels. Standard is the usual background of the theme, Primary a very dark shade of the accent color, and any other color comes from a color set (colors:). Dark, Medium, Elevated and Transparent are the other shades. A card has rounded corners that show the page behind them and a thin line. BorderStyle gives a panel, a table or a flow panel a thin frame (BorderColor changes its color); a table can also have lines around and between its cells (CellBorderStyle), of which only the ones between the cells or only the ones around the table can be shown (CellBorderLines) and which can be colored (CellBorderColor, InnerCellBorderColor, OuterCellBorderColor). The scroll bars of a scrolling panel follow the theme, also the square where they meet. The multi panel shows one page at a time, like a book: a header with the page count, the title and two arrows (flat or as a card; AddPage gives each page for its controls; Ctrl+PageDown and Ctrl+PageUp or Ctrl+Tab turn the pages from the keyboard; hover an arrow to see the title of the page it leads to). The version panel is a narrow strip with the version of the program at its right end and, while an update is known, a small green button next to it (ShowUpdate or CheckForUpdateAsync).",
                 AddPanelsSection),

                ("ProgressBars",
                 "Progress bars: Standard (gray, or any color of a color set), Primary (accent color), Green and Status (the color shifts from red over yellow to green with the value). Border and track: BorderStyle = None takes the border away, a BackColor of Transparent makes the track take the color of the panel (two separate properties). Style: Blocks, or Marquee for an action whose duration is not known. Text and percentage: both are off by default; the text stands on the left and the percentage on the right, TextAlign and PercentageAlign move them (at the same place they are one text). A text that is too long is cut with an ellipsis, or moves with TextScroll (as in a label). The slim progress bar is a thin strip for tight places (a status line under a text) with the same variants; its text stands above the strip, or below, left or right of it (TextPosition). All bars run on one shared animation; the first row of each kind also shows the disabled look (right, at a fixed value).",
                 AddProgressBarsSection),

                ("SliderBar",
                 "A slider for a value between 0 and 1. Standard is neutral, Primary follows the accent color. Left enabled, right disabled.",
                 AddSliderBarSection),

                ("Spinner",
                 "Loading circles. Standard, Green and Primary spin endlessly, in the default size and in a larger, heavier one. The Progress variants show a percentage instead; Progress Status shifts from red to green.",
                 AddSpinnerSection),

                ("Symbols",
                 "Ready-made symbols as text characters, for buttons, labels and menu items: UIStyles.Symbols.Add, UIStyles.Symbols.Delete and so on. They take the color of the control they are on and scale with its font. Draw them in UIStyles.Fonts.Icon through a control that draws with TextRenderer (a Button, one of the library's labels, a menu item); Graphics.DrawString can show empty boxes. CreateSymbol on the Buttons tab puts one on a button.",
                 AddSymbolsSection),

                ("TabControl",
                 "The tabs this window is made of. ReadOnlyTabControl can be switched but not edited; TabControl also lets the user add, rename and close tabs with a right-click menu, and individual tabs can be locked. Tabs that do not fit scroll with arrows, and a cut-off tab shows its full name as a tooltip.",
                 AddTabControlSection),

                ("TextBoxes",
                 "Text input in the theme colors. Standard has a border, Borderless blends into the surface behind it. Left enabled, right disabled.",
                 AddTextBoxesSection),

                ("ToastForm",
                 "A small confirmation that closes itself after a few seconds, near the bottom of the window. ShowStandard is neutral gray, ShowPrimary follows the accent color, and with colors: a color set (see the Buttons tab) any other color does; the text is white, or black on a light color. It belongs to its window: it moves with it, never takes the focus and is shown only while that window is in front. A toast that cannot be shown because the window is not in front is not lost - it waits (up to three, for one minute) and appears when the window is activated again. A new toast replaces one that is showing. The transparency is opaque, low, medium (the default) or high, and it stays a short, medium (the default) or long time. It can also wait before it appears: not at all (the default), a short, medium or long time.",
                 AddToastsSection),

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

            // With this many pages the strip wraps onto a second row instead of scrolling.
            tabs.Multiline = true;

            // Always first: the guide for someone new to the library.
            tabs.TabPages.Add(new TabPage("Guide") { Tag = new PageInfo { Custom = CreateGuidePage } });

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

            if (info.Custom != null)
            {
                page.Controls.Add(info.Custom());
                page.ResumeLayout();
                return;
            }

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

            Label label = UIStyles.Labels.CreateStandard(text);
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
