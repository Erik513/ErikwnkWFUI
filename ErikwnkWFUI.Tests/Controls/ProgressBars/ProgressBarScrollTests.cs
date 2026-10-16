using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ProgressBars;

/// <summary>A text that is too long for the progress bar and moves (<c>TextScroll</c>).</summary>
[Collection(AccentColorTestCollection.Name)]
public class ProgressBarScrollTests : IDisposable
{
    private const string LongText = "Connecting to the server, checking the certificate and loading the first part of the data";

    private readonly Func<bool> _animations = ThemedLabel.AnimationsEnabled;

    public void Dispose()
    {
        ThemedLabel.AnimationsEnabled = _animations;
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

    private static Bitmap Draw(Control bar)
    {
        Bitmap picture = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(picture, new Rectangle(Point.Empty, bar.Size));

        return picture;
    }

    private static ProgressTextScroller Scroller(Control bar) => bar.GetPrivateField<ProgressTextScroller>("_textScroller")!;

    private static Pair Bars(LabelScroll scroll, string text = LongText)
    {
        ErikwnkWFUI.Controls.ProgressBar thick = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreatePrimary();
        thick.Text = text;
        thick.TextScroll = scroll;

        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Text = text;
        slim.TextScroll = scroll;

        return new Pair(thick, slim);
    }

    private sealed record Pair(ErikwnkWFUI.Controls.ProgressBar Thick, SlimProgressBar Slim);

    public static IEnumerable<object[]> BothBars()
    {
        yield return new object[] { "ProgressBar" };
        yield return new object[] { "SlimProgressBar" };
    }

    private static Control Make(string which, LabelScroll scroll, string text = LongText)
    {
        Pair pair = Bars(scroll, text);

        return which == "ProgressBar" ? pair.Thick : pair.Slim;
    }

    private static bool Same(Bitmap a, Bitmap b, Rectangle region)
    {
        for (int y = region.Top; y < region.Bottom; y++)
            for (int x = region.Left; x < region.Right; x++)
                if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb())
                    return false;

        return true;
    }

    // --- The setting ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ByDefaultTheTextDoesNotMove_AndTheSettingIsGivenBack(string which)
    {
        Pair pair = Bars(LabelScroll.None);
        Control bar = which == "ProgressBar" ? pair.Thick : pair.Slim;
        using Form host = Host(bar, 300, 26);
        using Bitmap picture = Draw(bar);

        Assert.False(Scroller(bar).IsMoving);
        Assert.Same(LabelScroll.None, which == "ProgressBar" ? pair.Thick.TextScroll : pair.Slim.TextScroll);

        LabelScroll scroll = LabelScroll.Endless();
        if (which == "ProgressBar")
            pair.Thick.TextScroll = scroll;
        else
            pair.Slim.TextScroll = scroll;

        Assert.Same(scroll, which == "ProgressBar" ? pair.Thick.TextScroll : pair.Slim.TextScroll);

        if (which == "ProgressBar")
            pair.Thick.TextScroll = null!;
        else
            pair.Slim.TextScroll = null!;

        Assert.Same(LabelScroll.None, which == "ProgressBar" ? pair.Thick.TextScroll : pair.Slim.TextScroll);
    }

