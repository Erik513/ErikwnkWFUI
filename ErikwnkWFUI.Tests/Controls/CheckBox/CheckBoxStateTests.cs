using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.CheckBox;

/// <summary>
/// The look of the checkbox in each state, like a button: hovered, pressed, focused (once
/// the keyboard is used), disabled, undetermined, and the colors of the filled box. The
/// box is probed pixel by pixel in the picture the control draws of itself.
/// [Collection]: the default colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class CheckBoxStateTests
{
    private static readonly UIColorSet Green = UIStyles.Colors.Sets.Green;

    private static System.Windows.Forms.CheckBox Create(bool isChecked, UIColorSet? colors = null, string text = "Label")
    {
        return UIStyles.CheckBoxes.CreateStandard(text, isChecked, colors);
    }

    private static Bitmap Draw(Control control)
    {
        _ = control.Handle;
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    // Where the box is: left margin 2, 16 px wide, centered vertically.
    private static Point BoxTopLeft(Control control) => new Point(2, (control.Height - 16) / 2);

    // The bottom-left inside of the box: filled when checked, clear of the checkmark.
    private static Color Inside(Control control)
    {
        using Bitmap bitmap = Draw(control);
        Point box = BoxTopLeft(control);

        return bitmap.GetPixel(box.X + 3, box.Y + 13);
    }

    private static Color Middle(Control control)
    {
        using Bitmap bitmap = Draw(control);
        Point box = BoxTopLeft(control);

        return bitmap.GetPixel(box.X + 8, box.Y + 8);
    }

    private static void Raise(Control control, string handler, params object[] args) => control.InvokePrivate(handler, args);

    private static void Enter(Control control) => Raise(control, "OnMouseEnter", EventArgs.Empty);

    private static void Leave(Control control) => Raise(control, "OnMouseLeave", EventArgs.Empty);

    private static void Down(Control control, MouseButtons which = MouseButtons.Left) =>
        Raise(control, "OnMouseDown", new MouseEventArgs(which, 1, 5, 5, 0));

    private static void Up(Control control, MouseButtons which = MouseButtons.Left) =>
        Raise(control, "OnMouseUp", new MouseEventArgs(which, 1, 5, 5, 0));

    // ---- the filled box ----

    [Fact]
    public void TheCheckedBox_IsFilledWithTheSetsColorForEveryState()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        Assert.Equal(Green.Basic, Inside(box));

        Enter(box);
        Assert.Equal(Green.Hover, Inside(box));

        Down(box);
        Assert.Equal(Green.Pressed, Inside(box));

        Up(box);
        Assert.Equal(Green.Hover, Inside(box));

        Leave(box);
        Assert.Equal(Green.Basic, Inside(box));
    }

    [Fact]
    public void TheCheckmark_IsDrawnInTheSetsTextColorOfTheState()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);
        box.CheckState = CheckState.Indeterminate;
        box.ThreeState = true;

        // The dash passes through the middle of the box.
        Assert.Equal(Green.BasicText, Middle(box));

        Enter(box);
        Assert.Equal(Green.HoverText, Middle(box));

        Down(box);
        Assert.Equal(Green.PressedText, Middle(box));
    }

    [Fact]
    public void WithoutColors_TheBoxIsTheNeutralGray_ForEveryState_AndIgnoresTheAccent()
    {
        UIColorSet gray = UIStyles.Colors.Sets.Gray;

        try
        {
            UIColors.SetAccent(UIAccentColors.Purple);
            using System.Windows.Forms.CheckBox box = Create(true);

            Assert.Equal(gray.Basic, Inside(box));
            Enter(box);
            Assert.Equal(gray.Hover, Inside(box));
            Down(box);
            Assert.Equal(gray.Pressed, Inside(box));
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void CreatePrimary_IsTheStandardWithThePrimarySet_AndFollowsTheAccent()
    {
        try
        {
            UIColors.SetAccent(UIAccentColors.Purple);

            using System.Windows.Forms.CheckBox primary = UIStyles.CheckBoxes.CreatePrimary("Label", true);
            using System.Windows.Forms.CheckBox viaSet = UIStyles.CheckBoxes.CreateStandard("Label", true, UIStyles.Colors.Sets.Primary);

            UIColorSet set = UIStyles.Colors.Sets.Primary;
            Assert.Equal(set.Basic, Inside(primary));
            Assert.Equal(Inside(viaSet), Inside(primary));
            Assert.Equal(primary.Size, viaSet.Size);

            Enter(primary);
            Assert.Equal(set.Hover, Inside(primary));

            Assert.NotEqual(UIStyles.Colors.Sets.Gray.Basic, Inside(UIStyles.CheckBoxes.CreatePrimary("x", true)));
        }
        finally
        {
            UIColors.SetAccent(UIAccentColors.Blue);
        }
    }

    [Fact]
    public void AColorsSetLater_ChangesTheLook_AndNullGoesBackToTheGray()
    {
        using System.Windows.Forms.CheckBox box = Create(true);
        Color accent = Inside(box);

        ((ErikwnkWFUI.Controls.CheckBox)box).Colors = Green;
        Assert.Equal(Green.Basic, Inside(box));

        ((ErikwnkWFUI.Controls.CheckBox)box).Colors = null;
        Assert.Equal(accent, Inside(box));
    }

    [Fact]
    public void ADisabledBox_IsGray_AndIgnoresHoverAndPress()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        box.Enabled = false;
        Enter(box);
        Down(box);

        Assert.Equal(UIColors.DisabledGray, Inside(box));
    }

    // ---- the empty box ----

    [Fact]
    public void TheEmptyBox_ShowsHoverAndPress_ByItsBorderAndATint()
    {
        using System.Windows.Forms.CheckBox box = Create(false);
        Color rest = Inside(box);

        Enter(box);
        Color hovered = Inside(box);

        Down(box);
        Color pressed = Inside(box);

        Assert.NotEqual(rest, hovered);
        Assert.NotEqual(hovered, pressed);

        Up(box);
        Leave(box);
        Assert.Equal(rest, Inside(box));
    }

    // ---- the pressed state with odd input: like a button ----

    [Theory]
    [InlineData(MouseButtons.Right)]
    [InlineData(MouseButtons.Middle)]
    public void AnotherMouseButton_DoesNotShowThePressedLook(MouseButtons which)
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        Enter(box);
        Down(box, which);

        Assert.Equal(Green.Hover, Inside(box));
    }

    [Fact]
    public void TheSpaceKey_Presses_OtherKeysDoNot_AndLosingFocusEndsIt()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        Raise(box, "OnKeyDown", new KeyEventArgs(Keys.Enter));
        Assert.Equal(Green.Basic, Inside(box));

        Raise(box, "OnKeyUp", new KeyEventArgs(Keys.Space));
        Assert.Equal(Green.Basic, Inside(box));

        Raise(box, "OnKeyDown", new KeyEventArgs(Keys.Space));
        Assert.Equal(Green.Pressed, Inside(box));

        Raise(box, "OnLostFocus", EventArgs.Empty);
        Assert.Equal(Green.Basic, Inside(box));
    }

    [Fact]
    public void LeavingWhilePressed_EndsThePress_AndComingBackShowsHover()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        Enter(box);
        Down(box);
        Leave(box);
        Assert.Equal(Green.Basic, Inside(box));

        Enter(box);
        Assert.Equal(Green.Hover, Inside(box));
    }

    [Fact]
    public void PressingTwice_ThenReleasingOnce_LeavesNoPressedLookBehind()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        Enter(box);
        Down(box);
        Down(box);
        Up(box);

        Assert.Equal(Green.Hover, Inside(box));
    }

    [Fact]
    public void BeingDisabledMidPress_WithoutTheReleaseEverArriving_DoesNotStayPressed()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        Enter(box);
        Down(box);
        box.Enabled = false;
        box.Enabled = true;

        Assert.Equal(Green.Hover, Inside(box));
    }

    [Fact]
    public void ADisablingClickHandler_LeavesTheBoxNeitherStuckNorWrong()
    {
        using System.Windows.Forms.CheckBox box = Create(false, Green);
        box.Click += delegate { box.Enabled = false; };

        // WinForms raises Click before MouseUp.
        Enter(box);
        Down(box);
        Raise(box, "OnClick", EventArgs.Empty);
        Up(box);
        Leave(box);
        Assert.Equal(UIColors.DisabledGray, Inside(box));

        box.Enabled = true;
        Assert.True(box.Checked);
        Assert.Equal(Green.Basic, Inside(box));
    }

    [Fact]
    public void ThePressIsEnded_WhenTheMouseIsTakenAway_WithoutAMouseUp()
    {
        // Another window or a dialog took the mouse while pressing: no MouseUp ever comes.
        using System.Windows.Forms.CheckBox box = Create(true, Green);

        Enter(box);
        Down(box);
        Raise(box, "OnMouseCaptureChanged", EventArgs.Empty);

        Assert.Equal(Green.Hover, Inside(box));
    }

    [Fact]
    public void AHiddenBox_ComesBackWithoutAHoverOrPressLook()
    {
        using Form host = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000), ShowInTaskbar = false, BackColor = UIColors.BackgroundMedium };
        System.Windows.Forms.CheckBox box = Create(true, Green);
        host.Controls.Add(box);
        host.Show();

        Enter(box);
        Down(box);
        box.Visible = false;
        box.Visible = true;

        Assert.Equal(Green.Basic, Inside(box));
    }

    // ---- the third state ----

    [Fact]
    public void AnUndeterminedBox_IsFilledLikeACheckedOne_WithADashInsteadOfAMark()
    {
        using System.Windows.Forms.CheckBox box = Create(true, Green);
        box.ThreeState = true;

        box.CheckState = CheckState.Checked;
        Color checkedMiddle = Middle(box);

        box.CheckState = CheckState.Indeterminate;
        Assert.Equal(Green.Basic, Inside(box));
        Assert.Equal(Green.BasicText, Middle(box));

        box.CheckState = CheckState.Unchecked;
        Assert.NotEqual(Green.Basic, Inside(box));

        // The checkmark does not pass through the exact middle: the dash does.
        Assert.NotEqual(Green.BasicText, checkedMiddle);
    }
}

