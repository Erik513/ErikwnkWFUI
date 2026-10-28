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
