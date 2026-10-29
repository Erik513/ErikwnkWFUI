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

/// <summary>More button sets, own button texts, a check box ("do not ask again"), the sound of the icon and a help button.</summary>
[Collection(AccentColorTestCollection.Name)]
public class MessageBoxExtrasTests : IDisposable
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

    // ---- more button sets

    [Fact]
    public void RetryCancel_HasTwoButtons_RetryIsEnter_CancelIsEscape_AndClosingIsCancel()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("The disk is not ready.", buttons: MessageBoxButtons.RetryCancel, icon: MessageBoxIcon.Error));

        Assert.Equal(new[] { DialogResult.Retry, DialogResult.Cancel }, Buttons(form).Select(b => b.DialogResult).ToArray());
        Assert.Equal(new[] { "Retry", "Cancel" }, Buttons(form).Select(b => b.Text).ToArray());
        Assert.Same(Buttons(form)[0], form.AcceptButton);
        Assert.Same(Buttons(form)[1], form.CancelButton);

        form.Close();
        Assert.Equal(DialogResult.Cancel, form.DialogResult);
    }

    [Fact]
    public void AbortRetryIgnore_HasThreeButtons_AbortIsRed_AndClosingIsAbort()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("An error.", buttons: MessageBoxButtons.AbortRetryIgnore, icon: MessageBoxIcon.Error));

        Assert.Equal(new[] { DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore }, Buttons(form).Select(b => b.DialogResult).ToArray());
        Assert.Same(Buttons(form)[1], form.AcceptButton);
        Assert.Same(Buttons(form)[0], form.CancelButton);

        form.Close();
        Assert.Equal(DialogResult.Abort, form.DialogResult);
    }

    [Fact]
    public void TheTextsOfTheNewButtons_FollowTheLanguage_AndTheyAreOnTheCopy()
    {
        UIStrings.Language = UILanguage.German;
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Fehler.", buttons: MessageBoxButtons.AbortRetryIgnore));

        Assert.Equal(new[] { "Abbrechen", "Wiederholen", "Ignorieren" }, Buttons(form).Select(b => b.Text).ToArray());
        Assert.Contains("Abbrechen   Wiederholen   Ignorieren", form.GetTextForCopy());
    }

    [Fact]
    public void TheButtonsOfEverySet_FitIntoTheirPlaces_InEverySize()
    {
        foreach (UILanguage language in new[] { UILanguage.English, UILanguage.German })
        foreach (FormSize size in new[] { FormSize.Small, FormSize.Medium, FormSize.Large })
        foreach (MessageBoxButtons set in new[] { MessageBoxButtons.AbortRetryIgnore, MessageBoxButtons.RetryCancel })
        {
            UIStrings.Language = language;
            MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: set, size: size));

            foreach (Button button in Buttons(form))
            {
                int needed = TextRenderer.MeasureText(button.Text, button.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
                Assert.True(needed + 4 <= button.Width, language + ", " + size + ": '" + button.Text + "' needs " + needed + " of " + button.Width);
            }
        }
    }

    // ---- own button texts

    [Fact]
    public void OwnTexts_ReplaceTheSymbolsAndTheUsualTexts_ANullEntryKeepsTheUsualOne()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("Save the changes?", buttons: MessageBoxButtons.YesNoCancel, buttonTexts: new[] { "Save", "Discard", null! }));

        Assert.Equal(new[] { "Save", "Discard", "Cancel" }, Buttons(form).Select(b => b.Text).ToArray());
        Assert.Equal(new[] { "Save", "Discard", "Cancel" }, Buttons(form).Select(b => b.AccessibleName ?? "").ToArray());
        Assert.Equal(new[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel }, Buttons(form).Select(b => b.DialogResult).ToArray());
        Assert.Contains("Save   Discard   Cancel", form.GetTextForCopy());
    }

    [Fact]
    public void TooFewOrEmptyTexts_AreNoProblem_TheRestKeepsItsUsualText()
    {
        MessageBoxForm few = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo, buttonTexts: new[] { "Delete" }));
        MessageBoxForm empty = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo, buttonTexts: new[] { "", "" }));
        MessageBoxForm none = _host.Show(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo, buttonTexts: new string[0]));

        Assert.Equal("Delete", Buttons(few)[0].Text);
        Assert.Equal(UIStyles.Symbols.Cancel, Buttons(few)[1].Text);
        Assert.Equal(UIStyles.Symbols.Check, Buttons(empty)[0].Text);
        Assert.Equal(UIStyles.Symbols.Check, Buttons(none)[0].Text);
    }

    // ---- the check box

    private static DialogResult ShowAndAct(Action<MessageBoxForm> act, Func<DialogResult> show)
    {
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 30 };
        timer.Tick += (sender, e) =>
        {
            MessageBoxForm? open = Application.OpenForms.OfType<MessageBoxForm>().FirstOrDefault();

            if (open == null)
                return;

            timer.Stop();
            act(open);
        };
        timer.Start();

        try
        {
            return show();
        }
        finally
        {
            timer.Dispose();
        }
    }

    [Fact]
    public void ShowWithCheckBox_HandsBackWhatTheUserLeftTheBoxAt_WhicheverButtonEndedIt()
    {
        bool isChecked = false;
        DialogResult result = ShowAndAct(
            form =>
            {
                CheckBox box = form.GetPrivateField<CheckBox>("_checkBox")!;
                Assert.Equal("Do not ask again", box.Text);
                Assert.False(box.Checked);
                box.Checked = true;
                form.AcceptButton!.PerformClick();
            },
            () => ErikwnkWFUI.Forms.MessageBox.ShowWithCheckBox("Delete it?", "Do not ask again", ref isChecked, buttons: MessageBoxButtons.YesNo));

        Assert.Equal(DialogResult.Yes, result);
        Assert.True(isChecked);

        // Starts as it was given, and is kept when the window is closed without a button.
        DialogResult closed = ShowAndAct(
            form => { Assert.True(form.CheckBoxChecked); form.Close(); },
            () => ErikwnkWFUI.Forms.MessageBox.ShowWithCheckBox("Delete it?", "Do not ask again", ref isChecked, buttons: MessageBoxButtons.YesNo));

        Assert.Equal(DialogResult.No, closed);
        Assert.True(isChecked);

        // And it can be taken away again.
        ShowAndAct(form => { form.CheckBoxChecked = false; form.CancelButton!.PerformClick(); },
            () => ErikwnkWFUI.Forms.MessageBox.ShowWithCheckBox("Delete it?", "Do not ask again", ref isChecked, buttons: MessageBoxButtons.YesNo));

        Assert.False(isChecked);
    }

    [Fact]
    public void TheCheckBox_HasItsOwnRow_BetweenTheMessageAndTheButtons_AndNoneWithoutText()
    {
        MessageBoxForm without = _host.Show(UIStyles.Forms.CreateMessageStandard("x"));
        MessageBoxForm with = _host.Show(UIStyles.Forms.CreateMessageStandard("x"));
        with.CheckBoxText = "Do not ask again";
        Control buttons = with.GetPrivateField<Control>("_buttonPanel")!;
        Panel message = with.GetPrivateField<Panel>("_messagePanel")!;
        CheckBox box = with.GetPrivateField<CheckBox>("_checkBox")!;

        Assert.Null(without.GetPrivateField<CheckBox>("_checkBox"));
        Assert.True(box.Visible);
        Assert.True(message.Bottom <= box.Top && box.Bottom <= buttons.Top, "the check box overlaps");
        Assert.True(with.ContentPanel.ClientRectangle.Contains(box.Bounds));

        with.CheckBoxText = "";
        Assert.False(box.Visible);
        Assert.Equal(without.Height, with.Height);
    }

    // ---- the sound and the help button

    [Fact]
    public void TheSoundIsOffByDefault_AndTheHelpButtonToo()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", icon: MessageBoxIcon.Error));

        Assert.False(form.PlaySound);
        Assert.False(form.ShowHelpButton);
        Assert.False(form.GetPrivateField<Button>("_helpButton")!.Visible);
    }

    [Fact]
    public void TheHelpButton_RaisesTheEvent_AndTheDialogStaysOpen_AlongsideTheDetails()
    {
        MessageBoxForm form = _host.Show(UIStyles.Forms.CreateMessageStandard("x", details: "d"));
        Button help = form.GetPrivateField<Button>("_helpButton")!;
        Button details = form.GetPrivateField<Button>("_detailsButton")!;
        int raised = 0;
        form.HelpClicked += (sender, e) => raised++;

        form.ShowHelpButton = true;
        help.PerformClick();
        help.PerformClick();

        Assert.True(help.Visible);
        Assert.Equal(2, raised);
        Assert.False(form.IsDisposed);
        Assert.NotEqual(DialogResult.OK, form.DialogResult);
        Assert.Equal("Help", help.AccessibleName);
        Assert.True(help.Right <= details.Left + 1, "help " + help.Bounds + ", details " + details.Bounds);
    }

    [Fact]
    public void TheNewParameters_ReachTheFormFromShow()
    {
        string[]? texts = null;
        DialogResult result = ShowAndAct(
            form => { texts = Buttons(form).Select(b => b.Text).ToArray(); form.AcceptButton!.PerformClick(); },
            () => ErikwnkWFUI.Forms.MessageBox.Show("Save?", null!, MessageBoxButtons.YesNo, buttonTexts: new[] { "Save", "Discard" }));

        Assert.Equal(DialogResult.Yes, result);
        Assert.Equal(new[] { "Save", "Discard" }, texts);
    }
}
