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
    // The volume controls.
    public partial class ShowcaseForm
    {
        // The volume control: a mute button and a slider as one. As on the progress bar page the first row also shows the disabled look; the
        // other rows show the variants of the factory, the muted state, another size and the event.
        private void AddVolumeControlSection(PropertyTable table)
        {
            table.AddSection("Volume controls");

            VolumeControl disabled = UIStyles.VolumeControls.CreateStandard(0.65);
            disabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", UIStyles.VolumeControls.CreateStandard(0.4), disabled);
            AddVolumeControlRow(table, "CreatePrimary", UIStyles.VolumeControls.CreatePrimary(0.4));
            AddVolumeControlRow(table, "CreateStandard(colors: Yellow)", UIStyles.VolumeControls.CreateStandard(0.4, UIStyles.Colors.Sets.Yellow));

            table.AddSection("Volume controls: state and size");

            // Muted: the slider stands at 0 and the volume is remembered (60 %): a click on the button brings it back.
            VolumeControl muted = UIStyles.VolumeControls.CreatePrimary(0.6);
            muted.IsMuted = true;
            AddVolumeControlRow(table, "IsMuted = true", muted);

            AddVolumeControlRow(table, "size: 260 x 40", UIStyles.VolumeControls.CreatePrimary(0.6, new Size(260, 40)));

            // What the audio output gets: EffectiveVolume, after each change by the user.
            VolumeControl live = UIStyles.VolumeControls.CreatePrimary(0.6);
            Label shown = UIStyles.Labels.CreateStandard("EffectiveVolume = 60 %");
            shown.AutoSize = true;
            live.VolumeChanged += delegate
            {
                shown.Text = "EffectiveVolume = " + Math.Round(live.EffectiveVolume * 100) + " %";
            };
            table.AddRow("VolumeChanged", UIColumn.Percent(live, TwoColumnPercent), UIColumn.Percent(shown, TwoColumnPercent));
        }

        private void AddVolumeControlRow(PropertyTable table, string label, VolumeControl control)
        {
            table.AddRow(label, UIColumn.Percent(control, TwoColumnPercent));
        }
    }
}
