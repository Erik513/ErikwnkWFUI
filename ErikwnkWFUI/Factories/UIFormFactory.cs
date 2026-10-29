using System.Drawing;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIFormFactory
    {
        // The options window: the same, made as an OptionsForm (it brings its table and its buttons).
        public static OptionsForm CreateOptions(FormOptions options, UIColorSet colors, OptionsPreset[] presets, FormSizeOption? size)
        {
            if (colors != null)
                options.Colors = colors;

            return new OptionsForm(options, presets, size);
        }

        // The input box: the same, made as an InputBoxForm (a dialog; with no title it has the one of the language).
        public static InputBoxForm CreateInput(FormOptions options, UIColorSet colors, string prompt, string text, bool password, string placeholder, FormSizeOption? size)
        {
            if (colors != null)
                options.Colors = colors;

            return new InputBoxForm(options, prompt, text, password, placeholder, size);
        }

        // The message box: the same, made as a MessageBoxForm (a dialog whose title bar has the color set, or the accent for the primary one).
        public static MessageBoxForm CreateMessage(FormOptions options, UIColorSet colors, string message, MessageBoxButtons buttons, FormSizeOption? size, string details, MessageBoxDefault defaultButton, MessageBoxIcon icon, string[] buttonTexts, bool playSound)
        {
            if (colors != null)
                options.Colors = colors;

            MessageBoxForm form = new MessageBoxForm(options, message, buttons, size, details, defaultButton, icon, buttonTexts);
            form.PlaySound = playSound;

            return form;
        }

        // The progress dialog: the same, made as a ProgressForm (a dialog; with no title it has the one of the language).
        public static ProgressForm CreateProgress(FormOptions options, UIColorSet colors, string status, bool cancelable, FormSizeOption? size)
        {
            if (colors != null)
                options.Colors = colors;

            return new ProgressForm(options, status, cancelable, size);
        }

        // The window of the options, with the color set (when there is one) for its frame.
        // With a size (one of the three of the kind, or an own one) the window has it, scaled to the screen; without one it keeps the
        // size a window has by itself.
        public static AppForm Create(FormOptions options, UIColorSet colors, FormSizeOption? size = null, FormKind kind = FormKind.Window)
        {
            if (colors != null)
                options.Colors = colors;

            AppForm form = new AppForm(options);

            if (size.HasValue)
            {
                Size wanted = size.Value.Resolve(kind);
                form.Size = new Size(Dpi.Scale(wanted.Width, form.DeviceDpi), Dpi.Scale(wanted.Height, form.DeviceDpi));
            }

            return form;
        }
    }
}
