using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// The form the tests show their controls on: borderless, not in the task bar and far off the screen (a window there is
/// laid out and gets its messages, but nothing flashes up and nothing can be clicked by accident).
/// </summary>
public static class OffscreenForm
{
    public static Form Create(Size clientSize, Color? backColor = null)
    {
        Form form = new Form
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-3000, 0),
            FormBorderStyle = FormBorderStyle.None,
            ClientSize = clientSize
        };

        if (backColor.HasValue)
            form.BackColor = backColor.Value;

        return form;
    }
}
