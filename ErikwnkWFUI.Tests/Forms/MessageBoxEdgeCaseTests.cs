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

/// <summary>Unusual input and unusual order of events: empty and odd texts, every set with every icon, keys, a second click, a handler that closes the window.</summary>
[Collection(AccentColorTestCollection.Name)]
public class MessageBoxEdgeCaseTests : IDisposable
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

    private static Label MessageLabel(MessageBoxForm form)
    {
        return form.GetPrivateField<Label>("_messageLabel")!;
    }

    [Fact]
    public void NoText_AnEmptyText_AndOnlyBlanks_MakeAnUsableWindow()
    {
        foreach (string? message in new[] { null, "", "   ", "\n\n" })
        {
            MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard(message!));

            Assert.True(form.ContentPanel.ClientRectangle.Contains(Buttons(form)[0].Bounds));
            Assert.Equal(DialogResult.OK, ClickAccept(form));
        }
    }

    private static DialogResult ClickAccept(MessageBoxForm form)
    {
        ((Button)form.AcceptButton!).PerformClick();

        return form.DialogResult;
    }

    [Fact]
    public void EverySet_WithEveryIcon_AndEverySize_Builds_AndKeepsTheButtonsInsideTheWindow()
    {
        foreach (MessageBoxButtons set in Enum.GetValues(typeof(MessageBoxButtons)))
        foreach (MessageBoxIcon icon in Enum.GetValues(typeof(MessageBoxIcon)))
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        {
            MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Text", buttons: set, icon: icon, size: size));

            foreach (Button button in Buttons(form))
                Assert.True(form.ContentPanel.ClientRectangle.Contains(button.Bounds), set + ", " + icon + ", " + size + ": " + button.Text + " " + button.Bounds);
        }
    }

    [Fact]
    public void AWordWithoutSpaces_ThatIsWiderThanTheWindow_DoesNotBreakTheLayout()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard(new string('W', 400)));
        Panel message = form.GetPrivateField<Panel>("_messagePanel")!;
        Control buttons = form.GetPrivateField<Control>("_buttonPanel")!;

        Assert.True(message.Bottom <= buttons.Top);
        Assert.True(form.ContentPanel.ClientRectangle.Contains(Buttons(form)[0].Bounds));
    }

    [Fact]
    public void LineBreaksTabsAndAmpersands_AreShownAsTheyAre()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Fish & Chips\r\nline two\tTab"));

        Assert.Equal("Fish & Chips\r\nline two\tTab", MessageLabel(form).Text);
        Assert.False(MessageLabel(form).UseMnemonic);
        Assert.Contains("Fish & Chips", form.GetTextForCopy());
    }

    [Fact]
    public void AnAmpersandInAnOwnButtonText_IsAKeyAsUsual_AndADoubleOneIsALetter()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo, buttonTexts: new[] { "Save && Close", "Q&A" }));

        Assert.Equal("Save && Close", Buttons(form)[0].Text);
        Assert.Contains("Save && Close", form.GetTextForCopy());
        Assert.True(Buttons(form)[0].UseMnemonic);
    }

    [Theory]
    [InlineData(MessageBoxButtons.OK, DialogResult.OK, DialogResult.OK)]
    [InlineData(MessageBoxButtons.OKCancel, DialogResult.OK, DialogResult.Cancel)]
    [InlineData(MessageBoxButtons.YesNo, DialogResult.Yes, DialogResult.No)]
    [InlineData(MessageBoxButtons.YesNoCancel, DialogResult.Yes, DialogResult.Cancel)]
    [InlineData(MessageBoxButtons.RetryCancel, DialogResult.Retry, DialogResult.Cancel)]
    [InlineData(MessageBoxButtons.AbortRetryIgnore, DialogResult.Retry, DialogResult.Abort)]
    public void EnterAndEscape_AreTheAcceptAndTheCancelButton_OfEverySet(MessageBoxButtons set, DialogResult enter, DialogResult escape)
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: set));

        Assert.Equal(enter, ((Button)form.AcceptButton!).DialogResult);
        Assert.Equal(escape, ((Button)form.CancelButton!).DialogResult);
    }

    [Fact]
    public void ASecondClick_AfterTheFirstOne_DoesNotChangeTheResult()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo));
        Button yes = Buttons(form)[0];
        Button no = Buttons(form)[1];

        yes.PerformClick();
        no.PerformClick();

        Assert.Equal(DialogResult.Yes, form.DialogResult);
    }

    [Fact]
    public void AHandlerOfTheForm_ThatClosesIt_OrADisposedButton_IsNoProblem()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.OKCancel));
        form.FormClosing += (sender, e) => e.Cancel = false;
        Button ok = Buttons(form)[0];

        ok.PerformClick();
        form.Dispose();

        Assert.True(form.IsDisposed);
        Assert.Equal(DialogResult.OK, form.DialogResult);
    }

    [Fact]
    public void TheDetailsWithOddText_AreKept_AndTheSizeChangesOfTheTextDoNotLoseThem()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", details: "a\r\n\r\n\tb & c"));

        form.Details = "";
        Assert.False(form.GetPrivateField<Button>("_detailsButton")!.Visible);

        form.Details = null!;
        Assert.False(form.GetPrivateField<Button>("_detailsButton")!.Visible);

        form.Details = "again";
        Assert.True(form.GetPrivateField<Button>("_detailsButton")!.Visible);
    }

    [Fact]
    public void ALongCheckBoxText_StaysInsideTheWindow()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x"));
        form.CheckBoxText = new string('m', 200);
        CheckBox box = form.GetPrivateField<CheckBox>("_checkBox")!;

        Assert.True(form.ContentPanel.ClientRectangle.Contains(box.Bounds), box.Bounds.ToString());
    }

    [Fact]
    public void ALiveThemeChange_RecolorsTheMessageBox()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", details: "d"));
        form.CheckBoxText = "c";
        Color before = form.ContentPanel.BackColor;

        try
        {
            UIStyles.Colors.ApplyTheme(UIThemes.Light);
            Application.DoEvents();

            Assert.NotEqual(before, form.ContentPanel.BackColor);
            Assert.Equal(UIColors.BackgroundMedium, form.ContentPanel.BackColor);
        }
        finally
        {
            UIStyles.Colors.ApplyTheme(UIThemes.Dark);
        }
    }

    [Fact]
    public void TheCopy_OfAFormWithEverything_HasAllParts_InTheOrderOfWindows()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Message", "Title", MessageBoxButtons.YesNo, details: "More"));
        form.CheckBoxText = "Do not ask again";
        string copy = form.GetTextForCopy();

        Assert.True(copy.IndexOf("Title") < copy.IndexOf("Message"));
        Assert.True(copy.IndexOf("Message") < copy.IndexOf("More"));
        Assert.True(copy.IndexOf("More") < copy.IndexOf("Yes"));
    }
}