    // --- When it moves ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ATextThatFits_StandsStill_WhateverIsAsked(string which)
    {
        foreach (LabelScroll scroll in new[] { LabelScroll.BackAndForth(), LabelScroll.Endless() })
        {
            Control bar = Make(which, scroll, "Short");
            using Form host = Host(bar, 300, 26);
            using Bitmap picture = Draw(bar);

            Assert.False(Scroller(bar).IsMoving);
        }
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ATextThatIsTooLong_MovesOnlyWhileItCan(string which)
    {
        Control bar = Make(which, LabelScroll.BackAndForth());
        using Form host = Host(bar, 300, 26);

        using (Bitmap picture = Draw(bar))
            Assert.True(Scroller(bar).IsMoving);

        bar.Enabled = false;
        Application.DoEvents();
        using (Bitmap picture = Draw(bar))
            Assert.False(Scroller(bar).IsMoving);

        bar.Enabled = true;
        Application.DoEvents();
        using (Bitmap picture = Draw(bar))
            Assert.True(Scroller(bar).IsMoving);

        // The text is shorter now, or gone: nothing moves.
        bar.Text = "Short";
        Application.DoEvents();
        using (Bitmap picture = Draw(bar))
            Assert.False(Scroller(bar).IsMoving);

        bar.Text = LongText;
        Application.DoEvents();
        using (Bitmap picture = Draw(bar))
            Assert.True(Scroller(bar).IsMoving);

        bar.Text = "";
        Application.DoEvents();
        using (Bitmap picture = Draw(bar))
            Assert.False(Scroller(bar).IsMoving);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void WhereWindowsSwitchesTheAnimationsOff_TheTextDoesNotMove_AndIsCut(string which)
    {
        ThemedLabel.AnimationsEnabled = () => false;
        Control bar = Make(which, LabelScroll.Endless());
        using Form host = Host(bar, 300, 26);
        using Bitmap picture = Draw(bar);

        Assert.False(Scroller(bar).IsMoving);
    }

    // --- Back and forth ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void BackAndForth_RestsAtTheStart_GoesToTheEnd_RestsThere_AndComesBack(string which)
    {
        Control bar = Make(which, LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short));
        using Form host = Host(bar, 300, 26);
        using (Bitmap picture = Draw(bar)) { }
        ProgressTextScroller scroller = Scroller(bar);

        Assert.True(scroller.IsMoving);
        Assert.Equal(0, scroller.Offset);

        // The rest at the start (1000 ms for Short).
        scroller.Advance(900);
        Assert.Equal(0, scroller.Offset);

        // The way: the text wider than the room (and a little more), at 90 px a second.
        scroller.Advance(100 + 1000);
        Assert.InRange(scroller.Offset, 1, scroller.TextWidth);

        scroller.Advance(60000);
        int maxOffset = 0;

        for (int step = 0; step < 400; step++)
        {
            scroller.Advance(50);
            maxOffset = Math.Max(maxOffset, scroller.Offset);
        }

        // At the end, the last sign is clear of the edge: the text is shifted by its width minus the room, and a little.
        int room = which == "ProgressBar" ? 300 - 2 - 2 * ((ErikwnkWFUI.Controls.ProgressBar)bar).GetTextPadding() : 300;
        Assert.InRange(maxOffset, scroller.TextWidth - room, scroller.TextWidth - room + 4);

        // And back at the start after a whole round.
        scroller.Advance(0);
        int seen = scroller.Offset;
        Assert.InRange(seen, 0, maxOffset);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void TheMovingTextIsPaintedFurtherLeft_WhenItHasMoved(string which)
    {
        Control bar = Make(which, LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short));
        using Form host = Host(bar, 300, 26);
        using Bitmap atStart = Draw(bar);
        ProgressTextScroller scroller = Scroller(bar);

        scroller.Advance(1000 + 1000);
        using Bitmap moved = Draw(bar);

        Assert.True(scroller.Offset > 0);
        Assert.False(Same(atStart, moved, new Rectangle(0, 0, 300, bar.Height)), "the picture did not change");
    }

    // --- Endless ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void Endless_GoesRoundWithoutRest_ToTheLeftOrToTheRight(string which)
    {
        Control left = Make(which, LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot, LabelScrollSpeed.Fast));
        using Form leftHost = Host(left, 300, 26);
        using (Bitmap picture = Draw(left)) { }
        ProgressTextScroller leftScroller = Scroller(left);

        Assert.True(leftScroller.Cycle > leftScroller.TextWidth, "a round is the text, a gap, the separator and a gap");
        leftScroller.Advance(1000);
        Assert.InRange(leftScroller.Offset, 80, 100);
        leftScroller.Advance(1000.0 * leftScroller.Cycle / 90.0);
        Assert.InRange(leftScroller.Offset, 70, 110);

        Control right = Make(which, LabelScroll.Endless(LabelScrollDirection.Right, LabelScrollSeparator.Bar, LabelScrollSpeed.Fast));
        using Form rightHost = Host(right, 300, 26);
        using (Bitmap picture = Draw(right)) { }
        ProgressTextScroller rightScroller = Scroller(right);

        int start = rightScroller.Offset;
        rightScroller.Advance(1000);

        // To the right the text comes from the left: the offset falls (and wraps at the start of the round).
        Assert.NotEqual(start, rightScroller.Offset);
        Assert.InRange(rightScroller.Offset, rightScroller.Cycle - 100, rightScroller.Cycle - 80);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void AnEndlessText_ShowsItsNextRoundBehindIt(string which)
    {
        Control bar = Make(which, LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot, LabelScrollSpeed.Fast), "Long text to run round the bar and to come again");
        using Form host = Host(bar, 140, 26);
        using (Bitmap picture = Draw(bar)) { }
        ProgressTextScroller scroller = Scroller(bar);
        Assert.True(scroller.IsMoving);

        // Moved so far that the end of the text, the separator and the start of the next round are all in the room.
        scroller.Advance(1000.0 * (scroller.TextWidth - 60) / 90.0);
        using Bitmap picture2 = Draw(bar);

        int inkAtTheRight = 0;

        for (int y = 0; y < picture2.Height; y++)
            for (int x = 100; x < 138; x++)
                if (picture2.GetPixel(x, y).ToArgb() != bar.BackColor.ToArgb() && picture2.GetPixel(x, y).ToArgb() != Color.Black.ToArgb())
                    inkAtTheRight++;

        Assert.True(inkAtTheRight > 0, "nothing of the next round at the right");
    }

