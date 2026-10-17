using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;

namespace ErikwnkWFUI.Tests.Controls.Spinners;

/// <summary>The text next to the ring, a ring that hides when stopped, the speed, and the digits in a small ring.</summary>
public class SpinnerTextTests
{
    // A stopped spinner on black: what is drawn does not move between two pictures.
    private static Bitmap Picture(Spinner spinner, Size size, bool stopped = true)
    {
        spinner.IsSpinning = !stopped;
        spinner.SetBounds(0, 0, size.Width, size.Height);

        using Form host = OffscreenForm.Create(size, Color.Black);
        host.Controls.Add(spinner);
        host.Show();
        Application.DoEvents();

        return Draw(spinner);
    }

    private static int Ink(Bitmap picture, Rectangle area)
    {
        int count = 0;

        for (int x = area.Left; x < area.Right; x++)
        {
            for (int y = area.Top; y < area.Bottom; y++)
            {
                if (picture.GetPixel(x, y).ToArgb() != Color.Black.ToArgb())
                    count++;
            }
        }

        return count;
    }

    [Theory]
    [InlineData(SpinnerTextPosition.Right, 0, 0, 40, 40, 60, 0, 140, 40)]
    [InlineData(SpinnerTextPosition.Left, 160, 0, 40, 40, 0, 0, 140, 40)]
    public void TheTextStandsNextToTheRing_AndTheRingIsAsHighAsTheControl(SpinnerTextPosition position,
        int ringX, int ringY, int ringWidth, int ringHeight, int textX, int textY, int textWidth, int textHeight)
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        spinner.Text = "Loading data";
        spinner.TextPosition = position;
        using Bitmap picture = Picture(spinner, new Size(200, 40));

