using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
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

            // A single color instead of a color set: it is taken as the main color (the button stays neutral).
            AddVolumeControlRow(table, "CreateStandard(colors: MediumPurple)", UIStyles.VolumeControls.CreateStandard(0.4, Color.MediumPurple));

            table.AddSection("Volume controls: state, size and percentage");

            // Muted: the slider stands at 0 and the volume is remembered (60 %): a click on the button brings it back.
            VolumeControl muted = UIStyles.VolumeControls.CreatePrimary(0.6);
            muted.IsMuted = true;
            AddVolumeControlRow(table, "IsMuted = true", muted);

            // (The row is taller than the usual one: the control is 40 px high, and would be cut off at the top and the bottom.)
            table.AddRow("size: 260 x 40", 50, PropertyColumn.Percent(UIStyles.VolumeControls.CreatePrimary(0.6, new Size(260, 40)), TwoColumnPercent));

            // The number beside the slider instead of the popup; the wheel over the button turns the volume too.
            AddVolumeControlRow(table, "showPercentage: true", UIStyles.VolumeControls.CreatePrimary(0.6, showPercentage: true));

            // A player coupled with target:. The control sets it (volume and mute) and follows it when it changes by itself - the button
            // on the right is the player's own key. The label shows what the player holds, and the gain a curve gives the audio output.
            AudioVolumeState player = new AudioVolumeState(0.6);
            VolumeControl coupled = UIStyles.VolumeControls.CreatePrimary(target: player);
            Label held = UIStyles.Labels.CreateStandard();
            held.AutoSize = true;
            Action showPlayer = delegate
            {
                held.Text = "player: " + Math.Round(player.Volume * 100) + " %" + (player.IsMuted ? ", muted" : string.Empty)
                    + " - gain " + Math.Round(player.Gain() * 100) + " %";
            };
            player.VolumeChanged += delegate { showPlayer(); };
            showPlayer();

            Button byItself = UIStyles.Buttons.CreateStandard("Player +10 %", "The player changes its volume by itself", new Size(110, 28));
            byItself.Click += delegate { player.Volume = player.Volume >= 1.0 ? 0.1 : player.Volume + 0.1; };

            System.Windows.Forms.FlowLayoutPanel playerSide = new System.Windows.Forms.FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.LeftToRight
            };
            playerSide.Controls.Add(byItself);
            playerSide.Controls.Add(held);
            held.Margin = new Padding(12, 6, 0, 0);
            table.AddRow("target: player", PropertyColumn.Percent(coupled, TwoColumnPercent), PropertyColumn.Percent(playerSide, TwoColumnPercent));

            // What the audio output gets: EffectiveVolume, after each change by the user.
            VolumeControl live = UIStyles.VolumeControls.CreatePrimary(0.6);
            Label shown = UIStyles.Labels.CreateStandard("EffectiveVolume = 60 %");
            shown.AutoSize = true;
            live.VolumeChanged += delegate
            {
                shown.Text = "EffectiveVolume = " + Math.Round(live.EffectiveVolume * 100) + " %";
            };
            table.AddRow("VolumeChanged", PropertyColumn.Percent(live, TwoColumnPercent), PropertyColumn.Percent(shown, TwoColumnPercent));
        }

        private void AddVolumeControlRow(PropertyTable table, string label, VolumeControl control)
        {
            table.AddRow(label, PropertyColumn.Percent(control, TwoColumnPercent));
        }
    }
}
