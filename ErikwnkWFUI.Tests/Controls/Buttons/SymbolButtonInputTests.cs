using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Factories;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Buttons;

/// <summary>
/// The symbol button follows hover and press itself, also on odd input.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class SymbolButtonInputTests : SymbolButtonTestBase
{
    // ---- the symbol button follows hover and press itself: odd input ----

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

        // A tooltip that comes later names the symbol button too - but not the one with a caption.
        UIButtonFactory.UpdateTooltip(withoutTip, "Speichern");
        UIButtonFactory.UpdateTooltip(text, "Speichert die Datei");
        Assert.Equal("Speichern", withoutTip.AccessibleName);
        Assert.True(string.IsNullOrEmpty(text.AccessibleName));
    }

    [Fact]
    public void ATooltipThatComesLater_IsDisposedWithTheButton()
    {
        Button button = UIStyles.Buttons.CreateSymbol(UIStyles.Symbols.Save);
        UIButtonFactory.UpdateTooltip(button, "Save");
        System.Windows.Forms.ToolTip toolTip = ButtonToolTipReader.Find(button)!;
        bool disposed = false;
        toolTip.Disposed += (sender, e) => disposed = true;

        button.Dispose();

        Assert.True(disposed);
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
