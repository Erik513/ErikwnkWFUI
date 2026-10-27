using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>What the factories of the text box make.</summary>
[Collection(AccentColorTestCollection.Name)]
public class TextBoxFactoryTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(UIAccentColors.Blue);
    }

    [Fact]
    public void CreateBorderless_MakesATextBoxWithoutAFrame_AndTheTextAndThePlaceholderAreGiven()
    {
        using TextBox box = UIStyles.TextBoxes.CreateBorderless("text", "type here");

        Assert.Equal(BorderStyle.None, box.BorderStyle);
        Assert.Equal("text", box.Text);
    }

    [Fact]
    public void ATextThatIsGiven_StaysWhenThereIsAPlaceholder_AndWithoutOneThePlaceholderStandsIn()
    {
        using TextBox given = UIStyles.TextBoxes.CreateStandard("my text", "type here");
        using TextBox empty = UIStyles.TextBoxes.CreateStandard("", "type here");
        using TextBox borderless = UIStyles.TextBoxes.CreateBorderless("my text", "type here");

        Assert.Equal("my text", given.Text);
        Assert.Equal("my text", borderless.Text);
        Assert.Equal("type here", empty.Text);
        Assert.NotEqual(given.ForeColor, empty.ForeColor);
    }

    [Fact]
    public void TheFactories_MakeTheTextBoxOfTheLibrary_WhichCarriesTheColorSet()
    {
        using TextBox standard = UIStyles.TextBoxes.CreateStandard("x");
        using TextBox yellow = UIStyles.TextBoxes.CreateStandard("x", colors: UIStyles.Colors.Sets.Yellow);
        using TextBox purple = UIStyles.TextBoxes.CreateStandard("x", colors: System.Drawing.Color.MediumPurple);
        using TextBox primary = UIStyles.TextBoxes.CreatePrimary("x");
        using TextBox borderless = UIStyles.TextBoxes.CreateBorderless("x");

        Assert.IsType<ErikwnkWFUI.Controls.TextBox>(standard);
        Assert.IsType<ErikwnkWFUI.Controls.TextBox>(borderless);
        Assert.Null(((ErikwnkWFUI.Controls.TextBox)standard).Colors);
        Assert.Same(UIStyles.Colors.Sets.Yellow, ((ErikwnkWFUI.Controls.TextBox)yellow).Colors);
        Assert.Equal(System.Drawing.Color.MediumPurple.ToArgb(), ((ErikwnkWFUI.Controls.TextBox)purple).Colors.Hover.ToArgb());
        Assert.Equal(UIColors.PrimarySet.Hover.ToArgb(), ((ErikwnkWFUI.Controls.TextBox)primary).Colors.Hover.ToArgb());
    }

    [Fact]
    public void ThePrimaryColors_AreAskedForAgain_SoTheyFollowTheAccent_AndALaterColorsReplacesThem()
    {
        using TextBox primary = UIStyles.TextBoxes.CreatePrimary("x");
        ErikwnkWFUI.Controls.TextBox box = (ErikwnkWFUI.Controls.TextBox)primary;
        System.Drawing.Color before = box.Colors.Hover;

        UIColors.SetAccent(System.Drawing.Color.OrangeRed);
        System.Drawing.Color after = box.Colors.Hover;
        UIColors.SetAccent(UIAccentColors.Blue);

        Assert.NotEqual(before.ToArgb(), after.ToArgb());

        box.Colors = UIStyles.Colors.Sets.Green;
        Assert.Same(UIStyles.Colors.Sets.Green, box.Colors);
    }

    [Fact]
    public void CreateStandard_MakesATextBoxWithAFrame()
    {
        using TextBox box = UIStyles.TextBoxes.CreateStandard("text");

        Assert.Equal(BorderStyle.FixedSingle, box.BorderStyle);
        Assert.Equal("text", box.Text);
    }

    // The old name stays until the projects that use it have been changed; it has to make the same text box.
#pragma warning disable CS0618
    [Fact]
    public void TheOldName_CreateBorderstyleNone_StillMakesTheSameTextBox()
    {
        using TextBox old = UIStyles.TextBoxes.CreateBorderstyleNone("a", "b");
        using TextBox renamed = UIStyles.TextBoxes.CreateBorderless("a", "b");

        Assert.Equal(renamed.BorderStyle, old.BorderStyle);
        Assert.Equal(renamed.Text, old.Text);
        Assert.Equal(renamed.Font, old.Font);
        Assert.Equal(renamed.ForeColor, old.ForeColor);
    }
#pragma warning restore CS0618
}
