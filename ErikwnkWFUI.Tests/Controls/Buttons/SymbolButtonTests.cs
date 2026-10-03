using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// <c>CreateSymbol</c>: a button made for a symbol of <c>UIStyles.Symbols</c>,
/// in the neutral look or in the colors of a set. What is its own here: the
/// symbol in a font that grows with the button, no padding, and the choice
/// between the neutral look and a set; the states of a set button are covered
/// like those of any other set button (ColoredButtonTests).
/// [Collection]: the colors read the live accent and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class SymbolButtonTests
{
    private static void Raise(Button button, string handler, params object[] args)
    {
        button.InvokePrivate(handler, args);
    }

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
        Assert.Equal("Add", ButtonTestsToolTip.Text(button));
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

    // A Font has no "disposed" flag or event; its native handle is zeroed on
    // Dispose (named differently on .NET Framework and .NET).
    private static bool IsDisposed(Font font)
    {
        System.Reflection.FieldInfo? handle = typeof(Font)
            .GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .FirstOrDefault(field => field.FieldType == typeof(IntPtr) && field.Name.ToLowerInvariant().Contains("nativefont"));

        Assert.NotNull(handle);

        return (IntPtr)handle!.GetValue(font)! == IntPtr.Zero;
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

    // ---- drawing: centered symbols, and the fill of every state ----

    private static Bitmap Draw(Button button)
    {
        _ = button.Handle;
        Bitmap bitmap = new Bitmap(button.Width, button.Height);
        button.DrawToBitmap(bitmap, new Rectangle(0, 0, button.Width, button.Height));

        return bitmap;
    }

    // How far the visible part of the symbol is from the middle of the
    // button, in pixels.
    private static (double X, double Y)? InkOffset(Button button)
    {
        using Bitmap bitmap = Draw(button);
        Color back = button.BackColor;
        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;

        for (int y = 3; y < bitmap.Height - 3; y++)
        {
            for (int x = 3; x < bitmap.Width - 3; x++)
            {
                Color c = bitmap.GetPixel(x, y);
                int difference = Math.Abs(c.R - back.R) + Math.Abs(c.G - back.G) + Math.Abs(c.B - back.B);

                if (difference > 90)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < 0)
            return null;

        return ((minX + maxX + 1) / 2.0 - bitmap.Width / 2.0, (minY + maxY + 1) / 2.0 - bitmap.Height / 2.0);
    }

    [Theory]
    [InlineData(24)]
    [InlineData(30)]
    [InlineData(48)]
    [InlineData(64)]
    public void EverySymbol_SitsInTheMiddleOfTheButton_ForTheNeutralAndAColoredLook(int size)
    {
        List<string> offCenter = new List<string>();

        foreach (System.Reflection.FieldInfo field in typeof(UIStyles.Symbols).GetFields())
        {
            if (!field.IsLiteral)
                continue;

            string symbol = (string)field.GetRawConstantValue()!;

            foreach (UIColorSet? colors in new UIColorSet?[] { null, UIStyles.Colors.Sets.Teal })
            {
                using Button button = UIStyles.Buttons.CreateSymbol(symbol, size: new Size(size, size), colors: colors);
                var offset = InkOffset(button);

                if (offset == null)
                {
                    offCenter.Add(field.Name + " is not drawn at all");
                }
                else if (Math.Abs(offset.Value.X) > 1.0 || Math.Abs(offset.Value.Y) > 1.0)
                {
                    offCenter.Add($"{field.Name} ({offset.Value.X:F1}, {offset.Value.Y:F1})");
                }
            }
        }

        Assert.True(offCenter.Count == 0, "off center by more than a pixel: " + string.Join(", ", offCenter));
    }

    [Fact]
    public void TheFill_FollowsTheState_AndTheOutlineIsDrawn()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add, size: new Size(40, 40), colors: set);

        Color Corner()
        {
            using Bitmap bitmap = Draw(button);
            return bitmap.GetPixel(4, 4);
        }

        Color Edge()
        {
            using Bitmap bitmap = Draw(button);
            return bitmap.GetPixel(0, 20);
        }

        Assert.Equal(set.Basic, Corner());
        Assert.Equal(Color.FromArgb(50, 50, 50), Edge());

        // Every pixel of the outline, the four corners included.
        using (Bitmap outline = Draw(button))
        {
            for (int i = 0; i < outline.Width; i++)
            {
                Assert.Equal(Color.FromArgb(50, 50, 50), outline.GetPixel(i, 0));
                Assert.Equal(Color.FromArgb(50, 50, 50), outline.GetPixel(i, outline.Height - 1));
            }

            for (int i = 0; i < outline.Height; i++)
            {
                Assert.Equal(Color.FromArgb(50, 50, 50), outline.GetPixel(0, i));
                Assert.Equal(Color.FromArgb(50, 50, 50), outline.GetPixel(outline.Width - 1, i));
            }
        }

        Raise(button, "OnMouseEnter", EventArgs.Empty);
        Assert.Equal(set.Hover, Corner());

        Raise(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.Equal(set.Pressed, Corner());

        Raise(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Raise(button, "OnMouseLeave", EventArgs.Empty);
        Assert.Equal(set.Basic, Corner());

        button.Enabled = false;
        Assert.Equal(set.Disabled, Corner());
    }

    [Fact]
    public void TheSymbol_IsDrawnInTheTextColorOfTheState()
    {
        UIColorSet set = UIStyles.Colors.Sets.Green;
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add, size: new Size(40, 40), colors: set);

        // The plus is a thick cross: its middle is solid ink.
        Color Middle()
        {
            using Bitmap bitmap = Draw(button);
            return bitmap.GetPixel(20, 20);
        }

        Assert.Equal(set.BasicText, Middle());

        Raise(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.Equal(set.PressedText, Middle());
        Raise(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        button.Enabled = false;
        Assert.Equal(set.DisabledText, Middle());
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
        Assert.Equal("Pick", ButtonTestsToolTip.Text(browse));

        var offset = InkOffset(browse);

        Assert.NotNull(offset);
        Assert.InRange(offset!.Value.X, -1.0, 1.0);
        Assert.InRange(offset.Value.Y, -1.0, 1.0);
    }

    [Fact]
    public void TheYellowSet_HasTheLookOfTheOldBrowseButton_InEveryState()
    {
        using Button browse = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Folder, colors: UIStyles.Colors.Sets.Yellow);
        Color disabled = UIColors.Darken(UIColors.Yellow, 0.35);

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

    [Fact]
    public void ThePressedAndKeyBehavior_IsTheSameAsOnAnyOtherButton()
    {
        // It is built on the same button: the space bar presses it and a
        // disabled one ignores the mouse.
        UIColorSet set = UIStyles.Colors.CreateSet(Color.FromArgb(250, 220, 60));
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Heart, colors: set);

        Raise(button, "OnKeyDown", new KeyEventArgs(Keys.Space));
        Assert.Equal(set.PressedText, button.ForeColor);
        Raise(button, "OnKeyUp", new KeyEventArgs(Keys.Space));
        Assert.Equal(set.BasicText, button.ForeColor);

        button.Enabled = false;
        Raise(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.Equal(set.DisabledText, button.ForeColor);
    }
}

// The tooltip each button got, read back from the factory's own table.
internal static class ButtonTestsToolTip
{
    public static string Text(Button button)
    {
        System.Reflection.FieldInfo field = typeof(ErikwnkWFUI.Factories.UIButtonFactory)
            .GetField("_tooltips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        object table = field.GetValue(null)!;
        object?[] args = { button, null };
        bool found = (bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, args)!;

        return found ? ((ToolTip)args[1]!).GetToolTip(button) ?? "" : "";
    }
}
