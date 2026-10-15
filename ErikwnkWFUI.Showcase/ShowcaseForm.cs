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
    // and accent switching happen live: the controls follow by themselves.
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
        private readonly List<InfoPopupForm> _infoPopups = new List<InfoPopupForm>();
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
                DisposeInfoPopups();

                if (_progressBarAnimationTimer != null)
                {
                    _progressBarAnimationTimer.Stop();
                    _progressBarAnimationTimer.Dispose();
                    _progressBarAnimationTimer = null;
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
            ApplyThemeButtonLook(button, theme, selected);

            return button;
        }

        // The button shows the colors of the theme it switches to. They are set again after every change: the library
        // replaces a color that equals its old default, and these are exactly the defaults of the Dark theme (so the Dark
        // button would end up with the fill of the Light theme and the hover of the Dark one).
        private static void ApplyThemeButtonLook(Button button, UIColorTheme theme, bool selected)
        {
            Color hover = UIColors.IsLight(theme.BackgroundMedium)
                ? UIColors.Darken(theme.BackgroundMedium, 12)
                : UIColors.Lighten(theme.BackgroundMedium, 15);

            button.BackColor = theme.BackgroundMedium;
            button.ForeColor = theme.TextPrimary;
            button.FlatAppearance.MouseOverBackColor = hover;
            button.FlatAppearance.MouseDownBackColor = hover;
            button.FlatAppearance.BorderSize = selected ? 2 : 1;
            button.FlatAppearance.BorderColor = selected ? UIColors.PrimaryLight : theme.BorderMedium;
        }

        // Theme and accent change live: every control follows by itself, nothing is rebuilt. Only the marker on the
        // theme buttons (a border in the accent) is the showcase's own.
        private readonly List<Button> _themeButtons = new List<Button>();

        private void MarkSelectedTheme()
        {
            foreach (Button button in _themeButtons)
            {
                UIColorTheme theme = (UIColorTheme)button.Tag;
                bool selected = (theme == UIThemes.Light) == _isLightTheme;

                ApplyThemeButtonLook(button, theme, selected);
            }
        }

        private Panel BuildToolbar()
        {
            _themeButtons.Clear();

            Panel toolbar = UIStyles.Panels.CreateElevated();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 56;
            toolbar.Padding = new Padding(16, 0, 16, 0);

            FlowLayoutPanel flow = UIStyles.FlowLayoutPanels.CreateTransparent();
            flow.Dock = DockStyle.Fill;
            flow.FlowDirection = FlowDirection.LeftToRight;
            flow.WrapContents = false;
            flow.Margin = new Padding(0);

            Label themeLabel = UIStyles.Labels.CreateStandard("Theme:");
            themeLabel.AutoSize = true;
            themeLabel.Margin = new Padding(0, 18, 8, 0);
            flow.Controls.Add(themeLabel);

            Button darkButton = CreateThemeButton("Dark", UIThemes.Dark, !_isLightTheme);
            darkButton.Tag = UIThemes.Dark;
            _themeButtons.Add(darkButton);
            darkButton.Margin = new Padding(0, 10, 6, 0);
            darkButton.Width = 70;
            darkButton.Click += delegate
            {
                _isLightTheme = false;
                UIStyles.Colors.ApplyTheme(UIThemes.Dark);
                MarkSelectedTheme();
            };
            flow.Controls.Add(darkButton);

            Button lightButton = CreateThemeButton("Light", UIThemes.Light, _isLightTheme);
            lightButton.Tag = UIThemes.Light;
            _themeButtons.Add(lightButton);
            lightButton.Margin = new Padding(0, 10, 24, 0);
            lightButton.Width = 70;
            lightButton.Click += delegate
            {
                _isLightTheme = true;
                UIStyles.Colors.ApplyTheme(UIThemes.Light);
                MarkSelectedTheme();
            };
            flow.Controls.Add(lightButton);

            Label accentLabel = UIStyles.Labels.CreateStandard("Accent:");
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
                    MarkSelectedTheme();
                };

                flow.Controls.Add(swatch);
            }

            toolbar.Controls.Add(flow);
            return toolbar;
        }
    }
}
