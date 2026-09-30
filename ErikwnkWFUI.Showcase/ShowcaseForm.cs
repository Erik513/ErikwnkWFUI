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
    // Laid out as one tab per control (see ShowcaseForm.Pages): an
    // explanation on top and the variants below, each in a PropertyTable - a
    // real ErikwnkWFUI control too (dogfooding it here rather than a
    // one-off layout scheme) that keeps every row's label/editor evenly
    // aligned. The theme/accent toolbar and the popup buttons belong to no
    // single control and stay outside the tabs.
    public partial class ShowcaseForm : StyledForm
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
        private Panel _popupsHost;
        private Control _pageTabs;
        private int _selectedPageIndex;
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
            Size = new Size(1320, 900);
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
            // The old tabs are about to be disposed, so the page the user
            // was looking at is remembered first - otherwise every
            // theme/accent switch jumped back to the first page.
            if (_pageTabs is System.Windows.Forms.TabControl oldTabs && oldTabs.SelectedIndex >= 0)
                _selectedPageIndex = oldTabs.SelectedIndex;

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
            if (_pageTabs != null)
                _pageTabs.Dispose();
            if (_popupsHost != null)
                _popupsHost.Dispose();

            _toolbar = BuildToolbar();

            // The bars the pages are about to (re)create are brand new
            // instances - drop the setters that closed over the
            // just-disposed previous ones.
            _progressBarAnimationSetters.Clear();

            _pageTabs = BuildPages();
            _popupsHost = BuildPopupsHost();

            // Docked last-added-first: the pages fill what the toolbar (top)
            // and the popups column (right) leave over.
            ContentPanel.Controls.Add(_pageTabs);
            ContentPanel.Controls.Add(_popupsHost);
            ContentPanel.Controls.Add(_toolbar);
            ContentPanel.ResumeLayout();

            ShowSelectedPage();
        }

        // A theme button looks like the theme it switches to (dark stays
        // dark, light stays light) whichever theme is active; the one in use
        // is marked by an accent-colored border instead.
        private static Button CreateThemeButton(string text, UIColorTheme theme, bool selected)
        {
            Button button = UIStyles.Buttons.CreateStandard(text);
            Color hover = UIColors.IsLight(theme.BackgroundMedium)
                ? UIColors.Darken(theme.BackgroundMedium, 12)
                : UIColors.Lighten(theme.BackgroundMedium, 15);

            button.BackColor = theme.BackgroundMedium;
            button.ForeColor = theme.TextPrimary;
            button.FlatAppearance.MouseOverBackColor = hover;
            button.FlatAppearance.MouseDownBackColor = hover;
            button.FlatAppearance.BorderSize = selected ? 2 : 1;
            button.FlatAppearance.BorderColor = selected ? UIColors.PrimaryLight : theme.BorderMedium;

            return button;
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

            Button darkButton = CreateThemeButton("Dark", UIThemes.Dark, !_isLightTheme);
            darkButton.Margin = new Padding(0, 10, 6, 0);
            darkButton.Width = 70;
            darkButton.Click += delegate
            {
                _isLightTheme = false;
                UIStyles.Colors.ApplyTheme(UIThemes.Dark);
                BuildUi();
            };
            flow.Controls.Add(darkButton);

            Button lightButton = CreateThemeButton("Light", UIThemes.Light, _isLightTheme);
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
    }
}
