using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    internal static class UIFormFactory
    {
        // The options window: the same, made as an OptionsForm (it brings its table and its buttons).
        public static OptionsForm CreateOptions(FormOptions options, UIColorSet colors)
        {
            if (colors != null)
                options.Colors = colors;

            return new OptionsForm(options);
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
