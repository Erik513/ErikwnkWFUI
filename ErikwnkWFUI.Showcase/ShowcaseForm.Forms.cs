using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;

namespace ErikwnkWFUI.Showcase
{
    // The AppForm tab: ONE sample window and the controls that set its options. Every change closes the window and opens it again in the
    // same place with the new options, so there is never more than one window; what the change does is written in the window itself.
    public partial class ShowcaseForm
    {
        private AppForm _sampleForm;
        private ComboBox _formKind;
        private ComboBox _formLook;
        private ComboBox _formAlign;
        private ComboBox _formBorder;
        private ComboBox _formBar;
        private ToggleSwitch _formMinimize;
        private ToggleSwitch _formMaximize;
        private ToggleSwitch _formClose;
        private ToggleSwitch _formShadow;
        private ToggleSwitch _formResizable;
        private ToggleSwitch _formBorderless;
        private ToggleSwitch _formEscape;
        private ToggleSwitch _formVersion;
        private ToggleSwitch _formRemember;
        private bool _syncingFormControls;

        private const string FormDefaultChoice = "default";
        private const string FormOwnVersionText = "v9.9 (own text)";

        private void AddFormsSection(PropertyTable table)
        {
            CloseSampleForm();

            table.AddSection("One sample window - change a property and it shows in the window at once");

            Button open = UIStyles.Buttons.CreateStandard("Open", size: new Size(110, 32));
            Button close = UIStyles.Buttons.CreateStandard("Close", size: new Size(110, 32));
            open.Click += (sender, e) => ReopenSampleForm("Change the kind, the colors and the properties of the window in the Showcase: this window changes in place.");
            close.Click += (sender, e) => CloseSampleForm();
            table.AddRow("Sample window", open, close);

            _formKind = CreateFormChoice("Window", "Dialog", "Settings");
            _formLook = CreateFormChoice("Standard", "Primary", "colors: Yellow");
            _formKind.SelectedIndexChanged += (sender, e) => OnFormPresetChanged("The kind of window: a Window can be resized, minimized and maximized; a Dialog has only the close button, a fixed size, and Escape closes it; Settings is a Dialog with the version of the program in the strip below.");
            _formLook.SelectedIndexChanged += (sender, e) => OnFormPresetChanged("The colors: Standard is the usual dark title bar, Primary a very dark shade of the accent, and with colors a color set colors the title bar.");
            table.AddRow("UIStyles.Forms", _formKind, _formLook);

            _formAlign = CreateFormChoice("MiddleLeft", "MiddleCenter", "MiddleRight");
            _formBorder = CreateFormChoice(FormDefaultChoice, "Crimson", "Teal");
            _formBar = CreateFormChoice(FormDefaultChoice, "DarkGreen", "SteelBlue");
            _formAlign.SelectedIndexChanged += (sender, e) => ReopenSampleFormIfOpen("TitleTextAlign: where the title stands. In the middle it keeps clear of the buttons when the window gets narrow.");
            _formBorder.SelectedIndexChanged += (sender, e) => ReopenSampleFormIfOpen("BorderColor: the thin edge around the window (black by default). It has no effect with the native frame.");
            _formBar.SelectedIndexChanged += (sender, e) => ReopenSampleFormIfOpen("TitleBarBackColor: a color of the bar alone, which does not follow the theme.");
            table.AddRow("Appearance",
                FormName("TitleTextAlign"), PropertyColumn.Percent(_formAlign, 33f),
                FormName("BorderColor"), PropertyColumn.Percent(_formBorder, 33f),
                FormName("TitleBarBackColor"), PropertyColumn.Percent(_formBar, 33f));

            _formMinimize = CreateFormSwitch("ShowMinimizeButton: without it the taskbar and the system menu cannot minimize the window either.");
            _formMaximize = CreateFormSwitch("ShowMaximizeButton: the maximize button of the title bar.");
            _formClose = CreateFormSwitch("ShowCloseButton: without it close the window with Alt+F4 or from the system menu (right-click the title bar).");
            table.AddRow("Title bar buttons",
                FormSwitch(_formMinimize), FormName("ShowMinimizeButton"),
                FormSwitch(_formMaximize), FormName("ShowMaximizeButton"),
                FormSwitch(_formClose), FormName("ShowCloseButton"));

            _formShadow = CreateFormSwitch("Shadow: the drop shadow around a window without a frame.");
            _formResizable = CreateFormSwitch("Resizable: whether the window can be resized by its edges.");
            _formBorderless = CreateFormSwitch("Borderless = false: the native frame of Windows, and no title bar of the library (BorderColor and Shadow have nothing to show then).");
            table.AddRow("Frame",
                FormSwitch(_formShadow), FormName("Shadow"),
                FormSwitch(_formResizable), FormName("Resizable"),
                FormSwitch(_formBorderless), FormName("Borderless"));

            _formEscape = CreateFormSwitch("CloseOnEscape: Escape closes the window, as it does in every dialog.");
            _formVersion = CreateFormSwitch("VersionText: the strip at the bottom shows this text instead of the version of the program (without it a Window has no strip).");
            _formRemember = CreateFormSwitch("RememberBoundsKey: move this window, resize it, close it with Close and open it again: it comes back where it was.");
            table.AddRow("Behavior",
                FormSwitch(_formEscape), FormName("CloseOnEscape"),
                FormSwitch(_formVersion), FormName("VersionText = own text"),
                FormSwitch(_formRemember), FormName("RememberBoundsKey"));

            SyncFormControlsToPreset();
        }

