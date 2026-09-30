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
    // A single window that instantiates one of every ErikwnkWFUI control so
    // its look (and, for the interactive ones, its behavior) can all be
    // checked in one place instead of hunting through consuming apps. Theme
    // and accent switching rebuild every control from scratch rather than
    // recoloring in place, because several factories only read
    // UIColors/accent at construction time (documented on SetAccent/ApplyTheme
    // themselves) - there's no supported way to retint an already-built
    // control short of recreating it.
    //
    // Laid out with a PropertyTable instead of manually-positioned
    // cards - it's a real ErikwnkWFUI control too (dogfooding it here rather
    // than a one-off layout scheme), it keeps every row's label/editor
    // evenly aligned automatically, and it's exactly the kind of "several
    // controls in a settings-panel-shaped list" layout the Showcase already
    // needed.
    public class ShowcaseForm : StyledForm
    {
        // Gray is here for anyone who just wants a neutral look without
        // knowing CreateStandard already defaults to gray - see
        // UIAccentColors.Gray for why it's close, not pixel-identical.
        //
        // A property, not a field - UIAccentColors.BlackOrWhite resolves
        // live off whichever theme is currently active, and a static
        // readonly field here would freeze it at whatever that was the
        // first time this form was ever built, ignoring later theme
        // switches.
        private static (string Name, Color Color)[] AccentPresets => new (string, Color)[]
        {
            ("Blue", UIAccentColors.Blue),
            ("Red", UIAccentColors.Red),
            ("Orange", UIAccentColors.Orange),
            ("Amber", UIAccentColors.Amber),
            ("Yellow", UIAccentColors.Yellow),
            ("Green", UIAccentColors.Green),
            ("Teal", UIAccentColors.Teal),
            ("Cyan", UIAccentColors.Cyan),
            ("Indigo", UIAccentColors.Indigo),
            ("Purple", UIAccentColors.Purple),
            ("Magenta", UIAccentColors.Magenta),
            ("Pink", UIAccentColors.Pink),
            ("Brown", UIAccentColors.Brown),
            ("Gray", UIAccentColors.Gray),
            ("Black/White", UIAccentColors.BlackOrWhite)
        };

        private Panel _toolbar;
        private Panel _scrollHost;
        private InfoPopupForm _infoPopup;
        private Timer _infoPopupHideTimer;
        private InfoPopupForm _compactInfoPopup;
        private Timer _compactInfoPopupHideTimer;
        private bool _isLightTheme;

        // Lets every ProgressBars-section row (the setters registered by
        // AddProgressBarsSection) actually show what a real, moving load
        // looks like instead of just sitting at one fixed demo percentage -
        // one shared value drives all of them in lockstep. A list of
        // setters rather than a list of controls because ProgressBar.Value
        // and SlimProgressBar.Value aren't behind a common interface;
        // storing "how to apply the current percentage to this specific
        // bar" sidesteps that without needing one. Rebuilt (cleared, then
        // repopulated) on every BuildUi() call alongside the controls
        // themselves, since the old bars get disposed each time a
        // theme/accent switch tears down and recreates the whole UI - an
        // un-cleared list would keep invoking setters that close over
        // disposed controls.
        private readonly List<Action<int>> _progressBarAnimationSetters = new List<Action<int>>();
        private Timer _progressBarAnimationTimer;
        private int _progressBarAnimationPercent;

        public ShowcaseForm()
            : base(StyledFormOptions.CreateStandard("ErikwnkWFUI Showcase"))
        {
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1180, 900);
            MinimumSize = new Size(700, 500);

            _infoPopup = new InfoPopupForm("Info");

            _infoPopupHideTimer = new Timer { Interval = 3000 };
            _infoPopupHideTimer.Tick += delegate
            {
                _infoPopupHideTimer.Stop();
                _infoPopup.Hide();
            };

            // A separate instance from _infoPopup - Compact is meant to be
            // set once and left on for that instance's whole life (see its
            // own doc comment), so the plain-text demo above and this one
            // can't share a single popup.
            _compactInfoPopup = new InfoPopupForm { Compact = true, CompactSize = new Size(70, 32) };

            _compactInfoPopupHideTimer = new Timer { Interval = 3000 };
            _compactInfoPopupHideTimer.Tick += delegate
            {
                _compactInfoPopupHideTimer.Stop();
                _compactInfoPopup.Hide();
            };

            // 1% every 60ms - a full 0->100 sweep takes 6 seconds, slow
            // enough to actually watch rather than just flicker by.
            _progressBarAnimationTimer = new Timer { Interval = 60 };
            _progressBarAnimationTimer.Tick += delegate
            {
                _progressBarAnimationPercent = (_progressBarAnimationPercent + 1) % 101;

                foreach (Action<int> setter in _progressBarAnimationSetters)
                    setter(_progressBarAnimationPercent);
            };
            _progressBarAnimationTimer.Start();

            BuildUi();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_infoPopupHideTimer != null)
                {
                    _infoPopupHideTimer.Stop();
                    _infoPopupHideTimer.Dispose();
                    _infoPopupHideTimer = null;
                }

                if (_progressBarAnimationTimer != null)
                {
                    _progressBarAnimationTimer.Stop();
                    _progressBarAnimationTimer.Dispose();
                    _progressBarAnimationTimer = null;
                }

                if (_infoPopup != null)
                {
                    _infoPopup.Dispose();
                    _infoPopup = null;
                }

                if (_compactInfoPopupHideTimer != null)
                {
                    _compactInfoPopupHideTimer.Stop();
                    _compactInfoPopupHideTimer.Dispose();
                    _compactInfoPopupHideTimer = null;
                }

                if (_compactInfoPopup != null)
                {
                    _compactInfoPopup.Dispose();
                    _compactInfoPopup = null;
                }
            }

            base.Dispose(disposing);
        }

        // Full teardown/rebuild - see the class comment for why this can't
        // just recolor the existing controls in place.
        private void BuildUi()
        {
            // AutoScrollPosition's getter returns the offset negated (a
            // WinForms quirk - the setter expects it positive), and the old
            // _scrollHost is about to be disposed, so this has to be read
            // before teardown and reapplied after rebuild - otherwise every
            // theme/accent switch jumped back to the top, losing whatever
            // section you were actually looking at.
            Point savedScroll = _scrollHost != null
                ? new Point(-_scrollHost.AutoScrollPosition.X, -_scrollHost.AutoScrollPosition.Y)
                : Point.Empty;

            // StyledForm/TitleBarControl only read theme colors once, at
            // construction - this window itself is never recreated (only its
            // content is, which is why the rest of BuildUi exists at all),
            // so without this the title bar stayed on whatever theme was
            // active when the app first launched, regardless of later
            // Dark/Light switches.
            BackColor = UIColors.BackgroundBlack;
            TitleBar.RefreshTheme();

            ContentPanel.SuspendLayout();
            ContentPanel.Controls.Clear();

            if (_toolbar != null)
                _toolbar.Dispose();
            if (_scrollHost != null)
                _scrollHost.Dispose();

            _toolbar = BuildToolbar();
            _scrollHost = UIStyles.Panels.CreateMedium();
            _scrollHost.AutoScroll = true;

            // The bars AddProgressBarsSection is about to (re)create are
            // brand new instances - drop the setters that closed over the
            // just-disposed previous ones before it repopulates this.
            _progressBarAnimationSetters.Clear();

            BuildContent(_scrollHost);

            ContentPanel.Controls.Add(_scrollHost);
            ContentPanel.Controls.Add(_toolbar);
            ContentPanel.ResumeLayout();

            _scrollHost.AutoScrollPosition = savedScroll;
        }

        private Panel BuildToolbar()
        {
            Panel toolbar = UIStyles.Panels.CreateElevated();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 56;
            toolbar.Padding = new Padding(16, 0, 16, 0);

            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateStandard();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = false;
            flow.Margin = new Padding(0);

            Label themeLabel = UIStyles.Labels.CreateNormal("Theme:");
            themeLabel.AutoSize = true;
            themeLabel.Margin = new Padding(0, 18, 8, 0);
            flow.Controls.Add(themeLabel);

            Button darkButton = !_isLightTheme
                ? UIStyles.Buttons.CreatePrimary("Dark")
                : UIStyles.Buttons.CreateStandard("Dark");
            darkButton.Margin = new Padding(0, 10, 6, 0);
            darkButton.Width = 70;
            darkButton.Click += delegate
            {
                _isLightTheme = false;
                UIStyles.Colors.ApplyTheme(UIThemes.Dark);
                BuildUi();
            };
            flow.Controls.Add(darkButton);

            Button lightButton = _isLightTheme
                ? UIStyles.Buttons.CreatePrimary("Light")
                : UIStyles.Buttons.CreateStandard("Light");
            lightButton.Margin = new Padding(0, 10, 24, 0);
            lightButton.Width = 70;
            lightButton.Click += delegate
            {
                _isLightTheme = true;
                UIStyles.Colors.ApplyTheme(UIThemes.Light);
                BuildUi();
            };
            flow.Controls.Add(lightButton);

            Label accentLabel = UIStyles.Labels.CreateNormal("Accent:");
            accentLabel.AutoSize = true;
            accentLabel.Margin = new Padding(0, 18, 8, 0);
            flow.Controls.Add(accentLabel);

            foreach ((string name, Color color) in AccentPresets)
            {
                // The library button (with its tooltip), just filled with
                // the preset's own color.
                Button swatch = UIStyles.Buttons.CreateStandard(string.Empty, name, new Size(28, 28));
                swatch.Margin = new Padding(0, 9, 6, 0);
                swatch.BackColor = color;
                swatch.FlatAppearance.MouseOverBackColor = UIColors.Lighten(color, 20);
                swatch.FlatAppearance.MouseDownBackColor = UIColors.Darken(color, 20);

                swatch.Click += delegate
                {
                    UIStyles.Colors.SetAccent(color);
                    BuildUi();
                };

                flow.Controls.Add(swatch);
            }

            toolbar.Controls.Add(flow);
            return toolbar;
        }

        private void BuildContent(Panel host)
        {
            PropertyTable table = UIStyles.PropertyTables.CreateStandard();
            table.Dock = DockStyle.Top;

            // Alphabetical by section title, so the Showcase's own layout
            // doesn't silently depend on this list's order matching it.
            AddButtonsSection(table);
            AddCheckBoxesSection(table);
            AddComboBoxesSection(table);
            AddContextMenusSection(table);
            AddDataGridSection(table);
            AddLabelsSection(table);
            AddListBoxControlSection(table);
            AddListViewSection(table);
            AddNumericUpDownsSection(table);
            AddPanelsSection(table);
            AddPopupsSection(table);
            AddProgressBarsSection(table);
            AddSliderBarSection(table);
            AddSlimProgressBarsSection(table);
            AddSpinnerSection(table);
            AddTabControlSection(table);
            AddTextBoxesSection(table);
            AddToggleSwitchesSection(table);
            AddVolumeSliderSection(table);

            host.Controls.Add(table);
        }

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
            table.AddSection("Buttons");

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

            Button browse = UIStyles.Buttons.CreateBrowse("Browse", new Size(36, 30));
            Button browseDisabled = UIStyles.Buttons.CreateBrowse("Browse", new Size(36, 30));
            browseDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateBrowse", browse, browseDisabled);
        }

        private void AddCheckBoxesSection(PropertyTable table)
        {
            table.AddSection("CheckBoxes");

            CheckBox checkedBox = UIStyles.CheckBoxes.CreateStandard("Checked", true);
            CheckBox uncheckedBox = UIStyles.CheckBoxes.CreateStandard("Unchecked", false);
            CheckBox disabledBox = UIStyles.CheckBoxes.CreateStandard("Disabled", true);
            disabledBox.Enabled = false;
            AddUniformRow(table, "CreateStandard", checkedBox, uncheckedBox, disabledBox);

            CheckBox compactChecked = UIStyles.CheckBoxes.CreateCompact(true);
            CheckBox compactUnchecked = UIStyles.CheckBoxes.CreateCompact(false);
            CheckBox compactDisabled = UIStyles.CheckBoxes.CreateCompact(true);
            compactDisabled.Enabled = false;
            AddUniformRow(table, "CreateCompact", compactChecked, compactUnchecked, compactDisabled);
        }

        private void AddToggleSwitchesSection(PropertyTable table)
        {
            table.AddSection("ToggleSwitches");

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
            table.AddSection("TextBoxes");

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
            table.AddSection("ComboBoxes");

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
            table.AddSection("ContextMenuStrip");

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
            table.AddSection("NumericUpDowns");

            NumericUpDown numeric = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            NumericUpDown numericDisabled = UIStyles.NumericUpDowns.CreateStandard(0, 100, 1, 42);
            numericDisabled.Enabled = false;
            AddTwoColumnRow(table, "CreateStandard", numeric, numericDisabled);
        }

        private void AddProgressBarsSection(PropertyTable table)
        {
            table.AddSection("ProgressBars");

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
            table.AddSection("SlimProgressBars");

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
            table.AddSection("SliderBar");

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
            table.AddSection("Spinner");

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
            table.AddSection("VolumeSlider");

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

        private void AddLabelsSection(PropertyTable table)
        {
            table.AddSection("Labels");

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
            table.AddSection("Panels (background shades)");

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

        private void AddListBoxControlSection(PropertyTable table)
        {
            table.AddSection("ListBoxControl");

            ListBoxControl listBox = UIStyles.ListBoxControls.CreateStandard(
                "Sample list",
                allowReorder: true,
                showEnumeration: true);
            listBox.Items.Add("First item");
            listBox.Items.Add("Second item");
            listBox.Items.Add("Third item (drag to reorder)");
            listBox.Items.Add("A much longer item whose text wraps onto a second line instead of getting cut off");
            listBox.Items.Add("An even longer item whose text keeps going well past what even two full lines could ever hold, so the second line itself ends up ellipsized instead of overflowing into a third");

            table.AddRow("CreateStandard", 260, listBox);

            ListBoxControl primaryListBox = UIStyles.ListBoxControls.CreatePrimary(
                "Sample list",
                allowReorder: true,
                showEnumeration: true);
            primaryListBox.Items.Add("First item");
            primaryListBox.Items.Add("Second item");
            primaryListBox.Items.Add("Third item (drag to reorder)");
            primaryListBox.Items.Add("A much longer item whose text wraps onto a second line instead of getting cut off");
            primaryListBox.Items.Add("An even longer item whose text keeps going well past what even two full lines could ever hold, so the second line itself ends up ellipsized instead of overflowing into a third");

            table.AddRow("CreatePrimary", 260, primaryListBox);
        }

        private void AddTabControlSection(PropertyTable table)
        {
            table.AddSection("TabControl");

            // The display-only variants: tabs can be switched, not edited.
            table.AddRow(
                "CreateReadOnlyStandard",
                140,
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyStandard(), 3));

            // Two ways to handle more tabs than fit in a row: scroll arrows
            // at the right end of the strip (left), or wrapping onto
            // several rows (right).
            System.Windows.Forms.TabControl multiline = UIStyles.TabControls.CreateReadOnlyPrimary();
            multiline.Multiline = true;
            table.AddRow(
                "CreateReadOnlyPrimary",
                160,
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyPrimary(), 7, "TabControl with overflow arrows"),
                CreateTabControlDemo(multiline, 7, "TabControl with Multiline"));

            // The editable variants: right-click a tab to add a new one
            // behind it or close it (the last tab always stays).
            table.AddRow(
                "CreateStandard",
                140,
                CreateTabControlDemo(UIStyles.TabControls.CreateStandard(), 3, "Right-click a tab to add, rename or close tabs (double-click renames)"));

            // The first tab is locked: it can be neither renamed nor closed.
            Controls.TabControl lockedFirst = UIStyles.TabControls.CreatePrimary();
            CreateTabControlDemo(lockedFirst, 3, "First tab is locked: no rename, no close");
            lockedFirst.SetTabRenameAllowed(lockedFirst.TabPages[0], false);
            lockedFirst.SetTabCloseAllowed(lockedFirst.TabPages[0], false);
            table.AddRow("CreatePrimary", 140, lockedFirst);

            // Everything the tab controls can do, in one tab control: each
            // page of this read-only one shows a group of variants.
            table.AddRow("All variants", OverviewHeight + 20, CreateTabOverviewDemo());
        }

        private static System.Windows.Forms.Control CreateTabOverviewDemo()
        {
            System.Windows.Forms.TabControl overview = UIStyles.TabControls.CreateReadOnlyPrimary();

            AddOverviewPage(
                overview,
                "Read-only",
                "ReadOnlyTabControl: the user can switch tabs, not add, rename or close them. Standard (neutral bar) and Primary (accent bar).",
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyStandard(), 3),
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyPrimary(), 3));

            // Locked first tab, and every new tab starts out in rename mode.
            Controls.TabControl lockedFirst = UIStyles.TabControls.CreatePrimary();
            CreateTabControlDemo(lockedFirst, 3, "First tab is locked, new tabs start in rename mode");
            lockedFirst.SetTabRenameAllowed(lockedFirst.TabPages[0], false);
            lockedFirst.SetTabCloseAllowed(lockedFirst.TabPages[0], false);
            lockedFirst.RenameTabAfterAdding = true;

            AddOverviewPage(
                overview,
                "Editable",
                "TabControl: right-click a tab to add one behind it, rename it (or double-click) or close it. The last tab always stays; the right-click also selects. Right: first tab locked.",
                CreateTabControlDemo(UIStyles.TabControls.CreateStandard(), 3, "Right-click a tab to add, rename or close tabs"),
                lockedFirst);

            System.Windows.Forms.TabControl multiline = UIStyles.TabControls.CreateReadOnlyPrimary();
            multiline.Multiline = true;
            AddOverviewPage(
                overview,
                "Many tabs",
                "More tabs than fit: scroll arrows at the end of the strip (left), or wrapping onto several rows with Multiline (right).",
                CreateTabControlDemo(UIStyles.TabControls.CreateReadOnlyPrimary(), 9, "TabControl with overflow arrows"),
                CreateTabControlDemo(multiline, 9, "TabControl with Multiline"));

            AddOverviewPage(
                overview,
                "Alignment",
                "Alignment = Top, Bottom, Left and Right.",
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Top),
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Bottom),
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Left),
                CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment.Right));

            AddOverviewPage(
                overview,
                "Icons and states",
                "Tab images from an ImageList, HotTrack (hover highlight) and a disabled page (left). Right: AllowSelectingDisabledTabs = false - the disabled tabs cannot be selected, the arrow keys skip them.",
                CreateTabIconsDemo(),
                CreateTabDisabledTabsDemo());

            AddOverviewPage(
                overview,
                "Customization",
                "The customization routes of the standard control still work: DrawMode = OwnerDrawFixed with your own DrawItem (left) and pages with their own BackColor (right).",
                CreateTabOwnerDrawDemo(),
                CreateTabPageColorsDemo());

            // PropertyTable centers a control in its row at the control's own
            // height, it does not stretch it to the row - so the height has
            // to come from the control itself.
            overview.MinimumSize = new System.Drawing.Size(0, OverviewHeight);
            return overview;
        }

        private const int OverviewHeight = 320;

        // One page of the overview: a line of explanation on top, the demos
        // side by side underneath, sharing the width equally.
        private static void AddOverviewPage(
            System.Windows.Forms.TabControl overview,
            string title,
            string description,
            params System.Windows.Forms.Control[] demos)
        {
            System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
            page.Padding = new System.Windows.Forms.Padding(8);

            System.Windows.Forms.TableLayoutPanel grid = UIStyles.TableLayoutPanels.CreateStandard(demos.Length, 2);
            grid.BackColor = UIColors.BackgroundMedium;
            grid.Dock = System.Windows.Forms.DockStyle.Fill;

            for (int i = 0; i < demos.Length; i++)
            {
                grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f / demos.Length));
            }

            grid.RowStyles.Clear();
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100));

            System.Windows.Forms.Label text = UIStyles.Labels.CreateMuted(description);
            text.AutoSize = true;
            text.MaximumSize = new System.Drawing.Size(900, 0);
            text.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            grid.Controls.Add(text, 0, 0);
            grid.SetColumnSpan(text, demos.Length);

            for (int i = 0; i < demos.Length; i++)
            {
                demos[i].Dock = System.Windows.Forms.DockStyle.Fill;
                demos[i].Margin = new System.Windows.Forms.Padding(0, 0, i < demos.Length - 1 ? 8 : 0, 0);
                grid.Controls.Add(demos[i], i, 1);
            }

            page.Controls.Add(grid);
            overview.TabPages.Add(page);
        }

        // Tab images, HotTrack and a disabled page.
        private static System.Windows.Forms.Control CreateTabIconsDemo()
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();
            tabs.HotTrack = true;

            System.Windows.Forms.ImageList images = new System.Windows.Forms.ImageList
            {
                ImageSize = new System.Drawing.Size(16, 16),
                ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit
            };

            System.Collections.Generic.List<System.Drawing.Bitmap> dots = new System.Collections.Generic.List<System.Drawing.Bitmap>();

            foreach (System.Drawing.Color color in new[] { UIColors.GreenLight, UIColors.YellowLight, UIColors.RedLight })
            {
                System.Drawing.Bitmap dot = new System.Drawing.Bitmap(16, 16);

                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(dot))
                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(color))
                {
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    graphics.FillEllipse(brush, 2, 2, 12, 12);
                }

                images.Images.Add(dot);
                dots.Add(dot);
            }

            // The list only copies the images once its handle exists.
            _ = images.Handle;

            foreach (System.Drawing.Bitmap dot in dots)
            {
                dot.Dispose();
            }

            tabs.ImageList = images;
            tabs.Disposed += (sender, e) => images.Dispose();

            string[] titles = { "Done", "Pending", "Failed", "Disabled" };

            for (int i = 0; i < titles.Length; i++)
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(titles[i]);
                page.Controls.Add(CreateTabPageLabel(i < 3 ? "Tab with image " + i : "A disabled page: its content is disabled too"));

                if (i < 3)
                {
                    page.ImageIndex = i;
                }
                else
                {
                    page.Enabled = false;
                }

                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        // Disabled tabs that cannot be selected at all.
        private static System.Windows.Forms.Control CreateTabDisabledTabsDemo()
        {
            Controls.ReadOnlyTabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();
            tabs.AllowSelectingDisabledTabs = false;

            string[] titles = { "One", "Two (disabled)", "Three", "Four (disabled)", "Five" };

            for (int i = 0; i < titles.Length; i++)
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(titles[i]);
                page.Controls.Add(CreateTabPageLabel("Tab " + (i + 1) + ": click the tabs or use the arrow keys"));
                page.Enabled = i % 2 == 0;
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        // DrawMode = OwnerDrawFixed: the DrawItem handler draws every tab.
        private static System.Windows.Forms.Control CreateTabOwnerDrawDemo()
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();
            tabs.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;

            tabs.DrawItem += (sender, e) =>
            {
                bool selected = (e.State & System.Windows.Forms.DrawItemState.Selected) != 0;
                System.Drawing.Color back = selected
                    ? System.Drawing.Color.FromArgb(30, 150, 70)
                    : System.Drawing.Color.FromArgb(70, 30, 30);

                using (System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(back))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                System.Windows.Forms.TextRenderer.DrawText(
                    e.Graphics,
                    tabs.TabPages[e.Index].Text,
                    e.Font,
                    e.Bounds,
                    System.Drawing.Color.White,
                    System.Windows.Forms.TextFormatFlags.HorizontalCenter | System.Windows.Forms.TextFormatFlags.VerticalCenter);
            };

            foreach (string title in new[] { "Overview", "Details", "Settings" })
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
                page.Controls.Add(CreateTabPageLabel("Tabs drawn by DrawItem"));
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        // Pages with their own BackColor keep it; the middle one follows
        // the control's PageBackColor.
        private static System.Windows.Forms.Control CreateTabPageColorsDemo()
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreateReadOnlyPrimary();

            System.Drawing.Color[] colors =
            {
                System.Drawing.Color.FromArgb(90, 30, 30),
                System.Drawing.Color.Empty,
                System.Drawing.Color.FromArgb(20, 80, 50)
            };
            string[] titles = { "Own color", "Default", "Own color 2" };

            for (int i = 0; i < titles.Length; i++)
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(titles[i]);

                if (!colors[i].IsEmpty)
                {
                    page.BackColor = colors[i];
                }

                page.Controls.Add(CreateTabPageLabel(
                    colors[i].IsEmpty ? "No BackColor set: PageBackColor" : "TabPage.BackColor set on this page"));
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        private static System.Windows.Forms.Control CreateTabAlignmentDemo(System.Windows.Forms.TabAlignment alignment)
        {
            System.Windows.Forms.TabControl tabs = UIStyles.TabControls.CreatePrimary();
            tabs.Alignment = alignment;

            foreach (string title in new[] { "Overview", "Details" })
            {
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
                page.Controls.Add(CreateTabPageLabel("Alignment = " + alignment));
                tabs.TabPages.Add(page);
            }

            return tabs;
        }

        private static System.Windows.Forms.Label CreateTabPageLabel(string text)
        {
            System.Windows.Forms.Label label = UIStyles.Labels.CreateNormal(text);
            label.AutoSize = true;
            label.Location = new System.Drawing.Point(10, 10);
            return label;
        }

        private static System.Windows.Forms.Control CreateTabControlDemo(System.Windows.Forms.TabControl tabs, int pageCount, string description = null)
        {
            string[] titles = { "Overview", "Details", "Disabled" };

            for (int i = 0; i < pageCount; i++)
            {
                string title = i < titles.Length ? titles[i] : "Tab number " + (i + 1);
                System.Windows.Forms.TabPage page = new System.Windows.Forms.TabPage(title);
                page.Controls.Add(CreateTabPageLabel(
                    description == null
                        ? "Content of the " + title + " tab"
                        : title + ": " + description));
                tabs.TabPages.Add(page);
            }

            if (pageCount == 3)
            {
                tabs.TabPages[2].Enabled = false;
            }

            return tabs;
        }

        private void AddListViewSection(PropertyTable table)
        {
            table.AddSection("ListView");

            const int demoWidth = 300;

            var standardListView = UIStyles.ListViews.CreateStandard();
            PopulateListViewDemo(standardListView);
            table.AddRow("CreateStandard", 260, UIColumn.Absolute(standardListView, demoWidth));
            PinToDemoWidth(standardListView, demoWidth);

            var primaryListView = UIStyles.ListViews.CreatePrimary();
            PopulateListViewDemo(primaryListView);
            table.AddRow("CreatePrimary", 260, UIColumn.Absolute(primaryListView, demoWidth));
            PinToDemoWidth(primaryListView, demoWidth);
        }

        // AddRow's UIColumn.Absolute only bounds the WRAPPER cell to
        // demoWidth - PropertyTable.ConfigureEditorControl (run as part of
        // AddRow, no special case for ListView) still sets Dock = Fill on
        // the control itself afterward, which stretches it right back out
        // to fill that cell/wrapper rather than actually sizing it to
        // demoWidth. Overriding Dock/Width here, after AddRow has already
        // run, is what actually keeps the control narrow.
        private static void PinToDemoWidth(System.Windows.Forms.Control control, int width)
        {
            control.Dock = DockStyle.None;
            control.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            control.Width = width;

            // Widening a column past this fixed width makes a horizontal
            // scrollbar appear, which shrinks ClientSize.Height - and
            // ListView's own whole-rows-only height snapping (SnapHeight-
            // ToWholeRows) then reacts to THAT by shrinking the control's
            // actual Height too, even though nothing about the demo's own
            // requested size changed. Locking both dimensions here, not
            // just Width, keeps the whole footprint constant regardless of
            // column widths.
            //
            // Deferred one more tick than the fix above needs strictly for
            // Width, so this runs AFTER ListView's own initial whole-rows
            // snap (itself deferred via BeginInvoke from OnHandleCreated)
            // has already settled - capturing Height any earlier would
            // lock in the raw, pre-snap value and immediately fight that
            // snap the moment it actually ran. BeginInvoke needs a real
            // handle to post to - this control may not have one yet at
            // this point in the Showcase's own construction, so that case
            // waits for HandleCreated first.
            void LockCurrentSize()
            {
                var lockedSize = control.Size;
                control.Resize += (sender, e) =>
                {
                    if (control.Size != lockedSize)
                    {
                        control.Size = lockedSize;
                    }
                };
            }

            if (control.IsHandleCreated)
            {
                control.BeginInvoke(new MethodInvoker(LockCurrentSize));
            }
            else
            {
                control.HandleCreated += (sender, e) => control.BeginInvoke(new MethodInvoker(LockCurrentSize));
            }
        }

        private static void PopulateListViewDemo(System.Windows.Forms.ListView listView)
        {
            listView.Columns.Add("Item", 180);
            listView.Columns.Add("Status", 100);

            // PropertyTable centers a row's editor control inside an
            // AutoSize middle row rather than stretching it (see
            // PropertyTable.AddControlToCell) - so the row height passed to
            // AddRow above only changes how much padding surrounds the
            // control, not the control's own size. The control's actual
            // rendered height comes from this Height instead.
            listView.Height = 220;

            // Enough rows to force a vertical scrollbar - only ~7 fit in
            // that height at once, so this doubles as a way to actually
            // exercise drag-select + scroll behavior here instead of only
            // in a consuming app.
            for (int i = 1; i <= 40; i++)
            {
                listView.Items.Add(new ListViewItem(new[] { "Row " + i, i % 2 == 0 ? "OK" : "Pending" }));
            }
        }

        private void AddDataGridSection(PropertyTable table)
        {
            table.AddSection("DataGridView");

            // Demonstrates actual DataSource binding - the one thing a
            // plain ListView (and anything built on it, like
            // ListView) simply cannot do at all. A BindingList<T>, not a
            // DataTable - column-header sorting rewrites the bound list
            // itself (see ReadOnlyDataGridView.ReorderDataSource), which
            // needs an IList data source; a DataTable isn't one, so
            // sorting silently did nothing against it here.
            AddEditableDataGridRow(table, "CreateStandard", UIStyles.DataGridViews.CreateStandard);
            AddEditableDataGridRow(table, "CreatePrimary", UIStyles.DataGridViews.CreatePrimary);

            // The lighter of the two variants above - column-header sorting
            // still works, but no adding/deleting/cutting/pasting rows, so
            // neither the delete-row column nor the "type here to add a
            // row" placeholder the editable rows above demo has anything
            // to show here.
            var readOnlyGrid = UIStyles.DataGridViews.CreateReadOnlyStandard(CreateSampleTracks());
            readOnlyGrid.Dock = DockStyle.Fill;
            NarrowLengthColumn(readOnlyGrid);

            var disabledReadOnlyGrid = UIStyles.DataGridViews.CreateReadOnlyStandard(CreateSampleTracks());
            disabledReadOnlyGrid.Dock = DockStyle.Fill;
            disabledReadOnlyGrid.Enabled = false;
            NarrowLengthColumn(disabledReadOnlyGrid);

            table.AddRow(
                "CreateReadOnlyStandard",
                180,
                UIColumn.Percent(readOnlyGrid, 50),
                UIColumn.Percent(disabledReadOnlyGrid, 50));

            var readOnlyPrimaryGrid = UIStyles.DataGridViews.CreateReadOnlyPrimary(CreateSampleTracks());
            readOnlyPrimaryGrid.Dock = DockStyle.Fill;
            NarrowLengthColumn(readOnlyPrimaryGrid);

            var disabledReadOnlyPrimaryGrid = UIStyles.DataGridViews.CreateReadOnlyPrimary(CreateSampleTracks());
            disabledReadOnlyPrimaryGrid.Dock = DockStyle.Fill;
            disabledReadOnlyPrimaryGrid.Enabled = false;
            NarrowLengthColumn(disabledReadOnlyPrimaryGrid);

            table.AddRow(
                "CreateReadOnlyPrimary",
                180,
                UIColumn.Percent(readOnlyPrimaryGrid, 50),
                UIColumn.Percent(disabledReadOnlyPrimaryGrid, 50));
        }

        // "Length" only ever holds a short "m:ss" string - narrowed so the
        // three grids sharing one row here (each barely a third of the
        // row's own width, unlike the two-up ReadOnly rows below) don't
        // need a horizontal scrollbar just to fit a column whose default
        // auto-generated width is far wider than its content ever needs.
        private static void NarrowLengthColumn(System.Windows.Forms.DataGridView grid)
        {
            if (grid.Columns["Length"] != null)
            {
                grid.Columns["Length"].Width = 55;
            }
        }

        // Two states for both CreateStandard and CreatePrimary - plain
        // (without the "type here to add a row" placeholder) and with the
        // delete-row/enumeration columns (which keeps the placeholder,
        // being the one state actually meant to show off adding as well as
        // deleting rows). No disabled state here - the ReadOnlyStandard/
        // ReadOnlyPrimary rows below already demo one each; a third,
        // editable-but-disabled grid would just be the same thing shown
        // twice. showDeleteColumn/showEnumerationColumn let a caller demo
        // just one of the two pinned columns instead of always both
        // together - CreateStandard's own row only shows the enumeration
        // one, CreatePrimary's shows both coexisting.
        private void AddEditableDataGridRow(
            PropertyTable table,
            string labelText,
            Func<object, System.Windows.Forms.DataGridView> createGrid,
            bool showDeleteColumn = true,
            bool showEnumerationColumn = true)
        {
            var grid = createGrid(CreateSampleTracks());
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            NarrowLengthColumn(grid);

            var gridWithDeleteColumn = (ErikwnkWFUI.Controls.DataGridView)createGrid(CreateSampleTracks());
            gridWithDeleteColumn.Dock = DockStyle.Fill;
            gridWithDeleteColumn.ShowDeleteRowColumn = showDeleteColumn;
            gridWithDeleteColumn.ShowEnumeration = showEnumerationColumn;
            NarrowLengthColumn(gridWithDeleteColumn);

            table.AddRow(
                labelText,
                180,
                UIColumn.Percent(grid, 50),
                UIColumn.Percent(gridWithDeleteColumn, 50));
        }

        // A fresh list each call - CreateStandard/CreateReadOnly bind
        // their own independent DataSource, and a BindingList<T> (unlike
        // DataTable) has no built-in Copy() to hand out separate
        // instances backed by the same starting values instead.
        private static BindingList<SampleTrack> CreateSampleTracks()
        {
            return new BindingList<SampleTrack>
            {
                new SampleTrack { Track = "Sample Song", Artist = "Sample Artist", Length = "3:42" },
                new SampleTrack { Track = "Another Track", Artist = "Someone Else", Length = "4:15" },
                new SampleTrack { Track = "Third One", Artist = "Someone Else", Length = "2:58" },
            };
        }

        private sealed class SampleTrack
        {
            public string Track { get; set; }
            public string Artist { get; set; }
            public string Length { get; set; }
        }

        private void AddPopupsSection(PropertyTable table)
        {
            table.AddSection("Popups (MessageBox / ToastForm / InfoPopupForm)");

            Button messageBoxButton = UIStyles.Buttons.CreateStandard("Show MessageBox", size: new Size(200, 32));
            messageBoxButton.Click += delegate
            {
                MessageBox.Show(
                    "This is a sample MessageBox.",
                    "Sample",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    this);
            };

            Button toastButton = UIStyles.Buttons.CreateStandard("Show ToastForm", size: new Size(160, 32));
            toastButton.Click += delegate
            {
                ToastForm.ShowToast("Sample toast message", this);
            };

            Button infoPopupButton = UIStyles.Buttons.CreateStandard("Show InfoPopupForm", size: new Size(180, 32));
            infoPopupButton.Click += delegate
            {
                _infoPopup.ShowInfo("This is a sample InfoPopupForm.", infoPopupButton);

                // InfoPopupForm is designed as a hover tooltip - real
                // consumers show it on MouseEnter and hide it on MouseLeave.
                // A click-to-preview button has no such pairing, so without
                // this it would just stay open forever; restart the same
                // timer on every click instead of leaking a new one each time.
                _infoPopupHideTimer.Stop();
                _infoPopupHideTimer.Start();
            };

            Button compactInfoPopupButton = UIStyles.Buttons.CreateStandard("Show InfoPopupForm (Compact)", size: new Size(220, 32));
            compactInfoPopupButton.Click += delegate
            {
                // Fixed-size mode meant for a short value like a slider's
                // percentage (see VolumeSlider) - not the free-form text the
                // plain demo above uses.
                _compactInfoPopup.ShowInfo("70%", compactInfoPopupButton);

                _compactInfoPopupHideTimer.Stop();
                _compactInfoPopupHideTimer.Start();
            };

            table.AddRow("MessageBox", messageBoxButton);
            table.AddRow("ToastForm", toastButton);
            table.AddRow("InfoPopupForm", infoPopupButton);
            table.AddRow("InfoPopupForm.Compact", compactInfoPopupButton);
        }
    }
}
