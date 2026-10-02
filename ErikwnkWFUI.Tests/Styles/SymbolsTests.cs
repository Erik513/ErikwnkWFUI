using System.Reflection;
using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The symbols are plain characters, so what can go wrong is a code point
/// that no font has (it would show as an empty box), the same character under
/// two names, or a typo. Whether Windows can draw each one is checked by
/// drawing it, the way a button would.
/// </summary>
public class SymbolsTests
{
    private static List<(string Name, string Glyph)> AllSymbols()
    {
        List<(string, string)> symbols = new List<(string, string)>();

        foreach (FieldInfo field in typeof(UIStyles.Symbols).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.IsLiteral)
            {
                symbols.Add((field.Name, (string)field.GetRawConstantValue()!));
            }
        }

        return symbols;
    }

    // How Windows really draws the text: the way a button does, with the
    // symbol font and whatever it falls back to. The glyph lookup APIs only
    // know the first 65536 characters, and half of the symbols are beyond.
    private static string Render(string glyph)
    {
        using Font font = new Font("Segoe UI Symbol", 20f);
        using Bitmap bitmap = new Bitmap(48, 48);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Black);

        TextRenderer.DrawText(
            graphics,
            glyph,
            font,
            new Rectangle(0, 0, 48, 48),
            Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

        System.Text.StringBuilder pixels = new System.Text.StringBuilder();

        for (int y = 0; y < 48; y++)
        {
            for (int x = 0; x < 48; x++)
            {
                pixels.Append(bitmap.GetPixel(x, y).R / 32);
            }
        }

        return pixels.ToString();
    }

    [Fact]
    public void ThereAreSymbols_EachNonEmpty_AndNoneSharesACharacterWithAnother()
    {
        var symbols = AllSymbols();

        Assert.True(symbols.Count >= 40, "expected a real set of symbols, found " + symbols.Count);

        foreach ((string name, string glyph) in symbols)
        {
            Assert.False(string.IsNullOrWhiteSpace(glyph), name + " is empty");
            Assert.True(glyph.Length <= 2, name + " is more than one character");
        }

        var duplicates = symbols.GroupBy(s => s.Glyph).Where(g => g.Count() > 1).Select(g => string.Join("=", g.Select(s => s.Name)));

        Assert.True(!duplicates.Any(), "same character under two names: " + string.Join(", ", duplicates));
    }

    [Fact]
    public void EverySymbol_IsDrawnAsARealGlyph_NotABlankOrAMissingBox()
    {
        // A code point no font has comes out as the same empty box (or
        // nothing) as one that is made up.
        HashSet<string> notRealGlyphs = new HashSet<string>
        {
            Render("͸"),
            Render("￿"),
            Render("􏿽"),
            Render(" ")
        };

        List<string> missing = new List<string>();

        foreach ((string name, string glyph) in AllSymbols())
        {
            if (notRealGlyphs.Contains(Render(glyph)))
            {
                missing.Add(name);
            }
        }

        Assert.True(missing.Count == 0, "drawn as nothing or a missing-glyph box: " + string.Join(", ", missing));
    }
}
