using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The symbols are plain characters, so what can go wrong is a code point
/// that no font has (it would show as an empty box), the same character under
/// two names, invisible extra characters, or the wrong font or size for one.
/// Whether Windows can draw each one is checked by drawing it, the way a
/// button would.
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
    // know the first 65536 characters, and the emoji symbols are beyond.
    private static string Render(string glyph)
    {
        using Font font = new Font(UIStyles.Symbols.GetFontName(glyph), 20f);
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
            Assert.True(glyph.Length <= (IsEmojiSymbol(glyph) ? 3 : 2), name + " is more than one character");
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

    [Fact]
    public void TheFontOfASymbol_IsTheUsualSymbolFont_ExceptForTheIconFontOnes()
    {
        string usual = UIFonts.Icon.FontFamily.Name;

        Assert.Equal(usual, UIStyles.Symbols.GetFontName(UIStyles.Symbols.Add));
        Assert.Equal(usual, UIStyles.Symbols.GetFontName(UIStyles.Symbols.Folder));
        Assert.Equal(usual, UIStyles.Symbols.GetFontName(""));
        Assert.Equal(usual, UIStyles.Symbols.GetFontName(null!));

        // Delete comes from the Windows icon font where there is one.
        string deleteFont = UIStyles.Symbols.GetFontName(UIStyles.Symbols.Delete);
        bool hasIconFont = new System.Drawing.Text.InstalledFontCollection().Families.Any(f => f.Name == "Segoe MDL2 Assets");

        Assert.Equal(hasIconFont ? "Segoe MDL2 Assets" : usual, deleteFont);
    }

    private static bool IsInstalled(string family) =>
        new System.Drawing.Text.InstalledFontCollection().Families.Any(f => f.Name == family);

    [Fact]
    public void EverySymbolFromTheIconFontRange_GetsAnIconFont_AndTheOthersTheUsualOne()
    {
        string usual = UIFonts.Icon.FontFamily.Name;
        List<string> wrong = new List<string>();

        foreach ((string name, string glyph) in AllSymbols())
        {
            bool isIconFontSymbol = glyph[0] >= '' && glyph[0] <= '';
            string font = UIStyles.Symbols.GetFontName(glyph);

            bool isRight = IsEmojiSymbol(glyph)
                ? font == "Segoe UI Emoji" || (font == usual && !IsInstalled("Segoe UI Emoji"))
                : isIconFontSymbol
                    ? font == "Segoe MDL2 Assets" || font == "Segoe Fluent Icons" || (font == usual && !IsInstalled("Segoe MDL2 Assets"))
                    : font == usual;

            if (!isRight)
                wrong.Add(name + " -> " + font);
        }

        Assert.True(wrong.Count == 0, "wrong font: " + string.Join(", ", wrong));
    }

    [Fact]
    public void Copy_UsesTheNewerIconFont_WhereThereIsOne_AndEverythingElseTheOlderOne()
    {
        if (!IsInstalled("Segoe MDL2 Assets"))
            return;

        Assert.Equal(
            IsInstalled("Segoe Fluent Icons") ? "Segoe Fluent Icons" : "Segoe MDL2 Assets",
            UIStyles.Symbols.GetFontName(UIStyles.Symbols.Copy));

        Assert.Equal("Segoe MDL2 Assets", UIStyles.Symbols.GetFontName(UIStyles.Symbols.Lightning));
        Assert.Equal("Segoe MDL2 Assets", UIStyles.Symbols.GetFontName(UIStyles.Symbols.Delete));
    }

    [Fact]
    public void TheIconFontRange_IsDecidedByTheFirstCharacter_AndItsEdgesExactly()
    {
        if (!IsInstalled("Segoe MDL2 Assets"))
            return;

        string usual = UIFonts.Icon.FontFamily.Name;

        Assert.Equal("Segoe MDL2 Assets", UIStyles.Symbols.GetFontName("\uE000"));
        Assert.Equal("Segoe MDL2 Assets", UIStyles.Symbols.GetFontName("\uF8FF"));
        Assert.Equal(usual, UIStyles.Symbols.GetFontName("\uDFFF"));
        Assert.Equal(usual, UIStyles.Symbols.GetFontName("\uF900"));

        // Only the first character counts, and an emoji (a surrogate pair) is not in the range.
        Assert.Equal("Segoe MDL2 Assets", UIStyles.Symbols.GetFontName("\uE74D" + "x"));
        Assert.Equal(usual, UIStyles.Symbols.GetFontName("x" + "\uE74D"));
        Assert.Equal(usual, UIStyles.Symbols.GetFontName(UIStyles.Symbols.Save));
    }

    [Fact]
    public void OnlyTheCopySymbol_ItselfGetsTheNewerFont()
    {
        if (!IsInstalled("Segoe MDL2 Assets") || !IsInstalled("Segoe Fluent Icons"))
            return;

        Assert.Equal("Segoe Fluent Icons", UIStyles.Symbols.GetFontName(UIStyles.Symbols.Copy));

        // Not another symbol, not the same character with something after it.
        Assert.Equal("Segoe MDL2 Assets", UIStyles.Symbols.GetFontName(UIStyles.Symbols.Paste));
        Assert.Equal("Segoe MDL2 Assets", UIStyles.Symbols.GetFontName(UIStyles.Symbols.Copy + "x"));
    }

    [Fact]
    public void EveryFontNameAFontIsAskedFor_IsAnInstalledFamily_NotASilentDefault()
    {
        // A font that is not installed is replaced by a default without a word: the
        // name that comes back must be the name that is really used.
        List<string> notReal = new List<string>();

        foreach ((string name, string glyph) in AllSymbols())
        {
            string family = UIStyles.Symbols.GetFontName(glyph);
            using Font font = new Font(family, 12f);

            if (font.FontFamily.Name != family)
                notReal.Add(name + " -> " + family);
        }

        Assert.True(notReal.Count == 0, "not an installed font: " + string.Join(", ", notReal));
    }

    [Fact]
    public void OnlyTheChevronsAndTheLightningBolt_AreDrawnSmaller()
    {
        string[] smaller =
        {
            UIStyles.Symbols.ChevronLeft, UIStyles.Symbols.ChevronRight,
            UIStyles.Symbols.ChevronUp, UIStyles.Symbols.ChevronDown, UIStyles.Symbols.Lightning
        };

        foreach ((string name, string glyph) in AllSymbols())
        {
            float factor = UIStyles.Symbols.GetSizeFactor(glyph);

            if (smaller.Contains(glyph))
                Assert.True(factor > 0.5f && factor < 1f, name + " should be drawn smaller, factor " + factor);
            else
                Assert.True(factor == 1f, name + " should be drawn at full size, factor " + factor);
        }

        Assert.Equal(1f, UIStyles.Symbols.GetSizeFactor(null!));
        Assert.Equal(1f, UIStyles.Symbols.GetSizeFactor(""));
    }

    [Fact]
    public void EverySymbol_IsOneCharacterOrOneValidPair_WithoutInvisibleExtras()
    {
        // A variation selector (U+FE0F asks for the colored emoji) or a joiner would
        // change how the symbol looks or glue symbols together. The one exception is the
        // emoji selector at the very end of a symbol that is meant to be drawn in the
        // emoji font (UIStyles.Symbols.Document: the same page as File, in the other font):
        // there it is the mark that says so.
        List<string> problems = new List<string>();

        foreach ((string name, string glyph) in AllSymbols())
        {
            string visible = IsEmojiSymbol(glyph) ? glyph.Substring(0, glyph.Length - 1) : glyph;

            if (visible.Length == 2 && !(char.IsHighSurrogate(visible[0]) && char.IsLowSurrogate(visible[1])))
                problems.Add(name + " is two characters that are not one emoji");

            foreach (char c in visible)
            {
                if ((c >= '︀' && c <= '️') || c == '‍' || c == '​' || char.IsWhiteSpace(c))
                    problems.Add(name + " contains an invisible character U+" + ((int)c).ToString("X4"));
            }

            if (IsEmojiSymbol(glyph) && IsInstalled("Segoe UI Emoji") && UIStyles.Symbols.GetFontName(glyph) != "Segoe UI Emoji")
                problems.Add(name + " ends with the emoji selector but is not drawn in the emoji font");
        }

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    // A symbol that ends with the emoji selector (U+FE0F) is drawn in the emoji font.
    private static bool IsEmojiSymbol(string glyph) => glyph.Length > 1 && glyph[glyph.Length - 1] == '️';
}
