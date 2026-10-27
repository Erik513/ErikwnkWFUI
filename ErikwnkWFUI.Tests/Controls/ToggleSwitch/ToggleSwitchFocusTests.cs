using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using static ErikwnkWFUI.Tests.Infrastructure.TestPictures;
using ToggleSwitch = ErikwnkWFUI.Controls.ToggleSwitch;

namespace ErikwnkWFUI.Tests.Controls.ToggleSwitches;

/// <summary>The focus frame of the switch. It needs the real focus (the window is activated), so it is on its own: it takes the focus from whatever else is open on the computer while it runs. [Collection]: the focus belongs to the whole process.</summary>
[Collection(FocusTestCollection.Name)]
public class ToggleSwitchFocusTests
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

    private static Form Show(ToggleSwitch toggle)
    {
        Form form = OffscreenForm.Create(new Size(toggle.Width + 10, toggle.Height + 10), Color.FromArgb(20, 20, 20));
        toggle.Location = new Point(5, 5);
        form.Controls.Add(toggle);
        form.Show();
        form.Activate();
        Application.DoEvents();

        return form;
    }

    [Fact]
    public void TheFocusFrame_IsShownOnlyWithTheFocusAndAfterTheKeyboardWasUsed()
    {
        StaThread.Run(() =>
        {
            using ToggleSwitch toggle = UIStyles.ToggleSwitches.CreatePrimary(true);
            using Form form = Show(toggle);
            using Bitmap resting = Draw(toggle);

            toggle.Focus();
            Application.DoEvents();
            Assert.True(toggle.Focused);

            // Hidden until the keyboard is used (UIS_CLEAR + UISF_HIDEFOCUS shows the cues, as a key press does).
            using Bitmap withoutCues = Draw(toggle);
            Assert.True(SamePixels(resting, withoutCues), "the frame is there before the keyboard has been used");

            SendMessage(toggle.Handle, 0x0128, (IntPtr)((1 << 16) | 2), IntPtr.Zero);
            Application.DoEvents();
            using Bitmap withCues = Draw(toggle);
            Assert.False(SamePixels(resting, withCues), "no focus frame");
        });
    }
}