        // ---- the controls

        private ComboBox CreateFormChoice(params string[] items)
        {
            ComboBox combo = UIStyles.ComboBoxes.CreateStandard();
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.AddRange(items);
            combo.SelectedIndex = 0;

            return combo;
        }

        private ToggleSwitch CreateFormSwitch(string description)
        {
            ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(true);
            toggle.CheckedChanged += (sender, e) => ReopenSampleFormIfOpen(description);

            return toggle;
        }

        // The name of a property in front of its control, in a column as wide as the text.
        private static PropertyColumn FormName(string name)
        {
            Label label = UIStyles.Labels.CreateStandard(name);
            label.AutoSize = false;
            label.TextAlign = ContentAlignment.MiddleLeft;

            return PropertyColumn.Absolute(label, TextRenderer.MeasureText(name, label.Font).Width + 24);
        }

        private static PropertyColumn FormSwitch(ToggleSwitch toggle)
        {
            return PropertyColumn.Absolute(toggle, 56);
        }

        // ---- the options of the window, as the controls set them

        private FormOptions CreateFormPreset()
        {
            string title = "Sample window";
            bool primary = (string)_formLook.SelectedItem == "Primary";

            switch ((string)_formKind.SelectedItem)
            {
                case "Dialog":
                    return primary ? FormOptions.CreateDialogPrimary(title) : FormOptions.CreateDialog(title);
                case "Settings":
                    return primary ? FormOptions.CreateSettingsPrimary(title) : FormOptions.CreateSettings(title);
                default:
                    return primary ? FormOptions.CreatePrimary(title) : FormOptions.CreateStandard(title);
            }
        }

        private static Color? FormColorOf(ComboBox combo)
        {
            string name = (string)combo.SelectedItem;

            return name == FormDefaultChoice ? (Color?)null : Color.FromName(name);
        }

        private FormOptions CreateFormOptions()
        {
            FormOptions options = CreateFormPreset();

            if ((string)_formLook.SelectedItem == "colors: Yellow")
                options.Colors = UIStyles.Colors.Sets.Yellow;

            options.TitleTextAlign = (ContentAlignment)Enum.Parse(typeof(ContentAlignment), (string)_formAlign.SelectedItem);
            options.BorderColor = FormColorOf(_formBorder);
            options.TitleBarBackColor = FormColorOf(_formBar);
            options.ShowMinimizeButton = _formMinimize.Checked;
            options.ShowMaximizeButton = _formMaximize.Checked;
            options.ShowCloseButton = _formClose.Checked;
            options.Shadow = _formShadow.Checked;
            options.Resizable = _formResizable.Checked;
            options.Borderless = _formBorderless.Checked;
            options.CloseOnEscape = _formEscape.Checked;

            if (_formVersion.Checked)
                options.VersionText = FormOwnVersionText;

            if (_formRemember.Checked)
                options.RememberBoundsKey = "Showcase.Sample";

            return options;
        }

