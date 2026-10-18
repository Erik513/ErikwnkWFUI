using System.Drawing;
using System.Security.Cryptography;
using System.Windows.Forms;
using ErikwnkWFUI.Controls;
using ErikwnkWFUI.Styles;
using ErikwnkWFUI.Tests.Infrastructure;
using Panel = System.Windows.Forms.Panel;
using ProgressBar = System.Windows.Forms.ProgressBar;

namespace ErikwnkWFUI.Tests.Styles;

/// <summary>
/// The panels, labels, progress bars, spinners, sliders and toggle switches follow a change of the theme or the accent
/// while they are shown: afterwards they look as one that is made after the change. What was set by hand stays.
/// [Collection]: the theme and the accent are switched.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class ThemeFollowSimpleControlsTests : IDisposable
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

    private T Show<T>(T control) where T : Control
    {
        if (control.Dock == DockStyle.None)
            control.Bounds = new Rectangle(5, 5, 120, 40);

        control.TabStop = false; // the form that is shown last is the active one: no focus cue on only one of two
        Form form = OffscreenForm.Create(new Size(140, 60), Color.FromArgb(40, 90, 140));
        form.Controls.Add(control);
        form.Show();
        Application.DoEvents();
        _forms.Add(form);

        return control;
    }

    // The colors and the picture of a control.
    private static string Look(Control control)
    {
        if (control is ProgressBar)
            control.Refresh(); // the bar is painted now, not whenever the message loop gets to it
        string look = $"{control.BackColor.ToArgb()} {control.ForeColor.ToArgb()}";

        using Bitmap bitmap = new Bitmap(control.Width, control.Height);
        control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
        byte[] bytes = new byte[bitmap.Width * bitmap.Height * 4];
        int i = 0;

        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                int argb = bitmap.GetPixel(x, y).ToArgb();
                bytes[i++] = (byte)argb;
                bytes[i++] = (byte)(argb >> 8);
                bytes[i++] = (byte)(argb >> 16);
                bytes[i++] = (byte)(argb >> 24);
            }

        using SHA256 sha = SHA256.Create();

        return look + " " + Convert.ToBase64String(sha.ComputeHash(bytes));
    }

    // The ones with no background of their own and the ones in a color set (a set is the same in every theme): they look the
    // same whatever the theme, and are not told to paint again. A volume control is a transparent panel whose parts (the button and the
    // slider) paint themselves: the panel itself paints nothing (its parts are looked at by the tests of the track bar and the control).
    private static readonly HashSet<string> LooksTheSameInEveryTheme = new HashSet<string>
    {
        "Panels.Transparent",
        "Panels.Standard(colors)",
        "FlowLayoutPanels.Transparent",
        "FlowLayoutPanels.Standard(colors)",
        "VersionPanels.Standard(colors)",
        "VolumeControls.Primary"
    };

    // A spinner that stands still: a turning one looks different at every moment, so no two pictures of it are the same.
    private static Spinner Still(Spinner spinner)
    {
        spinner.IsSpinning = false;

        return spinner;
    }

    // A flow panel is as big as its content: with nothing in it it has no size to look at.
    private static Control WithContent(System.Windows.Forms.FlowLayoutPanel panel)
    {
        panel.Controls.Add(new Label { Text = "Text", AutoSize = false, Size = new Size(60, 20) });

        return panel;
    }

    // A multi panel with its first page shown (and a title, so that the header has a text).
    private static Control WithPages(ErikwnkWFUI.Controls.MultiPanel panel)
    {
        panel.AddPage("First");
        panel.AddPage("Second");
        panel.ShowPageCount = true;
        panel.ShowHeaderLine = true;

        return panel;
    }

    // name and how it is made. The picture of each can be compared (a spinner is made to stand still: a turning one looks different
    // at every moment).
    public static IEnumerable<object[]> Controls()
    {
        foreach (var entry in new (string, Func<Control>)[]
        {
            ("Panels.Standard", () => UIStyles.Panels.CreateStandard()),
            ("Panels.Dark", () => UIStyles.Panels.CreateDark()),
            ("Panels.Medium", () => UIStyles.Panels.CreateMedium()),
            ("Panels.Elevated", () => UIStyles.Panels.CreateElevated()),
            ("Panels.Primary", () => UIStyles.Panels.CreatePrimary()),
            ("TableLayoutPanels.Standard", () => UIStyles.TableLayoutPanels.CreateStandard(1, 1)),
            ("TableLayoutPanels.Dark", () => UIStyles.TableLayoutPanels.CreateDark(1, 1)),
            ("Panels.Transparent", () => UIStyles.Panels.CreateTransparent()),
            ("Panels.Standard(colors)", () => UIStyles.Panels.CreateStandard(UIStyles.Colors.Sets.Green)),
            ("Panels.CardStandard", () => UIStyles.Panels.CreateCardStandard()),
            ("Panels.CardPrimary", () => UIStyles.Panels.CreateCardPrimary()),
            ("Panels.CardStandard(colors)", () => UIStyles.Panels.CreateCardStandard(UIStyles.Colors.Sets.Green)),
            ("TableLayoutPanels.Primary", () => UIStyles.TableLayoutPanels.CreatePrimary(1, 1)),
            ("TableLayoutPanels.Transparent", () => UIStyles.TableLayoutPanels.CreateTransparent(1, 1)),
            ("TableLayoutPanels.Standard(colors)", () => UIStyles.TableLayoutPanels.CreateStandard(1, 1, UIStyles.Colors.Sets.Green)),
            ("FlowLayoutPanels.Standard", () => WithContent(UIStyles.FlowLayoutPanels.CreateStandard())),
            ("FlowLayoutPanels.Primary", () => WithContent(UIStyles.FlowLayoutPanels.CreatePrimary())),
            ("FlowLayoutPanels.Transparent", () => UIStyles.FlowLayoutPanels.CreateTransparent()),
            ("FlowLayoutPanels.Standard(colors)", () => WithContent(UIStyles.FlowLayoutPanels.CreateStandard(UIStyles.Colors.Sets.Green))),
            ("MultiPanels.Standard", () => WithPages(UIStyles.MultiPanels.CreateStandard())),
            ("MultiPanels.Primary", () => WithPages(UIStyles.MultiPanels.CreatePrimary())),
            ("MultiPanels.Standard(colors)", () => WithPages(UIStyles.MultiPanels.CreateStandard(UIStyles.Colors.Sets.Green))),
            ("MultiPanels.CardStandard", () => WithPages(UIStyles.MultiPanels.CreateCardStandard())),
            ("MultiPanels.CardPrimary", () => WithPages(UIStyles.MultiPanels.CreateCardPrimary())),
            ("VersionPanels.Standard", () => UIStyles.VersionPanels.CreateStandard()),
            ("VersionPanels.Primary", () => UIStyles.VersionPanels.CreatePrimary()),
            ("VersionPanels.Standard(colors)", () => UIStyles.VersionPanels.CreateStandard(UIStyles.Colors.Sets.Green)),
            ("Labels.Standard", () => UIStyles.Labels.CreateStandard("Text")),
            ("Labels.Primary", () => UIStyles.Labels.CreatePrimary("Text")),
            ("Labels.Title", () => UIStyles.Labels.CreateTitle("Text")),
            ("Labels.Muted", () => UIStyles.Labels.CreateMuted("Text")),
            ("SlimProgressBars.Standard", () => { var b = UIStyles.SlimProgressBars.CreateStandard(); b.Value = 40; return b; }),
            ("SlimProgressBars.Primary", () => { var b = UIStyles.SlimProgressBars.CreatePrimary(); b.Value = 40; return b; }),
            ("SlimProgressBars.Status", () => { var b = UIStyles.SlimProgressBars.CreateStatus(); b.Value = 40; return b; }),
            ("SlimProgressBars.Standard(colors)", () => { var b = UIStyles.SlimProgressBars.CreateStandard(UIStyles.Colors.Sets.Teal); b.Value = 40; return b; }),
            ("ProgressBars.Standard", () => { var b = UIStyles.ProgressBars.CreateStandard(); b.Value = 40; return b; }),
            ("ProgressBars.Primary", () => { var b = UIStyles.ProgressBars.CreatePrimary(); b.Value = 40; return b; }),
            ("ProgressBars.Status", () => { var b = UIStyles.ProgressBars.CreateStatus(); b.Value = 40; return b; }),
            ("ProgressBars.Standard(colors)", () => { var b = UIStyles.ProgressBars.CreateStandard(UIStyles.Colors.Sets.Teal); b.Value = 40; return b; }),
            ("ProgressBars.Standard(BorderStyle None)", () => { var b = (ErikwnkWFUI.Controls.ProgressBar)UIStyles.ProgressBars.CreateStandard(); b.BorderStyle = System.Windows.Forms.BorderStyle.None; b.Value = 40; return b; }),
            ("Spinners.Standard", () => Still(UIStyles.Spinners.CreateStandard())),
            ("Spinners.Primary", () => Still(UIStyles.Spinners.CreatePrimary())),
            ("TrackBars.Standard", () => UIStyles.TrackBars.CreateStandard(0.4)),
            ("TrackBars.Primary", () => UIStyles.TrackBars.CreatePrimary(0.4)),
            ("VolumeControls.Primary", () =>
            {
                VolumeControl control = UIStyles.VolumeControls.CreatePrimary(0.4);
                control.Slider.TabStop = false;   // no focus cue (a bigger thumb) on only one of two

                return control;
            }),
            ("TrackBars.PrimaryWithPopup", () => UIStyles.TrackBars.CreatePrimary(0.4, 1.0, showValuePopup: true, formatValue: ErikwnkWFUI.Controls.TrackBar.FormatPercent)),
            ("ToggleSwitches.Standard", () => UIStyles.ToggleSwitches.CreateStandard(true)),
            ("ToggleSwitches.Unchecked", () => UIStyles.ToggleSwitches.CreateStandard(false)),
        })
            yield return new object[] { entry.Item1, entry.Item2 };
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterTheChange_InBothDirections(string name, Func<Control> make)
    {
        Control shown = Show(make());
        string dark = Look(shown);

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();
        Control madeInLight = Show(make());

        Assert.Equal(Look(madeInLight), Look(shown));

        UIColors.ApplyTheme(UIThemes.Dark);
        Application.DoEvents();
        Control madeInDark = Show(make());

        Assert.Equal(Look(madeInDark), Look(shown));
        Assert.Equal(dark, Look(shown));
    }

    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_LooksAsOneMadeAfterAChangeOfTheAccent(string name, Func<Control> make)
    {
        Control shown = Show(make());

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();
        Control made = Show(make());

        Assert.Equal(Look(made), Look(shown));
    }

    // The picture can be taken at any time (DrawToBitmap paints with what is read then), so the look alone does not show
    // whether a shown control was told to paint again: it is counted (a window far off the screen is not painted at all).
    [Theory]
    [MemberData(nameof(Controls))]
    public void AShownControl_IsToldToPaintAgain_WhenTheThemeChanges(string name, Func<Control> make)
    {
        if (LooksTheSameInEveryTheme.Contains(name))
            return; // nothing that is painted depends on the theme, so there is nothing to paint again

        Control shown = Show(make());
        int paints = 0;
        shown.Invalidated += (s, e) => paints++;

        UIColors.ApplyTheme(UIThemes.Light);
        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        shown.Update();
        Assert.True(paints > 0);
    }

    [Fact]
    public void AColorSetByHand_Stays()
    {
        Panel panel = Show(UIStyles.Panels.CreateDark());
        panel.BackColor = Color.Red;
        SlimProgressBar slim = Show(UIStyles.SlimProgressBars.CreatePrimary());
        slim.ForeColor = Color.Yellow;
        slim.BackColor = Color.Purple;
        ProgressBar bar = Show(UIStyles.ProgressBars.CreatePrimary());
        bar.ForeColor = Color.Yellow;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        Assert.Equal(Color.Red, panel.BackColor);
        Assert.Equal(Color.Yellow, slim.ForeColor);
        Assert.Equal(Color.Purple, slim.BackColor);
        Assert.Equal(Color.Yellow, bar.ForeColor);
    }

    [Fact]
    public void ADisabledProgressBar_KeepsItsGray_AndRestoresTheNewColor()
    {
        ProgressBar bar = Show(UIStyles.ProgressBars.CreatePrimary());
        bar.Enabled = false;

        UIColors.SetAccent(Color.OrangeRed);
        Application.DoEvents();

        Assert.Equal(UIColors.DisabledGray, bar.ForeColor);

        bar.Enabled = true;

        Assert.Equal(UIColors.Primary, bar.ForeColor);
    }

    [Fact]
    public void TheTitleBar_FollowsToo_ButNotAColorThatWasGiven()
    {
        TitleBarControl bar = Show(new TitleBarControl(null, "Title"));
        TitleBarControl given = Show(new TitleBarControl(null, "Title", backColor: Color.Navy));
        Color darkBack = bar.BackColor;

        UIColors.ApplyTheme(UIThemes.Light);
        Application.DoEvents();

        using TitleBarControl made = new TitleBarControl(null, "Title");
        Assert.NotEqual(darkBack, bar.BackColor);
        Assert.Equal(made.BackColor, bar.BackColor);
        Assert.Equal(Color.Navy, given.BackColor);
    }
}
