using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>The text of the switch (what a screen reader is told it does) is in the language of the library. [Collection]: the language is static state of the process.</summary>
[Collection(LanguageTestCollection.Name)]
public class ToggleSwitchLanguageTests
{
    [Fact]
    public void EveryText_IsTranslatedForEveryLanguage()
    {
        LanguageTestHelper.AssertAllTranslatedForEveryLanguage(new[] { "ToggleSwitch.Toggle" });
    }

    [Fact]
    public void TheDefaultAction_IsInTheLanguage()
    {
        using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreateStandard(false);
        using Form form = OffscreenForm.Create(new Size(60, 40), Color.Black);
        form.Controls.Add(toggle);
        form.Show();

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () => Assert.Equal("Switch", toggle.AccessibilityObject.DefaultAction));
        LanguageTestHelper.RunWithLanguage(UILanguage.German, () => Assert.Equal("Umschalten", toggle.AccessibilityObject.DefaultAction));
    }
}
