using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// The buttons are plain System.Windows.Forms.Button instances that
/// UIStyles.Buttons dresses up, so these tests cover what the factory adds:
/// the variant colors, the disabled and pressed looks, and the tooltip.
/// [Collection] (see AccentColorTestCollection) - the colors read the live
/// accent, which UIColorsTests changes via SetAccent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ButtonTests
{
    public static IEnumerable<object[]> TextVariants()
    {
        yield return new object[] { "Standard" };
        yield return new object[] { "Primary" };
        yield return new object[] { "Green" };
        yield return new object[] { "Red" };
    }

    private static Button Create(string variant, string text = "", string tooltip = "", Size? size = null, bool isIcon = false)
    {
        return variant switch
        {
            "Standard" => UIStyles.Buttons.CreateStandard(text, tooltip, size, isIcon),
            "Primary" => UIStyles.Buttons.CreatePrimary(text, tooltip, size, isIcon),
            "Green" => UIStyles.Buttons.CreateGreen(text, tooltip, size, isIcon),
            "Red" => UIStyles.Buttons.CreateRed(text, tooltip, size, isIcon),
            _ => UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Folder, tooltip, size, UIStyles.Colors.Sets.Yellow)
        };
    }

    private static void Press(Button button)
    {
        button.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
    }

    private static void Release(Button button)
    {
        button.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
    }

    private static void Leave(Button button)
    {
        button.InvokePrivate("OnMouseLeave", EventArgs.Empty);
    }

    // ---- construction ----

    [Theory]
    [MemberData(nameof(TextVariants))]
    public void TextTooltipAndSize_AreApplied(string variant)
    {
        using Button defaults = Create(variant, "Save", "Saves it");

        Assert.Equal("Save", defaults.Text);
        Assert.Equal("Saves it", ButtonToolTipReader.Text(defaults));
        Assert.Equal(new Size(30, 30), defaults.Size);
        Assert.Equal(FlatStyle.Flat, defaults.FlatStyle);
        Assert.Equal(Cursors.Hand, defaults.Cursor);

        using Button sized = Create(variant, "Save", size: new Size(120, 40));

        Assert.Equal(new Size(120, 40), sized.Size);
        Assert.Equal("", ButtonToolTipReader.Text(sized));
    }

    [Fact]
    public void IsIcon_SwitchesToTheIconFontAndDropsThePadding()
    {
        using Button text = UIStyles.Buttons.CreateStandard("Go");
        using Button icon = UIStyles.Buttons.CreateStandard("Go", isIcon: true);

        Assert.Equal(UIFonts.Normal, text.Font);
        Assert.Equal(new Padding(6, 0, 6, 0), text.Padding);
        Assert.Equal(UIFonts.Icon, icon.Font);
        Assert.Equal(Padding.Empty, icon.Padding);
    }

    [Fact]
    public void AFolderOnYellow_ShowsTheGlyph_WhiteOnYellow()
    {
        using Button browse = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Folder, "Pick a folder", colors: UIStyles.Colors.Sets.Yellow);

        Assert.Equal(UIStyles.Symbols.Folder, browse.Text);
        Assert.Equal("Pick a folder", ButtonToolTipReader.Text(browse));
        Assert.Equal(UIFonts.Icon, browse.Font);
        Assert.Equal(UIColors.Yellow, browse.BackColor);
        Assert.Equal(UIColors.White, browse.ForeColor);
    }

    [Fact]
    public void EachVariant_HasItsOwnFill_AndReadableText()
    {
        (string Variant, Color Back)[] expected =
        {
            ("Standard", UIColors.BackgroundMedium),
            ("Primary", UIColors.PrimaryDark),
            ("Green", UIColors.GreenDark),
            ("Red", UIColors.RedDark)
        };

        foreach ((string variant, Color back) in expected)
        {
            using Button button = Create(variant, "Text");

            Assert.Equal(back, button.BackColor);
            Assert.True(
                ColorContrastHelper.MeetsMinimumContrast(button.ForeColor, button.BackColor, ColorContrastHelper.MinimumRatioNormalText),
                variant + " text is hard to read");
        }
    }

    [Fact]
    public void Primary_FollowsTheAccent_Standard_DoesNot()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Purple);

            using Button primary = UIStyles.Buttons.CreatePrimary("P");
            using Button standard = UIStyles.Buttons.CreateStandard("S");

            Assert.Equal(UIColors.PrimaryDark, primary.BackColor);
            Assert.Equal(UIColors.BackgroundMedium, standard.BackColor);
            Assert.NotEqual(UIAccentColors.Blue, UIColors.Primary);
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void HoverFillAndBorder_AreFixedPerVariant()
    {
        (string Variant, int Border, Color Over, Color Down)[] expected =
        {
            ("Standard", 1, UIColors.BackgroundLighter, UIColors.Primary),
            ("Primary", 0, UIColors.Primary, UIColors.PrimaryLight),
            ("Green", 1, UIColors.Green, UIColors.GreenLight),
            ("Red", 1, UIColors.Red, UIColors.RedLight),
            ("Browse", 1, UIColors.YellowLight, UIColors.YellowLighter)
        };

        foreach ((string variant, int border, Color over, Color down) in expected)
        {
            using Button button = Create(variant, "Text");

            Assert.Equal(border, button.FlatAppearance.BorderSize);
            Assert.Equal(UIColors.BorderDark, button.FlatAppearance.BorderColor);
            Assert.Equal(over, button.FlatAppearance.MouseOverBackColor);
            Assert.Equal(down, button.FlatAppearance.MouseDownBackColor);
        }
    }

    // ---- disabled ----

    [Theory]
    [InlineData("Standard")]
    [InlineData("Primary")]
    [InlineData("Green")]
    [InlineData("Red")]
    [InlineData("Browse")]
    public void Disabled_DarkensTheFill_AndKeepsTheTextVisible(string variant)
    {
        using Button button = Create(variant, "Text");
        Color enabledBack = button.BackColor;
        Color enabledFore = button.ForeColor;

        button.Enabled = false;

        Assert.True(button.BackColor.GetBrightness() < enabledBack.GetBrightness(), "fill is not darker");
        Assert.NotEqual(enabledFore, button.ForeColor);

        // The yellow Browse button once ended up with text at a ratio of
        // about 1.0 against its disabled fill - invisible.
        Assert.True(
            ColorContrastHelper.MeetsMinimumContrast(button.ForeColor, button.BackColor, ColorContrastHelper.MinimumRatioLargeTextOrUiComponents),
            variant + " disabled text is not visible");
    }

    [Fact]
    public void ReEnabling_RestoresTheColors_AndKeepsAManualOverride()
    {
        using Button plain = UIStyles.Buttons.CreateGreen("Go");
        Color back = plain.BackColor;
        Color fore = plain.ForeColor;

        plain.Enabled = false;
        plain.Enabled = true;

        Assert.Equal(back, plain.BackColor);
        Assert.Equal(fore, plain.ForeColor);

        using Button recolored = UIStyles.Buttons.CreateGreen("Go");
        recolored.BackColor = Color.Orange;
        recolored.ForeColor = Color.Black;

        recolored.Enabled = false;
        recolored.Enabled = true;

        Assert.Equal(Color.Orange, recolored.BackColor);
        Assert.Equal(Color.Black, recolored.ForeColor);
    }

    [Fact]
    public void DisabledText_IsPaintedByTheFactory_NotTheFrameworksEmbossedOne()
    {
        using Button button = UIStyles.Buttons.CreateStandard("Hello", size: new Size(120, 40));
        button.Enabled = false;
        _ = button.Handle;

        using Bitmap bitmap = new Bitmap(120, 40);
        button.DrawToBitmap(bitmap, new Rectangle(0, 0, 120, 40));

        // Somewhere in the middle a pixel has the muted text color
        // (blended, so close to it, not necessarily equal).
        bool textFound = false;

        for (int x = 20; x < 100 && !textFound; x++)
        {
            for (int y = 10; y < 30 && !textFound; y++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                textFound = Math.Abs(pixel.R - button.ForeColor.R) < 12
                    && Math.Abs(pixel.G - button.ForeColor.G) < 12
                    && Math.Abs(pixel.B - button.ForeColor.B) < 12;
            }
        }

        Assert.True(textFound);
    }

    // ---- pressed ----

    [Fact]
    public void Pressing_RecolorsTheText_ForThePressedFill_AndReleasingRestoresIt()
    {
        using Button button = UIStyles.Buttons.CreateStandard("Go");

        // A light pressed fill, so the contrasting text differs from the
        // light idle text and the test can tell the two apart.
        button.FlatAppearance.MouseDownBackColor = Color.White;
        Color idle = button.ForeColor;

        Press(button);
        Assert.Equal(UIColors.DarkForeColor, button.ForeColor);
        Assert.NotEqual(idle, button.ForeColor);

        Release(button);
        Assert.Equal(idle, button.ForeColor);
    }

    [Fact]
    public void Pressing_UsesTheCurrentPressedFill_NotTheOneFromCreation()
    {
        using Button button = UIStyles.Buttons.CreateStandard("Go");
        button.FlatAppearance.MouseDownBackColor = Color.White;

        Press(button);

        Assert.Equal(UIColors.DarkForeColor, button.ForeColor);
    }

    [Fact]
    public void LeavingWithoutAPress_LeavesTheTextAlone_LeavingDuringOneRestoresIt()
    {
        using Button button = UIStyles.Buttons.CreateStandard("Go");
        button.ForeColor = Color.Orange;

        Leave(button);
        Assert.Equal(Color.Orange, button.ForeColor);

        Press(button);
        Leave(button);
        Assert.Equal(Color.Orange, button.ForeColor);
    }

    [Fact]
    public void Pressing_AFixedTextButton_KeepsItsColor()
    {
        using Button browse = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Folder, colors: UIStyles.Colors.Sets.Yellow);

        Press(browse);

        Assert.Equal(UIColors.White, browse.ForeColor);
    }

    [Fact]
    public void Pressing_ADisabledButton_ChangesNothing()
    {
        using Button button = UIStyles.Buttons.CreateStandard("Go");
        button.Enabled = false;
        Color muted = button.ForeColor;

        Press(button);

        Assert.Equal(muted, button.ForeColor);
    }

    // ---- found in review: pressed state edge cases ----

    private static void Key(Button button, string handler, Keys key)
    {
        button.InvokePrivate(handler, new KeyEventArgs(key));
    }

    private static Button CreateWithLightPressedFill()
    {
        // A light pressed fill makes the contrasting text differ from the
        // light idle text, so the tests can tell the two apart.
        Button button = UIStyles.Buttons.CreateStandard("Go");
        button.FlatAppearance.MouseDownBackColor = Color.White;
        return button;
    }

    [Fact]
    public void ARightClick_DoesNotRecolorTheText()
    {
        // The fill only reacts to the left button; the text must not either,
        // or it flashes in a color meant for a fill that never appeared.
        using Button button = CreateWithLightPressedFill();
        Color idle = button.ForeColor;

        button.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Right, 1, 5, 5, 0));

        Assert.Equal(idle, button.ForeColor);
    }

    [Fact]
    public void TheSpaceKey_PressesLikeTheMouse_AndLosingFocusEndsIt()
    {
        using Button button = CreateWithLightPressedFill();
        Color idle = button.ForeColor;

        Key(button, "OnKeyDown", Keys.Space);
        Assert.Equal(UIColors.DarkForeColor, button.ForeColor);

        Key(button, "OnKeyUp", Keys.Space);
        Assert.Equal(idle, button.ForeColor);

        Key(button, "OnKeyDown", Keys.Space);
        button.InvokePrivate("OnLostFocus", EventArgs.Empty);
        Assert.Equal(idle, button.ForeColor);

        Key(button, "OnKeyDown", Keys.Enter);
        Assert.Equal(idle, button.ForeColor);
    }

    [Fact]
    public void PressingTwiceWithoutARelease_StillRestoresTheIdleText()
    {
        using Button button = CreateWithLightPressedFill();
        Color idle = button.ForeColor;

        Press(button);
        Press(button);
        Release(button);

        Assert.Equal(idle, button.ForeColor);
    }

    [Fact]
    public void DisablingWhilePressed_AsAClickHandlerDoes_KeepsTheDisabledAndTheEnabledLook()
    {
        // Click is raised before MouseUp, so a handler that disables the
        // button runs while the pressed text color is still in place.
        using Button button = CreateWithLightPressedFill();
        Color idle = button.ForeColor;

        Press(button);
        button.Enabled = false;
        Color muted = button.ForeColor;
        Release(button);

        Assert.NotEqual(idle, muted);
        Assert.Equal(muted, button.ForeColor);

        button.Enabled = true;

        Assert.Equal(idle, button.ForeColor);
    }

    // ---- tooltip ----

    [Fact]
    public void TheTooltip_IsReleasedWithItsButton()
    {
        Button button = UIStyles.Buttons.CreateStandard("Go", "Tip");
        ToolTip toolTip = ButtonToolTipReader.Find(button)!;
        bool disposed = false;
        toolTip.Disposed += (sender, e) => disposed = true;

        button.Dispose();

        Assert.True(disposed);
    }

    [Fact]
    public void UpdateTooltip_ChangesTheText_AndIgnoresButtonsWithoutOne()
    {
        using Button withTip = UIStyles.Buttons.CreateStandard("Go", "Old");
        using Button withoutTip = UIStyles.Buttons.CreateStandard("Go");

        UIStyles.Buttons.UpdateTooltip(withTip, "Neu");
        UIStyles.Buttons.UpdateTooltip(withTip, null!);
        Assert.Equal("", ButtonToolTipReader.Text(withTip));

        UIStyles.Buttons.UpdateTooltip(withTip, "Neu");
        Assert.Equal("Neu", ButtonToolTipReader.Text(withTip));

        UIStyles.Buttons.UpdateTooltip(withoutTip, "Never shown");
        Assert.Equal("", ButtonToolTipReader.Text(withoutTip));

        UIStyles.Buttons.UpdateTooltip(null!, "Anything");
    }

    // ---- the standard button's own features keep working on the themed one ----

    private static Bitmap DrawButton(Button button)
    {
        _ = button.Handle;
        Bitmap bitmap = new Bitmap(button.Width, button.Height);
        button.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, button.Width, button.Height));

        return bitmap;
    }

    // The leftmost column that holds a pixel other than the fill, inside the border.
    private static int FirstInkColumn(Button button)
    {
        using Bitmap bitmap = DrawButton(button);
        System.Drawing.Color back = button.BackColor;

        for (int x = 3; x < bitmap.Width - 3; x++)
        {
            for (int y = 3; y < bitmap.Height - 3; y++)
            {
                System.Drawing.Color c = bitmap.GetPixel(x, y);

                if (Math.Abs(c.R - back.R) + Math.Abs(c.G - back.G) + Math.Abs(c.B - back.B) > 90)
                    return x;
            }
        }

        return -1;
    }

    [Fact]
    public void TheTextAlignment_AndAnImage_StillWork_OnATextButton()
    {
        Button centered = UIStyles.Buttons.CreateStandard("Text", size: new System.Drawing.Size(140, 30));
        Button left = UIStyles.Buttons.CreateStandard("Text", size: new System.Drawing.Size(140, 30));
        left.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        Assert.True(FirstInkColumn(left) < FirstInkColumn(centered) - 20, "the text did not move to the left");

        Button withImage = UIStyles.Buttons.CreateStandard("Text", size: new System.Drawing.Size(140, 30));
        using System.Drawing.Bitmap image = new System.Drawing.Bitmap(16, 16);

        using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(image))
            g.Clear(System.Drawing.Color.Orange);

        withImage.Image = image;
        withImage.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;

        Assert.True(FirstInkColumn(withImage) < FirstInkColumn(centered) - 20, "the image is not shown");
    }
}
