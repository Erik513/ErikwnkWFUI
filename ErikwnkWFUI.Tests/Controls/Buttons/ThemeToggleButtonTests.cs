using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>The button that switches the theme: it shows the symbol of the theme a click leads to, switches it by itself and follows every change.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeToggleButtonTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
        UIStrings.Language = UILanguage.English;
    }

    private Button Shown(Button button)
    {
        Form form = OffscreenForm.Create(new Size(200, 100), UIColors.BackgroundMedium);
        button.Location = new Point(10, 10);
        form.Controls.Add(button);
        _forms.Add(form);
        form.Show();
        Application.DoEvents();

        return button;
    }

    private static void Click(Button button)
    {
        button.InvokePrivate("OnClick", EventArgs.Empty);
    }

    private static bool IsLight()
    {
        return UIColors.IsLight(UIColors.BackgroundMedium);
    }

    [Fact]
    public void ItShowsTheSymbolOfTheThemeAClickLeadsTo_WithATooltipAndAName_InTheLanguage()
    {
        Button button = Shown(UIStyles.Buttons.CreateThemeToggle());

        Assert.Equal(UIStyles.Symbols.Sun, button.Text);
        Assert.Equal("Switch to light theme", button.AccessibleName);
        Assert.Equal("Switch to light theme", ButtonToolTipReader.Text(button));

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(UIStyles.Symbols.Moon, button.Text);
        Assert.Equal("Switch to dark theme", ButtonToolTipReader.Text(button));

        UIStrings.Language = UILanguage.German;
        Assert.Equal("Zum dunklen Design wechseln", button.AccessibleName);
        Assert.Equal("Zum dunklen Design wechseln", ButtonToolTipReader.Text(button));

        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();
        Assert.Equal("Zum hellen Design wechseln", ButtonToolTipReader.Text(button));
    }

    [Fact]
    public void AClick_SwitchesTheTheme_ByItself_BothWays_AndTheSymbolFollows()
    {
        Button button = Shown(UIStyles.Buttons.CreateThemeToggle());
        Assert.False(IsLight());

        Click(button);
        Assert.True(IsLight());
        Assert.Equal(UIStyles.Symbols.Moon, button.Text);

        Click(button);
        Assert.False(IsLight());
        Assert.Equal(UIStyles.Symbols.Sun, button.Text);
    }

    [Fact]
    public void AHandlerOfTheProgram_RunsAfterTheThemeHasBeenSwitched()
    {
        Button button = Shown(UIStyles.Buttons.CreateThemeToggle());
        bool? lightInHandler = null;
        button.Click += (sender, e) => lightInHandler = IsLight();

        Click(button);

        Assert.True(lightInHandler);
    }

    [Fact]
    public void AChangeOfTheTheme_FromSomewhereElse_IsFollowed_AlsoByAButtonThatWasNotShownYet()
    {
        Button shown = Shown(UIStyles.Buttons.CreateThemeToggle());
        Button later = UIStyles.Buttons.CreateThemeToggle();

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Shown(later);

        Assert.Equal(UIStyles.Symbols.Moon, shown.Text);
        Assert.Equal(UIStyles.Symbols.Moon, later.Text);
    }

    [Fact]
    public void ThePrimaryOne_IsInTheAccent_AndFollowsIt_AColorSetGivesItsColors_TheSizeIsKept()
    {
        Button primary = Shown(UIStyles.Buttons.CreateThemeTogglePrimary());
        Assert.Equal(UIColors.PrimarySet.Basic, primary.BackColor);

        UIColors.SetAccent(UIAccentColors.Green);
        Application.DoEvents();
        Assert.Equal(UIColors.PrimarySet.Basic, primary.BackColor);

        Button green = UIStyles.Buttons.CreateThemeToggle(colors: UIStyles.Colors.Sets.Green);
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic, green.BackColor);

        Assert.Equal(new Size(50, 40), UIStyles.Buttons.CreateThemeToggle(new Size(50, 40)).Size);
        Assert.Equal(new Size(50, 40), UIStyles.Buttons.CreateThemeTogglePrimary(new Size(50, 40)).Size);
    }

    [Fact]
    public void ItTakesNoFocus_LikeAButtonOfAToolBar()
    {
        Button button = Shown(UIStyles.Buttons.CreateThemeToggle());

        Assert.False(button.TabStop);
        Assert.False(button.CanSelect);
    }

    [Fact]
    public void ADisposedButton_ListensToTheLanguageNoMore()
    {
        System.Reflection.FieldInfo? field = typeof(UIStrings).GetField("LanguageChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        int before = ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0;

        Button button = UIStyles.Buttons.CreateThemeToggle();
        Assert.Equal(before + 1, ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0);

        button.Dispose();
        button.Dispose();

        Assert.Equal(before, ((Delegate?)field?.GetValue(null))?.GetInvocationList().Length ?? 0);
        Assert.Null(Record.Exception(() => UIStrings.Language = UILanguage.German));
    }
}