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

    // A window that is not shown (the test shows it itself, over an owner or at a place of its own).
    private AppForm OwnHidden(AppForm form)
    {
        _forms.Add(form);

        return form;
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
        AppForm dialog = Own(UIStyles.Forms.CreateDialogStandard("Dialog"));
        AppForm settings = Own(UIStyles.Forms.CreateSettingsStandard("Settings"));

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
        AppForm form = Own(UIStyles.Forms.CreateSettingsStandard("Settings", UIStyles.Colors.Sets.Yellow));

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
        AppForm form = Own(UIStyles.Forms.CreateSettingsPrimary("Primary"));
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
        AppForm dialog = Own(UIStyles.Forms.CreateDialogStandard("Dialog"));
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

    private static Button CaptionButton(AppForm form, string field)
    {
        return form.TitleBar.GetPrivateField<Button>(field)!;
    }

    [Fact]
    public void TheButtonsOfTheTitleBar_AreFlatInTheColorOfTheBar_WithTheSameSymbolsInEveryWindow()
    {
        AppForm standard = Own(UIStyles.Forms.CreateStandard("Standard"));
        AppForm yellow = Own(UIStyles.Forms.CreateStandard("Yellow", UIStyles.Colors.Sets.Yellow));
        AppForm primary = Own(UIStyles.Forms.CreatePrimary("Primary"));

        foreach (string field in new[] { "_minimizeButton", "_maximizeButton", "_closeButton" })
        {
            Button reference = CaptionButton(standard, field);

            foreach (AppForm form in new[] { standard, yellow, primary })
            {
                Button button = CaptionButton(form, field);

                // flat: no outline, no fill of its own - the bar shows through
                Assert.Equal(form.TitleBar.BackColor.ToArgb(), button.BackColor.ToArgb());
                Assert.Equal(0, button.FlatAppearance.BorderSize);

                // the symbol is drawn the same way everywhere
                Assert.Equal(reference.GetType(), button.GetType());
                Assert.Equal(reference.Text, button.Text);
                Assert.Equal(reference.Font, button.Font);
            }
        }
    }

    [Fact]
    public void TheButtonsOfTheTitleBar_ShowAQuietFillUnderTheMouse_AndTheCloseButtonTurnsRed()
    {
        AppForm yellow = Own(UIStyles.Forms.CreateStandard("Yellow", UIStyles.Colors.Sets.Yellow));
        Button minimize = CaptionButton(yellow, "_minimizeButton");
        Button close = CaptionButton(yellow, "_closeButton");

        Assert.NotEqual(yellow.TitleBar.BackColor.ToArgb(), minimize.FlatAppearance.MouseOverBackColor.ToArgb());
        Assert.NotEqual(minimize.FlatAppearance.MouseOverBackColor.ToArgb(), minimize.FlatAppearance.MouseDownBackColor.ToArgb());
        Assert.True(close.FlatAppearance.MouseOverBackColor.R > 150 && close.FlatAppearance.MouseOverBackColor.G < 80);
    }

    [Fact]
    public void TheButtonsOfTheTitleBar_FollowAChangeOfTheTheme()
    {
        AppForm standard = Own(UIStyles.Forms.CreateStandard("Standard"));
        AppForm primary = Own(UIStyles.Forms.CreatePrimary("Primary"));

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        foreach (AppForm form in new[] { standard, primary })
            foreach (string field in new[] { "_minimizeButton", "_maximizeButton", "_closeButton" })
                Assert.Equal(form.TitleBar.BackColor.ToArgb(), CaptionButton(form, field).BackColor.ToArgb());
    }

    [Theory]
    [MemberData(nameof(Themes), MemberType = typeof(ColorContrastTestSupport))]
    public void TheSymbolsOfTheTitleBar_CanBeSeenOnTheBarOfEverySetAndOfThePrimaryOne_InBothThemes(bool lightTheme)
    {
        Run(lightTheme, shortfalls =>
        {
            foreach ((string name, UIColorSet? set) in AllSets(includeDark: true))
            {
                AppForm form = Own(UIStyles.Forms.CreateStandard("Title", set));
                Check(shortfalls, "symbol, " + name, CaptionButton(form, "_minimizeButton").ForeColor, form.TitleBar.BackColor, 4.5);
            }

            AppForm primary = Own(UIStyles.Forms.CreatePrimary("Title"));
            Check(shortfalls, "symbol, primary", CaptionButton(primary, "_minimizeButton").ForeColor, primary.TitleBar.BackColor, 4.5);
        });
    }

    [Fact]
    public void TheTitle_StaysInTheMiddleOfTheBar_AndKeepsClearOfTheButtonsWhenTheWindowIsNarrow()
    {
        AppForm form = Own(UIStyles.Forms.CreateStandard("A title of a window"));
        Label title = TitleLabel(form);
        Button minimize = CaptionButton(form, "_minimizeButton");
        int needed = TextRenderer.MeasureText(title.Text, title.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + title.Padding.Horizontal;

        // (A window cannot be narrower than its least size: the narrowest bar is that of a window of 260 px.)
        foreach (int width in new[] { 800, 600, 400, 300, 260 })
        {
            form.ClientSize = new Size(width, 200);
            Application.DoEvents();
            form.TitleBar.InvokePrivate("UpdateLayout");

            Assert.True(title.Right <= minimize.Left, "the title goes under the buttons at " + width);
            Assert.True(title.Left >= 0, "the title goes out of the bar at " + width);

            // Room enough on both sides: the same margin, so that it is in the middle of the bar.
            if (form.TitleBar.Width - 2 * (form.TitleBar.Width - minimize.Left) >= needed)
                Assert.Equal(title.Left, form.TitleBar.Width - title.Right);
        }
    }

    [Fact]
    public void TheTitle_IsNotCutWithDots_AsLongAsItFitsLeftOfTheButtons()
    {
        AppForm form = Own(UIStyles.Forms.CreateStandard("A rather long title of a window"));
        Label title = TitleLabel(form);
        Button minimize = CaptionButton(form, "_minimizeButton");
        int needed = TextRenderer.MeasureText(title.Text, title.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width + title.Padding.Horizontal;

        foreach (int width in new[] { 800, 600, 500, 420, 380, 340 })
        {
            form.ClientSize = new Size(width, 200);
            Application.DoEvents();
            form.TitleBar.InvokePrivate("UpdateLayout");

            if (minimize.Left >= needed)
                Assert.True(title.Width >= needed, "the title is cut with dots at " + width + " though it fits");
        }
    }

    [Fact]
    public void ALeftAlignedTitle_AlsoKeepsClearOfTheButtons()
    {
        AppForm form = Own(UIStyles.Forms.CreateSettingsStandard("A rather long title of a window"));
        Label title = TitleLabel(form);

        form.ClientSize = new Size(260, 200);
        Application.DoEvents();
        form.TitleBar.InvokePrivate("UpdateLayout");

        Assert.True(title.Right <= CaptionButton(form, "_closeButton").Left);
    }

    [Fact]
    public void EveryWindow_OpensInTheMiddleOfTheScreen_ByDefault()
    {
        Assert.Equal(FormStartPosition.CenterScreen, OwnHidden(UIStyles.Forms.CreateStandard("a")).StartPosition);
        Assert.Equal(FormStartPosition.CenterScreen, OwnHidden(UIStyles.Forms.CreateDialogStandard("b")).StartPosition);
        Assert.Equal(FormStartPosition.CenterScreen, OwnHidden(UIStyles.Forms.CreateSettingsStandard("c")).StartPosition);
        Assert.Equal(FormStartPosition.CenterScreen, OwnHidden(new AppForm()).StartPosition);
    }

    [Fact]
    public void AWindowThatOpensOverAnOwner_IsCenteredOverIt()
    {
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;
        AppForm owner = OwnHidden(UIStyles.Forms.CreateStandard("Owner"));
        owner.StartPosition = FormStartPosition.Manual;
        owner.Bounds = new Rectangle(area.X + 100, area.Y + 80, 500, 300);
        owner.Show();

        AppForm child = OwnHidden(UIStyles.Forms.CreateDialogStandard("Child"));
        child.Size = new Size(200, 120);
        child.Show(owner);

        Assert.Equal(owner.Left + (owner.Width - child.Width) / 2, child.Left);
        Assert.Equal(owner.Top + (owner.Height - child.Height) / 2, child.Top);
    }

    [Fact]
    public void AWindowWhoseStartPositionWasChanged_IsLeftWhereItWasPut()
    {
        Rectangle area = Screen.PrimaryScreen!.WorkingArea;
        AppForm owner = OwnHidden(UIStyles.Forms.CreateStandard("Owner"));
        owner.StartPosition = FormStartPosition.Manual;
        owner.Bounds = new Rectangle(area.X + 100, area.Y + 80, 500, 300);
        owner.Show();

        AppForm child = OwnHidden(UIStyles.Forms.CreateDialogStandard("Child"));
        child.StartPosition = FormStartPosition.Manual;
        child.Bounds = new Rectangle(area.X + 10, area.Y + 10, 200, 120);
        child.Show(owner);

        Assert.Equal(new Point(area.X + 10, area.Y + 10), child.Location);
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
                AppForm form = Own(UIStyles.Forms.CreateSettingsStandard("Settings", set));
                ErikwnkWFUI.Controls.ScrollingLabel version = (ErikwnkWFUI.Controls.ScrollingLabel)form.VersionLabel;
                Check(shortfalls, "version, " + name, version.GetTextColor(), form.VersionStrip!.BackColor, 3.0);
            }

            AppForm primary = Own(UIStyles.Forms.CreateSettingsPrimary("Settings"));
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
