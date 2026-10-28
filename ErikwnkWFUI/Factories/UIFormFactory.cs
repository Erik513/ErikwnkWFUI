using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIFormFactory
    {
        // The options window: the same, made as an OptionsForm (it brings its table and its buttons).
        public static OptionsForm CreateOptions(FormOptions options, UIColorSet colors, OptionsPreset[] presets)
        {
            if (colors != null)
                options.Colors = colors;

            return new OptionsForm(options, presets);
        }

        // The input box: the same, made as an InputBoxForm (a dialog; with no title it has the one of the language).
        public static InputBoxForm CreateInput(FormOptions options, UIColorSet colors, string prompt, string text, bool password)
        {
            if (colors != null)
                options.Colors = colors;

            return new InputBoxForm(options, prompt, text, password);
        }

        // The window of the options, with the color set (when there is one) for its frame.
        public static AppForm Create(FormOptions options, UIColorSet colors)
        {
            if (colors != null)
                options.Colors = colors;

            return new AppForm(options);
        }
    }
}
