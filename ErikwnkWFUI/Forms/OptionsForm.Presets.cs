using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;

namespace ErikwnkWFUI.Forms
{
    // The ready-made options (OptionsPreset): the rows, how they are applied as they are chosen, and how they are taken back.
    public partial class OptionsForm
    {
        private static readonly string[] AccentNames =
        {
            "Blue", "Red", "Orange", "Amber", "Yellow", "Green", "Teal", "Cyan", "Indigo", "Purple", "Magenta", "Pink", "Brown", "Gray", "BlackOrWhite"
        };

        private readonly Dictionary<OptionsPreset, PropertyRow> _presetRows = new Dictionary<OptionsPreset, PropertyRow>();
        private PropertyRow _optionsSection;
        private ComboBox _themeBox;
        private ComboBox _accentBox;
        private ComboBox _languageBox;
        private ToggleSwitch _topMostSwitch;
        private ToggleSwitch _startSwitch;
        private Button _resetButton;
        private bool _originalStartWithWindows;

        // How a toast is shown (the message and the window it stands over); the seam of the tests.
        internal Action<string, Form> ShowToast = (message, owner) => UIStyles.Toasts.ShowPrimary(message, owner, icon: ToastIcon.Success);
        private bool _loadingOptions;
        private bool _optionsChanged;

        // What was applied when the window was made, to give it back when the window is closed without Save.
        private UIColorTheme _originalTheme;
        private Color _originalAccent;
        private UILanguage _originalLanguage;
        private AppLanguage _originalAppLanguage;
        private Form _topMostTarget;
        private bool _topMostTouched;
        private bool _originalTopMost;

        /// <summary>The theme that is applied now (dark or light).</summary>
        public UIColorTheme SelectedTheme
        {
            get { return UIColors.IsLight(UIColors.BackgroundMedium) ? UIThemes.Light : UIThemes.Dark; }
        }

        /// <summary>The accent color that is applied now.</summary>
        public Color SelectedAccentColor
        {
            get { return UIColors.Primary; }
        }

        /// <summary>The language of the library that is applied now.</summary>
        public UILanguage SelectedLanguage
        {
            get { return UIStrings.Language; }
        }

        /// <summary>Whether the program is to start with Windows: what the switch says (written when the user saves), or - without that option - what Windows has.</summary>
        public bool SelectedStartWithWindows
        {
            get { return _startSwitch != null ? _startSwitch.Checked : WindowsAutoStart.IsEnabled(); }
        }

        /// <summary>
        /// Arguments the program is started with when it starts with Windows (null: none), so it can tell that it was not started by the user
        /// - to stay in the tray, say. Used when <see cref="OptionsPreset.StartWithWindows"/> is saved.
        /// </summary>
        public string StartWithWindowsArguments { get; set; }

        /// <summary>
        /// Whether the window tells the user by a toast, in the language of the library, that the settings were saved ("Settings saved", over the
        /// window that owns this one - none without an owner) and that the defaults were restored (over this window). The default.
        /// </summary>
        public bool ShowToasts { get; set; } = true;

        /// <summary>Raised after the button of <see cref="OptionsPreset.ResetToDefaults"/> has put the options of the library back: reset your own rows here.</summary>
        public event EventHandler DefaultsRestored;

        /// <summary>Whether the window that owns this one (the main window) is always on top now.</summary>
        public bool SelectedAlwaysOnTop
        {
            get
            {
                Form target = ResolveTopMostTarget();

                return target != null && target.TopMost;
            }
        }

