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
using ProgressBar = System.Windows.Forms.ProgressBar;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Showcase
{
    // The track bars.
    public partial class ShowcaseForm
    {
        // The track bar page: the sliders, and below them the volume control that is built from one.
        private void AddTrackBarPage(PropertyTable table)
        {
            AddTrackBarSection(table);
            AddVolumeControlSection(table);
        }

        // Value/Enabled are set directly rather than via AnimateProgressBar - a slider represents a user-set position, not a moving
        // load, so a fixed demo value is the right comparison here, not motion. As on the progress bar page, the first row also shows
        // the disabled look (right).
        private void AddTrackBarSection(PropertyTable table)
        {
            table.AddSection("Track bars");

            TrackBar disabled = UIStyles.TrackBars.CreateStandard(0.65);
            disabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", UIStyles.TrackBars.CreateStandard(0.4), disabled);
            AddTrackBarRow(table, "CreatePrimary", UIStyles.TrackBars.CreatePrimary(0.4));
            AddTrackBarRow(table, "CreateStandard(colors: Yellow)", UIStyles.TrackBars.CreateStandard(0.4, 1.0, UIStyles.Colors.Sets.Yellow));

            // A single color instead of a color set: it is taken as the main color.
            AddTrackBarRow(table, "CreateStandard(colors: MediumPurple)", UIStyles.TrackBars.CreateStandard(0.4, 1.0, Color.MediumPurple));

            table.AddSection("Track bars: range and value popup");

            // One slider each: ShowValuePopup writes the value above the thumb while the mouse is over it or it is dragged (hover over it,
            // drag it or use the keys to see it); the second one has its own range and writes its value with FormatValue. Both are
            // usable.
            TrackBar popup = UIStyles.TrackBars.CreatePrimary(40, 100, showValuePopup: true);
            AddTrackBarRow(table, "ShowValuePopup = true", popup);

            // The popup has the colors of the slider (here a color set) ...
            AddTrackBarRow(table, "ShowValuePopup, colors: Yellow",
                UIStyles.TrackBars.CreateStandard(40, 100, UIStyles.Colors.Sets.Yellow, showValuePopup: true));

            // ... unless it is given colors of its own.
            AddTrackBarRow(table, "ValuePopupColors: Green",
                UIStyles.TrackBars.CreateStandard(40, 100, showValuePopup: true, popupColors: UIStyles.Colors.Sets.Green));

            TrackBar range = UIStyles.TrackBars.CreatePrimary(3, 12);
            range.Minimum = -12;
            range.ShowValuePopup = true;
            range.SmallChange = 1;
            range.LargeChange = 6;
            range.FormatValue = value => (value > 0 ? "+" : string.Empty) + Math.Round(value) + " dB";
            AddTrackBarRow(table, "Minimum = -12, FormatValue", range);
        }

        private void AddTrackBarRow(PropertyTable table, string label, TrackBar bar)
        {
            table.AddRow(label, UIColumn.Percent(bar, TwoColumnPercent));
        }
    }
}
