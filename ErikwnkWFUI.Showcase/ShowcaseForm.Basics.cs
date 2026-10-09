using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
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

            Button green = UIStyles.Buttons.CreateGreen("Confirm", size: textButtonSize);
            Button greenDisabled = UIStyles.Buttons.CreateGreen("Disabled", size: textButtonSize);
            greenDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateGreen", green, greenDisabled);

            Button danger = UIStyles.Buttons.CreateRed("Delete", size: textButtonSize);
            Button dangerDisabled = UIStyles.Buttons.CreateRed("Disabled", size: textButtonSize);
            dangerDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateRed", danger, dangerDisabled);

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
                "Colors.Sets",
                118,
                UIColumn.Percent(CreateSetButtons(sets, enabled: true), TwoColumnPercent),
                UIColumn.Percent(CreateSetButtons(sets, enabled: false), TwoColumnPercent));

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

        private static Control CreatePresetButtons((string Name, UIButtonPreset Preset)[] presets)
        {
            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateStandard();
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
            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateStandard();
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

            // The third state (ThreeState and CheckState) of the Standard one ...
            System.Windows.Forms.CheckBox undetermined = UIStyles.CheckBoxes.CreateStandard("Undetermined", true);
            undetermined.ThreeState = true;
            undetermined.CheckState = CheckState.Indeterminate;
            System.Windows.Forms.CheckBox undeterminedDisabled = UIStyles.CheckBoxes.CreateStandard("Disabled", true);
            undeterminedDisabled.ThreeState = true;
            undeterminedDisabled.CheckState = CheckState.Indeterminate;
            undeterminedDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard (undetermined)", undetermined, undeterminedDisabled);

            // ... and of the Primary one.
            System.Windows.Forms.CheckBox primaryUndetermined = UIStyles.CheckBoxes.CreatePrimary("Undetermined", true);
            primaryUndetermined.ThreeState = true;
            primaryUndetermined.CheckState = CheckState.Indeterminate;
            System.Windows.Forms.CheckBox primaryUndeterminedDisabled = UIStyles.CheckBoxes.CreatePrimary("Disabled", true);
            primaryUndeterminedDisabled.ThreeState = true;
            primaryUndeterminedDisabled.CheckState = CheckState.Indeterminate;
            primaryUndeterminedDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary (undetermined)", primaryUndetermined, primaryUndeterminedDisabled);

            // CreateStandard with a color set.
            System.Windows.Forms.CheckBox greenChecked = UIStyles.CheckBoxes.CreateStandard("Checked", true, UIStyles.Colors.Sets.Green);
            System.Windows.Forms.CheckBox greenUnchecked = UIStyles.CheckBoxes.CreateStandard("Unchecked", false, UIStyles.Colors.Sets.Green);
            System.Windows.Forms.CheckBox greenDisabled = UIStyles.CheckBoxes.CreateStandard("Disabled", true, UIStyles.Colors.Sets.Green);
            greenDisabled.Enabled = false;
            AddUniformRow(table, "CreateStandard(colors)", greenChecked, greenUnchecked, greenDisabled);
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
            System.Windows.Forms.ComboBox combo = UIStyles.ComboBoxes.CreateStandard();
            combo.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            combo.SelectedIndex = 0;
            System.Windows.Forms.ComboBox comboDisabled = UIStyles.ComboBoxes.CreateStandard();
            comboDisabled.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            comboDisabled.SelectedIndex = 0;
            comboDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", combo, comboDisabled);

            System.Windows.Forms.ComboBox primaryCombo = UIStyles.ComboBoxes.CreatePrimary();
            primaryCombo.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            primaryCombo.SelectedIndex = 0;
            System.Windows.Forms.ComboBox primaryComboDisabled = UIStyles.ComboBoxes.CreatePrimary();
            primaryComboDisabled.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            primaryComboDisabled.SelectedIndex = 0;
            primaryComboDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", primaryCombo, primaryComboDisabled);

            System.Windows.Forms.ComboBox greenCombo = UIStyles.ComboBoxes.CreateStandard(ComboBoxStyle.DropDownList, UIStyles.Colors.Sets.Green);
            greenCombo.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            greenCombo.SelectedIndex = 0;
            System.Windows.Forms.ComboBox greenComboDisabled = UIStyles.ComboBoxes.CreateStandard(ComboBoxStyle.DropDownList, UIStyles.Colors.Sets.Green);
            greenComboDisabled.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            greenComboDisabled.SelectedIndex = 0;
            greenComboDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard(colors)", greenCombo, greenComboDisabled);
        }

        private void AddContextMenusSection(PropertyTable table)
        {
            Button standardButton = UIStyles.Buttons.CreateStandard("Right-click here (Standard)", size: new Size(220, 32));
            standardButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenuStrips.CreateStandard());

            Button primaryButton = UIStyles.Buttons.CreateStandard("Right-click here (Primary)", size: new Size(220, 32));
            primaryButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenuStrips.CreatePrimary());

            Button colorsButton = UIStyles.Buttons.CreateStandard("Right-click here (Green)", size: new Size(220, 32));
            colorsButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenuStrips.CreateStandard(UIStyles.Colors.Sets.Green));

            table.AddRow("CreateStandard", standardButton);
            table.AddRow("CreatePrimary", primaryButton);
            table.AddRow("CreateStandard(colors)", colorsButton);
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
            NumericUpDown numeric = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            NumericUpDown numericDisabled = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            numericDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", numeric, numericDisabled);
        }

        private void AddLabelsSection(PropertyTable table)
        {
            AddLabelRow(table, "CreateStandard", "Standard body text", text => UIStyles.Labels.CreateStandard(text));
            AddLabelRow(table, "CreatePrimary", "Body text in the accent color", text => UIStyles.Labels.CreatePrimary(text));
            AddLabelRow(table, "CreateStandard(colors)", "Body text in a color set", text => UIStyles.Labels.CreateStandard(text, UIStyles.Colors.Sets.Green));
            AddLabelRow(table, "CreateTitle", "Title label", text => UIStyles.Labels.CreateTitle(text));
            AddLabelRow(table, "CreateMuted", "Muted / de-emphasized text", text => UIStyles.Labels.CreateMuted(text));

            // A text that is too long for its label moves back and forth instead of ending in an ellipsis.
            const string longText = "This text is much longer than the label it is shown in, so it rests at its start, moves to its end, rests there and moves back again - and a label that is disabled shows the start with an ellipsis instead.";

            AddLabelRow(table, "CreateStandard (scrolling)", longText, text =>
            {
                ThemedLabel label = UIStyles.Labels.CreateStandard(text);
                label.ScrollWhenTooLong = true;

                return label;
            });

            AddLabelRow(table, "CreateTitle (scrolling)", longText, text =>
            {
                ThemedLabel label = UIStyles.Labels.CreateTitle(text);
                label.ScrollWhenTooLong = true;

                return label;
            });

            AddLabelRow(table, "CreatePrimary (fast, short pause)", longText, text =>
            {
                ThemedLabel label = UIStyles.Labels.CreatePrimary(text);
                label.ScrollWhenTooLong = true;
                label.ScrollSpeed = LabelScrollSpeed.Fast;
                label.EdgePause = LabelScrollPause.Short;

                return label;
            });
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
            (string Name, Panel Panel)[] swatches =
            {
                ("Dark", UIStyles.Panels.CreateDark()),
                ("Medium", UIStyles.Panels.CreateMedium()),
                ("Elevated", UIStyles.Panels.CreateElevated()),
                ("Primary", UIStyles.Panels.CreatePrimary())
            };

            foreach ((string name, Panel panel) in swatches)
            {
                Label caption = UIStyles.Labels.CreateStandard(name);
                caption.AutoSize = true;
                caption.Location = new Point(6, 6);
                caption.BackColor = Color.Transparent;
                panel.Controls.Add(caption);
            }

            table.AddRow("CreateDark", swatches[0].Panel);
            table.AddRow("CreateMedium", swatches[1].Panel);
            table.AddRow("CreateElevated", swatches[2].Panel);
            table.AddRow("CreatePrimary", swatches[3].Panel);
        }
    }
}