        Assert.True(Ink(picture, new Rectangle(ringX, ringY, ringWidth, ringHeight)) > 50, "no ring");
        Assert.True(Ink(picture, new Rectangle(textX, textY, textWidth, textHeight)) > 50, "no text");
        // The gap between the ring and the text stays empty.
        Assert.Equal(0, Ink(picture, position == SpinnerTextPosition.Right ? new Rectangle(41, 0, 3, 40) : new Rectangle(156, 0, 3, 40)));
    }

    [Theory]
    [InlineData(SpinnerTextPosition.Above)]
    [InlineData(SpinnerTextPosition.Below)]
    public void TheTextAboveOrBelow_TakesTheRoomItNeeds_AndTheRingTheRest(SpinnerTextPosition position)
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary();
        spinner.Text = "Wait";
        spinner.TextPosition = position;
        using Bitmap picture = Picture(spinner, new Size(100, 90));

        int textHeight = spinner.Font.Height;
        Rectangle textBand = position == SpinnerTextPosition.Above ? new Rectangle(0, 0, 100, textHeight) : new Rectangle(0, 90 - textHeight, 100, textHeight);
        Rectangle ringBand = position == SpinnerTextPosition.Above ? new Rectangle(0, textHeight + 8, 100, 90 - textHeight - 8) : new Rectangle(0, 0, 100, 90 - textHeight - 8);
        Rectangle gap = position == SpinnerTextPosition.Above ? new Rectangle(0, textHeight + 1, 100, 3) : new Rectangle(0, 90 - textHeight - 4, 100, 3);

        Assert.True(Ink(picture, textBand) > 30, "no text");
        Assert.True(Ink(picture, ringBand) > 100, "no ring");
        Assert.Equal(0, Ink(picture, gap));
    }

    [Fact]
    public void ABlankText_IsNoText_AndALongTextIsCutWithoutLeavingItsPlace()
    {
        using Spinner none = UIStyles.Spinners.CreatePrimary(40);
        using Bitmap plain = Picture(none, new Size(40, 40));

        using Spinner blank = UIStyles.Spinners.CreatePrimary(40);
        blank.Text = "   ";
        using Bitmap blanks = Picture(blank, new Size(40, 40));
        Assert.True(SamePixels(plain, blanks));

        using Spinner narrow = UIStyles.Spinners.CreatePrimary();
        narrow.Text = "A very long text that cannot fit into the room it has";
        using Bitmap cut = Picture(narrow, new Size(100, 40));
        Assert.True(Ink(cut, new Rectangle(50, 0, 49, 40)) > 20);
    }

    [Fact]
    public void AStoppedRing_IsHiddenOnlyOnRequest_AndTheTextStays()
    {
        using Spinner shown = UIStyles.Spinners.CreateGreen(40);
        shown.Text = "Done";
        using Bitmap standing = Picture(shown, new Size(140, 40));
        Assert.True(Ink(standing, new Rectangle(0, 0, 40, 40)) > 50);

        using Spinner hidden = UIStyles.Spinners.CreateGreen(40);
        hidden.Text = "Done";
        hidden.HideWhenStopped = true;
        using Bitmap gone = Picture(hidden, new Size(140, 40));
        Assert.Equal(0, Ink(gone, new Rectangle(0, 0, 40, 40)));
        Assert.True(Ink(gone, new Rectangle(46, 0, 94, 40)) > 20);

        // Turning, it is there although the wish to hide it when stopped is set.
        using Spinner turning = UIStyles.Spinners.CreateGreen(40);
        turning.Text = "Done";
        turning.HideWhenStopped = true;
        using Bitmap back = Picture(turning, new Size(140, 40), stopped: false);
        Assert.True(Ink(back, new Rectangle(0, 0, 40, 40)) > 50);
    }

    [Fact]
    public void TheSpeed_IsHowFarTheArcTurnsInTheTimeThatPassed_WhateverTheBeatOfTheClock()
    {
        using Spinner spinner = new Spinner();

        float Turn(SpinnerSpeed speed, double milliseconds)
        {
            spinner.Speed = speed;
            float before = spinner.GetPrivateField<float>("_angle");
            spinner.Advance(milliseconds);

            return (spinner.GetPrivateField<float>("_angle") - before + 360f) % 360f;
        }

        Assert.Equal(SpinnerSpeed.Medium, spinner.Speed);

        // A turn takes about 2, 1.2 and 0.7 seconds; the arc turns by the time, so a late or an uneven beat changes nothing.
        Assert.Equal(30f, Turn(SpinnerSpeed.Medium, 100), 0.01f);
        Assert.Equal(18f, Turn(SpinnerSpeed.Slow, 100), 0.01f);
        Assert.Equal(54f, Turn(SpinnerSpeed.Fast, 100), 0.01f);
        Assert.Equal(Turn(SpinnerSpeed.Medium, 100), Turn(SpinnerSpeed.Medium, 40) + Turn(SpinnerSpeed.Medium, 60), 0.01f);
    }

    [Fact]
    public void TheDigits_AreLeftOutInASmallRing_AndKeepTheirSizeWhileTheNumberCountsUp()
    {
        using Spinner small = UIStyles.Spinners.CreateProgressStandard(22);
        small.IsIndeterminate = false;
        small.Value = 50;
        using Bitmap withDigits = Picture(small, new Size(22, 22));

        using Spinner smallNone = UIStyles.Spinners.CreateProgressStandard(22);
        smallNone.IsIndeterminate = false;
        smallNone.Value = 50;
        smallNone.ShowPercentage = false;
        using Bitmap withoutDigits = Picture(smallNone, new Size(22, 22));
        Assert.True(SamePixels(withDigits, withoutDigits), "digits in a ring too small for them");

        int Height(int value)
        {
            using Spinner big = UIStyles.Spinners.CreateProgressStandard(60);
            big.IsIndeterminate = false;
            big.Value = value;
            using Bitmap picture = Picture(big, new Size(60, 60));

            int top = int.MaxValue, bottom = -1;
            for (int y = 16; y < 44; y++)
            {
                for (int x = 16; x < 44; x++)
                {
                    if (picture.GetPixel(x, y).ToArgb() == Color.Black.ToArgb())
                        continue;

                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }

            Assert.True(bottom >= 0, "no digits for " + value);

            return bottom - top;
        }

        Assert.Equal(Height(8), Height(100));
        Assert.Equal(Height(8), Height(5));
    }

    [Fact]
    public void TheFontOfTheDigits_IsWorkedOutOnce_AndAgainOnlyWhenTheRingChanges()
    {
        using Spinner spinner = UIStyles.Spinners.CreateProgressPrimary(60, 6);
        spinner.IsSpinning = false;
        using Form host = OffscreenForm.Create(new Size(120, 120), Color.Black);
        spinner.SetBounds(0, 0, 60, 60);
        host.Controls.Add(spinner);
        host.Show();
        Application.DoEvents();
        using (Bitmap first = Draw(spinner)) { }
        Font? font = spinner.GetPrivateField<Font>("_digitFont");
        Assert.NotNull(font);

        spinner.Value = 99;
        using (Bitmap again = Draw(spinner)) { }
        spinner.Invalidate();
        using (Bitmap third = Draw(spinner)) { }
        Assert.Same(font, spinner.GetPrivateField<Font>("_digitFont"));

        spinner.Width = 90;
        spinner.Height = 90;
        using (Bitmap bigger = Draw(spinner)) { }
        Font? other = spinner.GetPrivateField<Font>("_digitFont");
        Assert.NotNull(other);
        Assert.NotSame(font, other);
        Assert.True(other!.Size > font!.Size);
    }

    [Fact]
    public void AStallOfTheWindow_DoesNotMakeTheArcJump()
    {
        using Spinner spinner = UIStyles.Spinners.CreatePrimary(40);
        using Form host = OffscreenForm.Create(new Size(40, 40), Color.Black);
        spinner.SetBounds(0, 0, 40, 40);
        host.Controls.Add(spinner);
        host.Show();
        Application.DoEvents();

        float before = spinner.GetPrivateField<float>("_angle");
        spinner.SetPrivateField("_lastTickMs", -60120L); // the last beat was a minute ago (a number of turns plus a little: a jump would show)
        spinner.InvokePrivate("OnTick", spinner, EventArgs.Empty);
        float turned = (spinner.GetPrivateField<float>("_angle") - before + 360f) % 360f;

        Assert.True(turned > 0 && turned <= 30.5f, "the arc jumped by " + turned + " degrees");
    }
}
