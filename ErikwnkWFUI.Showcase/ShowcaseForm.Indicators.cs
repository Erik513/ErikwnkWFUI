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

            table.AddSection("Progress bars: border and track");

            // Two properties, independent of each other: the border (BorderStyle) and the track (BackColor; Transparent takes the color of the
            // panel the bar lies on, in both themes). First the border alone, then both.
            ErikwnkWFUI.Controls.ProgressBar withoutBorder = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            withoutBorder.BorderStyle = BorderStyle.None;
            AddBarRow(table, "BorderStyle = None", withoutBorder);

            ErikwnkWFUI.Controls.ProgressBar withoutBorderTransparent = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            withoutBorderTransparent.BorderStyle = BorderStyle.None;
            withoutBorderTransparent.BackColor = Color.Transparent;
            AddBarRow(table, "Border None, Transparent track", withoutBorderTransparent);

            table.AddSection("Progress bars: style");

            // The other styles of the standard control: blocks, and the marquee - a block that runs through the bar, for an action of
            // unknown duration (no value; MarqueeAnimationSpeed is the time between two steps).
            ProgressBar blocks = UIStyles.ProgressBars.CreatePrimary();
            blocks.Style = ProgressBarStyle.Blocks;
            AddBarRow(table, "Style = Blocks", blocks);

            ProgressBar marquee = UIStyles.ProgressBars.CreatePrimary();
            marquee.Style = ProgressBarStyle.Marquee;
            marquee.MarqueeAnimationSpeed = 30;
            table.AddRow("Style = Marquee", UIColumn.Percent(marquee, TwoColumnPercent));

            table.AddSection("Progress bars: text and percentage");

            // Both are off by default. Each alone stands at its own place (the text left, the percentage right) ...
            ErikwnkWFUI.Controls.ProgressBar textOnly = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            textOnly.Text = "Uploading";
            AddBarRow(table, "Text", textOnly);

            ErikwnkWFUI.Controls.ProgressBar percentage = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            percentage.ShowPercentage = true;
            AddBarRow(table, "ShowPercentage", percentage);

            // ... together they are apart as well, and the letters stay readable on the fill and on the track, also on a light fill.
            ErikwnkWFUI.Controls.ProgressBar both = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            both.Text = "Uploading";
            both.ShowPercentage = true;
            AddBarRow(table, "Text + ShowPercentage", both);

            ErikwnkWFUI.Controls.ProgressBar light = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard(UIStyles.Colors.Sets.Yellow);
            light.Text = "Uploading";
            light.ShowPercentage = true;
            AddBarRow(table, "colors: Yellow, Text + ShowPercentage", light);

            // TextAlign and PercentageAlign move the two (Left, Center or Right each); at the same place - "Both aligned" - they are one text.
            ErikwnkWFUI.Controls.ProgressBar centered = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            centered.Text = "Uploading";
            centered.ShowPercentage = true;
            centered.TextAlign = HorizontalAlignment.Center;
            centered.PercentageAlign = HorizontalAlignment.Center;
            AddBarRow(table, "Both aligned Center", centered);

            ErikwnkWFUI.Controls.ProgressBar right = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            right.Text = "Uploading";
            right.ShowPercentage = true;
            right.TextAlign = HorizontalAlignment.Right;
            right.PercentageAlign = HorizontalAlignment.Right;
            AddBarRow(table, "Both aligned Right", right);

            // A text that is longer than the bar is cut with "..." (the percentage keeps its place and stays whole). The bar has a fixed
            // width here, so it is cut whatever the width of the window.
            ErikwnkWFUI.Controls.ProgressBar tooLong = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            tooLong.Text = LongProgressText;
            tooLong.ShowPercentage = true;
            AnimateProgressBar(v => tooLong.Value = v);
            table.AddRow("Text longer than the bar", UIColumn.Absolute(tooLong, LongProgressBarWidth));
            PinToDemoWidth(tooLong, LongProgressBarWidth);

            // TextScroll moves it instead (the same values as for a label); the percentage stays where it is.
            foreach ((string name, LabelScroll scroll) in new[]
            {
                ("TextScroll = BackAndForth", LabelScroll.BackAndForth()),
                ("TextScroll = Endless", LabelScroll.Endless())
            })
            {
                ErikwnkWFUI.Controls.ProgressBar moving = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
                moving.Text = LongProgressText;
                moving.ShowPercentage = true;
                moving.TextScroll = scroll;
                AnimateProgressBar(v => moving.Value = v);
                table.AddRow(name, UIColumn.Absolute(moving, LongProgressBarWidth));
                PinToDemoWidth(moving, LongProgressBarWidth);
            }

            table.AddSection("Slim progress bars");
            AddSlimProgressBarsSection(table);
        }

        private const int LongProgressBarWidth = 300;

        private const string LongProgressText = "Connecting to the server, checking the certificate and loading the first part of the data";

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

            table.AddSection("Slim progress bars: style");

            SlimProgressBar slimBlocks = UIStyles.SlimProgressBars.CreatePrimary();
            slimBlocks.Style = ProgressBarStyle.Blocks;
            AddSlimBarRow(table, "Style = Blocks", slimBlocks);

            SlimProgressBar slimMarquee = UIStyles.SlimProgressBars.CreatePrimary();
            slimMarquee.Style = ProgressBarStyle.Marquee;
            slimMarquee.MarqueeAnimationSpeed = 30;
            table.AddRow("Style = Marquee", UIColumn.Percent(slimMarquee, TwoColumnPercent));

            table.AddSection("Slim progress bars: text and percentage");

            // Both are off by default; without them the bar is only the strip. Each alone stands above the strip (the text left, the
            // percentage right), the strip keeps its thickness and the control grows by the line (AutoSize).
            SlimProgressBar slimTextOnly = UIStyles.SlimProgressBars.CreatePrimary();
            slimTextOnly.Text = "Uploading";
            slimTextOnly.AutoSize = true;
            AddSlimBarRow(table, "Text", slimTextOnly);

            SlimProgressBar slimPercentage = UIStyles.SlimProgressBars.CreatePrimary();
            slimPercentage.ShowPercentage = true;
            slimPercentage.AutoSize = true;
            AddSlimBarRow(table, "ShowPercentage", slimPercentage);

            // Both together (Text + ShowPercentage) next to the strip: above it by default (the text left, the percentage right), or
            // below, left or right of it (at the sides they are one text).
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
                AddSlimBarRow(table, "TextPosition = " + position + (position == SlimProgressBarTextPosition.Above ? " (default)" : ""), labeled);
            }

            // A text that is longer than the control is cut with "..." (the percentage stays whole; at the sides the strip keeps its
            // room), or moves with TextScroll (the same values as for a label).
            foreach ((string name, SlimProgressBarTextPosition position, LabelScroll scroll) in new[]
            {
                ("Text longer than the bar, Above", SlimProgressBarTextPosition.Above, LabelScroll.None),
                ("Text longer than the bar, Right", SlimProgressBarTextPosition.Right, LabelScroll.None),
                ("TextScroll = BackAndForth, Above", SlimProgressBarTextPosition.Above, LabelScroll.BackAndForth()),
                ("TextScroll = Endless, Right", SlimProgressBarTextPosition.Right, LabelScroll.Endless())
            })
            {
                SlimProgressBar tooLong = UIStyles.SlimProgressBars.CreatePrimary();
                tooLong.Text = LongProgressText;
                tooLong.ShowPercentage = true;
                tooLong.TextPosition = position;
                tooLong.TextScroll = scroll;
                tooLong.Height = 22;
                AnimateProgressBar(v => tooLong.Value = v);
                table.AddRow(name, UIColumn.Absolute(tooLong, LongProgressBarWidth));
                PinToDemoWidth(tooLong, LongProgressBarWidth);
            }

            // Above or below, TextAlign and PercentageAlign move the two (Left, Center or Right each; at the same place they are one text).
            SlimProgressBar slimCentered = UIStyles.SlimProgressBars.CreatePrimary();
            slimCentered.Text = "Uploading";
            slimCentered.ShowPercentage = true;
            slimCentered.TextAlign = HorizontalAlignment.Center;
            slimCentered.PercentageAlign = HorizontalAlignment.Center;
            slimCentered.AutoSize = true;
            AddSlimBarRow(table, "Both aligned Center", slimCentered);
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