        // A new kind or new colors start from that preset: the controls show what it has.
        private void SyncFormControlsToPreset()
        {
            FormOptions preset = CreateFormPreset();
            _syncingFormControls = true;

            try
            {
                _formAlign.SelectedItem = preset.TitleTextAlign.ToString();
                _formBorder.SelectedItem = FormDefaultChoice;
                _formBar.SelectedItem = FormDefaultChoice;
                _formMinimize.Checked = preset.ShowMinimizeButton;
                _formMaximize.Checked = preset.ShowMaximizeButton;
                _formClose.Checked = preset.ShowCloseButton;
                _formShadow.Checked = preset.Shadow;
                _formResizable.Checked = preset.Resizable;
                _formBorderless.Checked = preset.Borderless;
                _formEscape.Checked = preset.CloseOnEscape;
                _formVersion.Checked = false;
                _formRemember.Checked = false;
            }
            finally
            {
                _syncingFormControls = false;
            }
        }

        private void OnFormPresetChanged(string description)
        {
            if (_syncingFormControls)
                return;

            SyncFormControlsToPreset();
            ReopenSampleFormIfOpen(description);
        }

        // What differs from the preset: the properties that were changed.
        private string DescribeFormChanges()
        {
            FormOptions preset = CreateFormPreset();
            FormOptions now = CreateFormOptions();
            List<string> changes = new List<string>();

            if ((string)_formLook.SelectedItem == "colors: Yellow")
                changes.Add("Colors = Sets.Yellow");

            if (now.TitleTextAlign != preset.TitleTextAlign) changes.Add("TitleTextAlign = " + now.TitleTextAlign);
            if (now.BorderColor != null) changes.Add("BorderColor = " + now.BorderColor.Value.Name);
            if (now.TitleBarBackColor != null) changes.Add("TitleBarBackColor = " + now.TitleBarBackColor.Value.Name);
            if (now.ShowMinimizeButton != preset.ShowMinimizeButton) changes.Add("ShowMinimizeButton = " + now.ShowMinimizeButton);
            if (now.ShowMaximizeButton != preset.ShowMaximizeButton) changes.Add("ShowMaximizeButton = " + now.ShowMaximizeButton);
            if (now.ShowCloseButton != preset.ShowCloseButton) changes.Add("ShowCloseButton = " + now.ShowCloseButton);
            if (now.Shadow != preset.Shadow) changes.Add("Shadow = " + now.Shadow);
            if (now.Resizable != preset.Resizable) changes.Add("Resizable = " + now.Resizable);
            if (now.Borderless != preset.Borderless) changes.Add("Borderless = " + now.Borderless);
            if (now.CloseOnEscape != preset.CloseOnEscape) changes.Add("CloseOnEscape = " + now.CloseOnEscape);
            if (now.VersionText != preset.VersionText) changes.Add("VersionText = \"" + now.VersionText + "\"");
            if (now.RememberBoundsKey != null) changes.Add("RememberBoundsKey = \"" + now.RememberBoundsKey + "\"");

            return changes.Count == 0 ? "The preset as it is." : "Changed from the preset: " + string.Join(", ", changes) + ".";
        }

        // ---- the one window

        private void ReopenSampleFormIfOpen(string description)
        {
            if (_syncingFormControls || _sampleForm == null || _sampleForm.IsDisposed)
                return;

            ReopenSampleForm(description);
        }

        private void ReopenSampleForm(string description)
        {
            Rectangle? old = null;
            bool wasResizable = false;

            if (_sampleForm != null && !_sampleForm.IsDisposed)
            {
                old = _sampleForm.Bounds;
                wasResizable = _sampleForm.MinimumSize != Size.Empty;
                _sampleForm.Close();
            }

            FormOptions options = CreateFormOptions();
            AppForm form = new AppForm(options);

            Label text = UIStyles.Labels.CreateStandard(description + "\r\n\r\n" + DescribeFormChanges() + "\r\n\r\nRight-click the title bar, or press Alt+Space, for the system menu.");
            text.AutoSize = false;
            text.Dock = DockStyle.Fill;
            text.Padding = new Padding(16);
            text.AutoEllipsis = false;
            form.ContentPanel.Controls.Add(text);

            form.ClientSize = new Size(520, 230);

            // The same place (and the size the user gave it) as the window before.
            if (old != null)
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = old.Value.Location;

                if (wasResizable && options.Resizable)
                    form.Size = old.Value.Size;
            }

            form.FormClosed += (sender, e) =>
            {
                if (ReferenceEquals(_sampleForm, form))
                    _sampleForm = null;
            };

            _sampleForm = form;
            form.Show(this);

            // The controls that were changed stay in front: the sample window does not take the keyboard from them.
            Activate();
        }

        private void CloseSampleForm()
        {
            if (_sampleForm != null && !_sampleForm.IsDisposed)
                _sampleForm.Close();

            _sampleForm = null;
        }
    }
}
