using System.Drawing;
using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// The form the tests show their controls on: borderless, not in the task bar and far off the screen (a window there is
/// laid out and gets its messages, but nothing flashes up and nothing can be clicked by accident).
/// </summary>
public static class OffscreenForm
{
    /// <summary>Shows a form that was made by the code under test (a window of the library) off-screen: not in the taskbar, out of the way, its messages handled.</summary>
    public static T Place<T>(T form) where T : Form
    {
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-3000, 0);
        form.Show();
        Application.DoEvents();

        return form;
    }

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

    /// <summary>
    /// Puts <paramref name="control"/> on such a form and shows it (laid out, its messages handled): the form is
    /// <paramref name="clientSize"/> big (the size of the control, at least 20 x 20, when left out) and has the color <paramref name="surface"/>.
    /// The control is placed at <paramref name="location"/>, or - <paramref name="bounds"/> - given those bounds, before it is added.
    /// The caller disposes the form.
    /// </summary>
    public static Form Show(Control control, Size? clientSize = null, Color? surface = null, Point? location = null, Rectangle? bounds = null)
    {
        Form form = Create(clientSize ?? new Size(Math.Max(control.Width, 20), Math.Max(control.Height, 20)), surface);

        if (bounds.HasValue)
            control.Bounds = bounds.Value;
        else if (location.HasValue)
            control.Location = location.Value;

        form.Controls.Add(control);
        form.Show();
        control.PerformLayout();
        Application.DoEvents();

        return form;
    }
}
