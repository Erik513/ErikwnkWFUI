using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;

namespace ErikwnkWFUI.Showcase
{
    // The AppForm tab: every row opens a small sample window.
    public partial class ShowcaseForm
    {
        private void AddFormsSection(PropertyTable table)
        {
            table.AddSection("UIStyles.Forms - a resizable window");

            table.AddRow("CreateStandard", CreateFormButton(() => UIStyles.Forms.CreateStandard("CreateStandard")));
            table.AddRow("CreatePrimary", CreateFormButton(() => UIStyles.Forms.CreatePrimary("CreatePrimary")));
            table.AddRow("CreateStandard(colors: Yellow)", CreateFormButton(() => UIStyles.Forms.CreateStandard("colors: Yellow", UIStyles.Colors.Sets.Yellow)));

            table.AddSection("UIStyles.Forms - a dialog (fixed size, only the close button, Escape closes it)");

            table.AddRow("CreateDialog", CreateFormButton(() => UIStyles.Forms.CreateDialog("CreateDialog")));
            table.AddRow("CreatePrimaryDialog", CreateFormButton(() => UIStyles.Forms.CreatePrimaryDialog("CreatePrimaryDialog")));
            table.AddRow("CreateDialog(colors: Yellow)", CreateFormButton(() => UIStyles.Forms.CreateDialog("colors: Yellow", UIStyles.Colors.Sets.Yellow)));

            table.AddSection("UIStyles.Forms - a settings window (a dialog with the version of the program)");

            table.AddRow("CreateSettings", CreateFormButton(() => UIStyles.Forms.CreateSettings("CreateSettings")));
            table.AddRow("CreatePrimarySettings", CreateFormButton(() => UIStyles.Forms.CreatePrimarySettings("CreatePrimarySettings")));
            table.AddRow("CreateSettings(colors: Yellow)", CreateFormButton(() => UIStyles.Forms.CreateSettings("colors: Yellow", UIStyles.Colors.Sets.Yellow)));
            table.AddRow("VersionStrip: what is added stands left of the text", CreateFormButton(() =>
            {
                AppForm form = UIStyles.Forms.CreateSettings("VersionStrip");
                form.VersionStrip.Controls.Add(UIStyles.Buttons.CreatePrimary("Update", size: new Size(80, 20)));

                return form;
            }));

            table.AddSection("FormOptions - single settings (start from a preset, change one thing)");

            table.AddRow("BorderColor = Crimson (the edge around the window; black by default)", CreateOptionsButton("BorderColor", options => options.BorderColor = Color.Crimson));
            table.AddRow("MinimumSize = 400 x 300 (try to make the window smaller)", CreateOptionsButton("MinimumSize", options => options.MinimumSize = new Size(400, 300)));
            table.AddRow("Shadow = false", CreateOptionsButton("Shadow = false", options => options.Shadow = false));
            table.AddRow("TitleTextAlign = MiddleLeft, ShowMinimizeButton = false", CreateOptionsButton("TitleTextAlign", options =>
            {
                options.TitleTextAlign = ContentAlignment.MiddleLeft;
                options.ShowMinimizeButton = false;
            }));
            table.AddRow("TitleBarBackColor (the bar alone)", CreateOptionsButton("TitleBarBackColor", options => options.TitleBarBackColor = Color.DarkGreen));
            table.AddRow("CloseOnEscape = true on a standard window", CreateOptionsButton("CloseOnEscape", options => options.CloseOnEscape = true));
            table.AddRow("VersionText = \"v9.9 (own text)\" on a standard window", CreateOptionsButton("VersionText", options => options.VersionText = "v9.9 (own text)"));
            table.AddRow("Borderless = false (the native frame, no title bar of the library)", CreateOptionsButton("Borderless = false", options => options.Borderless = false));
            table.AddRow("RememberBoundsKey (move it, resize it, close it, open it again)", CreateOptionsButton("RememberBoundsKey", options => options.RememberBoundsKey = "Showcase.Sample"));
        }

        private Button CreateFormButton(Func<AppForm> create)
        {
            Button button = UIStyles.Buttons.CreateStandard("Open", size: new Size(110, 32));
            button.Click += (sender, e) => OpenSampleForm(create());

            return button;
        }

        // A standard window with one setting changed.
        private Button CreateOptionsButton(string title, Action<FormOptions> change)
        {
            return CreateFormButton(() =>
            {
                FormOptions options = FormOptions.CreateStandard(title);
                change(options);

                return new AppForm(options);
            });
        }

        private void OpenSampleForm(AppForm form)
        {
            Label text = UIStyles.Labels.CreateStandard("Right-click the title bar, or press Alt+Space, for the system menu.");
            text.AutoSize = false;
            text.Dock = DockStyle.Fill;
            text.Padding = new Padding(16);
            form.ContentPanel.Controls.Add(text);

            form.ClientSize = new Size(440, 180);
            form.StartPosition = FormStartPosition.CenterParent;
            form.Show(this);
        }
    }
}
