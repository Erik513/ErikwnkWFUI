using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.LabelsTests;

/// <summary>
/// What the standard label does and the library label keeps: the text color from ForeColor, an
/// ampersand shown as it is (or as a mnemonic), no drop target, the border, the alignment, an
/// image. An enabled label is painted by the standard label, a disabled one by the library (in
/// its own gray) - both are looked at. [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class LabelBehaviorTests
{
    private static Label[] AllKinds() => new[]
    {
        UIStyles.Labels.CreateStandard("x"),
        UIStyles.Labels.CreatePrimary("x"),
        UIStyles.Labels.CreateTitle("x"),
        UIStyles.Labels.CreateMuted("x")
    };

    private static TextFormatFlags Flags(Label label) => label.InvokePrivate<TextFormatFlags>("GetTextFormatFlags");

    private static Color TextColor(Label label) => label.InvokePrivate<Color>("GetTextColor");

    // The label as it is painted on a page: in a window that is really shown (out of sight), because
    // a transparent label paints what is behind it, and that needs the window.
    private static Bitmap Paint(Label label, Color page)
    {
        using Form host = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            ShowInTaskbar = false,
            BackColor = page,
            ClientSize = new Size(label.Width + 10, label.Height + 10)
        };
        label.Location = new Point(5, 5);
        host.Controls.Add(label);
        host.Show();

        using Bitmap whole = new Bitmap(host.Width, host.Height);
        host.DrawToBitmap(whole, new Rectangle(0, 0, host.Width, host.Height));
        Bitmap bitmap = whole.Clone(new Rectangle(5, 5, label.Width, label.Height), whole.PixelFormat);

        // The label stays usable for the next look; only the window goes.
        host.Controls.Remove(label);

        return bitmap;
    }

    // How far apart two pictures are: the sum of the differences of all colors of all pixels.
    private static long Distance(Bitmap a, Bitmap b)
    {
        long sum = 0;

        for (int y = 0; y < a.Height; y++)
        {
            for (int x = 0; x < a.Width; x++)
            {
                Color first = a.GetPixel(x, y);
                Color second = b.GetPixel(x, y);
                sum += Math.Abs(first.R - second.R) + Math.Abs(first.G - second.G) + Math.Abs(first.B - second.B);
            }
        }

        return sum;
    }

    private static bool SamePixels(Bitmap a, Bitmap b)
    {
        if (a.Size != b.Size)
            return false;

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

    // ---- the text color ----

    [Fact]
    public void AForeColorThatWasSet_IsTheColorOfTheText()
    {
        foreach (Label label in AllKinds())
        {
            using (label)
            {
                label.ForeColor = Color.Red;

                Assert.Equal(Color.Red, label.ForeColor);
                Assert.Equal(Color.Red, TextColor(label));
            }
        }
    }

    [Fact]
    public void ForeColor_ReadWithoutOneSet_SaysTheColorTheEnabledTextHas()
    {
        using Label standard = UIStyles.Labels.CreateStandard("x");
        using Label title = UIStyles.Labels.CreateTitle("x");
        using Label muted = UIStyles.Labels.CreateMuted("x");

        Assert.Equal(UIColors.TextSecondary, standard.ForeColor);
        Assert.Equal(UIColors.TextPrimary, title.ForeColor);
        Assert.Equal(UIColors.TextMuted, muted.ForeColor);
    }

    [Fact]
    public void SettingForeColorBackToEmpty_GivesTheOwnColorAgain()
    {
        using Label label = UIStyles.Labels.CreateStandard("x");
        label.ForeColor = Color.Red;

        label.ForeColor = Color.Empty;

        Assert.Equal(UIColors.TextSecondary, TextColor(label));
    }

    [Fact]
    public void AForeColorThatWasSet_WinsOverTheColorSet()
    {
        using Label label = UIStyles.Labels.CreateStandard("x", UIStyles.Colors.Sets.Green);
        label.ForeColor = Color.Orange;

        Assert.Equal(Color.Orange, TextColor(label));
    }

    [Fact]
    public void ADisabledLabel_IsTheLibrarysGray_WhateverTheForeColor_AndKeepsItsForeColor()
    {
        using Label label = UIStyles.Labels.CreateStandard("x");
        label.ForeColor = Color.Red;

        label.Enabled = false;

        Assert.Equal(UIColors.DisabledGray, TextColor(label));
        Assert.Equal(Color.Red, label.ForeColor);

        label.Enabled = true;

        Assert.Equal(Color.Red, TextColor(label));
    }

    [Fact]
    public void ChangingTheForeColor_RaisesTheEvent_AndRepaints()
    {
        using Label label = UIStyles.Labels.CreateStandard("Some text");
        label.Size = new Size(200, 24);
        int raised = 0;
        label.ForeColorChanged += delegate { raised++; };
        using Bitmap before = Paint(label, Color.Black);

        label.ForeColor = Color.Red;
        using Bitmap after = Paint(label, Color.Black);

        Assert.Equal(1, raised);
        Assert.False(SamePixels(before, after), "the text did not change its color on the screen");
    }

    // ---- the ampersand ----

    [Fact]
    public void WithoutMnemonics_TheAmpersandIsShownAsItIs()
    {
        foreach (Label label in AllKinds())
        {
            using (label)
            {
                Assert.False(label.UseMnemonic);
                Assert.True(Flags(label).HasFlag(TextFormatFlags.NoPrefix));
            }
        }
    }

    [Fact]
    public void TheAmpersand_IsDrawnAsACharacter_NotAsTheMarkOfAKey()
    {
        using Label label = UIStyles.Labels.CreateStandard("Fish & Chips");
        label.Size = new Size(200, 24);
        using Bitmap painted = Paint(label, Color.FromArgb(40, 40, 40));

        // The same text drawn by the book with the ampersand left alone, and drawn the way
        // a label with mnemonics would (the ampersand taken out, the next sign underlined).
        Bitmap Drawn(TextFormatFlags prefix)
        {
            Bitmap bitmap = new Bitmap(label.Width, label.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(40, 40, 40));
                TextRenderer.DrawText(g, "Fish & Chips", label.Font, new Rectangle(0, 0, label.Width, label.Height), label.ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | prefix);
            }

            return bitmap;
        }

        using Bitmap literal = Drawn(TextFormatFlags.NoPrefix);
        using Bitmap mnemonic = Drawn(TextFormatFlags.Default);

        Assert.True(Distance(painted, literal) < Distance(painted, mnemonic) / 4, "the label did not draw the ampersand as it is: " + Distance(painted, literal) + " vs " + Distance(painted, mnemonic));
    }

    [Fact]
    public void WithMnemonics_TheAmpersandMarksAKey_UnderlinedOnlyWhileTheCuesAreShown()
    {
        using Label label = UIStyles.Labels.CreateStandard("&Name");
        label.UseMnemonic = true;
        _ = label.Handle;

        Assert.False(Flags(label).HasFlag(TextFormatFlags.NoPrefix));
        Assert.True(Flags(label).HasFlag(TextFormatFlags.HidePrefix));
    }

    // ---- what is painted ----

    [Fact]
    public void AnEnabledLabel_IsPaintedByTheStandardLabel_InTheColorItIsToldToHave()
    {
        using Label label = UIStyles.Labels.CreateStandard("Painted text");
        label.Size = new Size(200, 24);
        label.ForeColor = Color.Lime;
        using Bitmap painted = Paint(label, Color.Black);

        // Some pixel of the text has the color: green is the strongest channel there.
        bool found = false;

        for (int y = 0; y < painted.Height && !found; y++)
        {
            for (int x = 0; x < painted.Width && !found; x++)
            {
                Color pixel = painted.GetPixel(x, y);
                found = pixel.G > 150 && pixel.R < 80 && pixel.B < 80;
            }
        }

        Assert.True(found, "no text pixel in the ForeColor");
    }

    [Fact]
    public void ALabelWithASet_IsPaintedInTheColorOfTheSet()
    {
        UIColorSet set = UIStyles.Colors.CreateSet(Color.Lime);
        using Label label = UIStyles.Labels.CreateStandard("Painted text", set);
        label.Size = new Size(200, 24);
        using Bitmap painted = Paint(label, Color.Black);

        bool found = false;

        for (int y = 0; y < painted.Height && !found; y++)
        {
            for (int x = 0; x < painted.Width && !found; x++)
            {
                Color pixel = painted.GetPixel(x, y);
                found = pixel.G > 150 && pixel.R < 100 && pixel.B < 100;
            }
        }

        Assert.True(found, "no text pixel in the color of the set");
    }

    [Fact]
    public void ADisabledLabel_IsPaintedInTheGrayOfTheLibrary_WhateverItsForeColor()
    {
        using Label plain = UIStyles.Labels.CreateStandard("Disabled text");
        using Label red = UIStyles.Labels.CreateStandard("Disabled text");
        plain.Size = red.Size = new Size(200, 24);
        red.ForeColor = Color.Red;
        plain.Enabled = red.Enabled = false;

        using Bitmap first = Paint(plain, Color.FromArgb(40, 40, 40));
        using Bitmap second = Paint(red, Color.FromArgb(40, 40, 40));

        Assert.True(SamePixels(first, second), "the ForeColor changed a disabled label");

        // The text reaches the gray of the library (not the lighter or darker system gray).
        int strongest = 0;

        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
                strongest = Math.Max(strongest, first.GetPixel(x, y).G);
        }

        Assert.InRange(strongest, UIColors.DisabledGray.G - 25, UIColors.DisabledGray.G + 25);
    }

    [Fact]
    public void ADisabledLabel_ShowsTheAmpersandAsItIs_AsAnEnabledOneDoes()
    {
        using Label label = UIStyles.Labels.CreateStandard("Fish & Chips");
        label.Size = new Size(200, 24);
        label.Enabled = false;
        using Bitmap painted = Paint(label, Color.FromArgb(40, 40, 40));

        Bitmap Drawn(TextFormatFlags prefix)
        {
            Bitmap bitmap = new Bitmap(label.Width, label.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.FromArgb(40, 40, 40));
                TextRenderer.DrawText(g, "Fish & Chips", label.Font, new Rectangle(0, 0, label.Width, label.Height), UIColors.DisabledGray,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | prefix);
            }

            return bitmap;
        }

        using Bitmap literal = Drawn(TextFormatFlags.NoPrefix);
        using Bitmap mnemonic = Drawn(TextFormatFlags.Default);

        Assert.True(Distance(painted, literal) < Distance(painted, mnemonic) / 2, "the disabled label did not draw the ampersand as it is: " + Distance(painted, literal) + " vs " + Distance(painted, mnemonic));
    }

    [Fact]
    public void ADisabledLabel_LaysItsTextOutLikeAnEnabledOne()
    {
        // The same text in the same place: the disabled one differs only in its color.
        foreach (ContentAlignment alignment in new[] { ContentAlignment.TopLeft, ContentAlignment.MiddleCenter, ContentAlignment.BottomRight })
        {
            using Label enabled = UIStyles.Labels.CreateStandard("Where is the text");
            using Label disabled = UIStyles.Labels.CreateStandard("Where is the text");
            enabled.Size = disabled.Size = new Size(200, 60);
            enabled.TextAlign = disabled.TextAlign = alignment;
            disabled.Enabled = false;

            using Bitmap first = Paint(enabled, Color.Black);
            using Bitmap second = Paint(disabled, Color.Black);

            Assert.Equal(InkBounds(first), InkBounds(second));
        }
    }

    [Fact]
    public void ADisabledLabel_WithABorder_LaysItsTextOutInsideTheBorder_LikeAnEnabledOne()
    {
        foreach (ContentAlignment alignment in new[] { ContentAlignment.TopRight, ContentAlignment.BottomRight, ContentAlignment.MiddleCenter })
        {
            using Label enabled = UIStyles.Labels.CreateStandard("Where is the text");
            using Label disabled = UIStyles.Labels.CreateStandard("Where is the text");
            enabled.Size = disabled.Size = new Size(200, 60);
            enabled.TextAlign = disabled.TextAlign = alignment;
            enabled.BorderStyle = disabled.BorderStyle = BorderStyle.Fixed3D;
            disabled.Enabled = false;

            using Bitmap first = Paint(enabled, Color.Black);
            using Bitmap second = Paint(disabled, Color.Black);

            // (Inside the border itself, which both have.)
            Rectangle inside = new Rectangle(3, 3, first.Width - 6, first.Height - 6);
            using Bitmap firstInside = first.Clone(inside, first.PixelFormat);
            using Bitmap secondInside = second.Clone(inside, second.PixelFormat);

            Assert.Equal(InkBounds(firstInside), InkBounds(secondInside));
        }
    }

    // The smallest rectangle that holds every pixel that is not the page.
    private static Rectangle InkBounds(Bitmap bitmap)
    {
        int left = bitmap.Width, top = bitmap.Height, right = -1, bottom = -1;

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).R + bitmap.GetPixel(x, y).G + bitmap.GetPixel(x, y).B > 12)
                {
                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }
        }

        return Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }

    [Fact]
    public void ATitle_ShowsThePageBehindIt_NotADarkBox()
    {
        using Label title = UIStyles.Labels.CreateTitle("A title");
        title.Size = new Size(200, 30);
        Color page = Color.FromArgb(90, 120, 60);

        using Bitmap painted = Paint(title, page);

        // The corners, far from the centered text, are the page.
        Assert.Equal(page.ToArgb(), painted.GetPixel(1, 1).ToArgb());
        Assert.Equal(page.ToArgb(), painted.GetPixel(painted.Width - 2, painted.Height - 2).ToArgb());
    }

    // ---- not a drop target ----

    [Fact]
    public void ALabel_IsNoDropTarget_SoWhatIsDroppedOnTheWindowBehindItStillArrives()
    {
        foreach (Label label in AllKinds())
        {
            using (label)
                Assert.False(label.AllowDrop);
        }
    }

    // ---- what the standard label also does ----

    [Fact]
    public void TheBorder_IsKept()
    {
        using Label label = UIStyles.Labels.CreateStandard("x");
        label.Size = new Size(100, 30);
        label.BorderStyle = BorderStyle.FixedSingle;
        _ = label.Handle;

        Assert.True(label.ClientSize.Width < 100 && label.ClientSize.Height < 30, "no border: " + label.ClientSize);
    }

    [Fact]
    public void AnImage_IsDrawnByTheLabel_AtItsPlace()
    {
        using Label plain = UIStyles.Labels.CreateStandard("With an image");
        using Label withImage = UIStyles.Labels.CreateStandard("With an image");
        plain.Size = withImage.Size = new Size(200, 24);
        using Bitmap image = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(image))
            g.Clear(Color.Orange);

        withImage.Image = image;
        withImage.ImageAlign = ContentAlignment.MiddleRight;

        using Bitmap first = Paint(plain, Color.Black);
        using Bitmap second = Paint(withImage, Color.Black);

        // Orange pixels at the right edge, none without the image.
        Color corner = second.GetPixel(second.Width - 8, second.Height / 2);

        Assert.False(SamePixels(first, second), "the image was not drawn");
        Assert.True(corner.R > 200 && corner.G > 100 && corner.B < 60, "no image at the right: " + corner);
    }

    [Theory]
    [InlineData(ContentAlignment.TopLeft)]
    [InlineData(ContentAlignment.MiddleCenter)]
    [InlineData(ContentAlignment.BottomRight)]
    public void TheTextAlignment_MovesTheText(ContentAlignment alignment)
    {
        using Label left = UIStyles.Labels.CreateStandard("Aligned");
        using Label other = UIStyles.Labels.CreateStandard("Aligned");
        left.Size = other.Size = new Size(200, 60);
        left.TextAlign = ContentAlignment.TopLeft;
        other.TextAlign = alignment;

        using Bitmap first = Paint(left, Color.Black);
        using Bitmap second = Paint(other, Color.Black);

        Assert.Equal(alignment == ContentAlignment.TopLeft, SamePixels(first, second));
    }

    [Fact]
    public void ANewText_IsPaintedAtOnce()
    {
        using Label label = UIStyles.Labels.CreateStandard("First");
        label.Size = new Size(200, 24);
        using Bitmap before = Paint(label, Color.Black);

        label.Text = "Second text";
        using Bitmap after = Paint(label, Color.Black);

        Assert.False(SamePixels(before, after));
    }

    [Fact]
    public void AnAutoSizedLabel_IsAsBigAsItsText_WithAnAmpersandToo()
    {
        using Label label = UIStyles.Labels.CreateStandard("Fish & Chips");
        label.AutoSize = true;

        _ = label.Handle;

        // The ampersand counts: with it taken out the text would be several pixels narrower.
        int withoutIt = TextRenderer.MeasureText("Fish & Chips", label.Font).Width;
        int asItIs = TextRenderer.MeasureText("Fish & Chips", label.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPrefix).Width;

        Assert.True(asItIs > withoutIt + 4);
        Assert.True(label.PreferredSize.Width >= asItIs - 2, "the label is smaller than its text: " + label.PreferredSize + " vs " + asItIs);
    }
}
