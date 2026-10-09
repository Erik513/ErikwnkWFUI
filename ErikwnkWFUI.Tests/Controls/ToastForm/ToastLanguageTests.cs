using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ToastFormTests;

/// <summary>
/// The toast has no text of its own - the message and the title come from the caller, the
/// symbols are signs - so there is nothing to translate in it. What a language switch can still
/// touch: a text that was handed over stays as it was (also while the toast waits), and longer
/// German text wraps and ends in an ellipsis instead of growing the toast without end.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class ToastLanguageTests : ToastTestBase
{
    [Fact]
    public void ATextThatWasHandedOver_IsShownAsItIs_InEveryLanguage()
    {
        Form host = Host();

        foreach (UILanguage language in LanguageTestHelper.AllLanguages)
        {
            LanguageTestHelper.RunWithLanguage(language, () =>
            {
                ToastForm.ShowToast(new InfoPopupSection("Titel " + language, "Nachricht & Text " + language), host, icon: ToastIcon.Info);

                Assert.Equal("Titel " + language, Part("_titleLabel").Text);
                Assert.Equal("Nachricht & Text " + language, Part("_messageLabel").Text);
            });
        }
    }

    [Fact]
    public void TheSymbols_AreTheSameInEveryLanguage()
    {
        Form host = Host();
        Dictionary<ToastIcon, string> english = new Dictionary<ToastIcon, string>();

        foreach (ToastIcon icon in new[] { ToastIcon.Success, ToastIcon.Info, ToastIcon.Warning, ToastIcon.Error })
        {
            LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
            {
                ToastForm.ShowToast("x", host, icon: icon);
                english[icon] = Part("_iconLabel").Text;
            });

            foreach (UILanguage language in LanguageTestHelper.AllLanguages)
            {
                LanguageTestHelper.RunWithLanguage(language, () =>
                {
                    ToastForm.ShowToast("x", host, icon: icon);

                    Assert.Equal(english[icon], Part("_iconLabel").Text);
                });
            }
        }
    }

    [Fact]
    public void AToastThatWaits_KeepsItsText_WhenTheLanguageChangesMeanwhile()
    {
        Form host = Host();
        string english = "";

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            english = UIStrings.Get("MessageBox.Cancel");
            PopupSupport.OwnerIsActive = _ => false;
            ToastForm.ShowToast(english, host);
            PopupSupport.OwnerIsActive = _ => true;
        });

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            typeof(ToastForm).GetMethod("OnOwnerActivated", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { host, EventArgs.Empty });

            Assert.Equal(english, Part("_messageLabel").Text);
            Assert.NotEqual(UIStrings.Get("MessageBox.Cancel"), Part("_messageLabel").Text);
        });
    }

    [Fact]
    public void ALongGermanWord_AndALongGermanSentence_WrapOrEndInAnEllipsis_TheWidthStays()
    {
        Form host = Host();

        ToastForm.ShowToast("Kurz", host);
        int width = Current()!.Width;

        ToastForm.ShowToast("Donaudampfschifffahrtsgesellschaftskapitaensmuetzenabzeichenhersteller", host);
        Size word = Current()!.Size;

        ToastForm.ShowToast("Die Geschwindigkeitsbegrenzung auf Bundesautobahnen ist ein haeufig diskutiertes Thema in der Verkehrspolitik und in der Bevoelkerung, und niemand weiss genau, wann sie endet.", host);
        Size sentence = Current()!.Size;

        Assert.Equal(width, word.Width);
        Assert.Equal(width, sentence.Width);
        Assert.True(sentence.Height > 80, "the sentence did not make the toast higher");
        Assert.True(Part("_messageLabel").AutoEllipsis);
    }

    [Fact]
    public void ALongGermanTitle_StaysOneLine()
    {
        Form host = Host();

        ToastForm.ShowToast(new InfoPopupSection("Donaudampfschifffahrtsgesellschaftskapitaensmuetzenabzeichenhersteller", "Text"), host);

        Assert.True(Part("_titleLabel").Height < 2 * TextRenderer.MeasureText("Ag", Part("_titleLabel").Font).Height);
        Assert.True(Part("_titleLabel").AutoEllipsis);
    }
}
