using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiNumericUpDown = ErikwnkWFUI.Controls.NumericUpDown;

namespace ErikwnkWFUI.Tests.Controls.NumericUpDown;

/// <summary>
/// The NumericUpDown: the factory (Standard gray, Primary in the accent, a color set), the two arrow fields the
/// library draws (rest, hovered, pressed, disabled) and that the behavior of the standard control is unchanged.
/// [Collection]: the primary set reads the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class NumericUpDownTests : IDisposable
{
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private readonly List<Form> _forms = new List<Form>();
    private readonly Color _accentAtStart = UIColors.Primary;

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.SetAccent(_accentAtStart);
    }

    private T Show<T>(T control) where T : Control
    {
        control.Location = new Point(10, 10);
        control.Width = 110;
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(140, 60), BackColor = UIColors.BackgroundMedium };
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    private static Control Buttons(System.Windows.Forms.NumericUpDown box) => box.Controls.Cast<Control>().Single(c => c.GetType().Name == "UpDownButtons");

    // The pixel of the control at a point of its arrow area.
    private static Color Pixel(System.Windows.Forms.NumericUpDown box, int xInButtons, int yInButtons)
    {
        using Bitmap bitmap = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(bitmap, new Rectangle(Point.Empty, box.Size));
        Control buttons = Buttons(box);

        return bitmap.GetPixel(buttons.Left + xInButtons, buttons.Top + yInButtons);
    }

    // Inside the upper / lower field, beside the arrow.
    private static Color UpperField(System.Windows.Forms.NumericUpDown box) => Pixel(box, 3, 3);

    private static Color LowerField(System.Windows.Forms.NumericUpDown box) => Pixel(box, 3, Buttons(box).Height - 4);

    // The tip of an arrow lies at the middle of its field.
    private static Color UpperArrow(System.Windows.Forms.NumericUpDown box) => Pixel(box, Buttons(box).Width / 2, Buttons(box).Height / 4);

    // ---- factory ----

    [Fact]
    public void Standard_IsTheStandardControl_WithTheGivenValues_AndNoSet()
    {
        using System.Windows.Forms.NumericUpDown box = UIStyles.NumericUpDowns.CreateStandard(5, 50, 5, 20);

        Assert.IsType<WfuiNumericUpDown>(box);
        Assert.Equal(5, box.Minimum);
        Assert.Equal(50, box.Maximum);
        Assert.Equal(5, box.Increment);
        Assert.Equal(20, box.Value);
        Assert.Null(((WfuiNumericUpDown)box).Colors);
    }

    [Fact]
    public void TheValue_IsKeptInsideTheLimits()
    {
        using System.Windows.Forms.NumericUpDown high = UIStyles.NumericUpDowns.CreateStandard(0, 10, 1, 99);
        using System.Windows.Forms.NumericUpDown low = UIStyles.NumericUpDowns.CreateStandard(5, 10, 1, 1);

        Assert.Equal(10, high.Value);
        Assert.Equal(5, low.Value);
    }

    [Fact]
    public void Primary_UsesThePrimarySet_AndFollowsTheAccent()
    {
        using System.Windows.Forms.NumericUpDown box = UIStyles.NumericUpDowns.CreatePrimary();
        Color before = ((WfuiNumericUpDown)box).Colors!.Hover;

        UIColors.SetAccent(Color.OrangeRed);

        Assert.Equal(UIColors.PrimarySet.Hover, ((WfuiNumericUpDown)box).Colors!.Hover);
        Assert.NotEqual(before, ((WfuiNumericUpDown)box).Colors!.Hover);
    }

    [Fact]
    public void ASet_IsTakenOver()
    {
        using System.Windows.Forms.NumericUpDown box = UIStyles.NumericUpDowns.CreateStandard(colors: UIStyles.Colors.Sets.Green);

        Assert.Same(UIStyles.Colors.Sets.Green, ((WfuiNumericUpDown)box).Colors);
    }

    // ---- the arrow fields ----

    [Fact]
    public void TheFields_AreInTheColorsOfTheSet_AtRest()
    {
        System.Windows.Forms.NumericUpDown gray = Show(UIStyles.NumericUpDowns.CreateStandard());
        System.Windows.Forms.NumericUpDown green = Show(UIStyles.NumericUpDowns.CreateStandard(colors: UIStyles.Colors.Sets.Green));

        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), UpperField(gray).ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), LowerField(gray).ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Green.Basic.ToArgb(), UpperField(green).ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Gray.BasicText.ToArgb(), UpperArrow(gray).ToArgb());
    }

    [Fact]
    public void OnlyTheHoveredField_TakesTheHoverColor()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());
        box.InvokePrivate("SetHotField", 0);

        Assert.Equal(UIStyles.Colors.Sets.Gray.Hover.ToArgb(), UpperField(box).ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), LowerField(box).ToArgb());

        box.InvokePrivate("SetHotField", 1);

        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), UpperField(box).ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Gray.Hover.ToArgb(), LowerField(box).ToArgb());
    }

    [Fact]
    public void TheMouse_MovesTheHotField_AndLeavingClearsIt()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());
        Control buttons = Buttons(box);
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_MOUSELEAVE = 0x02A3;

        SendMessage(buttons.Handle, WM_MOUSEMOVE, IntPtr.Zero, (IntPtr)((3 << 16) | 3));
        Assert.Equal(0, box.GetPrivateField<int>("_hotField"));

        SendMessage(buttons.Handle, WM_MOUSEMOVE, IntPtr.Zero, (IntPtr)(((buttons.Height - 3) << 16) | 3));
        Assert.Equal(1, box.GetPrivateField<int>("_hotField"));

        SendMessage(buttons.Handle, WM_MOUSELEAVE, IntPtr.Zero, IntPtr.Zero);
        Assert.Equal(-1, box.GetPrivateField<int>("_hotField"));
    }

    [Fact]
    public void ThePressedField_TakesThePressedColor()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());
        box.SetPrivateField("_hotField", 0);
        box.SetPrivateField("_pressedField", 0);

        Assert.Equal(UIStyles.Colors.Sets.Gray.Pressed.ToArgb(), UpperField(box).ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), LowerField(box).ToArgb());
    }

    [Fact]
    public void Disabled_BothFieldsAreInTheDisabledColors_AndNothingIsHot()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());
        box.InvokePrivate("SetHotField", 0);
        box.Enabled = false;

        Assert.Equal(UIStyles.Colors.Sets.Gray.Disabled.ToArgb(), UpperField(box).ToArgb());
        Assert.Equal(UIStyles.Colors.Sets.Gray.Disabled.ToArgb(), LowerField(box).ToArgb());
        Assert.Equal(-1, box.GetPrivateField<int>("_hotField"));
    }

    [Fact]
    public void TheSet_CanBeChangedWhileShown()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());

        ((WfuiNumericUpDown)box).Colors = UIStyles.Colors.Sets.Red;

        Assert.Equal(UIStyles.Colors.Sets.Red.Basic.ToArgb(), UpperField(box).ToArgb());
    }

    [Fact]
    public void TheArrowFields_AreStillDrawnByTheLibrary_AfterTheHandleWasCreatedAgain()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());

        box.InvokePrivate("RecreateHandle");
        Application.DoEvents();

        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), UpperField(box).ToArgb());
    }

    [Fact]
    public void ADpiOf192_MakesTheArrowBigger()
    {
        WfuiNumericUpDown box = (WfuiNumericUpDown)Show(UIStyles.NumericUpDowns.CreateStandard());
        Size small;
        using (Bitmap bitmap = new Bitmap(box.Width, box.Height))
        {
            box.DrawToBitmap(bitmap, new Rectangle(Point.Empty, box.Size));
            small = ArrowSize(bitmap, box);
        }

        box.DpiOverride = 192;
        using Bitmap big = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(big, new Rectangle(Point.Empty, box.Size));

        Assert.True(ArrowSize(big, box).Width > small.Width);
    }

    // The extent of the pixels of the arrow color in the upper field.
    private static Size ArrowSize(Bitmap bitmap, System.Windows.Forms.NumericUpDown box)
    {
        Control buttons = Buttons(box);
        int minX = int.MaxValue, maxX = int.MinValue;
        int arrow = UIStyles.Colors.Sets.Gray.BasicText.ToArgb();

        for (int x = 0; x < buttons.Width; x++)
            for (int y = 0; y < buttons.Height / 2; y++)
                if (bitmap.GetPixel(buttons.Left + x, buttons.Top + y).ToArgb() == arrow)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                }

        return new Size(maxX - minX + 1, 1);
    }

    // ---- what the standard control offers ----

    [Fact]
    public void ReadOnly_KeepsTheColorOfTheField_AndTheArrowsStillChangeTheValue()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard(0, 100, 5, 50));
        Color back = box.BackColor;

        box.ReadOnly = true;
        Control buttons = Buttons(box);
        Click(box, buttons.Width / 2, 3);

        Assert.Equal(back, box.BackColor);
        Assert.Equal(55, box.Value);
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), LowerField(box).ToArgb());
    }

    [Fact]
    public void TheArrowsOnTheLeft_AreDrawnThere()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());

        box.UpDownAlign = LeftRightAlignment.Left;
        Application.DoEvents();

        Assert.Equal(0, Buttons(box).Left);
        Assert.Equal(UIStyles.Colors.Sets.Gray.Basic.ToArgb(), UpperField(box).ToArgb());
    }

    [Theory]
    [InlineData(BorderStyle.None)]
    [InlineData(BorderStyle.FixedSingle)]
    [InlineData(BorderStyle.Fixed3D)]
    public void TheArrowFields_AreDrawnWithEveryBorderStyle(BorderStyle style)
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());

        box.BorderStyle = style;
        Application.DoEvents();

        // (The standard control lays its border over the edge of the buttons, so the fill is looked for, not
        // read at one point.)
        using Bitmap bitmap = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(bitmap, new Rectangle(Point.Empty, box.Size));
        Control buttons = Buttons(box);
        int fill = UIStyles.Colors.Sets.Gray.Basic.ToArgb();
        int count = 0;

        for (int x = 0; x < buttons.Width; x++)
            for (int y = 0; y < buttons.Height; y++)
                if (bitmap.GetPixel(buttons.Left + x, buttons.Top + y).ToArgb() == fill)
                    count++;

        Assert.True(count > 40, $"{count} pixels of the fill");
    }

    [Fact]
    public void ALargerFont_MakesTheFieldsAndTheArrowsBigger()
    {
        WfuiNumericUpDown box = (WfuiNumericUpDown)Show(UIStyles.NumericUpDowns.CreateStandard());
        int smallHeight = box.Height;
        Size small;

        using (Bitmap bitmap = new Bitmap(box.Width, box.Height))
        {
            box.DrawToBitmap(bitmap, new Rectangle(Point.Empty, box.Size));
            small = ArrowSize(bitmap, box);
        }

        using Font big = new Font("Segoe UI", 20);
        box.Font = big;
        Application.DoEvents();

        using Bitmap bigBitmap = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(bigBitmap, new Rectangle(Point.Empty, box.Size));

        Assert.True(box.Height > smallHeight);
        Assert.True(ArrowSize(bigBitmap, box).Width > small.Width);
    }

    [Fact]
    public void TheNumberFormats_AreTheStandardOnes()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard(0, 100000, 1, 255));

        box.Hexadecimal = true;
        Assert.Equal("FF", box.Text);

        box.Hexadecimal = false;
        box.DecimalPlaces = 2;
        box.ThousandsSeparator = true;
        box.Value = 1234;
        Assert.Equal(1234m.ToString("N2"), box.Text);
    }

    // ---- the standard behavior stays ----

    private static void Click(System.Windows.Forms.NumericUpDown box, int x, int y)
    {
        Control buttons = Buttons(box);
        IntPtr point = (IntPtr)((y << 16) | (x & 0xFFFF));
        SendMessage(buttons.Handle, WM_LBUTTONDOWN, (IntPtr)1, point);
        SendMessage(buttons.Handle, WM_LBUTTONUP, IntPtr.Zero, point);
        Application.DoEvents();
    }

    [Fact]
    public void TheUpperArrow_RaisesTheValue_TheLowerOneLowersIt()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard(0, 100, 5, 50));
        Control buttons = Buttons(box);

        Click(box, buttons.Width / 2, 3);
        Assert.Equal(55, box.Value);

        Click(box, buttons.Width / 2, buttons.Height - 3);
        Assert.Equal(50, box.Value);
    }

    [Fact]
    public void TheValue_StopsAtTheLimits_WhenClicking()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard(0, 10, 5, 10));
        Control buttons = Buttons(box);

        Click(box, buttons.Width / 2, 3);

        Assert.Equal(10, box.Value);
    }

    [Fact]
    public void WhilePressed_TheFieldIsPressed_AfterTheClick_NotAnymore()
    {
        System.Windows.Forms.NumericUpDown box = Show(UIStyles.NumericUpDowns.CreateStandard());
        Control buttons = Buttons(box);
        IntPtr point = (IntPtr)((3 << 16) | (buttons.Width / 2));

        SendMessage(buttons.Handle, WM_LBUTTONDOWN, (IntPtr)1, point);
        Assert.Equal(0, box.GetPrivateField<int>("_pressedField"));

        SendMessage(buttons.Handle, WM_LBUTTONUP, IntPtr.Zero, point);
        Assert.Equal(-1, box.GetPrivateField<int>("_pressedField"));
    }
}
