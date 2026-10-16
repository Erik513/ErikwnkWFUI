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

namespace ErikwnkWFUI.Showcase
{
    // Progress bars, sliders and spinners - the ones that show a value or a state.
    public partial class ShowcaseForm
    {
        private void AddProgressBarsSection(PropertyTable table)
        {
            table.AddSection("Progress bars");

            // Every enabled bar below is driven by the shared animation
            // timer (see _progressBarAnimationSetters) instead of a fixed
            // demo value, so this section doubles as a preview of what an
            // actual, moving load looks like. The disabled look is shown
            // once, in the first row, at one fixed value - a constantly
            // changing bar would undercut it, and it is the same in every row.
            ProgressBar grayBar = UIStyles.ProgressBars.CreateStandard();
            AnimateProgressBar(v => grayBar.Value = v);
            ProgressBar grayDisabled = UIStyles.ProgressBars.CreateStandard();
            grayDisabled.Value = 65;
            grayDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", grayBar, grayDisabled);

            AddBarRow(table, "CreatePrimary", UIStyles.ProgressBars.CreatePrimary());
            AddBarRow(table, "CreateStandard(colors: Yellow)", UIStyles.ProgressBars.CreateStandard(UIStyles.Colors.Sets.Yellow));
            AddBarRow(table, "CreateGreen", UIStyles.ProgressBars.CreateGreen());
            AddBarRow(table, "CreateStatus", UIStyles.ProgressBars.CreateStatus());

            // Two properties, independent of each other: the border (BorderStyle) and the track (BackColor; Transparent takes the color of the
            // panel the bar lies on, in both themes). First the border alone, then both.
            ErikwnkWFUI.Controls.ProgressBar withoutBorder = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            withoutBorder.BorderStyle = BorderStyle.None;
            AddBarRow(table, "CreateStandard (BorderStyle = None)", withoutBorder);

            ErikwnkWFUI.Controls.ProgressBar withoutBorderTransparent = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            withoutBorderTransparent.BorderStyle = BorderStyle.None;
            withoutBorderTransparent.BackColor = Color.Transparent;
            AddBarRow(table, "CreateStandard (BorderStyle = None, BackColor = Transparent)", withoutBorderTransparent);

            // The other styles of the standard control: blocks, and the marquee - a block that runs through the bar, for an action of
            // unknown duration (no value; MarqueeAnimationSpeed is the time between two steps).
            ProgressBar blocks = UIStyles.ProgressBars.CreatePrimary();
            blocks.Style = ProgressBarStyle.Blocks;
            AddBarRow(table, "CreatePrimary (Style = Blocks)", blocks);

            ProgressBar marquee = UIStyles.ProgressBars.CreatePrimary();
            marquee.Style = ProgressBarStyle.Marquee;
            marquee.MarqueeAnimationSpeed = 30;
            table.AddRow("CreatePrimary (Style = Marquee)", UIColumn.Percent(marquee, TwoColumnPercent));

            // The value as a percentage on the bar (off by default), and a text for a message about the state; together they are one text.
            ErikwnkWFUI.Controls.ProgressBar percentage = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            percentage.ShowPercentage = true;
            AddBarRow(table, "CreatePrimary (ShowPercentage)", percentage);

            ErikwnkWFUI.Controls.ProgressBar labeled = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard(UIStyles.Colors.Sets.Yellow);
            labeled.Text = "Uploading";
            labeled.ShowPercentage = true;
            AddBarRow(table, "CreateStandard(colors: Yellow) (Text, ShowPercentage)", labeled);

            // Where the text and the percentage stand: the text on the left and the percentage on the right (as in the row above);
            // at the same place they are one text.
            ErikwnkWFUI.Controls.ProgressBar centered = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            centered.Text = "Uploading";
            centered.ShowPercentage = true;
            centered.TextAlign = HorizontalAlignment.Center;
            centered.PercentageAlign = HorizontalAlignment.Center;
            AddBarRow(table, "CreatePrimary (TextAlign = Center, PercentageAlign = Center)", centered);

            ErikwnkWFUI.Controls.ProgressBar right = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            right.Text = "Uploading";
            right.ShowPercentage = true;
            right.TextAlign = HorizontalAlignment.Right;
            right.PercentageAlign = HorizontalAlignment.Right;
            AddBarRow(table, "CreatePrimary (TextAlign = Right, PercentageAlign = Right)", right);

            table.AddSection("Slim progress bars");
            AddSlimProgressBarsSection(table);
        }

