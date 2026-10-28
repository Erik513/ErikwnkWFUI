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

/// <summary>The message box made by UIStyles.Forms: Standard and Primary, with a color set for the title bar, the title of the language, the size.</summary>
[Collection(AccentColorTestCollection.Name)]
public class MessageBoxFactoryTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        AppLanguage language = AppLanguage.English;
        AppLocalization.Language = language;
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    [Fact]
    public void TheFactories_MakeAFixedDialog_WithTheTitleOfTheLanguage_OrTheOneGiven()
    {
        MessageBoxForm standard = _host.Add(UIStyles.Forms.CreateMessageStandard("Saved."));
        MessageBoxForm primary = _host.Add(UIStyles.Forms.CreateMessagePrimary("Saved.", "Done"));

        Assert.Equal("Notice", standard.Text);
        Assert.Equal("Done", primary.Text);
        Assert.Equal(standard.Size, standard.MinimumSize);
        Assert.Equal(standard.Size, standard.MaximumSize);
        Assert.False(standard.TitleBar.ShowMaximizeButton);
        Assert.Equal(new Size(500, 220), standard.Size);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Hinweis", UIStyles.Forms.CreateMessageStandard("x").Text);
        Assert.Equal("Hinweis", UIStyles.Forms.CreateMessagePrimary("x").Text);
    }

    [Fact]
    public void TheTitleBar_TakesAColorSet_OrTheAccentWithPrimary()
    {
        MessageBoxForm plain = _host.Show(UIStyles.Forms.CreateMessageStandard("x"));
        MessageBoxForm yellow = _host.Show(UIStyles.Forms.CreateMessageStandard("x", colors: UIStyles.Colors.Sets.Yellow));
        MessageBoxForm primary = _host.Show(UIStyles.Forms.CreateMessagePrimary("x"));

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), plain.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), yellow.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), primary.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void SizeAndButtonsAndIcon_AreHandedOver()
    {
        MessageBoxForm small = _host.Add(UIStyles.Forms.CreateMessageStandard("x", size: FormSize.Small));
        MessageBoxForm own = _host.Add(UIStyles.Forms.CreateMessagePrimary("x", size: new Size(555, 222)));
        MessageBoxForm question = _host.Add(UIStyles.Forms.CreateMessageStandard("x", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question));
        MessageBoxForm none = _host.Add(UIStyles.Forms.CreateMessageStandard("x", icon: MessageBoxIcon.None));

        Assert.Equal(new Size(380, 160), small.Size);
        Assert.Equal(new Size(555, 222), own.Size);
        Assert.NotNull(question.AcceptButton);
        Assert.NotNull(question.CancelButton);
        Assert.NotNull(question.FormIcon);
        Assert.Null(none.FormIcon);
    }

    [Fact]
    public void TheOldConstructor_StillMakesTheSameWindow_AsTheFactory()
    {
        MessageBoxForm made = _host.Add(new MessageBoxForm("x", "Title", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning));
        MessageBoxForm factory = _host.Add(UIStyles.Forms.CreateMessageStandard("x", "Title", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning));

        Assert.Equal(factory.Size, made.Size);
        Assert.Equal(factory.Text, made.Text);
        Assert.Equal(factory.TitleBar.BackColor.ToArgb(), made.TitleBar.BackColor.ToArgb());
        Assert.NotNull(made.CancelButton);
    }

    // Shows the box modally and lets the code act on it once it is open (a timer inside the message loop of ShowDialog).
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
    public void Show_UsesTheFactory_WithAColorSet_AndTheTitleOfTheLanguageWhenNoneIsGiven()
    {
        string? title = null;
        Color? bar = null;

        DialogResult result = ShowAndAct(
            form => { title = form.Text; bar = form.TitleBar.BackColor; form.CancelButton!.PerformClick(); },
            () => ErikwnkWFUI.Forms.MessageBox.Show("Really?", null!, MessageBoxButtons.YesNo, MessageBoxIcon.Question, colors: UIStyles.Colors.Sets.Yellow));

        Assert.Equal(DialogResult.No, result);
        Assert.Equal("Notice", title);
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), bar!.Value.ToArgb());
    }

    [Fact]
    public void Show_ReturnsTheButtonThatWasClicked_AndTheSafeOneWhenTheWindowIsClosed()
    {
        Assert.Equal(DialogResult.Yes, ShowAndAct(form => form.AcceptButton!.PerformClick(), () => ErikwnkWFUI.Forms.MessageBox.Show("x", "t", MessageBoxButtons.YesNo)));
        Assert.Equal(DialogResult.No, ShowAndAct(form => form.Close(), () => ErikwnkWFUI.Forms.MessageBox.Show("x", "t", MessageBoxButtons.YesNo)));
        Assert.Equal(DialogResult.Cancel, ShowAndAct(form => form.Close(), () => ErikwnkWFUI.Forms.MessageBox.Show("x", "t", MessageBoxButtons.YesNoCancel)));
        Assert.Equal(DialogResult.OK, ShowAndAct(form => form.Close(), () => ErikwnkWFUI.Forms.MessageBox.Show("x")));
    }
}
