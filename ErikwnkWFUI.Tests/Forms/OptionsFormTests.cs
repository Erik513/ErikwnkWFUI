using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>The options window: a settings window with a table and the buttons Save and Cancel (or without them), in the language of the library.</summary>
[Collection(AccentColorTestCollection.Name)]
public class OptionsFormTests : IDisposable
{
    private readonly AppFormHost _host = new AppFormHost();

    public void Dispose()
    {
        _host.Dispose();
        UIStrings.Language = UILanguage.English;
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private static Button Find(OptionsForm form, string field)
    {
        return form.GetPrivateField<Button>(field)!;
    }

    [Fact]
    public void ItIsASettingsWindow_WithTheTableTheVersionStripAndTheTitleOfTheLanguage()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());

        Assert.NotNull(form.PropertyTable);
        Assert.NotNull(form.VersionStrip);
        Assert.Equal("Options", form.Text);
        Assert.False(form.TitleBar.ShowMaximizeButton);
        Assert.Equal(ContentAlignment.MiddleLeft, form.TitleBar.TitleTextAlign);

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Optionen", _host.Add(UIStyles.Forms.CreateOptionsStandard()).Text);
        Assert.Equal("My title", _host.Add(UIStyles.Forms.CreateOptionsStandard("My title")).Text);
    }

    [Fact]
    public void TheButtons_AreInTheLanguage_AndFollowAChangeWhileTheWindowIsOpen()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());

        Assert.Equal("Save", form.SaveButtonText);
        Assert.Equal("Cancel", form.CancelButtonText);

        UIStrings.Language = UILanguage.German;

        Assert.Equal("Speichern", form.SaveButtonText);
        Assert.Equal("Abbrechen", form.CancelButtonText);
    }

    [Fact]
    public void ATextThatWasSet_IsKeptWhateverTheLanguage_AndNullGoesBackToTheLanguage()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        form.SaveButtonText = "Apply";
        form.CancelButtonText = "Discard";

        UIStrings.Language = UILanguage.German;

        Assert.Equal("Apply", form.SaveButtonText);
        Assert.Equal("Discard", form.CancelButtonText);

        form.SaveButtonText = null!;
        form.CancelButtonText = null!;

        Assert.Equal("Speichern", form.SaveButtonText);
        Assert.Equal("Abbrechen", form.CancelButtonText);
    }

    [Fact]
    public void SaveRaisesTheEventAndClosesWithOk_CancelClosesWithCancel()
    {
        OptionsForm saved = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        OptionsForm cancelled = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        int raised = 0;
        saved.SaveClicked += (sender, e) => raised++;
        cancelled.SaveClicked += (sender, e) => raised += 100;

        Find(saved, "_saveButton").PerformClick();
        Find(cancelled, "_cancelButton").PerformClick();

        Assert.Equal(1, raised);
        Assert.Equal(DialogResult.OK, saved.DialogResult);
        Assert.Equal(DialogResult.Cancel, cancelled.DialogResult);
    }

    [Fact]
    public void SaveWaitsForTheEditorsToAcceptTheirValues_ALockedWindowStaysOpen()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        System.Windows.Forms.TextBox name = new System.Windows.Forms.TextBox();
        form.PropertyTable.AddRow("Name", name);
        int saved = 0;
        form.SaveClicked += (sender, e) => saved++;
        name.Validating += (sender, e) => e.Cancel = name.Text.Length == 0;

        Find(form, "_saveButton").PerformClick();

        Assert.Equal(0, saved);
        Assert.False(form.IsDisposed);
        Assert.NotEqual(DialogResult.OK, form.DialogResult);

        name.Text = "ready";
        Find(form, "_saveButton").PerformClick();

        Assert.Equal(1, saved);
        Assert.Equal(DialogResult.OK, form.DialogResult);
    }

    [Fact]
    public void WithoutTheButtons_TheBarIsGone_EnterAndEscapeAreNotTheirs_AndTheWindowIsLower()
    {
        OptionsForm with = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        OptionsForm without = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        without.ShowButtons = false;

        Assert.True(with.ShowButtons);
        Assert.False(without.ShowButtons);
        Assert.NotNull(with.AcceptButton);
        Assert.NotNull(with.CancelButton);
        Assert.Null(without.AcceptButton);
        Assert.Null(without.CancelButton);

        with.FitToContent();
        without.FitToContent();
        Assert.True(without.ClientSize.Height < with.ClientSize.Height);

        without.ShowButtons = true;
        Assert.NotNull(without.CancelButton);
    }

    [Fact]
    public void WithoutTheButtons_EscapeClosesTheWindow_LikeInEveryDialog()
    {
        OptionsForm form = _host.Show(UIStyles.Forms.CreateOptionsStandard());
        form.ShowButtons = false;
        bool closed = false;
        form.FormClosed += (sender, e) => closed = true;

        Assert.True(form.InvokePrivate<bool>("ProcessDialogKey", Keys.Escape));
        Assert.True(closed);
    }

    [Fact]
    public void FitToContent_MakesRoomForTheRows_TheButtonsAndTheVersionStrip()
    {
        OptionsForm form = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        form.PropertyTable.AddSection("Section");
        form.PropertyTable.AddRow("Row", new Button());
        form.PropertyTable.AddRow("Row 2", new Button());

        form.FitToContent();

        int rows = form.PropertyTable.PreferredSize.Height;
        Assert.True(form.ClientSize.Height >= form.TitleBar.Height + rows + 56 + form.VersionStrip!.Height);
    }

    [Fact]
    public void TheFactories_TakeAColorSetOrTheAccent_ForTheTitleBar()
    {
        OptionsForm standard = _host.Add(UIStyles.Forms.CreateOptionsStandard());
        OptionsForm yellow = _host.Add(UIStyles.Forms.CreateOptionsStandard("Yellow", UIStyles.Colors.Sets.Yellow));
        OptionsForm primary = _host.Show(UIStyles.Forms.CreateOptionsPrimary());

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), standard.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), yellow.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), primary.TitleBar.BackColor.ToArgb());

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), primary.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void TheWindowOfYourOwn_CanBeMadeFromOptions_AndNullOptionsAreASettingsWindow()
    {
        FormOptions options = FormOptions.CreateSettingsPrimary("Own");
        options.RememberBoundsKey = "options";

        OptionsForm own = _host.Add(new OptionsForm(options));
        OptionsForm none = _host.Add(new OptionsForm((FormOptions)null!));

        Assert.Equal("Own", own.Text);
        Assert.Equal("options", own.RememberBoundsKey);
        Assert.NotNull(none.VersionStrip);
    }

    [Fact]
    public void TheOptionsWindow_StopsListeningToTheLanguage_WhenItIsDisposed()
    {
        FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", BindingFlags.NonPublic | BindingFlags.Static);
        int Listeners() => ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;
        int before = Listeners();

        OptionsForm form = UIStyles.Forms.CreateOptionsStandard();
        Assert.True(Listeners() > before);

        form.Dispose();

        Assert.Equal(before, Listeners());
    }

#pragma warning disable CS0618
    [Fact]
    public void TheOldName_StyledOptionsForm_StillMakesAnOptionsWindow()
    {
        using StyledOptionsForm old = new StyledOptionsForm("Old");

        Assert.Equal("Old", old.Text);
        Assert.NotNull(old.PropertyTable);
    }
#pragma warning restore CS0618
}
