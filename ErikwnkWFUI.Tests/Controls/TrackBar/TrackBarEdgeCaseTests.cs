using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Localization;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using TrackBar = ErikwnkWFUI.Controls.TrackBar;

namespace ErikwnkWFUI.Tests.Controls.TrackBars;

/// <summary>
/// The odd cases of the track bar: numbers that are not numbers, a range as wide as a double can be, every size, a screen that is scaled,
/// a handler that disposes or disables it, a popup without a window, and the keys and the mouse in unusual order.
/// [Collection]: the popup test replaces the check which window is in front, as the popup tests do.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class TrackBarEdgeCaseTests
{
    private static Form Show(TrackBar bar, int width = 120, int height = 20)
    {
        return OffscreenForm.Show(bar, new Size(Math.Max(width, 20), Math.Max(height, 20)), Color.Black, bounds: new Rectangle(0, 0, width, height));
    }

    private static MouseEventArgs Mouse(int x, MouseButtons buttons = MouseButtons.Left) => new MouseEventArgs(buttons, 1, x, 10, 0);

    private static void Key(TrackBar bar, Keys key) => bar.InvokePrivate("OnKeyDown", new KeyEventArgs(key));

    // ---- numbers that are not numbers ----

    [Fact]
    public void NotANumber_IsIgnored_AndInfinityIsTheEndOfTheRange()
    {
        using TrackBar bar = new TrackBar { Minimum = -5, Maximum = 5, Value = 2 };

        bar.Value = double.NaN;
        bar.Minimum = double.NaN;
        bar.Maximum = double.NaN;
        bar.Minimum = double.NegativeInfinity;
        bar.Maximum = double.PositiveInfinity;
        Assert.Equal(2, bar.Value);
        Assert.Equal(-5, bar.Minimum);
        Assert.Equal(5, bar.Maximum);

        bar.Value = double.PositiveInfinity;
        Assert.Equal(5, bar.Value);
        bar.Value = double.NegativeInfinity;
        Assert.Equal(-5, bar.Value);

        bar.SmallChange = double.NaN;
        bar.LargeChange = double.NaN;
        Assert.Equal(0.5, bar.SmallChange, 6);
        Assert.Equal(1.0, bar.LargeChange, 6);
    }

    [Fact]
    public void ARangeAsWideAsADoubleCanBe_CanBeDrawn_AndTheEndsAreTheEnds()
    {
        using TrackBar bar = new TrackBar { Minimum = -double.MaxValue, Maximum = double.MaxValue };
        using Form host = Show(bar);
        double? last = null;
        bar.ValueChanged += (s, e) => last = bar.Value;

        using (Bitmap picture = Draw(bar)) { }

        bar.InvokePrivate("OnMouseDown", Mouse(113));
        Assert.Equal(double.MaxValue, last);
        bar.InvokePrivate("OnMouseMove", Mouse(7));
        Assert.Equal(-double.MaxValue, last);
        bar.InvokePrivate("OnMouseUp", Mouse(7));

        Key(bar, Keys.Right);
        Assert.False(double.IsNaN(bar.Value) || double.IsInfinity(bar.Value));
        using (Bitmap picture = Draw(bar)) { }
    }

    // ---- sizes and states ----

    public static IEnumerable<object[]> Sizes()
    {
        foreach (int width in new[] { 1, 2, 14, 15, 120, 500 })
        {
            foreach (int height in new[] { 1, 6, 20, 60 })
                yield return new object[] { width, height };
        }
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void EverySize_CanBeDrawn_InEveryState(int width, int height)
    {
        foreach (bool enabled in new[] { true, false })
        {
            foreach (double value in new[] { 0.0, 0.5, 1.0 })
            {
                using TrackBar bar = UIStyles.TrackBars.CreatePrimary(value, 1.0, showValuePopup: true);
                bar.Enabled = enabled;
                using Form host = Show(bar, width, height);
                bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);

                using Bitmap picture = Draw(bar);

                Assert.Equal(new Size(width, height), picture.Size);
            }
        }
    }

    // ---- a screen that is scaled ----

    [Fact]
    public void OnAScaledScreen_TheTrackTheThumbAndTheInsetAreScaled()
    {
        int TrackThickness(TrackBar bar)
        {
            using Bitmap picture = Draw(bar);
            int count = 0;

            for (int y = 0; y < picture.Height; y++)
            {
                if (picture.GetPixel(bar.Width - 14, y).ToArgb() != Color.Black.ToArgb())
                    count++;
            }

            return count;
        }

        using TrackBar normal = new TrackBar { Value = 0.5 };
        using Form hostNormal = Show(normal, 200, 60);
        using TrackBar scaled = new TrackBar { Value = 0.5, DpiOverride = 192 };
        using Form hostScaled = Show(scaled, 200, 60);

        Assert.Equal(7, normal.TrackInsetPixels);
        Assert.Equal(13, scaled.TrackInsetPixels);
        Assert.Equal(TrackBar.TrackInset, normal.TrackInsetPixels);
        Assert.InRange(TrackThickness(normal), 3, 5);
        Assert.InRange(TrackThickness(scaled), 7, 9);
    }

    [Fact]
    public void OnAScaledScreen_TheThumbIsGrippedWithinItsScaledSize()
    {
        using TrackBar scaled = new TrackBar { Maximum = 100, Value = 50, DpiOverride = 192 };
        using Form host = Show(scaled, 200, 40);

        // The thumb stands in the middle (x = 100); 12 px beside it is still the thumb at 192 dpi (radius 12 + 4), not the track.
        scaled.InvokePrivate("OnMouseDown", Mouse(112));

        Assert.Equal(50, scaled.Value, 3);
        scaled.InvokePrivate("OnMouseUp", Mouse(112));
    }

    // ---- a handler that disposes or disables the slider ----

    [Fact]
    public void AHandlerThatDisposesTheSlider_InTheMiddleOfADrag_DoesNotFail()
    {
        TrackBar bar = new TrackBar { ShowValuePopup = true };
        Form host = Show(bar);
        int changed = 0;
        bar.ValueChanged += (s, e) =>
        {
            changed++;
            bar.Dispose();
        };

        bar.InvokePrivate("OnMouseDown", Mouse(60));
        Assert.Null(Record.Exception(() => bar.InvokePrivate("OnMouseMove", Mouse(90))));
        Assert.Null(Record.Exception(() => bar.InvokePrivate("OnMouseUp", Mouse(90))));
        Assert.Equal(1, changed);

        host.Dispose();
    }

    [Fact]
    public void AHandlerThatDisablesTheSlider_EndsTheDrag_AndTheMouseMovingOnChangesNothing()
    {
        using TrackBar bar = new TrackBar();
        using Form host = Show(bar);
        int ended = 0;
        bar.DragEnded += (s, e) => ended++;
        bar.ValueChanged += (s, e) => bar.Enabled = false;

        bar.InvokePrivate("OnMouseDown", Mouse(60));
        double value = bar.Value;
        bar.InvokePrivate("OnMouseMove", Mouse(100));

        Assert.False(bar.IsDragging);
        Assert.Equal(1, ended);
        Assert.Equal(value, bar.Value);
    }

    [Fact]
    public void ADisposedSlider_MayBeDisposedAgain_AndItsPopupGoesWithIt()
    {
        TrackBar bar = new TrackBar { ShowValuePopup = true };
        Form host = Show(bar);
        Key(bar, Keys.Right);
        InfoPopupForm? popup = bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form;
        Assert.NotNull(popup);

        bar.Dispose();
        bar.Dispose();

        Assert.True(popup!.IsDisposed);
        host.Dispose();
    }

    // ---- the mouse in unusual order ----

    [Fact]
    public void APressWhileTheMouseIsHeldAlready_DoesNotStartASecondDrag()
    {
        using TrackBar bar = new TrackBar();
        using Form host = Show(bar);
        int started = 0;
        int ended = 0;
        bar.DragStarted += (s, e) => started++;
        bar.DragEnded += (s, e) => ended++;

        bar.InvokePrivate("OnMouseDown", Mouse(30));
        bar.InvokePrivate("OnMouseDown", Mouse(90));
        bar.InvokePrivate("OnMouseUp", Mouse(90));

        Assert.Equal(1, started);
        Assert.Equal(1, ended);
    }

    [Fact]
    public void AMouseUpWithoutAPress_AndAMoveWithoutAPress_DoNothing()
    {
        using TrackBar bar = new TrackBar { Value = 0.5 };
        using Form host = Show(bar);
        int events = 0;
        bar.ValueChanged += (s, e) => events++;
        bar.DragEnded += (s, e) => events++;

        bar.InvokePrivate("OnMouseUp", Mouse(10));
        bar.InvokePrivate("OnMouseMove", Mouse(100));

        Assert.Equal(0, events);
        Assert.Equal(0.5, bar.Value);
    }

    [Fact]
    public void TheWheel_IsIgnoredWhileDragging_AndAHighResolutionWheelAddsUp()
    {
        using TrackBar bar = new TrackBar { Maximum = 100, SmallChange = 8, Value = 50 };
        using Form host = Show(bar);

        bar.InvokePrivate("OnMouseDown", Mouse(60));
        double dragging = bar.Value;
        bar.InvokePrivate("OnMouseWheel", new HandledMouseEventArgs(MouseButtons.Left, 0, 60, 10, 120));
        Assert.Equal(dragging, bar.Value);
        bar.InvokePrivate("OnMouseUp", Mouse(60));

        bar.Value = 50;
        for (int step = 0; step < 4; step++)
            bar.InvokePrivate("OnMouseWheel", new HandledMouseEventArgs(MouseButtons.None, 0, 60, 10, 30));

        Assert.Equal(58, bar.Value, 3);
    }

    [Fact]
    public void RightToLeft_IsIgnored_TheMinimumStaysOnTheLeft()
    {
        using TrackBar bar = UIStyles.TrackBars.CreatePrimary(0.3);
        bar.RightToLeft = RightToLeft.Yes;
        using Form host = Show(bar);
        using Bitmap picture = Draw(bar);

        Color fill = bar.GetFillColor();
        Assert.Equal(fill.ToArgb(), picture.GetPixel(20, 10).ToArgb());
        Assert.NotEqual(fill.ToArgb(), picture.GetPixel(100, 10).ToArgb());
    }

    // ---- the keys ----

    [Fact]
    public void OtherKeys_ChangeNothing_AndTheMovingKeysAreInputKeys()
    {
        using TrackBar bar = new TrackBar { Value = 0.5 };
        using Form host = Show(bar);

        foreach (Keys key in new[] { Keys.Enter, Keys.A, Keys.Space, Keys.Escape, Keys.Tab })
            Key(bar, key);

        Assert.Equal(0.5, bar.Value);

        foreach (Keys key in new[] { Keys.Left, Keys.Right, Keys.Up, Keys.Down, Keys.PageUp, Keys.PageDown, Keys.Home, Keys.End })
            Assert.True(bar.InvokePrivate<bool>("IsInputKey", key), key + " is not an input key");

        Assert.False(bar.InvokePrivate<bool>("IsInputKey", Keys.Tab));
        Assert.False(bar.InvokePrivate<bool>("IsInputKey", Keys.Enter));
    }

    // ---- the popup ----

    [Fact]
    public void ThePopup_NeedsAWindow_AndASliderWithoutOneDoesNotFail()
    {
        using TrackBar bar = new TrackBar { ShowValuePopup = true };

        Assert.Null(Record.Exception(() => Key(bar, Keys.Right)));
        Assert.Null(Record.Exception(() => bar.InvokePrivate("OnMouseEnter", EventArgs.Empty)));
        Assert.Null(Record.Exception(() => bar.InvokePrivate("OnMouseLeave", EventArgs.Empty)));
        Assert.Null(bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form);
    }

    [Fact]
    public void ThePopup_ShowsWhileTheMouseIsOver_AndGoesWhenItLeavesOrTheSwitchIsOff()
    {
        // A popup is only shown over the window that is in front: the test does not depend on what is in front on the machine.
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            PopupShowsWhileTheMouseIsOver();
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }

    private static void PopupShowsWhileTheMouseIsOver()
    {
        using TrackBar bar = new TrackBar { ShowValuePopup = true, Value = 0.5 };
        using Form host = Show(bar);

        bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        InfoPopupForm popup = bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form!;
        Assert.True(popup.Visible, "no popup while the mouse is over the slider");

        bar.InvokePrivate("OnMouseLeave", EventArgs.Empty);
        Assert.False(popup.Visible, "the popup stays after the mouse has left");

        bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        Assert.True(popup.Visible);
        bar.ShowValuePopup = false;
        Assert.False(popup.Visible, "the popup stays after it was switched off");

        bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        Assert.False(popup.Visible, "a switched-off slider shows its popup");
    }

    // ---- events ----

    [Fact]
    public void ValueChanged_IsOnlyForTheUser_AndOnlyForAnotherValue()
    {
        using TrackBar bar = new TrackBar { Maximum = 100 };
        using Form host = Show(bar);
        int changed = 0;
        bar.ValueChanged += (s, e) => changed++;

        bar.Value = 40;
        bar.Minimum = 10;
        bar.Maximum = 50;
        Assert.Equal(0, changed);

        Key(bar, Keys.End);
        Key(bar, Keys.End);
        Assert.Equal(1, changed);

        Key(bar, Keys.Home);
        Assert.Equal(2, changed);
    }

    [Fact]
    public void AScreenReader_GetsTheNewValue_WhenTheValueChanges()
    {
        using TrackBar bar = new TrackBar { Value = 0.1 };
        using Form host = Show(bar);
        Assert.NotNull(bar.AccessibilityObject);

        Assert.Equal(0.1.ToString("0.##", UIStrings.NumberCulture), bar.AccessibilityObject.Value);

        bar.Value = 0.7;
        Assert.Equal(0.7.ToString("0.##", UIStrings.NumberCulture), bar.AccessibilityObject.Value);
    }

    [Fact]
    public void TheThumbIsNotLeftBig_WhenTheSliderWasDisabledOrHiddenUnderTheMouse()
    {
        using TrackBar bar = new TrackBar();
        using Form host = Show(bar);

        // The mouse is over the slider (the thumb grows); then it is disabled: it gets no MouseLeave, and the mouse goes away.
        bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        Assert.True(bar.GetPrivateField<bool>("_hover"));
        bar.Enabled = false;
        bar.Enabled = true;
        Assert.False(bar.GetPrivateField<bool>("_hover"), "the thumb stays big after the slider was disabled under the mouse");

        bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        bar.Visible = false;
        bar.Visible = true;
        Assert.False(bar.GetPrivateField<bool>("_hover"), "the thumb stays big after the slider was hidden under the mouse");
    }

    [Fact]
    public void OnlyTheKeysThatMoveTheValue_ShowThePopupWhenLetGo()
    {
        using TrackBar bar = new TrackBar { ShowValuePopup = true };
        using Form host = Show(bar);

        // Shift, Ctrl, Tab (the key that brought the focus here) and the like show nothing.
        foreach (Keys key in new[] { Keys.Tab, Keys.ShiftKey, Keys.ControlKey, Keys.A, Keys.Enter })
            bar.InvokePrivate("OnKeyUp", new KeyEventArgs(key));

        Assert.Null(bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form);

        bar.InvokePrivate("OnKeyUp", new KeyEventArgs(Keys.Right));
        Assert.NotNull(bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form);
    }

    private static string ShownText(TrackBar bar)
    {
        InfoPopupForm popup = bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form!;
        InfoPopupSection[] sections = popup.GetPrivateField<InfoPopupSection[]>("_sections")!;

        return sections.Length == 0 ? "" : sections[0].Text;
    }

    [Fact]
    public void AFastSlideToAnEnd_LeavesThePopupAtTheEnd_NotAtTheLastValueThatWasShown()
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            using TrackBar bar = new TrackBar { Maximum = 100, ShowValuePopup = true };
            bar.FormatValue = value => Math.Round(value).ToString();
            using Form host = Show(bar);

            // Down on the thumb, then fast to the right end: the moves come faster than the popup is updated (every 40 ms at most),
            // and the last one of them (at the end) is held back - no move comes after it, the mouse stays where it is.
            bar.InvokePrivate("OnMouseDown", Mouse(7));
            bar.InvokePrivate("OnMouseMove", Mouse(40));
            bar.InvokePrivate("OnMouseMove", Mouse(90));
            bar.InvokePrivate("OnMouseMove", Mouse(113));

            Assert.Equal(100, bar.Value);

            TestPictures.Pump(200);

            Assert.True(bar.IsDragging);
            Assert.Equal("100", ShownText(bar));
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }

    [Fact]
    public void ThePopupThatWasHeldBack_IsNotShownForASliderThatIsDisabledOrGoneInTheMeantime()
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            TrackBar bar = new TrackBar { Maximum = 100, ShowValuePopup = true, SmallChange = 10 };
            bar.FormatValue = value => Math.Round(value).ToString();
            Form host = Show(bar);

            Key(bar, Keys.Right);
            Key(bar, Keys.Right);               // held back
            bar.Enabled = false;                // the popup goes with it
            TestPictures.Pump(200);

            InfoPopupForm popup = bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form!;
            Assert.False(popup.Visible, "the held-back update showed the popup of a disabled slider");

            bar.Enabled = true;
            Key(bar, Keys.Right);
            Key(bar, Keys.Right);               // held back again, and the slider is disposed before it is made
            bar.Dispose();

            Assert.Null(Record.Exception(() => TestPictures.Pump(200)));
            host.Dispose();
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }

    [Fact]
    public void AfterADragThatEndsOutsideTheSlider_ThePopupStaysAMomentAndThenGoes()
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            ValuePopup.HideDelayMs = 200;
            using TrackBar bar = new TrackBar { Maximum = 100, ShowValuePopup = true };
            using Form host = Show(bar);

            bar.InvokePrivate("OnMouseDown", Mouse(60));
            bar.InvokePrivate("OnMouseMove", Mouse(113));
            bar.InvokePrivate("OnMouseUp", Mouse(300));      // let go far to the right of the slider ...
            bar.InvokePrivate("OnMouseLeave", EventArgs.Empty);   // ... and the mouse leaves right after the release

            InfoPopupForm popup = bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form!;
            Assert.True(popup.Visible, "the popup is gone as soon as the mouse is let go");

            // It waits for the hide timer (1.5 s, shortened for the test) to take it away - not for the mouse that has left.
            System.Windows.Forms.Timer linger = bar.GetPrivateField<ValuePopup>("_valuePopup")!.LingerTimer!;
            Assert.True(linger.Enabled, "nothing keeps the popup for a moment");

            TestPictures.Pump(600);
            Assert.False(popup.Visible, "the popup stays for good");
        }
        finally
        {
            ValuePopup.HideDelayMs = 1500;
            PopupSupport.OwnerIsActive = before;
        }
    }

    [Fact]
    public void ThePopupOfAHover_GoesAtOnce_WhenTheMouseLeaves()
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            using TrackBar bar = new TrackBar { Maximum = 100, ShowValuePopup = true };
            using Form host = Show(bar);

            bar.InvokePrivate("OnMouseEnter", EventArgs.Empty);
            InfoPopupForm popup = bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form!;
            Assert.True(popup.Visible);

            bar.InvokePrivate("OnMouseLeave", EventArgs.Empty);
            Assert.False(popup.Visible);
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }

    [Fact]
    public void ThePopupStaysForAMomentAndAHalfAfterAnInteraction_ByDefault()
    {
        Assert.Equal(1500, ValuePopup.HideDelayMs);
    }

    [Fact]
    public void ALossOfTheFocus_HidesThePopupAtOnce_EvenRightAfterItWasShown_AndTheFocusIsNotTakenBack()
    {
        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            using TrackBar bar = new TrackBar { Maximum = 100, ShowValuePopup = true };
            using Form host = Show(bar);
            int regained = 0;
            bar.GotFocus += (s, e) => regained++;

            Key(bar, Keys.Right);                    // the popup is shown ...
            InfoPopupForm popup = bar.GetPrivateField<ValuePopup>("_valuePopup")!.Form!;
            Assert.True(popup.Visible);

            bar.InvokePrivate("OnLostFocus", EventArgs.Empty);   // ... and the user goes to another control at once

            Assert.False(popup.Visible, "the popup stays on a slider that has lost the focus");
            Assert.False(bar.GetPrivateField<ValuePopup>("_valuePopup")!.LingerTimer!.Enabled);

            // Nothing takes the focus back later (the popup never had it).
            TestPictures.Pump(150);
            Assert.Equal(0, regained);
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }
}
