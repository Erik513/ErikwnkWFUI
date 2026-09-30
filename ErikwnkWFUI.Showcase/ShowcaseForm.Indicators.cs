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

namespace ErikwnkWFUI.Showcase
{
    // Progress bars, sliders and spinners - the ones that show a value or a state.
    public partial class ShowcaseForm
    {
        private void AddProgressBarsSection(PropertyTable table)
        {
            // Every enabled bar below is driven by the shared animation
            // timer (see _progressBarAnimationSetters) instead of a fixed
            // demo value, so this section doubles as a preview of what an
            // actual, moving load looks like. Disabled bars are deliberately
            // left out of the animation and kept at one fixed value - the
            // point of that column is to show the disabled look clearly,
            // which a constantly-changing bar would undercut.
            ProgressBar greyBar = UIStyles.ProgressBars.CreateStandard();
            AnimateProgressBar(v => greyBar.Value = v);
            ProgressBar greyDisabled = UIStyles.ProgressBars.CreateStandard();
            greyDisabled.Value = 65;
            greyDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", greyBar, greyDisabled);

            ProgressBar greyTransparentBar = UIStyles.ProgressBars.CreateStandardTransparent();
            greyTransparentBar.BackColor = UIColors.BackgroundLight;
            AnimateProgressBar(v => greyTransparentBar.Value = v);
            ProgressBar greyTransparentDisabled = UIStyles.ProgressBars.CreateStandardTransparent();
            greyTransparentDisabled.Value = 40;
            greyTransparentDisabled.BackColor = UIColors.BackgroundLight;
            greyTransparentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandardTransparent", greyTransparentBar, greyTransparentDisabled);

            ProgressBar standardBar = UIStyles.ProgressBars.CreateGreen();
            AnimateProgressBar(v => standardBar.Value = v);
            ProgressBar disabledBar = UIStyles.ProgressBars.CreateGreen();
            disabledBar.Value = 65;
            disabledBar.Enabled = false;
            AddTwoColumnRow(table, "CreateGreen", standardBar, disabledBar);

            ProgressBar transparentBar = UIStyles.ProgressBars.CreateGreenTransparent();
            transparentBar.BackColor = UIColors.BackgroundLight;
            AnimateProgressBar(v => transparentBar.Value = v);
            ProgressBar transparentDisabled = UIStyles.ProgressBars.CreateGreenTransparent();
            transparentDisabled.Value = 40;
            transparentDisabled.BackColor = UIColors.BackgroundLight;
            transparentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateGreenTransparent", transparentBar, transparentDisabled);

            ProgressBar primaryBar = UIStyles.ProgressBars.CreatePrimary();
            AnimateProgressBar(v => primaryBar.Value = v);
            ProgressBar primaryDisabled = UIStyles.ProgressBars.CreatePrimary();
            primaryDisabled.Value = 65;
            primaryDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", primaryBar, primaryDisabled);

            ProgressBar primaryTransparentBar = UIStyles.ProgressBars.CreatePrimaryTransparent();
            primaryTransparentBar.BackColor = UIColors.BackgroundLight;
            AnimateProgressBar(v => primaryTransparentBar.Value = v);
            ProgressBar primaryTransparentDisabled = UIStyles.ProgressBars.CreatePrimaryTransparent();
            primaryTransparentDisabled.Value = 40;
            primaryTransparentDisabled.BackColor = UIColors.BackgroundLight;
            primaryTransparentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimaryTransparent", primaryTransparentBar, primaryTransparentDisabled);

            ProgressBar statusBar = UIStyles.ProgressBars.CreateStatus();
            AnimateProgressBar(v => statusBar.Value = v);
            ProgressBar statusDisabled = UIStyles.ProgressBars.CreateStatus();
            statusDisabled.Value = 65;
            statusDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStatus", statusBar, statusDisabled);

            ProgressBar statusTransparentBar = UIStyles.ProgressBars.CreateStatusTransparent();
            statusTransparentBar.BackColor = UIColors.BackgroundLight;
            AnimateProgressBar(v => statusTransparentBar.Value = v);
            ProgressBar statusTransparentDisabled = UIStyles.ProgressBars.CreateStatusTransparent();
            statusTransparentDisabled.Value = 40;
            statusTransparentDisabled.BackColor = UIColors.BackgroundLight;
            statusTransparentDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStatusTransparent", statusTransparentBar, statusTransparentDisabled);
        }

        private void AddSlimProgressBarsSection(PropertyTable table)
        {
            SlimProgressBar slimGreyBar = UIStyles.SlimProgressBars.CreateStandard();
            AnimateProgressBar(v => slimGreyBar.Value = v);
            SlimProgressBar slimGreyDisabled = UIStyles.SlimProgressBars.CreateStandard();
            slimGreyDisabled.Value = 65;
            slimGreyDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", slimGreyBar, slimGreyDisabled);

            SlimProgressBar slimGreenBar = UIStyles.SlimProgressBars.CreateGreen();
            AnimateProgressBar(v => slimGreenBar.Value = v);
            SlimProgressBar slimGreenDisabled = UIStyles.SlimProgressBars.CreateGreen();
            slimGreenDisabled.Value = 65;
            slimGreenDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateGreen", slimGreenBar, slimGreenDisabled);

            SlimProgressBar slimPrimaryBar = UIStyles.SlimProgressBars.CreatePrimary();
            AnimateProgressBar(v => slimPrimaryBar.Value = v);
            SlimProgressBar slimPrimaryDisabled = UIStyles.SlimProgressBars.CreatePrimary();
            slimPrimaryDisabled.Value = 40;
            slimPrimaryDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreatePrimary", slimPrimaryBar, slimPrimaryDisabled);

            SlimProgressBar slimStatusBar = UIStyles.SlimProgressBars.CreateStatus();
            AnimateProgressBar(v => slimStatusBar.Value = v);
            SlimProgressBar slimStatusDisabled = UIStyles.SlimProgressBars.CreateStatus();
            slimStatusDisabled.Value = 40;
            slimStatusDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStatus", slimStatusBar, slimStatusDisabled);
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

            AddSpinnerRow(table, "CreateGreen",
                UIStyles.Spinners.CreateGreen(),
                UIStyles.Spinners.CreateGreen(40, 5));

            AddSpinnerRow(table, "CreatePrimary",
                UIStyles.Spinners.CreatePrimary(),
                UIStyles.Spinners.CreatePrimary(40, 5));

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
