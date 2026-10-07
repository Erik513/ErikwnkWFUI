using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Controls.ComboBox;

/// <summary>
/// Odd input on the combo box: other mouse buttons, the same key twice, the box losing its
/// state while open, plus the editable style's frame and the list's frame colors.
/// [Collection]: the colors read the live accent.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ComboBoxInputEdgeTests : IDisposable
{
    private readonly List<Form> _hosts = new List<Form>();

    public void Dispose()
    {
        foreach (Form host in _hosts)
            host.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr dc);

    [DllImport("gdi32.dll")]
    private static extern int GetTextColor(IntPtr dc);

    private ErikwnkWFUI.Controls.ComboBox Show(ComboBoxStyle style = ComboBoxStyle.DropDownList, bool primary = false)
    {
        ErikwnkWFUI.Controls.ComboBox box = (ErikwnkWFUI.Controls.ComboBox)(primary
            ? UIStyles.ComboBoxes.CreatePrimary(style)
            : UIStyles.ComboBoxes.CreateStandard(style));
        box.Items.AddRange(new object[] { "One", "Two", "Three" });
        box.Width = 160;
        box.Location = new Point(10, 10);

        Form host = new Form
        {
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-4000, -4000),
            ShowInTaskbar = false,
            ClientSize = new Size(300, 260),
            BackColor = UIColors.BackgroundMedium
        };

        host.Controls.Add(box);
        host.Show();
        _hosts.Add(host);
        Pump();

        return box;
    }

    private static void Pump()
    {
        for (int i = 0; i < 4; i++)
        {
            Application.DoEvents();
            Thread.Sleep(30);
        }
    }

    private static void Press(Control control, int down, int up, int buttonFlag)
    {
        IntPtr at = (IntPtr)(20 | (10 << 16));
        SendMessage(control.Handle, down, (IntPtr)buttonFlag, at);
        SendMessage(control.Handle, up, IntPtr.Zero, at);
        Pump();
    }

    private static void Key(Control control, Keys key)
    {
        SendMessage(control.Handle, 0x100, (IntPtr)(int)key, IntPtr.Zero);
        SendMessage(control.Handle, 0x101, (IntPtr)(int)key, IntPtr.Zero);
        Pump();
    }

    [Fact]
    public void TheRightAndTheMiddleButton_DoNotOpenTheList()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();

        Press(box, 0x204, 0x205, 2);
        Assert.False(box.DroppedDown, "the right button opened the list");

        Press(box, 0x207, 0x208, 16);
        Assert.False(box.DroppedDown, "the middle button opened the list");
    }

    [Fact]
    public void F4Twice_OpensAndClosesTheList_WithOneEventEach()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        box.Select();
        Pump();
        int opened = 0;
        int closed = 0;
        box.DropDown += delegate { opened++; };
        box.DropDownClosed += delegate { closed++; };

        Key(box, Keys.F4);
        Assert.True(box.DroppedDown);

        Key(box, Keys.F4);
        Assert.False(box.DroppedDown);
        Assert.Equal(1, opened);
        Assert.Equal(1, closed);
    }

    [Fact]
    public void ADisabledBox_WhileOpen_ClosesAndShowsTheDisabledBorder()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show();
        Press(box, 0x201, 0x202, 1);
        Assert.True(box.DroppedDown);

        box.Enabled = false;
        Pump();

        Assert.Equal(UIColors.DisabledGray, box.BorderColor);
        Assert.Equal(UIColors.DisabledGray, box.ListBorderColor);
    }

    [Fact]
    public void TheListFrame_IsTheBorderOfAnOpenBox_GrayForStandardAndAccentForPrimary()
    {
        ErikwnkWFUI.Controls.ComboBox standard = Show();
        ErikwnkWFUI.Controls.ComboBox primary = Show(primary: true);

        Assert.Equal(
            UIColors.EnsureContrast(UIStyles.Colors.Sets.Gray.Pressed, standard.BackColor, 6.0),
            standard.ListBorderColor);
        Assert.Equal(
            UIColors.EnsureContrast(UIColors.PrimarySet.Pressed, primary.BackColor, 6.0),
            primary.ListBorderColor);
        Assert.NotEqual(standard.ListBorderColor, primary.ListBorderColor);
    }

    [Fact]
    public void TheEditableStyle_FollowsTheHoverBorder_AndTheDisabledTextFieldIsMutedNotWhite()
    {
        ErikwnkWFUI.Controls.ComboBox box = Show(ComboBoxStyle.DropDown);
        Color rest = box.BorderColor;

        box.InvokePrivate("OnMouseEnter", EventArgs.Empty);
        Assert.NotEqual(rest, box.BorderColor);

        box.InvokePrivate("OnMouseLeave", EventArgs.Empty);
        Assert.Equal(rest, box.BorderColor);

        // A disabled text field asks for its colors as a "static" control.
        box.Enabled = false;
        Pump();
        IntPtr dc = GetDC(box.Handle);

        try
        {
            SendMessage(box.Handle, 0x138, dc, box.Handle);
            Assert.Equal(ColorTranslator.ToWin32(ErikwnkWFUI.Controls.ComboBox.GetDisabledTextColor(box.BackColor)), GetTextColor(dc));
        }
        finally
        {
            ReleaseDC(box.Handle, dc);
        }
    }
}
