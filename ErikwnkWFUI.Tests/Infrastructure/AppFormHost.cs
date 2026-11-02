using System.Windows.Forms;

namespace ErikwnkWFUI.Tests.Infrastructure;

/// <summary>
/// What the tests of the windows of the library (AppForm and what is built on it) use to keep their forms: <see cref="Show"/> puts one off-screen
/// (not in the taskbar, out of the way, its messages handled), <see cref="Add"/> only takes it over - for a test that shows it itself, over an
/// owner or at a place of its own. All of them are disposed with the host.
/// </summary>
internal sealed class AppFormHost : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly int _pumpMilliseconds;

    /// <param name="pumpMilliseconds">How long the messages are handled after a form was shown (what a window needs to lay itself out and to attach its title bar).</param>
    public AppFormHost(int pumpMilliseconds = 0)
    {
        _pumpMilliseconds = pumpMilliseconds;
    }

    /// <summary>Takes the form over without showing it.</summary>
    public T Add<T>(T form) where T : Form
    {
        _forms.Add(form);

        return form;
    }

    /// <summary>Takes the form over and shows it off-screen.</summary>
    public T Show<T>(T form) where T : Form
    {
        Add(form);
        OffscreenForm.Place(form);

        if (_pumpMilliseconds > 0)
            TestPictures.Pump(_pumpMilliseconds);

        return form;
    }

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        _forms.Clear();
    }
}
