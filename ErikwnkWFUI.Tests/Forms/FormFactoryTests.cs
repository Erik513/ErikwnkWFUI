using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
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
    public void ASet_ColorsTheTitleBarAndTheVersionStrip_WithTheBasicColor_NotTheEdgeAroundTheWindow()
    {
        AppForm form = Own(UIStyles.Forms.CreateSettings("Settings", UIStyles.Colors.Sets.Yellow));

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), form.VersionStrip!.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundMedium.ToArgb(), form.ContentPanel.BackColor.ToArgb());
    }

    [Fact]
    public void ASingleColor_IsTakenAsTheBasicColorOfASet()
    {
        AppForm form = Own(UIStyles.Forms.CreateStandard("Purple", Color.MediumPurple));

        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
        Assert.NotEqual(UIColors.BackgroundBlack.ToArgb(), form.TitleBar.BackColor.ToArgb());
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
        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.VersionStrip!.BackColor.ToArgb());
        Color before = form.TitleBar.BackColor;

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.NotEqual(before.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.PrimaryDarkDark.ToArgb(), form.VersionStrip!.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
    }

    [Fact]
    public void ATitleBarColorOfItsOwn_IsOnlyForTheBar_NotForTheEdge()
    {
        FormOptions options = FormOptions.CreateStandard("Window", backColor: Color.DarkGreen);
        options.Colors = UIStyles.Colors.Sets.Yellow;
        AppForm form = Own(new AppForm(options));

        Assert.Equal(Color.DarkGreen.ToArgb(), form.TitleBar.BackColor.ToArgb());
        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
    }

    [Fact]
    public void TheEdgeAroundTheWindow_IsBlackInAllThreeKinds_AndFollowsTheTheme()
    {
        AppForm standard = Own(UIStyles.Forms.CreateStandard("Standard"));
        AppForm primary = Own(UIStyles.Forms.CreatePrimary("Primary"));
        AppForm yellow = Own(UIStyles.Forms.CreateStandard("Yellow", UIStyles.Colors.Sets.Yellow));

        foreach (AppForm form in new[] { standard, primary, yellow })
            Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BorderColor.ToArgb());

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        foreach (AppForm form in new[] { standard, primary, yellow })
        {
            Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
            Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BorderColor.ToArgb());
        }
    }

    [Fact]
    public void TheBorderColor_CanBeSet_ByTheOptionsOrLater_AndEmptyGoesBackToBlack()
    {
        FormOptions options = FormOptions.CreateStandard("Window");
        options.Colors = UIStyles.Colors.Sets.Yellow;
        options.BorderColor = Color.Crimson;
        AppForm form = Own(new AppForm(options));

        Assert.Equal(Color.Crimson.ToArgb(), form.BackColor.ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Yellow.Basic.ToArgb(), form.TitleBar.BackColor.ToArgb());

        form.BorderColor = Color.Teal;
        Assert.Equal(Color.Teal.ToArgb(), form.BackColor.ToArgb());

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Assert.Equal(Color.Teal.ToArgb(), form.BackColor.ToArgb());

        form.BorderColor = Color.Empty;
        Assert.Equal(UIColors.BackgroundBlack.ToArgb(), form.BackColor.ToArgb());
    }

    [Fact]
    public void AResizableWindow_HasALeastSize_ADialogHasNone_AndTheOptionsOrTheWindowCanChangeIt()
    {
        AppForm window = Own(UIStyles.Forms.CreateStandard("Window"));
        AppForm dialog = Own(UIStyles.Forms.CreateDialog("Dialog"));
        FormOptions options = FormOptions.CreateStandard("Own");
        options.MinimumSize = new Size(400, 300);
        AppForm own = Own(new AppForm(options));

        Assert.Equal(Dpi.Scale(260, window.DeviceDpi), window.MinimumSize.Width);
        Assert.Equal(Dpi.Scale(140, window.DeviceDpi), window.MinimumSize.Height);
        Assert.Equal(Size.Empty, dialog.MinimumSize);
        Assert.Equal(Dpi.Scale(400, own.DeviceDpi), own.MinimumSize.Width);

        window.MinimumSize = new Size(500, 500);
        Assert.Equal(new Size(500, 500), window.MinimumSize);
    }

    [Fact]
    public void ASizeBelowTheLeastSize_IsRaisedToIt()
    {
        AppForm window = Own(UIStyles.Forms.CreateStandard("Window"));
        OffscreenForm.Place(window);

        window.Size = new Size(50, 50);

        Assert.True(window.Width >= window.MinimumSize.Width && window.Height >= window.MinimumSize.Height);
    }

    [Fact]
    public void TheButtonsOfTheTitleBar_LookTheSameInEveryKindOfWindow()
    {
        AppForm standard = Own(UIStyles.Forms.CreateStandard("Standard"));
        AppForm primary = Own(UIStyles.Forms.CreatePrimary("Primary"));
        AppForm yellow = Own(UIStyles.Forms.CreateStandard("Yellow", UIStyles.Colors.Sets.Yellow));

        foreach (string field in new[] { "_minimizeButton", "_maximizeButton", "_closeButton" })
        {
            Button reference = standard.TitleBar.GetPrivateField<Button>(field)!;

            foreach (AppForm other in new[] { primary, yellow })
            {
                Button button = other.TitleBar.GetPrivateField<Button>(field)!;
                Assert.Equal(reference.BackColor.ToArgb(), button.BackColor.ToArgb());
                Assert.Equal(reference.ForeColor.ToArgb(), button.ForeColor.ToArgb());
                Assert.Equal(reference.FlatAppearance.BorderColor.ToArgb(), button.FlatAppearance.BorderColor.ToArgb());
            }
        }
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

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheVersionText_CanBeRead_OnTheStripOfEverySetAndOfThePrimaryOne_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets(includeDark: true))
            {
                AppForm form = Own(UIStyles.Forms.CreateSettings("Settings", set));
                ErikwnkWFUI.Controls.ScrollingLabel version = (ErikwnkWFUI.Controls.ScrollingLabel)form.VersionLabel;
                Check(shortfalls, "version, " + name, version.GetTextColor(), form.VersionStrip!.BackColor, 3.0);
            }

            AppForm primary = Own(UIStyles.Forms.CreatePrimarySettings("Settings"));
            Check(shortfalls, "version, primary", ((ErikwnkWFUI.Controls.ScrollingLabel)primary.VersionLabel).GetTextColor(), primary.VersionStrip!.BackColor, 3.0);
        });
    }

    [Fact]
    public void TheSystemMenu_TakesTheColorsOfTheFrame_OrIsNeutral()
    {
        AppForm yellow = Own(UIStyles.Forms.CreateStandard("Yellow", UIStyles.Colors.Sets.Yellow));
        AppForm plain = Own(UIStyles.Forms.CreateStandard("Plain"));
        AppForm primary = Own(UIStyles.Forms.CreatePrimary("Primary"));

        using (ErikwnkWFUI.Controls.ContextMenuStrip menu = yellow.CreateSystemMenu())
            Assert.Equal(UIStyles.Colors.Sets.Yellow.Hover.ToArgb(), menu.Colors.Hover.ToArgb());

        using (ErikwnkWFUI.Controls.ContextMenuStrip menu = plain.CreateSystemMenu())
            Assert.Null(menu.Colors);

        using (ErikwnkWFUI.Controls.ContextMenuStrip menu = primary.CreateSystemMenu())
            Assert.Equal(UIColors.PrimarySet.Hover.ToArgb(), menu.Colors.Hover.ToArgb());
    }

    private static Label TitleLabel(AppForm form)
    {
        return form.TitleBar.GetPrivateField<Label>("_titleLabel")!;
    }
}
