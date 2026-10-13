using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using WfuiNumericUpDown = ErikwnkWFUI.Controls.NumericUpDown;

namespace ErikwnkWFUI.Tests.Controls.NumericUpDown;

/// <summary>
/// The NumericUpDown under the odd inputs: other mouse buttons, double clicks, a handler that disables or disposes
/// the control itself, losing the mouse mid-press, no window at all, limits that leave no room, and the suffix with
/// negative numbers, a thousands separator and the events. Real window messages, as in the other edge-case tests.
/// [Collection]: the theme and the accent are read.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class NumericUpDownEdgeCaseTests : IDisposable
{
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSELEAVE = 0x02A3;
    private const int WM_CAPTURECHANGED = 0x0215;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    private readonly List<Form> _forms = new List<Form>();

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
    }

    private WfuiNumericUpDown Show(decimal min = 0, decimal max = 100, decimal increment = 5, decimal value = 50)
    {
        WfuiNumericUpDown box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreateStandard(min, max, increment, value);
        box.Location = new Point(10, 10);
        box.Width = 110;
        box.TabStop = false;
        Form form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-3000, 0), FormBorderStyle = FormBorderStyle.None, ClientSize = new Size(140, 60), BackColor = UIColors.BackgroundMedium };
        form.Controls.Add(box);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return box;
    }

    private static Control Buttons(System.Windows.Forms.NumericUpDown box) => box.Controls.Cast<Control>().Single(c => c.GetType().Name == "UpDownButtons");

    private static IntPtr Point(int x, int y) => (IntPtr)((y << 16) | (x & 0xFFFF));

    private static void Send(System.Windows.Forms.NumericUpDown box, int message, int x, int y, int keys = 0)
    {
        SendMessage(Buttons(box).Handle, message, (IntPtr)keys, Point(x, y));
        Application.DoEvents();
    }

    private static int Upper(System.Windows.Forms.NumericUpDown box) => 3;

    private static int Lower(System.Windows.Forms.NumericUpDown box) => Buttons(box).Height - 3;

    private static int Pressed(WfuiNumericUpDown box) => box.GetPrivateField<int>("_pressedField");

    private static int Hot(WfuiNumericUpDown box) => box.GetPrivateField<int>("_hotField");

    // ---- other mouse buttons, double click ----

    [Theory]
    [InlineData(WM_RBUTTONDOWN, WM_RBUTTONUP)]
    [InlineData(WM_MBUTTONDOWN, WM_MBUTTONUP)]
    public void TheOtherMouseButtons_ChangeNothing_AndShowNoPressedField(int down, int up)
    {
        WfuiNumericUpDown box = Show();
        int x = Buttons(box).Width / 2;

        Send(box, down, x, Upper(box));
        Assert.Equal(-1, Pressed(box));

        Send(box, up, x, Upper(box));

        Assert.Equal(50, box.Value);
        Assert.Equal(-1, Pressed(box));
    }

    [Fact]
    public void ADoubleClick_LeavesNoPressedField_AndTheValueInsideTheLimits()
    {
        WfuiNumericUpDown box = Show(0, 100, 5, 50);
        int x = Buttons(box).Width / 2;

        Send(box, WM_LBUTTONDOWN, x, Upper(box), 1);
        Send(box, WM_LBUTTONUP, x, Upper(box));
        Send(box, WM_LBUTTONDBLCLK, x, Upper(box), 1);
        Send(box, WM_LBUTTONUP, x, Upper(box));

        Assert.Equal(-1, Pressed(box));
        Assert.InRange(box.Value, 55, 60);
    }

    [Fact]
    public void AnArrowReleasedOnTheOtherField_LeavesNoPressedField()
    {
        WfuiNumericUpDown box = Show();
        int x = Buttons(box).Width / 2;

        Send(box, WM_LBUTTONDOWN, x, Upper(box), 1);
        Assert.Equal(0, Pressed(box));

        Send(box, WM_LBUTTONUP, x, Lower(box));

        Assert.Equal(-1, Pressed(box));
    }

    // ---- losing the mouse ----

    [Fact]
    public void LosingTheMouseCapture_WhilePressed_ClearsThePressedField()
    {
        WfuiNumericUpDown box = Show();
        Send(box, WM_LBUTTONDOWN, Buttons(box).Width / 2, Upper(box), 1);

        SendMessage(Buttons(box).Handle, WM_CAPTURECHANGED, IntPtr.Zero, IntPtr.Zero);

        Assert.Equal(-1, Pressed(box));
    }

    [Fact]
    public void TheMouseLeaving_WhilePressed_ClearsPressedAndHot()
    {
        WfuiNumericUpDown box = Show();
        Send(box, WM_LBUTTONDOWN, Buttons(box).Width / 2, Upper(box), 1);

        SendMessage(Buttons(box).Handle, WM_MOUSELEAVE, IntPtr.Zero, IntPtr.Zero);

        Assert.Equal(-1, Pressed(box));
        Assert.Equal(-1, Hot(box));
    }

    // ---- a handler that does something to the control itself ----

    [Fact]
    public void AHandlerThatDisablesTheControl_LeavesItDisabledAndPaintedAsDisabled()
    {
        WfuiNumericUpDown box = Show();
        box.ValueChanged += (sender, e) => box.Enabled = false;

        Send(box, WM_LBUTTONDOWN, Buttons(box).Width / 2, Upper(box), 1);
        Send(box, WM_LBUTTONUP, Buttons(box).Width / 2, Upper(box));

        Assert.False(box.Enabled);
        Assert.Equal(-1, Pressed(box));
        Assert.Equal(-1, Hot(box));

        using Bitmap bitmap = new Bitmap(box.Width, box.Height);
        box.DrawToBitmap(bitmap, new Rectangle(System.Drawing.Point.Empty, box.Size));
        int border = (box.Width - box.ClientSize.Width) / 2;
        Control buttons = Buttons(box);

        Assert.Equal(WfuiNumericUpDown.GetStateColors(WfuiNumericUpDown.GetNeutralSet(), 3, box.BackColor).Fill.ToArgb(), bitmap.GetPixel(border + buttons.Left + 4, border + buttons.Top + 4).ToArgb());

        box.Enabled = true;
        Assert.True(box.Enabled);
    }

    [Fact]
    public void AHandlerThatDisposesTheControl_DoesNotThrow_AndTheListenersAreGone()
    {
        int before = Listeners();
        WfuiNumericUpDown box = Show();
        Control parent = box.Parent!;
        box.ValueChanged += (sender, e) => box.Dispose();

        Send(box, WM_LBUTTONDOWN, Buttons(box).Width / 2, Upper(box), 1);
        Application.DoEvents();

        Assert.True(box.IsDisposed);

        parent.Dispose();
        Assert.Equal(before, Listeners());
    }

    [Fact]
    public void ADisposedControl_CanBeAskedForItsColors_WithoutAnError()
    {
        WfuiNumericUpDown box = Show();
        box.Dispose();

        Exception? error = Record.Exception(() =>
        {
            _ = box.Colors;
            box.Colors = UIStyles.Colors.Sets.Green;
        });

        Assert.Null(error);
    }

    private static int Listeners()
    {
        FieldInfo field = typeof(UIColors).GetField("ThemeChanged", BindingFlags.NonPublic | BindingFlags.Static)!;
        Delegate? handlers = (Delegate?)field.GetValue(null);

        return handlers == null ? 0 : handlers.GetInvocationList().Length;
    }

    // ---- no window ----

    [Fact]
    public void WithoutAWindow_NothingNeedsOne_AndNothingListens()
    {
        int before = Listeners();

        using WfuiNumericUpDown box = (WfuiNumericUpDown)UIStyles.NumericUpDowns.CreatePrimary(0, 10, 1, 5);
        box.Colors = UIStyles.Colors.Sets.Green;
        box.Suffix = " %";
        box.Prefix = "~";
        box.Enabled = false;
        box.Font = new Font("Segoe UI", 14);

        Assert.Equal("~5 %", box.Text);
        Assert.Equal(before, Listeners());
    }

    // ---- limits ----

    [Fact]
    public void AMinimumEqualToTheMaximum_LeavesNoRoomForTheArrows()
    {
        WfuiNumericUpDown box = Show(7, 7, 1, 7);
        int x = Buttons(box).Width / 2;

        Send(box, WM_LBUTTONDOWN, x, Upper(box), 1);
        Send(box, WM_LBUTTONUP, x, Upper(box));
        Send(box, WM_LBUTTONDOWN, x, Lower(box), 1);
        Send(box, WM_LBUTTONUP, x, Lower(box));

        Assert.Equal(7, box.Value);
        Assert.Equal(-1, Pressed(box));
    }

    [Fact]
    public void AnIncrementBiggerThanTheRange_StopsAtTheLimit()
    {
        WfuiNumericUpDown box = Show(0, 10, 1000, 5);
        int x = Buttons(box).Width / 2;

        Send(box, WM_LBUTTONDOWN, x, Upper(box), 1);
        Send(box, WM_LBUTTONUP, x, Upper(box));
        Assert.Equal(10, box.Value);

        Send(box, WM_LBUTTONDOWN, x, Lower(box), 1);
        Send(box, WM_LBUTTONUP, x, Lower(box));
        Assert.Equal(0, box.Value);
    }

    [Fact]
    public void ANegativeRange_WorksWithTheArrows()
    {
        WfuiNumericUpDown box = Show(-10, 10, 5, 0);
        int x = Buttons(box).Width / 2;

        Send(box, WM_LBUTTONDOWN, x, Lower(box), 1);
        Send(box, WM_LBUTTONUP, x, Lower(box));

        Assert.Equal(-5, box.Value);
    }

    // ---- the suffix in the odd cases ----

    [Fact]
    public void ANegativeNumber_IsReadWithTheSuffix()
    {
        WfuiNumericUpDown box = Show(-100, 100, 5, 0);
        box.Suffix = " %";

        box.Text = "-25 %";
        box.InvokePrivate("ValidateEditText");

        Assert.Equal(-25, box.Value);
        Assert.Equal("-25 %", box.Text);
    }

    [Fact]
    public void AThousandsSeparator_AndASuffix_WorkTogether()
    {
        WfuiNumericUpDown box = Show(0, 100000, 100, 1234);
        box.ThousandsSeparator = true;
        box.Suffix = " m";

        Assert.Equal(1234m.ToString("N0") + " m", box.Text);

        box.Text = 5678m.ToString("N0") + " m";
        box.InvokePrivate("ValidateEditText");

        Assert.Equal(5678, box.Value);
    }

    [Fact]
    public void TheSuffixTwice_IsNotAnumber_AndTheValueStays()
    {
        WfuiNumericUpDown box = Show();
        box.Suffix = " %";

        box.Text = "70 % %";
        box.InvokePrivate("ValidateEditText");

        Assert.Equal(50, box.Value);
        Assert.Equal("50 %", box.Text);
    }

    [Fact]
    public void ASuffixThatIsOnlySpaces_IsHandled()
    {
        WfuiNumericUpDown box = Show();

        box.Suffix = "  ";
        box.Text = "60";
        box.InvokePrivate("ValidateEditText");

        Assert.Equal(60, box.Value);
    }

    [Fact]
    public void ANullAffix_IsTheEmptyOne()
    {
        WfuiNumericUpDown box = Show();

        box.Prefix = null!;
        box.Suffix = null!;

        Assert.Equal("", box.Prefix);
        Assert.Equal("", box.Suffix);
        Assert.Equal("50", box.Text);
    }

    [Fact]
    public void AnArrowStep_RaisesValueChangedOnce_WithASuffixToo()
    {
        WfuiNumericUpDown box = Show();
        box.Suffix = " %";
        int raised = 0;
        box.ValueChanged += (sender, e) => raised++;
        int x = Buttons(box).Width / 2;

        Send(box, WM_LBUTTONDOWN, x, Upper(box), 1);
        Send(box, WM_LBUTTONUP, x, Upper(box));

        Assert.Equal(1, raised);
        Assert.Equal(55, box.Value);
    }

    [Fact]
    public void SettingTheValueByCode_RaisesValueChangedOnce_WithASuffixToo()
    {
        WfuiNumericUpDown box = Show();
        box.Suffix = " %";
        int raised = 0;
        box.ValueChanged += (sender, e) => raised++;

        box.Value = 80;

        Assert.Equal(1, raised);
        Assert.Equal("80 %", box.Text);
    }

    [Fact]
    public void TheAffixes_SurviveTheHandleBeingCreatedAgain()
    {
        WfuiNumericUpDown box = Show();
        box.Prefix = "~";
        box.Suffix = " %";

        box.InvokePrivate("RecreateHandle");
        Application.DoEvents();

        Assert.Equal("~50 %", box.Text);
        Assert.Equal(50, box.Value);
    }

    [Fact]
    public void TheDecimalNumber_IsReadInTheCultureOfTheBox()
    {
        WfuiNumericUpDown box = Show(0, 1000, 1, 1);
        box.DecimalPlaces = 1;
        box.Suffix = " ms";
        string typed = 12.5m.ToString("N1", CultureInfo.CurrentCulture) + " ms";

        box.Text = typed;
        box.InvokePrivate("ValidateEditText");

        Assert.Equal(12.5m, box.Value);
    }
}
