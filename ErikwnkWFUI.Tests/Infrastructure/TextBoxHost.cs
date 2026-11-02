using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// What the tests of the text boxes (and of the other fields) use to show a box: on the off-screen form, on a page that follows the theme as in an
/// application, laid out and with its messages handled, without a tab stop (so that no box takes the focus when the form is shown). The forms are
/// disposed with the host.
/// </summary>
internal sealed class TextBoxHost : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();

    /// <summary>Shows <paramref name="box"/> <paramref name="width"/> wide (and <paramref name="height"/> high, when given) and returns it.</summary>
    public T Show<T>(T box, int width = 160, int height = 0) where T : TextBox
    {
        box.TabStop = false;
        box.Width = width;

        if (height > 0)
            box.Height = height;

        Form form = OffscreenForm.Show(box, new Size(width + 20, Math.Max(box.Height + 20, 40)), UIColors.BackgroundMedium, new Point(10, 8));
        ThemeWatcher.FollowBackColor(form, () => UIColors.BackgroundMedium);
        _forms.Add(form);

        return box;
    }

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        _forms.Clear();
    }
}