        // One animated bar in the column of the enabled ones (the width of the left column of a two column row).
        private void AddBarRow(PropertyTable table, string name, ProgressBar bar)
        {
            AnimateProgressBar(v => bar.Value = v);
            table.AddRow(name, UIColumn.Percent(bar, TwoColumnPercent));
        }

        private void AddSlimBarRow(PropertyTable table, string name, SlimProgressBar bar)
        {
            AnimateProgressBar(v => bar.Value = v);
            table.AddRow(name, UIColumn.Percent(bar, TwoColumnPercent));
        }

        private void AddSlimProgressBarsSection(PropertyTable table)
        {
            SlimProgressBar slimGrayBar = UIStyles.SlimProgressBars.CreateStandard();
            AnimateProgressBar(v => slimGrayBar.Value = v);
            SlimProgressBar slimGrayDisabled = UIStyles.SlimProgressBars.CreateStandard();
            slimGrayDisabled.Value = 65;
            slimGrayDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", slimGrayBar, slimGrayDisabled);

            AddSlimBarRow(table, "CreatePrimary", UIStyles.SlimProgressBars.CreatePrimary());
            AddSlimBarRow(table, "CreateStandard(colors: Yellow)", UIStyles.SlimProgressBars.CreateStandard(UIStyles.Colors.Sets.Yellow));
            AddSlimBarRow(table, "CreateGreen", UIStyles.SlimProgressBars.CreateGreen());
            AddSlimBarRow(table, "CreateStatus", UIStyles.SlimProgressBars.CreateStatus());

            SlimProgressBar slimBlocks = UIStyles.SlimProgressBars.CreatePrimary();
            slimBlocks.Style = ProgressBarStyle.Blocks;
            AddSlimBarRow(table, "CreatePrimary (Style = Blocks)", slimBlocks);

            // A text (a message about the state) and the percentage next to the strip: above it by default, or below, left or right.
            foreach (SlimProgressBarTextPosition position in new[]
            {
                SlimProgressBarTextPosition.Above,
                SlimProgressBarTextPosition.Below,
                SlimProgressBarTextPosition.Left,
                SlimProgressBarTextPosition.Right
            })
            {
                SlimProgressBar labeled = UIStyles.SlimProgressBars.CreatePrimary();
                labeled.Text = "Uploading";
                labeled.ShowPercentage = true;
                labeled.TextPosition = position;
                labeled.AutoSize = true;
                AddSlimBarRow(table, "CreatePrimary (Text, ShowPercentage, TextPosition = " + position + ")", labeled);
            }

            // Above or below, the text is on the left and the percentage on the right; each can be put elsewhere (one place: one text).
            SlimProgressBar slimCentered = UIStyles.SlimProgressBars.CreatePrimary();
            slimCentered.Text = "Uploading";
            slimCentered.ShowPercentage = true;
            slimCentered.TextAlign = HorizontalAlignment.Center;
            slimCentered.PercentageAlign = HorizontalAlignment.Center;
            slimCentered.AutoSize = true;
            AddSlimBarRow(table, "CreatePrimary (TextAlign = Center, PercentageAlign = Center)", slimCentered);

            SlimProgressBar slimMarquee = UIStyles.SlimProgressBars.CreatePrimary();
            slimMarquee.Style = ProgressBarStyle.Marquee;
            slimMarquee.MarqueeAnimationSpeed = 30;
            table.AddRow("CreatePrimary (Style = Marquee)", UIColumn.Percent(slimMarquee, TwoColumnPercent));
        }

