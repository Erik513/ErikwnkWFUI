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
using FlowLayoutPanel = System.Windows.Forms.FlowLayoutPanel;
using Panel = System.Windows.Forms.Panel;
using TableLayoutPanel = System.Windows.Forms.TableLayoutPanel;

namespace ErikwnkWFUI.Showcase
{
    // The plain controls: buttons, check boxes, text input, labels and the context menu.
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
            // The columns: on, off, disabled (on). Every row is one factory call; the tooltips say On and Off.
            ToggleSwitch disabledToggle = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off");
            disabledToggle.Enabled = false;
            AddUniformRow(table, "CreateStandard",
                UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off"),
                UIStyles.ToggleSwitches.CreateStandard(false, "On", "Off"),
                disabledToggle);

            ToggleSwitch primaryDisabled = UIStyles.ToggleSwitches.CreatePrimary(true, "On", "Off");
            primaryDisabled.Enabled = false;
            AddUniformRow(table, "CreatePrimary",
                UIStyles.ToggleSwitches.CreatePrimary(true, "On", "Off"),
                UIStyles.ToggleSwitches.CreatePrimary(false, "On", "Off"),
                primaryDisabled);

            ToggleSwitch yellowDisabled = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off", UIStyles.Colors.Sets.Yellow);
            yellowDisabled.Enabled = false;
            AddUniformRow(table, "CreateStandard(colors: Yellow)",
                UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off", UIStyles.Colors.Sets.Yellow),
                UIStyles.ToggleSwitches.CreateStandard(false, "On", "Off", UIStyles.Colors.Sets.Yellow),
                yellowDisabled);

            // A single color instead of a color set: it is taken as the main color.
            ToggleSwitch purpleDisabled = UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off", Color.MediumPurple);
            purpleDisabled.Enabled = false;
            AddUniformRow(table, "CreateStandard(colors: MediumPurple)",
                UIStyles.ToggleSwitches.CreateStandard(true, "On", "Off", Color.MediumPurple),
                UIStyles.ToggleSwitches.CreateStandard(false, "On", "Off", Color.MediumPurple),
                purpleDisabled);

            // Another size: small (35 x 20) and large (55 x 30).
            ToggleSwitch smallDisabled = UIStyles.ToggleSwitches.CreatePrimary(true, "On", "Off", new Size(35, 20));
            smallDisabled.Enabled = false;
            AddUniformRow(table, "size: 35 x 20",
                UIStyles.ToggleSwitches.CreatePrimary(true, "On", "Off", new Size(35, 20)),
                UIStyles.ToggleSwitches.CreatePrimary(false, "On", "Off", new Size(35, 20)),
                smallDisabled);

            ToggleSwitch largeDisabled = UIStyles.ToggleSwitches.CreatePrimary(true, "On", "Off", new Size(55, 30));
            largeDisabled.Enabled = false;
            AddUniformRow(table, "size: 55 x 30",
                UIStyles.ToggleSwitches.CreatePrimary(true, "On", "Off", new Size(55, 30)),
                UIStyles.ToggleSwitches.CreatePrimary(false, "On", "Off", new Size(55, 30)),
                largeDisabled);

            // The event: raised for every change, by the user and by code alike (as on a standard check box).
            ToggleSwitch live = UIStyles.ToggleSwitches.CreatePrimary(false, "On", "Off");
            Label shown = UIStyles.Labels.CreateStandard("Checked = False");
            shown.AutoSize = true;
            live.CheckedChanged += delegate { shown.Text = "Checked = " + live.Checked; };
            table.AddRow("CheckedChanged",
                UIColumn.Percent(live, UniformColumnPercent),
                UIColumn.Percent(shown, UniformColumnPercent * 2));
        }

        // The property table makes a text box that it holds borderless (its cells are the frame). Inside a panel the box keeps its own
        // frame - a quiet outline, and while the box has the focus the color of the factory: this is what the rows below show.
        private static Control Framed(TextBox box)
        {
            // (The cell gives a control the height it has, so the holder is as high as the box and its padding.)
            Panel holder = new Panel { BackColor = Color.Transparent, Padding = new Padding(0, 2, 0, 2), Height = box.PreferredHeight + 4 };
            box.Dock = DockStyle.Fill;
            holder.Controls.Add(box);

            return holder;
        }

        private static TextBox Disabled(TextBox box)
        {
            box.Enabled = false;

            return box;
        }

        private void AddTextBoxesSection(PropertyTable table)
        {
            // Click into a box to see the frame while it has the focus.
            AddTwoColumnRow(table, "CreateStandard",
                Framed(UIStyles.TextBoxes.CreateStandard("", "Standard")),
                Framed(Disabled(UIStyles.TextBoxes.CreateStandard("Disabled", ""))));

            AddTwoColumnRow(table, "CreatePrimary",
                Framed(UIStyles.TextBoxes.CreatePrimary("", "Primary")),
                Framed(Disabled(UIStyles.TextBoxes.CreatePrimary("Disabled", ""))));

            AddTwoColumnRow(table, "CreateStandard(colors: Yellow)",
                Framed(UIStyles.TextBoxes.CreateStandard("", "Yellow", UIStyles.Colors.Sets.Yellow)),
                Framed(Disabled(UIStyles.TextBoxes.CreateStandard("Disabled", "", UIStyles.Colors.Sets.Yellow))));

            // A single color instead of a color set: it is taken as the main color.
            AddTwoColumnRow(table, "CreateStandard(colors: MediumPurple)",
                Framed(UIStyles.TextBoxes.CreateStandard("", "MediumPurple", System.Drawing.Color.MediumPurple)),
                Framed(Disabled(UIStyles.TextBoxes.CreateStandard("Disabled", "", System.Drawing.Color.MediumPurple))));

            // The text can be read, selected and copied, only not changed: the background is set apart (less than a disabled one).
            TextBox readOnly = UIStyles.TextBoxes.CreateStandard("ReadOnly = true", "");
            readOnly.ReadOnly = true;
            TextBox readOnlyDisabled = UIStyles.TextBoxes.CreateStandard("Disabled", "");
            readOnlyDisabled.ReadOnly = true;
            readOnlyDisabled.Enabled = false;
            AddTwoColumnRow(table, "ReadOnly = true", Framed(readOnly), Framed(readOnlyDisabled));

            // A text that is given stays; the placeholder stands in only for an empty box.
            AddTwoColumnRow(table, "text and placeholder",
                Framed(UIStyles.TextBoxes.CreateStandard("A text that is given", "Placeholder")),
                Framed(UIStyles.TextBoxes.CreateStandard("", "Placeholder")));

            // Without a frame the box blends into the surface behind it (the table shows every box like this).
            TextBox borderless = UIStyles.TextBoxes.CreateBorderless("Borderless text", "");
            TextBox borderlessDisabled = UIStyles.TextBoxes.CreateBorderless("Disabled", "");
            borderlessDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateBorderless", borderless, borderlessDisabled);
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
    }
}
