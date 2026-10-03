using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// Button presets: a symbol in fixed colors, passed to <c>CreateSymbol</c>.
/// What is checked is the wiring - each ready-made preset carries the symbol
/// and the color it says, a button made from it looks like the symbol button
/// made by hand, it is the same in Dark and Light and with any accent - and
/// the preset object itself (own presets, the "with" copies).
/// [Collection]: the colors read the live accent and the tests switch themes.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ButtonPresetTests
{
    // Every preset on the class, found by reflection, so a new one is tested without being listed.
    private static (string Name, UIButtonPreset Preset)[] Ready() => typeof(UIStyles.Buttons.Presets)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Select(property => (property.Name, (UIButtonPreset)property.GetValue(null)!))
        .ToArray();

    public static IEnumerable<object[]> Names()
    {
        foreach ((string name, _) in Ready())
        {
            yield return new object[] { name };
        }
    }

    private static UIButtonPreset Get(string name) => Ready().First(entry => entry.Name == name).Preset;

    [Fact]
    public void EverySymbol_HasAPreset_AndNoSymbolHasTwo()
    {
        string[] symbols = typeof(UIStyles.Symbols)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && field.FieldType == typeof(string))
            .Select(field => (string)field.GetRawConstantValue()!)
            .OrderBy(symbol => symbol, StringComparer.Ordinal)
            .ToArray();

        string[] used = Ready()
            .Select(entry => entry.Preset.Symbol)
            .OrderBy(symbol => symbol, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(symbols, used);
    }

    [Fact]
    public void EachPreset_CarriesTheSymbolAndTheColorItIsNamedFor()
    {
        (string, string, UIColorSet)[] expected =
        {
            ("Add", UIStyles.Symbols.Add, UIStyles.Colors.Sets.Green),
            ("Save", UIStyles.Symbols.Save, UIStyles.Colors.Sets.Green),
            ("Delete", UIStyles.Symbols.Delete, UIStyles.Colors.Sets.Red),
            ("Close", UIStyles.Symbols.Close, UIStyles.Colors.Sets.Red),
            ("Edit", UIStyles.Symbols.Edit, UIStyles.Colors.Sets.Blue),
            ("Refresh", UIStyles.Symbols.Refresh, UIStyles.Colors.Sets.Blue),
            ("Login", UIStyles.Symbols.User, UIStyles.Colors.Sets.Blue),
            ("Sync", UIStyles.Symbols.Sync, UIStyles.Colors.Sets.Blue),
            ("Update", UIStyles.Symbols.Update, UIStyles.Colors.Sets.Green),
            ("Search", UIStyles.Symbols.Search, UIStyles.Colors.Sets.Dark),
            ("Settings", UIStyles.Symbols.Settings, UIStyles.Colors.Sets.Dark),
            ("Back", UIStyles.Symbols.Back, UIStyles.Colors.Sets.Dark),
            ("Forward", UIStyles.Symbols.Forward, UIStyles.Colors.Sets.Dark),
            ("Up", UIStyles.Symbols.Up, UIStyles.Colors.Sets.Dark),
            ("Down", UIStyles.Symbols.Down, UIStyles.Colors.Sets.Dark),
            ("Home", UIStyles.Symbols.Home, UIStyles.Colors.Sets.Dark),
            ("OpenFile", UIStyles.Symbols.File, UIStyles.Colors.Sets.Yellow),
            ("Browse", UIStyles.Symbols.Folder, UIStyles.Colors.Sets.Yellow),
            ("Remove", UIStyles.Symbols.Remove, UIStyles.Colors.Sets.Red),
            ("Cancel", UIStyles.Symbols.Cancel, UIStyles.Colors.Sets.Red),
            ("Check", UIStyles.Symbols.Check, UIStyles.Colors.Sets.Green),
            ("Undo", UIStyles.Symbols.Undo, UIStyles.Colors.Sets.Dark),
            ("Redo", UIStyles.Symbols.Redo, UIStyles.Colors.Sets.Dark),
            ("Cut", UIStyles.Symbols.Cut, UIStyles.Colors.Sets.Dark),
            ("Copy", UIStyles.Symbols.Copy, UIStyles.Colors.Sets.Dark),
            ("Paste", UIStyles.Symbols.Paste, UIStyles.Colors.Sets.Dark),
            ("Print", UIStyles.Symbols.Print, UIStyles.Colors.Sets.Dark),
            ("Download", UIStyles.Symbols.Download, UIStyles.Colors.Sets.Blue),
            ("Upload", UIStyles.Symbols.Upload, UIStyles.Colors.Sets.Blue),
            ("Share", UIStyles.Symbols.Share, UIStyles.Colors.Sets.Blue),
            ("Filter", UIStyles.Symbols.Filter, UIStyles.Colors.Sets.Dark),
            ("Sort", UIStyles.Symbols.Sort, UIStyles.Colors.Sets.Dark),
            ("ZoomIn", UIStyles.Symbols.ZoomIn, UIStyles.Colors.Sets.Dark),
            ("ZoomOut", UIStyles.Symbols.ZoomOut, UIStyles.Colors.Sets.Dark),
            ("Fullscreen", UIStyles.Symbols.Fullscreen, UIStyles.Colors.Sets.Dark),
            ("Send", UIStyles.Symbols.Send, UIStyles.Colors.Sets.Blue),
            ("ChevronUp", UIStyles.Symbols.ChevronUp, UIStyles.Colors.Sets.Dark),
            ("ChevronDown", UIStyles.Symbols.ChevronDown, UIStyles.Colors.Sets.Dark),
            ("Shuffle", UIStyles.Symbols.Shuffle, UIStyles.Colors.Sets.Dark),
            ("Repeat", UIStyles.Symbols.Repeat, UIStyles.Colors.Sets.Dark),
            ("FolderOpen", UIStyles.Symbols.FolderOpen, UIStyles.Colors.Sets.Dark),
            ("Menu", UIStyles.Symbols.Menu, UIStyles.Colors.Sets.Dark),
            ("TriangleUp", UIStyles.Symbols.TriangleUp, UIStyles.Colors.Sets.Dark),
            ("TriangleDown", UIStyles.Symbols.TriangleDown, UIStyles.Colors.Sets.Dark),
            ("ChevronLeft", UIStyles.Symbols.ChevronLeft, UIStyles.Colors.Sets.Dark),
            ("ChevronRight", UIStyles.Symbols.ChevronRight, UIStyles.Colors.Sets.Dark),
            ("Play", UIStyles.Symbols.Play, UIStyles.Colors.Sets.Green),
            ("Pause", UIStyles.Symbols.Pause, UIStyles.Colors.Sets.Dark),
            ("Stop", UIStyles.Symbols.Stop, UIStyles.Colors.Sets.Red),
            ("Record", UIStyles.Symbols.Record, UIStyles.Colors.Sets.Red),
            ("SkipBack", UIStyles.Symbols.SkipBack, UIStyles.Colors.Sets.Dark),
            ("SkipForward", UIStyles.Symbols.SkipForward, UIStyles.Colors.Sets.Dark),
            ("Volume", UIStyles.Symbols.Volume, UIStyles.Colors.Sets.Dark),
            ("Mute", UIStyles.Symbols.Mute, UIStyles.Colors.Sets.Dark),
            ("Music", UIStyles.Symbols.Music, UIStyles.Colors.Sets.Purple),
            ("Info", UIStyles.Symbols.Info, UIStyles.Colors.Sets.Blue),
            ("Warning", UIStyles.Symbols.Warning, UIStyles.Colors.Sets.Orange),
            ("Error", UIStyles.Symbols.Error, UIStyles.Colors.Sets.Red),
            ("Help", UIStyles.Symbols.Help, UIStyles.Colors.Sets.Blue),
            ("Lightning", UIStyles.Symbols.Lightning, UIStyles.Colors.Sets.Dark),
            ("Heart", UIStyles.Symbols.Heart, UIStyles.Colors.Sets.Pink),
            ("Lock", UIStyles.Symbols.Lock, UIStyles.Colors.Sets.Dark),
            ("Unlock", UIStyles.Symbols.Unlock, UIStyles.Colors.Sets.Dark),
            ("Logout", UIStyles.Symbols.Logout, UIStyles.Colors.Sets.Dark),
            ("Power", UIStyles.Symbols.Power, UIStyles.Colors.Sets.Red),
            ("Mail", UIStyles.Symbols.Mail, UIStyles.Colors.Sets.Blue),
            ("Calendar", UIStyles.Symbols.Calendar, UIStyles.Colors.Sets.Blue),
            ("Clock", UIStyles.Symbols.Clock, UIStyles.Colors.Sets.Dark),
            ("Bell", UIStyles.Symbols.Bell, UIStyles.Colors.Sets.Orange),
            ("Link", UIStyles.Symbols.Link, UIStyles.Colors.Sets.Blue),
            ("Pin", UIStyles.Symbols.Pin, UIStyles.Colors.Sets.Dark),
            ("Globe", UIStyles.Symbols.Globe, UIStyles.Colors.Sets.Blue),
            ("Minimize", UIStyles.Symbols.Minimize, UIStyles.Colors.Sets.Dark),
            ("Maximize", UIStyles.Symbols.Maximize, UIStyles.Colors.Sets.Dark),
            ("Restore", UIStyles.Symbols.Restore, UIStyles.Colors.Sets.Dark),
            ("Eye", UIStyles.Symbols.Eye, UIStyles.Colors.Sets.Dark),
        };

        // The table covers every preset - a new preset has to be added here with its meaning.
        Assert.Equal(expected.Length, Ready().Length);

        foreach ((string name, string symbol, UIColorSet colors) in expected)
        {
            UIButtonPreset preset = Get(name);

            Assert.Equal(symbol, preset.Symbol);

            // The symbol is white in every state; the fills are those of the set named
            // (a fill too bright for white is darkened, see EverySymbol_IsReadable...).
            Assert.Equal(colors.Disabled, preset.Colors!.Disabled);
            Assert.Equal(
                (UIColors.White, UIColors.White, UIColors.White),
                (preset.Colors.BasicText, preset.Colors.HoverText, preset.Colors.PressedText));
        }
    }

    [Fact]
    public void EverySymbol_IsReadable_OnEveryFillOfItsPreset()
    {
        // A symbol is a graphic: 3:1 is the bar. The yellow keeps its bright
        // fills on purpose (white on yellow is the look of the folder button) and is
        // held to what its resting look has always had.
        List<string> unreadable = new List<string>();

        foreach ((string name, UIButtonPreset preset) in Ready())
        {
            UIColorSet set = preset.Colors!;
            bool isYellow = set.Basic == UIStyles.Colors.Sets.Yellow.Basic;
            double minimum = isYellow ? 2.6 : ColorContrastHelper.MinimumRatioLargeTextOrUiComponents;

            (string State, Color Fill, Color Text)[] states =
            {
                ("rest", set.Basic, set.BasicText),
                ("hover", set.Hover, set.HoverText),
                ("pressed", set.Pressed, set.PressedText),
                ("disabled", set.Disabled, set.DisabledText)
            };

            foreach ((string state, Color fill, Color text) in states)
            {
                if (isYellow && state != "rest" && state != "disabled")
                    continue;

                double ratio = ColorContrastHelper.GetContrastRatio(text, fill);

                if (ratio < minimum)
                    unreadable.Add(name + " " + state + " " + ratio.ToString("0.0"));
            }
        }

        Assert.True(unreadable.Count == 0, "too little contrast: " + string.Join(", ", unreadable));
    }

    [Fact]
    public void EveryPreset_HasItsOwnSymbol()
    {
        List<string> symbols = Ready().Select(entry => entry.Preset.Symbol).ToList();

        Assert.Equal(symbols.Count, symbols.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void AButtonFromAPreset_IsTheSymbolButtonMadeByHand(string name)
    {
        UIButtonPreset preset = Get(name);

        using Button fromPreset = UIStyles.Buttons.CreateSymbol(preset, "Tip", new Size(40, 36));
        using Button byHand = UIStyles.Buttons.CreateSymbol(preset.Symbol, "Tip", new Size(40, 36), preset.Colors);

        Assert.Equal("SymbolButton", fromPreset.GetType().Name);
        Assert.Equal(byHand.Text, fromPreset.Text);
        Assert.Equal(byHand.Size, fromPreset.Size);
        Assert.Equal(byHand.BackColor, fromPreset.BackColor);
        Assert.Equal(byHand.ForeColor, fromPreset.ForeColor);
        Assert.Equal(byHand.Font.Size, fromPreset.Font.Size);
        Assert.Equal(byHand.FlatAppearance.MouseOverBackColor, fromPreset.FlatAppearance.MouseOverBackColor);
        Assert.Equal(byHand.FlatAppearance.MouseDownBackColor, fromPreset.FlatAppearance.MouseDownBackColor);
        Assert.Equal("Tip", ButtonToolTipReader.Text(fromPreset));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void AButtonFromAPreset_LooksTheSame_InBothThemes_AndWithAnyAccent(string name)
    {
        UIButtonPreset preset = Get(name);

        (Color, Color, Color, Color, Color, Color, Color, Color) Look()
        {
            using Button button = UIStyles.Buttons.CreateSymbol(preset);
            Color back = button.BackColor;
            Color fore = button.ForeColor;
            Color over = button.FlatAppearance.MouseOverBackColor;
            Color down = button.FlatAppearance.MouseDownBackColor;
            Color border = button.FlatAppearance.BorderColor;
            button.InvokePrivate("OnMouseDown", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
            Color pressed = button.ForeColor;
            button.InvokePrivate("OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));
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
    public void TheCallInTheDocs_WithSymbolAndColors_IsThePresetOfTheSameName()
    {
        // The example of CreateSymbol: symbol, tooltip, size and colors by hand ...
        using Button byHand = UIStyles.Buttons.CreateSymbol(
            UIStyles.Symbols.Folder, "Ordner auswählen", new Size(50, 30), UIStyles.Colors.Sets.Yellow);

        // ... is what the Browse preset makes.
        using Button preset = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Browse, "Ordner auswählen", new Size(50, 30));

        Assert.Equal(new Size(50, 30), byHand.Size);
        Assert.Equal("Ordner auswählen", ButtonToolTipReader.Text(byHand));
        Assert.Equal(byHand.Text, preset.Text);
        Assert.Equal(byHand.Size, preset.Size);
        Assert.Equal(byHand.BackColor, preset.BackColor);
        Assert.Equal(byHand.ForeColor, preset.ForeColor);
        Assert.Equal(byHand.FlatAppearance.MouseOverBackColor, preset.FlatAppearance.MouseOverBackColor);
        Assert.Equal(byHand.FlatAppearance.MouseDownBackColor, preset.FlatAppearance.MouseDownBackColor);
        Assert.Equal(ButtonToolTipReader.Text(byHand), ButtonToolTipReader.Text(preset));
    }

    [Fact]
    public void ThePresetsSymbolAndColors_CannotBeOverriddenByTheCaller_ButAnotherPresetCan()
    {
        // The preset decides; whoever wants other colors takes another preset.
        using Button green = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Add);
        using Button teal = UIStyles.Buttons.CreateSymbol(UIStyles.Buttons.Presets.Add.WithColors(UIStyles.Colors.Sets.Teal));

        Assert.Equal(UIStyles.Colors.Sets.Green.Basic, green.BackColor);
        Assert.Equal(UIStyles.Colors.Sets.Teal.Basic, teal.BackColor);
        Assert.Equal(green.Text, teal.Text);
    }

    // ---- the preset object ----

    [Fact]
    public void YourOwnPreset_IsMadeFromASymbolAndColors_OrASymbolAlone()
    {
        UIColorSet orange = UIStyles.Colors.Sets.Orange;
        UIButtonPreset star = new UIButtonPreset(UIStyles.Symbols.Heart, orange);
        UIButtonPreset plain = new UIButtonPreset(UIStyles.Symbols.Menu);

        Assert.Equal(UIStyles.Symbols.Heart, star.Symbol);
        Assert.Same(orange, star.Colors);
        Assert.Null(plain.Colors);

        using Button colored = UIStyles.Buttons.CreateSymbol(star);
        using Button neutral = UIStyles.Buttons.CreateSymbol(plain);
        using Button standard = UIStyles.Buttons.CreateStandard(UIStyles.Symbols.Menu, isIcon: true);

        Assert.Equal(orange.Basic, colored.BackColor);
        Assert.Equal(standard.BackColor, neutral.BackColor);
    }

    [Fact]
    public void TheWithCopies_ChangeOneThing_AndLeaveTheOriginalAlone()
    {
        UIButtonPreset add = UIStyles.Buttons.Presets.Add;

        UIButtonPreset teal = add.WithColors(UIStyles.Colors.Sets.Teal);
        UIButtonPreset plus = add.WithSymbol(UIStyles.Symbols.Heart);
        UIButtonPreset neutral = add.WithColors(null);

        Assert.Equal(add.Symbol, teal.Symbol);
        Assert.Same(UIStyles.Colors.Sets.Teal, teal.Colors);
        Assert.Equal(UIStyles.Symbols.Heart, plus.Symbol);
        Assert.Same(add.Colors, plus.Colors);
        Assert.Null(neutral.Colors);

        Assert.Equal(UIStyles.Symbols.Add, add.Symbol);
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic, add.Colors!.Basic);
    }

    [Fact]
    public void ANullSymbol_BecomesEmpty_AndANullPreset_IsRefused()
    {
        Assert.Equal("", new UIButtonPreset(null!).Symbol);
        Assert.Throws<ArgumentNullException>(() => UIStyles.Buttons.CreateSymbol((UIButtonPreset)null!));
    }
}
