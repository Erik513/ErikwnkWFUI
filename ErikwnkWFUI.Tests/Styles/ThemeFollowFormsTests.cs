using System.Drawing;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The property table and the forms of the library follow a change of the theme or the accent while they are shown:
/// afterwards they look as one made after the change. A color given by hand stays.
/// [Collection]: the theme and the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeFollowFormsTests : IDisposable
{
    private readonly List<Form> _forms = new List<Form>();
    private readonly Color _accentAtStart = UIColors.Primary;

    public void Dispose()
    {
        foreach (Form form in _forms)
            form.Dispose();

        UIColors.ApplyTheme(UIThemes.Dark);
        UIColors.SetAccent(_accentAtStart);
    }

    private T Open<T>(T form) where T : Form
    {
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-3000, 0);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return form;
    }

    // The colors of the form and of everything in it that has one.
    private static string Look(Control control)
    {
        List<string> colors = new List<string>();

        void Walk(Control c)
        {
            if (!c.Visible)
                return; // without a window it catches up when it gets one (see the test for that)

            colors.Add($"{c.GetType().Name}:{c.BackColor.ToArgb()}:{c.ForeColor.ToArgb()}");

            foreach (Control child in c.Controls)
                Walk(child);
        }

        Walk(control);

        return string.Join(" ", colors);
    }

    private static PropertyTable MakeTable()
    {
        PropertyTable table = UIStyles.PropertyTables.CreateStandard();
        table.AddSection("Section");
        table.AddRow("Name", UIStyles.TextBoxes.CreateStandard("x"));
        table.AddRow("On", UIStyles.CheckBoxes.CreateStandard("c"));

        return table;
    }

    private static StyledForm MakeStyledForm(bool withTable)
    {
        StyledForm form = new StyledForm(StyledFormOptions.CreateStandard("Title"));

        if (withTable)
            form.ContentPanel.Controls.Add(MakeTable());

        return form;
    }

    public static IEnumerable<object[]> Forms()
    {
        foreach (var entry in new (string, Func<Form>)[]
        {
            ("StyledForm", () => MakeStyledForm(true)),
            ("MessageBoxForm", () => new MessageBoxForm("Message", "Title", ErikwnkWFUI.Forms.MessageBoxButtons.OKCancel, ErikwnkWFUI.Forms.MessageBoxIcon.Info)),
            ("StyledOptionsForm", () => MakeOptionsForm()),
        })
            yield return new object[] { entry.Item1, entry.Item2 };
    }

    private static Form MakeOptionsForm() => MakeStyledForm(true);

    [Theory]
    [MemberData(nameof(Forms))]
    public void AShownForm_LooksAsOneMadeAfterTheChange_InBothDirections(string name, Func<Form> make)
    {
        Form shown = Open(make());
        string dark = Look(shown);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Form madeInLight = Open(make());

        Assert.NotEqual(dark, Look(shown));
        Assert.Equal(Look(madeInLight), Look(shown));

        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();
        Form madeInDark = Open(make());

        Assert.Equal(Look(madeInDark), Look(shown));
        Assert.Equal(dark, Look(shown));
    }

    [Theory]
    [MemberData(nameof(Forms))]
    public void AShownForm_LooksAsOneMadeAfterAChangeOfTheAccent(string name, Func<Form> make)
    {
        Form shown = Open(make());

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.Equal(Look(Open(make())), Look(shown));
    }

    [Fact]
    public void AControlWithoutAWindow_CatchesUpWhenItGetsOne()
    {
        // Hidden (never had a window) while the theme changes, shown afterwards.
        Form host = Open(new Form { ClientSize = new Size(200, 100) });
        Button hidden = UIStyles.Buttons.CreateStandard("Text");
        hidden.Visible = false;
        host.Controls.Add(hidden);
        Panel panel = UIStyles.Panels.CreateMedium();
        panel.Visible = false;
        host.Controls.Add(panel);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        hidden.Visible = true;
        panel.Visible = true;
        Application.DoEvents();

        Assert.Equal(UIColors.BackgroundMedium, hidden.BackColor);
        Assert.Equal(UIColors.BackgroundMedium, panel.BackColor);
        Assert.Equal(UIColors.TextPrimary, hidden.ForeColor);
    }

    [Fact]
    public void ATitleBarColorGivenByHand_Stays()
    {
        StyledForm form = Open(new StyledForm(StyledFormOptions.CreateStandard("Title", backColor: Color.Navy)));

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(Color.Navy, form.TitleBar.BackColor);
    }

    [Fact]
    public void ThePropertyTableAlone_FollowsToo()
    {
        Form host = Open(new Form { ClientSize = new Size(300, 200) });
        PropertyTable table = MakeTable();
        host.Controls.Add(table);
        string dark = Look(table);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Form other = Open(new Form { ClientSize = new Size(300, 200) });
        PropertyTable made = MakeTable();
        other.Controls.Add(made);

        Assert.NotEqual(dark, Look(table));
        Assert.Equal(Look(made), Look(table));
    }
}