/// <summary>
/// The dotted focus frame: shown once the keyboard is used, on the focused box only.
/// Needs a real window with the focus, so it runs on its own (see FocusTestCollection).
/// </summary>
[Collection(FocusTestCollection.Name)]
public class CheckBoxFocusTests
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
            Thread.Sleep(40);
        }
    }

    private static Bitmap Draw(Control control)
    {
        Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(0, 0, control.Width, control.Height));

        return bitmap;
    }

    private static bool SamePixels(Bitmap first, Bitmap second)
    {
        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                    return false;
            }
        }

        return true;
    }

    [Fact]
    public void TheFocusFrame_IsDrawnOnTheFocusedBox_OnceTheKeyboardIsUsed()
    {
        StaThread.Run(() =>
        {
            using Form host = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000),
                ShowInTaskbar = false,
                ClientSize = new Size(300, 100),
                BackColor = UIColors.BackgroundMedium
            };

            System.Windows.Forms.CheckBox first = UIStyles.CheckBoxes.CreateStandard("first", true);
            System.Windows.Forms.CheckBox second = UIStyles.CheckBoxes.CreateStandard("second", true);
            first.Location = new Point(10, 10);
            second.Location = new Point(10, 50);
            host.Controls.Add(first);
            host.Controls.Add(second);
            host.Show();
            Pump();

            // Before the keyboard is used, focus is not shown (as on any control).
            first.Select();
            Pump();
            using Bitmap focusedWithoutCues = Draw(first);

            // "The keyboard is being used": the message Windows sends on a key press.
            SendMessage(host.Handle, 0x127, (IntPtr)((1 << 16) | 2), IntPtr.Zero);
            first.Invalidate();
            Pump();
            Assert.True(first.Focused, "the box did not get the focus");
            using Bitmap focusedWithCues = Draw(first);

            second.Select();
            Pump();
            Assert.False(first.Focused);
            using Bitmap notFocused = Draw(first);

            Assert.False(SamePixels(focusedWithCues, notFocused), "no focus frame on the focused box");
            Assert.True(SamePixels(focusedWithoutCues, notFocused), "a focus frame before the keyboard was used");
        });
    }
}
