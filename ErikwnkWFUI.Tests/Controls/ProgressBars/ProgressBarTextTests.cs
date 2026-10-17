using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using ProgressBar = System.Windows.Forms.ProgressBar;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>The text and the percentage of the two progress bars.</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarTextTests : IDisposable
{
    public void Dispose()
    {
        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private static Form Host(Control bar, int width, int height)
    {
        Form host = OffscreenForm.Create(new Size(width, height), Color.Black);
        bar.SetBounds(0, 0, width, height);
        host.Controls.Add(bar);
        host.Show();
        Application.DoEvents();

        return host;
    }

    // The pixels that are none of the colors of the bar: the letters.
    private static List<Point> Ink(Bitmap picture, Control bar, params Color[] known)
    {
        HashSet<int> colors = new HashSet<int> { Color.Black.ToArgb(), bar.BackColor.ToArgb(), PaintedColors.Fill(bar).ToArgb(), UIColors.BorderMedium.ToArgb() };

        foreach (Color color in known)
            colors.Add(color.ToArgb());

        List<Point> ink = new List<Point>();

        for (int y = 0; y < picture.Height; y++)
            for (int x = 0; x < picture.Width; x++)
                if (!colors.Contains(picture.GetPixel(x, y).ToArgb()))
                    ink.Add(new Point(x, y));

        return ink;
    }

    // --- The helpers of both bars ---

    [Fact]
    public void ThePercentage_IsTheValueInTheRange_Rounded_AndZeroForARangeWithoutWidth()
    {
        Assert.Equal("0%", ProgressBarPainting.GetPercentText(0, 100, 0));
        Assert.Equal("42%", ProgressBarPainting.GetPercentText(0, 100, 42));
        Assert.Equal("100%", ProgressBarPainting.GetPercentText(0, 100, 100));
        Assert.Equal("50%", ProgressBarPainting.GetPercentText(10, 30, 20));
        Assert.Equal("33%", ProgressBarPainting.GetPercentText(0, 3, 1));
        Assert.Equal("0%", ProgressBarPainting.GetPercentText(5, 5, 5));
    }

    [Fact]
    public void TheTextAndThePercentage_AreOneText_OrOnlyTheOneThatIsThere()
    {
        Assert.Equal("Upload – 42 %", ProgressBarPainting.Compose("Upload", true, "42 %"));
        Assert.Equal("Upload", ProgressBarPainting.Compose("Upload", false, "42 %"));
        Assert.Equal("42 %", ProgressBarPainting.Compose("", true, "42 %"));
        Assert.Equal("42 %", ProgressBarPainting.Compose(null, true, "42 %"));
        Assert.Equal("", ProgressBarPainting.Compose(null, false, "42 %"));
    }

    // --- The bar ---

    [Fact]
    public void TheBar_WritesNothingByDefault_ThePercentageIsOff()
    {
        using ProgressBar bar = UIStyles.ProgressBars.CreatePrimary();
        bar.Value = 50;
        using Form host = Host(bar, 300, 23);

        Assert.False(((ErikwnkWFUI.Controls.ProgressBar)bar).ShowPercentage);
        Assert.Equal("", bar.Text);

        using Bitmap picture = Draw(bar);
        Assert.Empty(Ink(picture, bar));
    }

    [Fact]
    public void TheBarText_CanBeReadOnTheFillAndOnTheTrack_InTheirOwnColors()
    {
        using ProgressBar bar = UIStyles.ProgressBars.CreatePrimary();
        bar.Value = 50;
        bar.ForeColor = Color.Yellow;   // light fill, dark track: the two texts must have different colors
        bar.Text = "Uploading the file";
        ((ErikwnkWFUI.Controls.ProgressBar)bar).TextAlign = HorizontalAlignment.Center;
        using Form host = Host(bar, 300, 23);

        using Bitmap picture = Draw(bar);
        Color onFill = UIColors.GetBestTextColor(bar.ForeColor);
        Color onTrack = UIColors.GetBestTextColor(bar.BackColor);

        Assert.NotEqual(onFill.ToArgb(), onTrack.ToArgb());

        List<Point> ink = Ink(picture, bar, onFill, onTrack);
        Assert.True(ink.Count > 0, "no letters at all");

        // The text is centered and straddles the middle, where the fill ends (50 %): on the light fill it must be dark, on the dark
        // track light (the letters are thin, so look for pixels clearly darker or lighter than their ground, not for an exact color).
        bool darkOnFill = false;
        bool lightOnTrack = false;

        for (int y = 1; y < picture.Height - 1; y++)
        {
            for (int x = 1; x < 149; x++)
                if (picture.GetPixel(x, y).GetBrightness() < bar.ForeColor.GetBrightness() - 0.3f)
                    darkOnFill = true;

            for (int x = 151; x < 299; x++)
                if (picture.GetPixel(x, y).GetBrightness() > bar.BackColor.GetBrightness() + 0.3f)
                    lightOnTrack = true;
        }

        Assert.True(darkOnFill, "no dark letters on the light fill");
        Assert.True(lightOnTrack, "no light letters on the dark track");
    }

    [Fact]
    public void ThePercentage_IsWrittenOnTheBar_AndFollowsTheValue_AndTheSwitch()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        bar.Value = 50;
        using Form host = Host(bar, 300, 23);

        using Bitmap without = Draw(bar);
        Assert.Empty(Ink(without, bar));

        bar.ShowPercentage = true;
        Application.DoEvents();
        using Bitmap at50 = Draw(bar);
        Assert.False(SamePixels(without, at50), "the percentage is not written");

        bar.Value = 100;
        Application.DoEvents();
        using Bitmap at100 = Draw(bar);
        Assert.False(SamePixels(at50, at100), "the percentage did not follow the value");

        bar.Value = 50;
        bar.ShowPercentage = false;
        Application.DoEvents();
        using Bitmap off = Draw(bar);
        Assert.True(SamePixels(without, off), "the percentage is still there");
    }

    [Fact]
    public void TheMarqueeStyle_HasNoPercentage_ButShowsTheText()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        bar.Style = ProgressBarStyle.Marquee;
        bar.MarqueeAnimationSpeed = 0;
        bar.ShowPercentage = true;
        using Form host = Host(bar, 300, 23);

        using Bitmap onlyPercentage = Draw(bar);
        bar.ShowPercentage = false;
        Application.DoEvents();
        using Bitmap nothing = Draw(bar);
        Assert.True(SamePixels(nothing, onlyPercentage), "a percentage is written although there is no value");

        bar.Text = "Looking for updates";
        Application.DoEvents();
        using Bitmap withText = Draw(bar);
        Assert.False(SamePixels(nothing, withText), "the text is not written");
    }

    [Fact]
    public void ChangingTheTextOrThePercentage_PaintsTheBarAgain_ButNotWhenItStaysAsItWas()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        using Form host = Host(bar, 200, 23);
        int invalidated = 0;
        bar.Invalidated += (sender, e) => invalidated++;

        bar.ShowPercentage = false;
        Assert.Equal(0, invalidated);

        bar.ShowPercentage = true;
        Assert.True(invalidated > 0);

        invalidated = 0;
        bar.Text = "x";
        Assert.True(invalidated > 0);
    }

    // --- The slim bar ---

    private static SlimProgressBar Slim(string text, SlimProgressBarTextPosition position, bool percentage = false)
    {
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Value = 50;
        slim.Text = text;
        slim.TextPosition = position;
        slim.ShowPercentage = percentage;

        return slim;
    }

    // The rows (or, at the sides, the columns) the strip covers: where the track or the fill color is.
    private static (int First, int Last) StripRows(Bitmap picture, Control bar, int x)
    {
        int first = -1, last = -1;

        for (int y = 0; y < picture.Height; y++)
        {
            int pixel = picture.GetPixel(x, y).ToArgb();

            if (pixel == bar.BackColor.ToArgb() || pixel == PaintedColors.Fill(bar).ToArgb())
            {
                if (first < 0)
                    first = y;

                last = y;
            }
        }

        return (first, last);
    }

    [Fact]
    public void TheSlimBar_IsOnlyTheStrip_WithoutATextAndPercentage_AsBefore()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();

        Assert.False(slim.ShowPercentage);
        Assert.Equal(SlimProgressBarTextPosition.Above, slim.TextPosition);
        Assert.Equal("", slim.Text);
        Assert.Equal(4, slim.Height);
        Assert.Equal(new Size(slim.Width, 4), slim.GetPreferredSize(Size.Empty));
    }

    [Fact]
    public void TheSlimText_IsAboveTheStrip_ByDefault_AndBelowItWhenAsked()
    {
        foreach (SlimProgressBarTextPosition position in new[] { SlimProgressBarTextPosition.Above, SlimProgressBarTextPosition.Below })
        {
            SlimProgressBar slim = Slim("Downloading", position);
            using Form host = Host(slim, 200, 30);
            using Bitmap picture = Draw(slim);

            (int first, int last) = StripRows(picture, slim, 190);
            Assert.Equal(4, last - first + 1);

            List<Point> ink = Ink(picture, slim, UIColors.GetBestTextColor(Color.Black));
            Assert.NotEmpty(ink);

            if (position == SlimProgressBarTextPosition.Above)
                Assert.All(ink, point => Assert.True(point.Y < first, "a letter at row " + point.Y + ", the strip starts at " + first));
            else
                Assert.All(ink, point => Assert.True(point.Y > last, "a letter at row " + point.Y + ", the strip ends at " + last));
        }
    }

    [Fact]
    public void TheSlimText_IsLeftOrRightOfTheStrip_AndTheStripKeepsItsThickness()
    {
        foreach (SlimProgressBarTextPosition position in new[] { SlimProgressBarTextPosition.Left, SlimProgressBarTextPosition.Right })
        {
            SlimProgressBar slim = Slim("Downloading", position);
            using Form host = Host(slim, 300, 20);
            using Bitmap picture = Draw(slim);

            // Where the strip is, from its middle row.
            int firstColumn = -1, lastColumn = -1;

            for (int x = 0; x < picture.Width; x++)
            {
                int pixel = picture.GetPixel(x, 10).ToArgb();

                if (pixel == slim.BackColor.ToArgb() || pixel == PaintedColors.Fill(slim).ToArgb())
                {
                    if (firstColumn < 0)
                        firstColumn = x;

                    lastColumn = x;
                }
            }

            (int firstRow, int lastRow) = StripRows(picture, slim, firstColumn + 2);
            Assert.Equal(4, lastRow - firstRow + 1);

            List<Point> ink = Ink(picture, slim, UIColors.GetBestTextColor(Color.Black));
            Assert.NotEmpty(ink);

            if (position == SlimProgressBarTextPosition.Left)
                Assert.All(ink, point => Assert.True(point.X < firstColumn, "a letter at column " + point.X));
            else
                Assert.All(ink, point => Assert.True(point.X > lastColumn, "a letter at column " + point.X));
        }
    }

    [Fact]
    public void AboveAndBelow_TheTextIsOnTheLeft_AndThePercentageOnTheRight()
    {
        SlimProgressBar slim = Slim("Task", SlimProgressBarTextPosition.Above, percentage: true);
        using Form host = Host(slim, 300, 30);
        using Bitmap picture = Draw(slim);

        List<Point> ink = Ink(picture, slim, UIColors.GetBestTextColor(Color.Black));
        Assert.NotEmpty(ink);
        Assert.Contains(ink, point => point.X < 40);
        Assert.Contains(ink, point => point.X > 260);
        Assert.DoesNotContain(ink, point => point.X > 100 && point.X < 200);
    }

    [Fact]
    public void AtTheSides_TheTextAndThePercentage_AreOneText_AndThePercentageIsNotInTheMarqueeStyle()
    {
        SlimProgressBar slim = Slim("", SlimProgressBarTextPosition.Right, percentage: true);
        using Form host = Host(slim, 300, 20);

        using Bitmap withPercentage = Draw(slim);
        Assert.NotEmpty(Ink(withPercentage, slim, UIColors.GetBestTextColor(Color.Black)));

        slim.Style = ProgressBarStyle.Marquee;
        slim.MarqueeAnimationSpeed = 0;
        Application.DoEvents();

        using Bitmap marquee = Draw(slim);
        Assert.Empty(Ink(marquee, slim, UIColors.GetBestTextColor(Color.Black)));
    }

    [Fact]
    public void TheSlimBar_GrowsWithItsText_WhenAutoSizeIsOn_AndStaysWhenASizeWasSet()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.AutoSize = true;
        using Form host = Host(slim, 200, 4);
        slim.AutoSize = true;

        slim.Text = "Uploading";
        Assert.True(slim.Height > 4, "height " + slim.Height);

        int withText = slim.Height;
        slim.TextPosition = SlimProgressBarTextPosition.Left;
        Assert.True(slim.Height < withText, "a text at the side needs only one line");

        slim.Text = "";
        Assert.Equal(4, slim.Height);

        slim.AutoSize = false;
        slim.Height = 40;
        slim.Text = "Uploading";
        Assert.Equal(40, slim.Height);
    }

    [Fact]
    public void ChangingTheTextOfTheSlimBar_PaintsItAgain_ButNotWhenItStaysAsItWas()
    {
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();
        using Form host = Host(slim, 200, 30);
        int invalidated = 0;
        slim.Invalidated += (sender, e) => invalidated++;

        slim.TextPosition = SlimProgressBarTextPosition.Above;
        slim.ShowPercentage = false;
        Assert.Equal(0, invalidated);

        slim.TextPosition = SlimProgressBarTextPosition.Below;
        Assert.True(invalidated > 0);

        invalidated = 0;
        slim.ShowPercentage = true;
        Assert.True(invalidated > 0);
    }

    // --- The alignment of the text and of the percentage ---

    private static ErikwnkWFUI.Controls.ProgressBar Aligned(HorizontalAlignment text, HorizontalAlignment percentage)
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        bar.Value = 42;
        bar.Text = "Task";
        bar.ShowPercentage = true;
        bar.TextAlign = text;
        bar.PercentageAlign = percentage;

        return bar;
    }

    private static List<Point> BarInk(ErikwnkWFUI.Controls.ProgressBar bar)
    {
        using Form host = Host(bar, 300, 23);
        using Bitmap picture = Draw(bar);

        return Ink(picture, bar, UIColors.GetBestTextColor(bar.ForeColor), UIColors.GetBestTextColor(bar.BackColor));
    }

    [Fact]
    public void TheBar_HasTheTextOnTheLeftAndThePercentageOnTheRight_ByDefault_AsTheSlimBar()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();

        Assert.Equal(HorizontalAlignment.Left, bar.TextAlign);
        Assert.Equal(HorizontalAlignment.Right, bar.PercentageAlign);
        Assert.Equal(bar.TextAlign, slim.TextAlign);
        Assert.Equal(bar.PercentageAlign, slim.PercentageAlign);
        Assert.False(bar.ShowPercentage);
        Assert.False(slim.ShowPercentage);
        Assert.Equal("", bar.Text);
        Assert.Equal("", slim.Text);
    }

    [Fact]
    public void TheDefaultBarWithTextAndPercentage_HasTheTextLeftAndThePercentageRight_Apart()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        bar.Value = 42;
        bar.Text = "Task";
        bar.ShowPercentage = true;

        List<Point> ink = BarInk(bar);

        Assert.Contains(ink, point => point.X < 50);
        Assert.Contains(ink, point => point.X > 250);
        Assert.DoesNotContain(ink, point => point.X > 80 && point.X < 220);
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, HorizontalAlignment.Left, 0, 100)]       // one text, on the left
    [InlineData(HorizontalAlignment.Right, HorizontalAlignment.Right, 200, 300)]   // one text, on the right
    [InlineData(HorizontalAlignment.Center, HorizontalAlignment.Center, 90, 210)]  // one text, in the middle
    public void WhenTheTextAndThePercentageStandTogether_TheyAreOneText_AtThatPlace(HorizontalAlignment text, HorizontalAlignment percentage, int from, int to)
    {
        List<Point> ink = BarInk(Aligned(text, percentage));

        Assert.NotEmpty(ink);
        Assert.All(ink, point => Assert.InRange(point.X, from, to));
    }

    [Theory]
    [InlineData(HorizontalAlignment.Left, HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Right, HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Left, HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Right, HorizontalAlignment.Left)]
    public void TheText_KeepsAwayFromTheEdgeOfTheBar(HorizontalAlignment text, HorizontalAlignment percentage)
    {
        ErikwnkWFUI.Controls.ProgressBar bar = Aligned(text, percentage);
        int padding = bar.GetTextPadding();
        List<Point> ink = BarInk(bar);

        // 1 px border, then the room, then the letters (a little slack for the side bearing of a letter).
        Assert.NotEmpty(ink);
        Assert.All(ink, point => Assert.InRange(point.X, 1 + padding - 1, 298 - padding + 1));
    }

    [Fact]
    public void WhenTheyStandApart_TheTextIsLeftAndThePercentageRight_OrTheOtherWayRound()
    {
        List<Point> leftRight = BarInk(Aligned(HorizontalAlignment.Left, HorizontalAlignment.Right));
        Assert.Contains(leftRight, point => point.X < 50);
        Assert.Contains(leftRight, point => point.X > 250);
        Assert.DoesNotContain(leftRight, point => point.X > 80 && point.X < 220);

        List<Point> rightLeft = BarInk(Aligned(HorizontalAlignment.Right, HorizontalAlignment.Left));
        Assert.Contains(rightLeft, point => point.X < 50);
        Assert.Contains(rightLeft, point => point.X > 250);
        Assert.DoesNotContain(rightLeft, point => point.X > 80 && point.X < 220);
    }

    [Fact]
    public void ATextInTheMiddle_AndThePercentageOnTheRight_StandApart()
    {
        List<Point> ink = BarInk(Aligned(HorizontalAlignment.Center, HorizontalAlignment.Right));

        Assert.Contains(ink, point => point.X > 100 && point.X < 200);
        Assert.Contains(ink, point => point.X > 260);
        Assert.DoesNotContain(ink, point => point.X < 60);
    }

    [Fact]
    public void ThePercentageInTheMiddle_HasTheTextOnTheSideOfItsOwnAlignment()
    {
        List<Point> textLeft = BarInk(Aligned(HorizontalAlignment.Left, HorizontalAlignment.Center));
        Assert.Contains(textLeft, point => point.X < 50);
        Assert.Contains(textLeft, point => point.X > 130 && point.X < 170);
        Assert.DoesNotContain(textLeft, point => point.X > 190);

        List<Point> textRight = BarInk(Aligned(HorizontalAlignment.Right, HorizontalAlignment.Center));
        Assert.Contains(textRight, point => point.X > 250);
        Assert.Contains(textRight, point => point.X > 130 && point.X < 170);
        Assert.DoesNotContain(textRight, point => point.X < 110);
    }

    [Fact]
    public void ThePercentage_KeepsItsRoom_ThatTheLongTextDoesNotRunOverIt()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = Aligned(HorizontalAlignment.Left, HorizontalAlignment.Right);
        bar.Text = "A text that is much too long to fit next to the percentage on such a short bar";
        bar.Value = 100;

        List<Point> ink = BarInk(bar);
        int percentWidth = ProgressBarPainting.MeasureText(ProgressBarPainting.GetPercentText(0, 100, 100), bar.Font).Width;

        // The percentage stands in the last pixels (inside the border); the text ends before it (cut with an ellipsis), with a gap.
        Assert.Contains(ink, point => point.X >= 299 - bar.GetTextPadding() - percentWidth);
        Assert.DoesNotContain(ink, point => point.X > 299 - bar.GetTextPadding() - percentWidth - 6 && point.X < 299 - bar.GetTextPadding() - percentWidth);
    }

    [Fact]
    public void ChangingTheAlignment_PaintsTheBarAgain_ButNotWhenItStaysAsItWas()
    {
        ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard();
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();
        using Form host = Host(bar, 200, 23);
        slim.SetBounds(0, 0, 200, 30);
        host.Controls.Add(slim);
        int invalidated = 0;
        bar.Invalidated += (sender, e) => invalidated++;
        slim.Invalidated += (sender, e) => invalidated++;

        bar.TextAlign = HorizontalAlignment.Left;
        bar.PercentageAlign = HorizontalAlignment.Right;
        slim.TextAlign = HorizontalAlignment.Left;
        slim.PercentageAlign = HorizontalAlignment.Right;
        Assert.Equal(0, invalidated);

        bar.TextAlign = HorizontalAlignment.Center;
        bar.PercentageAlign = HorizontalAlignment.Center;
        slim.TextAlign = HorizontalAlignment.Right;
        slim.PercentageAlign = HorizontalAlignment.Left;
        Assert.True(invalidated >= 4, "repaints: " + invalidated);
    }

    [Fact]
    public void TheSlimBar_HasTheTextOnTheLeftAndThePercentageOnTheRight_ByDefault_AndTheSameAlignmentsAsTheBar()
    {
        using SlimProgressBar slim = UIStyles.SlimProgressBars.CreateStandard();

        Assert.Equal(HorizontalAlignment.Left, slim.TextAlign);
        Assert.Equal(HorizontalAlignment.Right, slim.PercentageAlign);

        foreach ((HorizontalAlignment alignment, int from, int to) in new[]
        {
            (HorizontalAlignment.Left, 0, 100),
            (HorizontalAlignment.Right, 200, 300),
            (HorizontalAlignment.Center, 90, 210)
        })
        {
            SlimProgressBar together = Slim("Task", SlimProgressBarTextPosition.Above, percentage: true);
            together.TextAlign = alignment;
            together.PercentageAlign = alignment;
            using Form host = Host(together, 300, 30);
            using Bitmap picture = Draw(together);

            List<Point> ink = Ink(picture, together, UIColors.GetBestTextColor(Color.Black));
            Assert.NotEmpty(ink);
            Assert.All(ink, point => Assert.InRange(point.X, from, to));
        }
    }

    [Fact]
    public void AtTheSidesOfTheStrip_TheAlignmentsDoNothing()
    {
        SlimProgressBar slim = Slim("Task", SlimProgressBarTextPosition.Right, percentage: true);
        using Form host = Host(slim, 300, 20);
        using Bitmap before = Draw(slim);

        slim.TextAlign = HorizontalAlignment.Right;
        slim.PercentageAlign = HorizontalAlignment.Left;
        Application.DoEvents();
        using Bitmap after = Draw(slim);

        Assert.True(SamePixels(before, after));
    }

    // --- A text that is longer than the control ---

    private const string LongText = "A text that is much too long to fit on such a short bar, whatever its alignment or position";

    [Theory]
    [InlineData(HorizontalAlignment.Left, HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center, HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right, HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Left, HorizontalAlignment.Right)]
    public void ALongText_IsCutOnTheBar_AndStaysInsideTheRoomToTheBorder(HorizontalAlignment text, HorizontalAlignment percentage)
    {
        ErikwnkWFUI.Controls.ProgressBar bar = Aligned(text, percentage);
        bar.Text = LongText;
        int padding = bar.GetTextPadding();

        List<Point> ink = BarInk(bar);

        Assert.NotEmpty(ink);
        Assert.All(ink, point => Assert.InRange(point.X, 1 + padding - 1, 298 - padding + 1));
    }

    [Theory]
    [InlineData(SlimProgressBarTextPosition.Above)]
    [InlineData(SlimProgressBarTextPosition.Below)]
    [InlineData(SlimProgressBarTextPosition.Left)]
    [InlineData(SlimProgressBarTextPosition.Right)]
    public void ALongSlimText_IsCut_TheStripKeepsItsRoom_AndNothingIsDrawnOutsideTheControl(SlimProgressBarTextPosition position)
    {
        SlimProgressBar slim = Slim(LongText, position, percentage: true);
        using Form host = Host(slim, 200, 26);
        using Bitmap picture = Draw(slim);

        // The strip is still there, at least 40 px long (the middle row of the strip is track or fill all along).
        int stripLength = 0;

        for (int y = 0; y < picture.Height; y++)
        {
            int run = 0;

            for (int x = 0; x < picture.Width; x++)
            {
                int pixel = picture.GetPixel(x, y).ToArgb();
                run = pixel == slim.BackColor.ToArgb() || pixel == PaintedColors.Fill(slim).ToArgb() ? run + 1 : 0;
                stripLength = Math.Max(stripLength, run);
            }
        }

        Assert.True(stripLength >= 40, "the longest piece of the strip is " + stripLength);
        Assert.NotEmpty(Ink(picture, slim, UIColors.GetBestTextColor(Color.Black)));
    }

    [Theory]
    [InlineData(SlimProgressBarTextPosition.Above)]
    [InlineData(SlimProgressBarTextPosition.Left)]
    [InlineData(SlimProgressBarTextPosition.Right)]
    public void AVeryNarrowControl_WithATextAndAPercentage_DoesNotFailAndKeepsToItsSize(SlimProgressBarTextPosition position)
    {
        SlimProgressBar slim = Slim(LongText, position, percentage: true);
        ErikwnkWFUI.Controls.ProgressBar bar = Aligned(HorizontalAlignment.Left, HorizontalAlignment.Right);
        bar.Text = LongText;
        using Form host = Host(slim, 20, 26);
        bar.SetBounds(0, 40, 12, 23);
        host.Controls.Add(bar);
        host.Size = new Size(60, 120);
        Application.DoEvents();

        Assert.Null(Record.Exception(() =>
        {
            using Bitmap picture = Draw(slim);
            using Bitmap barPicture = Draw(bar);
        }));
    }
}
