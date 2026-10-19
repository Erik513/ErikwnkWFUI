using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using VolumeControl = ErikwnkWFUI.Controls.VolumeControl;

namespace ErikwnkWFUI.Tests.Controls.VolumeControls;

/// <summary>The texts of the volume control (the name of the button, the name of the slider) are in the language of the library, also when it is switched while the control is shown. [Collection]: the language is static state of the process.</summary>
[Collection(LanguageTestCollection.Name)]
public class VolumeControlLanguageTests
{
    private static void Click(VolumeControl control) => control.MuteButton.InvokePrivate("OnClick", EventArgs.Empty);

    [Fact]
    public void EveryText_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[] { "VolumeControl.Mute", "VolumeControl.Unmute", "VolumeControl.Volume" });
    }

    [Fact]
    public void TheTooltipOfTheButton_IsInTheLanguage_AndNamesTheKey()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
        using Form host = OffscreenForm.Create(new Size(150, 30), Color.Black);
        host.Controls.Add(control);
        host.Show();
        Application.DoEvents();

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            Assert.Equal("Stumm schalten (M)", ButtonToolTipReader.Text(control.MuteButton));

            Click(control);
            Assert.Equal("Ton einschalten (M)", ButtonToolTipReader.Text(control.MuteButton));
        });
    }

    [Fact]
    public void TheNames_FollowTheLanguage_AlsoWhenItIsSwitchedWhileTheControlIsShown()
    {
        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.5);
            using Form host = OffscreenForm.Create(new Size(150, 30), Color.Black);
            host.Controls.Add(control);
            host.Show();
            Application.DoEvents();

            Assert.Equal("Mute", control.MuteButton.AccessibleName);
            Assert.Equal("Volume", control.Slider.AccessibleName);

            LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
            {
                Assert.Equal("Stumm schalten", control.MuteButton.AccessibleName);
                Assert.Equal("Lautstärke", control.Slider.AccessibleName);

                Click(control);
                Assert.Equal("Ton einschalten", control.MuteButton.AccessibleName);
            });

            // Back in English, while muted.
            Assert.Equal("Unmute", control.MuteButton.AccessibleName);
            Assert.Equal("Volume", control.Slider.AccessibleName);
        });
    }

    [Fact]
    public void ThePercentageOfTheSlider_IsWrittenInTheLanguage()
    {
        using VolumeControl control = UIStyles.VolumeControls.CreateStandard(0.42);

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                string expected = (0.42).ToString("P0", ErikwnkWFUI.Localization.UIStrings.NumberCulture);

                Assert.Equal(expected, control.Slider.AccessibilityObject.Value);
            });
        }
    }
}
