using System;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using Button = System.Windows.Forms.Button;

namespace ErikwnkWFUI.Showcase
{
    // The AppForm tab: the left column names what is shown, the buttons open a small sample window; what the window demonstrates is
    // written in the window itself.
    public partial class ShowcaseForm
    {
        private void AddFormsSection(PropertyTable table)
        {
            table.AddSection("UIStyles.Forms: Standard | Primary | colors: Yellow");

            AddFormKindRow(table, "Window", "A resizable window: minimize, maximize and close, and a least size.",
                (title, colors) => UIStyles.Forms.CreateStandard(title, colors),
                title => UIStyles.Forms.CreatePrimary(title),
                "CreateStandard", "CreatePrimary");
            AddFormKindRow(table, "Dialog", "A dialog: fixed size, only the close button, and Escape closes it.",
                (title, colors) => UIStyles.Forms.CreateDialogStandard(title, colors),
                title => UIStyles.Forms.CreateDialogPrimary(title),
                "CreateDialogStandard", "CreateDialogPrimary");
            AddFormKindRow(table, "Settings", "A settings window: a dialog with the version of the program in the strip at the bottom.",
                (title, colors) => UIStyles.Forms.CreateSettingsStandard(title, colors),
                title => UIStyles.Forms.CreateSettingsPrimary(title),
                "CreateSettingsStandard", "CreateSettingsPrimary");

            table.AddRow("VersionStrip", CreateFormButton("Open", () =>
            {
                AppForm form = UIStyles.Forms.CreateSettingsStandard("VersionStrip");

                // The update button of the library (AppUpdater.CreateUpdateAvailableButton): always the update symbol, never a text.
                Button update = UIStyles.Buttons.CreateSymbol(
                    UIStyles.Symbols.Update,
                    "An update is available",
                    new Size(22, 20),
                    UIStyles.Colors.Sets.Green);
                update.Margin = new Padding(6, 2, 0, 2);
                form.VersionStrip.Controls.Add(update);

                return form;
            }, "What is put into the strip stands left of the version text, in the middle of the strip's height (here: the update button, which is always a symbol)."));

            table.AddSection("FormOptions: one property, its values side by side");

            AddOptionRow(table, "TitleTextAlign",
                Option("MiddleLeft", "The title at the left, next to the logo.", o => o.TitleTextAlign = ContentAlignment.MiddleLeft),
                Option("MiddleCenter", "The title in the middle of the bar (the default for a window). It keeps clear of the buttons when the window gets narrow.", o => o.TitleTextAlign = ContentAlignment.MiddleCenter),
                Option("MiddleRight", "The title at the right, left of the buttons.", o => o.TitleTextAlign = ContentAlignment.MiddleRight));

            AddOptionRow(table, "BorderColor",
                Option("default (black)", "The thin edge around the window is black, whatever color the title bar has.", o => { }),
                Option("Crimson", "The edge around the window in Crimson. Look at the window's edge.", o => o.BorderColor = Color.Crimson),
                Option("Teal", "The edge around the window in Teal. Look at the window's edge.", o => o.BorderColor = Color.Teal));

            AddOptionRow(table, "TitleBarBackColor",
                Option("default", "The title bar has the usual dark color.", o => { }),
                Option("DarkGreen", "A color of the bar alone (it does not follow the theme); the buttons and the title keep readable.", o => o.TitleBarBackColor = Color.DarkGreen),
                Option("SteelBlue", "A color of the bar alone.", o => o.TitleBarBackColor = Color.SteelBlue));

            AddOptionRow(table, "Shadow",
                Option("true", "The window has a drop shadow (the default).", o => o.Shadow = true),
                Option("false", "No drop shadow around the window.", o => o.Shadow = false));

            AddOptionRow(table, "ShowMinimizeButton",
                Option("true", "The title bar has the minimize button.", o => o.ShowMinimizeButton = true),
                Option("false", "No minimize button (the taskbar and the system menu cannot minimize the window either).", o => o.ShowMinimizeButton = false));

            AddOptionRow(table, "ShowMaximizeButton",
                Option("true", "The title bar has the maximize button.", o => o.ShowMaximizeButton = true),
                Option("false", "No maximize button.", o => o.ShowMaximizeButton = false));

            AddOptionRow(table, "ShowCloseButton",
                Option("true", "The title bar has the close button.", o => o.ShowCloseButton = true),
                Option("false", "No close button: close this window with Alt+F4 or from the system menu (right-click the title bar).", o => o.ShowCloseButton = false));

            AddOptionRow(table, "Resizable",
                Option("true", "The window can be resized by its edges.", o => o.Resizable = true),
                Option("false", "A fixed size: no resizing by the edges.", o => o.Resizable = false));

            AddOptionRow(table, "CloseOnEscape",
                Option("false", "Escape does nothing in a standard window.", o => o.CloseOnEscape = false),
                Option("true", "Escape closes this window, as it does in every dialog.", o => o.CloseOnEscape = true));

            AddOptionRow(table, "VersionText",
                Option("null", "No version strip (the default for a window).", o => o.VersionText = null),
                Option("\"v9.9 (own text)\"", "The strip at the bottom shows this text instead of the version of the program.", o => o.VersionText = "v9.9 (own text)"));

            AddOptionRow(table, "Borderless",
                Option("true", "The window has the title bar of the library (the default).", o => o.Borderless = true),
                Option("false", "The native frame of Windows, and no title bar of the library. (BorderColor and Shadow have nothing to show here: there is no edge of the library.)", o => o.Borderless = false));

            AddOptionRow(table, "RememberBoundsKey",
                Option("null", "The window remembers nothing.", o => o.RememberBoundsKey = null),
                Option("\"Showcase.Sample\"", "Move this window, resize it, close it and open it again: it comes back where it was.", o => o.RememberBoundsKey = "Showcase.Sample"));
        }

