using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Controls.TextBoxes;

/// <summary>What the factories of the text box make.</summary>
public class TextBoxFactoryTests
{
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
