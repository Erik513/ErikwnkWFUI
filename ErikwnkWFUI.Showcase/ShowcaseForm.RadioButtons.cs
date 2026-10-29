using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Showcase
{
    // The radio buttons: the factory methods, the box on the right, a group across panels and a group in a group box.
    public partial class ShowcaseForm
    {
        // The groups of this page are kept alive here (a group listens to its buttons).
        private readonly List<RadioGroup> _radioGroups = new List<RadioGroup>();

        // Radio buttons with the same parent form one group, and every cell of the table is the same parent: each button of a row that shows
        // just a state gets a small panel of its own, so a click on one does not clear the others.
        private static Control Alone(System.Windows.Forms.RadioButton radio)
        {
            Panel holder = UIStyles.Panels.CreateTransparent();
            holder.Dock = DockStyle.None;
            holder.Size = new Size(radio.Width + 4, radio.Height + 2);
            radio.Location = new Point(2, 1);
            holder.Controls.Add(radio);

            return holder;
        }

        private static System.Windows.Forms.RadioButton Disabled(System.Windows.Forms.RadioButton radio)
        {
            radio.Enabled = false;

            return radio;
        }

        private void AddRadioButtonsSection(PropertyTable table)
        {
            // The columns: selected, not selected, disabled (selected).
            AddUniformRow(table, "CreateStandard",
                Alone(UIStyles.RadioButtons.CreateStandard("Selected", true)),
                Alone(UIStyles.RadioButtons.CreateStandard("Not selected")),
                Alone(Disabled(UIStyles.RadioButtons.CreateStandard("Disabled", true))));

            AddUniformRow(table, "CreatePrimary",
                Alone(UIStyles.RadioButtons.CreatePrimary("Selected", true)),
                Alone(UIStyles.RadioButtons.CreatePrimary("Not selected")),
                Alone(Disabled(UIStyles.RadioButtons.CreatePrimary("Disabled", true))));

            AddUniformRow(table, "CreateStandard(colors: Yellow)",
                Alone(UIStyles.RadioButtons.CreateStandard("Selected", true, UIStyles.Colors.Sets.Yellow)),
                Alone(UIStyles.RadioButtons.CreateStandard("Not selected", false, UIStyles.Colors.Sets.Yellow)),
                Alone(Disabled(UIStyles.RadioButtons.CreateStandard("Disabled", true, UIStyles.Colors.Sets.Yellow))));

            AddUniformRow(table, "CreateStandardCompact",
                Alone(UIStyles.RadioButtons.CreateStandardCompact(true)),
                Alone(UIStyles.RadioButtons.CreateStandardCompact(false)),
                Alone(Disabled(UIStyles.RadioButtons.CreateStandardCompact(true))));

            AddUniformRow(table, "CreatePrimaryCompact",
                Alone(UIStyles.RadioButtons.CreatePrimaryCompact(true)),
                Alone(UIStyles.RadioButtons.CreatePrimaryCompact(false)),
                Alone(Disabled(UIStyles.RadioButtons.CreatePrimaryCompact(true))));

            // CheckAlign puts the box on the right edge (the buttons are as wide as their cell).
            System.Windows.Forms.RadioButton RightHand(string text, bool isChecked, bool enabled)
            {
                System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreateStandard(text, isChecked);
                radio.CheckAlign = ContentAlignment.MiddleRight;
                radio.Width = 150;
                radio.Enabled = enabled;

                return radio;
            }

            AddUniformRow(table, "CheckAlign = MiddleRight",
                Alone(RightHand("Selected", true, true)),
                Alone(RightHand("Not selected", false, true)),
                Alone(RightHand("Disabled", true, false)));

            // One group across several panels: the buttons are in different parents, the RadioGroup makes one choice of them.
            System.Windows.Forms.RadioButton one = UIStyles.RadioButtons.CreatePrimary("One", true);
            System.Windows.Forms.RadioButton two = UIStyles.RadioButtons.CreatePrimary("Two");
            System.Windows.Forms.RadioButton three = UIStyles.RadioButtons.CreatePrimary("Three");
            RadioGroup group = new RadioGroup(one, two, three);
            _radioGroups.Add(group);

            Label chosen = UIStyles.Labels.CreateStandard("Chosen: One");
            group.SelectionChanged += (s, e) => chosen.Text = "Chosen: " + (group.SelectedButton?.Text ?? "none");

            AddUniformRow(table, "RadioGroup (three panels)", Alone(one), Alone(two), Alone(three));
            table.AddRow("RadioGroup.SelectionChanged", chosen);

            // The usual way: buttons that share a parent are one group by themselves - here the group box.
            System.Windows.Forms.GroupBox box = UIStyles.GroupBoxes.CreateStandard("Size");
            box.Size = new Size(190, 118);

            string[] sizes = { "Small", "Medium", "Large" };

            for (int i = 0; i < sizes.Length; i++)
            {
                System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreateStandard(sizes[i], i == 1);
                radio.Location = new Point(14, 26 + i * 28);
                box.Controls.Add(radio);
            }

            System.Windows.Forms.GroupBox second = UIStyles.GroupBoxes.CreatePrimary("Color");
            second.Size = new Size(190, 118);

            string[] colors = { "Red", "Green", "Blue" };

            for (int i = 0; i < colors.Length; i++)
            {
                System.Windows.Forms.RadioButton radio = UIStyles.RadioButtons.CreatePrimary(colors[i], i == 0);
                radio.Location = new Point(14, 26 + i * 28);
                second.Controls.Add(radio);
            }

            table.AddRow("In a group box (one group each)", 128, box, second);
        }
    }
}