    // --- The percentage ---

    [Fact]
    public void ThePercentage_StaysWhereItIs_WhileTheTextMoves_AlsoWhenTheyStoodTogether()
    {
        foreach ((HorizontalAlignment text, HorizontalAlignment percentage) in new[]
        {
            (HorizontalAlignment.Left, HorizontalAlignment.Right),
            (HorizontalAlignment.Right, HorizontalAlignment.Right),
            (HorizontalAlignment.Center, HorizontalAlignment.Center)
        })
        {
            ErikwnkWFUI.Controls.ProgressBar bar = (ErikwnkWFUI.Controls.ProgressBar)Make("ProgressBar", LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short));
            bar.Value = 42;
            bar.ShowPercentage = true;
            bar.TextAlign = text;
            bar.PercentageAlign = percentage;
            using Form host = Host(bar, 300, 23);
            using Bitmap atStart = Draw(bar);
            ProgressTextScroller scroller = Scroller(bar);
            Assert.True(scroller.IsMoving, text + "/" + percentage);

            scroller.Advance(2000);
            using Bitmap moved = Draw(bar);

            Assert.True(scroller.Offset > 0);

            // The last 30 pixels (before the border and the room) show the percentage: the same before and after.
            Assert.True(Same(atStart, moved, new Rectangle(262, 4, 24, 15)), "the percentage moved: " + text + "/" + percentage);
            Assert.False(Same(atStart, moved, new Rectangle(8, 4, 120, 15)), "the text did not move: " + text + "/" + percentage);
        }
    }

    [Theory]
    [InlineData(SlimProgressBarTextPosition.Above)]
    [InlineData(SlimProgressBarTextPosition.Right)]
    [InlineData(SlimProgressBarTextPosition.Left)]
    public void OnTheSlimBar_ThePercentageStaysAtTheRightEndOfTheTextRoom_WhileTheTextMoves(SlimProgressBarTextPosition position)
    {
        SlimProgressBar slim = UIStyles.SlimProgressBars.CreatePrimary();
        slim.Text = LongText;
        slim.Value = 42;
        slim.ShowPercentage = true;
        slim.TextPosition = position;
        slim.TextScroll = LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot, LabelScrollSpeed.Fast);
        using Form host = Host(slim, 300, 26);
        using Bitmap atStart = Draw(slim);
        ProgressTextScroller scroller = Scroller(slim);
        Assert.True(scroller.IsMoving, position.ToString());

        scroller.Advance(1000);
        using Bitmap moved = Draw(slim);

        // Where the percentage is: at the right end of the line (above) or of the room of the text at the side.
        Rectangle percentageRoom = position == SlimProgressBarTextPosition.Right
            ? new Rectangle(268, 0, 30, 26)
            : position == SlimProgressBarTextPosition.Left
                ? new Rectangle(0, 0, 0, 26)
                : new Rectangle(262, 0, 36, 14);

        if (position == SlimProgressBarTextPosition.Left)
        {
            // The room of the text is on the left, up to the strip: the percentage is at its right end, before the strip begins.
            int stripStart = 0;

            for (int x = 0; x < 300; x++)
            {
                int pixel = atStart.GetPixel(x, 13).ToArgb();

                if (pixel == slim.BackColor.ToArgb() || pixel == PaintedColors.Fill(slim).ToArgb())
                {
                    stripStart = x;
                    break;
                }
            }

            percentageRoom = new Rectangle(Math.Max(0, stripStart - 8 - 30), 0, 30, 26);
        }

        Assert.True(Same(atStart, moved, percentageRoom), "the percentage moved: " + position);
        Assert.False(Same(atStart, moved, new Rectangle(0, 0, 150, 26)), "the text did not move: " + position);
    }

    // --- The mouse ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void WhileTheMouseIsOnTheBar_TheTextStandsStill_IfTheScrollAsksForIt(string which)
    {
        Control bar = Make(which, LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short, pauseOnHover: true));
        using Form host = Host(bar, 300, 26);
        using (Bitmap picture = Draw(bar)) { }
        ProgressTextScroller scroller = Scroller(bar);

        scroller.SetHovered(true);
        scroller.Advance(5000);
        Assert.Equal(0, scroller.Offset);

        scroller.SetHovered(false);
        scroller.Advance(2500);
        Assert.True(scroller.Offset > 0);
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void TheMouse_DoesNotStopTheTextWhenTheScrollDoesNotAskForIt(string which)
    {
        Control bar = Make(which, LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short));
        using Form host = Host(bar, 300, 26);
        using (Bitmap picture = Draw(bar)) { }
        ProgressTextScroller scroller = Scroller(bar);

        scroller.SetHovered(true);
        scroller.Advance(2500);

        Assert.True(scroller.Offset > 0);
    }

    // --- The clock ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void TheClockMovesTheText_OnItsOwn(string which)
    {
        Control bar = Make(which, LabelScroll.BackAndForth(LabelScrollSpeed.Fast, LabelScrollPause.Short));
        using Form host = Host(bar, 300, 26);
        using (Bitmap picture = Draw(bar)) { }
        ProgressTextScroller scroller = Scroller(bar);

        DateTime end = DateTime.UtcNow.AddMilliseconds(1700);

        while (DateTime.UtcNow < end)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }

        Assert.True(scroller.Offset > 0, "the clock did not move the text");
    }

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ABarThatIsDisposedWhileTheTextMoves_LeavesNothingBehind(string which)
    {
        Control bar = Make(which, LabelScroll.Endless(LabelScrollDirection.Left, LabelScrollSeparator.Dot, LabelScrollSpeed.Fast));
        Form host = Host(bar, 300, 26);
        using (Bitmap picture = Draw(bar)) { }

        bar.Dispose();

        Assert.Null(Record.Exception(() =>
        {
            DateTime end = DateTime.UtcNow.AddMilliseconds(150);

            while (DateTime.UtcNow < end)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
        }));

        host.Dispose();
    }

    // --- Tiny bars ---

    [Theory]
    [MemberData(nameof(BothBars))]
    public void ATinyBar_WithAMovingText_DoesNotFail(string which)
    {
        foreach (Size size in new[] { new Size(1, 1), new Size(3, 3), new Size(12, 23), new Size(40, 26) })
        {
            Control bar = Make(which, LabelScroll.Endless());
            using Form host = Host(bar, size.Width, size.Height);
            bar.GetType().GetProperty("ShowPercentage")!.SetValue(bar, true);
            Application.DoEvents();

            Assert.Null(Record.Exception(() => Draw(bar).Dispose()));
            Scroller(bar).Advance(500);
            Assert.Null(Record.Exception(() => Draw(bar).Dispose()));
        }
    }
}