        /// <summary>
        /// Puts a ready-made option into the table (under the section "General", which is made with the first one): the row, its choice, and
        /// what applying it does. The program keeps the result where it keeps its settings: read <see cref="SelectedTheme"/>,
        /// <see cref="SelectedAccentColor"/>, <see cref="SelectedLanguage"/> and <see cref="SelectedAlwaysOnTop"/> in <see cref="SaveClicked"/>.
        /// An option that is there already is not added twice: its row is returned.
        /// </summary>
        public PropertyRow AddOption(OptionsPreset preset)
        {
            PropertyRow existing;

            if (_presetRows.TryGetValue(preset, out existing))
                return existing;

            if (_optionsSection == null)
                _optionsSection = _propertyTable.AddSection(UIStrings.Get("OptionsForm.General"));

            PropertyRow row;

            switch (preset)
            {
                case OptionsPreset.Theme:
                    _themeBox = CreateOptionChoice();
                    row = _propertyTable.AddRow(GetOptionText(preset), _themeBox);
                    _themeBox.SelectedIndexChanged += (s, e) => ApplyOption(() => UIColors.ApplyTheme(_themeBox.SelectedIndex == 1 ? UIThemes.Light : UIThemes.Dark));
                    break;

                case OptionsPreset.AccentColor:
                    _accentBox = CreateOptionChoice();
                    row = _propertyTable.AddRow(GetOptionText(preset), _accentBox);
                    _accentBox.SelectedIndexChanged += (s, e) =>
                    {
                        if (_accentBox.SelectedIndex >= 0)
                            ApplyOption(() => UIColors.SetAccent(UIAccentColors.All[_accentBox.SelectedIndex]));
                    };
                    break;

                case OptionsPreset.Language:
                    _languageBox = CreateOptionChoice();
                    row = _propertyTable.AddRow(GetOptionText(preset), _languageBox);
                    _languageBox.SelectedIndexChanged += (s, e) => ApplyOption(() => UIStyles.SetLanguage(_languageBox.SelectedIndex == 1 ? AppLanguage.German : AppLanguage.English));
                    break;

                case OptionsPreset.StartWithWindows:
                    _originalStartWithWindows = WindowsAutoStart.IsEnabled();
                    _startSwitch = UIStyles.ToggleSwitches.CreateStandard(_originalStartWithWindows);

                    // Written when the user saves; without the buttons there is no save, so at once.
                    _startSwitch.CheckedChanged += (s, e) =>
                    {
                        if (!_showButtons && !_loadingOptions)
                            WindowsAutoStart.Set(_startSwitch.Checked, StartWithWindowsArguments);
                    };
                    row = _propertyTable.AddRow(GetOptionText(preset), _startSwitch);
                    break;

                case OptionsPreset.ResetToDefaults:
                    _resetButton = UIStyles.Buttons.CreateStandard(UIStrings.Get("OptionsForm.ResetToDefaults"));
                    _resetButton.AutoSize = true;
                    _resetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                    _resetButton.Click += (s, e) => RestoreDefaults();
                    row = _propertyTable.AddRow(GetOptionText(preset), _resetButton);
                    break;

                default:
                    _topMostSwitch = UIStyles.ToggleSwitches.CreateStandard(false);
                    row = _propertyTable.AddRow(GetOptionText(preset), _topMostSwitch);
                    _topMostSwitch.CheckedChanged += (s, e) => ApplyOption(ApplyAlwaysOnTop);
                    SyncAlwaysOnTopSwitch();
                    break;
            }

            _presetRows[preset] = row;
            FillOptionItems();

            return row;
        }

        /// <summary>Puts several ready-made options into the table, in this order.</summary>
        public void AddOptions(params OptionsPreset[] presets)
        {
            foreach (OptionsPreset preset in presets)
                AddOption(preset);
        }

        // ---- applying

        private void RememberOriginalSettings()
        {
            _originalTheme = SelectedTheme;
            _originalAccent = UIColors.Primary;
            _originalLanguage = UIStrings.Language;
            _originalAppLanguage = AppLocalization.Language;
        }

        // A change of a choice: applied at once - except while the items are being filled.
        private void ApplyOption(Action apply)
        {
            if (_loadingOptions)
                return;

            _optionsChanged = true;
            apply();
        }

        private Form ResolveTopMostTarget()
        {
            return _topMostTarget ?? Owner ?? Application.OpenForms.Cast<Form>().FirstOrDefault(form => form != this && !form.IsDisposed);
        }

        private void ApplyAlwaysOnTop()
        {
            Form target = ResolveTopMostTarget();

            if (target == null)
                return;

            if (!_topMostTouched)
            {
                _topMostTarget = target;
                _originalTopMost = target.TopMost;
                _topMostTouched = true;
            }

            target.TopMost = _topMostSwitch.Checked;
        }

        // The switch shows whether the window it acts on is on top already.
        private void SyncAlwaysOnTopSwitch()
        {
            if (_topMostSwitch == null)
                return;

            Form target = ResolveTopMostTarget();
            bool wasLoading = _loadingOptions;
            _loadingOptions = true;

            try
            {
                _topMostSwitch.Checked = target != null && target.TopMost;
            }
            finally
            {
                _loadingOptions = wasLoading;
            }
        }

        // What the user chose for Windows is written when the user saves: only when it differs from what Windows has now.
        private void ApplyStartWithWindows()
        {
            if (_startSwitch != null && _startSwitch.Checked != _originalStartWithWindows)
                WindowsAutoStart.Set(_startSwitch.Checked, StartWithWindowsArguments);
        }

