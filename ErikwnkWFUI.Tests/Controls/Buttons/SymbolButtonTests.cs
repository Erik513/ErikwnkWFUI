using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// The symbol button: creation, fonts, the folder on the yellow set.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class SymbolButtonTests : SymbolButtonTestBase
{
    [Fact]
    public void AtTheStandardSize_ItUsesTheSharedIconFont()
    {
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add);

        Assert.Same(UIFonts.Icon, button.Font);
    }

    [Fact]
    public void ItShowsTheSymbol_InTheIconFont_AtTheStandardSize()
    {
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add, "Add");

        Assert.Equal(UIStyles.Symbols.Add, button.Text);
        Assert.Equal(new Size(30, 30), button.Size);
        Assert.Equal("Add", ButtonToolTipReader.Text(button));
        Assert.Equal(Padding.Empty, button.Padding);
        Assert.Equal(FlatStyle.Flat, button.FlatStyle);
        Assert.Equal(UIFonts.Icon.FontFamily.Name, button.Font.FontFamily.Name);
        Assert.Equal(UIFonts.Icon.Size, button.Font.Size);
    }

    [Fact]
    public void ASymbolFromTheWindowsIconFont_GetsThatFont_AtAnySize()
    {
        string expected = UIStyles.Symbols.GetFontName(UIStyles.Symbols.Delete);

        using Button standardSize = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Delete);
        using Button large = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Delete, size: new Size(48, 48));

        Assert.Equal(expected, standardSize.Font.Name);
        Assert.Equal(expected, large.Font.Name);
        Assert.Equal(UIFonts.Icon.Size, standardSize.Font.Size);
    }

    [Fact]
    public void TheDocument_IsDrawnInTheEmojiFont_AndTheFileInTheUsualOne()
    {
        // (Windows has Segoe UI Emoji; without it the usual font is the fallback.)
        string emoji = UIStyles.Symbols.GetFontName(UIStyles.Symbols.Document);

        using Button document = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Document);
        using Button file = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.File);

        Assert.NotEqual(UIStyles.Symbols.File, UIStyles.Symbols.Document);
        Assert.Equal(emoji, document.Font.Name);
        Assert.Equal(UIStyles.Symbols.GetFontName(UIStyles.Symbols.File), file.Font.Name);
        Assert.Equal(UIFonts.Icon.FontFamily.Name, file.Font.Name);
    }

    [Theory]
    [InlineData(24)]
    [InlineData(30)]
    [InlineData(48)]
    [InlineData(64)]
    public void TheSymbol_GrowsWithTheButton(int height)
    {
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Home, size: new Size(height + 10, height));

        Assert.Equal(new Size(height + 10, height), button.Size);
        Assert.Equal(height * 13f / 30f, button.Font.Size, 0.01);
    }

    [Fact]
    public void WithoutColors_TheLookIsTheNeutralOne_LikeCreateStandard()
    {
        using Button symbol = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Search);
        using Button standard = UIStyles.Buttons.CreateStandard(UIStyles.Symbols.Search, isIcon: true);

        Assert.Equal(standard.BackColor, symbol.BackColor);
        Assert.Equal(standard.ForeColor, symbol.ForeColor);
        Assert.Equal(standard.FlatAppearance.MouseOverBackColor, symbol.FlatAppearance.MouseOverBackColor);
        Assert.Equal(standard.FlatAppearance.MouseDownBackColor, symbol.FlatAppearance.MouseDownBackColor);
        Assert.Equal(standard.FlatAppearance.BorderColor, symbol.FlatAppearance.BorderColor);
    }

    [Fact]
    public void WithColors_EveryStateComesFromTheSet()
    {
        UIColorSet set = UIStyles.Colors.Sets.Green;
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Save, colors: set);

        Assert.Equal(set.Basic, button.BackColor);
        Assert.Equal(set.BasicText, button.ForeColor);
        Assert.Equal(set.Hover, button.FlatAppearance.MouseOverBackColor);
        Assert.Equal(set.Pressed, button.FlatAppearance.MouseDownBackColor);

        Raise(button, "OnMouseEnter", EventArgs.Empty);
        Assert.Equal(set.HoverText, button.ForeColor);

        Raise(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.Equal(set.PressedText, button.ForeColor);

        Raise(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Raise(button, "OnMouseLeave", EventArgs.Empty);

        button.Enabled = false;
        Assert.Equal(set.Disabled, button.BackColor);
        Assert.Equal(set.DisabledText, button.ForeColor);
    }

    [Fact]
    public void AnyColor_CanBePassed_AndAMadeSetWorksToo()
    {
        Color color = Color.FromArgb(200, 60, 110);

        using Button direct = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Edit, colors: color);
        using Button viaSet = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Edit, colors: UIStyles.Colors.CreateSet(color));

        Assert.Equal(viaSet.BackColor, direct.BackColor);
        Assert.Equal(viaSet.ForeColor, direct.ForeColor);
        Assert.Equal(UIStyles.Colors.CreateSet(color).Basic, direct.BackColor);
    }

    [Fact]
    public void ASetButton_LooksTheSame_InBothThemes_AndWithAnyAccent()
    {
        (Color, Color, Color, Color, Color, Color, Color, Color) Look()
        {
            using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Delete, colors: UIStyles.Colors.Sets.Red);
            Color back = button.BackColor;
            Color fore = button.ForeColor;
            Color over = button.FlatAppearance.MouseOverBackColor;
            Color down = button.FlatAppearance.MouseDownBackColor;
            Color border = button.FlatAppearance.BorderColor;
            Raise(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            Color pressed = button.ForeColor;
            Raise(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            button.Enabled = false;

            return (back, fore, over, down, border, pressed, button.BackColor, button.ForeColor);
        }

        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            var dark = Look();

            UIColors.ApplyTheme(UIThemes.Light);
            var light = Look();

            UIColors.SetAccent(UIAccentColors.Purple);

            Assert.Equal(dark, light);
            Assert.Equal(dark, Look());
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void TheNeutralSymbolButton_FollowsTheTheme_ASetOneDoesNot()
    {
        try
        {
            UIColors.ApplyTheme(UIThemes.Dark);
            using Button neutralDark = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Up);
            UIColors.ApplyTheme(UIThemes.Light);
            using Button neutralLight = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Up);

            Assert.NotEqual(neutralDark.BackColor, neutralLight.BackColor);
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }
    }

    [Fact]
    public void ItsFont_IsReleasedWithTheButton_AndTheSharedIconFontIsLeftAlone()
    {
        // At the standard size the button simply uses the shared icon font.
        Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Close, size: new Size(48, 48));
        Font own = button.Font;

        Assert.NotSame(UIFonts.Icon, own);
        Assert.False(IsDisposed(own));

        button.Dispose();

        Assert.True(IsDisposed(own));
        Assert.False(IsDisposed(UIFonts.Icon));
    }

    // ---- the folder on the yellow set: what the old Browse button looked like ----

    [Theory]
    [InlineData(30, 30)]
    [InlineData(36, 30)]
    [InlineData(50, 30)]
    [InlineData(64, 48)]
    public void AFolderOnYellow_IsDrawnInTheMiddle_AtAnySize(int width, int height)
    {
        using Button browse = UIStyles.Buttons.CreateSymbol(
            UIStyles.Symbols.Folder, "Pick", new Size(width, height), UIStyles.Colors.Sets.Yellow);

        Assert.Equal("SymbolButton", browse.GetType().Name);
        Assert.Equal("Pick", ButtonToolTipReader.Text(browse));

        var offset = InkOffset(browse);

        Assert.NotNull(offset);
        Assert.InRange(offset!.Value.X, -1.0, 1.0);
        Assert.InRange(offset.Value.Y, -1.0, 1.0);
    }

    [Fact]
    public void TheYellowSet_HasTheLookOfTheOldBrowseButton_InEveryState()
    {
        using Button browse = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Folder, colors: UIStyles.Colors.Sets.Yellow);
        Color disabled = UIColors.DarkenByShare(UIColors.Yellow, 0.35);

        Assert.Equal(UIColors.Yellow, browse.BackColor);
        Assert.Equal(UIColors.White, browse.ForeColor);
        Assert.Equal(UIColors.YellowLight, browse.FlatAppearance.MouseOverBackColor);
        Assert.Equal(UIColors.YellowLighter, browse.FlatAppearance.MouseDownBackColor);

        Raise(browse, "OnMouseEnter", EventArgs.Empty);
        Assert.Equal(UIColors.White, browse.ForeColor);
        Raise(browse, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.Equal(UIColors.White, browse.ForeColor);
        Raise(browse, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Raise(browse, "OnMouseLeave", EventArgs.Empty);

        browse.Enabled = false;
        Assert.Equal(disabled, browse.BackColor);
        Assert.Equal(UIColors.BlendToward(UIColors.White, disabled, 0.35), browse.ForeColor);
    }

    [Fact]
    public void TheLightningBolt_IsDrawnSmallerThanOtherSymbols_ButLargerThanTheChevrons()
    {
        using Button bolt = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Lightning);
        using Button other = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Up);
        using Button chevron = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.ChevronLeft);

        Assert.True(bolt.Font.Size < other.Font.Size);
        Assert.True(bolt.Font.Size > chevron.Font.Size);
    }

    [Fact]
    public void TheChevrons_AreDrawnSmallerThanOtherSymbols_OnAButtonOfTheSameSize()
    {
        using Button normal = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Up, size: new Size(40, 40));

        foreach (string chevron in new[] { UIStyles.Symbols.ChevronLeft, UIStyles.Symbols.ChevronRight, UIStyles.Symbols.ChevronUp, UIStyles.Symbols.ChevronDown })
        {
            using Button button = UIStyles.Buttons.CreateSymbol(chevron, size: new Size(40, 40));

            Assert.True(button.Font.Size < normal.Font.Size, "chevron not smaller");
        }

        // At the standard size too (it must not take the shared icon font as it is).
        using Button standardChevron = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.ChevronLeft);
        using Button standardOther = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Up);

        Assert.True(standardChevron.Font.Size < standardOther.Font.Size);
    }
}
