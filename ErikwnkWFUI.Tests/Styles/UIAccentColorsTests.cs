using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// BlackOrWhite resolves live off UIColors.BackgroundDark, which
/// ApplyTheme mutates just like SetAccent mutates the fields UIColorsTests
/// covers - restores the real default (Dark) in a finally block, and
/// shares AccentColorTestCollection with it for the same reason.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class UIAccentColorsTests
{
    [Fact]
    public void BlackOrWhite_IsLight_OnTheDarkTheme()
    {
        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);

            Assert.Equal(UIColors.LightForeColor, UIAccentColors.BlackOrWhite);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    [Fact]
    public void BlackOrWhite_IsDark_OnTheLightTheme()
    {
        try
        {
            UIColors.ApplyTheme(UIThemes.Light);

            Assert.Equal(UIColors.DarkForeColor, UIAccentColors.BlackOrWhite);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }
}
