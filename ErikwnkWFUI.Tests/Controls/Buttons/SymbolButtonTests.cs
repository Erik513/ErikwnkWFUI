using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
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

    // The box around everything drawn in the symbol's color.
    private static Rectangle? InkBounds(Button button)
    {
        using Bitmap bitmap = Draw(button);
        Color back = button.BackColor;
        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;

        for (int y = 3; y < bitmap.Height - 3; y++)
        {
            for (int x = 3; x < bitmap.Width - 3; x++)
            {
                Color c = bitmap.GetPixel(x, y);

                if (Math.Abs(c.R - back.R) + Math.Abs(c.G - back.G) + Math.Abs(c.B - back.B) > 90)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        return maxX < 0 ? null : new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    [Theory]
    [InlineData(24)]
    [InlineData(30)]
    [InlineData(48)]
    public void EverySymbol_IsNeitherTinyNorCrowdingTheEdge(int size)
    {
        // At the standard size a symbol has to be seen (not a dot) and to leave room
        // around it; the bars and lines (Minimize, Menu, Remove) are thin by nature.
        List<string> problems = new List<string>();
        int margin = size / 8;

        foreach (System.Reflection.FieldInfo field in typeof(UIStyles.Symbols).GetFields())
        {
            if (!field.IsLiteral)
                continue;

            using Button button = UIStyles.Buttons.CreateSymbol((string)field.GetRawConstantValue()!, size: new Size(size, size), colors: UIStyles.Colors.Sets.Teal);
            Rectangle? ink = InkBounds(button);

            if (ink == null)
            {
                problems.Add(field.Name + " is not drawn");
                continue;
            }

            int longest = Math.Max(ink.Value.Width, ink.Value.Height);
            bool touchesEdge = ink.Value.Left < margin || ink.Value.Top < margin || ink.Value.Right > size - margin || ink.Value.Bottom > size - margin;

            if (longest < size * 0.3)
                problems.Add($"{field.Name} is tiny ({ink.Value.Width}x{ink.Value.Height})");

            if (touchesEdge)
                problems.Add($"{field.Name} crowds the edge ({ink.Value})");
        }

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Theory]
    [InlineData("Delete")]
    [InlineData("Folder")]
    [InlineData("Heart")]
    [InlineData("Play")]
    [InlineData("Lock")]
    public void ASymbol_IsDrawnInTheTextColorOfTheState_WhateverFontItComesFrom(string name)
    {
        // From the icon font (Delete), an emoji (Folder, Lock), a text character (Heart, Play).
        string symbol = (string)typeof(UIStyles.Symbols).GetField(name)!.GetRawConstantValue()!;
        UIColorSet set = UIStyles.Colors.Sets.Green;
        using Button button = UIStyles.Buttons.CreateSymbol(symbol, size: new Size(48, 48), colors: set);

        bool Has(Color color)
        {
            using Bitmap bitmap = Draw(button);

            for (int y = 3; y < bitmap.Height - 3; y++)
            {
                for (int x = 3; x < bitmap.Width - 3; x++)
                {
                    if (bitmap.GetPixel(x, y) == color)
                        return true;
                }
            }

            return false;
        }

        Assert.True(Has(set.BasicText), name + " at rest");

        Raise(button, "OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
        Assert.True(Has(set.PressedText), name + " pressed");
        Raise(button, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        button.Enabled = false;
        Assert.True(Has(set.DisabledText), name + " disabled");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EverySymbol_IsSeenAndCentered_OnTheNeutralButton_InBothThemes(bool lightTheme)
    {
        // The neutral button follows the theme: dark symbols on a light fill and the
        // other way round. The symbol must be there and in the middle in both.
        List<string> problems = new List<string>();

        try
        {
            UIColors.ApplyTheme(lightTheme ? UIThemes.Light : UIThemes.Dark);

            foreach (System.Reflection.FieldInfo field in typeof(UIStyles.Symbols).GetFields())
            {
                if (!field.IsLiteral)
                    continue;

                using Button button = UIStyles.Buttons.CreateSymbol((string)field.GetRawConstantValue()!);
                var offset = InkOffset(button);

                if (offset == null)
                    problems.Add(field.Name + " is not seen");
                else if (Math.Abs(offset.Value.X) > 1.0 || Math.Abs(offset.Value.Y) > 1.0)
                    problems.Add($"{field.Name} ({offset.Value.X:F1}, {offset.Value.Y:F1})");
            }
        }
        finally
        {
            UIColors.ApplyTheme(UIThemes.Dark);
        }

        Assert.True(problems.Count == 0, (lightTheme ? "Light: " : "Dark: ") + string.Join(", ", problems));
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

    // ---- the symbol button follows hover and press itself: odd input ----

    private static Color FillOf(Button button)
    {
        using Bitmap bitmap = Draw(button);
        return bitmap.GetPixel(4, 4);
    }

    private static void MouseDown(Button button, MouseButtons which = MouseButtons.Left)
    {
        Raise(button, "OnMouseDown", new MouseEventArgs(which, 1, 5, 5, 0));
    }

    private static void MouseUp(Button button, MouseButtons which = MouseButtons.Left)
    {
        Raise(button, "OnMouseUp", new MouseEventArgs(which, 1, 5, 5, 0));
    }

    private static Button CreateTeal() =>
        UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Add, size: new Size(40, 40), colors: UIStyles.Colors.Sets.Teal);

    [Theory]
    [InlineData(MouseButtons.Right)]
    [InlineData(MouseButtons.Middle)]
    public void AnotherMouseButton_DoesNotShowThePressedFill(MouseButtons which)
    {
        using Button button = CreateTeal();

        Raise(button, "OnMouseEnter", EventArgs.Empty);
        MouseDown(button, which);

        Assert.Equal(UIStyles.Colors.Sets.Teal.Hover, FillOf(button));
    }

    [Fact]
    public void TheSpaceKey_ShowsThePressedFill_OtherKeysDoNot_AndLosingFocusEndsIt()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = CreateTeal();

        Raise(button, "OnKeyDown", new KeyEventArgs(Keys.Enter));
        Assert.Equal(set.Basic, FillOf(button));

        Raise(button, "OnKeyUp", new KeyEventArgs(Keys.Space));
        Assert.Equal(set.Basic, FillOf(button));

        Raise(button, "OnKeyDown", new KeyEventArgs(Keys.Space));
        Assert.Equal(set.Pressed, FillOf(button));

        Raise(button, "OnLostFocus", EventArgs.Empty);
        Assert.Equal(set.Basic, FillOf(button));
    }

    [Fact]
    public void LeavingWhilePressed_EndsThePress_AndComingBackShowsHoverNotPressed()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = CreateTeal();

        Raise(button, "OnMouseEnter", EventArgs.Empty);
        MouseDown(button);
        Raise(button, "OnMouseLeave", EventArgs.Empty);
        Assert.Equal(set.Basic, FillOf(button));

        Raise(button, "OnMouseEnter", EventArgs.Empty);
        Assert.Equal(set.Hover, FillOf(button));
    }

    [Fact]
    public void PressingTwice_ThenReleasingOnce_LeavesNoPressedFillBehind()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = CreateTeal();

        Raise(button, "OnMouseEnter", EventArgs.Empty);
        MouseDown(button);
        MouseDown(button);
        MouseUp(button);

        Assert.Equal(set.Hover, FillOf(button));
    }

    [Fact]
    public void ADisabledButton_IgnoresPressAndHover_AndComesBackIdle()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = CreateTeal();

        button.Enabled = false;
        Raise(button, "OnMouseEnter", EventArgs.Empty);
        MouseDown(button);
        Assert.Equal(set.Disabled, FillOf(button));

        // Whatever arrived while it was disabled must not show once it is enabled.
        Raise(button, "OnMouseLeave", EventArgs.Empty);
        button.Enabled = true;
        Assert.Equal(set.Basic, FillOf(button));
    }

    [Fact]
    public void DisablingWhilePressed_AsAClickHandlerDoes_DoesNotLeaveItStuckPressed()
    {
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = CreateTeal();
        button.Click += delegate { button.Enabled = false; };

        // WinForms raises Click before MouseUp.
        Raise(button, "OnMouseEnter", EventArgs.Empty);
        MouseDown(button);
        Raise(button, "OnClick", EventArgs.Empty);
        MouseUp(button);
        Raise(button, "OnMouseLeave", EventArgs.Empty);
        Assert.Equal(set.Disabled, FillOf(button));

        button.Enabled = true;
        Assert.Equal(set.Basic, FillOf(button));
    }

    [Fact]
    public void BeingDisabledMidPress_WithoutTheReleaseEverArriving_DoesNotStayPressed()
    {
        // A disabled control gets no MouseUp: the press must not outlive it.
        UIColorSet set = UIStyles.Colors.Sets.Teal;
        using Button button = CreateTeal();

        Raise(button, "OnMouseEnter", EventArgs.Empty);
        MouseDown(button);
        button.Enabled = false;
        button.Enabled = true;

        Assert.Equal(set.Hover, FillOf(button));
    }

    [Fact]
    public void ASymbolThatIsEmpty_DrawsJustTheButton()
    {
        using Button empty = UIStyles.Buttons.CreateSymbol("", size: new Size(40, 40), colors: UIStyles.Colors.Sets.Teal);
        using Button missing = UIStyles.Buttons.CreateSymbol(null!, size: new Size(40, 40), colors: UIStyles.Colors.Sets.Teal);

        Assert.Equal(UIStyles.Colors.Sets.Teal.Basic, FillOf(empty));
        Assert.Equal(UIStyles.Colors.Sets.Teal.Basic, FillOf(missing));
        Assert.Equal("", missing.Text);
    }

    private static bool SamePixels(Button first, Button second)
    {
        using Bitmap a = Draw(first);
        using Bitmap b = Draw(second);

        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                if (a.GetPixel(x, y) != b.GetPixel(x, y))
                    return false;
            }
        }

        return true;
    }

    [Fact]
    public void ChangingTheSymbol_OrTheFont_DrawsItLikeAFreshButton()
    {
        // The measured shift belongs to one symbol in one font: a stale one
        // would push the next symbol off the middle. (Only symbols of the usual
        // font: changing the text keeps the font the button was made with.)
        string[] symbols =
        {
            UIStyles.Symbols.Add, UIStyles.Symbols.Up, UIStyles.Symbols.Home, UIStyles.Symbols.Heart,
            UIStyles.Symbols.Play, UIStyles.Symbols.TriangleDown, UIStyles.Symbols.Mail, UIStyles.Symbols.Search
        };

        using Button changing = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Back, size: new Size(48, 48));
        List<string> different = new List<string>();

        foreach (string symbol in symbols)
        {
            changing.Text = symbol;
            using Button fresh = UIStyles.Buttons.CreateSymbol(symbol, size: new Size(48, 48));

            if (!SamePixels(changing, fresh))
                different.Add(symbol);
        }

        using Font larger = new Font(changing.Font.FontFamily, 30f);
        using Font largerToo = new Font(changing.Font.FontFamily, 30f);
        using Button freshLarger = UIStyles.Buttons.CreateSymbol(changing.Text, size: new Size(48, 48));
        changing.Font = larger;
        freshLarger.Font = largerToo;

        if (!SamePixels(changing, freshLarger))
            different.Add("after the font change");

        Assert.True(different.Count == 0, "drawn differently from a fresh button: " + string.Join(", ", different));
    }

    [Fact]
    public void ASymbolButton_IsNamedByItsTooltip_ForAScreenReader_AndTheNameFollowsTheTooltip()
    {
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Save, "Save");
        using Button withoutTip = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Save);
        using Button text = UIStyles.Buttons.CreateStandard("Save", "Saves the file");

        Assert.Equal("Save", button.AccessibleName);

        UIButtonFactory.UpdateTooltip(button, "Speichern");
        Assert.Equal("Speichern", button.AccessibleName);

        // Without a tooltip there is nothing to name it by; a button with a caption
        // is named by that caption, not by its tooltip.
        Assert.True(string.IsNullOrEmpty(withoutTip.AccessibleName));
        Assert.True(string.IsNullOrEmpty(text.AccessibleName));
    }

    [Fact]
    public void ANameSetByTheCaller_IsLeftAloneByATooltipChange()
    {
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Save, "Save");
        button.AccessibleName = "Store document";

        UIButtonFactory.UpdateTooltip(button, "Speichern");

        Assert.Equal("Store document", button.AccessibleName);
    }

    [Fact]
    public void ImageAlignmentPaddingAndTheFlatStyle_AreIgnored_TheSymbolIsAlwaysDrawnInTheMiddle()
    {
        // The symbol button draws the symbol itself, centered on what is visible of it: what a
        // standard button would do with an image, an alignment or padding does not apply.
        using Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Save, size: new Size(60, 36));

        using Bitmap before = Draw(button);
        using Bitmap image = new Bitmap(12, 12);

        button.Image = image;
        button.ImageAlign = ContentAlignment.MiddleRight;
        button.TextAlign = ContentAlignment.BottomLeft;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.Padding = new Padding(12, 2, 0, 8);
        button.AutoEllipsis = true;
        button.UseMnemonic = false;
        button.RightToLeft = RightToLeft.Yes;

        using Bitmap after = Draw(button);

        for (int y = 0; y < before.Height; y++)
        {
            for (int x = 0; x < before.Width; x++)
                Assert.True(before.GetPixel(x, y) == after.GetPixel(x, y), "differs at " + x + "," + y);
        }
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
