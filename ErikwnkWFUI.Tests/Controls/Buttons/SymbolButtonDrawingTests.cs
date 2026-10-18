using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// The symbol button: centered symbols and the fill of every state.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class SymbolButtonDrawingTests : SymbolButtonTestBase
{
    // ---- drawing: centered symbols, and the fill of every state ----

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
}