        // The defaults of the options of the library (as the choices say): dark, blue, the language of Windows, not on top.
        private void RestoreDefaults()
        {
            if (_themeBox != null)
                _themeBox.SelectedIndex = 0;

            if (_accentBox != null)
                _accentBox.SelectedIndex = Array.FindIndex(UIAccentColors.All, color => color.ToArgb() == UIAccentColors.Blue.ToArgb());

            if (_languageBox != null)
                _languageBox.SelectedIndex = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de" ? 1 : 0;

            if (_topMostSwitch != null)
                _topMostSwitch.Checked = false;

            if (_startSwitch != null)
                _startSwitch.Checked = false;

            DefaultsRestored?.Invoke(this, EventArgs.Empty);

            if (ShowToasts)
                ShowToast(UIStrings.Get("OptionsForm.DefaultsRestored"), this);
        }

        // "Settings saved" over the window that owns this one (it stays when this one is closed).
        private void ShowSavedToast()
        {
            if (ShowToasts && Owner != null && !Owner.IsDisposed)
                ShowToast(UIStrings.Get("OptionsForm.Saved"), Owner);
        }

        // The owner is known when the window opens.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SyncAlwaysOnTopSwitch();
        }

        // Closed without Save: what the user tried is taken back.
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (_optionsChanged && _showButtons && DialogResult != DialogResult.OK)
                RestoreOriginalSettings();
        }

        private void RestoreOriginalSettings()
        {
            UIColors.ApplyTheme(_originalTheme);
            UIColors.SetAccent(_originalAccent);
            AppLocalization.Language = _originalAppLanguage;
            UIStrings.Language = _originalLanguage;

            if (_topMostTouched && _topMostTarget != null && !_topMostTarget.IsDisposed)
                _topMostTarget.TopMost = _originalTopMost;

            _optionsChanged = false;
        }

        // ---- the texts and the items

        private static ComboBox CreateOptionChoice()
        {
            ComboBox box = UIStyles.ComboBoxes.CreateStandard();
            box.DropDownStyle = ComboBoxStyle.DropDownList;

            return box;
        }

        private static string GetOptionText(OptionsPreset preset)
        {
            switch (preset)
            {
                case OptionsPreset.Theme:
                    return UIStrings.Get("OptionsForm.Theme");
                case OptionsPreset.AccentColor:
                    return UIStrings.Get("OptionsForm.AccentColor");
                case OptionsPreset.Language:
                    return UIStrings.Get("OptionsForm.Language");
                case OptionsPreset.StartWithWindows:
                    return UIStrings.Get("OptionsForm.StartWithWindows");
                case OptionsPreset.ResetToDefaults:
                    return UIStrings.Get("OptionsForm.Defaults");
                default:
                    return UIStrings.Get("OptionsForm.AlwaysOnTop");
            }
        }

        // The items of the choices, in the language of the library; what is applied now is selected.
        private void FillOptionItems()
        {
            bool wasLoading = _loadingOptions;
            _loadingOptions = true;

            try
            {
                if (_themeBox != null)
                {
                    _themeBox.Items.Clear();
                    _themeBox.Items.Add(UIStrings.Get("OptionsForm.ThemeDark"));
                    _themeBox.Items.Add(UIStrings.Get("OptionsForm.ThemeLight"));
                    _themeBox.SelectedIndex = SelectedTheme == UIThemes.Light ? 1 : 0;
                }

                if (_accentBox != null)
                {
                    _accentBox.Items.Clear();

                    foreach (string name in AccentNames)
                        _accentBox.Items.Add(UIStrings.Get("OptionsForm.Accent." + name));

                    _accentBox.SelectedIndex = Array.FindIndex(UIAccentColors.All, color => color.ToArgb() == UIColors.Primary.ToArgb());
                }

                if (_languageBox != null)
                {
                    _languageBox.Items.Clear();
                    _languageBox.Items.Add("English");
                    _languageBox.Items.Add("Deutsch");
                    _languageBox.SelectedIndex = UIStrings.Language == UILanguage.German ? 1 : 0;
                }
            }
            finally
            {
                _loadingOptions = wasLoading;
            }
        }

        // After a switch of the language: the names of the rows and the items.
        private void RefreshOptionTexts()
        {
            if (_optionsSection == null)
                return;

            _optionsSection.Text = UIStrings.Get("OptionsForm.General");

            foreach (KeyValuePair<OptionsPreset, PropertyRow> pair in _presetRows)
                pair.Value.Text = GetOptionText(pair.Key);

            if (_resetButton != null)
                _resetButton.Text = UIStrings.Get("OptionsForm.ResetToDefaults");

            FillOptionItems();
        }
    }
}