        // Value/Enabled are set directly rather than via AnimateProgressBar -
        // a slider represents a user-set position, not a moving load, so a
        // fixed demo value (like the disabled ProgressBar rows use) is the
        // right comparison here, not motion.
        private void AddSliderBarSection(PropertyTable table)
        {
            SliderBar slider = UIStyles.SliderBars.CreateStandard(0.4);
            SliderBar sliderDisabled = UIStyles.SliderBars.CreateStandard(0.65);
            sliderDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", slider, sliderDisabled);

            SliderBar sliderAccent = UIStyles.SliderBars.CreatePrimary(0.4);
            SliderBar sliderAccentDisabled = UIStyles.SliderBars.CreatePrimary(0.65);
            sliderAccentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", sliderAccent, sliderAccentDisabled);
        }

        // The plain rows show a default and a bigger, heavier-stroke one in two
        // 50/50 columns. The CreateProgress* rows are a single spinner whose
        // percentage counts up on the shared animation timer (like the
        // ProgressBar rows) - that's the only way to see CreateProgressStatus
        // shift red -> yellow -> green. The control always draws a centred
        // circle, so a stretched column is fine.
        private void AddSpinnerSection(PropertyTable table)
        {
            AddSpinnerRow(table, "CreateStandard",
                UIStyles.Spinners.CreateStandard(),
                UIStyles.Spinners.CreateStandard(40, 5));

            AddSpinnerRow(table, "CreatePrimary",
                UIStyles.Spinners.CreatePrimary(),
                UIStyles.Spinners.CreatePrimary(40, 5));

            AddSpinnerRow(table, "CreateGreen",
                UIStyles.Spinners.CreateGreen(),
                UIStyles.Spinners.CreateGreen(40, 5));

            AddAnimatedSpinnerRow(table, "CreateProgressStandard",
                UIStyles.Spinners.CreateProgressStandard(40, 4));
            AddAnimatedSpinnerRow(table, "CreateProgressGreen",
                UIStyles.Spinners.CreateProgressGreen(40, 4));
            AddAnimatedSpinnerRow(table, "CreateProgressPrimary",
                UIStyles.Spinners.CreateProgressPrimary(40, 4));
            AddAnimatedSpinnerRow(table, "CreateProgressStatus",
                UIStyles.Spinners.CreateProgressStatus(40, 4));
        }

        private void AddSpinnerRow(PropertyTable table, string label, Spinner a, Spinner b)
        {
            a.Anchor = AnchorStyles.Left;
            b.Anchor = AnchorStyles.Left;
            AddTwoColumnRow(table, label, a, b);
        }

        private void AddAnimatedSpinnerRow(PropertyTable table, string label, Spinner spinner)
        {
            spinner.Anchor = AnchorStyles.Left;
            AnimateProgressBar(v => spinner.Progress = v);
            table.AddRow(label, UIColumn.Percent(spinner, TwoColumnPercent));
        }

        // VolumeSlider adds its own drag-value popup on top of SliderBar -
        // drag the enabled one to see it. No separate "with popup" variant
        // to demo since that popup is the whole point of this control, not
        // an optional extra.
        private void AddVolumeSliderSection(PropertyTable table)
        {
            VolumeSlider volume = UIStyles.VolumeSliders.CreateStandard(0.4);
            VolumeSlider volumeDisabled = UIStyles.VolumeSliders.CreateStandard(0.65);
            volumeDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", volume, volumeDisabled);

            VolumeSlider volumeAccent = UIStyles.VolumeSliders.CreatePrimary(0.4);
            VolumeSlider volumeAccentDisabled = UIStyles.VolumeSliders.CreatePrimary(0.65);
            volumeAccentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", volumeAccent, volumeAccentDisabled);
        }

        // Applies the current animation percentage immediately (so the bar
        // doesn't sit at its Value=0 default until the next timer tick)
        // and registers the setter so future ticks keep it moving.
        private void AnimateProgressBar(Action<int> setValue)
        {
            setValue(_progressBarAnimationPercent);
            _progressBarAnimationSetters.Add(setValue);
        }
    }
}
