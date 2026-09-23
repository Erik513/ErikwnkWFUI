using System.Drawing;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// SetAccent mutates static state shared by the whole process - every test
/// here restores the real default (SetAccent(UIAccentColors.Blue), which
/// reproduces UIColors' own hardcoded defaults exactly) in a finally block,
/// so an assertion failure can't leave later tests reading a wrong accent.
/// [Collection] (see AccentColorTestCollection) keeps this from racing any
/// other class that reads an accent-derived color while this one changes it.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class UIColorsTests
{
    [Fact]
    public void SetAccent_NeutralGrayAccent_UsesAHigherSelectionAlpha()
    {
        try
        {
            UIColors.SetAccent(Color.FromArgb(90, 90, 90));

            // A hued Selection (the default, alpha 60) barely lifts a gray
            // row's own shade - a gray-on-gray selection needs more alpha
            // than that to actually read as selected. See ListView's own
            // SelectionOverlayColor default (also 130) for the same reasoning.
            Assert.Equal(130, UIColors.Selection.A);
            Assert.Equal(Color.FromArgb(130, 90, 90, 90), UIColors.Selection);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void SetAccent_HuedAccent_KeepsTheLowerSelectionAlpha()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Red);

            Assert.Equal(60, UIColors.Selection.A);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }
}
