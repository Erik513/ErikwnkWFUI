using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>
/// The popup has no text of its own - what it shows comes from the caller - so there is
/// nothing to translate in it. What a language switch can still touch is how the caller's text
/// reaches it: content from a function is asked for again each time the popup shows, a text
/// that was handed over stays as it was, and longer German text wraps instead of growing
/// without end.
/// </summary>
[Collection(LanguageTestCollection.Name)]
public class InfoPopupLanguageTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Func<Form, bool> _originalActiveCheck = PopupSupport.OwnerIsActive;

    public InfoPopupLanguageTests()
    {
        PopupSupport.OwnerIsActive = _ => true;
    }

    public void Dispose()
    {
        PopupSupport.OwnerIsActive = _originalActiveCheck;

        foreach (Form form in _forms)
            form.Dispose();
    }

    private (InfoPopupForm Popup, Button Anchor) Create()
    {
        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(100, 100),
            ShowInTaskbar = false,
            ClientSize = new Size(400, 300)
        };
        Button anchor = new Button { Location = new Point(20, 60), Size = new Size(80, 30) };
        host.Controls.Add(anchor);
        host.Show();
        _forms.Add(host);

        InfoPopupForm popup = ErikwnkWFUI.UIStyles.InfoPopups.CreateStandard(delay: PopupDelay.Instant);
        _forms.Add(popup);

        return (popup, anchor);
    }

    private static string Shown(InfoPopupForm popup)
    {
        InfoPopupSection[] sections = popup.GetPrivateField<InfoPopupSection[]>("_sections")!;

        return string.Join("|", sections.Select(section => section.Text));
    }

    [Fact]
    public void TheContentOfAFunction_IsAskedForAgain_SoTheNewLanguageShowsAtTheNextShow()
    {
        (InfoPopupForm popup, Button anchor) = Create();
        popup.AttachTo(anchor, () => new[] { new InfoPopupSection(null, UIStrings.Get("MessageBox.Cancel")) });

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            anchor.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));
            Assert.Equal("Cancel", Shown(popup));
            anchor.InvokePrivate("OnMouseLeave", EventArgs.Empty);
        });

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            anchor.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));
            Assert.Equal("Abbrechen", Shown(popup));
            anchor.InvokePrivate("OnMouseLeave", EventArgs.Empty);
        });
    }

    [Fact]
    public void ATextThatWasHandedOver_StaysAsItWas_UntilTheCallerSetsANewOne()
    {
        (InfoPopupForm popup, Button anchor) = Create();
        string english = "";

        LanguageTestHelper.RunWithLanguage(UILanguage.English, () =>
        {
            english = UIStrings.Get("MessageBox.Cancel");
            popup.AttachTo(anchor, english);
        });

        LanguageTestHelper.RunWithLanguage(UILanguage.German, () =>
        {
            anchor.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));
            Assert.Equal(english, Shown(popup));

            popup.Detach(anchor);
            popup.AttachTo(anchor, UIStrings.Get("MessageBox.Cancel"));
            anchor.InvokePrivate("OnMouseLeave", EventArgs.Empty);
            anchor.InvokePrivate("OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 5, 0));
            Assert.Equal("Abbrechen", Shown(popup));
        });
    }

    [Fact]
    public void ALongGermanWord_AndALongGermanSentence_WrapAtTheMaximumWidth()
    {
        (InfoPopupForm popup, Button anchor) = Create();

        popup.SetContent("Donaudampfschifffahrtsgesellschaftskapitaensmuetzenabzeichenhersteller");
        int word = popup.Width;
        popup.SetContent("Die Geschwindigkeitsbegrenzung auf Bundesautobahnen ist ein haeufig diskutiertes Thema in der Verkehrspolitik und in der Bevoelkerung.");
        int sentence = popup.Width;

        Assert.True(word <= 400 && sentence <= 400, "the popup grew without end: " + word + ", " + sentence);
        Assert.True(popup.Height > 60, "the sentence did not wrap");
    }
}
