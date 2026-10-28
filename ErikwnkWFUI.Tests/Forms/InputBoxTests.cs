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
                Label prompt = form.GetPrivateField<Label>("_promptLabel")!;
                Color back = prompt.BackColor.A == 0 ? form.ContentPanel.BackColor : prompt.BackColor;

                ColorContrastTestSupport.Check(shortfalls, window + ", prompt", prompt.ForeColor, back, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", text", form.InputTextBox.ForeColor, form.InputTextBox.BackColor, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", OK", OkButton(form).ForeColor, OkButton(form).BackColor, 4.5);
                ColorContrastTestSupport.Check(shortfalls, window + ", Cancel", CancelButton(form).ForeColor, CancelButton(form).BackColor, 4.5);
            }
        });
    }
}