        // One kind of window in three columns: Standard, Primary and Standard with the color set Yellow.
        private void AddFormKindRow(
            PropertyTable table,
            string kind,
            string description,
            Func<string, UIColorSet, AppForm> standard,
            Func<string, AppForm> primary,
            string standardName,
            string primaryName)
        {
            Button first = CreateFormButton(standardName, () => standard(standardName, null), description);
            Button second = CreateFormButton(primaryName, () => primary(primaryName), description + " The title bar is in a very dark shade of the accent.");
            Button third = CreateFormButton(standardName + "(colors: Yellow)", () => standard("colors: Yellow", UIStyles.Colors.Sets.Yellow), description + " The title bar takes the color of the set; the strip stays a darker gray.");

            table.AddRow(kind,
                PropertyColumn.Percent(first, 100f / 3),
                PropertyColumn.Percent(second, 100f / 3),
                PropertyColumn.Percent(third, 100f / 3));
        }

        // A property with its values: the left column only names it, each value is a button that opens a standard window with that value.
        private void AddOptionRow(PropertyTable table, string property, params (string Value, string Description, Action<FormOptions> Change)[] values)
        {
            PropertyColumn[] columns = new PropertyColumn[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                (string value, string description, Action<FormOptions> change) = values[i];
                string title = property + " = " + value;

                columns[i] = PropertyColumn.Percent(CreateFormButton(value, () =>
                {
                    FormOptions options = FormOptions.CreateStandard(title);
                    change(options);

                    return new AppForm(options);
                }, description), 100f / values.Length);
            }

            table.AddRow(property, columns);
        }

        private static (string Value, string Description, Action<FormOptions> Change) Option(string value, string description, Action<FormOptions> change)
        {
            return (value, description, change);
        }

        private Button CreateFormButton(string text, Func<AppForm> create, string description)
        {
            Button button = UIStyles.Buttons.CreateStandard(text, size: new Size(160, 32));
            button.AutoSize = true;
            button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            button.Click += (sender, e) => OpenSampleForm(create(), description);

            return button;
        }

        private void OpenSampleForm(AppForm form, string description)
        {
            Label text = UIStyles.Labels.CreateStandard(description + "\r\n\r\nRight-click the title bar, or press Alt+Space, for the system menu.");
            text.AutoSize = false;
            text.Dock = DockStyle.Fill;
            text.Padding = new Padding(16);
            text.AutoEllipsis = false;
            form.ContentPanel.Controls.Add(text);

            form.ClientSize = new Size(460, 190);
            form.Show(this);
        }
    }
}
