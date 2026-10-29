using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using MessageBoxButtons = ErikwnkWFUI.Forms.MessageBoxButtons;
using MessageBoxIcon = ErikwnkWFUI.Forms.MessageBoxIcon;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>German and English, and a change of the language while the box is open.</summary>
[Collection(AccentColorTestCollection.Name)]
public class MessageBoxLanguageTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;
    }

    private static List<Button> Buttons(MessageBoxForm form)
    {
        return form.GetPrivateField<List<Button>>("_dialogButtons")!;
    }

    [Fact]
    public void EveryTextOfTheBox_ExistsInBothLanguages_AndTheGermanOnesAreGerman()
    {
        string[] keys = { "Title", "Ok", "Yes", "No", "Cancel", "Retry", "Abort", "Ignore", "Details", "Help" };

        foreach (UILanguage language in new[] { UILanguage.English, UILanguage.German })
        {
            UIStrings.Language = language;

            foreach (string key in keys)
                Assert.NotEqual("MessageBox." + key, UIStrings.Get("MessageBox." + key));
        }

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Hinweis", UIStrings.Get("MessageBox.Title"));
        Assert.Equal("Ja", UIStrings.Get("MessageBox.Yes"));
        Assert.Equal("Nein", UIStrings.Get("MessageBox.No"));
        Assert.Equal("Wiederholen", UIStrings.Get("MessageBox.Retry"));
        Assert.Equal("Ignorieren", UIStrings.Get("MessageBox.Ignore"));
        Assert.Equal("Hilfe", UIStrings.Get("MessageBox.Help"));
    }

    [Fact]
    public void AChangeOfTheLanguage_WhileTheBoxIsOpen_ChangesTheTitle_TheButtonsAndTheirNames_TheDetailsAndHelp()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.AbortRetryIgnore, details: "d"));
        form.ShowHelpButton = true;
        Assert.Equal("Notice", form.Text);

        UIStrings.Language = UILanguage.German;

        Assert.Equal("Hinweis", form.Text);
        Assert.Equal(new[] { "Abbrechen", "Wiederholen", "Ignorieren" }, Buttons(form).Select(b => b.Text).ToArray());
        Assert.Equal(new[] { "Abbrechen", "Wiederholen", "Ignorieren" }, Buttons(form).Select(b => b.AccessibleName ?? "").ToArray());
        Assert.Equal("Hilfe", form.GetPrivateField<Button>("_helpButton")!.AccessibleName);
        Assert.StartsWith("Details", form.GetPrivateField<Button>("_detailsButton")!.Text);
        Assert.Contains("Abbrechen   Wiederholen   Ignorieren", form.GetTextForCopy());

        UIStrings.Language = UILanguage.English;

        Assert.Equal("Notice", form.Text);
        Assert.Equal(new[] { "Abort", "Retry", "Ignore" }, Buttons(form).Select(b => b.Text).ToArray());
    }

    [Fact]
    public void ASymbolButton_KeepsItsSymbol_ButItsNameFollowsTheLanguage()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo));

        UIStrings.Language = UILanguage.German;

        Assert.Equal(UIStyles.Symbols.Check, Buttons(form)[0].Text);
        Assert.Equal("Ja", Buttons(form)[0].AccessibleName);
        Assert.Equal("Nein", Buttons(form)[1].AccessibleName);
    }

    [Fact]
    public void ATitleAndButtonTextsThatWereGiven_StayWhenTheLanguageChanges()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", "My title", MessageBoxButtons.YesNoCancel, buttonTexts: new[] { "Save", null!, "Stop" }));

        UIStrings.Language = UILanguage.German;

        Assert.Equal("My title", form.Text);
        Assert.Equal("Save", Buttons(form)[0].Text);
        Assert.Equal(UIStyles.Symbols.Cancel, Buttons(form)[1].Text);
        Assert.Equal("Nein", Buttons(form)[1].AccessibleName);
        Assert.Equal("Stop", Buttons(form)[2].Text);
    }

    [Fact]
    public void ThePrimaryBoxAndTheOldConstructor_FollowTheLanguageToo_ButOnlyWithTheStandardTitle()
    {
        MessageBoxForm primary = _host.Show(UIStyles.Forms.CreateMessagePrimary("x"));
        MessageBoxForm old = _host.Show(new MessageBoxForm("x", null!, MessageBoxButtons.OK, MessageBoxIcon.Info));
        MessageBoxForm named = _host.Show(new MessageBoxForm("x", "Named", MessageBoxButtons.OK, MessageBoxIcon.Info));

        UIStrings.Language = UILanguage.German;

        Assert.Equal("Hinweis", primary.Text);
        Assert.Equal("Hinweis", old.Text);
        Assert.Equal("Named", named.Text);
    }

    [Fact]
    public void AfterTheChange_TheButtonsStillFitAndTheWindowStillHoldsThemAll()
    {
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        {
            MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.AbortRetryIgnore, details: "d", size: size));
            form.ShowHelpButton = true;

            UIStrings.Language = UILanguage.German;

            foreach (Button button in Buttons(form))
            {
                int needed = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
                Assert.True(needed + 4 <= button.Width, size + ": '" + button.Text + "' needs " + needed + " of " + button.Width);
            }

            Control row = form.GetPrivateField<Control>("_buttonPanel")!;
            Assert.True(row.Right <= form.ContentPanel.ClientSize.Width + 1);

            UIStrings.Language = UILanguage.English;
        }
    }

    [Fact]
    public void ADisposedBox_ListensToTheLanguageNoMore()
    {
        System.Reflection.FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int before = ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;

        MessageBoxForm form = UIStyles.Forms.CreateMessageStandard("x");
        form.Dispose();
        form.Dispose();

        Assert.Equal(before, ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0);
        UIStrings.Language = UILanguage.German;
    }
}
