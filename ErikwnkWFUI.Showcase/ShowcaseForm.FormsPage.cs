using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Showcase
{
    // The Forms tab: one page for each kind of window, turned like the pages of a book (a MultiPanel), so that nobody has to scroll through all of
    // them. Every page has the same build: a short text on top, under it the table with the sample first (the buttons that open the window) and the
    // settings below.
    public partial class ShowcaseForm
    {
        private MultiPanel CreateFormsPage()
        {
            MultiPanel pages = UIStyles.MultiPanels.CreateStandard();
            pages.Dock = DockStyle.Fill;
            pages.Padding = new Padding(0);

            (string Title, string Description, Action<PropertyTable> Fill)[] forms =
            {
                ("Window (AppForm)",
                 "The window of the library with its own title bar (AppForm), made by UIStyles.Forms: CreateStandard is a resizable window (it has a least size), CreateDialogStandard a fixed-size one with only the close button that Escape closes, CreateSettingsStandard a dialog with the version of the program in a strip at the bottom. Standard has the usual dark title bar, Primary a very dark shade of the accent, and with colors: a color set the title bar takes its color; the thin edge is black in all of them (BorderColor sets another color). size: gives one of the three sizes of the kind of window, or an own Size. There is one sample window: open it, then change the kind, the colors, the size and the properties of FormOptions in the rows below - the window closes and opens again in the same place with the new options, and says in itself what the change does. Right-click its title bar or press Alt+Space for the system menu.",
                 AddFormsSection),

                ("Options window (OptionsForm)",
                 "A settings window around a property table with ready-made options (AddOption): a sun/moon button in the top right corner for the theme, accent color, language and always on top - applied as they are chosen, taken back by Cancel; start with Windows - written on Save; a reset button. Save and Cancel (ShowButtons), the table as a card in the middle (size: sets its width). Open it and try it, it works on this window. Saving also writes the appearance (theme, accent color, language) when the program called UIStyles.RememberAppearance() at startup; UIAppearance keeps it in the program's own settings instead.",
                 AddOptionsFormSection),

                ("Input box (InputBox)",
                 "A small modal dialog with a prompt, one text box (password: with an eye to show it) and OK / Cancel, made by UIStyles.Forms.CreateInputStandard / CreateInputPrimary (colors: a color set for the title bar) or in one line by InputBox.Show, which returns the text or null when cancelled. Hint is a gray note under the prompt, ErrorText a red line over the box that goes when the user types, Validator checks the text when OK is clicked (a message keeps the box open and selects the text), TrimValue cuts the spaces at the ends, MaxLength limits the box, AllowEmpty lets OK take an empty text. The switches say what the box has; the line Result shows what came back.",
                 AddInputBoxSection),

                ("Progress dialog (ProgressForm)",
                 "A small dialog that shows how far a task has come: a text and a progress bar with the percentage on it, made by UIStyles.Forms.CreateProgressStandard / CreateProgressPrimary, or - for a task that blocks the program for a while - in one call by ProgressBox.Run, which runs the task and closes the window when it is done. The task reports with Report(percent, status) from any thread; the user cannot close the window, the program does, or the task ends it with Succeed (green) or Fail (red, the message stays) and the user closes it with the button. Cancelable adds a Cancel button (Escape too) that asks the task to stop through CancellationToken; ShowTime adds the time that has passed and an estimate of what is left; IsIndeterminate lets the bar just move. size: sets the width. Open shows the window of the page, ProgressBox.Run the modal one; the line Result says how the task ended.",
                 AddProgressFormSection),

                ("Message box (MessageBox)",
                 "The themed replacement for the message box of Windows: MessageBox.Show(message, title, buttons, icon, owner, size) returns the button that was clicked (or the safe one when the window is closed: No, Cancel, Abort or OK). The sample is made with UIStyles.Forms.CreateMessageStandard / CreateMessagePrimary, which also take details (a Details button), the default button, own button texts and playSound; CheckBoxText, ShowHelpButton and HelpClicked are set on the form (MessageBox.ShowWithCheckBox is the one-line way for a check box). Ctrl+C copies the text of the box. The rows below set what the sample shows; the line Result says what came back.",
                 AddMessageBoxSection)
            };

            bool[] built = new bool[forms.Length];

            Action<int> build = index =>
            {
                if (built[index])
                    return;

                built[index] = true;
                FillTablePage(pages.Pages[index], forms[index].Description, forms[index].Fill);
            };

            for (int i = 0; i < forms.Length; i++)
                pages.AddPage(forms[i].Title);

            // A page is built when it is first turned to; the windows of the page that was left are closed (they belong to its settings).
            pages.SelectedIndexChanged += (sender, e) =>
            {
                CloseSampleForm();
                CloseOptionsForm();
                CloseProgressSample();
                build(pages.SelectedIndex);
            };

            build(0);

            return pages;
        }

        // A page: the text on top, under it the table in a scrolling host (filled first, so the scroll area gets what the text leaves).
        private void FillTablePage(Control parent, string description, Action<PropertyTable> fill)
        {
            Panel scrollHost = UIStyles.Panels.CreateMedium();
            scrollHost.Dock = DockStyle.Fill;
            scrollHost.AutoScroll = true;

            PropertyTable table = UIStyles.PropertyTables.CreateStandard();
            table.Dock = DockStyle.Top;

            // The names of the rows (property and factory names) need more than the usual width of the label column.
            table.LabelWidth = 200;
            fill(table);
            scrollHost.Controls.Add(table);

            parent.Controls.Add(scrollHost);
            parent.Controls.Add(CreateDescription(description));
        }
    }
}
