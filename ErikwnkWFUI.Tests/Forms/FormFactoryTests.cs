using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.ColorContrastTestSupport;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>What UIStyles.Forms makes: the three kinds of window, and the frame in the normal color, in the color of a set or in the accent.</summary>
[Collection(AccentColorTestCollection.Name)]
public class FormFactoryTests : IDisposable
{
    private readonly List<AppForm> _forms = new List<AppForm>();

    public void Dispose()
    {
        foreach (AppForm form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    private AppForm Own(AppForm form)
    {
        _forms.Add(form);

        return OffscreenForm.Place(form);
    }

    [Fact]
    public void TheThreeKinds_AreAResizableWindow_ADialogWithOnlyClose_AndASettingsWindowWithAVersion()
    {
        AppForm window = Own(UIStyles.Forms.CreateStandard("Window"));
        AppForm dialog = Own(UIStyles.Forms.CreateDialog("Dialog"));
        AppForm settings = Own(UIStyles.Forms.CreateSettings("Settings"));

        Assert.Equal("Window", window.FormTitle);
        Assert.True(window.TitleBar.ShowMinimizeButton && window.TitleBar.ShowMaximizeButton && window.TitleBar.ShowCloseButton);
        Assert.Null(window.VersionStrip);

        Assert.False(dialog.TitleBar.ShowMinimizeButton || dialog.TitleBar.ShowMaximizeButton);
        Assert.True(dialog.TitleBar.ShowCloseButton);
        Assert.Null(dialog.VersionStrip);

        Assert.False(settings.TitleBar.ShowMaximizeButton);
        Assert.NotNull(settings.VersionStrip);
        Assert.Equal(ContentAlignment.MiddleLeft, settings.TitleBar.TitleTextAlign);
    }

    [Fact]
    public void ASet_ColorsTheEdge_TheTitleBar_AndTheVersionStrip_WithTheBasicColor()
    {
        AppForm form = Own(UIStyles.Forms.CreateSettings("Settings", UIStyles.Colors.Sets.Yellow));

        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), form.VersionStrip!.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundMedium.ToArgb(), form.ContentPanel.BackColor.ToArgb());
    }

    [Fact]
    public void ASingleColor_IsTakenAsTheBasicColorOfASet()
    {
        AppForm form = Own(UIStyles.Forms.CreateStandard("Purple", Color.MediumPurple));

        Assert.NotEqual(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(form.BackColor.ToArgb(), form.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void WithoutASet_TheBarAndTheEdgeAreTheDarkestBackground()
    {
        AppForm form = Own(UIStyles.Forms.CreateStandard("Window"));

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.TitleBar.BackColor.ToArgb());

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void ThePrimaryFrame_IsAVeryDarkShadeOfTheAccent_AndFollowsAChangeOfTheAccent()
    {
        AppForm form = Own(UIStyles.Forms.CreatePrimarySettings("Primary"));
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.VersionStrip!.BackColor.ToArgb());
        Color before = form.BackColor;

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.NotEqual(before.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.TitleBar.BackColor.ToArgb());
    }

    [Fact]
    public void ATitleBarColorOfItsOwn_IsOnlyForTheBar_NotForTheEdge()
    {
        FormOptions options = FormOptions.CreateStandard("Window", backColor: Color.DarkGreen);
        options.Colors = UIStyles.Colors.Sets.Yellow;
        AppForm form = Own(new AppForm(options));

        Assert.Equal(Color.DarkGreen.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), form.BackColor.ToArgb());
    }

    [Fact]
    public void ANullSet_GoesBackToTheNormalFrame()
    {
        FormOptions options = FormOptions.CreateStandard("Window");
        options.Colors = UIStyles.Colors.Sets.Red;
        options.Colors = null;
        AppForm form = Own(new AppForm(options));

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheTitle_CanBeRead_OnTheFrameOfEverySetAndOfThePrimaryOne_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets(includeDark: true))
            {
                AppForm form = Own(UIStyles.Forms.CreateStandard("Title", set));
                Label title = TitleLabel(form);
                Check(shortfalls, "title, " + name, title.ForeColor, title.BackColor, 4.5);
            }

            AppForm primary = Own(UIStyles.Forms.CreatePrimary("Title"));
            Check(shortfalls, "title, primary", TitleLabel(primary).ForeColor, TitleLabel(primary).BackColor, 4.5);
        });
    }

    private static Label TitleLabel(AppForm form)
    {
        return form.TitleBar.GetPrivateField<Label>("_titleLabel")!;
    }
}
