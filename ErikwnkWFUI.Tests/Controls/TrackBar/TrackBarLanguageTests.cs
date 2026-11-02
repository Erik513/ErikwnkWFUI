using System.Globalization;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>
/// The track bar has no translated text of its own. The language of the library changes how a value is written (by default, and as
/// percent): the decimal sign and "42%" in English, "42 %" in German - not by the culture of the computer, and also while the slider
/// exists. A format of the user is left alone. [Collection]: the language is static state of the process.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class TrackBarLanguageTests
{
    private static string Number(UILanguage language, double value)
    {
        return value.ToString("0.##", CultureInfo.GetCultureInfo(language == UILanguage.German ? "de-DE" : "en-US"));
    }

    private static string Percent(UILanguage language, double value)
    {
        return (Math.Round(value * 100) / 100.0).ToString("P0", CultureInfo.GetCultureInfo(language == UILanguage.German ? "de-DE" : "en-US"));
    }

    [Fact]
    public void TheDefaultFormat_WritesTheDecimalSignOfTheLanguage()
    {
        using TrackBar bar = new TrackBar();

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Assert.Equal(Number(language, 3.5), bar.FormatValue(3.5));
                Assert.Equal(Number(language, 0.25), bar.FormatValue(0.25));
                Assert.Equal(Number(language, 12), bar.FormatValue(12));
            });
        }

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () => Assert.Equal("3.5", bar.FormatValue(3.5)));
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () => Assert.Equal("3,5", bar.FormatValue(3.5)));
    }

    [Fact]
    public void ThePercentFormat_IsWrittenTheWayOfTheLanguage()
    {
        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                foreach (double value in new[] { 0.0, 0.07, 0.42, 1.0 })
                    Assert.Equal(Percent(language, value), TrackBar.FormatPercent(value));
            });
        }

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () => Assert.Equal("42%", TrackBar.FormatPercent(0.42)));
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            string german = TrackBar.FormatPercent(0.42);

            Assert.EndsWith("%", german);
            Assert.True(char.IsWhiteSpace(german[german.Length - 2]), "German writes a space before the percent sign: '" + german + "'");
        });
    }

    [Fact]
    public void TheCultureOfTheComputer_ChangesNothing()
    {
        CultureInfo before = CultureInfo.CurrentCulture;

        try
        {
            using TrackBar bar = new TrackBar();

            foreach (string name in new[] { "de-DE", "fr-FR", "ar-SA", "en-GB" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

                LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
                {
                    Assert.Equal("3.5", bar.FormatValue(3.5));
                    Assert.Equal("42%", TrackBar.FormatPercent(0.42));
                });
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void AScreenReader_GetsTheValueInTheLanguage_AlsoWhenItIsSwitchedWhileTheSliderExists()
    {
        using TrackBar bar = new TrackBar { Value = 0.35 };
        using TrackBar volume = new TrackBar { Value = 0.42, FormatValue = TrackBar.FormatPercent };

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                Assert.Equal(Number(language, 0.35), bar.AccessibilityObject.Value);
                Assert.Equal(Percent(language, 0.42), volume.AccessibilityObject.Value);
            });
        }
    }

    [Fact]
    public void AFormatOfTheUser_IsLeftAlone_InEveryLanguage()
    {
        using TrackBar bar = new TrackBar { FormatValue = value => value.ToString("0.0", CultureInfo.InvariantCulture) + " dB" };

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
            LanguageTestHelper.RunWithLanguage(language, () => Assert.Equal("3.5 dB", bar.FormatValue(3.5)));
    }

    [Fact]
    public void AnOpenPopup_WritesTheNextValueInTheLanguageOfThatMoment()
    {
        Func<Form, bool> before = ErikwnkWFUI.Helpers.PopupSupport.OwnerIsActive;
        ErikwnkWFUI.Helpers.PopupSupport.OwnerIsActive = _ => true;

        try
        {
            using TrackBar bar = new TrackBar { ShowValuePopup = true, SmallChange = 0.25 };
            using Form host = OffscreenForm.Create(new System.Drawing.Size(100, 20), System.Drawing.Color.Black);
            bar.SetBounds(0, 0, 100, 20);
            host.Controls.Add(bar);
            host.Show();
            Application.DoEvents();

            LanguageTestHelper.RunWithLanguage(UILanguage.English, () => bar.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Right)));
            Assert.Equal("0.25", bar.FormatValue(bar.Value));

            TestPictures.Pump(80);

            LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
            {
                bar.InvokePrivate("OnKeyDown", new KeyEventArgs(Keys.Right));
                Assert.Equal("0,5", bar.FormatValue(bar.Value));
            });
        }
        finally
        {
            ErikwnkWFUI.Helpers.PopupSupport.OwnerIsActive = before;
        }
    }
}
