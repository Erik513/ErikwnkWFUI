using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Helpers;

namespace ErikwnkWFUI.Tests.Controls.InfoPopupFormTests;

/// <summary>A popup never takes the front from the window it is shown for: moving it with the mouse must not make it the active window (and so not hand the front to another program when it goes).</summary>
public class InfoPopupActivationTests
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);

    [Fact]
    public void ShowingThePopupAtTheMouse_LeavesTheOwnerInFront()
    {
        using Form owner = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(100, 100), Size = new Size(400, 300) };
        Button button = new Button { Location = new Point(20, 20), Size = new Size(120, 30) };
        owner.Controls.Add(button);
        owner.Show();
        owner.Activate();
        SetForegroundWindow(owner.Handle);
        Application.DoEvents();

        // (Where the machine does not let the test window to the front, there is nothing to look at.)
        if (GetForegroundWindow() != owner.Handle)
            return;

        Func<Form, bool> before = PopupSupport.OwnerIsActive;
        PopupSupport.OwnerIsActive = _ => true;

        try
        {
            using InfoPopupForm popup = UIStyles.InfoPopups.CreatePrimary();
            popup.SetContent("Hello popup");

            for (int i = 0; i < 10; i++)
            {
                popup.ShowAtMouse(button, owner.PointToScreen(new Point(30 + i, 30)));
                Application.DoEvents();
            }

            Assert.True(popup.Visible);
            Assert.Equal(owner.Handle, GetForegroundWindow());
            Assert.Same(owner, Form.ActiveForm);

            popup.Hide();
            Application.DoEvents();

            Assert.Equal(owner.Handle, GetForegroundWindow());
        }
        finally
        {
            PopupSupport.OwnerIsActive = before;
        }
    }
}
