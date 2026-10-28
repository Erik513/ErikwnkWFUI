using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;

namespace ErikwnkWFUI.Showcase
{
    // The OptionsForm part of the forms tab: ONE options window and the switches that say what it has. Every change closes it and opens it again
    // in the same place. It is not modal, so what the options do (the theme, the accent color, the language, always on top) can be seen on
    // this window while it is open; Cancel and the close button take it back, Save keeps it and shows what SaveClicked can read.
    public partial class ShowcaseForm
    {
        private OptionsForm _optionsForm;
        private ComboBox _optionsLook;
        private ToggleSwitch _optionTheme;
        private ToggleSwitch _optionAccent;
        private ToggleSwitch _optionLanguage;
        private ToggleSwitch _optionTop;
        private ToggleSwitch _optionButtons;
        private ToggleSwitch _optionOwnRows;

        private void AddOptionsFormSection(PropertyTable table)
        {
            CloseOptionsForm();

            table.AddSection("OptionsForm: a settings window with ready-made options (one window, it is reopened in place)");

            Button open = UIStyles.Buttons.CreateStandard("Open", size: new Size(110, 32));
            Button close = UIStyles.Buttons.CreateStandard("Close", size: new Size(110, 32));
            open.Click += (sender, e) => ReopenOptionsForm();
            close.Click += (sender, e) => CloseOptionsForm();
            table.AddRow("Options window", open, close);

            _optionsLook = CreateFormChoice("Standard", "Primary", "colors: Yellow");
            _optionsLook.SelectedIndexChanged += (sender, e) => ReopenOptionsFormIfOpen();
            table.AddRow("Colors", _optionsLook);

            table.AddSection("AddOption(OptionsPreset...)");

            _optionTheme = CreateOptionSwitch();
            _optionAccent = CreateOptionSwitch();
            _optionLanguage = CreateOptionSwitch();
            _optionTop = CreateOptionSwitch();
            table.AddRow("OptionsPreset.Theme", _optionTheme);
            table.AddRow("OptionsPreset.AccentColor", _optionAccent);
            table.AddRow("OptionsPreset.Language", _optionLanguage);
            table.AddRow("OptionsPreset.AlwaysOnTop", _optionTop);

            table.AddSection("OptionsForm");

            _optionButtons = CreateOptionSwitch();
            _optionOwnRows = CreateOptionSwitch();
            table.AddRow("ShowButtons", _optionButtons);
            table.AddRow("Own rows in the table", _optionOwnRows);
        }

        private ToggleSwitch CreateOptionSwitch()
        {
            ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(true);
            toggle.CheckedChanged += (sender, e) => ReopenOptionsFormIfOpen();

            return toggle;
        }

        private void ReopenOptionsFormIfOpen()
        {
            if (_optionsForm != null && !_optionsForm.IsDisposed)
                ReopenOptionsForm();
        }

        private void ReopenOptionsForm()
        {
            // The page does not jump while windows close and open (see ReopenSampleForm).
            ScrollableControl scrollHost = FindScrollHost(_optionsLook);
            Point scrolled = scrollHost == null ? Point.Empty : new Point(-scrollHost.AutoScrollPosition.X, -scrollHost.AutoScrollPosition.Y);
            Point? place = null;

            if (_optionsForm != null && !_optionsForm.IsDisposed)
            {
                place = _optionsForm.Location;
                _optionsForm.Close();
            }

            List<OptionsPreset> presets = new List<OptionsPreset>();

            if (_optionTheme.Checked) presets.Add(OptionsPreset.Theme);
            if (_optionAccent.Checked) presets.Add(OptionsPreset.AccentColor);
            if (_optionLanguage.Checked) presets.Add(OptionsPreset.Language);
            if (_optionTop.Checked) presets.Add(OptionsPreset.AlwaysOnTop);

            OptionsForm form;

            switch ((string)_optionsLook.SelectedItem)
            {
                case "Primary":
                    form = UIStyles.Forms.CreateOptionsPrimary(presets: presets.ToArray());
                    break;
                case "colors: Yellow":
                    form = UIStyles.Forms.CreateOptionsStandard(colors: UIStyles.Colors.Sets.Yellow, presets: presets.ToArray());
                    break;
                default:
                    form = UIStyles.Forms.CreateOptionsStandard(presets: presets.ToArray());
                    break;
            }

            form.ShowButtons = _optionButtons.Checked;

            if (_optionOwnRows.Checked)
            {
                form.PropertyTable.AddSection("Own rows");
                form.PropertyTable.AddRow("Name", UIStyles.TextBoxes.CreateStandard("My app"));
                form.PropertyTable.AddRow("Notifications", UIStyles.ToggleSwitches.CreateStandard());
            }

            // What the program does when the settings are saved: it keeps them (SelectedTheme, SelectedAccentColor, SelectedLanguage and
            // SelectedAlwaysOnTop say what was chosen) and tells the user.
            form.SaveClicked += (sender, e) => UIStyles.Toasts.ShowPrimary("Settings saved", this, icon: ToastIcon.Success);

            form.FitToContent();

            if (place != null)
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = place.Value;
            }

            form.FormClosed += (sender, e) =>
            {
                if (ReferenceEquals(_optionsForm, form))
                    _optionsForm = null;
            };

            _optionsForm = form;
            form.Show(this);
            Activate();

            RestoreScroll(scrollHost, scrolled);

            if (scrollHost != null)
                BeginInvoke(new Action(() => RestoreScroll(scrollHost, scrolled)));
        }

        private void CloseOptionsForm()
        {
            if (_optionsForm != null && !_optionsForm.IsDisposed)
                _optionsForm.Close();

            _optionsForm = null;
        }
    }
}
