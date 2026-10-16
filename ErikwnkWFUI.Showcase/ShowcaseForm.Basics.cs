using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using MessageBox = ErikwnkWFUI.Forms.MessageBox;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Showcase
{
    // The plain controls: buttons, check boxes, text input, labels, panels and the context menu.
    public partial class ShowcaseForm
    {
        // Every "small control" row (everything but Lists, which keeps its
        // own wider/taller layout) goes through this one helper so all rows
        // share the same 3 equal-width editor columns - Variant 1 (standard
        // state) | Variant 2 (another state, or empty) | Disabled. Equal
        // Percent columns line up across rows because every row's editor
        // area is the same overall width, not because the widths are
        // absolute - see PropertyTable.CreateEditorLayout.
        private const float UniformColumnPercent = 100f / 3f;

        private void AddUniformRow(
            PropertyTable table,
            string labelText,
            Control variant1,
            Control variant2,
            Control disabled)
        {
            table.AddRow(
                labelText,
                UIColumn.Percent(variant1, UniformColumnPercent),
                UIColumn.Percent(variant2, UniformColumnPercent),
                UIColumn.Percent(disabled, UniformColumnPercent));
        }

        // For sections where every row only ever has a Variant 1 and a
        // Disabled control - no natural "another state" to put in the
        // middle slot (Buttons, ProgressBars, Labels) - AddUniformRow's
        // always-null Variant 2 column just sat there empty in every single
        // row of those sections. Two 50/50 columns instead of three, at the
        // cost of no longer lining up column-for-column with sections that
        // DO use all three (CheckBoxes/ToggleSwitches, TextBoxes, Panels -
        // this doesn't attempt to keep cross-section alignment, only
        // requested for the sections that never used the middle slot).
        private const float TwoColumnPercent = 50f;

        private void AddTwoColumnRow(
            PropertyTable table,
            string labelText,
            Control primary,
            Control disabled)
        {
            table.AddRow(
                labelText,
                UIColumn.Percent(primary, TwoColumnPercent),
                UIColumn.Percent(disabled, TwoColumnPercent));
        }

        private void AddButtonsSection(PropertyTable table)
        {
            Size textButtonSize = new Size(110, 32);

            Button standard = UIStyles.Buttons.CreateStandard("Standard", size: textButtonSize);
            Button standardDisabled = UIStyles.Buttons.CreateStandard("Disabled", size: textButtonSize);
            standardDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", standard, standardDisabled);

            Button primary = UIStyles.Buttons.CreatePrimary("Primary", size: textButtonSize);
            Button primaryDisabled = UIStyles.Buttons.CreatePrimary("Disabled", size: textButtonSize);
            primaryDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", primary, primaryDisabled);

            // Any color, ready-made or your own: fill, hover, pressed, disabled
            // and the text on each come from the color set.
            (string Name, UIColorSet Set)[] sets =
            {
                ("Primary", UIStyles.Colors.Sets.Primary),
                ("Red", UIStyles.Colors.Sets.Red),
                ("Orange", UIStyles.Colors.Sets.Orange),
                ("Yellow", UIStyles.Colors.Sets.Yellow),
                ("Green", UIStyles.Colors.Sets.Green),
                ("Teal", UIStyles.Colors.Sets.Teal),
                ("Blue", UIStyles.Colors.Sets.Blue),
                ("Purple", UIStyles.Colors.Sets.Purple),
                ("Pink", UIStyles.Colors.Sets.Pink),
                ("Brown", UIStyles.Colors.Sets.Brown),
                ("Gray", UIStyles.Colors.Sets.Gray)
            };

            table.AddRow(
                "CreateStandard(colors: …)",
                118,
                UIColumn.Percent(CreateSetButtons(sets, enabled: true), TwoColumnPercent),
                UIColumn.Percent(CreateSetButtons(sets, enabled: false), TwoColumnPercent));

            // The two short forms for confirming and for destructive actions, in one row (each with its disabled look on the right).
            AddTwoColumnRow(
                table,
                "CreateGreen, CreateRed",
                CreateShortFormButtons(textButtonSize, enabled: true),
                CreateShortFormButtons(textButtonSize, enabled: false));

            // Ready-made designs: pass a preset to CreateSymbol and it brings the symbol and
            // the colors. All presets of UIStyles.Buttons.Presets, found by reflection so a
            // new one shows up by itself. Hover over one for its name.
            (string Name, UIButtonPreset Preset)[] presets = typeof(UIStyles.Buttons.Presets)
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(property => (property.Name, (UIButtonPreset)property.GetValue(null)))
                .ToArray();

            const int presetsPerLine = 18;
            int presetLines = (presets.Length + presetsPerLine - 1) / presetsPerLine;

            table.AddRow(
                "CreateSymbol (preset)",
                presetLines * 36 + 14,
                CreatePresetButtons(presets));

            (string Name, UIColorSet Set)[] custom =
            {
                ("Lime", UIStyles.Colors.CreateSet(Color.FromArgb(120, 200, 40))),
                ("Sky", UIStyles.Colors.CreateSet(Color.FromArgb(100, 180, 240))),
                ("White", UIStyles.Colors.CreateSet(Color.FromArgb(250, 250, 250))),
                ("Black", UIStyles.Colors.CreateSet(Color.FromArgb(15, 15, 15))),
                ("Navy", UIStyles.Colors.CreateSet(Color.FromArgb(10, 20, 100))),
                ("Lilac", UIStyles.Colors.CreateSet(Color.FromArgb(180, 150, 230)))
            };

            table.AddRow(
                "CreateSet(color)",
                84,
                UIColumn.Percent(CreateSetButtons(custom, enabled: true), TwoColumnPercent),
                UIColumn.Percent(CreateSetButtons(custom, enabled: false), TwoColumnPercent));
        }

        private static Control CreateShortFormButtons(Size size, bool enabled)
        {
            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateTransparent();
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = true;

            foreach (Button button in new[]
            {
                UIStyles.Buttons.CreateGreen(enabled ? "Confirm" : "Disabled", size: size),
                UIStyles.Buttons.CreateRed(enabled ? "Delete" : "Disabled", size: size)
            })
            {
                button.Margin = new Padding(0, 0, 6, 6);
                button.Enabled = enabled;
                flow.Controls.Add(button);
            }

            return flow;
        }

        private static Control CreatePresetButtons((string Name, UIButtonPreset Preset)[] presets)
        {
            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateTransparent();
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = true;

            foreach ((string name, UIButtonPreset preset) in presets)
            {
                Button button = UIStyles.Buttons.CreateSymbol(preset, name);
                button.Margin = new Padding(0, 0, 6, 6);
                flow.Controls.Add(button);
            }

            return flow;
        }

        private static Control CreateSetButtons((string Name, UIColorSet Set)[] sets, bool enabled)
        {
            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateTransparent();
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = true;

            foreach ((string name, UIColorSet set) in sets)
            {
                Button button = UIStyles.Buttons.CreateStandard(name, size: new Size(74, 30), colors: set);
                button.Margin = new Padding(0, 0, 6, 6);
                button.Enabled = enabled;
                flow.Controls.Add(button);
            }

            return flow;
        }

        private void AddCheckBoxesSection(PropertyTable table)
        {
            System.Windows.Forms.CheckBox checkedBox = UIStyles.CheckBoxes.CreateStandard("Checked", true);
            System.Windows.Forms.CheckBox uncheckedBox = UIStyles.CheckBoxes.CreateStandard("Unchecked", false);
            System.Windows.Forms.CheckBox disabledBox = UIStyles.CheckBoxes.CreateStandard("Disabled", true);
            disabledBox.Enabled = false;
            AddUniformRow(table, "CreateStandard", checkedBox, uncheckedBox, disabledBox);

            System.Windows.Forms.CheckBox primaryChecked = UIStyles.CheckBoxes.CreatePrimary("Checked", true);
            System.Windows.Forms.CheckBox primaryUnchecked = UIStyles.CheckBoxes.CreatePrimary("Unchecked", false);
            System.Windows.Forms.CheckBox primaryDisabled = UIStyles.CheckBoxes.CreatePrimary("Disabled", true);
            primaryDisabled.Enabled = false;
            AddUniformRow(table, "CreatePrimary", primaryChecked, primaryUnchecked, primaryDisabled);

            // CreateStandard with a color set.
            System.Windows.Forms.CheckBox yellowChecked = UIStyles.CheckBoxes.CreateStandard("Checked", true, UIStyles.Colors.Sets.Yellow);
            System.Windows.Forms.CheckBox yellowUnchecked = UIStyles.CheckBoxes.CreateStandard("Unchecked", false, UIStyles.Colors.Sets.Yellow);
            System.Windows.Forms.CheckBox yellowDisabled = UIStyles.CheckBoxes.CreateStandard("Disabled", true, UIStyles.Colors.Sets.Yellow);
            yellowDisabled.Enabled = false;
            AddUniformRow(table, "CreateStandard(colors: Yellow)", yellowChecked, yellowUnchecked, yellowDisabled);

            System.Windows.Forms.CheckBox compactChecked = UIStyles.CheckBoxes.CreateStandardCompact(true);
            System.Windows.Forms.CheckBox compactUnchecked = UIStyles.CheckBoxes.CreateStandardCompact(false);
            System.Windows.Forms.CheckBox compactDisabled = UIStyles.CheckBoxes.CreateStandardCompact(true);
            compactDisabled.Enabled = false;
            AddUniformRow(table, "CreateStandardCompact", compactChecked, compactUnchecked, compactDisabled);

            System.Windows.Forms.CheckBox compactPrimaryChecked = UIStyles.CheckBoxes.CreatePrimaryCompact(true);
            System.Windows.Forms.CheckBox compactPrimaryUnchecked = UIStyles.CheckBoxes.CreatePrimaryCompact(false);
            System.Windows.Forms.CheckBox compactPrimaryDisabled = UIStyles.CheckBoxes.CreatePrimaryCompact(true);
            compactPrimaryDisabled.Enabled = false;
            AddUniformRow(table, "CreatePrimaryCompact", compactPrimaryChecked, compactPrimaryUnchecked, compactPrimaryDisabled);

            // The third state (ThreeState and CheckState): Standard, Primary and the disabled Standard one.
            AddUniformRow(
                table,
                "ThreeState (undetermined)",
                CreateUndetermined(UIStyles.CheckBoxes.CreateStandard("Undetermined", true), enabled: true),
                CreateUndetermined(UIStyles.CheckBoxes.CreatePrimary("Undetermined", true), enabled: true),
                CreateUndetermined(UIStyles.CheckBoxes.CreateStandard("Disabled", true), enabled: false));
        }

        private static System.Windows.Forms.CheckBox CreateUndetermined(System.Windows.Forms.CheckBox box, bool enabled)
        {
            box.ThreeState = true;
            box.CheckState = CheckState.Indeterminate;
            box.Enabled = enabled;

            return box;
        }

        private void AddToggleSwitchesSection(PropertyTable table)
        {
            ToggleSwitch standardOn = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off");
            ToggleSwitch standardOff = UIStyles.ToggleSwitches.CreateStandard(false, "On", "Off");
            ToggleSwitch disabledToggle = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off");
            disabledToggle.Enabled = false;
            AddUniformRow(table, "CreateStandard", standardOn, standardOff, disabledToggle);

            ToggleSwitch smallOn = UIStyles.ToggleSwitches.CreateSmall(true, "On", "Off");
            ToggleSwitch smallOff = UIStyles.ToggleSwitches.CreateSmall(false, "On", "Off");
            ToggleSwitch smallDisabled = UIStyles.ToggleSwitches.CreateSmall(true, "On", "Off");
            smallDisabled.Enabled = false;
            AddUniformRow(table, "CreateSmall", smallOn, smallOff, smallDisabled);

            ToggleSwitch largeOn = UIStyles.ToggleSwitches.CreateLarge(true, "On", "Off");
            ToggleSwitch largeOff = UIStyles.ToggleSwitches.CreateLarge(false, "On", "Off");
            ToggleSwitch largeDisabled = UIStyles.ToggleSwitches.CreateLarge(true, "On", "Off");
            largeDisabled.Enabled = false;
            AddUniformRow(table, "CreateLarge", largeOn, largeOff, largeDisabled);
        }

        private void AddTextBoxesSection(PropertyTable table)
        {
            TextBox standardBox = UIStyles.TextBoxes.CreateStandard("", "Standard");
            TextBox textDisabled = UIStyles.TextBoxes.CreateStandard("Disabled", "");
            textDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", standardBox, textDisabled);

            TextBox borderless = UIStyles.TextBoxes.CreateBorderstyleNone("Borderless text", "");
            TextBox borderlessDisabled = UIStyles.TextBoxes.CreateBorderstyleNone("Disabled", "");
            borderlessDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateBorderstyleNone", borderless, borderlessDisabled);
        }

        private void AddComboBoxesSection(PropertyTable table)
        {
            AddComboBoxRow(table, "CreateStandard", () => UIStyles.ComboBoxes.CreateStandard());
            AddComboBoxRow(table, "CreatePrimary", () => UIStyles.ComboBoxes.CreatePrimary());
            AddComboBoxRow(table, "CreateStandard(colors: Yellow)", () => UIStyles.ComboBoxes.CreateStandard(colors: UIStyles.Colors.Sets.Yellow));
        }

        // Enabled on the left, disabled on the right.
        private void AddComboBoxRow(PropertyTable table, string label, Func<System.Windows.Forms.ComboBox> create)
        {
            System.Windows.Forms.ComboBox enabled = create();
            System.Windows.Forms.ComboBox disabled = create();
            disabled.Enabled = false;

            foreach (System.Windows.Forms.ComboBox box in new[] { enabled, disabled })
            {
                box.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
                box.SelectedIndex = 0;
            }

            AddTwoColumnRow(table, label, enabled, disabled);
        }

        private void AddContextMenusSection(PropertyTable table)
        {
            Button standardButton = UIStyles.Buttons.CreateStandard("Right-click here", size: new Size(140, 32));
            standardButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenuStrips.CreateStandard());

            Button primaryButton = UIStyles.Buttons.CreateStandard("Right-click here", size: new Size(140, 32));
            primaryButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenuStrips.CreatePrimary());

            Button colorsButton = UIStyles.Buttons.CreateStandard("Right-click here", size: new Size(140, 32));
            colorsButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Yellow));

            table.AddRow("CreateStandard", standardButton);
            table.AddRow("CreatePrimary", primaryButton);
            table.AddRow("CreateStandard(colors: Yellow)", colorsButton);
        }

        // Exercises every native ToolStripMenuItem feature (icon, checkmark,
        // disabled state, separator, submenu) so a look at this demo
        // confirms the themed renderer handles all of them, not just plain
        // text items.
        private ErikwnkWFUI.Controls.ContextMenuStrip BuildDemoContextMenu(ErikwnkWFUI.Controls.ContextMenuStrip menu)
        {
            var openItem = new ToolStripMenuItem("Open", UIStyles.Icons.Folder);
            var saveItem = new ToolStripMenuItem("Save", UIStyles.Icons.Document);
            var detailsItem = new ToolStripMenuItem("Show details") { CheckOnClick = true, Checked = true };
            var disabledItem = new ToolStripMenuItem("Disabled item") { Enabled = false };

            var moreItem = new ToolStripMenuItem("More");
            moreItem.DropDownItems.Add(new ToolStripMenuItem("Sub item 1"));
            moreItem.DropDownItems.Add(new ToolStripMenuItem("Sub item 2"));

            menu.Items.Add(openItem);
            menu.Items.Add(saveItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(detailsItem);
            menu.Items.Add(disabledItem);
            menu.Items.Add(moreItem);

            return menu;
        }

        private void AddNumericUpDownsSection(PropertyTable table)
        {
            AddNumericUpDownRow(table, "CreateStandard", () => UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42));
            AddNumericUpDownRow(table, "CreatePrimary", () => UIStyles.NumericUpDowns.CreatePrimary(0, 100, 1, 42));
            AddNumericUpDownRow(table, "CreateStandard(colors: Yellow)", () => UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42, UIStyles.Colors.Sets.Yellow));
            AddNumericUpDownRow(table, "Suffix", () =>
            {
                ErikwnkWFUI.Controls.NumericUpDown box = (ErikwnkWFUI.Controls.NumericUpDown)UIStyles.NumericUpDowns.CreatePrimary(0, 100, 5, 40);
                box.Suffix = " %";

                return box;
            });
            AddNumericUpDownRow(table, "Prefix", () =>
            {
                ErikwnkWFUI.Controls.NumericUpDown box = (ErikwnkWFUI.Controls.NumericUpDown)UIStyles.NumericUpDowns.CreatePrimary(0, 1000, 10, 250);
                box.Prefix = "$ ";

                return box;
            });
        }

        // Enabled on the left, disabled on the right.
        private void AddNumericUpDownRow(PropertyTable table, string label, Func<System.Windows.Forms.NumericUpDown> create)
        {
            System.Windows.Forms.NumericUpDown enabled = create();
            System.Windows.Forms.NumericUpDown disabled = create();
            disabled.Enabled = false;
            AddTwoColumnRow(table, label, enabled, disabled);
        }

        private void AddLabelsSection(PropertyTable table)
        {
            table.AddSection("Normal");

            AddLabelRow(table, "CreateStandard", "Standard body text", text => UIStyles.Labels.CreateStandard(text));
            AddLabelRow(table, "CreatePrimary", "Body text in the accent color", text => UIStyles.Labels.CreatePrimary(text));
            AddLabelRow(table, "CreateStandard(colors: Yellow)", "Body text in a color set", text => UIStyles.Labels.CreateStandard(text, UIStyles.Colors.Sets.Yellow));

            table.AddSection("Title");

            AddLabelRow(table, "CreateTitle", "Title label", text => UIStyles.Labels.CreateTitle(text));
            AddLabelRow(table, "CreateTitle(colors: Yellow)", "Title label in a color set", text => UIStyles.Labels.CreateTitle(text, UIStyles.Colors.Sets.Yellow));

            table.AddSection("Muted");

            AddLabelRow(table, "CreateMuted", "Muted / de-emphasized text", text => UIStyles.Labels.CreateMuted(text));
            AddLabelRow(table, "CreateMuted(colors: Yellow)", "Muted text in a color set (dimmed)", text => UIStyles.Labels.CreateMuted(text, UIStyles.Colors.Sets.Yellow));

            table.AddSection("Animated");

            // A text that is too long for its label moves instead of ending in an ellipsis: back and forth with a
            // rest at both ends, or endlessly in one direction, coming again behind a gap and a separator.
            const string longText = "This text is much longer than the label it is shown in, so it rests at its start, moves to its end, rests there and moves back again - and a label that is disabled shows the start with an ellipsis instead.";

            table.AddRow("CreatePrimary (scroll: BackAndForth)",
                UIColumn.Percent(UIStyles.Labels.CreatePrimary(longText, scroll: LabelScroll.BackAndForth()), TwoColumnPercent));

            // Endless moves whether the text fits or not: a long text, and (second column) a short one that
            // keeps the room of the label.
            AddTwoColumnRow(table, "CreatePrimary (scroll: Endless)",
                UIStyles.Labels.CreatePrimary(longText, scroll: LabelScroll.Endless()),
                UIStyles.Labels.CreatePrimary("Short text", scroll: LabelScroll.Endless()));

            // Several texts, one behind the other: the same room after each of them, the separator in its middle.
            table.AddRow("CreatePrimary (scroll: Endless, texts)",
                UIColumn.Percent(UIStyles.Labels.CreatePrimary("", scroll: LabelScroll.Endless(new[] { "First message", "Second message", "Third" })), TwoColumnPercent));
        }

        // One row: the label, and the same label disabled in the second column.
        private void AddLabelRow(PropertyTable table, string name, string text, Func<string, Label> create)
        {
            Label label = create(text);
            Label disabled = create(text);
            disabled.Enabled = false;

            AddTwoColumnRow(table, name, label, disabled);
        }

        private void AddPanelsSection(PropertyTable table)
        {
            UIColorSet yellow = UIStyles.Colors.Sets.Yellow;
            const int swatchHeight = 42;

            table.AddSection("Panels");

            // The panels in each background of the theme, one row per factory method.
            (string Name, Panel Panel)[] swatches =
            {
                ("CreateStandard", UIStyles.Panels.CreateStandard()),
                ("CreatePrimary", UIStyles.Panels.CreatePrimary()),
                ("CreateStandard(colors: Yellow)", UIStyles.Panels.CreateStandard(yellow)),
                ("CreateDark", UIStyles.Panels.CreateDark()),
                ("CreateMedium", UIStyles.Panels.CreateMedium()),
                ("CreateElevated", UIStyles.Panels.CreateElevated())
            };

            foreach ((string name, Panel panel) in swatches)
            {
                // (The table does not stretch a control to the height of its row.)
                panel.MinimumSize = new Size(100, swatchHeight - 8);
                panel.Height = swatchHeight - 8;
                panel.Controls.Add(CreatePanelCaption(name, panel));
                table.AddRow(name, panel);
            }

            // BorderStyle alone: the frame in the color of the theme (BorderColor not set).
            Panel framed = UIStyles.Panels.CreateMedium();
            framed.BorderStyle = BorderStyle.FixedSingle;
            framed.Size = new Size(260, 50);
            framed.MinimumSize = framed.Size;
            framed.MaximumSize = framed.Size;
            framed.Dock = DockStyle.None;
            framed.Controls.Add(new Label { Text = "BorderStyle = FixedSingle", AutoSize = true, Location = new Point(6, 6), ForeColor = UIColors.TextPrimary, BackColor = Color.Transparent });
            table.AddRow("CreateMedium (BorderStyle = FixedSingle)", 60, framed);

            // The options of a panel in one: a frame (BorderStyle) in the library's color (BorderColor) and AutoScroll
            // (the scroll bars follow the theme, also the square where they meet).
            ThemedPanel options = (ThemedPanel)UIStyles.Panels.CreateMedium();
            options.BorderStyle = BorderStyle.FixedSingle;
            options.BorderColor = UIColors.Primary;
            options.AutoScroll = true;
            options.Size = new Size(260, 90);
            options.MinimumSize = options.Size;
            options.MaximumSize = options.Size;
            options.Dock = DockStyle.None;
            options.Controls.Add(new Label { Text = "BorderColor, AutoScroll: scroll in both directions", AutoSize = true, Location = new Point(6, 6), ForeColor = UIColors.TextPrimary, BackColor = Color.Transparent });
            options.Controls.Add(new Panel { Location = new Point(6, 30), Size = new Size(520, 160), BackColor = UIColors.PrimaryDark });
            table.AddRow("CreateMedium (BorderStyle, BorderColor, AutoScroll)", 110, options);

            // No background of its own: the page shows through (here, with a line of text on it).
            Panel transparent = UIStyles.Panels.CreateTransparent();
            transparent.Size = new Size(200, 24);
            transparent.MinimumSize = transparent.Size;
            transparent.Controls.Add(CreatePanelCaption("What is behind it shows", transparent));
            table.AddRow("CreateTransparent", transparent);

            table.AddSection("Cards");

            // A card: rounded corners that show the page behind them and a thin line.
            table.AddRow("CreateCardStandard", 92, CreateCardDemo(UIStyles.Panels.CreateCardStandard(), "A card"));
            table.AddRow("CreateCardPrimary", 92, CreateCardDemo(UIStyles.Panels.CreateCardPrimary(), "A card in the accent"));
            table.AddRow("CreateCardStandard(colors: Yellow)", 92, CreateCardDemo(UIStyles.Panels.CreateCardStandard(yellow), "A card in a color set"));

            // A card does not scroll itself (its scroll bars would lie over the rounded corners): a panel with AutoScroll is put in it.
            Panel scrollingCard = UIStyles.Panels.CreateCardStandard();
            scrollingCard.Size = new Size(260, 100);
            scrollingCard.MinimumSize = scrollingCard.Size;
            scrollingCard.MaximumSize = scrollingCard.Size;
            Panel scrollInside = UIStyles.Panels.CreateTransparent();
            scrollInside.Dock = DockStyle.Fill;
            scrollInside.AutoScroll = true;
            scrollInside.Controls.Add(new Label { Text = "A panel with AutoScroll in the card", AutoSize = true, Location = new Point(0, 0), ForeColor = UIColors.TextPrimary, BackColor = Color.Transparent });
            scrollInside.Controls.Add(new Panel { Location = new Point(0, 24), Size = new Size(520, 160), BackColor = UIColors.PrimaryDark });
            scrollingCard.Controls.Add(scrollInside);
            table.AddRow("CreateCardStandard (scrolling panel inside)", 120, scrollingCard);

            table.AddSection("Multi panels");

            AddMultiPanelsSection(table);

            table.AddSection("Table layout panels");

            TableLayoutPanel cells = UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(cells);
            table.AddRow("CreateStandard", 72, cells);

            TableLayoutPanel cellsPrimary = UIStyles.TableLayoutPanels.CreatePrimary(3, 2);
            ConfigureCellsDemo(cellsPrimary);
            table.AddRow("CreatePrimary", 72, cellsPrimary);

            TableLayoutPanel cellsInColor = UIStyles.TableLayoutPanels.CreateStandard(3, 2, yellow);
            ConfigureCellsDemo(cellsInColor);
            table.AddRow("CreateStandard(colors: Yellow)", 72, cellsInColor);

            TableLayoutPanel cellsDark = UIStyles.TableLayoutPanels.CreateDark(3, 2);
            ConfigureCellsDemo(cellsDark);
            table.AddRow("CreateDark", 72, cellsDark);

            TableLayoutPanel cellsTransparent = UIStyles.TableLayoutPanels.CreateTransparent(3, 2);
            ConfigureCellsDemo(cellsTransparent);
            table.AddRow("CreateTransparent", 72, cellsTransparent);

            // BorderStyle alone: the frame around the table in the color of the theme (BorderColor not set).
            TableLayoutPanel cellsFramed = UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(cellsFramed);
            cellsFramed.BorderStyle = BorderStyle.FixedSingle;
            table.AddRow("CreateStandard (BorderStyle = FixedSingle)", 72, cellsFramed);

            // The lines between the cells (CellBorderStyle) are off by default. CellBorderLines selects which are shown: only the
            // ones between the cells, or only the ones around the table.
            ThemedTableLayoutPanel inner = (ThemedTableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(inner);
            inner.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            inner.CellBorderLines = CellBorderLines.InnerOnly;
            inner.InnerCellBorderColor = UIColors.PrimaryLight;
            table.AddRow("CreateStandard (CellBorderLines: InnerOnly, InnerCellBorderColor)", 72, inner);

            ThemedTableLayoutPanel outer = (ThemedTableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(outer);
            outer.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            outer.CellBorderLines = CellBorderLines.OuterOnly;
            outer.OuterCellBorderColor = UIColors.PrimaryLight;
            table.AddRow("CreateStandard (CellBorderLines: OuterOnly, OuterCellBorderColor)", 72, outer);

            // Both kinds of lines in their own color: the ones around the table in red, the ones between the cells in green.
            ThemedTableLayoutPanel twoColors = (ThemedTableLayoutPanel)UIStyles.TableLayoutPanels.CreateStandard(3, 2);
            ConfigureCellsDemo(twoColors);
            twoColors.CellBorderStyle = TableLayoutPanelCellBorderStyle.Single;
            twoColors.OuterCellBorderColor = UIStyles.Colors.Sets.Red.Basic;
            twoColors.InnerCellBorderColor = UIStyles.Colors.Sets.Green.Basic;
            table.AddRow("CreateStandard (OuterCellBorderColor, InnerCellBorderColor)", 72, twoColors);

            table.AddSection("Flow layout panels");

            table.AddRow("CreateStandard", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreateStandard()));
            table.AddRow("CreatePrimary", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreatePrimary()));
            table.AddRow("CreateStandard(colors: Yellow)", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreateStandard(yellow)));
            table.AddRow("CreateTransparent", CreateFlowDemo(UIStyles.FlowLayoutPanels.CreateTransparent()));

            // BorderStyle alone: the frame in the color of the theme (BorderColor not set).
            FlowLayoutPanel framedDefault = UIStyles.FlowLayoutPanels.CreateStandard();
            framedDefault.BorderStyle = BorderStyle.FixedSingle;
            table.AddRow("CreateStandard (BorderStyle = FixedSingle)", CreateFlowDemo(framedDefault));

            // A frame around the panel (BorderStyle) in the library's color.
            ThemedFlowLayoutPanel framedFlow = (ThemedFlowLayoutPanel)UIStyles.FlowLayoutPanels.CreateStandard();
            framedFlow.BorderStyle = BorderStyle.FixedSingle;
            framedFlow.BorderColor = UIColors.Primary;
            table.AddRow("CreateStandard (BorderStyle, BorderColor)", CreateFlowDemo(framedFlow));

            table.AddSection("Version panels");

            table.AddRow("CreateStandard", CreateVersionPanelDemo(UIStyles.VersionPanels.CreateStandard()));
            table.AddRow("CreatePrimary", CreateVersionPanelDemo(UIStyles.VersionPanels.CreatePrimary()));
            table.AddRow("CreateStandard(colors: Yellow)", CreateVersionPanelDemo(UIStyles.VersionPanels.CreateStandard(yellow)));

            // An update is known: the green button appears next to the version (a click shows the prompt of the AppUpdater).
            VersionPanel updateAvailable = CreateVersionPanelDemo(UIStyles.VersionPanels.CreateStandard());
            updateAvailable.ShowUpdate(
                new AppUpdater("owner", "repo", VersionPanelDemoClient, VersionPanelDemoClient),
                new UpdateCheckResult(new Version(9, 9, 9), "", null),
                new Version(1, 0, 0));
            table.AddRow("CreateStandard (ShowUpdate)", updateAvailable);
        }

        // Not asked at all: the demo update is made up (no download address, no release page).
        private static readonly System.Net.Http.HttpClient VersionPanelDemoClient = new System.Net.Http.HttpClient();

        // The strip does not dock here, so it gets a width of its own.
        private static VersionPanel CreateVersionPanelDemo(VersionPanel panel)
        {
            panel.Dock = DockStyle.None;
            panel.Width = 380;
            panel.MinimumSize = panel.Size;
            panel.MaximumSize = panel.Size;

            return panel;
        }

        // Two buttons in a flow panel, to see the background around them.
        private static FlowLayoutPanel CreateFlowDemo(FlowLayoutPanel flow)
        {
            flow.Controls.Add(UIStyles.Buttons.CreateStandard("One", size: new Size(70, 28)));
            flow.Controls.Add(UIStyles.Buttons.CreateStandard("Two", size: new Size(70, 28)));

            return flow;
        }

        private static Label CreatePanelCaption(string text, Control surface)
        {
            Label caption = UIStyles.Labels.CreateStandard(text);
            caption.AutoSize = true;
            caption.Location = new Point(6, 6);
            caption.BackColor = Color.Transparent;
            FollowSurface(caption, surface);

            return caption;
        }

        // The text of a label is the best one for the surface it stands on (the theme's text color would be dark on the dark
        // accent in the light theme); a transparent surface leaves the text as it is.
        private static void FollowSurface(Label label, Control surface)
        {
            void Update()
            {
                label.ForeColor = surface.BackColor.A == 0 ? UIColors.TextPrimary : (surface.BackColor.GetBrightness() > 0.5f ? Color.Black : Color.White);
            }

            // Later than the surface itself, which changes its color on the same event.
            EventHandler changed = (sender, e) =>
            {
                if (label.IsHandleCreated && !label.IsDisposed)
                    label.BeginInvoke((Action)Update);
            };

            Update();
            UIStyles.Colors.ThemeChanged += changed;
            label.Disposed += (sender, e) => UIStyles.Colors.ThemeChanged -= changed;
        }

        // A card with a line of text in it; it does not dock, so it gets its size here.
        private static Panel CreateCardDemo(Panel card, string text)
        {
            card.Size = new Size(240, 70);
            card.MinimumSize = card.Size;

            Label caption = UIStyles.Labels.CreateStandard(text);
            caption.Dock = DockStyle.Fill;
            caption.BackColor = Color.Transparent;
            FollowSurface(caption, card);
            card.Controls.Add(caption);

            return card;
        }

        // Six cells (no lines by default; CellBorderStyle.Single draws them in the color of the theme or one that is set).
        private static void ConfigureCellsDemo(TableLayoutPanel cells)
        {
            cells.Size = new Size(260, 60);
            cells.MinimumSize = cells.Size;

            for (int i = 0; i < 3; i++)
                cells.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3));

            for (int i = 0; i < 2; i++)
                cells.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            for (int i = 0; i < 6; i++)
            {
                Label cell = UIStyles.Labels.CreateStandard("Cell " + (i + 1));
                cell.Dock = DockStyle.Fill;
                cell.BackColor = Color.Transparent;
                FollowSurface(cell, cells);
                cells.Controls.Add(cell);
            }
        }
    }
}
