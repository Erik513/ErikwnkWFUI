using System.Drawing;
using System.Windows.Forms;
using ErikwnkCore;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The input box: a small dialog with a prompt, one text box and OK / Cancel.</summary>
[Collection(AccentColorTestCollection.Name)]
public class InputBoxTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        AppLocalization.Language = AppLanguage.English;
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Button OkButton(InputBoxForm form)
    {
        return form.GetPrivateField<Button>("_okButton")!;
    }

    private static Button CancelButton(InputBoxForm form)
    {
        return form.GetPrivateField<Button>("_cancelButton")!;
    }

    // ---- the factories

    [Fact]
    public void TheFactories_MakeAFixedDialog_WithTheTitleOfTheLanguage_OrTheOneGiven()
    {
        InputBoxForm standard = _host.Add(UIStyles.Forms.CreateInputStandard("Name?"));
        InputBoxForm primary = _host.Add(UIStyles.Forms.CreateInputPrimary("Name?", "Your name"));

        Assert.Equal("Input", standard.Text);
        Assert.Equal("Your name", primary.Text);
        Assert.Equal(standard.Size, standard.MinimumSize);
        Assert.Equal(standard.Size, standard.MaximumSize);
        Assert.False(standard.TitleBar.ShowMaximizeButton);
        Assert.False(standard.TitleBar.ShowMinimizeButton);
        Assert.Equal("Name?", standard.Prompt);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Eingabe", UIStyles.Forms.CreateInputStandard("Name?").Text);
        Assert.Equal("Eingabe", UIStyles.Forms.CreateInputPrimary("Name?").Text);
    }

    [Fact]
    public void TheTitleBar_TakesAColorSet_OrTheAccentWithPrimary()
    {
        InputBoxForm plain = _host.Show(UIStyles.Forms.CreateInputStandard("x"));
        InputBoxForm yellow = _host.Show(UIStyles.Forms.CreateInputStandard("x", colors: UIStyles.Colors.Sets.Yellow));
        InputBoxForm primary = _host.Show(UIStyles.Forms.CreateInputPrimary("x"));

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), plain.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), yellow.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), primary.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void TheBox_StartsWithTheText_SelectedWhenTheWindowOpens_AndHidesWhatIsTypedForAPassword()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", text: "Erik"));
        InputBoxForm secret = _host.Show(UIStyles.Forms.CreateInputStandard("Password?", password: true));

        Assert.Equal("Erik", form.InputTextBox.Text);
        Assert.Equal("Erik", form.InputTextBox.SelectedText);
        Assert.True(secret.InputTextBox.UseSystemPasswordChar);
        Assert.False(form.InputTextBox.UseSystemPasswordChar);
    }

    // ---- OK and Cancel

    [Fact]
    public void OK_TakesTheText_AndClosesWithOK()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));
        form.InputTextBox.Text = "  Erik ";

        OkButton(form).PerformClick();

        Assert.Equal("  Erik ", form.Value);
        Assert.Equal(DialogResult.OK, form.DialogResult);
    }

    [Fact]
    public void AnEmptyText_IsNotTaken_UnlessAllowed_AndTheBoxGetsTheFocus()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));

        OkButton(form).PerformClick();

        Assert.NotEqual(DialogResult.OK, form.DialogResult);
        Assert.False(form.IsDisposed);

        form.AllowEmpty = true;
        OkButton(form).PerformClick();

        Assert.Equal(DialogResult.OK, form.DialogResult);
        Assert.Equal("", form.Value);
    }

    [Fact]
    public void Cancel_LeavesTheValueEmpty_AndClosesWithCancel()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", text: "Erik"));
        form.InputTextBox.Text = "Anna";

        CancelButton(form).PerformClick();

        Assert.Equal("", form.Value);
        Assert.Equal(DialogResult.Cancel, form.DialogResult);
    }

    [Fact]
    public void EnterConfirms_AndEscapeCancels()
    {
        InputBoxForm entered = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", text: "Erik"));
        Assert.True(entered.InvokePrivate<bool>("ProcessDialogKey", Keys.Enter));
        Assert.Equal(DialogResult.OK, entered.DialogResult);
        Assert.Equal("Erik", entered.Value);

        InputBoxForm escaped = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", text: "Erik"));
        Assert.True(escaped.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape));
        Assert.Equal(DialogResult.Cancel, escaped.DialogResult);
    }

    // ---- hint, message, validation

    private static Label LabelOf(InputBoxForm form, string field)
    {
        return form.GetPrivateField<Label>(field)!;
    }

    [Fact]
    public void TheHint_IsSmallGrayText_UnderThePrompt_AndTakesNoRoomWhenThereIsNone()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Password?"));
        int without = form.Height;
        Label hint = LabelOf(form, "_hintLabel");
        Assert.False(hint.Visible);

        form.Hint = "It was sent to you by mail.";

        Assert.True(hint.Visible);
        Assert.True(form.Height > without);
        Assert.True(hint.Top >= LabelOf(form, "_promptLabel").Bottom);
        Assert.True(hint.Bottom <= form.InputTextBox.Top);
        Assert.Equal("It was sent to you by mail.", form.Hint);

        form.Hint = null;
        Assert.False(hint.Visible);
        Assert.Equal(without, form.Height);
    }

    [Fact]
    public void TheErrorText_IsRed_OverTheBox_AndGoesWhenTheUserTypes()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Password?"));
        int without = form.Height;
        Label error = LabelOf(form, "_errorLabel");

        form.ErrorText = "Wrong password.";

        Assert.True(error.Visible);
        Assert.True(form.Height > without);
        Assert.True(error.Bottom <= form.InputTextBox.Top);
        Assert.True(error.ForeColor.R > error.ForeColor.G + 40, "red");

        form.InputTextBox.Text = "x";

        Assert.Equal("", form.ErrorText);
        Assert.False(error.Visible);
        Assert.Equal(without, form.Height);
    }

    [Fact]
    public void WithHintAndErrorAndAPrompt_NothingOverlaps_AndEverythingIsInsideTheWindow()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard(string.Join(" ", Enumerable.Repeat("A prompt that needs more than one line.", 4))));
        form.Hint = string.Join(" ", Enumerable.Repeat("A hint that wraps too.", 5));
        form.ErrorText = string.Join(" ", Enumerable.Repeat("An error that wraps as well.", 4));

        Control[] order = { LabelOf(form, "_promptLabel"), LabelOf(form, "_hintLabel"), LabelOf(form, "_errorLabel"), form.InputTextBox, OkButton(form) };

        for (int i = 1; i < order.Length; i++)
            Assert.True(order[i].Top >= order[i - 1].Bottom, order[i].GetType().Name + " overlaps the one above");

        Assert.True(form.ContentPanel.ClientRectangle.Contains(OkButton(form).Bounds));
    }

    [Fact]
    public void TheValidator_AcceptsOrRejectsWithAMessage_TheWindowStaysOpen_AndTheTextIsSelected()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Code?"));
        form.Validator = text => text.Length == 6 ? null : "The code has 6 digits.";
        form.InputTextBox.Text = "123";

        OkButton(form).PerformClick();

        Assert.NotEqual(DialogResult.OK, form.DialogResult);
        Assert.Equal("The code has 6 digits.", form.ErrorText);
        Assert.Equal("123", form.InputTextBox.SelectedText);
        Assert.Equal("", form.Value);

        form.InputTextBox.Text = "123456";
        Assert.Equal("", form.ErrorText);
        OkButton(form).PerformClick();

        Assert.Equal(DialogResult.OK, form.DialogResult);
        Assert.Equal("123456", form.Value);
    }

    [Fact]
    public void TheValidator_IsNotAskedForAnEmptyText_UnlessItIsAllowed_AndAnEmptyMessageCountsAsAccepted()
    {
        int asked = 0;
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Code?"));
        form.Validator = text => { asked++; return ""; };

        OkButton(form).PerformClick();
        Assert.Equal(0, asked);

        form.InputTextBox.Text = "a";
        OkButton(form).PerformClick();

        Assert.Equal(1, asked);
        Assert.Equal(DialogResult.OK, form.DialogResult);
    }

    [Fact]
    public void TrimValue_CutsTheSpaces_ForTheValueAndTheValidator_AndSpacesAloneAreEmpty()
    {
        string? seen = null;
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Code?"));
        form.TrimValue = true;
        form.Validator = text => { seen = text; return null; };

        form.InputTextBox.Text = "    ";
        OkButton(form).PerformClick();
        Assert.Null(seen);
        Assert.NotEqual(DialogResult.OK, form.DialogResult);

        form.InputTextBox.Text = "  12345 ";
        OkButton(form).PerformClick();

        Assert.Equal("12345", seen);
        Assert.Equal("12345", form.Value);
    }

    [Fact]
    public void WithoutTrim_ThePasswordKeepsItsSpaces()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Password?", password: true));
        form.InputTextBox.Text = " pass word ";

        OkButton(form).PerformClick();

        Assert.Equal(" pass word ", form.Value);
    }

    [Fact]
    public void MaxLength_LimitsTheBox_AndZeroIsNoLimit()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Code?"));
        Assert.Equal(0, form.MaxLength);

        form.MaxLength = 6;
        Assert.Equal(6, form.InputTextBox.MaxLength);
        Assert.Equal(6, form.MaxLength);

        form.MaxLength = 0;
        Assert.Equal(0, form.MaxLength);
        Assert.True(form.InputTextBox.MaxLength > 100);
    }

    [Fact]
    public void ThePlaceholder_IsHandedToTheBox()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Code?", placeholder: "12345"));
        InputBoxForm primary = _host.Show(UIStyles.Forms.CreateInputPrimary("Code?", placeholder: "12345"));

        foreach (InputBoxForm shown in new[] { form, primary })
            Assert.Equal("12345", ((ErikwnkWFUI.Controls.TextField)shown.InputTextBox).Placeholder);
    }
    // ---- layout

    [Fact]
    public void ALongPrompt_WrapsAndMakesTheWindowHigher_AndTheBoxAndTheButtonsStayInside()
    {
        InputBoxForm shortPrompt = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));
        InputBoxForm longPrompt = _host.Show(UIStyles.Forms.CreateInputStandard(string.Join(" ", Enumerable.Repeat("A prompt that needs more than one line.", 6))));

        Assert.True(longPrompt.Height > shortPrompt.Height);
        Assert.Equal(shortPrompt.Width, longPrompt.Width);

        foreach (InputBoxForm form in new[] { shortPrompt, longPrompt })
        {
            Rectangle client = form.ContentPanel.ClientRectangle;

            foreach (Control control in form.ContentPanel.Controls)
                Assert.True(client.Contains(control.Bounds), control.GetType().Name + " " + control.Bounds + " is not inside " + client);

            Assert.True(OkButton(form).Left > CancelButton(form).Right);
            Assert.True(form.InputTextBox.Bottom < OkButton(form).Top);
        }
    }

    [Fact]
    public void ChangingThePrompt_ResizesTheWindow()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));
        int before = form.Height;

        form.Prompt = string.Join(" ", Enumerable.Repeat("A prompt that needs more than one line.", 6));
        int taller = form.Height;
        form.Prompt = "";

        Assert.True(taller > before);
        Assert.True(form.Height < before);
        Assert.Equal(form.Size, form.MaximumSize);
    }

    [Fact]
    public void OnAnotherDpi_EverythingIsScaled()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));
        int width96 = form.ClientSize.Width;
        int button96 = OkButton(form).Height;

        form.DpiOverride = 192;

        Assert.Equal(width96 * 2, form.ClientSize.Width);
        Assert.Equal(button96 * 2, OkButton(form).Height);
    }

    // ---- language

    [Fact]
    public void TheButtons_AreInTheLanguage_AndFollowAChangeWhileTheWindowIsOpen_ATextThatWasSetStays()
    {
        InputBoxForm form = _host.Show(UIStyles.Forms.CreateInputStandard("Name?"));
        Assert.Equal("OK", OkButton(form).Text);
        Assert.Equal("Cancel", CancelButton(form).Text);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Abbrechen", CancelButton(form).Text);
        Assert.Equal("Eingabe", form.Text);

        form.CancelButtonText = "Nein";
        Assert.Equal("Nein", CancelButton(form).Text);
        form.CancelButtonText = null;
        Assert.Equal("Abbrechen", CancelButton(form).Text);

        InputBoxForm titled = _host.Show(UIStyles.Forms.CreateInputStandard("Name?", "My title"));
        UIStrings.Language = UILanguage.English;
        Assert.Equal("My title", titled.Text);
    }

    [Fact]
    public void ADisposedWindow_ListensToTheLanguageNoMore()
    {
        System.Reflection.FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int before = ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;

        InputBoxForm form = UIStyles.Forms.CreateInputStandard("Name?");
        form.Dispose();
        form.Dispose();

        Assert.Equal(before, ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0);
    }

    // ---- both themes

    [Theory]
    [MemberData(nameof(ColorContrastTestSupport.Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void EverythingCanBeReadAndSeen_InBothThemes(bool lightTheme)
    {
        ColorContrastTestSupport.Run(lightTheme, shortfalls =>
        {
            foreach (bool primary in new[] { false, true })
            {
                InputBoxForm form = _host.Show(primary ? UIStyles.Forms.CreateInputPrimary("Name?", text: "Erik") : UIStyles.Forms.CreateInputStandard("Name?", text: "Erik"));
                string window = primary ? "primary" : "standard";
                form.Hint = "A hint";
                form.ErrorText = "An error";
                Label prompt = form.GetPrivateField<Label>("_promptLabel")!;
                Color back = prompt.BackColor.A == 0 ? form.ContentPanel.BackColor : prompt.BackColor;

                ColorContrastTestSupport.Check(shortfalls, window + ", prompt", prompt.ForeColor, back, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", hint", LabelOf(form, "_hintLabel").ForeColor, back, 3.0);
                ColorContrastTestSupport.Check(shortfalls, window + ", error", LabelOf(form, "_errorLabel").ForeColor, back, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", text", form.InputTextBox.ForeColor, form.InputTextBox.BackColor, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", OK", OkButton(form).ForeColor, OkButton(form).BackColor, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", Cancel", CancelButton(form).ForeColor, CancelButton(form).BackColor, 4.5);
            }
        });
    }
}
