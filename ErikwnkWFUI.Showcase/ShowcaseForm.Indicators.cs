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
        private const string LongProgressText = "Connecting to the server, checking the certificate and loading the first part of the data";

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

            table.AddSection("Progress bars: border, track and style");

            // Several variants of one property side by side, in two columns: the label names them from left to right.
            // Border and track are two properties, independent of each other: the border (BorderStyle) and the track (BackColor;
            // Transparent takes the color of the panel the bar lies on, in both themes).
            ErikwnkWFUI.Controls.ProgressBar withoutBorder = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            withoutBorder.BorderStyle = BorderStyle.None;

            ErikwnkWFUI.Controls.ProgressBar withoutBorderTransparent = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            withoutBorderTransparent.BorderStyle = BorderStyle.None;
            withoutBorderTransparent.BackColor = Color.Transparent;

            AddBarPair(table, "BorderStyle None | + Transparent track", withoutBorder, withoutBorderTransparent);

            // Every color on its own: the border (BorderColor), and - on the right - the fill, the track, the border and the text together.
            ErikwnkWFUI.Controls.ProgressBar borderColor = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            borderColor.BorderColor = UIColors.Primary;

            ErikwnkWFUI.Controls.ProgressBar byHand = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
            byHand.ForeColor = Color.FromArgb(230, 120, 30);
            byHand.BackColor = Color.FromArgb(70, 40, 20);
            byHand.BorderColor = Color.FromArgb(230, 120, 30);
            byHand.Text = "By hand";
            byHand.ShowPercentage = true;
            byHand.TextColor = Color.FromArgb(255, 220, 180);
            byHand.TextOnFillColor = Color.Black;

            AddBarPair(table, "BorderColor | all by hand", borderColor, byHand);

            // The other styles of the standard control: blocks, and the marquee - a block that runs through the bar, for an action of
            // unknown duration (no value; MarqueeAnimationSpeed is the time between two steps).
            ProgressBar blocks = UIStyles.ProgressBars.CreatePrimary();
            blocks.Style = ProgressBarStyle.Blocks;

            ProgressBar marquee = UIStyles.ProgressBars.CreatePrimary();
            marquee.Style = ProgressBarStyle.Marquee;
            marquee.MarqueeAnimationSpeed = 30;

            AddBarPair(table, "Style = Blocks | Marquee", blocks, marquee, animateRight: false);

            table.AddSection("Progress bars: text and percentage");

            // Both are off by default. Each alone stands at its own place (the text left, the percentage right) ...
            AddBarPair(
                table,
                "Text | ShowPercentage",
                CreateTextBar(text: true, percentage: false),
                CreateTextBar(text: false, percentage: true));

            // ... together they stand apart as well, and the letters stay readable on the fill and on the track, also on a light one.
            ErikwnkWFUI.Controls.ProgressBar light = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard(UIStyles.Colors.Sets.Yellow);
            light.Text = "Uploading";
            light.ShowPercentage = true;

            AddBarPair(table, "Text + percentage | Yellow set", CreateTextBar(text: true, percentage: true), light);

            // TextAlign and PercentageAlign move the two (Left, Center or Right each); at the same place they are one text.
            ErikwnkWFUI.Controls.ProgressBar centered = CreateTextBar(text: true, percentage: true);
            centered.TextAlign = HorizontalAlignment.Center;
            centered.PercentageAlign = HorizontalAlignment.Center;

            ErikwnkWFUI.Controls.ProgressBar right = CreateTextBar(text: true, percentage: true);
            right.TextAlign = HorizontalAlignment.Right;
            right.PercentageAlign = HorizontalAlignment.Right;

            AddBarPair(table, "Both aligned: Center | Right", centered, right);

            // The color of the text on the track (TextColor) and on the fill (TextOnFillColor); automatic where they are not set.
            ErikwnkWFUI.Controls.ProgressBar textColor = CreateTextBar(text: true, percentage: true);
            textColor.TextColor = Color.FromArgb(120, 200, 255);

            ErikwnkWFUI.Controls.ProgressBar textOnFill = CreateTextBar(text: true, percentage: true);
            textOnFill.TextColor = Color.FromArgb(120, 200, 255);
            textOnFill.TextOnFillColor = Color.Black;

            AddBarPair(table, "TextColor | + TextOnFillColor", textColor, textOnFill);

            // A text that is longer than the bar is cut with an ellipsis (the percentage stays whole), or moves with TextScroll
            // (the same values as for a label; the percentage stays where it is).
            AddBarPair(
                table,
                "Long text: cut | BackAndForth",
                CreateLongTextBar(LabelScroll.None),
                CreateLongTextBar(LabelScroll.BackAndForth()));

            AddBarPair(
                table,
                "Endless: Left | Right",
                CreateLongTextBar(LabelScroll.Endless()),
                CreateLongTextBar(LabelScroll.Endless(LabelScrollDirection.Right)));

            table.AddSection("Slim progress bars");
            AddSlimProgressBarsSection(table);
        }

        private static ErikwnkWFUI.Controls.ProgressBar CreateTextBar(bool text, bool percentage)
        {
            ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();

            if (text)
                bar.Text = "Uploading";

            bar.ShowPercentage = percentage;

            return bar;
        }

        private static ErikwnkWFUI.Controls.ProgressBar CreateLongTextBar(LabelScroll scroll)
        {
            ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
            bar.Text = LongProgressText;
            bar.ShowPercentage = true;
            bar.TextScroll = scroll;

            return bar;
        }

        // One animated bar in the column of the enabled ones (the width of the left column of a two column row).
        private void AddBarRow(PropertyTable table, string name, ProgressBar bar)
        {
            AnimateProgressBar(v => bar.Value = v);
            table.AddRow(name, UIColumn.Percent(bar, TwoColumnPercent));
        }

        // Two animated bars side by side (the left and the right half of the row).
        private void AddBarPair(PropertyTable table, string name, ProgressBar left, ProgressBar right, bool animateRight = true)
        {
            AnimateProgressBar(v => left.Value = v);

            if (animateRight)
                AnimateProgressBar(v => right.Value = v);

            table.AddRow(name, UIColumn.Percent(left, TwoColumnPercent), UIColumn.Percent(right, TwoColumnPercent));
        }

        private void AddSlimBarRow(PropertyTable table, string name, SlimProgressBar bar)
        {
            AnimateProgressBar(v => bar.Value = v);
            table.AddRow(name, UIColumn.Percent(bar, TwoColumnPercent));
        }

        private void AddSlimBarPair(PropertyTable table, string name, SlimProgressBar left, SlimProgressBar right, bool animateRight = true)
        {
            AnimateProgressBar(v => left.Value = v);

            if (animateRight)
                AnimateProgressBar(v => right.Value = v);

            table.AddRow(name, UIColumn.Percent(left, TwoColumnPercent), UIColumn.Percent(right, TwoColumnPercent));
        }

        private static SlimProgressBar CreateSlimTextBar(bool text, bool percentage, SlimProgressBarTextPosition position = SlimProgressBarTextPosition.Above)
        {
            SlimProgressBar bar = UIStyles.SlimProgressBars.CreatePrimary();

            if (text)
                bar.Text = "Uploading";

            bar.ShowPercentage = percentage;
            bar.TextPosition = position;
            bar.AutoSize = true;

            return bar;
        }

        private static SlimProgressBar CreateSlimLongTextBar(SlimProgressBarTextPosition position, LabelScroll scroll)
        {
            SlimProgressBar bar = CreateSlimTextBar(text: false, percentage: true, position);
            bar.Text = LongProgressText;
            bar.TextScroll = scroll;

            return bar;
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

            SlimProgressBar slimMarquee = UIStyles.SlimProgressBars.CreatePrimary();
            slimMarquee.Style = ProgressBarStyle.Marquee;
            slimMarquee.MarqueeAnimationSpeed = 30;

            AddSlimBarPair(table, "Style = Blocks | Marquee", slimBlocks, slimMarquee, animateRight: false);

            table.AddSection("Slim progress bars: text and percentage");

            // Both are off by default; without them the bar is only the strip. Each alone stands above the strip (the text left, the
            // percentage right), the strip keeps its thickness and the control grows by the line (AutoSize).
            AddSlimBarPair(
                table,
                "Text | ShowPercentage",
                CreateSlimTextBar(text: true, percentage: false),
                CreateSlimTextBar(text: false, percentage: true));

            // Both together next to the strip: above it by default (the text left, the percentage right), or below, left or right of it
            // (at the sides they are one text).
            AddSlimBarPair(
                table,
                "TextPosition Above | Below",
                CreateSlimTextBar(text: true, percentage: true, SlimProgressBarTextPosition.Above),
                CreateSlimTextBar(text: true, percentage: true, SlimProgressBarTextPosition.Below));

            AddSlimBarPair(
                table,
                "TextPosition Left | Right",
                CreateSlimTextBar(text: true, percentage: true, SlimProgressBarTextPosition.Left),
                CreateSlimTextBar(text: true, percentage: true, SlimProgressBarTextPosition.Right));

            // Above or below, TextAlign and PercentageAlign move the two (Left, Center or Right each; at the same place they are one text).
            SlimProgressBar slimCentered = CreateSlimTextBar(text: true, percentage: true);
            slimCentered.TextAlign = HorizontalAlignment.Center;
            slimCentered.PercentageAlign = HorizontalAlignment.Center;

            SlimProgressBar slimRight = CreateSlimTextBar(text: true, percentage: true);
            slimRight.TextAlign = HorizontalAlignment.Right;
            slimRight.PercentageAlign = HorizontalAlignment.Right;

            AddSlimBarPair(table, "Both aligned: Center | Right", slimCentered, slimRight);

            // The color of the text next to the strip (TextColor), and all three colors by hand: fill, track and text.
            SlimProgressBar slimTextColor = CreateSlimTextBar(text: true, percentage: true);
            slimTextColor.TextColor = Color.FromArgb(120, 200, 255);

            SlimProgressBar slimByHand = CreateSlimTextBar(text: true, percentage: true);
            slimByHand.ForeColor = Color.FromArgb(230, 120, 30);
            slimByHand.BackColor = Color.FromArgb(120, 70, 30);
            slimByHand.TextColor = Color.FromArgb(255, 200, 150);

            AddSlimBarPair(table, "TextColor | all by hand", slimTextColor, slimByHand);

            // A text that is longer than the control is cut with an ellipsis (the percentage stays whole; at the sides the strip keeps its
            // room), or moves with TextScroll (the same values as for a label).
            AddSlimBarPair(
                table,
                "Above, long: cut | BackAndForth",
                CreateSlimLongTextBar(SlimProgressBarTextPosition.Above, LabelScroll.None),
                CreateSlimLongTextBar(SlimProgressBarTextPosition.Above, LabelScroll.BackAndForth()));

            AddSlimBarPair(
                table,
                "Right, long: cut | Endless",
                CreateSlimLongTextBar(SlimProgressBarTextPosition.Right, LabelScroll.None),
                CreateSlimLongTextBar(SlimProgressBarTextPosition.Right, LabelScroll.Endless()));
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

        // Like the progress bars: one row for each factory method, the variants of a property side by side in two columns (the label
        // names them from left to right). The plain rows show a default and a larger, heavier ring; the progress rows a ring of 40 and
        // of 48 px (smaller rings leave the digits out). The status color and the percentage need a load that counts up, so those spinners
        // run on the shared animation timer (like the ProgressBar rows). The control always draws a centred circle, so a stretched
        // column is fine.
        private void AddSpinnerSection(PropertyTable table)
        {
            table.AddSection("Spinners (size: 24 (default) | size: 40, thickness: 5)");

            AddSpinnerRow(table, "CreateStandard",
                UIStyles.Spinners.CreateStandard(),
                UIStyles.Spinners.CreateStandard(40, 5));
            AddSpinnerRow(table, "CreatePrimary",
                UIStyles.Spinners.CreatePrimary(),
                UIStyles.Spinners.CreatePrimary(40, 5));
            AddSpinnerRow(table, "CreateStandard(colors: Yellow)",
                UIStyles.Spinners.CreateStandard(24, 0, UIStyles.Colors.Sets.Yellow),
                UIStyles.Spinners.CreateStandard(40, 5, UIStyles.Colors.Sets.Yellow));
            AddSpinnerRow(table, "CreateGreen",
                UIStyles.Spinners.CreateGreen(),
                UIStyles.Spinners.CreateGreen(40, 5));
            AddSpinnerRow(table, "CreateStatus",
                UIStyles.Spinners.CreateStatus(),
                UIStyles.Spinners.CreateStatus(40, 5), animate: true);

            table.AddSection("Spinners: progress (size: 40 (default) | size: 48, thickness: 5)");

            AddSpinnerRow(table, "CreateProgressStandard",
                UIStyles.Spinners.CreateProgressStandard(),
                UIStyles.Spinners.CreateProgressStandard(48, 5), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressPrimary",
                UIStyles.Spinners.CreateProgressPrimary(),
                UIStyles.Spinners.CreateProgressPrimary(48, 5), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressStandard(colors: Yellow)",
                UIStyles.Spinners.CreateProgressStandard(40, 0, UIStyles.Colors.Sets.Yellow),
                UIStyles.Spinners.CreateProgressStandard(48, 5, UIStyles.Colors.Sets.Yellow), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressGreen",
                UIStyles.Spinners.CreateProgressGreen(),
                UIStyles.Spinners.CreateProgressGreen(48, 5), animate: true, rowHeight: 56);
            AddSpinnerRow(table, "CreateProgressStatus",
                UIStyles.Spinners.CreateProgressStatus(),
                UIStyles.Spinners.CreateProgressStatus(48, 5), animate: true, rowHeight: 56);

            table.AddSection("Spinners: states, text and speed");

            Spinner stopped = UIStyles.Spinners.CreatePrimary(40, 5);
            stopped.IsSpinning = false;
            Spinner disabled = UIStyles.Spinners.CreatePrimary(40, 5);
            disabled.Enabled = false;
            AddSpinnerRow(table, "IsSpinning = false | Enabled = false", stopped, disabled);

            Spinner slow = UIStyles.Spinners.CreateGreen(40, 5);
            slow.Speed = SpinnerSpeed.Slow;
            Spinner medium = UIStyles.Spinners.CreateGreen(40, 5);
            Spinner fast = UIStyles.Spinners.CreateGreen(40, 5);
            fast.Speed = SpinnerSpeed.Fast;
            slow.Anchor = medium.Anchor = fast.Anchor = AnchorStyles.Left;
            table.AddRow("Speed = Slow | Medium | Fast",
                UIColumn.Percent(slow, 100f / 3),
                UIColumn.Percent(medium, 100f / 3),
                UIColumn.Percent(fast, 100f / 3));

            Spinner determinate = UIStyles.Spinners.CreateProgressPrimary();
            determinate.IsIndeterminate = false;
            Spinner determinateStatus = UIStyles.Spinners.CreateProgressStatus();
            determinateStatus.IsIndeterminate = false;
            AddSpinnerRow(table, "IsIndeterminate = false", determinate, determinateStatus, animate: true);

            Spinner right = UIStyles.Spinners.CreatePrimary(32);
            right.Text = "Loading data...";
            right.Width = 170;
            Spinner below = UIStyles.Spinners.CreatePrimary(60);
            below.Text = "Please wait";
            below.TextPosition = SpinnerTextPosition.Below;
            below.Width = 120;
            below.Height = 84;
            AddSpinnerRow(table, "Text | TextPosition = Below", right, below, rowHeight: 96);
        }

        private void AddSpinnerRow(PropertyTable table, string label, Spinner a, Spinner b, bool animate = false, int rowHeight = 0)
        {
            a.Anchor = AnchorStyles.Left;
            b.Anchor = AnchorStyles.Left;

            if (animate)
            {
                AnimateProgressBar(v => a.Value = v);
                AnimateProgressBar(v => b.Value = v);
            }

            UIColumn left = UIColumn.Percent(a, TwoColumnPercent);
            UIColumn rightColumn = UIColumn.Percent(b, TwoColumnPercent);

            if (rowHeight > 0)
                table.AddRow(label, rowHeight, left, rightColumn);
            else
                table.AddRow(label, left, rightColumn);
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
