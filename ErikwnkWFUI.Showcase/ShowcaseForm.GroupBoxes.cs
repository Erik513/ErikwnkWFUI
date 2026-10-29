using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Showcase
{
    // The group boxes: the factory methods, a color set, the colors of the frame and the title, on a colored surface, disabled.
    public partial class ShowcaseForm
    {
        private const int GroupBoxHeight = 84;

        // A group box with a check box in it (so there is something to frame).
        private static System.Windows.Forms.GroupBox Filled(System.Windows.Forms.GroupBox box, bool enabled = true)
        {
            System.Windows.Forms.CheckBox check = UIStyles.CheckBoxes.CreateStandard("An option", true);
            check.Location = new Point(14, 26);
            box.Controls.Add(check);

            System.Windows.Forms.CheckBox other = UIStyles.CheckBoxes.CreateStandard("Another one", false);
            other.Location = new Point(14, 50);
            box.Controls.Add(other);

            box.Size = new Size(190, GroupBoxHeight);
            box.Enabled = enabled;

            return box;
        }

        private void AddGroupBoxesSection(PropertyTable table)
        {
            // The columns: the box, the same disabled.
            table.AddRow("CreateStandard", GroupBoxHeight + 10,
                PropertyColumn.Percent(Filled(UIStyles.GroupBoxes.CreateStandard("Options")), 50f),
                PropertyColumn.Percent(Filled(UIStyles.GroupBoxes.CreateStandard("Disabled"), false), 50f));

            table.AddRow("CreatePrimary", GroupBoxHeight + 10,
                PropertyColumn.Percent(Filled(UIStyles.GroupBoxes.CreatePrimary("Options")), 50f),
                PropertyColumn.Percent(Filled(UIStyles.GroupBoxes.CreatePrimary("Disabled"), false), 50f));

            table.AddRow("CreateStandard(colors: Yellow)", GroupBoxHeight + 10,
                PropertyColumn.Percent(Filled(UIStyles.GroupBoxes.CreateStandard("Options", UIStyles.Colors.Sets.Yellow)), 50f),
                PropertyColumn.Percent(Filled(UIStyles.GroupBoxes.CreateStandard("Disabled", UIStyles.Colors.Sets.Yellow), false), 50f));

            // BorderColor and TitleColor by hand (Color.Empty gives the theme's back).
            System.Windows.Forms.GroupBox colored = Filled(UIStyles.GroupBoxes.CreateStandard("BorderColor, TitleColor"));
            ((ErikwnkWFUI.Controls.GroupBox)colored).BorderColor = Color.OrangeRed;
            ((ErikwnkWFUI.Controls.GroupBox)colored).TitleColor = Color.Orange;
            table.AddRow("BorderColor, TitleColor", GroupBoxHeight + 10, PropertyColumn.Percent(colored, 50f));

            // On a colored surface: the frame is moved until it is seen against it.
            Panel surface = UIStyles.Panels.CreateStandard(UIStyles.Colors.Sets.Green);
            surface.Dock = DockStyle.None;
            surface.Size = new Size(220, GroupBoxHeight + 12);
            // (The title is set to the text color that reads on the panel; the check boxes of the theme would not.)
            ErikwnkWFUI.Controls.GroupBox onColor = (ErikwnkWFUI.Controls.GroupBox)UIStyles.GroupBoxes.CreateStandard("On a green panel");
            onColor.TitleColor = UIColors.GetContrastingForeColor(surface.BackColor);
            onColor.SetBounds(14, 6, 190, GroupBoxHeight - 10);
            surface.Controls.Add(onColor);
            table.AddRow("On a colored panel", GroupBoxHeight + 20, PropertyColumn.Percent(surface, 50f));

            // A long title is cut with an ellipsis.
            System.Windows.Forms.GroupBox longTitle = Filled(UIStyles.GroupBoxes.CreateStandard(
                "A title that is so much longer than the box is wide that it cannot be shown whole and has to end in an ellipsis, however wide the window is"));
            table.AddRow("A long title", GroupBoxHeight + 10, PropertyColumn.Percent(longTitle, 50f));
        }
    }
}