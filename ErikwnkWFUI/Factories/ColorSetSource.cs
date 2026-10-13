using System;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Factories
{
    // The color set a factory was handed, in the form the controls take it: a function that is asked again each time.
    // None handed (null) stays none, so the control keeps its own colors. (The primary set is given as a function of its
    // own, () => UIColors.PrimarySet, which follows the accent.)
    internal static class ColorSetSource
    {
        public static Func<UIColorSet> Of(UIColorSet colors)
        {
            return colors == null ? (Func<UIColorSet>)null : () => colors;
        }
    }
}
