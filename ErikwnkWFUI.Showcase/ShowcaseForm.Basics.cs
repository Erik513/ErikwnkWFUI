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

            System.Windows.Forms.CheckBox compactChecked = UIStyles.CheckBoxes.CreateCompact(true);
            System.Windows.Forms.CheckBox compactUnchecked = UIStyles.CheckBoxes.CreateCompact(false);
            System.Windows.Forms.CheckBox compactDisabled = UIStyles.CheckBoxes.CreateCompact(true);
            compactDisabled.Enabled = false;
            AddUniformRow(table, "CreateCompact", compactChecked, compactUnchecked, compactDisabled);
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
            ComboBox combo = UIStyles.ComboBoxes.CreateStandard();
            combo.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            combo.SelectedIndex = 0;
            ComboBox comboDisabled = UIStyles.ComboBoxes.CreateStandard();
            comboDisabled.Items.AddRange(new object[] { "Option A", "Option B", "Option C" });
            comboDisabled.SelectedIndex = 0;
            comboDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", combo, comboDisabled);
        }

        private void AddContextMenusSection(PropertyTable table)
        {
            Button standardButton = UIStyles.Buttons.CreateStandard("Right-click here (Standard)", size: new Size(220, 32));
            standardButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenus.CreateStandard());

            Button primaryButton = UIStyles.Buttons.CreateStandard("Right-click here (Primary)", size: new Size(220, 32));
            primaryButton.ContextMenuStrip = BuildDemoContextMenu(UIStyles.ContextMenus.CreatePrimary());

            table.AddRow("CreateStandard", standardButton);
            table.AddRow("CreatePrimary", primaryButton);
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
            Label title = UIStyles.Labels.CreateTitle("Title label");
            Label titleDisabled = UIStyles.Labels.CreateTitle("Title label");
            titleDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateTitle", title, titleDisabled);

            Label normal = UIStyles.Labels.CreateNormal("Normal body text");
            Label normalDisabled = UIStyles.Labels.CreateNormal("Normal body text");
            normalDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateNormal", normal, normalDisabled);

            Label muted = UIStyles.Labels.CreateMuted("Muted / de-emphasized text");
            Label mutedDisabled = UIStyles.Labels.CreateMuted("Muted / de-emphasized text");
            mutedDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateMuted", muted, mutedDisabled);
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
                Label caption = UIStyles.Labels.CreateNormal(name);
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
